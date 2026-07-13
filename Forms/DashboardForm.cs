using System;
using System.Drawing;
using System.Linq;
using System.Windows.Forms;
using Microsoft.EntityFrameworkCore;
using FishFarmManager.Data;
using FishFarmManager.Models;
using FishFarmManager.Services;

namespace FishFarmManager.Forms
{
    public partial class DashboardForm : Form
    {
        private readonly FishFarmContext _context = null!;
        private readonly PerformanceCalculator _performanceCalculator = null!;
        private readonly NotificationService _notificationService = null!;
        private TabControl _tabControl = null!;

        public DashboardForm(FishFarmContext context, PerformanceCalculator performanceCalculator, 
                           NotificationService notificationService)
        {
            // ✅ فحص المصادقة - Dashboard متاح للمستخدمين المسجلين فقط
            if (AuthenticationService.CurrentUser == null)
            {
                MessageBox.Show(
                    "يجب تسجيل الدخول أولاً للوصول إلى لوحة التحكم.",
                    "خطأ في المصادقة",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Error
                );
                
                this.Load += (s, e) => this.Close();
                return;
            }

            _context = context;
            _performanceCalculator = performanceCalculator;
            _notificationService = notificationService;
            InitializeComponent();
            LoadDashboardData();
            
            // Apply AquaFarm Pro theme
            try { ThemeManager.ApplyTheme(this); } catch { }
            
            // ✅ عرض اسم المستخدم والدور في العنوان
            this.Text = $"لوحة التحكم - {AuthenticationService.CurrentUserFullName} ({AuthenticationService.CurrentUserRole})";
            
            LoggingService.LogInfo(
                "فتح لوحة التحكم للمستخدم: {Username} ({Role})",
                AuthenticationService.CurrentUsername,
                AuthenticationService.CurrentUserRole
            );
        }

        private void InitializeComponent()
        {
            this.SuspendLayout();
            
            this.Text = "لوحة التحكم والتقارير";
            this.Size = new Size(1400, 900);
            this.StartPosition = FormStartPosition.CenterScreen;
            this.WindowState = FormWindowState.Maximized;
            this.RightToLeft = RightToLeft.Yes;
            this.RightToLeftLayout = true;
            try { this.Font = new Font("Cairo", 10F, FontStyle.Regular); } catch { }

            // إنشاء التبويبات
            _tabControl = new TabControl();
            _tabControl.Dock = DockStyle.Fill;
            
            // تبويب المؤشرات الرئيسية
            var overviewTab = new TabPage("نظرة عامة");
            CreateOverviewTab(overviewTab);
            _tabControl.TabPages.Add(overviewTab);
            
            // تبويب تقارير الأداء
            var performanceTab = new TabPage("تقارير الأداء");
            CreatePerformanceTab(performanceTab);
            _tabControl.TabPages.Add(performanceTab);
            
            // تبويب التنبيهات
            var alertsTab = new TabPage("التنبيهات");
            CreateAlertsTab(alertsTab);
            _tabControl.TabPages.Add(alertsTab);

            this.Controls.Add(_tabControl);
            this.ResumeLayout(false);
        }

