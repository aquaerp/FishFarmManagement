using System.Security.Cryptography;
using System.Security.Cryptography.X509Certificates;
using System.Text;
using FishFarmManager.Models;
using FishFarmManager.Services;

namespace FishFarmManager.Tests;

public sealed class Phase4ZatcaAdvancedQrTests
{
    [Fact]
    public void SimplifiedQr_PreservesBinaryCryptographicTagsAndRequiresTag9()
    {
        var hash = Enumerable.Range(0, 32).Select(value => (byte)value).ToArray();
        var signature = new byte[] { 0, 255, 1, 128, 2 };
        var publicKey = new byte[] { 4, 10, 0, 20 };
        var caSignature = new byte[] { 48, 70, 0, 255 };
        var request = CreateRequest(ZatcaInvoiceProfile.Simplified,
            new ZatcaQrCryptographicFields(hash, signature, publicKey, caSignature));

        var tlv = new ZatcaAdvancedQrCodeService().GenerateTlv(request);
        var fields = Decode(tlv);

        Assert.Equal(9, fields.Count);
        Assert.Equal("مزرعة الأسماك", Encoding.UTF8.GetString(fields[1]));
        Assert.Equal("2026-07-16T09:30:00Z", Encoding.UTF8.GetString(fields[3]));
        Assert.Equal("115.00", Encoding.UTF8.GetString(fields[4]));
        Assert.Equal(hash, fields[6]);
        Assert.Equal(signature, fields[7]);
        Assert.Equal(publicKey, fields[8]);
        Assert.Equal(caSignature, fields[9]);

        var missingTag9 = request with
        {
            CryptographicFields = request.CryptographicFields with { TechnicalCaSignature = null }
        };
        Assert.Throws<InvalidOperationException>(() => new ZatcaAdvancedQrCodeService().GenerateTlv(missingTag9));
    }

    [Fact]
    public void StandardQr_ContainsTags1Through8AndRejectsTag9()
    {
        var crypto = new ZatcaQrCryptographicFields(new byte[32], new byte[64], new byte[65], null);
        var request = CreateRequest(ZatcaInvoiceProfile.Standard, crypto);
        var fields = Decode(new ZatcaAdvancedQrCodeService().GenerateTlv(request));
        Assert.Equal(Enumerable.Range(1, 8).Select(value => (byte)value), fields.Keys);

        var invalid = request with { CryptographicFields = crypto with { TechnicalCaSignature = new byte[] { 1 } } };
        Assert.Throws<InvalidOperationException>(() => new ZatcaAdvancedQrCodeService().GenerateTlv(invalid));
    }

    [Fact]
    public void Qr_RejectsNonSha256Tag6AndOversizedFields()
    {
        var invalidHash = CreateRequest(ZatcaInvoiceProfile.Standard,
            new ZatcaQrCryptographicFields(new byte[31], new byte[64], new byte[65], null));
        Assert.Throws<InvalidOperationException>(() => new ZatcaAdvancedQrCodeService().GenerateTlv(invalidHash));

        var oversized = CreateRequest(ZatcaInvoiceProfile.Standard,
            new ZatcaQrCryptographicFields(new byte[32], new byte[256], new byte[65], null));
        Assert.Throws<InvalidOperationException>(() => new ZatcaAdvancedQrCodeService().GenerateTlv(oversized));
    }

    [Fact]
    public void SigningMaterialValidator_ProvesCertificateAndPrivateKeyMatch()
    {
        using var key = ECDsa.Create(ECCurve.NamedCurves.nistP256);
        var request = new CertificateRequest("CN=Test EGS,O=Fish Farm,C=SA", key, HashAlgorithmName.SHA256);
        request.CertificateExtensions.Add(new X509KeyUsageExtension(X509KeyUsageFlags.DigitalSignature, true));
        using var certificate = request.CreateSelfSigned(DateTimeOffset.UtcNow.AddDays(-1), DateTimeOffset.UtcNow.AddDays(30));

        var evidence = new ZatcaSigningMaterialValidator().Validate(certificate, key, DateTimeOffset.UtcNow);

        Assert.NotEmpty(evidence.CertificateSha256Base64);
        Assert.Contains("Test EGS", evidence.CertificateSubject);
        using var otherKey = ECDsa.Create(ECCurve.NamedCurves.nistP256);
        Assert.Throws<InvalidOperationException>(() =>
            new ZatcaSigningMaterialValidator().Validate(certificate, otherKey, DateTimeOffset.UtcNow));
    }

    private static ZatcaAdvancedQrRequest CreateRequest(
        ZatcaInvoiceProfile profile,
        ZatcaQrCryptographicFields fields) => new(
            profile,
            "مزرعة الأسماك",
            "310123456700003",
            new DateTimeOffset(2026, 7, 16, 12, 30, 0, TimeSpan.FromHours(3)),
            115m,
            15m,
            fields);

    private static SortedDictionary<byte, byte[]> Decode(byte[] tlv)
    {
        var result = new SortedDictionary<byte, byte[]>();
        for (var offset = 0; offset < tlv.Length;)
        {
            var tag = tlv[offset++];
            var length = tlv[offset++];
            result.Add(tag, tlv.AsSpan(offset, length).ToArray());
            offset += length;
        }
        return result;
    }
}
