using System.Numerics;
using System.Security.Cryptography;
using System.Security.Cryptography.X509Certificates;
using System.Security.Cryptography.Xml;
using System.Xml;

namespace FishFarmManager.Services;

public sealed record ZatcaXadesSigningRequest(
    string UnsignedXml,
    X509Certificate2 SigningCertificate,
    ECDsa PrivateKey,
    IReadOnlyList<X509Certificate2> CertificateChain,
    DateTimeOffset SigningTime);

public sealed record ZatcaXadesSignatureResult(
    string SignedXml,
    byte[] InvoiceHash,
    string InvoiceHashBase64,
    byte[] SignatureValue,
    string SignatureValueBase64);

public sealed class ZatcaXadesSignatureService
{
    public const string C14N11 = "http://www.w3.org/2006/12/xml-c14n11";
    public const string EcdsaSha256 = "http://www.w3.org/2001/04/xmldsig-more#ecdsa-sha256";
    public const string XadesNamespace = "http://uri.etsi.org/01903/v1.3.2#";
    public const string SignedPropertiesType = "http://uri.etsi.org/01903#SignedProperties";
    private const string ExtNamespace = "urn:oasis:names:specification:ubl:schema:xsd:CommonExtensionComponents-2";
    private const string CacNamespace = "urn:oasis:names:specification:ubl:schema:xsd:CommonAggregateComponents-2";
    private const string CbcNamespace = "urn:oasis:names:specification:ubl:schema:xsd:CommonBasicComponents-2";
    private const string SigNamespace = "urn:oasis:names:specification:ubl:schema:xsd:CommonSignatureComponents-2";
    private const string SacNamespace = "urn:oasis:names:specification:ubl:schema:xsd:SignatureAggregateComponents-2";
    private const string SbcNamespace = "urn:oasis:names:specification:ubl:schema:xsd:SignatureBasicComponents-2";
    private static readonly object AlgorithmRegistrationLock = new();
    private static bool _algorithmsRegistered;

