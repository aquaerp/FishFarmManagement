using System.Text;
using FishFarmManager.Models;

namespace FishFarmManager.Tests;

public sealed class Phase4ZatcaFoundationTests
{
    [Fact]
    public void GenerationQr_UsesUtf8TlvTagsOneThroughFive()
    {
        var invoice = new TaxInvoice
        {
            SellerName = "مزرعة الأسماك",
            SellerVATNumber = "310123456700003",
            IssueDate = new DateTime(2026, 7, 15, 14, 30, 45, DateTimeKind.Local),
            TotalWithVAT = 115.00m,
            VATAmount = 15.00m
        };

        var fields = Decode(Convert.FromBase64String(invoice.GenerateQRCodeContent()));

        Assert.Equal(new byte[] { 1, 2, 3, 4, 5 }, fields.Select(value => value.Tag));
        Assert.Equal("مزرعة الأسماك", fields[0].Value);
        Assert.Equal("310123456700003", fields[1].Value);
        Assert.StartsWith("2026-07-15T14:30:45", fields[2].Value);
        Assert.Equal("115.00", fields[3].Value);
        Assert.Equal("15.00", fields[4].Value);
        Assert.Equal(Encoding.UTF8.GetByteCount("مزرعة الأسماك"), fields[0].ByteLength);
    }

    [Fact]
    public void GenerationQr_RejectsFieldThatCannotFitOneByteTlvLength()
    {
        var invoice = new TaxInvoice
        {
            SellerName = new string('أ', 128),
            SellerVATNumber = "310123456700003",
            IssueDate = DateTime.Now,
            TotalWithVAT = 1m,
            VATAmount = 0m
        };

        Assert.Throws<InvalidOperationException>(() => invoice.GenerateQRCodeContent());
    }

    private static List<(byte Tag, int ByteLength, string Value)> Decode(byte[] payload)
    {
        var fields = new List<(byte Tag, int ByteLength, string Value)>();
        var offset = 0;
        while (offset < payload.Length)
        {
            var tag = payload[offset++];
            var length = payload[offset++];
            fields.Add((tag, length, Encoding.UTF8.GetString(payload, offset, length)));
            offset += length;
        }
        Assert.Equal(payload.Length, offset);
        return fields;
    }
}