        private void CreateOverviewTab(TabPage tab)
        {
            var panel = new Panel();
            panel.Dock = DockStyle.Fill;
            panel.AutoScroll = true;
            panel.BackColor = ThemeManager.NeutralLightGray;

            var y = 20;
            var cardWidth = 280;
            var cardHeight = 150;
            var margin = 20;

            // الصف الأول من البطاقات
            var pondsCard = CreateStatCard("إجمالي الأحواض", GetTotalPonds().ToString(), 
                                         $"نشط: {GetActivePonds()}", ThemeManager.SecondarySkyBlue, 
                                         margin, y, cardWidth, cardHeight);
            panel.Controls.Add(pondsCard);

            var cyclesCard = CreateStatCard("الدورات النشطة", GetActiveCycles().ToString(), 
                                          $"مكتملة: {GetCompletedCycles()}", ThemeManager.SecondaryAquaGreen, 
                                          margin + cardWidth + margin, y, cardWidth, cardHeight);
            panel.Controls.Add(cyclesCard);

            var fishCard = CreateStatCard("إجمالي الأسماك", GetTotalFish().ToString(), 
                                        $"متوسط الوزن: {GetAverageWeight():F1} جم", ThemeManager.SecondarySkyBlue, 
                                        margin + (cardWidth + margin) * 2, y, cardWidth, cardHeight);
            panel.Controls.Add(fishCard);

            var productionCard = CreateStatCard("الإنتاج المتوقع", $"{GetExpectedProduction():F1} كجم", 
                                              $"القيمة: {GetExpectedValue():F0} ريال", ThemeManager.SecondaryAquaGreen, 
                                              margin + (cardWidth + margin) * 3, y, cardWidth, cardHeight);
            panel.Controls.Add(productionCard);

            y += cardHeight + margin * 2;

            // الصف الثاني من البطاقات
            var feedCostCard = CreateStatCard("تكلفة الأعلاف (شهر)", $"{GetMonthlyFeedCost():F0} ريال", 
                                            $"متوسط FCR: {GetAverageFCR():F2}", ThemeManager.SecondarySkyBlue, 
                                            margin, y, cardWidth, cardHeight);
            panel.Controls.Add(feedCostCard);

            var mortalityCard = CreateStatCard("معدل النفوق (شهر)", $"{GetMonthlyMortalityRate():F1}%", 
                                             $"إجمالي النافق: {GetMonthlyMortality()}", ThemeManager.SecondaryAquaGreen, 
                                             margin + cardWidth + margin, y, cardWidth, cardHeight);
            panel.Controls.Add(mortalityCard);

            var waterQualityCard = CreateStatCard("جودة المياه", GetWaterQualityStatus(), 
                                                $"آخر فحص: {GetLastWaterTest()}", ThemeManager.SecondarySkyBlue, 
                                                margin + (cardWidth + margin) * 2, y, cardWidth, cardHeight);
            panel.Controls.Add(waterQualityCard);

            var alertsCard = CreateStatCard("التنبيهات", GetActiveAlerts().ToString(), 
                                          "تحتاج متابعة", ThemeManager.SecondaryAquaGreen, 
                                          margin + (cardWidth + margin) * 3, y, cardWidth, cardHeight);
            panel.Controls.Add(alertsCard);

            y += cardHeight + margin * 2;

            // جدول الدورات النشطة
            var activeCyclesLabel = new Label 
            { 
                Text = "الدورات النشطة", 
                Font = new Font("Arial", 14, FontStyle.Bold),
                Location = new Point(margin, y),
                Size = new Size(200, 30)
            };
            panel.Controls.Add(activeCyclesLabel);
            y += 40;

            var activeCyclesGrid = CreateActiveCyclesGrid();
            activeCyclesGrid.Location = new Point(margin, y);
            activeCyclesGrid.Size = new Size(1200, 200);
            panel.Controls.Add(activeCyclesGrid);

            tab.Controls.Add(panel);
        }

