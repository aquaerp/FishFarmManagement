using System.Globalization;
using FishFarmManager.Models;

namespace FishFarmManager.Services;

public sealed class SalesOrderDocumentService
{
    public IReadOnlyList<string> BuildInvoiceLines(SalesOrder order)
    {
        ArgumentNullException.ThrowIfNull(order);
        if (order.Customer == null) throw new InvalidOperationException("Customer is required for invoice printing.");
        if (order.Items.Count == 0) throw new InvalidOperationException("Sales order has no items to print.");
        var culture = CultureInfo.GetCultureInfo("ar-SA");
        var lines = new List<string>
        {
            "فاتورة مبيعات", $"رقم الطلب: {order.OrderNumber}", $"التاريخ: {order.OrderDate:yyyy/MM/dd}",
            $"العميل: {order.Customer.Name}", new string('─', 72),
            "الصنف | الكمية | سعر الوحدة | الخصم | الإجمالي"
        };
        lines.AddRange(order.Items.OrderBy(item => item.Id).Select(item =>
            $"{item.ProductName} | {item.Quantity.ToString("N3", culture)} | {item.UnitPrice.ToString("N2", culture)} | {item.DiscountAmount.ToString("N2", culture)} | {item.TotalPrice.ToString("N2", culture)}"));
        lines.Add(new string('─', 72));
        lines.Add($"الإجمالي قبل الضريبة: {order.SubTotal.ToString("N2", culture)} ر.س");
        lines.Add($"الضريبة: {(order.VATAmount != 0 ? order.VATAmount : order.TaxAmount).ToString("N2", culture)} ر.س");
        lines.Add($"الإجمالي النهائي: {(order.GrandTotal != 0 ? order.GrandTotal : order.TotalAmount).ToString("N2", culture)} ر.س");
        return lines;
    }
}
