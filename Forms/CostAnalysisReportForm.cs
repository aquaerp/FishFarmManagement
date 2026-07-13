using System;
using FishFarmManager.Services;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Windows.Forms;
using Microsoft.EntityFrameworkCore;
using FishFarmManager.Data;
using FishFarmManager.Models;

namespace FishFarmManager.Forms
{
    public class CostAnalysisReportForm : Form
    {
        private readonly FishFarmContext _context;
        
        // Category Analysis Tab
        private TabControl _reportTabControl = null!;
        private DataGridView _categoryGrid = null!;
        private Panel _categoryChartPanel = null!;
        private DateTimePicker _categoryFromDatePicker = null!;
        private DateTimePicker _categoryToDatePicker = null!;
        private Button _generateCategoryButton = null!;
        
        // Cycle Comparison Tab
        private DataGridView _cycleComparisonGrid = null!;
        private Panel _cycleChartPanel = null!;
        private ComboBox _cycle1ComboBox = null!;
        private ComboBox _cycle2ComboBox = null!;
        private Button _generateCycleButton = null!;
        
        // Cost Per Kg Tab
        private DataGridView _costPerKgGrid = null!;
        private TextBox _costPerKgSummaryTextBox = null!;
        private DateTimePicker _costPerKgFromDatePicker = null!;
        private DateTimePicker _costPerKgToDatePicker = null!;
        private Button _generateCostPerKgButton = null!;
        
        // Trends Tab
        private DataGridView _trendsGrid = null!;
        private Panel _trendsChartPanel = null!;
        private NumericUpDown _monthsBackNumeric = null!;
        private Button _generateTrendsButton = null!;
        
        // Top Suppliers Tab
        private DataGridView _topSuppliersGrid = null!;
        private Panel _topSuppliersChartPanel = null!;
        private NumericUpDown _topCountNumeric = null!;
        private DateTimePicker _topFromDatePicker = null!;
        private DateTimePicker _topToDatePicker = null!;
        private Button _generateTopSuppliersButton = null!;

        public CostAnalysisReportForm(FishFarmContext context)
        {
            _context = context;
            InitializeComponent();
            LoadCycles();
        }

        private void InitializeComponent()
        {
            Text = "تحليل التكاليف";
            Size = new Size(1400, 800);
            RightToLeft = RightToLeft.Yes;
            RightToLeftLayout = true;
            StartPosition = FormStartPosition.CenterScreen;

            // Create main tab control
            _reportTabControl = new TabControl
            {
                Dock = DockStyle.Fill
            };
            Controls.Add(_reportTabControl);

            CreateCategoryAnalysisTab();
            CreateCycleComparisonTab();
            CreateCostPerKgTab();
            CreateMonthlyTrendsTab();
            CreateTopSuppliersTab();
        }

        private void CreateCategoryAnalysisTab()
        {
            var tab = new TabPage("تحليل حسب الفئة");

            var controlPanel = new Panel
            {
                Dock = DockStyle.Top,
                Height = 80,
                Padding = new Padding(10)
            };

            controlPanel.Controls.Add(new Label { Text = "من تاريخ:", Location = new Point(1230, 15), AutoSize = true });
            _categoryFromDatePicker = new DateTimePicker
            {
                Location = new Point(1070, 13
),
                Width = 150,
                Format = DateTimePickerFormat.Short
            };
            _categoryFromDatePicker.Value = DateTime.Now.AddMonths(-1);
            controlPanel.Controls.Add(_categoryFromDatePicker);

            controlPanel.Controls.Add(new Label { Text = "إلى:", Location = new Point(1050, 45), AutoSize = true });
            _categoryToDatePicker = new DateTimePicker
            {
                Location = new Point(890, 45),
                Width = 150,
                Format = DateTimePickerFormat.Short
            };
            controlPanel.Controls.Add(_categoryToDatePicker);

            _generateCategoryButton = new Button
            {
                Text = "إنشاء التقرير",
                Location = new Point(730, 43),
                Width = 150,
                Height = 30,
                BackColor = ThemeManager.SecondarySkyBlue,
                ForeColor = Color.White,
                FlatStyle = FlatStyle.Flat
            };
            _generateCategoryButton.Click += GenerateCategoryButton_Click;
            controlPanel.Controls.Add(_generateCategoryButton);

            tab.Controls.Add(controlPanel);

            // Split container for grid and chart
            var splitContainer = new SplitContainer
            {
                Dock = DockStyle.Fill,
                Orientation = Orientation.Horizontal,
                SplitterDistance = 300
            };

            _categoryGrid = new DataGridView
            {
                Dock = DockStyle.Fill,
                AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill,
                SelectionMode = DataGridViewSelectionMode.FullRowSelect,
                ReadOnly = true,
                AllowUserToAddRows = false
            };
            splitContainer.Panel1.Controls.Add(_categoryGrid);

            _categoryChartPanel = new Panel
            {
                Dock = DockStyle.Fill,
                BackColor = Color.White
            };
            _categoryChartPanel.Paint += CategoryChartPanel_Paint;
            splitContainer.Panel2.Controls.Add(_categoryChartPanel);

            tab.Controls.Add(splitContainer);

            _reportTabControl.TabPages.Add(tab);
        }

