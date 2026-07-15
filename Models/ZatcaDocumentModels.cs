namespace FishFarmManager.Models;

public enum ZatcaInvoiceProfile { Standard, Simplified }
public enum ZatcaDocumentKind { TaxInvoice, CreditNote, DebitNote }
public enum ZatcaSubmissionRoute { Clearance, Reporting }

public sealed record ZatcaParty(
    string RegistrationName,
    string VatNumber,
    string Street,
    string BuildingNumber,
    string City,
    string PostalZone,
    string CountryCode = "SA");

public sealed record ZatcaDocumentLine(
    string Id,
    string Name,
    decimal Quantity,
    string UnitCode,
    decimal UnitPrice,
    decimal DiscountAmount,
    string TaxCategoryCode,
    decimal TaxPercent);

public sealed record ZatcaUblDocumentRequest(
    string DocumentNumber,
    Guid Uuid,
    DateTimeOffset IssueDateTime,
    ZatcaInvoiceProfile Profile,
    ZatcaDocumentKind Kind,
    long InvoiceCounterValue,
    string PreviousInvoiceHashBase64,
    ZatcaParty Seller,
    ZatcaParty Buyer,
    IReadOnlyList<ZatcaDocumentLine> Lines,
    string CurrencyCode = "SAR",
    string? OriginalInvoiceNumber = null,
    string? NoteReason = null)
{
    public ZatcaSubmissionRoute SubmissionRoute => Profile == ZatcaInvoiceProfile.Standard
        ? ZatcaSubmissionRoute.Clearance
        : ZatcaSubmissionRoute.Reporting;
}
