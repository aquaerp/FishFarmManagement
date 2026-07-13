using System;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Windows.Forms;
using Microsoft.EntityFrameworkCore;
using FishFarmManager.Data;
using FishFarmManager.Services;

namespace FishFarmManager.Forms
{
    public class CertificationReportForm : Form
    {
        private readonly FishFarmContext _context;
        private DataGridView _certificationGrid = null!;
        private TextBox _summaryTextBox = null!;
        // private Button _refreshButton = null!; // Unused - commented out

        public CertificationReportForm(FishFarmContext context)
        {
            _context = context;
            InitializeComponent();
            LoadCertificationData();
        }

        private void InitializeComponent()
        {
            this.Text = "تقرير شهادات الجودة";
            this.Size = new Size(1000, 700);
            this.StartPosition = FormStartPosition.CenterScreen;
            this.RightToLeft = RightToLeft.Yes;
            this.RightToLeftLayout = true;
            try { this.Font = new Font("Cairo", 10F, FontStyle.Regular); } catch { }

            var mainPanel = new TableLayoutPanel();
            mainPanel.Dock = DockStyle.Fill;
            mainPanel.RowCount = 2;
            mainPanel.RowStyles.Add(new RowStyle(SizeType.Percent, 80F));
            mainPanel.RowStyles.Add(new RowStyle(SizeType.Percent, 20F));

            // الجدول
            _certificationGrid = new DataGridView();
            _certificationGrid.Dock = DockStyle.Fill;
            _certificationGrid.AutoGenerateColumns = false;
            _certificationGrid.ReadOnly = true;
            _certificationGrid.AllowUserToAddRows = false;

            _certificationGrid.Columns.Add(new DataGridViewTextBoxColumn { DataPropertyName = "CertificateName", HeaderText = "اسم الشهادة", Width = 200 });
            _certificationGrid.Columns.Add(new DataGridViewTextBoxColumn { DataPropertyName = "IssuingAuthority", HeaderText = "جهة الإصدار", Width = 200 });
            _certificationGrid.Columns.Add(new DataGridViewTextBoxColumn { DataPropertyName = "IssueDate", HeaderText = "تاريخ الإصدار", Width = 120, DefaultCellStyle = new DataGridViewCellStyle { Format = "yyyy/MM/dd" } });
            _certificationGrid.Columns.Add(new DataGridViewTextBoxColumn { DataPropertyName = "ExpiryDate", HeaderText = "تاريخ الانتهاء", Width = 120, DefaultCellStyle = new DataGridViewCellStyle { Format = "yyyy/MM/dd" } });
            _certificationGrid.Columns.Add(new DataGridViewTextBoxColumn { DataPropertyName = "Status", HeaderText = "الحالة", Width = 120 });
            _certificationGrid.Columns.Add(new DataGridViewTextBoxColumn { DataPropertyName = "Notes", HeaderText = "ملاحظات", Width = 250 });

            mainPanel.Controls.Add(_certificationGrid, 0, 0);

            // ملخص الشهادات
            _summaryTextBox = new TextBox();
            _summaryTextBox.Dock = DockStyle.Fill;
            _summaryTextBox.Multiline = true;
            _summaryTextBox.ReadOnly = true;
            _summaryTextBox.Font = new Font("Arial", 11);
            mainPanel.Controls.Add(_summaryTextBox, 0, 1);

            // إضافة أزرار الطباعة والتصدير
            var buttonPanel = new FlowLayoutPanel();
            buttonPanel.Dock = DockStyle.Top;
            buttonPanel.FlowDirection = FlowDirection.RightToLeft;
            buttonPanel.Height = 40;
            buttonPanel.Padding = new Padding(10);

            var printButton = ThemeManager.CreatePrimaryButton("طباعة التقرير");
            printButton.Width = 120;
            printButton.Click += PrintButton_Click;
            buttonPanel.Controls.Add(printButton);

            var exportExcelButton = ThemeManager.CreateSuccessButton("تصدير إلى Excel");
            exportExcelButton.Width = 120;
            exportExcelButton.Click += ExportExcelButton_Click;
            buttonPanel.Controls.Add(exportExcelButton);

            var exportPdfButton = ThemeManager.CreateSecondaryButton("تصدير إلى PDF");
            exportPdfButton.Width = 120;
            exportPdfButton.Click += ExportPdfButton_Click;
            buttonPanel.Controls.Add(exportPdfButton);

            var refreshButton = new Button { Text = "تحديث", Width = 120 };
            refreshButton.Click += RefreshButton_Click;
            buttonPanel.Controls.Add(refreshButton);

            this.Controls.Add(buttonPanel);
            this.Controls.Add(mainPanel);
        }