        private void CreateCycleComparisonTab()
        {
            var tab = new TabPage("مقارنة الدورات");
            
            var controlPanel = new Panel
            {
                Dock = DockStyle.Top,
                Height = 80,
                Padding = new Padding(10)
            };

            var titleLabel = new Label
            {
                Text = "مقارنة التكاليف بين الدورات الإنتاجية",
                Font = new Font("Segoe UI", 11, FontStyle.Bold),
                Location = new Point(10, 10),
                AutoSize = true
            };
            controlPanel.Controls.Add(titleLabel);

            controlPanel.Controls.Add(new Label { Text = "الدورة الأولى:", Location = new Point(1200, 45), AutoSize = true });
            _cycle1ComboBox = new ComboBox
            {
                Location = new Point(1020, 45),
                Width = 170,
                DropDownStyle = ComboBoxStyle.DropDownList
            };
            controlPanel.Controls.Add(_cycle1ComboBox);

            controlPanel.Controls.Add(new Label { Text = "الدورة الثانية:", Location = new Point(940, 45), AutoSize = true });
            _cycle2ComboBox = new ComboBox
            {
                Location = new Point(760, 45),
                Width = 170,
                DropDownStyle = ComboBoxStyle.DropDownList
            };
            controlPanel.Controls.Add(_cycle2ComboBox);

            _generateCycleButton = new Button
            {
                Text = "مقارنة",
                Location = new Point(610, 43),
                Width = 140,
                Height = 30,
                BackColor = ThemeManager.SuccessGreen,
                ForeColor = Color.White,
                FlatStyle = FlatStyle.Flat
            };
            _generateCycleButton.Click += GenerateCycleButton_Click;
            controlPanel.Controls.Add(_generateCycleButton);

            tab.Controls.Add(controlPanel);

            var splitContainer = new SplitContainer
            {
                Dock = DockStyle.Fill,
                Orientation = Orientation.Horizontal,
                SplitterDistance = 300
            };

            _cycleComparisonGrid = new DataGridView
            {
                Dock = DockStyle.Fill,
                AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill,
                SelectionMode = DataGridViewSelectionMode.FullRowSelect,
                ReadOnly = true,
                AllowUserToAddRows = false
            };
            splitContainer.Panel1.Controls.Add(_cycleComparisonGrid);

            _cycleChartPanel = new Panel
            {
                Dock = DockStyle.Fill,
                BackColor = Color.White
            };
            _cycleChartPanel.Paint += CycleChartPanel_Paint;
            splitContainer.Panel2.Controls.Add(_cycleChartPanel);

            tab.Controls.Add(splitContainer);

            LoadCycles();

            _reportTabControl.TabPages.Add(tab);
        }

        private void CreateCostPerKgTab()
        {
            var tab = new TabPage("تكلفة الكيلو");
            
            var controlPanel = new Panel
            {
                Dock = DockStyle.Top,
                Height = 80,
                Padding = new Padding(10)
            };

            var titleLabel = new Label
            {
                Text = "تحليل تكلفة الكيلوجرام (إجمالي التكاليف / الإنتاج)",
                Font = new Font("Segoe UI", 11, FontStyle.Bold),
                Location = new Point(10, 10),
                AutoSize = true
            };
            controlPanel.Controls.Add(titleLabel);

            controlPanel.Controls.Add(new Label { Text = "من:", Location = new Point(1250, 45), AutoSize = true });
            _costPerKgFromDatePicker = new DateTimePicker
            {
                Location = new Point(1090, 45),
                Width = 150,
                Format = DateTimePickerFormat.Short
            };
            _costPerKgFromDatePicker.Value = DateTime.Now.AddMonths(-3);
            controlPanel.Controls.Add(_costPerKgFromDatePicker);

            controlPanel.Controls.Add(new Label { Text = "إلى:", Location = new Point(1050, 45), AutoSize = true });
            _costPerKgToDatePicker = new DateTimePicker
            {
                Location = new Point(890, 45),
                Width = 150,
                Format = DateTimePickerFormat.Short
            };
            controlPanel.Controls.Add(_costPerKgToDatePicker);

            _generateCostPerKgButton = new Button
            {
                Text = "إنشاء التقرير",
                Location = new Point(730, 43),
                Width = 150,
                Height = 30,
                BackColor = ThemeManager.SecondarySkyBlue,
                ForeColor = Color.White,
                FlatStyle = FlatStyle.Flat
            };
            _generateCostPerKgButton.Click += GenerateCostPerKgButton_Click;
            controlPanel.Controls.Add(_generateCostPerKgButton);

            tab.Controls.Add(controlPanel);

            var splitContainer = new SplitContainer
            {
                Dock = DockStyle.Fill,
                Orientation = Orientation.Vertical,
                SplitterDistance = 800
            };

            _costPerKgGrid = new DataGridView
            {
                Dock = DockStyle.Fill,
                AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill,
                SelectionMode = DataGridViewSelectionMode.FullRowSelect,
                ReadOnly = true,
                AllowUserToAddRows = false
            };
            splitContainer.Panel1.Controls.Add(_costPerKgGrid);

            _costPerKgSummaryTextBox = new TextBox
            {
                Dock = DockStyle.Fill,
                Multiline = true,
                ReadOnly = true,
                Font = new Font("Consolas", 10),
                BackColor = Color.FromArgb(250, 250, 250)
            };
            splitContainer.Panel2.Controls.Add(_costPerKgSummaryTextBox);

            tab.Controls.Add(splitContainer);

            _reportTabControl.TabPages.Add(tab);
        }

