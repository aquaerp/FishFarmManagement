using System;
using System.Drawing;
using System.Threading.Tasks;
using System.Windows.Forms;
using FishFarmManager.Data;
using FishFarmManager.Models;
using FishFarmManager.Services;

namespace FishFarmManager.Forms
{
    /// <summary>
    /// نموذج قائمة الدخل المطور
    /// Enhanced Income Statement Form
    /// General-ledger based; subject to external accounting review.
    /// </summary>
    public partial class IncomeStatementForm : Form
    {
        private readonly FishFarmContext _context;
        private readonly FinancialService _financialService;
        private readonly PrintService _printService;
        private readonly ExcelExportService _excelExportService;
        private IncomeStatementData? _currentIncomeStatement;

        private DateTimePicker _startDatePicker = null!;
        private DateTimePicker _endDatePicker = null!;
        private ComboBox _periodComboBox = null!;
        private ComboBox _comparisonComboBox = null!;

        private Button _generateButton = null!;
        private Button _printButton = null!;
        private Button _exportButton = null!;
        private Button _emailButton = null!;

        private Panel _reportPanel = null!;
        private Panel _summaryPanel = null!;
        private Panel _chartsPanel = null!;
        private ProgressBar _progressBar = null!;
        private TabControl _tabControl = null!;

        public IncomeStatementForm(FishFarmContext context)
        {
            _context = context ?? throw new ArgumentNullException(nameof(context));
            _financialService = new FinancialService(_context);
            _printService = new PrintService();
            _excelExportService = new ExcelExportService();

            if (!AuthenticationService.HasPermission(UserRole.Admin, UserRole.Manager, UserRole.Accountant))
            {
                MessageBox.Show("ليس لديك صلاحية لعرض قائمة الدخل", "تحذير",
                    MessageBoxButtons.OK, MessageBoxIcon.Warning);
                this.Load += (s, e) => this.Close();
                return;
            }

            InitializeComponent();
            InitializeForm();
        }

        private void InitializeComponent()
        {
            this.Text = "قائمة الدخل المتقدمة - Advanced Income Statement";
            this.Size = new Size(1600, 1000);
            this.StartPosition = FormStartPosition.CenterScreen;
            this.RightToLeft = RightToLeft.Yes;
            this.RightToLeftLayout = true;
            this.Font = new Font("Cairo", 9F);
            this.WindowState = FormWindowState.Maximized;
            this.BackColor = Color.FromArgb(245, 245, 245);
        }

        private void InitializeForm()
        {
            var mainPanel = new Panel 
            { 
                Dock = DockStyle.Fill, 
                Padding = new Padding(20), 
                BackColor = Color.FromArgb(245, 245, 245) 
            };

            // Header Panel
            CreateHeaderPanel(mainPanel);

            // Control Panel
            CreateControlPanel(mainPanel);

            // Progress Bar
            _progressBar = new ProgressBar
            {
                Location = new Point(20, 160),
                Size = new Size(this.Width - 80, 10),
                Anchor = AnchorStyles.Left | AnchorStyles.Right | AnchorStyles.Top,
                Visible = false,
                Style = ProgressBarStyle.Marquee
            };
            mainPanel.Controls.Add(_progressBar);

            // Tab Control for different views
            CreateTabControl(mainPanel);

            this.Controls.Add(mainPanel);
        }

        private void CreateHeaderPanel(Panel parent)
        {
            var headerPanel = new Panel
            {
                Location = new Point(20, 10),
                Size = new Size(parent.Width - 40, 60),
                Anchor = AnchorStyles.Left | AnchorStyles.Right | AnchorStyles.Top,
                BackColor = Color.FromArgb(0, 122, 204),
                BorderStyle = BorderStyle.None
            };

            var titleLabel = new Label
            {
                Text = "📊 قائمة الدخل التفصيلية",
                Location = new Point(20, 15),
                Size = new Size(400, 30),
                Font = new Font("Cairo", 14F, FontStyle.Bold),
                ForeColor = Color.White,
                TextAlign = ContentAlignment.MiddleRight
            };

            var subtitleLabel = new Label
            {
                Text = "مستخرجة من الأستاذ العام — قيد المراجعة المحاسبية",
                Location = new Point(20, 35),
                Size = new Size(350, 20),
                Font = new Font("Cairo", 8F),
                ForeColor = Color.FromArgb(220, 220, 220),
                TextAlign = ContentAlignment.MiddleRight
            };

            headerPanel.Controls.AddRange(new Control[] { titleLabel, subtitleLabel });
            parent.Controls.Add(headerPanel);
        }

