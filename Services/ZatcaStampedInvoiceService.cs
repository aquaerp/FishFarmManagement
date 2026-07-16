using System.Formats.Asn1;
using System.Security.Cryptography.X509Certificates;
using System.Xml;
using FishFarmManager.Models;

namespace FishFarmManager.Services;

public sealed record ZatcaCertificateQrMaterial(
    byte[] EcdsaSubjectPublicKeyInfo,
    byte[] TechnicalCaSignature);

public sealed record ZatcaStampedInvoiceRequest(
    ZatcaXadesSigningRequest SigningRequest,
    ZatcaInvoiceProfile Profile,
    string SellerName,
    string SellerVatNumber,
    DateTimeOffset IssueDateTime,
    decimal TaxInclusiveAmount,
    decimal VatAmount);

public sealed record ZatcaStampedInvoiceResult(
    string SignedXml,
    string QrCodeBase64,
    byte[] InvoiceHash,
    byte[] SignatureValue,
    byte[] EcdsaSubjectPublicKeyInfo,
    byte[]? TechnicalCaSignature);

public sealed class ZatcaCertificateQrMaterialExtractor
{
    public ZatcaCertificateQrMaterial Extract(X509Certificate2 certificate)
    {
        ArgumentNullException.ThrowIfNull(certificate);
        using var publicKey = certificate.GetECDsaPublicKey()
            ?? throw new InvalidOperationException("The CSID certificate does not contain an ECDSA public key.");
        if (publicKey.KeySize != 256)
            throw new InvalidOperationException("The CSID ECDSA public key must be 256 bits.");

        var certificateReader = new AsnReader(certificate.RawData, AsnEncodingRules.DER);
        var certificateSequence = certificateReader.ReadSequence();
        _ = certificateSequence.ReadEncodedValue(); // tbsCertificate
        _ = certificateSequence.ReadEncodedValue(); // signatureAlgorithm
        var caSignature = certificateSequence.ReadBitString(out var unusedBitCount);
        if (unusedBitCount != 0 || certificateSequence.HasData || certificateReader.HasData)
            throw new InvalidOperationException("The CSID certificate signature encoding is invalid.");
        if (caSignature.Length == 0 || caSignature.Length > byte.MaxValue)
            throw new InvalidOperationException("The CSID certificate signature cannot fit ZATCA QR tag 9.");

        var subjectPublicKeyInfo = publicKey.ExportSubjectPublicKeyInfo();
        if (subjectPublicKeyInfo.Length == 0 || subjectPublicKeyInfo.Length > byte.MaxValue)
            throw new InvalidOperationException("The CSID public key cannot fit ZATCA QR tag 8.");
        return new ZatcaCertificateQrMaterial(subjectPublicKeyInfo, caSignature);
    }
}

public sealed class ZatcaStampedInvoiceService
{
    private const string CacNamespace = "urn:oasis:names:specification:ubl:schema:xsd:CommonAggregateComponents-2";
    private const string CbcNamespace = "urn:oasis:names:specification:ubl:schema:xsd:CommonBasicComponents-2";
    private readonly ZatcaXadesSignatureService _signatureService = new();
    private readonly ZatcaAdvancedQrCodeService _qrCodeService = new();
    private readonly ZatcaCertificateQrMaterialExtractor _materialExtractor = new();

    public ZatcaStampedInvoiceResult Stamp(ZatcaStampedInvoiceRequest request)
    {
        ArgumentNullException.ThrowIfNull(request);
        if (request.Profile != ZatcaInvoiceProfile.Simplified)
            throw new InvalidOperationException(
                "Local EGS stamping is limited to simplified documents; standard documents must use ZATCA clearance and the returned stamp.");
        var signed = _signatureService.Sign(request.SigningRequest);
        var material = _materialExtractor.Extract(request.SigningRequest.SigningCertificate);
        var tag9 = material.TechnicalCaSignature;
        var qr = _qrCodeService.GenerateBase64(new ZatcaAdvancedQrRequest(
            request.Profile,
            request.SellerName,
            request.SellerVatNumber,
            request.IssueDateTime,
            request.TaxInclusiveAmount,
            request.VatAmount,
            new ZatcaQrCryptographicFields(
                signed.InvoiceHash,
                signed.SignatureValue,
                material.EcdsaSubjectPublicKeyInfo,
                tag9)));
        var document = new XmlDocument { PreserveWhitespace = true };
        document.LoadXml(signed.SignedXml);
        InsertQrReference(document, qr);
        var output = Serialize(document);
        if (!_signatureService.Verify(output))
            throw new InvalidOperationException("The XAdES signature failed verification after QR insertion.");
        var postInsertionHash = new ZatcaInvoiceHashService().Compute(output).Digest;
        if (!System.Security.Cryptography.CryptographicOperations.FixedTimeEquals(
                signed.InvoiceHash, postInsertionHash))
            throw new InvalidOperationException("The invoice hash changed after inserting the excluded QR reference.");
        return new ZatcaStampedInvoiceResult(output, qr, signed.InvoiceHash, signed.SignatureValue,
            material.EcdsaSubjectPublicKeyInfo, tag9);
    }

    private static void InsertQrReference(XmlDocument document, string qrBase64)
    {
        var root = document.DocumentElement!;
        if (root.ChildNodes.OfType<XmlElement>().Any(value => value.LocalName == "AdditionalDocumentReference"
            && value.ChildNodes.OfType<XmlElement>().Any(child => child.LocalName == "ID" && child.InnerText == "QR")))
            throw new InvalidOperationException("A QR document reference already exists.");
        var reference = document.CreateElement("cac", "AdditionalDocumentReference", CacNamespace);
        Add(reference, "cbc", "ID", CbcNamespace).InnerText = "QR";
        var attachment = Add(reference, "cac", "Attachment", CacNamespace);
        var binary = Add(attachment, "cbc", "EmbeddedDocumentBinaryObject", CbcNamespace);
        binary.SetAttribute("mimeCode", "text/plain");
        binary.InnerText = qrBase64;

        var ublSignature = root.ChildNodes.OfType<XmlElement>()
            .FirstOrDefault(value => value.LocalName == "Signature" && value.NamespaceURI == CacNamespace);
        root.InsertBefore(reference, ublSignature);
    }

    private static XmlElement Add(XmlElement parent, string prefix, string localName, string ns)
    {
        var element = parent.OwnerDocument!.CreateElement(prefix, localName, ns);
        parent.AppendChild(element);
        return element;
    }

    private static string Serialize(XmlDocument document)
    {
        using var writer = new StringWriter();
        using var xmlWriter = XmlWriter.Create(writer, new XmlWriterSettings
        {
            OmitXmlDeclaration = false,
            Indent = false,
            Encoding = System.Text.Encoding.UTF8
        });
        document.Save(xmlWriter);
        return writer.ToString();
    }
}
