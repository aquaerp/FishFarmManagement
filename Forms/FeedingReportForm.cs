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
    public class FeedingReportForm : Form
    {
        private readonly FishFarmContext _context;
        private ComboBox _periodComboBox = null!;
        private ComboBox _yearComboBox = null!;
        private DateTimePicker _startDatePicker = null!;
        private DateTimePicker _endDatePicker = null!;
        private DataGridView _feedingGrid = null!;
        private TextBox _summaryTextBox = null!;
        private Button _generateButton = null!;
        private ChartControl _feedingChart = null!;

        public FeedingReportForm(FishFarmContext context)
        {
            _context = context;
            InitializeComponent();
            LoadYears();
            LoadFeedingData();
        }

        private void InitializeComponent()
        {
            this.Text = "تقرير التغذية";
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
            _feedingGrid = new DataGridView();
            _feedingGrid.Dock = DockStyle.Fill;
            _feedingGrid.AutoGenerateColumns = false;
            _feedingGrid.ReadOnly = true;
            _feedingGrid.AllowUserToAddRows = false;

            _feedingGrid.Columns.Add(new DataGridViewTextBoxColumn { DataPropertyName = "Date", HeaderText = "التاريخ", Width = 120, DefaultCellStyle = new DataGridViewCellStyle { Format = "yyyy/MM/dd" } });
            _feedingGrid.Columns.Add(new DataGridViewTextBoxColumn { DataPropertyName = "PondName", HeaderText = "الحوض", Width = 120 });
            _feedingGrid.Columns.Add(new DataGridViewTextBoxColumn { DataPropertyName = "FeedType", HeaderText = "نوع العلف", Width = 150 });
            _feedingGrid.Columns.Add(new DataGridViewTextBoxColumn { DataPropertyName = "FeedQuantity", HeaderText = "كمية العلف (كجم)", Width = 150, DefaultCellStyle = new DataGridViewCellStyle { Format = "F2" } });
            _feedingGrid.Columns.Add(new DataGridViewTextBoxColumn { DataPropertyName = "FishCount", HeaderText = "عدد الأسماك", Width = 120 });
            _feedingGrid.Columns.Add(new DataGridViewTextBoxColumn { DataPropertyName = "Cost", HeaderText = "التكلفة", Width = 120, DefaultCellStyle = new DataGridViewCellStyle { Format = "C2", Alignment = DataGridViewContentAlignment.MiddleRight } });

            contentPanel.Controls.Add(_feedingGrid, 0, 0);

            // الرسم البياني
            _feedingChart = new ChartControl();
            _feedingChart.Dock = DockStyle.Fill;
            contentPanel.Controls.Add(_feedingChart, 1, 0);

            mainPanel.Controls.Add(contentPanel, 0, 1);

            // ملخص التغذية
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

        private void LoadFeedingData()
        {
            GenerateButton_Click(this, EventArgs.Empty);
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
            var yearObj = _yearComboBox.SelectedItem;
            var periodObj = _periodComboBox.SelectedItem;
            if (yearObj == null || periodObj == null)
                return;
            if (!int.TryParse(yearObj.ToString(), out var selectedYear))
                selectedYear = DateTime.Now.Year;
            var selectedPeriod = periodObj.ToString();

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

            var records = _context.FeedingRecords
                .Include(f => f.Cycle)
                    .ThenInclude(c => c.ProductionCyclePonds)
                        .ThenInclude(pcp => pcp.Pond)
                .Where(f => f.FeedingDate >= startDate && f.FeedingDate <= endDate)
                .OrderByDescending(f => f.FeedingDate)
                .Select(f => new
                {
                    Date = f.FeedingDate,
                    PondName = (f.Cycle.ProductionCyclePonds.Any() 
                        ? f.Cycle.ProductionCyclePonds.Select(pcp => pcp.Pond.Name).FirstOrDefault() 
                        : "غير محدد"),
                    f.FeedType,
                    FeedQuantity = f.Quantity,
                    FishCount = f.Cycle.InitialFishCount,
                    Cost = f.Quantity * f.FeedPrice
                })
                .ToList();

            _feedingGrid.DataSource = records;

            // ملخص التغذية
            if (!records.Any())
            {
                _summaryTextBox.Text = "لا توجد بيانات تغذية متاحة في الفترة المحددة";
                return;
            }

            var totalFeedQuantity = records.Sum(r => r.FeedQuantity);
            var totalCost = records.Sum(r => r.Cost);
            var avgFeedPerDay = totalFeedQuantity / ((endDate - startDate).Days + 1);
            var mostUsedFeed = records.GroupBy(r => r.FeedType)
                                     .OrderByDescending(g => g.Sum(r => r.FeedQuantity))
                                     .FirstOrDefault();

            var pondFeeding = records.GroupBy(r => r.PondName)
                                    .Select(g => new { Pond = g.Key, Quantity = g.Sum(r => r.FeedQuantity) })
                                    .OrderByDescending(g => g.Quantity)
                                    .ToList();

            _summaryTextBox.Text = $"ملخص التغذية ({startDate:yyyy/MM/dd} - {endDate:yyyy/MM/dd}):\n\n" +
                $"إجمالي كمية العلف: {totalFeedQuantity:F2} كجم\n" +
                $"التكلفة الإجمالية: {totalCost:C2}\n" +
                $"متوسط كمية العلف اليومي: {avgFeedPerDay:F2} كجم\n" +
                $"نوع العلف الأكثر استخداماً: {(mostUsedFeed != null ? mostUsedFeed.Key.ToString() : "غير محدد")} ({(mostUsedFeed != null ? mostUsedFeed.Sum(r => r.FeedQuantity) : 0):F2} كجم)\n\n" +
                $"أعلى 5 أحواض من حيث استهلاك العلف:\n";

            for (int i = 0; i < Math.Min(5, pondFeeding.Count); i++)
            {
                _summaryTextBox.Text += $"{i + 1}. {pondFeeding[i].Pond}: {pondFeeding[i].Quantity:F2} كجم\n";
            }

            // تحديث الرسم البياني
            UpdateFeedingChart(records.Cast<dynamic>().ToList());
        }

        private void UpdateFeedingChart(System.Collections.Generic.List<dynamic> records)
        {
            _feedingChart.ClearSeries();
            var groups = records.GroupBy(r => r.PondName)
                                .Select(g => new { Pond = g.Key, Qty = g.Sum(r => (double)r.FeedQuantity) })
                                .OrderByDescending(g => g.Qty)
                                .Take(10)
                                .ToList();

            var values = groups.Select(g => g.Qty).ToArray();
            _feedingChart.AddSeries("استهلاك العلف", values);
        }

        private void PrintButton_Click(object? sender, EventArgs e)
        {
            PrintDialog printDialog = new PrintDialog();
            System.Drawing.Printing.PrintDocument printDocument = new System.Drawing.Printing.PrintDocument();
            printDocument.PrintPage += (s, ev) =>
            {
                if (_feedingGrid == null) return;
                using (Bitmap bm = new Bitmap(_feedingGrid.Width, _feedingGrid.Height))
                {
                    _feedingGrid.DrawToBitmap(bm, new Rectangle(0, 0, _feedingGrid.Width, _feedingGrid.Height));
                    ev.Graphics?.DrawImage(bm, 0, 0);
                }
            };
            printDialog.Document = printDocument;
            if (printDialog.ShowDialog() == DialogResult.OK)
            {
                printDocument.Print();
            }
        }

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
                    for (int i = 0; i < _feedingGrid.Columns.Count; i++)
                    {
                        sw.Write(_feedingGrid.Columns[i].HeaderText);
                        if (i < _feedingGrid.Columns.Count - 1) sw.Write(",");
                    }
                    sw.WriteLine();
                    // البيانات
                    foreach (DataGridViewRow row in _feedingGrid.Rows)
                    {
                        for (int i = 0; i < _feedingGrid.Columns.Count; i++)
                        {
                            var val = row.Cells[i].Value?.ToString() ?? string.Empty;
                            sw.Write(val);
                            if (i < _feedingGrid.Columns.Count - 1) sw.Write(",");
                        }
                        sw.WriteLine();
                    }
                }
                MessageBox.Show("تم تصدير التقرير إلى Excel (بصيغة CSV)", "نجاح", MessageBoxButtons.OK, MessageBoxIcon.Information);
            }
        }

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
                    sw.WriteLine("تقرير التغذية");
                    sw.WriteLine("----------------------");
                    // رؤوس الأعمدة
                    for (int i = 0; i < _feedingGrid.Columns.Count; i++)
                    {
                        sw.Write(_feedingGrid.Columns[i].HeaderText + "	");
                    }
                    sw.WriteLine();
                    // البيانات
                    foreach (DataGridViewRow row in _feedingGrid.Rows)
                    {
                        for (int i = 0; i < _feedingGrid.Columns.Count; i++)
                        {
                            sw.Write(row.Cells[i].Value + "	");
}
}
}
}
}
}
}