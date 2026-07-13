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
    public class TreatmentReportForm : Form
    {
        private readonly FishFarmContext _context;
        private ComboBox _periodComboBox = null!;
        private ComboBox _yearComboBox = null!;
        private DateTimePicker _startDatePicker = null!;
        private DateTimePicker _endDatePicker = null!;
        private DataGridView _treatmentGrid = null!;
        private TextBox _summaryTextBox = null!;
        private Button _generateButton = null!;
        private ChartControl _treatmentChart = null!;

        public TreatmentReportForm(FishFarmContext context)
        {
            _context = context;
            InitializeComponent();
            LoadYears();
            LoadTreatmentData();
        }

        private void InitializeComponent()
        {
            this.Text = "تقرير المعالجات والعلاجات";
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
            _treatmentGrid = new DataGridView();
            _treatmentGrid.Dock = DockStyle.Fill;
            _treatmentGrid.AutoGenerateColumns = false;
            _treatmentGrid.ReadOnly = true;
            _treatmentGrid.AllowUserToAddRows = false;

            _treatmentGrid.Columns.Add(new DataGridViewTextBoxColumn { DataPropertyName = "Date", HeaderText = "التاريخ", Width = 120, DefaultCellStyle = new DataGridViewCellStyle { Format = "yyyy/MM/dd" } });
            _treatmentGrid.Columns.Add(new DataGridViewTextBoxColumn { DataPropertyName = "PondName", HeaderText = "الحوض", Width = 120 });
            _treatmentGrid.Columns.Add(new DataGridViewTextBoxColumn { DataPropertyName = "TreatmentName", HeaderText = "اسم العلاج", Width = 180 });
            _treatmentGrid.Columns.Add(new DataGridViewTextBoxColumn { DataPropertyName = "Dosage", HeaderText = "الجرعة", Width = 120 });
            _treatmentGrid.Columns.Add(new DataGridViewTextBoxColumn { DataPropertyName = "WithdrawalPeriod", HeaderText = "فترة السحب", Width = 120 });
            _treatmentGrid.Columns.Add(new DataGridViewTextBoxColumn { DataPropertyName = "Cost", HeaderText = "التكلفة", Width = 120, DefaultCellStyle = new DataGridViewCellStyle { Format = "C2", Alignment = DataGridViewContentAlignment.MiddleRight } });

            contentPanel.Controls.Add(_treatmentGrid, 0, 0);

            // الرسم البياني
            _treatmentChart = new ChartControl();
            _treatmentChart.Dock = DockStyle.Fill;
            contentPanel.Controls.Add(_treatmentChart, 1, 0);

            mainPanel.Controls.Add(contentPanel, 0, 1);

            // ملخص المعالجات
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

        private void LoadTreatmentData()
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

#pragma warning disable CS8602
            var records = _context.TreatmentRecords
                .Include(t => t.Cycle)
                    .ThenInclude(c => c.ProductionCyclePonds)
                        .ThenInclude(pcp => pcp.Pond)
                .Where(t => t.Date >= startDate && t.Date <= endDate)
                .OrderByDescending(t => t.Date)
                .Select(t => new
                {
                    t.Date,
                    PondName = t.Cycle != null && t.Cycle.ProductionCyclePonds != null && t.Cycle.ProductionCyclePonds.Any()
                        ? t.Cycle.ProductionCyclePonds.Select(pcp => pcp.Pond.Name).FirstOrDefault() ?? "غير محدد"
                        : "غير محدد",
                    t.TreatmentName,
                    t.Dosage,
                    t.WithdrawalPeriod,
                    Cost = 100.0 // هنا يمكنك إضافة حساب تكلفة العلاج حسب اسم الدواء والجرعة
                })
                .ToList();
#pragma warning restore CS8602

            _treatmentGrid.DataSource = records;

            // ملخص المعالجات
            if (!records.Any())
            {
                _summaryTextBox.Text = "لا توجد بيانات معالجات متاحة في الفترة المحددة";
                return;
            }

            var totalTreatments = records.Count;
            var totalCost = records.Sum(r => r.Cost);
            var avgCostPerTreatment = totalCost / totalTreatments;
            var mostUsedTreatment = records.GroupBy(r => r.TreatmentName)
                                         .OrderByDescending(g => g.Count())
                                         .FirstOrDefault();

            var pondTreatments = records.GroupBy(r => r.PondName)
                                       .Select(g => new { Pond = g.Key, Count = g.Count() })
                                       .OrderByDescending(g => g.Count)
                                       .ToList();

            _summaryTextBox.Text = $"ملخص المعالجات والعلاجات ({startDate:yyyy/MM/dd} - {endDate:yyyy/MM/dd}):\n\n" +
                $"إجمالي عدد المعالجات: {totalTreatments}\n" +
                $"التكلفة الإجمالية: {totalCost:C2}\n" +
                $"متوسط التكلفة للعلاج الواحد: {avgCostPerTreatment:C2}\n" +
                $"العلاج الأكثر استخداماً: {mostUsedTreatment?.Key ?? "غير محدد"} ({mostUsedTreatment?.Count() ?? 0} مرة)\n\n" +
                $"الأحواض الأكثر تلقياً للعلاجات:\n";

            foreach (var pond in pondTreatments.Take(5))
            {
                _summaryTextBox.Text += $"- {pond.Pond}: {pond.Count} معالجات\n";
            }

            // تحديث الرسم البياني
            UpdateTreatmentChart(records.Cast<dynamic>().ToList());
        }

        private void UpdateTreatmentChart(System.Collections.Generic.List<dynamic> records)
        {
            _treatmentChart.ClearSeries();

            var treatmentCounts = records.GroupBy(r => r.TreatmentName)
                                         .Select(g => new { Name = g.Key, Count = g.Count() })
                                         .OrderByDescending(g => g.Count)
                                         .Take(10);

            var values = treatmentCounts.Select(t => (double)t.Count).ToArray();
            _treatmentChart.AddSeries("العلاجات", values);
        }

        // طباعة التقرير
        private void PrintButton_Click(object? sender, EventArgs e)
        {
            PrintDialog printDialog = new PrintDialog();
            System.Drawing.Printing.PrintDocument printDocument = new System.Drawing.Printing.PrintDocument();
            printDocument.PrintPage += (s, ev) =>
            {
                using Bitmap bm = new Bitmap(_treatmentGrid.Width, _treatmentGrid.Height);
                _treatmentGrid.DrawToBitmap(bm, new Rectangle(0, 0, _treatmentGrid.Width, _treatmentGrid.Height));
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
                    for (int i = 0; i < _treatmentGrid.Columns.Count; i++)
                    {
                        sw.Write(_treatmentGrid.Columns[i].HeaderText);
                        if (i < _treatmentGrid.Columns.Count - 1) sw.Write(",");
                    }
                    sw.WriteLine();
                    // البيانات
                    foreach (DataGridViewRow row in _treatmentGrid.Rows)
                    {
                        for (int i = 0; i < _treatmentGrid.Columns.Count; i++)
                        {
                            sw.Write(row.Cells[i].Value);
                            if (i < _treatmentGrid.Columns.Count - 1) sw.Write(",");
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
                    sw.WriteLine("تقرير المعالجات والعلاجات");
                    sw.WriteLine("----------------------");
                    // رؤوس الأعمدة
                    for (int i = 0; i < _treatmentGrid.Columns.Count; i++)
                    {
                        sw.Write(_treatmentGrid.Columns[i].HeaderText + "	");
                    }
                    sw.WriteLine();
                    // البيانات
                    foreach (DataGridViewRow row in _treatmentGrid.Rows)
                    {
                        for (int i = 0; i < _treatmentGrid.Columns.Count; i++)
                        {
                            sw.Write(row.Cells[i].Value + "	");
                        }
                        sw.WriteLine();
                    }
                    sw.WriteLine();
}
}
}
}
}