        private void CreateControlPanel(Panel parent)
        {
            var controlPanel = new Panel
            {
                Location = new Point(20, 80),
                Size = new Size(parent.Width - 40, 70),
                Anchor = AnchorStyles.Left | AnchorStyles.Right | AnchorStyles.Top,
                BackColor = Color.White,
                BorderStyle = BorderStyle.FixedSingle
            };

            // Period Selection
            var periodLabel = new Label
            {
                Text = "الفترة:",
                Location = new Point(1350, 15),
                Size = new Size(60, 20),
                TextAlign = ContentAlignment.MiddleRight
            };

            _periodComboBox = new ComboBox
            {
                Location = new Point(1200, 12),
                Size = new Size(140, 25),
                DropDownStyle = ComboBoxStyle.DropDownList
            };
            _periodComboBox.Items.AddRange(new[] { "فترة مخصصة", "الشهر الحالي", "الربع الحالي", "السنة الحالية", "الشهر السابق", "السنة السابقة" });
            _periodComboBox.SelectedIndex = 0;
            _periodComboBox.SelectedIndexChanged += PeriodComboBox_SelectedIndexChanged;

            // Date Pickers
            var startLabel = new Label
            {
                Text = "من:",
                Location = new Point(1150, 15),
                Size = new Size(30, 20),
                TextAlign = ContentAlignment.MiddleRight
            };

            _startDatePicker = new DateTimePicker
            {
                Location = new Point(1000, 12),
                Size = new Size(140, 25),
                Format = DateTimePickerFormat.Short,
                Value = DateTime.Today.AddMonths(-1)
            };

            var endLabel = new Label
            {
                Text = "إلى:",
                Location = new Point(950, 15),
                Size = new Size(30, 20),
                TextAlign = ContentAlignment.MiddleRight
            };

            _endDatePicker = new DateTimePicker
            {
                Location = new Point(800, 12),
                Size = new Size(140, 25),
                Format = DateTimePickerFormat.Short,
                Value = DateTime.Today
            };

            // Comparison Option
            var comparisonLabel = new Label
            {
                Text = "مقارنة مع:",
                Location = new Point(720, 15),
                Size = new Size(70, 20),
                TextAlign = ContentAlignment.MiddleRight
            };

            _comparisonComboBox = new ComboBox
            {
                Location = new Point(560, 12),
                Size = new Size(150, 25),
                DropDownStyle = ComboBoxStyle.DropDownList
            };
            _comparisonComboBox.Items.AddRange(new[] { "بدون مقارنة", "الفترة السابقة", "نفس الفترة العام السابق", "الموازنة المعتمدة" });
            _comparisonComboBox.SelectedIndex = 0;

            // Buttons
            _generateButton = new Button
            {
                Text = "🔄 إنشاء التقرير",
                Location = new Point(400, 10),
                Size = new Size(130, 30),
                BackColor = Color.FromArgb(40, 167, 69),
                ForeColor = Color.White,
                FlatStyle = FlatStyle.Flat,
                Font = new Font("Cairo", 9F, FontStyle.Bold),
                Cursor = Cursors.Hand
            };
            _generateButton.Click += GenerateButton_Click;

            _printButton = new Button
            {
                Text = "🖨️ طباعة",
                Location = new Point(260, 10),
                Size = new Size(100, 30),
                BackColor = Color.FromArgb(108, 117, 125),
                ForeColor = Color.White,
                FlatStyle = FlatStyle.Flat,
                Cursor = Cursors.Hand,
                Enabled = false
            };
            _printButton.Click += PrintButton_Click;

            _exportButton = new Button
            {
                Text = "📤 تصدير Excel",
                Location = new Point(120, 10),
                Size = new Size(120, 30),
                BackColor = Color.FromArgb(255, 193, 7),
                ForeColor = Color.Black,
                FlatStyle = FlatStyle.Flat,
                Cursor = Cursors.Hand,
                Enabled = false
            };
            _exportButton.Click += ExportButton_Click;

            _emailButton = new Button
            {
                Text = "📧 إرسال",
                Location = new Point(20, 10),
                Size = new Size(80, 30),
                BackColor = Color.FromArgb(0, 123, 255),
                ForeColor = Color.White,
                FlatStyle = FlatStyle.Flat,
                Cursor = Cursors.Hand,
                Enabled = false
            };
            _emailButton.Click += EmailButton_Click;

            // Second Row - Additional Options
            var showPercentagesCheckBox = new CheckBox
            {
                Text = "إظهار النسب المئوية",
                Location = new Point(1200, 45),
                Size = new Size(150, 20),
                Checked = true
            };

            var showComparisonsCheckBox = new CheckBox
            {
                Text = "إظهار التغييرات",
                Location = new Point(1050, 45),
                Size = new Size(120, 20),
                Checked = true
            };

            var showChartsCheckBox = new CheckBox
            {
                Text = "إظهار الرسوم البيانية",
                Location = new Point(900, 45),
                Size = new Size(140, 20),
                Checked = true
            };

            controlPanel.Controls.AddRange(new Control[] 
            {
                periodLabel, _periodComboBox, startLabel, _startDatePicker, endLabel, _endDatePicker,
                comparisonLabel, _comparisonComboBox, _generateButton, _printButton, _exportButton, _emailButton,
                showPercentagesCheckBox, showComparisonsCheckBox, showChartsCheckBox
            });

            parent.Controls.Add(controlPanel);
        }

