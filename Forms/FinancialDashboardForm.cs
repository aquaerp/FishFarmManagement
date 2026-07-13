using System;
using System.Drawing;
using System.Linq;
using System.Threading.Tasks;
using System.Windows.Forms;
using FishFarmManager.Data;
using FishFarmManager.Models;
using FishFarmManager.Services;

namespace FishFarmManager.Forms
{
    /// <summary>
    /// نموذج لوحة التحكم المالية
    /// Financial Dashboard Form
    /// </summary>
    public partial class FinancialDashboardForm : Form
    {
        private readonly FishFarmContext _context;
        private readonly FinancialService _financialService;

        private Panel _kpiPanel = null!;
        private Panel _chartPanel = null!;
        private Button _refreshButton = null!;

        // KPI Labels
        private Label _totalRevenueLabel = null!;
        private Label _netProfitLabel = null!;
        private Label _totalAssetsLabel = null!;
        private Label _currentRatioLabel = null!;
        private Label _roeLabel = null!;
        private Label _roaLabel = null!;

        public FinancialDashboardForm(FishFarmContext context)
        {
            _context = context ?? throw new ArgumentNullException(nameof(context));
            _financialService = new FinancialService(_context);

            if (!AuthenticationService.HasPermission(UserRole.Admin, UserRole.Manager, UserRole.Accountant))
            {
                MessageBox.Show("ليس لديك صلاحية لعرض لوحة التحكم المالية", "تحذير",
                    MessageBoxButtons.OK, MessageBoxIcon.Warning);
                this.Load += (s, e) => this.Close();
                return;
            }

            InitializeComponent();
            InitializeForm();
        }

        private void InitializeComponent()
        {
            this.Text = "لوحة التحكم المالية - Financial Dashboard";
            this.Size = new Size(1600, 900);
            this.StartPosition = FormStartPosition.CenterScreen;
            this.RightToLeft = RightToLeft.Yes;
            this.RightToLeftLayout = true;
            this.Font = new Font("Cairo", 10F);
            this.WindowState = FormWindowState.Maximized;

            var mainPanel = new Panel { Dock = DockStyle.Fill, Padding = new Padding(20), BackColor = Color.FromArgb(240, 240, 240) };

            var titleLabel = new Label
            {
                Text = "لوحة التحكم المالية",
                Location = new Point(20, 10),
                Size = new Size(1540, 40),
                Font = new Font("Cairo", 14F, FontStyle.Bold),
                ForeColor = Color.FromArgb(46, 92, 138),
                TextAlign = ContentAlignment.MiddleRight
            };
            mainPanel.Controls.Add(titleLabel);

            _refreshButton = new Button
            {
                Text = "تحديث البيانات",
                Location = new Point(20, 15),
                Size = new Size(150, 35),
                Font = new Font("Cairo", 10F, FontStyle.Bold),
                BackColor = Color.FromArgb(38, 166, 154),
                ForeColor = Color.White,
                FlatStyle = FlatStyle.Flat,
                Cursor = Cursors.Hand
            };
            _refreshButton.Click += async (s, e) => await RefreshButton_ClickAsync();
            mainPanel.Controls.Add(_refreshButton);

            CreateKPISection(mainPanel, 60);
            CreateChartSection(mainPanel, 220);

            this.Controls.Add(mainPanel);
        }

        private void CreateKPISection(Panel parent, int startY)
        {
            _kpiPanel = new FlowLayoutPanel
            {
                Location = new Point(20, startY),
                Size = new Size(1540, 150),
                FlowDirection = FlowDirection.RightToLeft,
                WrapContents = false,
                BackColor = Color.Transparent
            };

            _kpiPanel.Controls.Add(CreateKPICard("إجمالي الإيرادات", ref _totalRevenueLabel, "0 ريال", Color.FromArgb(38, 166, 154)));
            _kpiPanel.Controls.Add(CreateKPICard("صافي الربح", ref _netProfitLabel, "0 ريال", Color.FromArgb(46, 92, 138)));
            _kpiPanel.Controls.Add(CreateKPICard("إجمالي الأصول", ref _totalAssetsLabel, "0 ريال", Color.FromArgb(255, 152, 0)));
            _kpiPanel.Controls.Add(CreateKPICard("نسبة السيولة", ref _currentRatioLabel, "0.00", Color.FromArgb(74, 144, 226)));
            _kpiPanel.Controls.Add(CreateKPICard("العائد على حقوق الملكية", ref _roeLabel, "0%", Color.FromArgb(156, 39, 176)));
            _kpiPanel.Controls.Add(CreateKPICard("العائد على الأصول", ref _roaLabel, "0%", Color.FromArgb(229, 57, 53)));

            parent.Controls.Add(_kpiPanel);
        }

