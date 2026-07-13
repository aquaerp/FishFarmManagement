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
    public partial class MortalityReportForm : Form
    {
        private readonly FishFarmContext _context;
        private ComboBox _periodComboBox = null!;
        private ComboBox _yearComboBox = null!;
        private DateTimePicker _startDatePicker = null!;
        private DateTimePicker _endDatePicker = null!;
        private DataGridView _mortalityGrid = null!;
        private TextBox _summaryTextBox = null!;
        private Button _generateButton = null!;
        private ChartControl _mortalityChart = null!;

        public MortalityReportForm(FishFarmContext context)
        {
            _context = context;
            InitializeComponent();
            LoadYears();
            LoadMortalityData();
        }

        private void InitializeComponent()
        {
            this.Text = "تقرير النفوق";
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
            _mortalityGrid = new DataGridView();
            _mortalityGrid.Dock = DockStyle.Fill;
            _mortalityGrid.AutoGenerateColumns = false;
            _mortalityGrid.ReadOnly = true;
            _mortalityGrid.AllowUserToAddRows = false;

            _mortalityGrid.Columns.Add(new DataGridViewTextBoxColumn { DataPropertyName = "Date", HeaderText = "التاريخ", Width = 120, DefaultCellStyle = new DataGridViewCellStyle { Format = "yyyy/MM/dd" } });
            _mortalityGrid.Columns.Add(new DataGridViewTextBoxColumn { DataPropertyName = "PondName", HeaderText = "الحوض", Width = 120 });
            _mortalityGrid.Columns.Add(new DataGridViewTextBoxColumn { DataPropertyName = "DeadFishCount", HeaderText = "عدد الأسماك الميتة", Width = 150 });
            _mortalityGrid.Columns.Add(new DataGridViewTextBoxColumn { DataPropertyName = "CauseDescription", HeaderText = "السبب", Width = 200 });
            _mortalityGrid.Columns.Add(new DataGridViewTextBoxColumn { DataPropertyName = "Treatment", HeaderText = "العلاج المطبق", Width = 200 });

            contentPanel.Controls.Add(_mortalityGrid, 0, 0);

            // الرسم البياني
            _mortalityChart = new ChartControl();
            _mortalityChart.Dock = DockStyle.Fill;
            contentPanel.Controls.Add(_mortalityChart, 1, 0);

            mainPanel.Controls.Add(contentPanel, 0, 1);

            // ملخص النفوق
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

        private void LoadMortalityData()
        {
            GenerateButton_Click(null, EventArgs.Empty);
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
            var selectedYear = int.Parse(_yearComboBox.SelectedItem?.ToString() ?? DateTime.Now.Year.ToString());
            var selectedPeriod = _periodComboBox.SelectedItem?.ToString() ?? "العام الكامل";

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

            var records = _context.MortalityRecords
                .Include(m => m.Cycle)
                    .ThenInclude(c => c.ProductionCyclePonds)
                        .ThenInclude(pcp => pcp.Pond)
                .Where(m => m.Date >= startDate && m.Date <= endDate)
                .OrderByDescending(m => m.Date)
                .Select(m => new
                {
                    m.Date,
                    PondName = (m.Cycle.ProductionCyclePonds.Select(pcp => pcp.Pond.Name).FirstOrDefault() ?? "غير محدد"),
                    m.DeadFishCount,
                    m.CauseDescription,
                    m.Treatment
                })
                .ToList();

            _mortalityGrid.DataSource = records;

            // ملخص النفوق
            if (!records.Any())
            {
                _summaryTextBox.Text = "لا توجد بيانات نفوق متاحة في الفترة المحددة";
                return;
            }

            var totalDeadFish = records.Sum(r => r.DeadFishCount);
            var avgDailyMortality = records.Average(r => r.DeadFishCount);
            var topCause = records.GroupBy(r => r.CauseDescription)
                                 .OrderByDescending(g => g.Sum(r => r.DeadFishCount))
                                 .FirstOrDefault();

            var pondMortality = records.GroupBy(r => r.PondName)
                                      .Select(g => new { Pond = g.Key, Count = g.Sum(r => r.DeadFishCount) })
                                      .OrderByDescending(g => g.Count)
                                      .ToList();

            _summaryTextBox.Text = $"ملخص النفوق ({startDate:yyyy/MM/dd} - {endDate:yyyy/MM/dd}):\n\n" +
                $"إجمالي عدد الأسماك الميتة: {totalDeadFish} سمكة\n" +
                $"متوسط النفوق اليومي: {avgDailyMortality:F1} سمكة\n" +
                $"السبب الأكثر شيوعاً: {topCause?.Key ?? "غير محدد"} ({topCause?.Sum(r => r.DeadFishCount) ?? 0} سمكة)\n\n" +
                $"الأحواض الأكثر تأثراً بالنفوق:\n";

            foreach (var pond in pondMortality.Take(5))
            {
                _summaryTextBox.Text += $"- {pond.Pond}: {pond.Count} سمكة\n";
            }

            // تحديث الرسم البياني
            UpdateMortalityChart(records.Cast<dynamic>().ToList());
        }

        private void UpdateMortalityChart(System.Collections.Generic.List<dynamic> records)
        {
            _mortalityChart.ClearSeries();
            var dailyMortality = records.GroupBy(r => r.Date)
                                       .Select(g => new { Date = g.Key, Count = g.Sum(r => (double)r.DeadFishCount) })
                                       .OrderBy(g => g.Date)
                                       .ToList();
            var values = dailyMortality.Select(x => x.Count).ToArray();
            var labels = dailyMortality.Select(x => x.Date.ToString("MM/dd")).Cast<string>().ToArray();
            
            _mortalityChart.SetCustomLabels(labels);
            _mortalityChart.AddSeries("النفوق اليومي", values);
        }

        // طباعة التقرير
        private void PrintButton_Click(object? sender, EventArgs e)
        {
            PrintDialog printDialog = new PrintDialog();
            System.Drawing.Printing.PrintDocument printDocument = new System.Drawing.Printing.PrintDocument();
            printDocument.PrintPage += (s, ev) =>
            {
                Bitmap bm = new Bitmap(this.Width, this.Height);
                this.DrawToBitmap(bm, new Rectangle(0, 0, this.Width, this.Height));
                ev.Graphics?.DrawImage(bm, 0, 0);
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
                    for (int i = 0; i < _mortalityGrid.Columns.Count; i++)
                    {
                        sw.Write(_mortalityGrid.Columns[i].HeaderText);
                        if (i < _mortalityGrid.Columns.Count - 1) sw.Write(",");
                    }
                    sw.WriteLine();

                    // البيانات
                    foreach (DataGridViewRow row in _mortalityGrid.Rows)
                    {
                        for (int i = 0; i < _mortalityGrid.Columns.Count; i++)
                        {
                            sw.Write(row.Cells[i].Value);
                            if (i < _mortalityGrid.Columns.Count - 1) sw.Write(",");
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
                    sw.WriteLine("تقرير النفوق");
                    sw.WriteLine("----------------------");
                    sw.WriteLine($"التاريخ: {_startDatePicker.Value:yyyy/MM/dd} - {_endDatePicker.Value:yyyy/MM/dd}");
                    sw.WriteLine();

                    // رؤوس الأعمدة
                    for (int i = 0; i < _mortalityGrid.Columns.Count; i++)
                    {
                        sw.Write(_mortalityGrid.Columns[i].HeaderText + "	");
                    }
                    sw.WriteLine();

                    // البيانات
                    foreach (DataGridViewRow row in _mortalityGrid.Rows)
                    {
                        for (int i = 0; i < _mortalityGrid.Columns.Count; i++)
                        {
                            sw.Write(row.Cells[i].Value + "	");
                        }
                        sw.WriteLine();
                    }

                    sw.WriteLine();
                    sw.WriteLine(_summaryTextBox.Text);
                }
                MessageBox.Show("تم تصدير التقرير إلى PDF (بصيغة نصية)", "نجاح", MessageBoxButtons.OK, MessageBoxIcon.Information);
            }
        }
    }
}