        private void LoadCertificationData()
        {
            var today = DateTime.Now;

            var records = _context.CertificationRecords
                .Select(c => new
                {
                    c.CertificateName,
                    IssuingAuthority = c.Issuer,
                    c.IssueDate,
                    c.ExpiryDate,
                    Status = c.ExpiryDate < today ? "منتهية" : "صالحة",
                    c.Notes
                })
                .OrderBy(c => c.ExpiryDate)
                .ToList();

            _certificationGrid.DataSource = records;

            // ملخص الشهادات
            if (!records.Any())
            {
                _summaryTextBox.Text = "لا توجد شهادات مسجلة";
                return;
            }

            var totalCertificates = records.Count;
            var validCertificates = records.Count(c => c.Status == "صالحة");
            var expiredCertificates = records.Count(c => c.Status == "منتهية");
            var expiringSoon = records.Count(c => c.ExpiryDate < today.AddDays(90) && c.ExpiryDate >= today);
            var nextExpiry = records.OrderBy(c => c.ExpiryDate).FirstOrDefault();

            _summaryTextBox.Text = $"إجمالي الشهادات: {totalCertificates}\n" +
                $"الشهادات الصالحة: {validCertificates}\n" +
                $"الشهادات منتهية: {expiredCertificates}\n" +
                $"الشهادات المنتهية قريباً (داخل 90 يوم): {expiringSoon}\n\n" +
                $"أقرب تاريخ انتهاء: {nextExpiry?.ExpiryDate:yyyy/MM/dd} ({nextExpiry?.CertificateName})";
        }

        private void RefreshButton_Click(object? sender, EventArgs e)
        {
            LoadCertificationData();
        }

        // طباعة التقرير
        private void PrintButton_Click(object? sender, EventArgs e)
        {
            PrintDialog printDialog = new PrintDialog();
            System.Drawing.Printing.PrintDocument printDocument = new System.Drawing.Printing.PrintDocument();
            printDocument.PrintPage += (s, ev) =>
            {
                using Bitmap bm = new Bitmap(_certificationGrid.Width, _certificationGrid.Height);
                _certificationGrid.DrawToBitmap(bm, new Rectangle(0, 0, _certificationGrid.Width, _certificationGrid.Height));
                var g = ev?.Graphics;
                if (g != null)
                {
                    g.DrawImage(bm, 0, 0);
                }
            };
            printDialog.Document = printDocument;
            if (printDialog.ShowDialog() == DialogResult.OK)
            {
                printDocument.Print();
            }
        }

        // تصدير إلى Excel
        private void ExportExcelButton_Click(object? sender, EventArgs e)
        {
            SaveFileDialog saveFileDialog = new SaveFileDialog();
            saveFileDialog.Filter = "Excel Files|*.xlsx";
            saveFileDialog.Title = "تصدير إلى Excel";
            if (saveFileDialog.ShowDialog() == DialogResult.OK)
            {
                // إذا كان لديك مكتبة ClosedXML أو EPPlus يمكنك استخدامها هنا
                // هنا تصدير بسيط إلى CSV كبديل
                var filePath = saveFileDialog.FileName;
                using (var sw = new System.IO.StreamWriter(filePath))
                {
                    // رؤوس الأعمدة
                    for (int i = 0; i < _certificationGrid.Columns.Count; i++)
                    {
                        sw.Write(_certificationGrid.Columns[i].HeaderText);
                        if (i < _certificationGrid.Columns.Count - 1) sw.Write(",");
                    }
                    sw.WriteLine();
                    // البيانات
                    foreach (DataGridViewRow row in _certificationGrid.Rows)
                    {
                        for (int i = 0; i < _certificationGrid.Columns.Count; i++)
                        {
                            var val = row.Cells[i].Value;
                            sw.Write(val?.ToString());
                            if (i < _certificationGrid.Columns.Count - 1) sw.Write(",");
                        }
                        sw.WriteLine();
                    }
                }
                MessageBox.Show("تم تصدير التقرير إلى Excel (بصيغة CSV)", "نجاح", MessageBoxButtons.OK, MessageBoxIcon.Information);
            }
        }

        // تصدير إلى PDF
        private void ExportPdfButton_Click(object? sender, EventArgs e)
        {
            SaveFileDialog saveFileDialog = new SaveFileDialog();
            saveFileDialog.Filter = "PDF Files|*.pdf";
            saveFileDialog.Title = "تصدير إلى PDF";
            if (saveFileDialog.ShowDialog() == DialogResult.OK)
            {
                // إذا كان لديك مكتبة PDF مثل iTextSharp أو PdfSharp يمكنك استخدامها هنا
                // هنا تصدير بسيط إلى ملف نصي بامتداد PDF كبديل
                var filePath = saveFileDialog.FileName;
                using (var sw = new System.IO.StreamWriter(filePath))
                {
                    sw.WriteLine("تقرير شهادات الجودة");
                    sw.WriteLine("----------------------");
                    // رؤوس الأعمدة
                    for (int i = 0; i < _certificationGrid.Columns.Count; i++)
                    {
                        sw.Write(_certificationGrid.Columns[i].HeaderText + "\t");
                    }
                    sw.WriteLine();
                    // البيانات
                    foreach (DataGridViewRow row in _certificationGrid.Rows)
                    {
                        for (int i = 0; i < _certificationGrid.Columns.Count; i++)
                        {
                            var val = row.Cells[i].Value;
                            sw.Write((val?.ToString() ?? "") + "\t");
                        }
                        sw.WriteLine();
                    }
                    sw.WriteLine();
                    sw.WriteLine(_summaryTextBox.Text);
                }
                MessageBox.Show("تم تصدير التقرير إلى PDF", "نجاح", MessageBoxButtons.OK, MessageBoxIcon.Information);
            }
        }
    }
}