        private Panel CreateKPICard(string title, ref Label valueLabel, string defaultValue, Color color)
        {
            var card = new Panel
            {
                Size = new Size(240, 130),
                BackColor = Color.White,
                BorderStyle = BorderStyle.FixedSingle,
                Margin = new Padding(5)
            };

            var titleLabel = new Label
            {
                Text = title,
                Location = new Point(10, 15),
                Size = new Size(220, 30),
                Font = new Font("Cairo", 9F),
                ForeColor = Color.Gray,
                TextAlign = ContentAlignment.MiddleCenter
            };
            card.Controls.Add(titleLabel);

            valueLabel = new Label
            {
                Text = defaultValue,
                Location = new Point(10, 50),
                Size = new Size(220, 50),
                Font = new Font("Cairo", 18F, FontStyle.Bold),
                ForeColor = color,
                TextAlign = ContentAlignment.MiddleCenter
            };
            card.Controls.Add(valueLabel);

            return card;
        }

        private void CreateChartSection(Panel parent, int startY)
        {
            _chartPanel = new Panel
            {
                Location = new Point(20, startY),
                Size = new Size(1540, 600),
                BackColor = Color.White,
                BorderStyle = BorderStyle.FixedSingle
            };

            var chart1 = CreateChart("الإيرادات والمصروفات", 20, 20, 730, 270);
            _chartPanel.Controls.Add(chart1);

            var chart2 = CreateChart("النسب المالية", 780, 20, 730, 270);
            _chartPanel.Controls.Add(chart2);

            var chart3 = CreateChart("الربحية الشهرية", 20, 310, 730, 270);
            _chartPanel.Controls.Add(chart3);

            var chart4 = CreateChart("الأصول والخصوم", 780, 310, 730, 270);
            _chartPanel.Controls.Add(chart4);

            parent.Controls.Add(_chartPanel);
        }

        private Panel CreateChart(string title, int x, int y, int width, int height)
        {
            var panel = new Panel
            {
                Location = new Point(x, y),
                Size = new Size(width, height),
                BackColor = Color.White,
                BorderStyle = BorderStyle.FixedSingle
            };

            var titleLabel = new Label
            {
                Text = title,
                Location = new Point(10, 10),
                Size = new Size(width - 20, 30),
                Font = new Font("Cairo", 11F, FontStyle.Bold),
                ForeColor = Color.FromArgb(46, 92, 138),
                TextAlign = ContentAlignment.MiddleCenter
            };
            panel.Controls.Add(titleLabel);

            var contentLabel = new Label
            {
                Text = "البيانات ستظهر هنا",
                Location = new Point(10, 50),
                Size = new Size(width - 20, height - 60),
                Font = new Font("Cairo", 10F),
                ForeColor = Color.Gray,
                TextAlign = ContentAlignment.MiddleCenter
            };
            panel.Controls.Add(contentLabel);

            return panel;
        }

        private void InitializeForm()
        {
            _ = LoadDashboardDataAsync();
            LoggingService.LogInfo("FinancialDashboardForm initialized");
        }

        private async Task RefreshButton_ClickAsync()
        {
            await LoadDashboardDataAsync();
        }