    public ZatcaXadesSignatureResult Sign(ZatcaXadesSigningRequest request)
    {
        ArgumentNullException.ThrowIfNull(request);
        if (string.IsNullOrWhiteSpace(request.UnsignedXml))
            throw new ArgumentException("Unsigned UBL XML is required.", nameof(request));
        if (request.CertificateChain is null || request.CertificateChain.Count == 0)
            throw new InvalidOperationException("The signing certificate chain is required.");
        if (!request.CertificateChain[0].RawData.SequenceEqual(request.SigningCertificate.RawData))
            throw new InvalidOperationException("The signing certificate must be the first certificate in the chain.");
        _ = new ZatcaSigningMaterialValidator().Validate(
            request.SigningCertificate, request.PrivateKey, request.SigningTime);
        RegisterAlgorithms();

        var document = LoadUbl(request.UnsignedXml);
        if (document.SelectSingleNode("//*[local-name()='UBLExtensions' or local-name()='Signature']") is not null)
            throw new InvalidOperationException("Only an unsigned UBL document without signature containers can be signed.");
        var typeCode = document.SelectSingleNode("//*[local-name()='InvoiceTypeCode' or local-name()='CreditNoteTypeCode']")
            as XmlElement ?? throw new InvalidOperationException("The ZATCA invoice type code is required.");
        if (typeCode.GetAttribute("name") != "0200000")
            throw new InvalidOperationException(
                "Standard documents are cleared and stamped by ZATCA; the EGS may locally stamp only simplified documents and their notes.");
        var hash = new ZatcaInvoiceHashService().Compute(request.UnsignedXml);
        var signedXml = new ZatcaSignedXml(document)
        {
            SigningKey = request.PrivateKey
        };
        signedXml.Signature.Id = "signature";
        var signedInfo = signedXml.SignedInfo
            ?? throw new CryptographicException("XML SignedInfo could not be initialized.");
        signedInfo.CanonicalizationMethod = C14N11;
        signedInfo.SignatureMethod = EcdsaSha256;

        var invoiceReference = new Reference(string.Empty)
        {
            Id = "invoiceSignedData",
            DigestMethod = ZatcaInvoiceHashService.DigestAlgorithm
        };
        invoiceReference.AddTransform(XPath("not(//ancestor-or-self::ext:UBLExtensions)"));
        invoiceReference.AddTransform(XPath("not(//ancestor-or-self::cac:Signature)"));
        invoiceReference.AddTransform(XPath("not(//ancestor-or-self::cac:AdditionalDocumentReference[cbc:ID='QR'])"));
        invoiceReference.AddTransform(new ZatcaC14N11EquivalentTransform());
        signedXml.AddReference(invoiceReference);

        var signedProperties = BuildQualifyingProperties(request.CertificateChain, request.SigningTime);
        var dataObject = new System.Security.Cryptography.Xml.DataObject
            { Data = signedProperties.DocumentElement!.SelectNodes(".")! };
        signedXml.AddObject(dataObject);
        var propertiesReference = new Reference("#xadesSignedProperties")
        {
            Type = SignedPropertiesType,
            DigestMethod = ZatcaInvoiceHashService.DigestAlgorithm
        };
        propertiesReference.AddTransform(new ZatcaC14N11EquivalentTransform());
        signedXml.AddReference(propertiesReference);

        var keyInfo = new KeyInfo();
        var x509Data = new KeyInfoX509Data();
        foreach (var certificate in request.CertificateChain) x509Data.AddCertificate(certificate);
        keyInfo.AddClause(x509Data);
        signedXml.KeyInfo = keyInfo;
        signedXml.ComputeSignature();

        var signatureElement = signedXml.GetXml();
        AttachUblSignature(document, signatureElement);
        // Inclusive canonicalization carries ancestor namespaces into XAdES nodes. Refresh the
        // SignedProperties digest and re-sign SignedInfo after embedding so both cover the exact
        // UBL namespace context.
        var embeddedProperties = document.GetElementsByTagName("SignedProperties", XadesNamespace)
            .OfType<XmlElement>().Single();
        var propertiesDigest = SHA256.HashData(CanonicalizeSubtree(embeddedProperties));
        var propertiesReferenceElement = document.GetElementsByTagName("Reference", SignedXml.XmlDsigNamespaceUrl)
            .OfType<XmlElement>().Single(value => value.GetAttribute("URI") == "#xadesSignedProperties");
        propertiesReferenceElement.GetElementsByTagName("DigestValue", SignedXml.XmlDsigNamespaceUrl)[0]!.InnerText =
            Convert.ToBase64String(propertiesDigest);
        var embeddedSignedInfo = document.GetElementsByTagName("SignedInfo", SignedXml.XmlDsigNamespaceUrl)
            .OfType<XmlElement>().Single();
        var canonicalSignedInfo = CanonicalizeSubtree(embeddedSignedInfo);
        var signatureValue = request.PrivateKey.SignData(canonicalSignedInfo, HashAlgorithmName.SHA256,
            DSASignatureFormat.IeeeP1363FixedFieldConcatenation);
        document.GetElementsByTagName("SignatureValue", SignedXml.XmlDsigNamespaceUrl)[0]!.InnerText =
            Convert.ToBase64String(signatureValue);
        var output = Serialize(document);
        if (!Verify(output)) throw new CryptographicException("Local XAdES verification failed after UBL embedding.");
        return new ZatcaXadesSignatureResult(output, hash.Digest, hash.Base64,
            signatureValue, Convert.ToBase64String(signatureValue));
    }

