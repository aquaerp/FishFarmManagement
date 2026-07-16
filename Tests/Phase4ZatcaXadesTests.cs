using System.Security.Cryptography;
using System.Security.Cryptography.X509Certificates;
using System.Security.Cryptography.Xml;
using System.Xml;
using System.Xml.Linq;
using FishFarmManager.Models;
using FishFarmManager.Services;

namespace FishFarmManager.Tests;

public sealed class Phase4ZatcaXadesTests
{
    [Fact]
    public void Sign_ProducesLocallyVerifiableEnvelopedXadesAndRawEcdsaValue()
    {
        using var key = ECDsa.Create(ECCurve.NamedCurves.nistP256);
        using var certificate = Certificate(key);
        var unsignedXml = new ZatcaUblGenerator().Generate(Request()).ToString(SaveOptions.DisableFormatting);
        var service = new ZatcaXadesSignatureService();

        var result = service.Sign(new ZatcaXadesSigningRequest(
            unsignedXml, certificate, key, new[] { certificate },
            new DateTimeOffset(2026, 7, 16, 9, 30, 0, TimeSpan.Zero)));

        Assert.True(service.Verify(result.SignedXml));
        Assert.Equal(32, result.InvoiceHash.Length);
        Assert.Equal(64, result.SignatureValue.Length);
        var document = new XmlDocument();
        document.LoadXml(result.SignedXml);
        Assert.Single(document.GetElementsByTagName("UBLExtensions",
            "urn:oasis:names:specification:ubl:schema:xsd:CommonExtensionComponents-2").OfType<XmlElement>());
        Assert.Single(document.GetElementsByTagName("SignedProperties", ZatcaXadesSignatureService.XadesNamespace).OfType<XmlElement>());
        Assert.Equal(2, document.GetElementsByTagName("Reference", SignedXml.XmlDsigNamespaceUrl).Count);
        Assert.Equal(ZatcaXadesSignatureService.EcdsaSha256,
            document.GetElementsByTagName("SignatureMethod", SignedXml.XmlDsigNamespaceUrl)[0]!.Attributes!["Algorithm"]!.Value);
    }

    [Fact]
    public void Verify_RejectsTamperedBusinessContentButAllowsExcludedUblSignatureChange()
    {
        using var key = ECDsa.Create(ECCurve.NamedCurves.nistP256);
        using var certificate = Certificate(key);
        var service = new ZatcaXadesSignatureService();
        var signed = service.Sign(new ZatcaXadesSigningRequest(
            new ZatcaUblGenerator().Generate(Request()).ToString(SaveOptions.DisableFormatting),
            certificate, key, new[] { certificate }, DateTimeOffset.UtcNow)).SignedXml;

        var tampered = signed.Replace("INV-XADES-1", "INV-XADES-2", StringComparison.Ordinal);
        Assert.False(service.Verify(tampered));
        var excluded = signed.Replace(
            "urn:oasis:names:specification:ubl:signature:Invoice</cbc:ID>",
            "urn:oasis:names:specification:ubl:signature:Changed</cbc:ID>",
            StringComparison.Ordinal);
        Assert.True(service.Verify(excluded));
    }

    [Fact]
    public void Sign_RejectsMissingChainAndMismatchedSigningCertificate()
    {
        using var key = ECDsa.Create(ECCurve.NamedCurves.nistP256);
        using var certificate = Certificate(key);
        using var otherKey = ECDsa.Create(ECCurve.NamedCurves.nistP256);
        using var otherCertificate = Certificate(otherKey);
        var xml = new ZatcaUblGenerator().Generate(Request()).ToString(SaveOptions.DisableFormatting);
        var service = new ZatcaXadesSignatureService();
        Assert.Throws<InvalidOperationException>(() => service.Sign(new ZatcaXadesSigningRequest(
            xml, certificate, key, Array.Empty<X509Certificate2>(), DateTimeOffset.UtcNow)));
        Assert.Throws<InvalidOperationException>(() => service.Sign(new ZatcaXadesSigningRequest(
            xml, certificate, key, new[] { otherCertificate }, DateTimeOffset.UtcNow)));
    }

    private static X509Certificate2 Certificate(ECDsa key)
    {
        var request = new CertificateRequest("CN=Test EGS,O=Fish Farm,C=SA", key, HashAlgorithmName.SHA256);
        request.CertificateExtensions.Add(new X509KeyUsageExtension(X509KeyUsageFlags.DigitalSignature, true));
        return request.CreateSelfSigned(DateTimeOffset.UtcNow.AddDays(-1), DateTimeOffset.UtcNow.AddDays(30));
    }

    private static ZatcaUblDocumentRequest Request() => new(
        "INV-XADES-1", Guid.Parse("602ddbb0-2a7f-4785-a5df-3c80e629c596"),
        new DateTimeOffset(2026, 7, 16, 12, 30, 0, TimeSpan.FromHours(3)),
        ZatcaInvoiceProfile.Simplified, ZatcaDocumentKind.TaxInvoice, 1,
        Convert.ToBase64String(new byte[32]),
        new ZatcaParty("Aqua Farm", "310123456700003", "King Road", "1234", "Riyadh", "12345"),
        new ZatcaParty("Customer", "310987654300003", "Market Road", "5678", "Riyadh", "12345"),
        new[] { new ZatcaDocumentLine("1", "Fresh fish", 2m, "KGM", 50m, 0m, "S", 15m) });
}