        private void CreateTabControl(Panel parent)
        {
            _tabControl = new TabControl
            {
                Location = new Point(20, 180),
                Size = new Size(parent.Width - 40, parent.Height - 220),
                Anchor = AnchorStyles.Left | AnchorStyles.Right | AnchorStyles.Top | AnchorStyles.Bottom,
                Font = new Font("Cairo", 9F)
            };

            // Financial Statement Tab
            var statementTab = new TabPage("البيان المالي");
            _reportPanel = new Panel
            {
                Dock = DockStyle.Fill,
                AutoScroll = true,
                BackColor = Color.White,
                BorderStyle = BorderStyle.FixedSingle
            };
            statementTab.Controls.Add(_reportPanel);
            _tabControl.TabPages.Add(statementTab);

            // Summary & KPIs Tab
            var summaryTab = new TabPage("الملخص والمؤشرات");
            _summaryPanel = new Panel
            {
                Dock = DockStyle.Fill,
                AutoScroll = true,
                BackColor = Color.White,
                BorderStyle = BorderStyle.FixedSingle
            };
            summaryTab.Controls.Add(_summaryPanel);
            _tabControl.TabPages.Add(summaryTab);

            // Charts & Analysis Tab
            var chartsTab = new TabPage("الرسوم البيانية والتحليل");
            _chartsPanel = new Panel
            {
                Dock = DockStyle.Fill,
                AutoScroll = true,
                BackColor = Color.White,
                BorderStyle = BorderStyle.FixedSingle
            };
            chartsTab.Controls.Add(_chartsPanel);
            _tabControl.TabPages.Add(chartsTab);

            parent.Controls.Add(_tabControl);
        }