        private void CreateMonthlyTrendsTab()
        {
            var tab = new TabPage("الاتجاهات الشهرية");
            
            var controlPanel = new Panel
            {
                Dock = DockStyle.Top,
                Height = 80,
                Padding = new Padding(10)
            };

            var titleLabel = new Label
            {
                Text = "اتجاهات التكاليف الشهرية",
                Font = new Font("Segoe UI", 11, FontStyle.Bold),
                Location = new Point(10, 10),
                AutoSize = true
            };
            controlPanel.Controls.Add(titleLabel);

            controlPanel.Controls.Add(new Label { Text = "عدد الأشهر الماضية:", Location = new Point(1150, 45), AutoSize = true });
            _monthsBackNumeric = new NumericUpDown
            {
                Location = new Point(1030, 45),
                Width = 110,
                Minimum = 1,
                Maximum = 24,
                Value = 6
            };
            controlPanel.Controls.Add(_monthsBackNumeric);

            _generateTrendsButton = new Button
            {
                Text = "إنشاء التقرير",
                Location = new Point(870, 43),
                Width = 150,
                Height = 30,
                BackColor = Color.FromArgb(230, 126, 34),
                ForeColor = Color.White,
                FlatStyle = FlatStyle.Flat
            };
            _generateTrendsButton.Click += GenerateTrendsButton_Click;
            controlPanel.Controls.Add(_generateTrendsButton);

            tab.Controls.Add(controlPanel);

            var splitContainer = new SplitContainer
            {
                Dock = DockStyle.Fill,
                Orientation = Orientation.Horizontal,
                SplitterDistance = 300
            };

            _trendsGrid = new DataGridView
            {
                Dock = DockStyle.Fill,
                AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill,
                SelectionMode = DataGridViewSelectionMode.FullRowSelect,
                ReadOnly = true,
                AllowUserToAddRows = false
            };
            splitContainer.Panel1.Controls.Add(_trendsGrid);

            _trendsChartPanel = new Panel
            {
                Dock = DockStyle.Fill,
                BackColor = Color.White
            };
            _trendsChartPanel.Paint += TrendsChartPanel_Paint;
            splitContainer.Panel2.Controls.Add(_trendsChartPanel);

            tab.Controls.Add(splitContainer);

            _reportTabControl.TabPages.Add(tab);
        }

        private void CreateTopSuppliersTab()
        {
            var tab = new TabPage("أكبر الموردين");
            
            var controlPanel = new Panel
            {
                Dock = DockStyle.Top,
                Height = 80,
                Padding = new Padding(10)
            };

            var titleLabel = new Label
            {
                Text = "أكبر الموردين حسب الإنفاق",
                Font = new Font("Segoe UI", 11, FontStyle.Bold),
                Location = new Point(10, 10),
                AutoSize = true
            };
            controlPanel.Controls.Add(titleLabel);

            controlPanel.Controls.Add(new Label { Text = "عدد الموردين:", Location = new Point(1220, 45), AutoSize = true });
            _topCountNumeric = new NumericUpDown
            {
                Location = new Point(1120, 45),
                Width = 90,
                Minimum = 5,
                Maximum = 50,
                Value = 10
            };
            controlPanel.Controls.Add(_topCountNumeric);

            controlPanel.Controls.Add(new Label { Text = "من:", Location = new Point(1070, 45), AutoSize = true });
            _topFromDatePicker = new DateTimePicker
            {
                Location = new Point(910, 45),
                Width = 150,
                Format = DateTimePickerFormat.Short
            };
            _topFromDatePicker.Value = DateTime.Now.AddMonths(-6);
            controlPanel.Controls.Add(_topFromDatePicker);

            controlPanel.Controls.Add(new Label { Text = "إلى:", Location = new Point(870, 45), AutoSize = true });
            _topToDatePicker = new DateTimePicker
            {
                Location = new Point(710, 45),
                Width = 150,
                Format = DateTimePickerFormat.Short
            };
            controlPanel.Controls.Add(_topToDatePicker);

            _generateTopSuppliersButton = new Button
            {
                Text = "إنشاء التقرير",
                Location = new Point(550, 43),
                Width = 150,
                Height = 30,
                BackColor = ThemeManager.ErrorRed,
                ForeColor = Color.White,
                FlatStyle = FlatStyle.Flat
            };
            _generateTopSuppliersButton.Click += GenerateTopSuppliersButton_Click;
            controlPanel.Controls.Add(_generateTopSuppliersButton);

            tab.Controls.Add(controlPanel);

            var splitContainer = new SplitContainer
            {
                Dock = DockStyle.Fill,
                Orientation = Orientation.Horizontal,
                SplitterDistance = 300
            };

            _topSuppliersGrid = new DataGridView
            {
                Dock = DockStyle.Fill,
                AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill,
                SelectionMode = DataGridViewSelectionMode.FullRowSelect,
                ReadOnly = true,
                AllowUserToAddRows = false
            };
            splitContainer.Panel1.Controls.Add(_topSuppliersGrid);

            _topSuppliersChartPanel = new Panel
            {
                Dock = DockStyle.Fill,
                BackColor = Color.White
            };
            _topSuppliersChartPanel.Paint += TopSuppliersChartPanel_Paint;
            splitContainer.Panel2.Controls.Add(_topSuppliersChartPanel);

            tab.Controls.Add(splitContainer);

            _reportTabControl.TabPages.Add(tab);
        }