    public bool Verify(string signedUblXml)
    {
        RegisterAlgorithms();
        try
        {
            var document = LoadUbl(signedUblXml);
            var signatureElement = document.GetElementsByTagName("Signature", SignedXml.XmlDsigNamespaceUrl)
                .OfType<XmlElement>().Single();
            var signedXml = new ZatcaSignedXml(document);
            signedXml.LoadXml(signatureElement);
            var encodedCertificate = signatureElement
                .GetElementsByTagName("X509Certificate", SignedXml.XmlDsigNamespaceUrl)
                .OfType<XmlElement>().First().InnerText;
            using var certificate = new X509Certificate2(Convert.FromBase64String(encodedCertificate));
            using var publicKey = certificate.GetECDsaPublicKey();
            if (publicKey is null) return false;

            var signedInfo = signatureElement.GetElementsByTagName("SignedInfo", SignedXml.XmlDsigNamespaceUrl)
                .OfType<XmlElement>().Single();
            var signatureValue = Convert.FromBase64String(signatureElement
                .GetElementsByTagName("SignatureValue", SignedXml.XmlDsigNamespaceUrl)[0]!.InnerText);
            if (!publicKey.VerifyData(CanonicalizeSubtree(signedInfo), signatureValue,
                    HashAlgorithmName.SHA256, DSASignatureFormat.IeeeP1363FixedFieldConcatenation))
                return false;

            var references = signedInfo.GetElementsByTagName("Reference", SignedXml.XmlDsigNamespaceUrl)
                .OfType<XmlElement>().ToArray();
            var invoiceReference = references.Single(value => value.GetAttribute("URI").Length == 0);
            var expectedInvoiceDigest = Convert.FromBase64String(invoiceReference
                .GetElementsByTagName("DigestValue", SignedXml.XmlDsigNamespaceUrl)[0]!.InnerText);
            if (!CryptographicOperations.FixedTimeEquals(
                    new ZatcaInvoiceHashService().Compute(signedUblXml).Digest, expectedInvoiceDigest))
                return false;

            var propertiesReference = references.Single(value =>
                value.GetAttribute("URI") == "#xadesSignedProperties"
                && value.GetAttribute("Type") == SignedPropertiesType);
            var expectedPropertiesDigest = Convert.FromBase64String(propertiesReference
                .GetElementsByTagName("DigestValue", SignedXml.XmlDsigNamespaceUrl)[0]!.InnerText);
            var properties = signatureElement.GetElementsByTagName("SignedProperties", XadesNamespace)
                .OfType<XmlElement>().Single();
            if (!CryptographicOperations.FixedTimeEquals(
                    SHA256.HashData(CanonicalizeSubtree(properties)), expectedPropertiesDigest))
                return false;

            var boundCertificateDigest = properties.GetElementsByTagName("CertDigest", XadesNamespace)
                .OfType<XmlElement>().First()
                .GetElementsByTagName("DigestValue", SignedXml.XmlDsigNamespaceUrl)[0]!.InnerText;
            return CryptographicOperations.FixedTimeEquals(
                SHA256.HashData(certificate.RawData), Convert.FromBase64String(boundCertificateDigest));
        }
        catch (Exception exception) when (exception is CryptographicException or XmlException
                                          or InvalidOperationException or FormatException or ArgumentException)
        {
            return false;
        }
    }

    private static XmlDocument BuildQualifyingProperties(
        IReadOnlyList<X509Certificate2> chain,
        DateTimeOffset signingTime)
    {
        var document = new XmlDocument { PreserveWhitespace = true };
        var qualifying = document.CreateElement("xades", "QualifyingProperties", XadesNamespace);
        qualifying.SetAttribute("Target", "signature");
        document.AppendChild(qualifying);
        var signedProperties = Add(qualifying, "xades", "SignedProperties", XadesNamespace);
        signedProperties.SetAttribute("Id", "xadesSignedProperties");
        var signatureProperties = Add(signedProperties, "xades", "SignedSignatureProperties", XadesNamespace);
        Add(signatureProperties, "xades", "SigningTime", XadesNamespace).InnerText =
            signingTime.ToUniversalTime().ToString("yyyy-MM-dd'T'HH:mm:ss'Z'");
        var signingCertificate = Add(signatureProperties, "xades", "SigningCertificate", XadesNamespace);
        foreach (var certificate in chain)
        {
            var cert = Add(signingCertificate, "xades", "Cert", XadesNamespace);
            var certDigest = Add(cert, "xades", "CertDigest", XadesNamespace);
            var digestMethod = Add(certDigest, "ds", "DigestMethod", SignedXml.XmlDsigNamespaceUrl);
            digestMethod.SetAttribute("Algorithm", ZatcaInvoiceHashService.DigestAlgorithm);
            Add(certDigest, "ds", "DigestValue", SignedXml.XmlDsigNamespaceUrl).InnerText =
                Convert.ToBase64String(SHA256.HashData(certificate.RawData));
            var issuerSerial = Add(cert, "xades", "IssuerSerial", XadesNamespace);
            Add(issuerSerial, "ds", "X509IssuerName", SignedXml.XmlDsigNamespaceUrl).InnerText = certificate.Issuer;
            Add(issuerSerial, "ds", "X509SerialNumber", SignedXml.XmlDsigNamespaceUrl).InnerText =
                new BigInteger(certificate.GetSerialNumber(), isUnsigned: true, isBigEndian: false).ToString();
        }
        var policy = Add(signatureProperties, "xades", "SignaturePolicyIdentifier", XadesNamespace);
        Add(policy, "xades", "SignaturePolicyImplied", XadesNamespace);
        var dataProperties = Add(signedProperties, "xades", "SignedDataObjectProperties", XadesNamespace);
        var format = Add(dataProperties, "xades", "DataObjectFormat", XadesNamespace);
        format.SetAttribute("ObjectReference", "#invoiceSignedData");
        Add(format, "xades", "MimeType", XadesNamespace).InnerText = "text/xml";
        return document;
    }

