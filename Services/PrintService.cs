using System;
using System.IO;
using System.Drawing;
using System.Drawing.Printing;
using PdfSharpCore.Pdf;
using PdfSharpCore.Drawing;
using FishFarmManager.Models;

namespace FishFarmManager.Services
{
    /// <summary>
    /// خدمة الطباعة - Print Service
    /// توفر وظائف الطباعة والتصدير لـ PDF
    /// </summary>
    public class PrintService
    {
        private readonly string _defaultPrinter;
        private readonly string _pdfOutputPath;

        public PrintService()
        {
            _defaultPrinter = GetDefaultPrinter();
            _pdfOutputPath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments), "AquaFarm Pro", "PDF");
            
            // إنشاء مجلد PDF إذا لم يكن موجوداً
            if (!Directory.Exists(_pdfOutputPath))
            {
                Directory.CreateDirectory(_pdfOutputPath);
            }
        }

        /// <summary>
        /// الحصول على الطابعة الافتراضية
        /// </summary>
        private string GetDefaultPrinter()
        {
            try
            {
                var ps = new PrinterSettings();
                return ps.PrinterName;
            }
            catch
            {
                return string.Empty;
            }
        }

        /// <summary>
        /// إنشاء PDF للفاتورة الضريبية
        /// </summary>
        public string CreateTaxInvoicePDF(TaxInvoice invoice, bool includeQR = true)
        {
            try
            {
                // إنشاء ملف PDF
                var document = new PdfDocument();
                document.Info.Title = $"فاتورة ضريبية - {invoice.InvoiceNumber}";
                document.Info.Author = "AquaFarm Pro";
                document.Info.Subject = "Tax Invoice - فاتورة ضريبية";

                // إضافة صفحة
                var page = document.AddPage();
                page.Width = XUnit.FromMillimeter(210); // A4
                page.Height = XUnit.FromMillimeter(297);

                var gfx = XGraphics.FromPdfPage(page);
                
                // الخطوط
                var titleFont = new XFont("Arial", 20, XFontStyle.Bold);
                var headerFont = new XFont("Arial", 14, XFontStyle.Bold);
                var normalFont = new XFont("Arial", 11, XFontStyle.Regular);
                var smallFont = new XFont("Arial", 9, XFontStyle.Regular);

                double y = 40;
                double margin = 40;
                double pageWidth = page.Width.Point;

                // الشعار والعنوان
                DrawRTLText(gfx, "AquaFarm Pro", titleFont, margin, y, pageWidth - margin * 2, XStringFormats.CenterRight);
                y += 25;
                
                DrawRTLText(gfx, "فاتورة ضريبية - Tax Invoice", headerFont, margin, y, pageWidth - margin * 2, XStringFormats.Center);
                y += 30;

                // خط فاصل
                gfx.DrawLine(XPens.Black, margin, y, pageWidth - margin, y);
                y += 20;

                // بيانات الفاتورة
                DrawRTLText(gfx, $"رقم الفاتورة: {invoice.InvoiceNumber}", normalFont, margin, y, 250, XStringFormats.CenterRight);
                DrawRTLText(gfx, $"Invoice No: {invoice.InvoiceNumber}", normalFont, pageWidth - margin - 250, y, 250, XStringFormats.CenterLeft);
                y += 20;

                DrawRTLText(gfx, $"التاريخ: {invoice.IssueDate:yyyy/MM/dd}", normalFont, margin, y, 250, XStringFormats.CenterRight);
                DrawRTLText(gfx, $"Date: {invoice.IssueDate:yyyy/MM/dd}", normalFont, pageWidth - margin - 250, y, 250, XStringFormats.CenterLeft);
                y += 30;

                // بيانات البائع
                DrawRTLText(gfx, "بيانات البائع - Seller Information", headerFont, margin, y, pageWidth - margin * 2, XStringFormats.Center);
                y += 20;

                DrawRTLText(gfx, $"الاسم: {invoice.SellerName}", normalFont, margin, y, pageWidth - margin * 2, XStringFormats.CenterRight);
                y += 18;
                DrawRTLText(gfx, $"الرقم الضريبي: {invoice.SellerVATNumber}", normalFont, margin, y, pageWidth - margin * 2, XStringFormats.CenterRight);
                y += 18;
                if (!string.IsNullOrEmpty(invoice.SellerAddress))
                {
                    DrawRTLText(gfx, $"العنوان: {invoice.SellerAddress}", normalFont, margin, y, pageWidth - margin * 2, XStringFormats.CenterRight);
                    y += 18;
                }
                y += 10;

                // بيانات المشتري
                DrawRTLText(gfx, "بيانات المشتري - Buyer Information", headerFont, margin, y, pageWidth - margin * 2, XStringFormats.Center);
                y += 20;

                DrawRTLText(gfx, $"الاسم: {invoice.BuyerName}", normalFont, margin, y, pageWidth - margin * 2, XStringFormats.CenterRight);
                y += 18;
                if (!string.IsNullOrEmpty(invoice.BuyerVATNumber))
                {
                    DrawRTLText(gfx, $"الرقم الضريبي: {invoice.BuyerVATNumber}", normalFont, margin, y, pageWidth - margin * 2, XStringFormats.CenterRight);
                    y += 18;
                }
                y += 20;

                // جدول البنود
                DrawRTLText(gfx, "البنود - Items", headerFont, margin, y, pageWidth - margin * 2, XStringFormats.Center);
                y += 20;

                // رأس الجدول
                var tableTop = y;
                var col1 = margin;
                var col2 = margin + 220;
                var col3 = margin + 300;
                var col4 = margin + 360;
                var col5 = margin + 420;
                var col6 = margin + 480;

                gfx.DrawRectangle(XBrushes.LightGray, col1, y, pageWidth - margin * 2, 25);
                DrawRTLText(gfx, "الوصف", normalFont, col1 + 5, y + 5, 210, XStringFormats.CenterRight);
                DrawRTLText(gfx, "الكمية", normalFont, col2 + 5, y + 5, 70, XStringFormats.Center);
                DrawRTLText(gfx, "السعر", normalFont, col3 + 5, y + 5, 55, XStringFormats.Center);
                DrawRTLText(gfx, "الخصم", normalFont, col4 + 5, y + 5, 55, XStringFormats.Center);
                DrawRTLText(gfx, "الضريبة", normalFont, col5 + 5, y + 5, 55, XStringFormats.Center);
                DrawRTLText(gfx, "الإجمالي", normalFont, col6 + 5, y + 5, 55, XStringFormats.Center);
                y += 25;

                // البنود
                foreach (var item in invoice.Items)
                {
                    gfx.DrawLine(XPens.LightGray, margin, y, pageWidth - margin, y);
                    y += 5;

                    var itemText = $"{item.ItemName}";
                    if (!string.IsNullOrEmpty(item.Description))
                        itemText += $"\n{item.Description}";

                    DrawRTLText(gfx, itemText, smallFont, col1 + 5, y, 210, XStringFormats.TopRight);
                    DrawRTLText(gfx, item.Quantity.ToString("N2"), smallFont, col2 + 5, y, 70, XStringFormats.Center);
                    DrawRTLText(gfx, item.UnitPrice.ToString("N2"), smallFont, col3 + 5, y, 55, XStringFormats.Center);
                    DrawRTLText(gfx, item.DiscountAmount.ToString("N2"), smallFont, col4 + 5, y, 55, XStringFormats.Center);
                    DrawRTLText(gfx, item.VATAmount.ToString("N2"), smallFont, col5 + 5, y, 55, XStringFormats.Center);
                    DrawRTLText(gfx, item.TotalAmount.ToString("N2"), smallFont, col6 + 5, y, 55, XStringFormats.Center);

                    y += 25;

                    // صفحة جديدة إذا امتلأت الصفحة
                    if (y > page.Height.Point - 150)
                    {
                        page = document.AddPage();
                        page.Width = XUnit.FromMillimeter(210);
                        page.Height = XUnit.FromMillimeter(297);
                        gfx = XGraphics.FromPdfPage(page);
                        y = 40;
                    }
                }

                y += 10;
                gfx.DrawLine(XPens.Black, margin, y, pageWidth - margin, y);
                y += 20;

                // الإجماليات
                var totalsX = pageWidth - margin - 200;
                DrawRTLText(gfx, $"المجموع الفرعي: {invoice.SubTotal:N2} ريال", normalFont, totalsX, y, 200, XStringFormats.CenterRight);
                y += 20;

                if (invoice.DiscountAmount > 0)
                {
                    DrawRTLText(gfx, $"الخصم: {invoice.DiscountAmount:N2} ريال", normalFont, totalsX, y, 200, XStringFormats.CenterRight);
                    y += 20;
                }

                DrawRTLText(gfx, $"ضريبة القيمة المضافة ({invoice.VATRate}%): {invoice.VATAmount:N2} ريال", normalFont, totalsX, y, 200, XStringFormats.CenterRight);
                y += 20;

                gfx.DrawRectangle(XBrushes.LightGray, totalsX - 5, y - 5, 205, 25);
                DrawRTLText(gfx, $"الإجمالي: {invoice.TotalWithVAT:N2} ريال", headerFont, totalsX, y, 200, XStringFormats.CenterRight);
                y += 30;

                // QR Code (إذا كان موجوداً)
                if (includeQR && invoice.QRCodeImage != null && invoice.QRCodeImage.Length > 0)
                {
                    try
                    {
                        using (var ms = new MemoryStream(invoice.QRCodeImage))
                        {
                            var qrImage = XImage.FromStream(() => ms);
                            gfx.DrawImage(qrImage, margin, page.Height.Point - 140, 100, 100);
                        }

                        DrawRTLText(gfx, "امسح رمز QR للتحقق", smallFont, margin, page.Height.Point - 35, 100, XStringFormats.Center);
                    }
                    catch
                    {
                        // تجاهل خطأ QR
                    }
                }

                // الملاحظات
                if (!string.IsNullOrEmpty(invoice.Notes))
                {
                    y = page.Height.Point - 120;
                    DrawRTLText(gfx, $"ملاحظات: {invoice.Notes}", smallFont, margin + 120, y, pageWidth - margin * 2 - 120, XStringFormats.TopRight);
                }

                // حفظ الملف
                var fileName = $"TaxInvoice_{invoice.InvoiceNumber}_{DateTime.Now:yyyyMMdd_HHmmss}.pdf";
                var fullPath = Path.Combine(_pdfOutputPath, fileName);

                document.Save(fullPath);
                document.Close();

                LoggingService.LogInfo($"PDF created successfully: {fileName}");
                return fullPath;
            }
            catch (Exception ex)
            {
                LoggingService.LogError(ex, "Error creating tax invoice PDF");
                throw;
            }
        }

        /// <summary>
        /// رسم نص بـ RTL (من اليمين لليسار)
        /// </summary>
        private void DrawRTLText(XGraphics gfx, string text, XFont font, double x, double y, double width, XStringFormat format)
        {
            var rect = new XRect(x, y, width, 20);
            gfx.DrawString(text, font, XBrushes.Black, rect, format);
        }

        /// <summary>
        /// طباعة PDF مباشرة
        /// </summary>
        public bool PrintPDF(string pdfPath)
        {
            try
            {
                if (!File.Exists(pdfPath))
                {
                    LoggingService.LogError($"PDF file not found: {pdfPath}");
                    return false;
                }

                // فتح الملف باستخدام البرنامج الافتراضي
                System.Diagnostics.Process.Start(new System.Diagnostics.ProcessStartInfo
                {
                    FileName = pdfPath,
                    UseShellExecute = true
                });

                LoggingService.LogInfo($"PDF opened for printing: {pdfPath}");
                return true;
            }
            catch (Exception ex)
            {
                LoggingService.LogError(ex, "Error printing PDF");
                return false;
            }
        }

        /// <summary>
        /// الحصول على مسار مجلد PDF
        /// </summary>
        public string GetPDFOutputPath() => _pdfOutputPath;

        /// <summary>
        /// إنشاء PDF لإقرار ضريبة القيمة المضافة
        /// </summary>
        public string CreateVATReturnPDF(VATReturn vatReturn)
        {
            try
            {
                var document = new PdfDocument();
                document.Info.Title = $"إقرار ضريبة القيمة المضافة - {vatReturn.PeriodNumber}";
                document.Info.Author = "AquaFarm Pro";
                document.Info.Subject = "VAT Return - إقرار ضريبة القيمة المضافة";

                var page = document.AddPage();
                page.Width = XUnit.FromMillimeter(210); // A4
                page.Height = XUnit.FromMillimeter(297);

                var gfx = XGraphics.FromPdfPage(page);
                
                // الخطوط
                var titleFont = new XFont("Arial", 18, XFontStyle.Bold);
                var headerFont = new XFont("Arial", 14, XFontStyle.Bold);
                var normalFont = new XFont("Arial", 11, XFontStyle.Regular);
                var boldFont = new XFont("Arial", 11, XFontStyle.Bold);

                double y = 40;
                double margin = 40;
                double pageWidth = page.Width.Point;

                // العنوان
                DrawRTLText(gfx, "المملكة العربية السعودية", headerFont, margin, y, pageWidth - margin * 2, XStringFormats.Center);
                y += 20;
                DrawRTLText(gfx, "هيئة الزكاة والضريبة والجمارك", headerFont, margin, y, pageWidth - margin * 2, XStringFormats.Center);
                y += 25;
                DrawRTLText(gfx, "إقرار ضريبة القيمة المضافة", titleFont, margin, y, pageWidth - margin * 2, XStringFormats.Center);
                y += 20;
                DrawRTLText(gfx, "VAT Return", normalFont, margin, y, pageWidth - margin * 2, XStringFormats.Center);
                y += 30;

                // خط فاصل
                gfx.DrawLine(XPens.Black, margin, y, pageWidth - margin, y);
                y += 20;

                // بيانات الإقرار
                DrawRTLText(gfx, $"رقم الفترة: {vatReturn.PeriodNumber}", normalFont, margin, y, 300, XStringFormats.CenterRight);
                DrawRTLText(gfx, $"Period: {vatReturn.PeriodNumber}", normalFont, pageWidth - margin - 250, y, 250, XStringFormats.CenterLeft);
                y += 20;

                DrawRTLText(gfx, $"الفترة: من {vatReturn.PeriodStartDate:yyyy/MM/dd} إلى {vatReturn.PeriodEndDate:yyyy/MM/dd}", normalFont, margin, y, pageWidth - margin * 2, XStringFormats.CenterRight);
                y += 20;

                DrawRTLText(gfx, $"تاريخ الاستحقاق: {vatReturn.DueDate:yyyy/MM/dd}", normalFont, margin, y, 300, XStringFormats.CenterRight);
                y += 20;

                DrawRTLText(gfx, $"الرقم الضريبي: {vatReturn.TaxRegistrationNumber}", normalFont, margin, y, 300, XStringFormats.CenterRight);
                y += 30;

                // جدول الإقرار
                var boxWidth = pageWidth - margin * 2;
                var col1Width = 400;
                var col2Width = boxWidth - col1Width;

                // قسم المبيعات
                gfx.DrawRectangle(XBrushes.LightBlue, margin, y, boxWidth, 25);
                DrawRTLText(gfx, "المبيعات - Sales", headerFont, margin + 10, y + 5, col1Width, XStringFormats.CenterRight);
                y += 25;

                y = DrawVATBox(gfx, y, margin, col1Width, col2Width, "1", "المبيعات المحلية الخاضعة لضريبة القيمة المضافة", vatReturn.Box1_TaxableSalesInKSA, normalFont);
                y = DrawVATBox(gfx, y, margin, col1Width, col2Width, "2", "مبيعات الصفر", vatReturn.Box2_ZeroRatedSales, normalFont);
                y = DrawVATBox(gfx, y, margin, col1Width, col2Width, "3", "الصادرات", vatReturn.Box3_Exports, normalFont);
                y = DrawVATBox(gfx, y, margin, col1Width, col2Width, "4", "مبيعات معفاة", vatReturn.Box4_ExemptSales, normalFont);
                
                gfx.DrawRectangle(XBrushes.LightYellow, margin, y, boxWidth, 25);
                y = DrawVATBox(gfx, y, margin, col1Width, col2Width, "5", "إجمالي المبيعات", vatReturn.Box5_TotalSales, boldFont, true);
                
                gfx.DrawRectangle(XBrushes.LightGreen, margin, y, boxWidth, 25);
                y = DrawVATBox(gfx, y, margin, col1Width, col2Width, "6", "ضريبة القيمة المضافة على المبيعات", vatReturn.Box6_VATOnSales, boldFont, true);
                
                y += 10;

                // قسم المشتريات
                gfx.DrawRectangle(XBrushes.LightBlue, margin, y, boxWidth, 25);
                DrawRTLText(gfx, "المشتريات - Purchases", headerFont, margin + 10, y + 5, col1Width, XStringFormats.CenterRight);
                y += 25;

                y = DrawVATBox(gfx, y, margin, col1Width, col2Width, "7", "إجمالي المشتريات", vatReturn.Box7_TotalPurchases, normalFont);
                y = DrawVATBox(gfx, y, margin, col1Width, col2Width, "8", "مشتريات من دول مجلس التعاون", vatReturn.Box8_GCCPurchases, normalFont);
                y = DrawVATBox(gfx, y, margin, col1Width, col2Width, "9", "الواردات الخاضعة للضريبة", vatReturn.Box9_TaxableImports, normalFont);
                
                gfx.DrawRectangle(XBrushes.LightGreen, margin, y, boxWidth, 25);
                y = DrawVATBox(gfx, y, margin, col1Width, col2Width, "10", "ضريبة القيمة المضافة على المشتريات", vatReturn.Box10_VATOnPurchases, boldFont, true);
                
                y += 10;

                // قسم الصافي
                gfx.DrawRectangle(XBrushes.LightBlue, margin, y, boxWidth, 25);
                DrawRTLText(gfx, "الصافي - Net Amount", headerFont, margin + 10, y + 5, col1Width, XStringFormats.CenterRight);
                y += 25;

                y = DrawVATBox(gfx, y, margin, col1Width, col2Width, "11", "صافي ضريبة القيمة المضافة المستحقة", vatReturn.Box11_NetVATDue, normalFont);
                y = DrawVATBox(gfx, y, margin, col1Width, col2Width, "12", "تعديلات", vatReturn.Box12_Adjustments, normalFont);
                
                gfx.DrawRectangle(XBrushes.LightCoral, margin, y, boxWidth, 30);
                y = DrawVATBox(gfx, y, margin, col1Width, col2Width, "13", "إجمالي ضريبة القيمة المضافة المستحقة", vatReturn.Box13_TotalVATDue, titleFont, true);
                
                y = DrawVATBox(gfx, y, margin, col1Width, col2Width, "14", "المبلغ المسترد من الفترة السابقة", vatReturn.Box14_RecoverablePreviousPeriod, normalFont);
                
                gfx.DrawRectangle(XBrushes.Gold, margin, y, boxWidth, 30);
                y = DrawVATBox(gfx, y, margin, col1Width, col2Width, "15", "صافي ضريبة القيمة المضافة المستحقة للفترة", vatReturn.Box15_NetVATDueForPeriod, titleFont, true);

                // الملاحظات
                if (!string.IsNullOrEmpty(vatReturn.Notes))
                {
                    y += 20;
                    DrawRTLText(gfx, "ملاحظات:", boldFont, margin, y, 100, XStringFormats.CenterRight);
                    y += 20;
                    DrawRTLText(gfx, vatReturn.Notes, normalFont, margin, y, pageWidth - margin * 2, XStringFormats.TopRight);
                }

                // التذييل
                y = page.Height.Point - 80;
                gfx.DrawLine(XPens.Black, margin, y, pageWidth - margin, y);
                y += 10;
                DrawRTLText(gfx, $"تاريخ الطباعة: {DateTime.Now:yyyy/MM/dd HH:mm}", normalFont, margin, y, pageWidth - margin * 2, XStringFormats.CenterRight);
                y += 15;
                DrawRTLText(gfx, "هذا الإقرار مُنشأ آلياً بواسطة AquaFarm Pro", normalFont, margin, y, pageWidth - margin * 2, XStringFormats.Center);

                // حفظ
                var fileName = $"VATReturn_{vatReturn.PeriodNumber}_{DateTime.Now:yyyyMMdd_HHmmss}.pdf";
                var fullPath = Path.Combine(_pdfOutputPath, fileName);

                document.Save(fullPath);
                document.Close();

                LoggingService.LogInfo($"VAT Return PDF created: {fileName}");
                return fullPath;
            }
            catch (Exception ex)
            {
                LoggingService.LogError(ex, "Error creating VAT return PDF");
                throw;
            }
        }

        private double DrawVATBox(XGraphics gfx, double y, double margin, double col1Width, double col2Width, string boxNumber, string label, decimal amount, XFont font, bool isBold = false)
        {
            var boxHeight = 25;
            var totalWidth = col1Width + col2Width;
            
            // خلفية للصفوف البديلة
            if (!isBold && int.Parse(boxNumber) % 2 == 0)
            {
                gfx.DrawRectangle(new XSolidBrush(XColor.FromArgb(245, 245, 245)), margin, y, totalWidth, boxHeight);
            }

            // رقم الصندوق والتسمية
            DrawRTLText(gfx, $"{boxNumber}. {label}", font, margin + 10, y + 5, col1Width - 20, XStringFormats.CenterRight);
            
            // المبلغ
            DrawRTLText(gfx, $"{amount:N2}", font, margin + col1Width + 10, y + 5, col2Width - 20, XStringFormats.Center);

            // حدود
            gfx.DrawRectangle(XPens.Gray, margin, y, totalWidth, boxHeight);

            return y + boxHeight;
        }

        /// <summary>
        /// إنشاء PDF لتقرير مالي عام
        /// </summary>
        public string CreateFinancialReportPDF(string reportTitle, string reportSubtitle, System.Data.DataTable data, DateTime startDate, DateTime endDate)
        {
            try
            {
                var document = new PdfDocument();
                document.Info.Title = reportTitle;
                document.Info.Author = "AquaFarm Pro";

                var page = document.AddPage();
                page.Width = XUnit.FromMillimeter(210); // A4
                page.Height = XUnit.FromMillimeter(297);

                var gfx = XGraphics.FromPdfPage(page);
                
                var titleFont = new XFont("Arial", 16, XFontStyle.Bold);
                var headerFont = new XFont("Arial", 12, XFontStyle.Bold);
                var normalFont = new XFont("Arial", 10, XFontStyle.Regular);

                double y = 40;
                double margin = 40;
                double pageWidth = page.Width.Point;

                // العنوان
                DrawRTLText(gfx, reportTitle, titleFont, margin, y, pageWidth - margin * 2, XStringFormats.Center);
                y += 25;
                DrawRTLText(gfx, reportSubtitle, headerFont, margin, y, pageWidth - margin * 2, XStringFormats.Center);
                y += 20;
                DrawRTLText(gfx, $"من {startDate:yyyy/MM/dd} إلى {endDate:yyyy/MM/dd}", normalFont, margin, y, pageWidth - margin * 2, XStringFormats.Center);
                y += 30;

                gfx.DrawLine(XPens.Black, margin, y, pageWidth - margin, y);
                y += 20;

                // البيانات من DataTable
                if (data != null && data.Rows.Count > 0)
                {
                    // رأس الجدول
                    double colWidth = (pageWidth - margin * 2) / data.Columns.Count;
                    double x = margin;

                    gfx.DrawRectangle(XBrushes.LightGray, margin, y, pageWidth - margin * 2, 25);
                    foreach (System.Data.DataColumn column in data.Columns)
                    {
                        DrawRTLText(gfx, column.ColumnName, headerFont, x, y + 5, colWidth, XStringFormats.Center);
                        x += colWidth;
                    }
                    y += 25;

                    // الصفوف
                    foreach (System.Data.DataRow row in data.Rows)
                    {
                        if (y > page.Height.Point - 80)
                        {
                            page = document.AddPage();
                            page.Width = XUnit.FromMillimeter(210);
                            page.Height = XUnit.FromMillimeter(297);
                            gfx = XGraphics.FromPdfPage(page);
                            y = 40;
                        }

                        x = margin;
                        bool isTotal = row[0].ToString()?.Contains("إجمالي") == true || 
                                      row[0].ToString()?.Contains("صافي") == true;

                        if (isTotal)
                        {
                            gfx.DrawRectangle(XBrushes.LightYellow, margin, y, pageWidth - margin * 2, 22);
                        }

                        foreach (var item in row.ItemArray)
                        {
                            var text = item?.ToString() ?? "";
                            var font = isTotal ? headerFont : normalFont;
                            DrawRTLText(gfx, text, font, x, y + 3, colWidth, XStringFormats.Center);
                            x += colWidth;
                        }

                        gfx.DrawLine(XPens.LightGray, margin, y + 22, pageWidth - margin, y + 22);
                        y += 22;
                    }
                }

                // التذييل
                y = page.Height.Point - 60;
                gfx.DrawLine(XPens.Black, margin, y, pageWidth - margin, y);
                y += 10;
                DrawRTLText(gfx, $"تاريخ الطباعة: {DateTime.Now:yyyy/MM/dd HH:mm}", normalFont, margin, y, pageWidth - margin * 2, XStringFormats.CenterRight);
                y += 15;
                DrawRTLText(gfx, "مُنشأ آلياً بواسطة AquaFarm Pro", normalFont, margin, y, pageWidth - margin * 2, XStringFormats.Center);

                // حفظ
                var fileName = $"{reportTitle.Replace(" ", "_")}_{DateTime.Now:yyyyMMdd_HHmmss}.pdf";
                var fullPath = Path.Combine(_pdfOutputPath, fileName);

                document.Save(fullPath);
                document.Close();

                LoggingService.LogInfo($"Financial report PDF created: {fileName}");
                return fullPath;
            }
            catch (Exception ex)
            {
                LoggingService.LogError(ex, "Error creating financial report PDF");
                throw;
            }
        }
    }
}