        private void PeriodComboBox_SelectedIndexChanged(object? sender, EventArgs e)
        {
            var selectedPeriod = _periodComboBox.SelectedItem?.ToString();
            var today = DateTime.Today;

            switch (selectedPeriod)
            {
                case "الشهر الحالي":
                    _startDatePicker.Value = new DateTime(today.Year, today.Month, 1);
                    _endDatePicker.Value = today;
                    break;
                case "الربع الحالي":
                    var quarterStart = new DateTime(today.Year, ((today.Month - 1) / 3) * 3 + 1, 1);
                    _startDatePicker.Value = quarterStart;
                    _endDatePicker.Value = today;
                    break;
                case "السنة الحالية":
                    _startDatePicker.Value = new DateTime(today.Year, 1, 1);
                    _endDatePicker.Value = today;
                    break;
                case "الشهر السابق":
                    var lastMonth = today.AddMonths(-1);
                    _startDatePicker.Value = new DateTime(lastMonth.Year, lastMonth.Month, 1);
                    _endDatePicker.Value = new DateTime(lastMonth.Year, lastMonth.Month, DateTime.DaysInMonth(lastMonth.Year, lastMonth.Month));
                    break;
                case "السنة السابقة":
                    _startDatePicker.Value = new DateTime(today.Year - 1, 1, 1);
                    _endDatePicker.Value = new DateTime(today.Year - 1, 12, 31);
                    break;
            }
        }