    private static XmlDsigXPathTransform XPath(string expression)
    {
        var document = new XmlDocument();
        document.LoadXml($"<ds:XPath xmlns:ds='{SignedXml.XmlDsigNamespaceUrl}' xmlns:ext='{ExtNamespace}' xmlns:cac='{CacNamespace}' xmlns:cbc='{CbcNamespace}'>{expression}</ds:XPath>");
        var transform = new XmlDsigXPathTransform();
        transform.LoadInnerXml(document.ChildNodes);
        return transform;
    }

    private static void AttachUblSignature(XmlDocument document, XmlElement signature)
    {
        var root = document.DocumentElement!;
        var extensions = document.CreateElement("ext", "UBLExtensions", ExtNamespace);
        var extension = Add(extensions, "ext", "UBLExtension", ExtNamespace);
        Add(extension, "ext", "ExtensionURI", ExtNamespace).InnerText =
            "urn:oasis:names:specification:ubl:dsig:enveloped:xades";
        var content = Add(extension, "ext", "ExtensionContent", ExtNamespace);
        var signatures = Add(content, "sig", "UBLDocumentSignatures", SigNamespace);
        var information = Add(signatures, "sac", "SignatureInformation", SacNamespace);
        Add(information, "cbc", "ID", CbcNamespace).InnerText =
            "urn:oasis:names:specification:ubl:signature:1";
        Add(information, "sbc", "ReferencedSignatureID", SbcNamespace).InnerText =
            "urn:oasis:names:specification:ubl:signature:Invoice";
        information.AppendChild(document.ImportNode(signature, true));
        root.InsertBefore(extensions, root.FirstChild);

        var ublSignature = document.CreateElement("cac", "Signature", CacNamespace);
        Add(ublSignature, "cbc", "ID", CbcNamespace).InnerText =
            "urn:oasis:names:specification:ubl:signature:Invoice";
        Add(ublSignature, "cbc", "SignatureMethod", CbcNamespace).InnerText =
            "urn:oasis:names:specification:ubl:dsig:enveloped:xades";
        var supplier = root.ChildNodes.OfType<XmlElement>()
            .FirstOrDefault(value => value.LocalName == "AccountingSupplierParty");
        root.InsertBefore(ublSignature, supplier);
    }

    private static XmlElement Add(XmlElement parent, string prefix, string localName, string ns)
    {
        var element = parent.OwnerDocument!.CreateElement(prefix, localName, ns);
        parent.AppendChild(element);
        return element;
    }

