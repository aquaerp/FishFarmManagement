using System.Globalization;
using System.Xml.Linq;
using FishFarmManager.Models;

namespace FishFarmManager.Services;

public sealed class ZatcaUblGenerator
{
    public const string CustomizationId = "urn:cen.eu:en16931:2017#compliant#urn:zatca:einvoicing:1.0.0";
    public const string ProfileId = "reporting:1.0";
    public static readonly XNamespace Cac = "urn:oasis:names:specification:ubl:schema:xsd:CommonAggregateComponents-2";
    public static readonly XNamespace Cbc = "urn:oasis:names:specification:ubl:schema:xsd:CommonBasicComponents-2";
    public static readonly XNamespace InvoiceNs = "urn:oasis:names:specification:ubl:schema:xsd:Invoice-2";
    public static readonly XNamespace CreditNoteNs = "urn:oasis:names:specification:ubl:schema:xsd:CreditNote-2";

    public XDocument Generate(ZatcaUblDocumentRequest request)
    {
        Validate(request);
        var isCredit = request.Kind == ZatcaDocumentKind.CreditNote;
        var rootNs = isCredit ? CreditNoteNs : InvoiceNs;
        var lineExtension = request.Lines.Sum(LineNet);
        var allowanceTotal = request.Lines.Sum(value => value.DiscountAmount);
        var taxTotal = request.Lines.Sum(LineTax);
        var taxInclusive = lineExtension + taxTotal;
        var root = new XElement(rootNs + (isCredit ? "CreditNote" : "Invoice"),
            new XAttribute(XNamespace.Xmlns + "cac", Cac),
            new XAttribute(XNamespace.Xmlns + "cbc", Cbc),
            Element("CustomizationID", CustomizationId),
            Element("ProfileID", ProfileId),
            Element("ID", request.DocumentNumber.Trim()),
            Element("UUID", request.Uuid.ToString()),
            Element("IssueDate", request.IssueDateTime.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture)),
            Element("IssueTime", request.IssueDateTime.ToString("HH:mm:sszzz", CultureInfo.InvariantCulture)),
            new XElement(Cbc + (isCredit ? "CreditNoteTypeCode" : "InvoiceTypeCode"),
                new XAttribute("name", request.Profile == ZatcaInvoiceProfile.Standard ? "0100000" : "0200000"),
                TypeCode(request.Kind)),
            Element("DocumentCurrencyCode", request.CurrencyCode),
            Element("TaxCurrencyCode", "SAR"),
            AdditionalReference("ICV", request.InvoiceCounterValue.ToString(CultureInfo.InvariantCulture), null),
            AdditionalReference("PIH", null, request.PreviousInvoiceHashBase64));

        if (request.Kind != ZatcaDocumentKind.TaxInvoice)
        {
            root.Add(new XElement(Cac + "BillingReference",
                new XElement(Cac + "InvoiceDocumentReference",
                    Element("ID", request.OriginalInvoiceNumber!),
                    Element("DocumentDescription", request.NoteReason!))));
        }

        root.Add(Party("AccountingSupplierParty", request.Seller));
        root.Add(Party("AccountingCustomerParty", request.Buyer));
        root.Add(TaxTotal(request.CurrencyCode, request.Lines, taxTotal));
        root.Add(new XElement(Cac + "LegalMonetaryTotal",
            Money("LineExtensionAmount", request.CurrencyCode, lineExtension),
            Money("TaxExclusiveAmount", request.CurrencyCode, lineExtension),
            Money("TaxInclusiveAmount", request.CurrencyCode, taxInclusive),
            Money("AllowanceTotalAmount", request.CurrencyCode, allowanceTotal),
            Money("PayableAmount", request.CurrencyCode, taxInclusive)));
        foreach (var line in request.Lines)
            root.Add(DocumentLine(request.CurrencyCode, line, isCredit));