        private void CreatePerformanceTab(TabPage tab)
        {
            var panel = new Panel();
            panel.Dock = DockStyle.Fill;
            panel.AutoScroll = true;

            var y = 20;
            var margin = 20;

            // تقرير الأداء للدورات المكتملة
            var performanceLabel = new Label 
            { 
                Text = "تقرير أداء الدورات المكتملة", 
                Font = new Font("Arial", 14, FontStyle.Bold),
                Location = new Point(margin, y),
                Size = new Size(300, 30)
            };
            panel.Controls.Add(performanceLabel);
            y += 40;

            var performanceGrid = CreatePerformanceGrid();
            performanceGrid.Location = new Point(margin, y);
            performanceGrid.Size = new Size(1200, 300);
            panel.Controls.Add(performanceGrid);
            y += 320;

            // إحصائيات الأداء
            var statsLabel = new Label 
            { 
                Text = "إحصائيات الأداء", 
                Font = new Font("Arial", 14, FontStyle.Bold),
                Location = new Point(margin, y),
                Size = new Size(200, 30)
            };
            panel.Controls.Add(statsLabel);
            y += 40;

            var statsTextBox = new TextBox 
            { 
                Location = new Point(margin, y),
                Size = new Size(800, 200),
                Multiline = true,
                ReadOnly = true,
                ScrollBars = ScrollBars.Vertical,
                Text = GetPerformanceStats()
            };
            panel.Controls.Add(statsTextBox);

            tab.Controls.Add(panel);
        }

        private void CreateFinancialTab(TabPage tab)
        {
            var panel = new Panel();
            panel.Dock = DockStyle.Fill;
            panel.AutoScroll = true;
        }
        
          private void CreateAlertsTab(TabPage tab)
        {
            var panel = new Panel();
            panel.Dock = DockStyle.Fill;
            panel.AutoScroll = true;

            var y = 20;
            var margin = 20;

            // التنبيهات
            var alertsLabel = new Label 
            { 
                Text = "التنبيهات والتحذيرات", 
                Font = new Font("Arial", 14, FontStyle.Bold),
                Location = new Point(margin, y),
                Size = new Size(200, 30)
            };
            panel.Controls.Add(alertsLabel);
            y += 40;

            var alertsListBox = new ListBox 
            { 
                Location = new Point(margin, y),
                Size = new Size(800, 400),
                Font = new Font("Arial", 10)
            };

            var alerts = _notificationService.GetAllAlerts();
            foreach (var alert in alerts)
            {
                alertsListBox.Items.Add(alert);
            }

            if (alerts.Count == 0)
            {
                alertsListBox.Items.Add("لا توجد تنبيهات حالياً");
            }

            panel.Controls.Add(alertsListBox);

            // زر تحديث التنبيهات
            var refreshButton = new Button 
            { 
                Text = "تحديث التنبيهات",
                Location = new Point(margin, y + 420),
                Size = new Size(120, 30),
                BackColor = ThemeManager.SecondarySkyBlue,
                ForeColor = ThemeManager.PureWhite,
            };
            refreshButton.Click += (s, e) => {
                alertsListBox.Items.Clear();
                var newAlerts = _notificationService.GetAllAlerts();
                foreach (var alert in newAlerts)
                {
                    alertsListBox.Items.Add(alert);
                }
                if (newAlerts.Count == 0)
                {
                    alertsListBox.Items.Add("لا توجد تنبيهات حالياً");
                }
            };
            try { ThemeManager.StyleSecondaryButton(refreshButton); } catch { }
            panel.Controls.Add(refreshButton);

            tab.Controls.Add(panel);
        }

        private Panel CreateStatCard(string title, string value, string subtitle, Color color, 
                                   int x, int y, int width, int height)
        {
            var card = new Panel();
            card.Size = new Size(width, height);
            card.Location = new Point(x, y);
            // Card styling aligned with theme
            card.BackColor = ThemeManager.PureWhite;
            card.BorderStyle = BorderStyle.FixedSingle;

            var titleLabel = new Label();
            titleLabel.Text = title;
            try { titleLabel.Font = new Font(this.Font.FontFamily, 12, FontStyle.Bold); } catch { }
            titleLabel.ForeColor = ThemeManager.PrimaryDeepBlue;
            titleLabel.Location = new Point(10, 10);
            titleLabel.Size = new Size(width - 20, 25);
            titleLabel.TextAlign = ContentAlignment.MiddleCenter;

            var valueLabel = new Label();
            valueLabel.Text = value;
            try { valueLabel.Font = new Font(this.Font.FontFamily, 24, FontStyle.Bold); } catch { }
            valueLabel.ForeColor = color; // use passed color as accent
            valueLabel.Location = new Point(10, 40);
            valueLabel.Size = new Size(width - 20, 50);
            valueLabel.TextAlign = ContentAlignment.MiddleCenter;

            var subtitleLabel = new Label();
            subtitleLabel.Text = subtitle;
            try { subtitleLabel.Font = new Font(this.Font.FontFamily, 10, FontStyle.Regular); } catch { }
            subtitleLabel.Location = new Point(10, 100);
            subtitleLabel.Size = new Size(width - 20, 40);
            subtitleLabel.TextAlign = ContentAlignment.MiddleCenter;

            card.Controls.Add(titleLabel);
            card.Controls.Add(valueLabel);
            card.Controls.Add(subtitleLabel);

            return card;
        }