        private void LoadCycles()
        {
            var cycles = _context.ProductionCycles
                .OrderByDescending(c => c.StartDate)
                .Select(c => new
                {
                    c.Id,
                    Display = c.Name + " (" + c.StartDate.ToString("dd/MM/yyyy") + ")"
                })
                .ToList();

            _cycle1ComboBox.DataSource = cycles.ToList();
            _cycle1ComboBox.DisplayMember = "Display";
            _cycle1ComboBox.ValueMember = "Id";

            _cycle2ComboBox.DataSource = cycles.ToList();
            _cycle2ComboBox.DisplayMember = "Display";
            _cycle2ComboBox.ValueMember = "Id";
        }

        private void GenerateCategoryButton_Click(object? sender, EventArgs e)
        {
            try
            {
                var fromDate = _categoryFromDatePicker.Value.Date;
                var toDate = _categoryToDatePicker.Value.Date;

                // Load data first to avoid SQLite decimal aggregate issues
                var allRecords = _context.CostRecords
                    .Where(cr => cr.Date >= fromDate && cr.Date <= toDate)
                    .ToList();

                var categoryData = allRecords
                    .GroupBy(cr => cr.Category)
                    .Select(g => new
                    {
                        الفئة = g.Key.ToString(),
                        عدد_السجلات = g.Count(),
                        إجمالي_المبلغ = g.Sum(cr => cr.Amount),
                        متوسط_المبلغ = g.Average(cr => cr.Amount),
                        أقل_مبلغ = g.Min(cr => cr.Amount),
                        أعلى_مبلغ = g.Max(cr => cr.Amount)
                    })
                    .OrderByDescending(x => x.إجمالي_المبلغ)
                    .ToList();

                _categoryGrid.DataSource = categoryData;

                // Format currency columns
                foreach (DataGridViewColumn col in _categoryGrid.Columns)
                {
                    if (col.Name.Contains("المبلغ"))
                    {
                        col.DefaultCellStyle.Format = "N2";
                    }
                }

                _categoryChartPanel.Invalidate();

                var totalAmount = categoryData.Sum(x => x.إجمالي_المبلغ);
                MessageBox.Show($"تم إنشاء التقرير بنجاح\nإجمالي التكاليف: {totalAmount:N2} ريال", 
                    "نجح", MessageBoxButtons.OK, MessageBoxIcon.Information);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"خطأ في إنشاء التقرير: {ex.Message}", "خطأ",
                    MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void CategoryChartPanel_Paint(object? sender, PaintEventArgs e)
        {
            if (_categoryGrid.DataSource == null) return;

            var data = _categoryGrid.DataSource as System.Collections.IList;
            if (data == null || data.Count == 0) return;

            var g = e.Graphics;
            g.SmoothingMode = System.Drawing.Drawing2D.SmoothingMode.AntiAlias;

            var width = _categoryChartPanel.Width;
            var height = _categoryChartPanel.Height;

            // Draw title
            var titleFont = ThemeManager.SubtitleFont;
            g.DrawString("توزيع التكاليف حسب الفئة", titleFont, Brushes.Black, new PointF(width / 2 - 100, 10));

            // Calculate total
            decimal total = 0;
            foreach (var item in data)
            {
                var prop = item.GetType().GetProperty("إجمالي_المبلغ");
                if (prop != null)
                {
                    var val = prop.GetValue(item);
                    if (val is decimal d) total += d;
                }
            }

            if (total == 0) return;

            // Draw pie chart
            var chartRect = new RectangleF(50, 50, Math.Min(width - 100, height - 100), Math.Min(width - 100, height - 100));
            float startAngle = 0;

            var colors = new[]
            {
                ThemeManager.SecondarySkyBlue, ThemeManager.SuccessGreen, ThemeManager.SecondarySkyBlue,
                Color.FromArgb(230, 126, 34), ThemeManager.ErrorRed, Color.FromArgb(26, 188, 156),
                Color.FromArgb(241, 196, 15), Color.FromArgb(149, 165, 166), Color.FromArgb(52, 73, 94),
                Color.FromArgb(192, 57, 43), Color.FromArgb(142, 68, 173), Color.FromArgb(39, 174, 96),
                Color.FromArgb(41, 128, 185), Color.FromArgb(243, 156, 18), Color.FromArgb(211, 84, 0),
                Color.FromArgb(189, 195, 199), Color.FromArgb(127, 140, 141), Color.FromArgb(44, 62, 80),
                Color.FromArgb(22, 160, 133), Color.FromArgb(241, 148, 138), Color.FromArgb(174, 214, 241),
                Color.FromArgb(210, 180, 222), Color.FromArgb(250, 219, 216), Color.FromArgb(248, 196, 113),
                Color.FromArgb(162, 217, 206), Color.FromArgb(229, 231, 233)
            };

            int colorIndex = 0;
            int legendY = 60;
            var legendX = chartRect.Right + 30;

            foreach (var item in data)
            {
                var categoryProp = item.GetType().GetProperty("الفئة");
                var amountProp = item.GetType().GetProperty("إجمالي_المبلغ");
                
                if (categoryProp != null && amountProp != null)
                {
                    var category = categoryProp.GetValue(item)?.ToString();
                    var amountObj = amountProp.GetValue(item);
                    if (amountObj is not decimal amount) continue;
                    var percentage = (float)(amount / total) * 100;
                    var sweepAngle = (float)(amount / total) * 360;

                    var color = colors[colorIndex % colors.Length];
                    using (var brush = new SolidBrush(color))
                    {
                        g.FillPie(brush, chartRect, startAngle, sweepAngle);
                    }

                    // Draw legend
                    g.FillRectangle(new SolidBrush(color), legendX, legendY, 20, 15);
                    g.DrawString($"{category}: {percentage:F1}%", 
                        new Font("Segoe UI", 8), Brushes.Black, legendX + 25, legendY);

                    startAngle += sweepAngle;
                    colorIndex++;
                    legendY += 20;
                }
            }
        }

        private void GenerateCycleButton_Click(object? sender, EventArgs e)
        {
            try
            {
                var cycle1Id = _cycle1ComboBox.SelectedValue is int c1 ? c1 : 0;
                var cycle2Id = _cycle2ComboBox.SelectedValue is int c2 ? c2 : 0;
                if (cycle1Id == 0 || cycle2Id == 0)
                {
                    MessageBox.Show("يرجى اختيار الدورتين أولاً", "تنبيه",
                        MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    return;
                }

                if (cycle1Id == cycle2Id)
                {
                    MessageBox.Show("يرجى اختيار دورتين مختلفتين للمقارنة", "تنبيه",
                        MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    return;
                }

                // Load data first to avoid SQLite decimal aggregate issues
                var cycle1Records = _context.CostRecords
                    .Where(cr => cr.ProductionCycleId == cycle1Id)
                    .ToList();

                var cycle1Costs = cycle1Records
                    .GroupBy(cr => cr.Category)
                    .Select(g => new { Category = g.Key, Amount = g.Sum(cr => cr.Amount) })
                    .ToList();

                var cycle2Records = _context.CostRecords
                    .Where(cr => cr.ProductionCycleId == cycle2Id)
                    .ToList();

                var cycle2Costs = cycle2Records
                    .GroupBy(cr => cr.Category)
                    .Select(g => new { Category = g.Key, Amount = g.Sum(cr => cr.Amount) })
                    .ToList();

                var allCategories = cycle1Costs.Select(c => c.Category)
                    .Union(cycle2Costs.Select(c => c.Category))
                    .Distinct()
                    .OrderBy(c => c.ToString());

                var comparisonData = allCategories.Select(cat => new
                {
                    الفئة = cat.ToString(),
                    الدورة_الأولى = cycle1Costs.FirstOrDefault(c => c.Category == cat)?.Amount ?? 0,
                    الدورة_الثانية = cycle2Costs.FirstOrDefault(c => c.Category == cat)?.Amount ?? 0,
                    الفرق = (cycle1Costs.FirstOrDefault(c => c.Category == cat)?.Amount ?? 0) -
                            (cycle2Costs.FirstOrDefault(c => c.Category == cat)?.Amount ?? 0)
                }).ToList();

                _cycleComparisonGrid.DataSource = comparisonData;

                // Format currency columns
                foreach (DataGridViewColumn col in _cycleComparisonGrid.Columns)
                {
                    if (col.Name != "الفئة")
                    {
                        col.DefaultCellStyle.Format = "N2";
                    }
                }

                _cycleChartPanel.Invalidate();

                var total1 = comparisonData.Sum(x => x.الدورة_الأولى);
                var total2 = comparisonData.Sum(x => x.الدورة_الثانية);
                MessageBox.Show($"تم إنشاء المقارنة بنجاح\n\nالدورة الأولى: {total1:N2} ريال\nالدورة الثانية: {total2:N2} ريال\nالفرق: {(total1 - total2):N2} ريال",
                    "نجح", MessageBoxButtons.OK, MessageBoxIcon.Information);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"خطأ في إنشاء المقارنة: {ex.Message}", "خطأ",
                    MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void CycleChartPanel_Paint(object? sender, PaintEventArgs e)
        {
            if (_cycleComparisonGrid.DataSource == null) return;

            var data = _cycleComparisonGrid.DataSource as System.Collections.IList;
            if (data == null || data.Count == 0) return;

            var g = e.Graphics;
            g.SmoothingMode = System.Drawing.Drawing2D.SmoothingMode.AntiAlias;

            var width = _cycleChartPanel.Width;
            var height = _cycleChartPanel.Height;

            // Draw title
            var titleFont = ThemeManager.SubtitleFont;
            g.DrawString("مقارنة التكاليف بين الدورتين", titleFont, Brushes.Black, new PointF(width / 2 - 100, 10));

            // Find max value for scaling
            decimal maxValue = 0;
            foreach (var item in data)
            {
                var prop1 = item.GetType().GetProperty("الدورة_الأولى");
                var prop2 = item.GetType().GetProperty("الدورة_الثانية");
                if (prop1 != null && prop2 != null)
                {
                    var v1 = prop1.GetValue(item);
                    var v2 = prop2.GetValue(item);
                    if (v1 is decimal d1) maxValue = Math.Max(maxValue, d1);
                    if (v2 is decimal d2) maxValue = Math.Max(maxValue, d2);
                }
            }

            if (maxValue == 0) return;

            // Draw bars
            var chartX = 50;
            var chartY = 50;
            var chartWidth = width - 100;
            var chartHeight = height - 100;
            var barWidth = Math.Min(30, chartWidth / (data.Count * 3));
            var spacing = 10;

            int index = 0;
            foreach (var item in data)
            {
                var categoryProp = item.GetType().GetProperty("الفئة");
                var cycle1Prop = item.GetType().GetProperty("الدورة_الأولى");
                var cycle2Prop = item.GetType().GetProperty("الدورة_الثانية");

                if (categoryProp != null && cycle1Prop != null && cycle2Prop != null)
                {
                    var v1 = cycle1Prop.GetValue(item);
                    var v2 = cycle2Prop.GetValue(item);
                    if (v1 is not decimal cycle1Amount || v2 is not decimal cycle2Amount) continue;

                    var x = chartX + (index * (barWidth * 2 + spacing));
                    var bar1Height = (float)(cycle1Amount / maxValue * chartHeight);
                    var bar2Height = (float)(cycle2Amount / maxValue * chartHeight);

                    // Draw cycle 1 bar
                    g.FillRectangle(Brushes.Blue, x, chartY + chartHeight - bar1Height, barWidth, bar1Height);
                    
                    // Draw cycle 2 bar
                    g.FillRectangle(Brushes.Red, x + barWidth, chartY + chartHeight - bar2Height, barWidth, bar2Height);

                    index++;
                }
            }

            // Draw legend
            g.FillRectangle(Brushes.Blue, width - 200, 20, 20, 15);
            g.DrawString("الدورة الأولى", ThemeManager.SmallFont, Brushes.Black, width - 175, 18);
            
            g.FillRectangle(Brushes.Red, width - 200, 40, 20, 15);
            g.DrawString("الدورة الثانية", ThemeManager.SmallFont, Brushes.Black, width - 175, 38);
        }

        private void GenerateCostPerKgButton_Click(object? sender, EventArgs e)
        {
            try
            {
                var fromDate = _costPerKgFromDatePicker.Value.Date;
                var toDate = _costPerKgToDatePicker.Value.Date;

                // Load data first, then calculate in memory to avoid SQLite decimal aggregate issues
                var cyclesRaw = _context.ProductionCycles
                    .Include(pc => pc.CostRecords)
                    .Where(pc => pc.StartDate >= fromDate && pc.StartDate <= toDate)
                    .ToList();

                var cyclesData = cyclesRaw
                    .Select(pc => new
                    {
                        الدورة = pc.Name,
                        تاريخ_البداية = pc.StartDate,
                        تاريخ_النهاية = pc.EndDate,
                        كمية_الإنتاج_كجم = pc.TotalHarvestWeight ?? 0,
                        إجمالي_التكاليف = pc.CostRecords.Sum(cr => cr.Amount),
                        تكلفة_الكيلو = pc.TotalHarvestWeight > 0 ? 
                            pc.CostRecords.Sum(cr => cr.Amount) / (decimal)pc.TotalHarvestWeight.Value : 0,
                        عدد_سجلات_التكلفة = pc.CostRecords.Count
                    })
                    .OrderBy(x => x.تاريخ_البداية)
                    .ToList();

                _costPerKgGrid.DataSource = cyclesData;

                // Format columns
                if (_costPerKgGrid.Columns.Contains("إجمالي_التكاليف"))
                    _costPerKgGrid.Columns["إجمالي_التكاليف"].DefaultCellStyle.Format = "N2";
                
                if (_costPerKgGrid.Columns.Contains("تكلفة_الكيلو"))
                {
                    _costPerKgGrid.Columns["تكلفة_الكيلو"].DefaultCellStyle.Format = "N2";
                    _costPerKgGrid.Columns["تكلفة_الكيلو"].DefaultCellStyle.BackColor = Color.LightYellow;
                }

                // Generate summary
                var totalProduction = cyclesData.Sum(x => x.كمية_الإنتاج_كجم);
                var totalCost = cyclesData.Sum(x => x.إجمالي_التكاليف);
                var avgCostPerKg = totalProduction > 0 ? totalCost / (decimal)totalProduction : 0;
                var minCostPerKg = cyclesData.Where(x => x.تكلفة_الكيلو > 0).Min(x => (decimal?)x.تكلفة_الكيلو) ?? 0;
                var maxCostPerKg = cyclesData.Max(x => (decimal?)x.تكلفة_الكيلو) ?? 0;

                var summary = $@"
═══════════════════════════════════════════
        ملخص تكلفة الكيلوجرام
═══════════════════════════════════════════

عدد الدورات المحللة: {cyclesData.Count}

إجمالي الإنتاج: {totalProduction:N2} كجم
إجمالي التكاليف: {totalCost:N2} ريال

متوسط تكلفة الكيلو: {avgCostPerKg:N2} ريال
أقل تكلفة للكيلو: {minCostPerKg:N2} ريال
أعلى تكلفة للكيلو: {maxCostPerKg:N2} ريال

الفرق بين الأعلى والأقل: {(maxCostPerKg - minCostPerKg):N2} ريال

═══════════════════════════════════════════
التوصيات:
═══════════════════════════════════════════

• استهدف تكلفة الكيلو: {(avgCostPerKg * 0.9m):N2} ريال
• مراجعة الدورات ذات التكلفة العالية
• تحليل أسباب الاختلاف بين الدورات
• تطبيق ممارسات الدورات ذات التكلفة المنخفضة

═══════════════════════════════════════════
";

                _costPerKgSummaryTextBox.Text = summary;

                MessageBox.Show($"تم إنشاء التقرير بنجاح\n\nمتوسط تكلفة الكيلو: {avgCostPerKg:N2} ريال",
                    "نجح", MessageBoxButtons.OK, MessageBoxIcon.Information);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"خطأ في إنشاء التقرير: {ex.Message}", "خطأ",
                    MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void GenerateTrendsButton_Click(object? sender, EventArgs e)
        {
            try
            {
                var monthsBack = (int)_monthsBackNumeric.Value;
                var startDate = DateTime.Now.AddMonths(-monthsBack).Date;

                // Load data first to avoid SQLite decimal aggregate issues
                var allRecords = _context.CostRecords
                    .Where(cr => cr.Date >= startDate)
                    .ToList();

                var monthlyData = allRecords
                    .GroupBy(cr => new { cr.Date.Year, cr.Date.Month })
                    .Select(g => new
                    {
                        السنة = g.Key.Year,
                        الشهر = g.Key.Month,
                        الشهر_والسنة = g.Key.Month + "/" + g.Key.Year,
                        عدد_السجلات = g.Count(),
                        إجمالي_التكاليف = g.Sum(cr => cr.Amount),
                        متوسط_التكلفة = g.Average(cr => cr.Amount)
                    })
                    .OrderBy(x => x.السنة).ThenBy(x => x.الشهر)
                    .ToList();

                _trendsGrid.DataSource = monthlyData;

                // Format columns
                foreach (DataGridViewColumn col in _trendsGrid.Columns)
                {
                    if (col.Name.Contains("التكاليف") || col.Name.Contains("التكلفة"))
                    {
                        col.DefaultCellStyle.Format = "N2";
                    }
                }

                _trendsChartPanel.Invalidate();

                var avgMonthly = monthlyData.Average(x => x.إجمالي_التكاليف);
                MessageBox.Show($"تم إنشاء التقرير بنجاح\n\nمتوسط التكاليف الشهرية: {avgMonthly:N2} ريال",
                    "نجح", MessageBoxButtons.OK, MessageBoxIcon.Information);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"خطأ في إنشاء التقرير: {ex.Message}", "خطأ",
                    MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void TrendsChartPanel_Paint(object? sender, PaintEventArgs e)
        {
            if (_trendsGrid.DataSource == null) return;

            var data = _trendsGrid.DataSource as System.Collections.IList;
            if (data == null || data.Count == 0) return;

            var g = e.Graphics;
            g.SmoothingMode = System.Drawing.Drawing2D.SmoothingMode.AntiAlias;

            var width = _trendsChartPanel.Width;
            var height = _trendsChartPanel.Height;

            // Draw title
            var titleFont = ThemeManager.SubtitleFont;
            g.DrawString("اتجاه التكاليف الشهرية", titleFont, Brushes.Black, new PointF(width / 2 - 80, 10));

            // Find max value
            decimal maxValue = 0;
            foreach (var item in data)
            {
                var prop = item.GetType().GetProperty("إجمالي_التكاليف");
                if (prop != null)
                {
                    var v = prop.GetValue(item);
                    if (v is decimal d) maxValue = Math.Max(maxValue, d);
                }
            }

            if (maxValue == 0) return;

            // Chart dimensions
            var chartX = 80;
            var chartY = 50;
            var chartWidth = width - 120;
            var chartHeight = height - 100;

            // Draw axes
            g.DrawLine(Pens.Black, chartX, chartY + chartHeight, chartX + chartWidth, chartY + chartHeight); // X-axis
            g.DrawLine(Pens.Black, chartX, chartY, chartX, chartY + chartHeight); // Y-axis

            // Draw line chart
            var points = new PointF[data.Count];
            int index = 0;

            foreach (var item in data)
            {
                var monthProp = item.GetType().GetProperty("الشهر_والسنة");
                var amountProp = item.GetType().GetProperty("إجمالي_التكاليف");

                if (monthProp != null && amountProp != null)
                {
                    var v = amountProp.GetValue(item);
                    if (v is not decimal amount) continue;
                    var x = chartX + (index * chartWidth / (data.Count - 1));
                    var y = chartY + chartHeight - (float)(amount / maxValue * chartHeight);

                    points[index] = new PointF(x, y);

                    // Draw point
                    g.FillEllipse(Brushes.Blue, x - 3, y - 3, 6, 6);

                    index++;
                }
            }

            // Draw line
            if (points.Length > 1)
            {
                g.DrawLines(new Pen(Color.Blue, 2), points);
            }

            // Draw value labels on Y-axis
            for (int i = 0; i <= 5; i++)
            {
                var value = maxValue * i / 5;
                var y = chartY + chartHeight - (i * chartHeight / 5);
                g.DrawString($"{value:N0}", new Font("Segoe UI", 8), Brushes.Black, 10, y - 8);
                g.DrawLine(Pens.LightGray, chartX, y, chartX + chartWidth, y);
            }
        }

        private void GenerateTopSuppliersButton_Click(object? sender, EventArgs e)
        {
            try
            {
                var topCount = (int)_topCountNumeric.Value;
                var fromDate = _topFromDatePicker.Value.Date;
                var toDate = _topToDatePicker.Value.Date;

                // Load data first to avoid SQLite decimal aggregate issues
                var allRecords = _context.CostRecords
                    .Include(cr => cr.Supplier)
                    .Where(cr => cr.SupplierId.HasValue && cr.Date >= fromDate && cr.Date <= toDate)
                    .ToList();

                var topSuppliers = allRecords
                    .GroupBy(cr => cr.Supplier)
                    .Select(g => new
                    {
                        المورد = g.Key?.Name ?? "غير معروف",
                        النوع = g.Key?.Type.ToString() ?? "غير محدد",
                        عدد_العمليات = g.Count(),
                        إجمالي_الإنفاق = g.Sum(cr => cr.Amount),
                        متوسط_المبلغ = g.Average(cr => cr.Amount),
                        آخر_عملية = g.Max(cr => cr.Date)
                    })
                    .OrderByDescending(x => x.إجمالي_الإنفاق)
                    .Take(topCount)
                    .ToList();

                _topSuppliersGrid.DataSource = topSuppliers;

                // Format columns
                foreach (DataGridViewColumn col in _topSuppliersGrid.Columns)
                {
                    if (col.Name.Contains("الإنفاق") || col.Name.Contains("المبلغ"))
                    {
                        col.DefaultCellStyle.Format = "N2";
                    }
                }

                _topSuppliersChartPanel.Invalidate();

                var totalSpending = topSuppliers.Sum(x => x.إجمالي_الإنفاق);
                MessageBox.Show($"تم إنشاء التقرير بنجاح\n\nإجمالي الإنفاق على أكبر {topCount} موردين: {totalSpending:N2} ريال",
                    "نجح", MessageBoxButtons.OK, MessageBoxIcon.Information);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"خطأ في إنشاء التقرير: {ex.Message}", "خطأ",
                    MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void TopSuppliersChartPanel_Paint(object? sender, PaintEventArgs e)
        {
            if (_topSuppliersGrid.DataSource == null) return;

            var data = _topSuppliersGrid.DataSource as System.Collections.IList;
            if (data == null || data.Count == 0) return;

            var g = e.Graphics;
            g.SmoothingMode = System.Drawing.Drawing2D.SmoothingMode.AntiAlias;

            var width = _topSuppliersChartPanel.Width;
            var height = _topSuppliersChartPanel.Height;

            // Draw title
            var titleFont = ThemeManager.SubtitleFont;
            g.DrawString("أكبر الموردين حسب الإنفاق", titleFont, Brushes.Black, new PointF(width / 2 - 100, 10));

            // Find max value
            decimal maxValue = 0;
            foreach (var item in data)
            {
                var prop = item.GetType().GetProperty("إجمالي_الإنفاق");
                if (prop != null)
                {
                    var v = prop.GetValue(item);
                    if (v is decimal d) maxValue = Math.Max(maxValue, d);
                }
            }

            if (maxValue == 0) return;

            // Draw horizontal bar chart
            var chartWidth = width - 250;
            var chartHeight = height - 80;
            var barHeight = Math.Min(30, chartHeight / (data.Count * 2));

            var colors = new[]
            {
                ThemeManager.ErrorRed, Color.FromArgb(230, 126, 34), Color.FromArgb(241, 196, 15),
                ThemeManager.SuccessGreen, ThemeManager.SecondarySkyBlue, ThemeManager.SecondarySkyBlue,
                Color.FromArgb(26, 188, 156), Color.FromArgb(149, 165, 166), Color.FromArgb(127, 140, 141),
                Color.FromArgb(192, 57, 43)
            };

            foreach (var item in data)
            {
                var supplierProp = item.GetType().GetProperty("المورد");
                var amountProp = item.GetType().GetProperty("إجمالي_الإنفاق");

                if (supplierProp != null && amountProp != null)
                {
                    var supplier = supplierProp.GetValue(item)?.ToString();
                    var v = amountProp.GetValue(item);
                    if (v is not decimal amount) continue;

                    var index = data.IndexOf(item);
                    var y = 60 + (index * (barHeight + 12));
                    var barWidth = (int)(amount / maxValue * chartWidth);
                    var color = colors[index % colors.Length];

                    using var brush = new SolidBrush(color);
                    g.FillRectangle(brush, 200, y, barWidth, barHeight);
                    g.DrawRectangle(Pens.Gray, 200, y, barWidth, barHeight);

                    var label = string.IsNullOrWhiteSpace(supplier) ? "غير معروف" : supplier;
                    if (label.Length > 24)
                        label = label.Substring(0, 21) + "...";

                    g.DrawString(label, ThemeManager.SmallFont, Brushes.Black, 10, y + 4);
                    g.DrawString($"{amount:N0}", ThemeManager.SmallFont, Brushes.Black, 205 + barWidth, y + 4);
                }
            }
        }
    }
}