        private async Task LoadDashboardDataAsync()
        {
            try
            {
                _refreshButton.Enabled = false;
                Cursor = Cursors.WaitCursor;

                var today = DateTime.Now.Date;
                var startOfYear = new DateTime(today.Year, 1, 1);

                // Load Income Statement
                var incomeStatement = await _financialService.GenerateIncomeStatementAsync(startOfYear, today);
                _totalRevenueLabel.Text = incomeStatement.TotalRevenue.ToString("N0") + " ريال";
                _netProfitLabel.Text = incomeStatement.NetProfit.ToString("N0") + " ريال";

                // Load Balance Sheet
                var balanceSheet = await _financialService.GenerateBalanceSheetAsync(today);
                _totalAssetsLabel.Text = balanceSheet.TotalAssets.ToString("N0") + " ريال";
                _currentRatioLabel.Text = balanceSheet.TotalCurrentLiabilities > 0
                    ? (balanceSheet.TotalCurrentAssets / balanceSheet.TotalCurrentLiabilities).ToString("N2")
                    : "∞";

                // Load Financial Ratios
                var ratios = await _financialService.CalculateFinancialRatiosAsync(today);
                _roeLabel.Text = ratios.ROE.ToString("N1") + "%";
                _roaLabel.Text = ratios.ROA.ToString("N1") + "%";

                // Load Charts
                await LoadChartsDataAsync(incomeStatement, balanceSheet, ratios);

                LoggingService.LogInfo("Dashboard data loaded successfully");
            }
            catch (Exception ex)
            {
                LoggingService.LogError("Error loading dashboard data", ex);
                MessageBox.Show($"حدث خطأ: {ex.Message}", "خطأ", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
            finally
            {
                _refreshButton.Enabled = true;
                Cursor = Cursors.Default;
            }
        }

        private async Task LoadChartsDataAsync(IncomeStatementData incomeStatement, BalanceSheetData balanceSheet, FinancialRatios ratios)
        {
            var panels = _chartPanel.Controls.OfType<Panel>().ToArray();

            // Panel 1: الإيرادات والمصروفات
            var revenueExpensePanel = panels.FirstOrDefault(p => p.Controls.OfType<Label>().Any(l => l.Text == "الإيرادات والمصروفات"));
            if (revenueExpensePanel != null)
            {
                LoadRevenueExpensePanel(revenueExpensePanel, incomeStatement);
            }

            // Panel 2: النسب المالية
            var ratiosPanel = panels.FirstOrDefault(p => p.Controls.OfType<Label>().Any(l => l.Text == "النسب المالية"));
            if (ratiosPanel != null)
            {
                LoadRatiosPanel(ratiosPanel, ratios);
            }

            // Panel 3: الربحية الشهرية
            var profitabilityPanel = panels.FirstOrDefault(p => p.Controls.OfType<Label>().Any(l => l.Text == "الربحية الشهرية"));
            if (profitabilityPanel != null)
            {
                await LoadMonthlyProfitabilityPanelAsync(profitabilityPanel);
            }

            // Panel 4: الأصول والخصوم
            var assetsLiabilitiesPanel = panels.FirstOrDefault(p => p.Controls.OfType<Label>().Any(l => l.Text == "الأصول والخصوم"));
            if (assetsLiabilitiesPanel != null)
            {
                LoadAssetsLiabilitiesPanel(assetsLiabilitiesPanel, balanceSheet);
            }
        }

        private void LoadRevenueExpensePanel(Panel panel, IncomeStatementData data)
        {
            var contentLabel = panel.Controls.OfType<Label>().FirstOrDefault(l => l.Text == "البيانات ستظهر هنا");
            if (contentLabel != null)
            {
                contentLabel.Text = $"إجمالي الإيرادات: {data.TotalRevenue:N0} ريال\n" +
                                  $"تكلفة المبيعات: {data.CostOfGoodsSold:N0} ريال\n" +
                                  $"المصروفات التشغيلية: {data.TotalOperatingExpenses:N0} ريال\n" +
                                  $"صافي الربح: {data.NetProfit:N0} ريال";
            }
        }

        private void LoadRatiosPanel(Panel panel, FinancialRatios ratios)
        {
            var contentLabel = panel.Controls.OfType<Label>().FirstOrDefault(l => l.Text == "البيانات ستظهر هنا");
            if (contentLabel != null)
            {
                contentLabel.Text = $"ROE: {ratios.ROE:N1}%\n" +
                                  $"ROA: {ratios.ROA:N1}%\n" +
                                  $"هامش الربح الإجمالي: {ratios.GrossProfitMargin:N1}%\n" +
                                  $"هامش الربح التشغيلي: {ratios.OperatingProfitMargin:N1}%\n" +
                                  $"نسبة السيولة: {ratios.CurrentRatio:N2}";
            }
        }

        private async Task LoadMonthlyProfitabilityPanelAsync(Panel panel)
        {
            var contentLabel = panel.Controls.OfType<Label>().FirstOrDefault(l => l.Text == "البيانات ستظهر هنا");
            if (contentLabel != null)
            {
                var currentYear = DateTime.Now.Year;
                var monthlyData = new List<string>();
                
                for (int month = 1; month <= 12; month++)
                {
                    var startDate = new DateTime(currentYear, month, 1);
                    var endDate = startDate.AddMonths(1).AddDays(-1);
                    
                    if (endDate > DateTime.Now) endDate = DateTime.Now;

                    var monthlyIncome = await _financialService.GenerateIncomeStatementAsync(startDate, endDate);
                    monthlyData.Add($"{month}: {monthlyIncome.NetProfit:N0} ريال");
                }
                
                contentLabel.Text = string.Join("\n", monthlyData);
            }
        }

        private void LoadAssetsLiabilitiesPanel(Panel panel, BalanceSheetData data)
        {
            var contentLabel = panel.Controls.OfType<Label>().FirstOrDefault(l => l.Text == "البيانات ستظهر هنا");
            if (contentLabel != null)
            {
                contentLabel.Text = $"الأصول المتداولة: {data.TotalCurrentAssets:N0} ريال\n" +
                                  $"الأصول الثابتة: {data.FixedAssetsNet:N0} ريال\n" +
                                  $"إجمالي الأصول: {data.TotalAssets:N0} ريال\n" +
                                  $"الخصوم المتداولة: {data.TotalCurrentLiabilities:N0} ريال\n" +
                                  $"إجمالي الخصوم: {data.TotalLiabilities:N0} ريال";
            }
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing) { }
            base.Dispose(disposing);
        }
    }
}