        private DataGridView CreateActiveCyclesGrid()
        {
            var grid = new DataGridView();
            grid.AutoGenerateColumns = false;
            grid.ReadOnly = true;
            grid.AllowUserToAddRows = false;

            grid.Columns.Add(new DataGridViewTextBoxColumn { DataPropertyName = "Name", HeaderText = "اسم الدورة", Width = 150 });
            grid.Columns.Add(new DataGridViewTextBoxColumn { DataPropertyName = "PondName", HeaderText = "الحوض", Width = 120 });
            grid.Columns.Add(new DataGridViewTextBoxColumn { DataPropertyName = "StartDate", HeaderText = "تاريخ البداية", Width = 100, DefaultCellStyle = new DataGridViewCellStyle { Format = "yyyy/MM/dd" } });
            grid.Columns.Add(new DataGridViewTextBoxColumn { DataPropertyName = "DaysRunning", HeaderText = "الأيام", Width = 80 });
            grid.Columns.Add(new DataGridViewTextBoxColumn { DataPropertyName = "InitialFishCount", HeaderText = "عدد الأسماك", Width = 100 });
            grid.Columns.Add(new DataGridViewTextBoxColumn { DataPropertyName = "EstimatedWeight", HeaderText = "الوزن المقدر", Width = 100 });

            var activeCycles = _context.ProductionCycles
                .Include(c => c.ProductionCyclePonds)
                .ThenInclude(pcp => pcp.Pond)
                .Where(c => c.Status == CycleStatus.Active)
                .Select(c => new
                {
                    c.Name,
                    PondName = string.Join(", ", c.ProductionCyclePonds.Select(pcp => pcp.Pond.Name)),
                    c.StartDate,
                    DaysRunning = (DateTime.Now - c.StartDate).Days,
                    c.InitialFishCount,
                    EstimatedWeight = c.InitialAverageWeight + ((DateTime.Now - c.StartDate).Days * 2) // تقدير نمو 2 جم يومياً
                })
                .ToList();

            grid.DataSource = activeCycles;
            return grid;
        }

