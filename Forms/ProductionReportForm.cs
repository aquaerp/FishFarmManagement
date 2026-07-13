using System;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Windows.Forms;
using Microsoft.EntityFrameworkCore;
using FishFarmManager.Data;
using FishFarmManager.Services;
using FishFarmManager.Controls;

namespace FishFarmManager.Forms
{
    public class ProductionReportForm : Form
    {
        private readonly FishFarmContext _context;
        private ComboBox _periodComboBox = null!;
        private DataGridView _productionGrid = null!;
        private ChartControl _productionChart = null!;
        private TextBox _summaryTextBox = null!;
        private ComboBox _yearComboBox = null!;
        private DateTimePicker _startDatePicker = null!;
        private DateTimePicker _endDatePicker = null!;
        private Button _generateButton = null!;

        public ProductionReportForm(FishFarmContext context)
        {
            _context = context;
            InitializeComponent();
            LoadYears();
            LoadProductionData();
        }

        private void InitializeComponent()
        {
            this.Text = "تقرير الإنتاج";
            this.Size = new Size(1200, 800);
            this.StartPosition = FormStartPosition.CenterScreen;
            this.RightToLeft = RightToLeft.Yes;
            this.RightToLeftLayout = true;
            try { this.Font = new Font("Cairo", 10F, FontStyle.Regular); } catch { }

            // إنشاء عناصر التحكم الرئيسية
            var mainPanel = new TableLayoutPanel();
            mainPanel.Dock = DockStyle.Fill;
            mainPanel.RowCount = 3;
            mainPanel.RowStyles.Add(new RowStyle(SizeType.AutoSize));
            mainPanel.RowStyles.Add(new RowStyle(SizeType.Percent, 70F));
            mainPanel.RowStyles.Add(new RowStyle(SizeType.Percent, 30F));

            // لوحة التحكم
            var controlPanel = CreateControlPanel();
            mainPanel.Controls.Add(controlPanel, 0, 0);

            // لوحة الجدول والرسم البياني
            var contentPanel = new TableLayoutPanel();
            contentPanel.Dock = DockStyle.Fill;
            contentPanel.ColumnCount = 2;
            contentPanel.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 60F));
            contentPanel.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 40F));

            // الجدول
            _productionGrid = new DataGridView();
            _productionGrid.Dock = DockStyle.Fill;
            _productionGrid.AutoGenerateColumns = false;
            _productionGrid.ReadOnly = true;
            _productionGrid.AllowUserToAddRows = false;

            _productionGrid.Columns.Add(new DataGridViewTextBoxColumn { DataPropertyName = "Name", HeaderText = "اسم الدورة", Width = 180 });
            _productionGrid.Columns.Add(new DataGridViewTextBoxColumn { DataPropertyName = "PondName", HeaderText = "الحوض", Width = 120 });
            _productionGrid.Columns.Add(new DataGridViewTextBoxColumn { DataPropertyName = "StartDate", HeaderText = "تاريخ البدء", Width = 120, DefaultCellStyle = new DataGridViewCellStyle { Format = "yyyy/MM/dd" } });
            _productionGrid.Columns.Add(new DataGridViewTextBoxColumn { DataPropertyName = "EndDate", HeaderText = "تاريخ النهاية", Width = 120, DefaultCellStyle = new DataGridViewCellStyle { Format = "yyyy/MM/dd" } });
            _productionGrid.Columns.Add(new DataGridViewTextBoxColumn { DataPropertyName = "SurvivalRate", HeaderText = "معدل البقاء %", Width = 120, DefaultCellStyle = new DataGridViewCellStyle { Format = "F1" } });
            _productionGrid.Columns.Add(new DataGridViewTextBoxColumn { DataPropertyName = "TotalHarvestWeight", HeaderText = "إجمالي الوزن (كجم)", Width = 150, DefaultCellStyle = new DataGridViewCellStyle { Format = "F2" } });
            _productionGrid.Columns.Add(new DataGridViewTextBoxColumn { DataPropertyName = "FCR", HeaderText = "FCR", Width = 100, DefaultCellStyle = new DataGridViewCellStyle { Format = "F2" } });
            _productionGrid.Columns.Add(new DataGridViewTextBoxColumn { DataPropertyName = "ADG", HeaderText = "ADG", Width = 100, DefaultCellStyle = new DataGridViewCellStyle { Format = "F2" } });

            contentPanel.Controls.Add(_productionGrid, 0, 0);

            // الرسم البياني
            _productionChart = new ChartControl();
            _productionChart.Dock = DockStyle.Fill;
            contentPanel.Controls.Add(_productionChart, 1, 0);

            mainPanel.Controls.Add(contentPanel, 0, 1);

            // ملخص الإنتاج
            _summaryTextBox = new TextBox();
            _summaryTextBox.Dock = DockStyle.Fill;
            _summaryTextBox.Multiline = true;
            _summaryTextBox.ReadOnly = true;
            _summaryTextBox.Font = new Font("Arial", 11);
            mainPanel.Controls.Add(_summaryTextBox, 0, 2);

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

        private Panel CreateControlPanel()
        {
            var panel = new Panel();
            panel.Height = 120;
            panel.Dock = DockStyle.Top;
            panel.Padding = new Padding(10);

            var y = 10;
            var spacing = 30;
            var labelWidth = 100;
            var controlWidth = 150;

            // الفترة
            var periodLabel = new Label { Text = "الفترة:", Location = new Point(10, y), Size = new Size(labelWidth, 20) };
            _periodComboBox = new ComboBox { Location = new Point(120, y), Size = new Size(controlWidth, 20) };
            _periodComboBox.Items.AddRange(new object[] { "يوم", "أسبوع", "شهر", "سنة", "مخصص" });
            _periodComboBox.SelectedIndex = 3; // شهر
            _periodComboBox.SelectedIndexChanged += PeriodComboBox_SelectedIndexChanged;
            panel.Controls.Add(periodLabel);
            panel.Controls.Add(_periodComboBox);

            // السنة
            var yearLabel = new Label { Text = "السنة:", Location = new Point(300, y), Size = new Size(labelWidth, 20) };
            _yearComboBox = new ComboBox { Location = new Point(410, y), Size = new Size(controlWidth, 20) };
            _yearComboBox.SelectedIndexChanged += YearComboBox_SelectedIndexChanged;
            panel.Controls.Add(yearLabel);
            panel.Controls.Add(_yearComboBox);

            y += spacing;

            // التاريخ المخصص
            var startDateLabel = new Label { Text = "من تاريخ:", Location = new Point(10, y), Size = new Size(labelWidth, 20) };
            _startDatePicker = new DateTimePicker { Location = new Point(120, y), Size = new Size(controlWidth, 20) };
            _startDatePicker.Value = DateTime.Now.AddDays(-30); // آخر 30 يوم افتراضياً
            panel.Controls.Add(startDateLabel);
            panel.Controls.Add(_startDatePicker);

            var endDateLabel = new Label { Text = "إلى تاريخ:", Location = new Point(300, y), Size = new Size(labelWidth, 20) };
            _endDatePicker = new DateTimePicker { Location = new Point(410, y), Size = new Size(controlWidth, 20) };
            _endDatePicker.Value = DateTime.Now;
            panel.Controls.Add(endDateLabel);
            panel.Controls.Add(_endDatePicker);

            y += spacing;

            // زر التوليد
            _generateButton = ThemeManager.CreatePrimaryButton("توليد التقرير");
            _generateButton.Location = new Point(10, y);
            _generateButton.Size = new Size(120, 30);
            _generateButton.Click += GenerateButton_Click;
            panel.Controls.Add(_generateButton);

            return panel;
        }

        private void LoadYears()
        {
            var currentYear = DateTime.Now.Year;
            var years = Enumerable.Range(currentYear - 5, 6).Select(y => y.ToString()).ToList();
            _yearComboBox.Items.AddRange(years.ToArray());
            _yearComboBox.SelectedItem = currentYear.ToString();
        }

        private void LoadProductionData()
        {
            GenerateButton_Click(null!, EventArgs.Empty);
        }

        private void PeriodComboBox_SelectedIndexChanged(object? sender, EventArgs e)
        {
            UpdateDatePickers();
        }

        private void YearComboBox_SelectedIndexChanged(object? sender, EventArgs e)
        {
            UpdateDatePickers();
        }

        private void UpdateDatePickers()
        {
            var yearText = _yearComboBox.SelectedItem?.ToString();
            if (!int.TryParse(yearText, out var selectedYear))
            {
                selectedYear = DateTime.Now.Year;
            }
            var selectedPeriod = _periodComboBox.SelectedItem?.ToString() ?? "مخصص";

            if (selectedPeriod != "مخصص")
            {
                DateTime startDate, endDate;
                var today = DateTime.Now;

                switch (selectedPeriod)
                {
                    case "يوم":
                        startDate = today;
                        endDate = today;
                        break;
                    case "أسبوع":
                        startDate = today.AddDays(-(int)today.DayOfWeek);
                        endDate = startDate.AddDays(6);
                        break;
                    case "شهر":
                        startDate = new DateTime(selectedYear, today.Month, 1);
                        endDate = new DateTime(selectedYear, today.Month, DateTime.DaysInMonth(selectedYear, today.Month));
                        break;
                    case "سنة":
                        startDate = new DateTime(selectedYear, 1, 1);
                        endDate = new DateTime(selectedYear, 12, 31);
                        break;
                    default:
                        startDate = today.AddDays(-30);
                        endDate = today;
                        break;
                }

                _startDatePicker.Value = startDate;
                _endDatePicker.Value = endDate;
            }
        }

        private void GenerateButton_Click(object? sender, EventArgs e)
        {
            var startDate = _startDatePicker.Value;
            var endDate = _endDatePicker.Value;

            var records = _context.ProductionCycles
                .Include(c => c.ProductionCyclePonds)
                    .ThenInclude(pcp => pcp.Pond)
                .Where(c => c.EndDate.HasValue && c.EndDate.Value >= startDate && c.EndDate.Value <= endDate)
                .OrderByDescending(c => c.EndDate)
                .Select(c => new
                {
                    c.Name,
                    PondName = string.Join(", ", c.ProductionCyclePonds.Select(pcp => pcp.Pond.Name)),
                    c.StartDate,
                    c.EndDate,
                    c.SurvivalRate,
                    c.TotalHarvestWeight,
                    c.FCR,
                    c.ADG
                })
                .ToList();

            _productionGrid.DataSource = records;

            // ملخص الإنتاج
            if (!records.Any())
            {
                _summaryTextBox.Text = "لا توجد بيانات إنتاج متاحة في الفترة المحددة";
                return;
            }

            var totalCycles = records.Count;
            var totalProduction = records.Sum(r => r.TotalHarvestWeight ?? 0);
            var avgSurvivalRate = records.Average(r => r.SurvivalRate ?? 0);
            var avgFCR = records.Average(r => r.FCR ?? 0);
            var avgADG = records.Average(r => r.ADG ?? 0);
            var avgProduction = totalProduction / totalCycles;

            var bestCycle = records.OrderByDescending(r => r.SurvivalRate).FirstOrDefault();
            var mostProductiveCycle = records.OrderByDescending(r => r.TotalHarvestWeight).FirstOrDefault();

            _summaryTextBox.Text = $"ملخص الإنتاج ({startDate:yyyy/MM/dd} - {endDate:yyyy/MM/dd}):\n\n" +
                $"إجمالي عدد الدورات المكتملة: {totalCycles}\n" +
                $"إجمالي الإنتاج: {totalProduction:F2} كجم\n" +
                $"متوسط الإنتاج للدورة: {avgProduction:F2} كجم\n" +
                $"متوسط معدل البقاء: {avgSurvivalRate:F1}%\n" +
                $"متوسط معدل التحويل الغذائي (FCR): {avgFCR:F2}\n" +
                $"متوسط النمو اليومي (ADG): {avgADG:F2} جم/يوم\n\n" +
                $"أفضل دورة من حيث معدل البقاء: {bestCycle?.Name} ({bestCycle?.SurvivalRate:F1}%)\n" +
                $"أكثر دورة إنتاجاً: {mostProductiveCycle?.Name} ({mostProductiveCycle?.TotalHarvestWeight:F2} كجم)";
        }

        // طباعة التقرير
        private void PrintButton_Click(object? sender, EventArgs e)
        {
            PrintDialog printDialog = new PrintDialog();
            System.Drawing.Printing.PrintDocument printDocument = new System.Drawing.Printing.PrintDocument();
            printDocument.PrintPage += (s, ev) =>
            {
                using Bitmap bm = new Bitmap(_productionGrid.Width, _productionGrid.Height);
                _productionGrid.DrawToBitmap(bm, new Rectangle(0, 0, _productionGrid.Width, _productionGrid.Height));
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
                var filePath = saveFileDialog.FileName;
                using (var sw = new System.IO.StreamWriter(filePath))
                {
                    // رؤوس الأعمدة
                    for (int i = 0; i < _productionGrid.Columns.Count; i++)
                    {
                        sw.Write(_productionGrid.Columns[i].HeaderText);
                        if (i < _productionGrid.Columns.Count - 1) sw.Write(",");
                    }
                    sw.WriteLine();
                    // البيانات
                    foreach (DataGridViewRow row in _productionGrid.Rows)
                    {
                        for (int i = 0; i < _productionGrid.Columns.Count; i++)
                        {
                            sw.Write(row.Cells[i].Value);
                            if (i < _productionGrid.Columns.Count - 1) sw.Write(",");
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
                var filePath = saveFileDialog.FileName;
                using (var sw = new System.IO.StreamWriter(filePath))
                {
                    sw.WriteLine("تقرير الإنتاج");
                    sw.WriteLine("----------------------");
                    sw.WriteLine(_summaryTextBox.Text);
                    sw.WriteLine();
                    sw.WriteLine("البيانات التفصيلية:");
                    sw.WriteLine("----------------------");
                    // رؤوس الأعمدة
                    for (int i = 0; i < _productionGrid.Columns.Count; i++)
                    {
                        sw.Write(_productionGrid.Columns[i].HeaderText + "	");
                    }
                    sw.WriteLine();
                    // البيانات
                    foreach (DataGridViewRow row in _productionGrid.Rows)
               {
                        for (int i = 0; i < _productionGrid.Columns.Count; i++)
                        {
                            sw.Write(row.Cells[i].Value + "	");
                                                sw.WriteLine();
                    }
                                       sw.WriteLine();
                    }
                }
                MessageBox.Show("تم تصدير التقرير", "نجاح", MessageBoxButtons.OK, MessageBoxIcon.Information);
            }
        }
    }
}