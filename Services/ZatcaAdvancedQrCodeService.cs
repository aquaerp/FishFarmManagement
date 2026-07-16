using System.Globalization;
using System.Security.Cryptography;
using System.Security.Cryptography.X509Certificates;
using System.Text;
using FishFarmManager.Models;

namespace FishFarmManager.Services;

public sealed record ZatcaQrCryptographicFields(
    byte[] InvoiceHash,
    byte[] EcdsaSignature,
    byte[] EcdsaPublicKey,
    byte[]? TechnicalCaSignature);

public sealed record ZatcaAdvancedQrRequest(
    ZatcaInvoiceProfile Profile,
    string SellerName,
    string SellerVatNumber,
    DateTimeOffset IssueDateTime,
    decimal TaxInclusiveAmount,
    decimal VatAmount,
    ZatcaQrCryptographicFields CryptographicFields);

public sealed record ZatcaSigningMaterialEvidence(
    string CertificateSha256Base64,
    string CertificateSubject,
    string CertificateIssuer,
    string CertificateSerialNumber,
    DateTime NotBeforeUtc,
    DateTime NotAfterUtc);

public sealed class ZatcaAdvancedQrCodeService
{
    public string GenerateBase64(ZatcaAdvancedQrRequest request)
    {
        var result = Convert.ToBase64String(GenerateTlv(request));
        if (result.Length > 700)
            throw new InvalidOperationException("The ZATCA QR Base64 payload exceeds the 700-character limit.");
        return result;
    }

    public byte[] GenerateTlv(ZatcaAdvancedQrRequest request)
    {
        ArgumentNullException.ThrowIfNull(request);
        ArgumentNullException.ThrowIfNull(request.CryptographicFields);
        if (string.IsNullOrWhiteSpace(request.SellerName))
            throw new ArgumentException("Seller name is required.", nameof(request));
        if (string.IsNullOrWhiteSpace(request.SellerVatNumber))
            throw new ArgumentException("Seller VAT number is required.", nameof(request));
        if (request.TaxInclusiveAmount < 0 || request.VatAmount < 0)
            throw new ArgumentOutOfRangeException(nameof(request), "QR monetary values cannot be negative.");

        var crypto = request.CryptographicFields;
        if (crypto.InvoiceHash is not { Length: 32 })
            throw new InvalidOperationException("ZATCA QR tag 6 must contain the raw 32-byte SHA-256 invoice hash.");
        RequireCryptographicValue(crypto.EcdsaSignature, 7);
        RequireCryptographicValue(crypto.EcdsaPublicKey, 8);
        if (request.Profile == ZatcaInvoiceProfile.Simplified)
            RequireCryptographicValue(crypto.TechnicalCaSignature, 9);
        else if (crypto.TechnicalCaSignature is { Length: > 0 })
            throw new InvalidOperationException("ZATCA QR tag 9 is only included for simplified documents and their notes.");

        using var output = new MemoryStream();
        WriteUtf8(output, 1, request.SellerName.Trim());
        WriteUtf8(output, 2, request.SellerVatNumber.Trim());
        WriteUtf8(output, 3, request.IssueDateTime.ToUniversalTime().ToString("yyyy-MM-dd'T'HH:mm:ss'Z'", CultureInfo.InvariantCulture));
        WriteUtf8(output, 4, request.TaxInclusiveAmount.ToString("0.00", CultureInfo.InvariantCulture));
        WriteUtf8(output, 5, request.VatAmount.ToString("0.00", CultureInfo.InvariantCulture));
        WriteRaw(output, 6, crypto.InvoiceHash);
        WriteRaw(output, 7, crypto.EcdsaSignature);
        WriteRaw(output, 8, crypto.EcdsaPublicKey);
        if (request.Profile == ZatcaInvoiceProfile.Simplified)
            WriteRaw(output, 9, crypto.TechnicalCaSignature!);
        return output.ToArray();
    }

    private static void RequireCryptographicValue(byte[]? value, byte tag)
    {
        if (value is not { Length: > 0 })
            throw new InvalidOperationException($"ZATCA QR tag {tag} is required after cryptographic stamping.");
        if (value.Length > byte.MaxValue)
            throw new InvalidOperationException($"ZATCA QR tag {tag} exceeds the one-byte TLV length limit.");
    }

    private static void WriteUtf8(Stream output, byte tag, string value)
        => WriteRaw(output, tag, Encoding.UTF8.GetBytes(value));

    private static void WriteRaw(Stream output, byte tag, byte[] value)
    {
        if (value.Length > byte.MaxValue)
            throw new InvalidOperationException($"ZATCA QR tag {tag} exceeds the one-byte TLV length limit.");
        output.WriteByte(tag);
        output.WriteByte((byte)value.Length);
        output.Write(value);
    }
}

public sealed class ZatcaSigningMaterialValidator
{
    public ZatcaSigningMaterialEvidence Validate(
        X509Certificate2 certificate,
        ECDsa privateKey,
        DateTimeOffset validationTime)
    {
        ArgumentNullException.ThrowIfNull(certificate);
        ArgumentNullException.ThrowIfNull(privateKey);
        var utc = validationTime.UtcDateTime;
        if (utc < certificate.NotBefore.ToUniversalTime() || utc > certificate.NotAfter.ToUniversalTime())
            throw new InvalidOperationException("The CSID certificate is not valid at the requested signing time.");
        if (certificate.PublicKey.Oid?.Value != "1.2.840.10045.2.1")
            throw new InvalidOperationException("The CSID certificate must contain an ECDSA public key.");
        using var publicKey = certificate.GetECDsaPublicKey()
            ?? throw new InvalidOperationException("The CSID ECDSA public key could not be read.");
        if (publicKey.KeySize != 256 || privateKey.KeySize != 256)
            throw new InvalidOperationException("ZATCA cryptographic stamping requires a 256-bit ECDSA key.");
        var keyUsage = certificate.Extensions.OfType<X509KeyUsageExtension>().SingleOrDefault();
        if (keyUsage is not null && !keyUsage.KeyUsages.HasFlag(X509KeyUsageFlags.DigitalSignature))
            throw new InvalidOperationException("The CSID certificate is not authorized for digital signatures.");

        var challenge = SHA256.HashData(Encoding.UTF8.GetBytes("ZATCA-CSID-KEY-PAIR-CHECK-v1"));
        var signature = privateKey.SignHash(challenge, DSASignatureFormat.IeeeP1363FixedFieldConcatenation);
        if (!publicKey.VerifyHash(challenge, signature, DSASignatureFormat.IeeeP1363FixedFieldConcatenation))
            throw new InvalidOperationException("The private key does not match the CSID certificate.");

        return new ZatcaSigningMaterialEvidence(
            Convert.ToBase64String(SHA256.HashData(certificate.RawData)),
            certificate.Subject,
            certificate.Issuer,
            certificate.SerialNumber,
            certificate.NotBefore.ToUniversalTime(),
            certificate.NotAfter.ToUniversalTime());
    }
}
