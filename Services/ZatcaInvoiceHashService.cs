using System.Security.Cryptography;
using System.Security.Cryptography.Xml;
using System.Xml;

namespace FishFarmManager.Services;

public sealed record ZatcaInvoiceHashResult(
    byte[] CanonicalXml,
    byte[] Digest,
    string Hex,
    string Base64);

public sealed class ZatcaInvoiceHashService
{
    public const string CanonicalizationAlgorithm = "http://www.w3.org/2006/12/xml-c14n11";
    public const string DigestAlgorithm = "http://www.w3.org/2001/04/xmlenc#sha256";

    public ZatcaInvoiceHashResult Compute(string xml)
    {
        if (string.IsNullOrWhiteSpace(xml)) throw new ArgumentException("Invoice XML is required.", nameof(xml));
        var document = new XmlDocument { PreserveWhitespace = true };
        try { document.LoadXml(xml); }
        catch (XmlException exception) { throw new InvalidOperationException("Invoice XML is not well formed.", exception); }
        if (document.DocumentElement?.LocalName is not ("Invoice" or "CreditNote"))
            throw new InvalidOperationException("Only UBL Invoice or CreditNote documents can be hashed.");

        Remove(document, "//*[local-name()='UBLExtensions']");
        Remove(document, "//*[local-name()='AdditionalDocumentReference'][*[local-name()='ID' and normalize-space(text())='QR']]");
        Remove(document, "//*[local-name()='Signature']");
        EnsureC14N11EquivalentSubset(document);
        // .NET exposes Canonical XML 1.0 but not 1.1. For the generated UBL subset,
        // which forbids xml:* attributes and namespace undeclarations, both versions
        // produce identical octets. Inputs outside that proven subset are rejected.
        var transform = new XmlDsigC14NTransform(false);
        transform.LoadInput(document);
        using var canonicalStream = (Stream)transform.GetOutput(typeof(Stream));
        using var memory = new MemoryStream();
        canonicalStream.CopyTo(memory);
        var canonical = memory.ToArray();
        var digest = SHA256.HashData(canonical);
        return new ZatcaInvoiceHashResult(canonical, digest,
            Convert.ToHexString(digest).ToLowerInvariant(), Convert.ToBase64String(digest));
    }

    private static void Remove(XmlDocument document, string xpath)
    {
        var nodes = document.SelectNodes(xpath)?.Cast<XmlNode>().ToArray() ?? Array.Empty<XmlNode>();
        foreach (var node in nodes) node.ParentNode?.RemoveChild(node);
    }

    private static void EnsureC14N11EquivalentSubset(XmlDocument document)
    {
        foreach (XmlElement element in document.SelectNodes("//*")!.Cast<XmlElement>())
        {
            foreach (XmlAttribute attribute in element.Attributes)
            {
                if (attribute.NamespaceURI == "http://www.w3.org/XML/1998/namespace")
                    throw new InvalidOperationException("xml:* attributes require a native C14N 1.1 engine and are blocked.");
                if (attribute.Prefix == "xmlns" && attribute.Value.Length == 0)
                    throw new InvalidOperationException("Namespace undeclarations require a native C14N 1.1 engine and are blocked.");
            }
        }
    }
}