        return new XDocument(new XDeclaration("1.0", "UTF-8", null), root);
    }

    private static XElement AdditionalReference(string id, string? uuid, string? embedded)
    {
        var element = new XElement(Cac + "AdditionalDocumentReference", Element("ID", id));
        if (uuid != null) element.Add(Element("UUID", uuid));
        if (embedded != null)
            element.Add(new XElement(Cac + "Attachment",
                new XElement(Cbc + "EmbeddedDocumentBinaryObject",
                    new XAttribute("mimeCode", "text/plain"), embedded)));
        return element;
    }

    private static XElement Party(string elementName, ZatcaParty party) =>
        new(Cac + elementName,
            new XElement(Cac + "Party",
                new XElement(Cac + "PostalAddress",
                    Element("StreetName", party.Street),
                    Element("BuildingNumber", party.BuildingNumber),
                    Element("CityName", party.City),
                    Element("PostalZone", party.PostalZone),
                    new XElement(Cac + "Country", Element("IdentificationCode", party.CountryCode))),
                string.IsNullOrWhiteSpace(party.VatNumber) ? null : new XElement(Cac + "PartyTaxScheme",
                    Element("CompanyID", party.VatNumber),
                    new XElement(Cac + "TaxScheme", Element("ID", "VAT"))),
                new XElement(Cac + "PartyLegalEntity", Element("RegistrationName", party.RegistrationName))));

    private static XElement TaxTotal(string currency, IReadOnlyList<ZatcaDocumentLine> lines, decimal total) =>
        new(Cac + "TaxTotal",
            Money("TaxAmount", currency, total),
            lines.GroupBy(value => new { value.TaxCategoryCode, value.TaxPercent })
                .Select(group => new XElement(Cac + "TaxSubtotal",
                    Money("TaxableAmount", currency, group.Sum(LineNet)),
                    Money("TaxAmount", currency, group.Sum(LineTax)),
                    new XElement(Cac + "TaxCategory",
                        Element("ID", group.Key.TaxCategoryCode),
                        Element("Percent", Number(group.Key.TaxPercent)),
                        new XElement(Cac + "TaxScheme", Element("ID", "VAT"))))));

    private static XElement DocumentLine(string currency, ZatcaDocumentLine line, bool credit)
    {
        var net = LineNet(line);
        var quantityName = credit ? "CreditedQuantity" : "InvoicedQuantity";
        return new XElement(Cac + (credit ? "CreditNoteLine" : "InvoiceLine"),
            Element("ID", line.Id),
            new XElement(Cbc + quantityName,
                new XAttribute("unitCode", line.UnitCode), Number(line.Quantity, 3)),
            Money("LineExtensionAmount", currency, net),
            line.DiscountAmount > 0m
                ? new XElement(Cac + "AllowanceCharge",
                    Element("ChargeIndicator", "false"),
                    Element("AllowanceChargeReason", "discount"),
                    Money("Amount", currency, line.DiscountAmount))
                : null,
            new XElement(Cac + "TaxTotal", Money("TaxAmount", currency, LineTax(line))),
            new XElement(Cac + "Item",
                Element("Name", line.Name),
                new XElement(Cac + "ClassifiedTaxCategory",
                    Element("ID", line.TaxCategoryCode),
                    Element("Percent", Number(line.TaxPercent)),
                    new XElement(Cac + "TaxScheme", Element("ID", "VAT")))),
            new XElement(Cac + "Price", Money("PriceAmount", currency, line.UnitPrice)));
    }

    private static void Validate(ZatcaUblDocumentRequest request)
    {
        Require(request.DocumentNumber, "Document number");
        if (request.Uuid == Guid.Empty) throw new InvalidOperationException("A non-empty UUID is required.");
        if (request.InvoiceCounterValue <= 0) throw new InvalidOperationException("ICV must be positive.");
        if (!string.Equals(request.CurrencyCode, "SAR", StringComparison.Ordinal))
            throw new InvalidOperationException("The initial G4 scope supports SAR documents only.");
        try
        {
            if (Convert.FromBase64String(request.PreviousInvoiceHashBase64).Length != 32)
                throw new InvalidOperationException("PIH must be a Base64 SHA-256 value.");
        }
        catch (FormatException) { throw new InvalidOperationException("PIH must be a valid Base64 hash."); }
        ValidateParty(request.Seller, true, request.Profile);
        ValidateParty(request.Buyer, request.Profile == ZatcaInvoiceProfile.Standard, request.Profile);
        if (request.Lines.Count == 0) throw new InvalidOperationException("At least one document line is required.");
        if (request.Kind != ZatcaDocumentKind.TaxInvoice
            && (string.IsNullOrWhiteSpace(request.OriginalInvoiceNumber) || string.IsNullOrWhiteSpace(request.NoteReason)))
            throw new InvalidOperationException("Credit and debit notes require the original invoice and a reason.");
        foreach (var line in request.Lines)
        {
            Require(line.Id, "Line ID");
            Require(line.Name, "Line name");
            if (line.Quantity <= 0m || line.UnitPrice < 0m || line.DiscountAmount < 0m)
                throw new InvalidOperationException("Line quantity and amounts are invalid.");
            var gross = decimal.Round(line.Quantity * line.UnitPrice, 2, MidpointRounding.AwayFromZero);
            if (line.DiscountAmount > gross) throw new InvalidOperationException("A line discount cannot exceed its gross amount.");
            if (line.TaxPercent is < 0m or > 100m) throw new InvalidOperationException("Tax percent is invalid.");
            if (line.TaxCategoryCode != "S" || line.TaxPercent != 15m)
                throw new InvalidOperationException("This first UBL increment supports only Saudi standard-rated category S at 15%; zero, exempt and export rules remain gated.");
            Require(line.UnitCode, "Unit code");
        }
    }

    private static void ValidateParty(ZatcaParty party, bool requireVat, ZatcaInvoiceProfile profile)
    {
        Require(party.RegistrationName, "Party registration name");
        if (requireVat && !ValidVatNumber(party.VatNumber))
            throw new InvalidOperationException($"A valid 15-digit VAT number is required for the {profile} party.");
        if (!string.IsNullOrWhiteSpace(party.VatNumber) && !ValidVatNumber(party.VatNumber))
            throw new InvalidOperationException("VAT numbers must contain 15 digits and start and end with 3.");
        Require(party.Street, "Street");
        Require(party.BuildingNumber, "Building number");
        Require(party.City, "City");
        Require(party.PostalZone, "Postal zone");
        if (party.CountryCode.Length != 2) throw new InvalidOperationException("Country code must be ISO 3166-1 alpha-2.");
    }

    private static bool ValidVatNumber(string value) => value.Length == 15 && value.All(char.IsDigit)
        && value[0] == '3' && value[^1] == '3';
    private static decimal LineNet(ZatcaDocumentLine line) => decimal.Round(
        line.Quantity * line.UnitPrice - line.DiscountAmount, 2, MidpointRounding.AwayFromZero);
    private static decimal LineTax(ZatcaDocumentLine line) => decimal.Round(
        LineNet(line) * line.TaxPercent / 100m, 2, MidpointRounding.AwayFromZero);
    private static string TypeCode(ZatcaDocumentKind kind) => kind switch
    {
        ZatcaDocumentKind.TaxInvoice => "388",
        ZatcaDocumentKind.CreditNote => "381",
        ZatcaDocumentKind.DebitNote => "383",
        _ => throw new ArgumentOutOfRangeException(nameof(kind))
    };
    private static XElement Element(string name, string value) => new(Cbc + name, value);
    private static XElement Money(string name, string currency, decimal value) =>
        new(Cbc + name, new XAttribute("currencyID", currency), Number(value));
    private static string Number(decimal value, int decimals = 2) =>
        value.ToString(decimals == 2 ? "0.00" : "0.000", CultureInfo.InvariantCulture);
    private static void Require(string? value, string name)
    {
        if (string.IsNullOrWhiteSpace(value)) throw new InvalidOperationException($"{name} is required.");
    }
}