    private static XmlDocument LoadUbl(string xml)
    {
        var document = new XmlDocument { PreserveWhitespace = true };
        document.LoadXml(xml);
        if (document.DocumentElement?.LocalName is not ("Invoice" or "CreditNote"))
            throw new InvalidOperationException("Only UBL Invoice or CreditNote documents can be signed.");
        return document;
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

    private static byte[] CanonicalizeSubtree(XmlElement element)
    {
        var nodes = element.SelectNodes(". | .//node() | .//@*")
            ?? throw new CryptographicException("The SignedInfo node set could not be selected.");
        var transform = new ZatcaC14N11EquivalentTransform();
        transform.LoadInput(nodes);
        using var stream = (Stream)transform.GetOutput(typeof(Stream));
        using var output = new MemoryStream();
        stream.CopyTo(output);
        return output.ToArray();
    }

    private static void RegisterAlgorithms()
    {
        if (_algorithmsRegistered) return;
        lock (AlgorithmRegistrationLock)
        {
            if (_algorithmsRegistered) return;
            CryptoConfig.AddAlgorithm(typeof(ZatcaC14N11EquivalentTransform), C14N11);
            CryptoConfig.AddAlgorithm(typeof(ZatcaEcdsaSha256SignatureDescription), EcdsaSha256);
            _algorithmsRegistered = true;
        }
    }
}

public sealed class ZatcaSignedXml : SignedXml
{
    public ZatcaSignedXml(XmlDocument document) : base(document) { }

    public override XmlElement? GetIdElement(XmlDocument? document, string idValue)
        => base.GetIdElement(document, idValue)
           ?? Signature.ObjectList.Cast<System.Security.Cryptography.Xml.DataObject>()
               .SelectMany(value => value.Data is null
                   ? Enumerable.Empty<XmlElement>()
                   : value.Data.OfType<XmlElement>())
               .SelectMany(value => value.SelectNodes(".//*[@Id]")!.OfType<XmlElement>().Prepend(value))
               .FirstOrDefault(value => value.GetAttribute("Id") == idValue);
}

public sealed class ZatcaC14N11EquivalentTransform : XmlDsigC14NTransform
{
    public ZatcaC14N11EquivalentTransform() => Algorithm = ZatcaXadesSignatureService.C14N11;
}

public sealed class ZatcaEcdsaSha256SignatureDescription : SignatureDescription
{
    public ZatcaEcdsaSha256SignatureDescription()
    {
        KeyAlgorithm = typeof(ECDsa).AssemblyQualifiedName;
        DigestAlgorithm = typeof(SHA256).AssemblyQualifiedName;
        FormatterAlgorithm = typeof(ZatcaEcdsaSignatureFormatter).AssemblyQualifiedName;
        DeformatterAlgorithm = typeof(ZatcaEcdsaSignatureDeformatter).AssemblyQualifiedName;
    }

    public override HashAlgorithm CreateDigest() => SHA256.Create();
}

public sealed class ZatcaEcdsaSignatureFormatter : AsymmetricSignatureFormatter
{
    private ECDsa? _key;
    public ZatcaEcdsaSignatureFormatter() { }
    public ZatcaEcdsaSignatureFormatter(AsymmetricAlgorithm key) => SetKey(key);
    public override void SetHashAlgorithm(string strName) { }
    public override void SetKey(AsymmetricAlgorithm key) =>
        _key = key as ECDsa ?? throw new CryptographicException("An ECDSA signing key is required.");
    public override byte[] CreateSignature(byte[] rgbHash) => (_key
        ?? throw new CryptographicException("The ECDSA signing key is not initialized."))
        .SignHash(rgbHash, DSASignatureFormat.IeeeP1363FixedFieldConcatenation);
}

public sealed class ZatcaEcdsaSignatureDeformatter : AsymmetricSignatureDeformatter
{
    private ECDsa? _key;
    public ZatcaEcdsaSignatureDeformatter() { }
    public ZatcaEcdsaSignatureDeformatter(AsymmetricAlgorithm key) => SetKey(key);
    public override void SetHashAlgorithm(string strName) { }
    public override void SetKey(AsymmetricAlgorithm key) =>
        _key = key as ECDsa ?? throw new CryptographicException("An ECDSA verification key is required.");
    public override bool VerifySignature(byte[] rgbHash, byte[] rgbSignature) => (_key
        ?? throw new CryptographicException("The ECDSA verification key is not initialized."))
        .VerifyHash(rgbHash, rgbSignature, DSASignatureFormat.IeeeP1363FixedFieldConcatenation);
}