        private async void GenerateButton_Click(object? sender, EventArgs e)
        {
            try
            {
                _generateButton.Enabled = false;
                _progressBar.Visible = true;
                this.Cursor = Cursors.WaitCursor;

                await GenerateIncomeStatementAsync();
                await GenerateSummaryAsync();
                await GenerateChartsAsync();

                _printButton.Enabled = true;
                _exportButton.Enabled = true;
                _emailButton.Enabled = true;
            }
            catch (Exception ex)
            {
                MessageBox.Show($"حدث خطأ أثناء إنشاء قائمة الدخل: {ex.Message}",
                    "خطأ", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
            finally
            {
                _generateButton.Enabled = true;
                _progressBar.Visible = false;
                this.Cursor = Cursors.Default;
            }
        }

        private async Task GenerateIncomeStatementAsync()
        {
            var startDate = _startDatePicker.Value.Date;
            var endDate = _endDatePicker.Value.Date;
            
            var incomeStatement = await _financialService.GenerateIncomeStatementAsync(startDate, endDate);
            _currentIncomeStatement = incomeStatement;

            _reportPanel.Controls.Clear();

            // Report Header
            AddReportHeader(_reportPanel, startDate, endDate);

            int y = 100;

            // Revenue Section
            y = AddSection(_reportPanel, "الإيرادات", y, Color.FromArgb(40, 167, 69));
            y = AddFinancialLine(_reportPanel, "إيرادات المبيعات", incomeStatement.SalesRevenue, y);
            y = AddFinancialLine(_reportPanel, "إيرادات أخرى", incomeStatement.OtherRevenue, y);
            y = AddTotalLine(_reportPanel, "إجمالي الإيرادات", incomeStatement.TotalRevenue, y, Color.FromArgb(40, 167, 69));
            y += 20;

            // Cost of Goods Sold
            y = AddSection(_reportPanel, "تكلفة المبيعات", y, Color.FromArgb(220, 53, 69));
            y = AddFinancialLine(_reportPanel, "تكلفة البضاعة المباعة", incomeStatement.CostOfGoodsSold, y);
            y = AddTotalLine(_reportPanel, "إجمالي تكلفة المبيعات", incomeStatement.CostOfGoodsSold, y, Color.FromArgb(220, 53, 69));
            y += 20;

            // Gross Profit
            y = AddTotalLine(_reportPanel, "إجمالي الربح", incomeStatement.GrossProfit, y, Color.FromArgb(0, 123, 255), true);
            y = AddPercentageLine(_reportPanel, $"هامش الربح الإجمالي: {incomeStatement.GrossProfitMargin:F2}%", y);
            y += 30;

            // Operating Expenses
            y = AddSection(_reportPanel, "المصروفات التشغيلية", y, Color.FromArgb(255, 193, 7));
            y = AddFinancialLine(_reportPanel, "مصروف الرواتب", incomeStatement.SalariesExpense, y);
            y = AddFinancialLine(_reportPanel, "مصروف الإهلاك", incomeStatement.DepreciationExpense, y);
            y = AddFinancialLine(_reportPanel, "مصروف المرافق", incomeStatement.UtilitiesExpense, y);
            y = AddFinancialLine(_reportPanel, "مصروف الصيانة", incomeStatement.MaintenanceExpense, y);
            y = AddFinancialLine(_reportPanel, "مصروفات تشغيلية أخرى", incomeStatement.OtherOperatingExpenses, y);
            y = AddTotalLine(_reportPanel, "إجمالي المصروفات التشغيلية", incomeStatement.TotalOperatingExpenses, y, Color.FromArgb(255, 193, 7));
            y += 20;

            // Operating Profit
            y = AddTotalLine(_reportPanel, "الربح التشغيلي", incomeStatement.OperatingProfit, y, Color.FromArgb(0, 123, 255), true);
            y = AddPercentageLine(_reportPanel, $"هامش الربح التشغيلي: {incomeStatement.OperatingProfitMargin:F2}%", y);
            y += 30;

            // Other Income/Expenses
            if (incomeStatement.InterestIncome > 0 || incomeStatement.InterestExpense > 0 || 
                incomeStatement.OtherIncome > 0 || incomeStatement.OtherExpenses > 0)
            {
                y = AddSection(_reportPanel, "الإيرادات والمصروفات الأخرى", y, Color.FromArgb(108, 117, 125));
                y = AddFinancialLine(_reportPanel, "إيرادات الفوائد", incomeStatement.InterestIncome, y);
                y = AddFinancialLine(_reportPanel, "مصروف الفوائد", -incomeStatement.InterestExpense, y);
                y = AddFinancialLine(_reportPanel, "إيرادات أخرى", incomeStatement.OtherIncome, y);
                y = AddFinancialLine(_reportPanel, "مصروفات أخرى", -incomeStatement.OtherExpenses, y);
                y += 20;
            }

            // Net Profit Before Tax
            y = AddTotalLine(_reportPanel, "صافي الربح قبل الضريبة", incomeStatement.NetProfitBeforeTax, y, Color.FromArgb(0, 123, 255), true);
            y += 20;

            // Income Tax
            if (incomeStatement.IncomeTax > 0)
            {
                y = AddFinancialLine(_reportPanel, "ضريبة الدخل", incomeStatement.IncomeTax, y);
                y += 20;
            }

            // Net Profit
            y = AddTotalLine(_reportPanel, "صافي الربح", incomeStatement.NetProfit, y, 
                incomeStatement.NetProfit >= 0 ? Color.FromArgb(40, 167, 69) : Color.FromArgb(220, 53, 69), true);
            y = AddPercentageLine(_reportPanel, $"هامش صافي الربح: {incomeStatement.NetProfitMargin:F2}%", y);

            // Report Footer
            AddReportFooter(_reportPanel, y + 50);
        }

        private async Task GenerateSummaryAsync()
        {
            _summaryPanel.Controls.Clear();
            var value = _currentIncomeStatement
                ?? throw new InvalidOperationException("Income statement data is not available.");
            
            // Add KPI cards and summary information
            var summaryTitle = new Label
            {
                Text = "ملخص الأداء المالي والمؤشرات الرئيسية",
                Location = new Point(20, 20),
                Size = new Size(600, 30),
                Font = new Font("Cairo", 12F, FontStyle.Bold),
                ForeColor = Color.FromArgb(0, 123, 255),
                TextAlign = ContentAlignment.MiddleRight
            };
            _summaryPanel.Controls.Add(summaryTitle);

            var summary = new Label
            {
                Text = $"إجمالي الإيرادات: {value.TotalRevenue:N2} ر.س\n" +
                       $"إجمالي الربح: {value.GrossProfit:N2} ر.س ({value.GrossProfitMargin:N2}%)\n" +
                       $"المصروفات التشغيلية: {value.TotalOperatingExpenses:N2} ر.س\n" +
                       $"الربح التشغيلي: {value.OperatingProfit:N2} ر.س ({value.OperatingProfitMargin:N2}%)\n" +
                       $"صافي الربح: {value.NetProfit:N2} ر.س ({value.NetProfitMargin:N2}%)",
                Location = new Point(20, 70),
                Size = new Size(700, 220),
                Font = new Font("Cairo", 12F),
                TextAlign = ContentAlignment.TopRight
            };
            _summaryPanel.Controls.Add(summary);

            await Task.CompletedTask;
        }

        private async Task GenerateChartsAsync()
        {
            _chartsPanel.Controls.Clear();
            var value = _currentIncomeStatement
                ?? throw new InvalidOperationException("Income statement data is not available.");
            
            // Create revenue vs expenses summary
            var revenueExpensesLabel = new Label
            {
                Text = $"مقارنة الإيرادات والمصروفات\n\n" +
                       $"الإيرادات: {value.TotalRevenue:N2} ر.س\n" +
                       $"تكلفة المبيعات: {value.CostOfGoodsSold:N2} ر.س\n" +
                       $"المصروفات التشغيلية: {value.TotalOperatingExpenses:N2} ر.س\n" +
                       $"صافي الربح: {value.NetProfit:N2} ر.س",
                Location = new Point(20, 20),
                Size = new Size(600, 400),
                BackColor = Color.White,
                Font = new Font("Cairo", 12F),
                TextAlign = ContentAlignment.MiddleCenter,
                BorderStyle = BorderStyle.FixedSingle
            };

            _chartsPanel.Controls.Add(revenueExpensesLabel);

            await Task.CompletedTask;
        }

        #region Helper Methods for Report Generation

        private void AddReportHeader(Panel panel, DateTime startDate, DateTime endDate)
        {
            var companyLabel = new Label
            {
                Text = "مزرعة الأسماك المتقدمة",
                Location = new Point(20, 20),
                Size = new Size(panel.Width - 40, 30),
                Font = new Font("Cairo", 14F, FontStyle.Bold),
                ForeColor = Color.FromArgb(0, 123, 255),
                TextAlign = ContentAlignment.TopCenter
            };

            var titleLabel = new Label
            {
                Text = "قائمة الدخل",
                Location = new Point(20, 50),
                Size = new Size(panel.Width - 40, 25),
                Font = new Font("Cairo", 11F, FontStyle.Bold),
                TextAlign = ContentAlignment.TopCenter
            };

            var periodLabel = new Label
            {
                Text = $"للفترة من {startDate:dd/MM/yyyy} إلى {endDate:dd/MM/yyyy}",
                Location = new Point(20, 75),
                Size = new Size(panel.Width - 40, 20),
                Font = new Font("Cairo", 9F),
                TextAlign = ContentAlignment.TopCenter,
                ForeColor = Color.Gray
            };

            panel.Controls.AddRange(new Control[] { companyLabel, titleLabel, periodLabel });
        }

        private int AddSection(Panel panel, string title, int y, Color color)
        {
            var sectionHeader = new Label
            {
                Text = title,
                Location = new Point(50, y),
                Size = new Size(panel.Width - 100, 25),
                Font = new Font("Cairo", 10F, FontStyle.Bold),
                ForeColor = color,
                TextAlign = ContentAlignment.MiddleRight
            };

            var line = new Panel
            {
                Location = new Point(50, y + 27),
                Size = new Size(panel.Width - 100, 2),
                BackColor = color
            };

            panel.Controls.AddRange(new Control[] { sectionHeader, line });
            return y + 35;
        }

        private int AddFinancialLine(Panel panel, string description, decimal amount, int y)
        {
            var descLabel = new Label
            {
                Text = description,
                Location = new Point(panel.Width - 450, y),
                Size = new Size(300, 25),
                Font = new Font("Cairo", 9F),
                TextAlign = ContentAlignment.MiddleRight
            };

            var amountLabel = new Label
            {
                Text = $"{amount:N2} ريال",
                Location = new Point(100, y),
                Size = new Size(150, 25),
                Font = new Font("Cairo", 9F),
                TextAlign = ContentAlignment.MiddleRight,
                ForeColor = amount >= 0 ? Color.Black : Color.FromArgb(220, 53, 69)
            };

            panel.Controls.AddRange(new Control[] { descLabel, amountLabel });
            return y + 30;
        }

        private int AddTotalLine(Panel panel, string description, decimal amount, int y, Color color, bool isBold = false)
        {
            var font = isBold ? new Font("Cairo", 10F, FontStyle.Bold) : new Font("Cairo", 9F, FontStyle.Bold);

            var descLabel = new Label
            {
                Text = description,
                Location = new Point(panel.Width - 450, y),
                Size = new Size(300, 25),
                Font = font,
                ForeColor = color,
                TextAlign = ContentAlignment.MiddleRight
            };

            var amountLabel = new Label
            {
                Text = $"{amount:N2} ريال",
                Location = new Point(100, y),
                Size = new Size(150, 25),
                Font = font,
                ForeColor = color,
                TextAlign = ContentAlignment.MiddleRight
            };

            var underline = new Panel
            {
                Location = new Point(100, y + 27),
                Size = new Size(350, 2),
                BackColor = color
            };

            panel.Controls.AddRange(new Control[] { descLabel, amountLabel, underline });
            return y + 35;
        }

        private int AddPercentageLine(Panel panel, string text, int y)
        {
            var label = new Label
            {
                Text = text,
                Location = new Point(panel.Width - 300, y),
                Size = new Size(250, 20),
                Font = new Font("Cairo", 8F, FontStyle.Italic),
                ForeColor = Color.Gray,
                TextAlign = ContentAlignment.MiddleRight
            };

            panel.Controls.Add(label);
            return y + 25;
        }

        private void AddReportFooter(Panel panel, int y)
        {
            var dateLabel = new Label
            {
                Text = $"تم إنشاء التقرير في: {DateTime.Now:dd/MM/yyyy HH:mm}",
                Location = new Point(50, y),
                Size = new Size(300, 20),
                Font = new Font("Cairo", 7F),
                ForeColor = Color.Gray,
                TextAlign = ContentAlignment.MiddleLeft
            };

            var pageLabel = new Label
            {
                Text = "صفحة 1 من 1",
                Location = new Point(panel.Width - 200, y),
                Size = new Size(150, 20),
                Font = new Font("Cairo", 7F),
                ForeColor = Color.Gray,
                TextAlign = ContentAlignment.MiddleRight
            };

            panel.Controls.AddRange(new Control[] { dateLabel, pageLabel });
        }

        #endregion

        #region Event Handlers

        private void PrintButton_Click(object? sender, EventArgs e)
        {
            try
            {
                // الحصول على البيانات من التقرير الحالي
                var dataTable = GetIncomeStatementDataTable();
                
                if (dataTable == null || dataTable.Rows.Count == 0)
                {
                    MessageBox.Show("لا توجد بيانات للطباعة. الرجاء إنشاء التقرير أولاً.",
                        "تنبيه", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    return;
                }

                var pdfPath = _printService.CreateFinancialReportPDF(
                    "قائمة الدخل - Income Statement",
                    "AquaFarm Pro",
                    dataTable,
                    _startDatePicker.Value,
                    _endDatePicker.Value
                );

                var result = MessageBox.Show(
                    $"تم إنشاء ملف PDF بنجاح:\n{pdfPath}\n\nهل تريد فتحه؟",
                    "نجح",
                    MessageBoxButtons.YesNo,
                    MessageBoxIcon.Information
                );

                if (result == DialogResult.Yes)
                {
                    _printService.PrintPDF(pdfPath);
                }

                LoggingService.LogInfo("Income statement printed successfully");
            }
            catch (Exception ex)
            {
                LoggingService.LogError(ex, "Error printing income statement");
                MessageBox.Show($"خطأ في الطباعة: {ex.Message}", "خطأ",
                    MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private System.Data.DataTable GetIncomeStatementDataTable()
        {
            var dt = new System.Data.DataTable();
            dt.Columns.Add("البند", typeof(string));
            dt.Columns.Add("المبلغ", typeof(decimal));
            var value = _currentIncomeStatement
                ?? throw new InvalidOperationException("Generate the income statement before exporting it.");
            dt.Rows.Add("إيرادات المبيعات", value.SalesRevenue);
            dt.Rows.Add("إيرادات أخرى", value.OtherRevenue);
            dt.Rows.Add("إجمالي الإيرادات", value.TotalRevenue);
            dt.Rows.Add("تكلفة البضاعة المباعة", value.CostOfGoodsSold);
            dt.Rows.Add("إجمالي الربح", value.GrossProfit);
            dt.Rows.Add("مصروف الرواتب", value.SalariesExpense);
            dt.Rows.Add("مصروف الإهلاك", value.DepreciationExpense);
            dt.Rows.Add("مصروف المرافق", value.UtilitiesExpense);
            dt.Rows.Add("مصروف الصيانة", value.MaintenanceExpense);
            dt.Rows.Add("مصروفات تشغيلية أخرى", value.OtherOperatingExpenses);
            dt.Rows.Add("إجمالي المصروفات التشغيلية", value.TotalOperatingExpenses);
            dt.Rows.Add("الربح التشغيلي", value.OperatingProfit);
            dt.Rows.Add("صافي الربح قبل الضريبة", value.NetProfitBeforeTax);
            dt.Rows.Add("ضريبة الدخل", value.IncomeTax);
            dt.Rows.Add("صافي الربح", value.NetProfit);

            return dt;
        }

        private void ExportButton_Click(object? sender, EventArgs e)
        {
            try
            {
                var dataTable = GetIncomeStatementDataTable();
                
                if (dataTable == null || dataTable.Rows.Count == 0)
                {
                    MessageBox.Show("لا توجد بيانات للتصدير. الرجاء إنشاء التقرير أولاً.",
                        "تنبيه", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    return;
                }

                var fileName = $"IncomeStatement_{_startDatePicker.Value:yyyyMMdd}_{_endDatePicker.Value:yyyyMMdd}.xlsx";
                var excelPath = _excelExportService.ExportDataTableToExcel(dataTable, fileName, "قائمة الدخل");

                if (MessageBox.Show($"تم التصدير:\n{excelPath}\n\nفتح؟", "نجح",
                    MessageBoxButtons.YesNo, MessageBoxIcon.Information) == DialogResult.Yes)
                    _excelExportService.OpenExcelFile(excelPath);

                LoggingService.LogInfo("Income statement exported to Excel");
            }
            catch (Exception ex)
            {
                LoggingService.LogError(ex, "Error exporting income statement");
                MessageBox.Show($"خطأ في التصدير: {ex.Message}", "خطأ",
                    MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void EmailButton_Click(object? sender, EventArgs e)
        {
            try
            {
                var value = _currentIncomeStatement
                    ?? throw new InvalidOperationException("أنشئ قائمة الدخل قبل إعداد الرسالة.");
                var subject = Uri.EscapeDataString($"قائمة الدخل {_startDatePicker.Value:yyyy-MM-dd} - {_endDatePicker.Value:yyyy-MM-dd}");
                var body = Uri.EscapeDataString(
                    $"إجمالي الإيرادات: {value.TotalRevenue:N2} ر.س\n" +
                    $"إجمالي المصروفات التشغيلية: {value.TotalOperatingExpenses:N2} ر.س\n" +
                    $"صافي الربح: {value.NetProfit:N2} ر.س\n\n" +
                    "يرجى مراجعة التقرير المعتمد داخل النظام.");
                System.Diagnostics.Process.Start(new System.Diagnostics.ProcessStartInfo
                {
                    FileName = $"mailto:?subject={subject}&body={body}",
                    UseShellExecute = true
                });
            }
            catch (Exception ex)
            {
                LoggingService.LogError(ex, "Error opening income statement email draft");
                MessageBox.Show("تعذر فتح عميل البريد الافتراضي. راجع إعدادات Windows.", "خطأ",
                    MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        #endregion

        protected override void Dispose(bool disposing)
        {
            if (disposing) { }
            base.Dispose(disposing);
        }
    }
}
