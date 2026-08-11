using System.Formats.Asn1;
using System.Security.Cryptography;
using System.Security.Cryptography.X509Certificates;
using System.Text;
using System.Xml;
using System.Xml.Linq;
using FishFarmManager.Models;
using FishFarmManager.Services;

namespace FishFarmManager.Tests;

public sealed class Phase4ZatcaStampedInvoiceTests
{
    [Fact]
    public void Stamp_BuildsSimplifiedQrFromCertificateAndKeepsXadesValid()
    {
        const ZatcaInvoiceProfile profile = ZatcaInvoiceProfile.Simplified;
        var signingTime = new DateTimeOffset(2026, 7, 16, 9, 30, 0, TimeSpan.Zero);
        using var key = ECDsa.Create(ECCurve.NamedCurves.nistP256);
        using var certificate = Certificate(key, signingTime);
        var unsignedXml = new ZatcaUblGenerator().Generate(Request(profile)).ToString(SaveOptions.DisableFormatting);
        var signingRequest = new ZatcaXadesSigningRequest(unsignedXml, certificate, key,
            new[] { certificate }, signingTime);

        var result = new ZatcaStampedInvoiceService().Stamp(new ZatcaStampedInvoiceRequest(
            signingRequest, profile, "Aqua Farm", "310123456700003",
            new DateTimeOffset(2026, 7, 16, 12, 30, 0, TimeSpan.FromHours(3)), 115m, 15m));

        Assert.True(new ZatcaXadesSignatureService().Verify(result.SignedXml));
        Assert.Equal(result.InvoiceHash, new ZatcaInvoiceHashService().Compute(result.SignedXml).Digest);
        var fields = Decode(Convert.FromBase64String(result.QrCodeBase64));
        Assert.Equal(9, fields.Count);
        Assert.Equal(result.InvoiceHash, fields[6]);
        Assert.Equal(result.SignatureValue, fields[7]);
        Assert.Equal(result.EcdsaSubjectPublicKeyInfo, fields[8]);
        Assert.Equal(key.ExportSubjectPublicKeyInfo(), fields[8]);
        Assert.Equal(result.TechnicalCaSignature, fields[9]);

        var document = new XmlDocument();
        document.LoadXml(result.SignedXml);
        var qrNode = document.GetElementsByTagName("AdditionalDocumentReference",
                "urn:oasis:names:specification:ubl:schema:xsd:CommonAggregateComponents-2")
            .OfType<XmlElement>().Single(value => value.InnerText.Contains(result.QrCodeBase64));
        Assert.Contains(result.QrCodeBase64, qrNode.InnerText);
    }

    [Fact]
    public void Stamp_RejectsStandardBecauseZatcaMustClearAndStampIt()
    {
        using var key = ECDsa.Create(ECCurve.NamedCurves.nistP256);
        using var certificate = Certificate(key);
        var signingRequest = new ZatcaXadesSigningRequest(
            new ZatcaUblGenerator().Generate(Request(ZatcaInvoiceProfile.Standard)).ToString(SaveOptions.DisableFormatting),
            certificate, key, new[] { certificate }, DateTimeOffset.UtcNow);
        var request = new ZatcaStampedInvoiceRequest(signingRequest, ZatcaInvoiceProfile.Standard,
            "Aqua Farm", "310123456700003", DateTimeOffset.UtcNow, 115m, 15m);

        var exception = Assert.Throws<InvalidOperationException>(() =>
            new ZatcaStampedInvoiceService().Stamp(request));
        Assert.Contains("clearance", exception.Message, StringComparison.OrdinalIgnoreCase);
        Assert.Throws<InvalidOperationException>(() => new ZatcaXadesSignatureService().Sign(signingRequest));
    }

    [Fact]
    public void CertificateExtractor_ReturnsDerSubjectPublicKeyInfoAndOuterSignature()
    {
        using var key = ECDsa.Create(ECCurve.NamedCurves.nistP256);
        using var certificate = Certificate(key);
        var material = new ZatcaCertificateQrMaterialExtractor().Extract(certificate);

        Assert.Equal(key.ExportSubjectPublicKeyInfo(), material.EcdsaSubjectPublicKeyInfo);
        var reader = new AsnReader(material.EcdsaSubjectPublicKeyInfo, AsnEncodingRules.DER);
        _ = reader.ReadSequence();
        Assert.False(reader.HasData);
        Assert.InRange(material.TechnicalCaSignature.Length, 64, 80);
        Assert.Equal(0x30, material.TechnicalCaSignature[0]);
    }

    [Fact]
    public void AdvancedQr_RejectsPayloadBeyondOfficialCharacterLimit()
    {
        var request = new ZatcaAdvancedQrRequest(
            ZatcaInvoiceProfile.Standard,
            new string('A', 255),
            new string('3', 255),
            DateTimeOffset.UtcNow,
            1m,
            0m,
            new ZatcaQrCryptographicFields(new byte[32], new byte[255], new byte[255], null));
        Assert.Throws<InvalidOperationException>(() => new ZatcaAdvancedQrCodeService().GenerateBase64(request));
    }

    private static SortedDictionary<byte, byte[]> Decode(byte[] tlv)
    {
        var fields = new SortedDictionary<byte, byte[]>();
        for (var offset = 0; offset < tlv.Length;)
        {
            var tag = tlv[offset++];
            var length = tlv[offset++];
            fields.Add(tag, tlv.AsSpan(offset, length).ToArray());
            offset += length;
        }
        return fields;
    }

    private static X509Certificate2 Certificate(ECDsa key, DateTimeOffset? validAt = null)
    {
        var referenceTime = validAt ?? DateTimeOffset.UtcNow;
        var request = new CertificateRequest("CN=Test EGS,O=Fish Farm,C=SA", key, HashAlgorithmName.SHA256);
        request.CertificateExtensions.Add(new X509KeyUsageExtension(X509KeyUsageFlags.DigitalSignature, true));
        return request.CreateSelfSigned(referenceTime.AddDays(-1), referenceTime.AddDays(30));
    }

    private static ZatcaUblDocumentRequest Request(ZatcaInvoiceProfile profile) => new(
        "INV-STAMP-1", Guid.Parse("58e68b13-6a09-417c-af10-c0ea4adbe96b"),
        new DateTimeOffset(2026, 7, 16, 12, 30, 0, TimeSpan.FromHours(3)),
        profile, ZatcaDocumentKind.TaxInvoice, 1, Convert.ToBase64String(new byte[32]),
        new ZatcaParty("Aqua Farm", "310123456700003", "King Road", "1234", "Riyadh", "12345"),
        new ZatcaParty("Customer", profile == ZatcaInvoiceProfile.Standard ? "310987654300003" : "",
            "Market Road", "5678", "Riyadh", "12345"),
        new[] { new ZatcaDocumentLine("1", "Fresh fish", 2m, "KGM", 50m, 0m, "S", 15m) });
}
