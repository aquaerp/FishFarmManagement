using System.Xml.Linq;
using FishFarmManager.Models;
using FishFarmManager.Services;

namespace FishFarmManager.Tests;

public sealed class Phase4ZatcaUblTests
{
    [Fact]
    public void StandardTaxInvoice_GeneratesUblInvoiceForClearance()
    {
        var request = Request(ZatcaInvoiceProfile.Standard, ZatcaDocumentKind.TaxInvoice);
        var document = new ZatcaUblGenerator().Generate(request);
        var root = document.Root!;

        Assert.Equal(ZatcaUblGenerator.InvoiceNs + "Invoice", root.Name);
        Assert.Equal("388", root.Element(ZatcaUblGenerator.Cbc + "InvoiceTypeCode")!.Value);
        Assert.Equal("0100000", root.Element(ZatcaUblGenerator.Cbc + "InvoiceTypeCode")!.Attribute("name")!.Value);
        Assert.Equal(ZatcaSubmissionRoute.Clearance, request.SubmissionRoute);
        Assert.Equal("100.00", root.Descendants(ZatcaUblGenerator.Cbc + "TaxExclusiveAmount").Single().Value);
        Assert.Equal("15.00", root.Elements(ZatcaUblGenerator.Cac + "TaxTotal")
            .Single().Element(ZatcaUblGenerator.Cbc + "TaxAmount")!.Value);
        Assert.Equal("115.00", root.Descendants(ZatcaUblGenerator.Cbc + "PayableAmount").Single().Value);
        Assert.Equal("ICV", root.Elements(ZatcaUblGenerator.Cac + "AdditionalDocumentReference").First()
            .Element(ZatcaUblGenerator.Cbc + "ID")!.Value);
    }

    [Fact]
    public void SimplifiedCreditNote_UsesCreditNoteRootAndOriginalReferenceForReporting()
    {
        var request = Request(ZatcaInvoiceProfile.Simplified, ZatcaDocumentKind.CreditNote) with
        {
            OriginalInvoiceNumber = "INV-ORIGINAL-1",
            NoteReason = "Returned goods"
        };
        var root = new ZatcaUblGenerator().Generate(request).Root!;

        Assert.Equal(ZatcaUblGenerator.CreditNoteNs + "CreditNote", root.Name);
        Assert.Equal("381", root.Element(ZatcaUblGenerator.Cbc + "CreditNoteTypeCode")!.Value);
        Assert.Equal("0200000", root.Element(ZatcaUblGenerator.Cbc + "CreditNoteTypeCode")!.Attribute("name")!.Value);
        Assert.Equal("INV-ORIGINAL-1", root.Descendants(ZatcaUblGenerator.Cac + "InvoiceDocumentReference")
            .Single().Element(ZatcaUblGenerator.Cbc + "ID")!.Value);
        Assert.Single(root.Elements(ZatcaUblGenerator.Cac + "CreditNoteLine"));
        Assert.Equal(ZatcaSubmissionRoute.Reporting, request.SubmissionRoute);
    }

    [Fact]
    public void DebitNote_UsesInvoiceMessageAndCode383()
    {
        var request = Request(ZatcaInvoiceProfile.Standard, ZatcaDocumentKind.DebitNote) with
        {
            OriginalInvoiceNumber = "INV-ORIGINAL-2",
            NoteReason = "Price correction"
        };
        var root = new ZatcaUblGenerator().Generate(request).Root!;

        Assert.Equal(ZatcaUblGenerator.InvoiceNs + "Invoice", root.Name);
        Assert.Equal("383", root.Element(ZatcaUblGenerator.Cbc + "InvoiceTypeCode")!.Value);
        Assert.Single(root.Elements(ZatcaUblGenerator.Cac + "InvoiceLine"));
    }

    [Fact]
    public void NotesAndStandardBuyer_RequireGovernedReferencesAndVatIdentity()
    {
        var generator = new ZatcaUblGenerator();
        Assert.Throws<InvalidOperationException>(() => generator.Generate(
            Request(ZatcaInvoiceProfile.Standard, ZatcaDocumentKind.CreditNote)));
        Assert.Throws<InvalidOperationException>(() => generator.Generate(
            Request(ZatcaInvoiceProfile.Standard, ZatcaDocumentKind.TaxInvoice) with
            {
                Buyer = Party("Buyer", "")
            }));
    }

    private static ZatcaUblDocumentRequest Request(ZatcaInvoiceProfile profile, ZatcaDocumentKind kind) =>
        new("INV-2026-0001", Guid.Parse("4674c44c-2d72-44a5-9289-77d5b102c9b3"),
            new DateTimeOffset(2026, 7, 15, 14, 30, 45, TimeSpan.FromHours(3)), profile, kind, 1,
            Convert.ToBase64String(new byte[32]), Party("Aqua Farm", "310123456700003"),
            Party("Customer", "310987654300003"),
            new[] { new ZatcaDocumentLine("1", "Fresh fish", 2m, "KGM", 50m, 0m, "S", 15m) });

    private static ZatcaParty Party(string name, string vat) =>
        new(name, vat, "King Road", "1234", "Riyadh", "12345", "SA");
}