        private DataGridView CreatePerformanceGrid()
        {
            var grid = new DataGridView();
            grid.AutoGenerateColumns = false;
            grid.ReadOnly = true;
            grid.AllowUserToAddRows = false;

            grid.Columns.Add(new DataGridViewTextBoxColumn { DataPropertyName = "Name", HeaderText = "اسم الدورة", Width = 150 });
            grid.Columns.Add(new DataGridViewTextBoxColumn { DataPropertyName = "PondName", HeaderText = "الحوض", Width = 120 });
            grid.Columns.Add(new DataGridViewTextBoxColumn { DataPropertyName = "Duration", HeaderText = "المدة (يوم)", Width = 100 });
            grid.Columns.Add(new DataGridViewTextBoxColumn { DataPropertyName = "SurvivalRate", HeaderText = "معدل البقاء %", Width = 100, DefaultCellStyle = new DataGridViewCellStyle { Format = "F1" } });
            grid.Columns.Add(new DataGridViewTextBoxColumn { DataPropertyName = "FCR", HeaderText = "FCR", Width = 80, DefaultCellStyle = new DataGridViewCellStyle { Format = "F2" } });
            grid.Columns.Add(new DataGridViewTextBoxColumn { DataPropertyName = "ADG", HeaderText = "ADG", Width = 80, DefaultCellStyle = new DataGridViewCellStyle { Format = "F2" } });
            grid.Columns.Add(new DataGridViewTextBoxColumn { DataPropertyName = "TotalHarvestWeight", HeaderText = "وزن الحصاد", Width = 120, DefaultCellStyle = new DataGridViewCellStyle { Format = "F1" } });

            var completedCycles = _context.ProductionCycles
                .Include(c => c.ProductionCyclePonds)
                .ThenInclude(pcp => pcp.Pond)
                .Where(c => c.Status == CycleStatus.Completed && c.EndDate.HasValue)
                .Select(c => new
                {
                    c.Name,
                    PondName = string.Join(", ", c.ProductionCyclePonds.Select(pcp => pcp.Pond.Name)),
                    Duration = c.EndDate.HasValue ? (c.EndDate.Value - c.StartDate).Days : 0,
                    c.SurvivalRate,
                    c.FCR,
                    c.ADG,
                    c.TotalHarvestWeight
                })
                .ToList();

            grid.DataSource = completedCycles;
            return grid;
        }

        private DataGridView CreateFinancialGrid()
        {
            var grid = new DataGridView();
            grid.AutoGenerateColumns = false;
            grid.ReadOnly = true;
            grid.AllowUserToAddRows = false;

            grid.Columns.Add(new DataGridViewTextBoxColumn { DataPropertyName = "CycleName", HeaderText = "الدورة", Width = 150 });
            grid.Columns.Add(new DataGridViewTextBoxColumn { DataPropertyName = "FeedCost", HeaderText = "تكلفة الأعلاف", Width = 120, DefaultCellStyle = new DataGridViewCellStyle { Format = "F0" } });
            grid.Columns.Add(new DataGridViewTextBoxColumn { DataPropertyName = "Revenue", HeaderText = "الإيرادات", Width = 120, DefaultCellStyle = new DataGridViewCellStyle { Format = "F0" } });
            grid.Columns.Add(new DataGridViewTextBoxColumn { DataPropertyName = "Profit", HeaderText = "الربح", Width = 120, DefaultCellStyle = new DataGridViewCellStyle { Format = "F0" } });
            grid.Columns.Add(new DataGridViewTextBoxColumn { DataPropertyName = "ProfitMargin", HeaderText = "هامش الربح %", Width = 120, DefaultCellStyle = new DataGridViewCellStyle { Format = "F1" } });

            var completedCycles = _context.ProductionCycles
                .Where(c => c.Status == CycleStatus.Completed)
                .ToList();

            var financialData = completedCycles.Select(c =>
            {
                var feedCost = (decimal)_context.FeedingRecords
                    .Where(f => f.CycleId == c.Id)
                    .Sum(f => f.Quantity * f.FeedPrice);
                var revenue = (c.TotalHarvestWeight ?? 0) * 15m; // سعر افتراضي 15 ريال للكيلو
                var profit = revenue - feedCost;
                var profitMargin = revenue > 0 ? (profit / revenue) * 100 : 0;

                return new
                {
                    CycleName = c.Name,
                    FeedCost = feedCost,
                    Revenue = revenue,
                    Profit = profit,
                    ProfitMargin = profitMargin
                };
            }).ToList();

            grid.DataSource = financialData;
            return grid;
        }

        private void LoadDashboardData()
        {
            // يتم تحديث البيانات عند فتح النموذج
        }

