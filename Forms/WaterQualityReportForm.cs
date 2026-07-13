using System;
using System.Drawing;
using System.Linq;
using System.Windows.Forms;
using Microsoft.EntityFrameworkCore;
using FishFarmManager.Data;
using FishFarmManager.Services;

namespace FishFarmManager.Forms
{
    public class WaterQualityReportForm : Form
    {
        private readonly FishFarmContext _context;
        private DataGridView _waterGrid = null!;
        private TextBox _summaryTextBox = null!;

        public WaterQualityReportForm(FishFarmContext context)
        {
            _context = context;
            InitializeComponent();
            LoadWaterQualityData();
        }

        private void InitializeComponent()
        {
            this.Text = "تقرير جودة المياه";
            this.Size = new Size(1000, 650);
            this.StartPosition = FormStartPosition.CenterScreen;
            this.RightToLeft = RightToLeft.Yes;
            this.RightToLeftLayout = true;
            try { this.Font = new Font("Cairo", 10F, FontStyle.Regular); } catch { }

            var mainPanel = new TableLayoutPanel();
            mainPanel.Dock = DockStyle.Fill;
            mainPanel.RowCount = 2;
            mainPanel.RowStyles.Add(new RowStyle(SizeType.Percent, 70F));
            mainPanel.RowStyles.Add(new RowStyle(SizeType.Percent, 30F));

            // جدول جود�� المياه
            _waterGrid = new DataGridView();
            _waterGrid.Dock = DockStyle.Fill;
            _waterGrid.AutoGenerateColumns = false;
            _waterGrid.ReadOnly = true;
            _waterGrid.AllowUserToAddRows = false;

            _waterGrid.Columns.Add(new DataGridViewTextBoxColumn { DataPropertyName = "PondName", HeaderText = "الحوض", Width = 120 });
            _waterGrid.Columns.Add(new DataGridViewTextBoxColumn { DataPropertyName = "MeasurementDate", HeaderText = "تاريخ القياس", Width = 120, DefaultCellStyle = new DataGridViewCellStyle { Format = "yyyy/MM/dd" } });
            _waterGrid.Columns.Add(new DataGridViewTextBoxColumn { DataPropertyName = "Temperature", HeaderText = "درجة الحرارة (°C)", Width = 120 });
            _waterGrid.Columns.Add(new DataGridViewTextBoxColumn { DataPropertyName = "PH", HeaderText = "الرقم الهيدروجيني (pH)", Width = 120 });
            _waterGrid.Columns.Add(new DataGridViewTextBoxColumn { DataPropertyName = "Oxygen", HeaderText = "الأكسجين (mg/L)", Width = 120 });
            _waterGrid.Columns.Add(new DataGridViewTextBoxColumn { DataPropertyName = "Ammonia", HeaderText = "الأمونيا (mg/L)", Width = 120 });
            _waterGrid.Columns.Add(new DataGridViewTextBoxColumn { DataPropertyName = "Notes", HeaderText = "ملاحظات", Width = 200 });

            mainPanel.Controls.Add(_waterGrid, 0, 0);

            // ملخص جودة المياه
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

            this.Controls.Add(buttonPanel);
            this.Controls.Add(mainPanel);
        }

        // طباعة التقرير
        private void PrintButton_Click(object? sender, EventArgs e)
        {
            PrintDialog printDialog = new PrintDialog();
            System.Drawing.Printing.PrintDocument printDocument = new System.Drawing.Printing.PrintDocument();
            printDocument.PrintPage += (s, ev) =>
            {
                if (_waterGrid == null) return;
                using (Bitmap bm = new Bitmap(_waterGrid.Width, _waterGrid.Height))
                {
                    _waterGrid.DrawToBitmap(bm, new Rectangle(0, 0, _waterGrid.Width, _waterGrid.Height));
                    ev.Graphics?.DrawImage(bm, 0, 0);
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
                    for (int i = 0; i < _waterGrid.Columns.Count; i++)
                    {
                        sw.Write(_waterGrid.Columns[i].HeaderText);
                        if (i < _waterGrid.Columns.Count - 1) sw.Write(",");
                    }
                    sw.WriteLine();
                    // البيانات
                    foreach (DataGridViewRow row in _waterGrid.Rows)
                    {
                        for (int i = 0; i < _waterGrid.Columns.Count; i++)
                        {
                            var val = row.Cells[i].Value?.ToString() ?? string.Empty;
                            sw.Write(val);
                            if (i < _waterGrid.Columns.Count - 1) sw.Write(",");
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
                    sw.WriteLine("تقرير جودة المياه");
                    sw.WriteLine("----------------------");
                    // رؤوس الأعمدة
                    for (int i = 0; i < _waterGrid.Columns.Count; i++)
                    {
                        sw.Write(_waterGrid.Columns[i].HeaderText + "\t");
                    }
                    sw.WriteLine();
                    // البيانات
                    foreach (DataGridViewRow row in _waterGrid.Rows)
                    {
                        for (int i = 0; i < _waterGrid.Columns.Count; i++)
                        {
                            var val = row.Cells[i].Value?.ToString() ?? string.Empty;
                            sw.Write(val + "\t");
                        }
                        sw.WriteLine();
                    }
                    sw.WriteLine();
                    sw.WriteLine(_summaryTextBox.Text);
                }
                MessageBox.Show("تم تصدير التقرير إلى PDF (بصيغة نصية)", "نجاح", MessageBoxButtons.OK, MessageBoxIcon.Information);
            }
        }

        private void LoadWaterQualityData()
        {
#pragma warning disable CS8602
            var records = _context.WaterQualityRecords
                .Include(w => w.Cycle)
                    .ThenInclude(c => c.ProductionCyclePonds)
                        .ThenInclude(pcp => pcp.Pond)
                .OrderByDescending(w => w.MeasurementDate)
                .Select(w => new
                {
                    PondName = w.Cycle != null && w.Cycle.ProductionCyclePonds != null 
                        ? string.Join(", ", w.Cycle.ProductionCyclePonds.Select(pcp => pcp.Pond.Name)) 
                        : string.Empty,
                    w.MeasurementDate,
                    Temperature = w.Temperature,
                    PH = w.pH,
                    Oxygen = w.DissolvedOxygen,
                    Ammonia = w.Ammonia,
                    w.Notes
                })
                .ToList();
#pragma warning restore CS8602
            _waterGrid.DataSource = records;

            // ملخص جودة المياه
            if (!records.Any())
            {
                _summaryTextBox.Text = "لا توجد بيانات جودة مياه متاحة";
                return;
            }
            var avgTemp = records.Average(r => r.Temperature);
            var avgPH = records.Average(r => r.PH);
            var avgOxygen = records.Average(r => r.Oxygen);
            var avgAmmonia = records.Average(r => r.Ammonia);

            _summaryTextBox.Text = $"ملخص جودة المياه:\n\n" +
                $"متوسط درجة الحرارة: {avgTemp:F1}°س\n" +
                $"متوسط الأس الهيدروجيني: {avgPH:F1}\n" +
                $"متوسط الأكسجين: {avgOxygen:F1} ملجم/لتر\n" +
                $"متوسط الأمونيا: {avgAmmonia:F2} ملجم/لتر";
        }
    }
}