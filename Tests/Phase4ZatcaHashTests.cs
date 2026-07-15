using System.Text;
using FishFarmManager.Services;

namespace FishFarmManager.Tests;

public sealed class Phase4ZatcaHashTests
{
    [Fact]
    public void Hash_UsesCanonicalOctetsAndSha256Base64()
    {
        const string xml = "<?xml version=\"1.0\"?><Invoice xmlns=\"urn:oasis:names:specification:ubl:schema:xsd:Invoice-2\" xmlns:cbc=\"urn:oasis:names:specification:ubl:schema:xsd:CommonBasicComponents-2\"><cbc:ID>INV-1</cbc:ID></Invoice>";
        var result = new ZatcaInvoiceHashService().Compute(xml);

        Assert.Equal("<Invoice xmlns=\"urn:oasis:names:specification:ubl:schema:xsd:Invoice-2\" xmlns:cbc=\"urn:oasis:names:specification:ubl:schema:xsd:CommonBasicComponents-2\"><cbc:ID>INV-1</cbc:ID></Invoice>",
            Encoding.UTF8.GetString(result.CanonicalXml));
        Assert.Equal("02b58ac9b1e09040f0345cf16ab2c439e837cbca9a733ebf83df3302e2be0a50", result.Hex);
        Assert.Equal("ArWKybHgkEDwNFzxarLEOeg3y8qacz6/g98zAuK+ClA=", result.Base64);
    }

    [Fact]
    public void Hash_ExcludesUblExtensionsQrAndSignature()
    {
        const string template = """
            <Invoice xmlns="urn:oasis:names:specification:ubl:schema:xsd:Invoice-2"
                     xmlns:ext="urn:oasis:names:specification:ubl:schema:xsd:CommonExtensionComponents-2"
                     xmlns:cac="urn:oasis:names:specification:ubl:schema:xsd:CommonAggregateComponents-2"
                     xmlns:cbc="urn:oasis:names:specification:ubl:schema:xsd:CommonBasicComponents-2">
              <ext:UBLExtensions><ext:UBLExtension><ext:ExtensionContent>{0}</ext:ExtensionContent></ext:UBLExtension></ext:UBLExtensions>
              <cbc:ID>INV-1</cbc:ID>
              <cac:AdditionalDocumentReference><cbc:ID>QR</cbc:ID><cac:Attachment>{1}</cac:Attachment></cac:AdditionalDocumentReference>
              <cac:Signature><cbc:ID>{2}</cbc:ID></cac:Signature>
            </Invoice>
            """;
        var service = new ZatcaInvoiceHashService();
        var first = service.Compute(string.Format(template, "signature-a", "qr-a", "sig-ref-a"));
        var second = service.Compute(string.Format(template, "signature-b", "qr-b", "sig-ref-b"));

        Assert.Equal(first.Base64, second.Base64);
        Assert.DoesNotContain("UBLExtensions", Encoding.UTF8.GetString(first.CanonicalXml));
        Assert.DoesNotContain("AdditionalDocumentReference", Encoding.UTF8.GetString(first.CanonicalXml));
        Assert.DoesNotContain("Signature", Encoding.UTF8.GetString(first.CanonicalXml));
    }

    [Fact]
    public void Hash_RejectsInputOutsideProvenC14n11EquivalentSubset()
    {
        const string xml = "<Invoice xmlns=\"urn:oasis:names:specification:ubl:schema:xsd:Invoice-2\" xml:lang=\"ar\"><ID>1</ID></Invoice>";
        Assert.Throws<InvalidOperationException>(() => new ZatcaInvoiceHashService().Compute(xml));
    }
}
