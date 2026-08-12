using System.Globalization;
using FishFarmManager.Models;

namespace FishFarmManager.Services;

public sealed class PurchaseOrderDocumentService
{
    public IReadOnlyList<string> BuildLines(PurchaseOrder order)
    {
        ArgumentNullException.ThrowIfNull(order);
        if (string.IsNullOrWhiteSpace(order.OrderNumber))
            throw new InvalidOperationException("Purchase order number is required for printing.");
        if (order.Supplier == null)
            throw new InvalidOperationException("Purchase order supplier is required for printing.");
        if (order.Items.Count == 0)
            throw new InvalidOperationException("Purchase order has no items to print.");

        var lines = new List<string>
        {
            "أمر شراء",
            $"رقم الأمر: {order.OrderNumber}",
            $"التاريخ: {order.OrderDate:yyyy/MM/dd}",
            $"المورد: {order.Supplier.Name}",
            $"التسليم المتوقع: {order.ExpectedDeliveryDate:yyyy/MM/dd}",
            $"الحالة: {order.GetStatusDisplay()}",
            new string('─', 72),
            "الصنف | الكمية | سعر الوحدة | الخصم | الإجمالي"
        };

        lines.AddRange(order.Items.OrderBy(item => item.Id).Select(item =>
            $"{item.ItemName} | {Number(item.Quantity)} | {Money(item.UnitPrice)} | {Money(item.DiscountAmount)} | {Money(item.TotalPrice)}"));
        lines.Add(new string('─', 72));
        lines.Add($"الإجمالي قبل الضريبة: {Money(order.SubTotal)} ر.س");
        lines.Add($"الخصم: {Money(order.DiscountAmount)} ر.س");
        lines.Add($"ضريبة القيمة المضافة: {Money(order.VATAmount)} ر.س");
        lines.Add($"الإجمالي النهائي: {Money(order.Total)} ر.س");
        if (!string.IsNullOrWhiteSpace(order.PaymentTerms))
            lines.Add($"شروط الدفع: {order.PaymentTerms}");
        if (!string.IsNullOrWhiteSpace(order.Notes))
            lines.Add($"ملاحظات: {order.Notes}");
        return lines;
    }

    private static string Money(decimal value) => value.ToString("N2", CultureInfo.GetCultureInfo("ar-SA"));
    private static string Number(decimal value) => value.ToString("N3", CultureInfo.GetCultureInfo("ar-SA"));
}