        // Helper Methods للحصول على البيانات
        private int GetTotalPonds() => _context.Ponds.Count();
        private int GetActivePonds() => _context.Ponds.Count(p => p.Status == PondStatus.Active);
        private int GetActiveCycles() => _context.ProductionCycles.Count(c => c.Status == CycleStatus.Active);
        private int GetCompletedCycles() => _context.ProductionCycles.Count(c => c.Status == CycleStatus.Completed);
        private int GetTotalFish() => _context.ProductionCycles.Where(c => c.Status == CycleStatus.Active).Sum(c => c.InitialFishCount);
        private double GetAverageWeight()
        {
            var activeCycles = _context.ProductionCycles.Where(c => c.Status == CycleStatus.Active).ToList();
            return activeCycles.Any() ? (double)activeCycles.Average(c => c.InitialAverageWeight) : 0;
        }

        private double GetExpectedProduction()
        {
            var activeCycles = _context.ProductionCycles.Where(c => c.Status == CycleStatus.Active).ToList();
            return activeCycles.Sum(c => c.InitialFishCount * ((double)c.InitialAverageWeight + 200) / 1000);
        }

        private double GetExpectedValue() => GetExpectedProduction() * 15; // سعر افتراضي
        private double GetMonthlyFeedCost() => _context.FeedingRecords.Where(f => f.FeedingDate >= DateTime.Now.AddDays(-30)).Sum(f => f.Quantity * f.FeedPrice);

        private double GetAverageFCR()
        {
            var cyclesWithFCR = _context.ProductionCycles.Where(c => c.FCR.HasValue).ToList();
            return cyclesWithFCR.Any() ? (double)cyclesWithFCR.Average(c => c.FCR.GetValueOrDefault()) : 0;
        }

        private double GetMonthlyMortalityRate()
        {
            var totalFish = GetTotalFish();
            if (totalFish == 0) return 0;
            var monthlyDeaths = _context.MortalityRecords.Where(m => m.Date >= DateTime.Now.AddDays(-30)).Sum(m => m.DeadFishCount);
            return (double)monthlyDeaths / totalFish * 100;
        }
        private int GetMonthlyMortality() => _context.MortalityRecords.Where(m => m.Date >= DateTime.Now.AddDays(-30)).Sum(m => m.DeadFishCount);
        private string GetWaterQualityStatus() => "جيد"; // يمكن تحسينه
        private string GetLastWaterTest()
        {
            var lastTest = _context.WaterQualityRecords.OrderByDescending(w => w.MeasurementDate).FirstOrDefault();
            if (lastTest?.MeasurementDate != null)
                return lastTest.MeasurementDate.Value.ToString("yyyy/MM/dd");
            return "غير متوفر";
        }
        private int GetActiveAlerts() => _notificationService.GetAllAlerts().Count;

        private string GetPerformanceStats()
        {
            var completedCycles = _context.ProductionCycles.Where(c => c.Status == CycleStatus.Completed).ToList();
            if (!completedCycles.Any()) return "لا توجد دورات مكتملة لعرض الإحصائيات";

            var cyclesWithSurvival = completedCycles.Where(c => c.SurvivalRate.HasValue).ToList();
            var cyclesWithFCR = completedCycles.Where(c => c.FCR.HasValue).ToList();
            var cyclesWithADG = completedCycles.Where(c => c.ADG.HasValue).ToList();

            var avgSurvival = cyclesWithSurvival.Any() ? cyclesWithSurvival.Average(c => c.SurvivalRate.GetValueOrDefault()) : 0;
            var avgFCR = cyclesWithFCR.Any() ? cyclesWithFCR.Average(c => c.FCR.GetValueOrDefault()) : 0;
            var avgADG = cyclesWithADG.Any() ? cyclesWithADG.Average(c => c.ADG.GetValueOrDefault()) : 0;
            var totalProduction = completedCycles.Sum(c => c.TotalHarvestWeight ?? 0);

            return $"متوسط البقاء: {avgSurvival:F1}% | متوسط FCR: {avgFCR:F2} | متوسط ADG: {avgADG:F2} جم/يوم | الإنتاج الكلي: {totalProduction:F1} كجم";
        }
    }
}