using System;
using System.Drawing;
using System.Linq;
using System.Windows.Forms;
using Microsoft.Extensions.DependencyInjection;
using FishFarmManager.Data;
using FishFarmManager.Models;
using FishFarmManager.Services;
using System.Net.Http;
using System.Threading.Tasks;
using System.Threading;
using System.Collections.Generic;

namespace FishFarmManager.Forms
{
    public partial class MainForm : Form
    {
        private readonly FishFarmContext _context;
        private readonly IServiceProvider _serviceProvider;
        private readonly NotificationService _notificationService;
        private readonly BackupService _backupService;
        private readonly IHttpClientFactory _httpClientFactory;
        private readonly CancellationTokenSource _cancellationTokenSource = new CancellationTokenSource();
        private readonly List<Form> _openChildForms = new List<Form>();

        public MainForm(FishFarmContext context, IServiceProvider serviceProvider, 
                       NotificationService notificationService, BackupService backupService,
                       IHttpClientFactory httpClientFactory)
        {
            _context = context;
            _serviceProvider = serviceProvider;
            _notificationService = notificationService;
            _backupService = backupService;
            _httpClientFactory = httpClientFactory;
            InitializeComponent();
            LoadDashboardData();
            CheckAlerts();
            _ = CheckForUpdatesAsync();
            // Apply AquaFarm Pro theme
            try
            {
                ThemeManager.ApplyTheme(this);
                LocalizationManager.ApplyResources(this);
            }
            catch { }
            LocalizationManager.CultureChanged += LocalizationManager_CultureChanged;
            
            // Register form closing event
            this.FormClosing += MainForm_FormClosing;
        }

        private void MainForm_FormClosing(object? sender, FormClosingEventArgs e)
        {
            try
            {
                LocalizationManager.CultureChanged -= LocalizationManager_CultureChanged;
                // Cancel any pending async operations
                _cancellationTokenSource?.Cancel();
                
                // Close all child forms
                CloseAllChildForms();
                
                // Dispose resources
                _context?.Dispose();
                _cancellationTokenSource?.Dispose();
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error during form closing: {ex.Message}");
            }
        }

        private void CloseAllChildForms()
        {
            // Create a copy of the list to avoid modification during iteration
            var formsToClose = _openChildForms.ToList();
            
            foreach (var form in formsToClose)
            {
                try
                {
                    if (form != null && !form.IsDisposed)
                    {
                        form.Close();
                        form.Dispose();
                    }
                }
                catch (Exception ex)
                {
                    System.Diagnostics.Debug.WriteLine($"Error closing child form: {ex.Message}");
                }
            }
            
            _openChildForms.Clear();
        }

        private void ShowChildForm(Form childForm)
        {
            if (childForm != null)
            {
                _openChildForms.Add(childForm);
                childForm.FormClosed += (s, e) =>
                {
                    _openChildForms.Remove(childForm);
                    childForm.Dispose();
                };
                childForm.ShowDialog();
            }
        }

        private void InitializeComponent()
        {
            this.SuspendLayout();
            
            // إعداد النافذة الرئيسية
            LocalizationManager.Bind(this, "MainWindowTitle");
            this.Size = new Size(1200, 800);
            this.StartPosition = FormStartPosition.CenterScreen;
            this.WindowState = FormWindowState.Maximized;
            this.RightToLeft = LocalizationManager.CurrentCulture.TextInfo.IsRightToLeft
                ? RightToLeft.Yes
                : RightToLeft.No;
            this.RightToLeftLayout = LocalizationManager.CurrentCulture.TextInfo.IsRightToLeft;
            try { this.Font = new Font("Cairo", 10F, FontStyle.Regular); } catch { }
            
            // إضافة أيقونة التطبيق
            try
            {
                if (System.IO.File.Exists("Assets/Images/AppIcon.ico"))
                {
                    this.Icon = new Icon("Assets/Images/AppIcon.ico");
                }
            }
            catch
            {
                // استخدام الأيقونة الافتراضية
            }

            // إنشاء شريط القوائم
            CreateMenuBar();
            
            // إنشاء شريط الأدوات
            CreateToolBar();
            
            // إنشاء لوحة التحكم الرئيسية
            CreateDashboardPanel();
            
            // إنشاء شريط الحالة
            CreateStatusBar();

            this.ResumeLayout(false);
        }

        private void CreateMenuBar()
        {
            var menuStrip = new MenuStrip();
            
            // قائمة الملف
            var fileMenu = new ToolStripMenuItem("ملف");
            LocalizationManager.Bind(fileMenu, "MenuFile");
            fileMenu.DropDownItems.Add("نسخ احتياطي", null, BackupData_Click);
            fileMenu.DropDownItems.Add("استعادة", null, RestoreData_Click);
            fileMenu.DropDownItems.Add("تصدير البيانات", null, ExportData_Click);
            fileMenu.DropDownItems.Add(new ToolStripSeparator());
            fileMenu.DropDownItems.Add("خروج", null, Exit_Click);
            
            // قائمة الإدارة
            var managementMenu = new ToolStripMenuItem("الإدارة");
            LocalizationManager.Bind(managementMenu, "MenuManagement");
            managementMenu.DropDownItems.Add("إدارة الأحواض", null, ManagePonds_Click);
            managementMenu.DropDownItems.Add("الدورات الإنتاجية", null, ManageCycles_Click);
            managementMenu.DropDownItems.Add("تسجيل التغذية", null, RecordFeeding_Click);
            managementMenu.DropDownItems.Add("مراقبة جودة المياه", null, MonitorWaterQuality_Click);
            managementMenu.DropDownItems.Add("مراقبة HACCP وسلامة الغذاء", null, MonitorHaccp_Click);
            managementMenu.DropDownItems.Add("تسجيل النفوق", null, RecordMortality_Click);
            managementMenu.DropDownItems.Add(new ToolStripSeparator());
            managementMenu.DropDownItems.Add("سجل الأدوية والمعالجات", null, TreatmentRecord_Click);
            managementMenu.DropDownItems.Add("سجل التتبع البيئي", null, EnvironmentalRecord_Click);
            managementMenu.DropDownItems.Add("سجل الصحة الحيوانية", null, FishHealthRecord_Click);
            managementMenu.DropDownItems.Add("سجل الدفعات", null, BatchRecord_Click);
            managementMenu.DropDownItems.Add("سجل الشهادات", null, CertificationRecord_Click);
            managementMenu.DropDownItems.Add("سجل العمال والتدريب", null, StaffRecord_Click);
            
            // قائمة المبيعات
            var salesMenu = new ToolStripMenuItem("المبيعات");
            LocalizationManager.Bind(salesMenu, "MenuSales");
            salesMenu.DropDownItems.Add("إدارة العملاء", null, ManageCustomers_Click);
            salesMenu.DropDownItems.Add("أوامر المبيعات", null, ManageSalesOrders_Click);
            salesMenu.DropDownItems.Add("المدفوعات", null, ManagePayments_Click);
            salesMenu.DropDownItems.Add(new ToolStripSeparator());
            salesMenu.DropDownItems.Add("تقارير المبيعات", null, SalesReport_Click);
            
            // قائمة التكاليف والموردين
            var costMenu = new ToolStripMenuItem("التكاليف والموردين");
            LocalizationManager.Bind(costMenu, "MenuCostsSuppliers");
            costMenu.DropDownItems.Add("إدارة الموردين", null, ManageSuppliers_Click);
            costMenu.DropDownItems.Add("تسجيل التكاليف", null, RecordCosts_Click);
            costMenu.DropDownItems.Add("تكلفة الإنتاج والنفوق والحصاد", null, ProductionCosting_Click);
            costMenu.DropDownItems.Add("دفع المستحقات", null, SupplierPayments_Click);
            costMenu.DropDownItems.Add(new ToolStripSeparator());
            costMenu.DropDownItems.Add("تقارير تحليل التكاليف", null, CostAnalysisReport_Click);
            
            // قائمة الصيانة
            var maintenanceMenu = new ToolStripMenuItem("الصيانة");
            LocalizationManager.Bind(maintenanceMenu, "MenuMaintenance");
            maintenanceMenu.DropDownItems.Add("إدارة المعدات", null, ManageEquipment_Click);
            maintenanceMenu.DropDownItems.Add("جدولة الصيانة", null, ManageSchedules_Click);
            maintenanceMenu.DropDownItems.Add("تسجيل الصيانة", null, RecordMaintenance_Click);
            maintenanceMenu.DropDownItems.Add("قطع الغيار", null, ManageSpareParts_Click);
            
            // قائمة الموارد البشرية
            var hrMenu = new ToolStripMenuItem("الموارد البشرية");
            LocalizationManager.Bind(hrMenu, "MenuHumanResources");
            hrMenu.DropDownItems.Add("إدارة الموظفين", null, ManageEmployees_Click);
            hrMenu.DropDownItems.Add("تسجيل الحضور", null, AttendanceRecord_Click);
            hrMenu.DropDownItems.Add("معالجة الرواتب", null, ProcessSalaries_Click);
            hrMenu.DropDownItems.Add("إدارة الإجازات", null, ManageLeaves_Click);
            hrMenu.DropDownItems.Add(new ToolStripSeparator());
            hrMenu.DropDownItems.Add("تقارير الموارد البشرية", null, HRReports_Click);
            
            // قائمة المخزون
            var inventoryMenu = new ToolStripMenuItem("المخزون");
            LocalizationManager.Bind(inventoryMenu, "MenuInventory");
            inventoryMenu.DropDownItems.Add("إدارة بنود المخزون", null, ManageInventoryItems_Click);
            inventoryMenu.DropDownItems.Add("حركات المخزون", null, ManageStockMovements_Click);
            inventoryMenu.DropDownItems.Add("تعديلات المخزون والجرد", null, ManageStockAdjustments_Click);
            inventoryMenu.DropDownItems.Add("التتبع التشغيلي للدفعات", null, ManageOperationalTraceability_Click);
            
            // قائمة المشتريات
            var purchasingMenu = new ToolStripMenuItem("المشتريات");
            LocalizationManager.Bind(purchasingMenu, "MenuPurchasing");
            purchasingMenu.DropDownItems.Add("أوامر الشراء", null, ManagePurchaseOrders_Click);
            purchasingMenu.DropDownItems.Add("استلام البضائع", null, ManagePurchaseReceiving_Click);
            purchasingMenu.DropDownItems.Add(new ToolStripSeparator());
            purchasingMenu.DropDownItems.Add("تقارير المشتريات", null, PurchaseReports_Click);
            
            // قائمة المحاسبة المالية
            var accountingMenu = new ToolStripMenuItem("المحاسبة المالية");
            LocalizationManager.Bind(accountingMenu, "MenuFinancialAccounting");
            accountingMenu.DropDownItems.Add("إدارة المحاسبة", null, AccountingManagement_Click);
            accountingMenu.DropDownItems.Add("مطابقة المخزون مع الأستاذ", null, InventoryReconciliation_Click);
            accountingMenu.DropDownItems.Add(new ToolStripSeparator());
            accountingMenu.DropDownItems.Add("لوحة التحكم المالية", null, FinancialDashboard_Click);
            accountingMenu.DropDownItems.Add("قائمة الدخل", null, IncomeStatement_Click);
            accountingMenu.DropDownItems.Add("الميزانية العمومية", null, BalanceSheet_Click);
            accountingMenu.DropDownItems.Add("قائمة التدفق النقدي", null, CashFlow_Click);
            accountingMenu.DropDownItems.Add("الميزان التجريبي", null, TrialBalance_Click);
            accountingMenu.DropDownItems.Add(new ToolStripSeparator());
            accountingMenu.DropDownItems.Add("الأصول الثابتة", null, ManageFixedAssets_Click);
            accountingMenu.DropDownItems.Add("الإهلاك", null, ManageDepreciation_Click);
            
            // قائمة الضرائب والفاتورة الضريبية
            var taxMenu = new ToolStripMenuItem("الضرائب والفاتورة الضريبية");
            LocalizationManager.Bind(taxMenu, "MenuTaxInvoice");
            taxMenu.DropDownItems.Add("الفاتورة الضريبية", null, ManageTaxInvoices_Click);
            taxMenu.DropDownItems.Add("إقرار ضريبة القيمة المضافة", null, ManageVATReturns_Click);
            taxMenu.DropDownItems.Add(new ToolStripSeparator());
            taxMenu.DropDownItems.Add("تقارير ضريبة القيمة المضافة", null, VATReports_Click);
            taxMenu.DropDownItems.Add(new ToolStripSeparator());
            taxMenu.DropDownItems.Add("🔐 التكامل مع ZATCA (فاتورة)", null, ZATCAIntegration_Click);

            // قائمة التقارير
            var reportsMenu = new ToolStripMenuItem("التقارير");
            LocalizationManager.Bind(reportsMenu, "MenuReports");
            reportsMenu.DropDownItems.Add("تقرير الأداء", null, PerformanceReport_Click);
            reportsMenu.DropDownItems.Add("تقرير جودة المياه", null, WaterQualityReport_Click);
            reportsMenu.DropDownItems.Add("تقرير التكاليف", null, CostReport_Click);
            reportsMenu.DropDownItems.Add("تقرير النفوق", null, MortalityReport_Click);
            reportsMenu.DropDownItems.Add("تقرير المعالجات والعلاجات", null, TreatmentReport_Click);
            reportsMenu.DropDownItems.Add("تقرير التغذية", null, FeedingReport_Click);
            reportsMenu.DropDownItems.Add("تقرير الصحة الحيوانية", null, FishHealthReport_Click);
            reportsMenu.DropDownItems.Add("تقرير الإنتاج", null, ProductionReport_Click);
            reportsMenu.DropDownItems.Add("تقرير بيئي", null, EnvironmentalReport_Click);
            reportsMenu.DropDownItems.Add("تقرير شهادات الجودة", null, CertificationReport_Click);
            reportsMenu.DropDownItems.Add("تقرير مقارنة الأداء بين الأحواض", null, PondPerformanceReport_Click);
            reportsMenu.DropDownItems.Add("تقرير المخزون", null, InventoryReport_Click);
            
            // قائمة الأدوات
            var toolsMenu = new ToolStripMenuItem("🔧 أدوات");
            LocalizationManager.Bind(toolsMenu, "MenuTools");
            toolsMenu.DropDownItems.Add("📝 عارض السجلات", null, ViewLogs_Click);
            toolsMenu.DropDownItems.Add(new ToolStripSeparator());
            toolsMenu.DropDownItems.Add("👥 إدارة المستخدمين", null, ManageUsers_Click);
            toolsMenu.DropDownItems.Add("💾 النسخ الاحتياطي", null, Backup_Click);
            toolsMenu.DropDownItems.Add(new ToolStripSeparator());
            toolsMenu.DropDownItems.Add("⚙️ الإعدادات", null, Settings_Click);
            
            // قائمة المساعدة
            var helpMenu = new ToolStripMenuItem("مساعدة");
            LocalizationManager.Bind(helpMenu, "MenuHelp");
            helpMenu.DropDownItems.Add("حول البرنامج", null, About_Click);
            
            menuStrip.Items.AddRange(new ToolStripItem[] { fileMenu, managementMenu, salesMenu, costMenu, maintenanceMenu, hrMenu, inventoryMenu, purchasingMenu, accountingMenu, taxMenu, reportsMenu, toolsMenu, helpMenu });
            this.MainMenuStrip = menuStrip;
            this.Controls.Add(menuStrip);
        }

        private void CreateToolBar()
        {
            var toolStrip = new ToolStrip { Dock = DockStyle.Top };

            var pondsButton = new ToolStripButton { Text = "الأحواض" };
            LocalizationManager.Bind(pondsButton, "Ponds");
            pondsButton.Click += ManagePonds_Click;
            var cyclesButton = new ToolStripButton { Text = "الدورات" };
            LocalizationManager.Bind(cyclesButton, "Cycles");
            cyclesButton.Click += ManageCycles_Click;
            var feedingButton = new ToolStripButton { Text = "التغذية" };
            LocalizationManager.Bind(feedingButton, "Feeding");
            feedingButton.Click += RecordFeeding_Click;
            var waterButton = new ToolStripButton { Text = "جودة المياه" };
            LocalizationManager.Bind(waterButton, "WaterQuality");
            waterButton.Click += MonitorWaterQuality_Click;
            var mortalityButton = new ToolStripButton { Text = "النفوق" };
            LocalizationManager.Bind(mortalityButton, "Mortality");
            mortalityButton.Click += RecordMortality_Click;
            var dashboardButton = new ToolStripButton { Text = "لوحة التحكم" };
            LocalizationManager.Bind(dashboardButton, "Dashboard");
            dashboardButton.Click += ShowDashboard_Click;

            toolStrip.Items.AddRange(new ToolStripItem[] 
            { 
                pondsButton, new ToolStripSeparator(),
                cyclesButton, new ToolStripSeparator(),
                feedingButton, waterButton, mortalityButton, new ToolStripSeparator(),
                dashboardButton
            });

            this.Controls.Add(toolStrip);
        }

        private void CreateDashboardPanel()
        {
            var dashboardPanel = new Panel();
            dashboardPanel.Dock = DockStyle.Fill;
            dashboardPanel.BackColor = ThemeManager.NeutralLightGray;
            dashboardPanel.Name = "DashboardPanel";
            
            // إضافة بطاقات المعلومات
            CreateInfoCards(dashboardPanel);
            
            this.Controls.Add(dashboardPanel);
        }

        private void CreateInfoCards(Panel parent)
        {
            var cardWidth = 250;
            var cardHeight = 120;
            var margin = 20;
            var startX = margin;
            var startY = margin;

            // بطاقة الأحواض
            var pondsCard = CreateInfoCard("الأحواض", GetActivePondsCount().ToString(), 
                                         ThemeManager.SecondarySkyBlue, startX, startY, cardWidth, cardHeight);
            parent.Controls.Add(pondsCard);

            // بطاقة الدورات النشطة
            var cyclesCard = CreateInfoCard("الدورات النشطة", GetActiveCyclesCount().ToString(), 
                                          ThemeManager.SecondaryAquaGreen, startX + cardWidth + margin, startY, cardWidth, cardHeight);
            parent.Controls.Add(cyclesCard);

            // بطاقة إجمالي الأسماك
            var fishCard = CreateInfoCard("إجمالي الأسماك", GetTotalFishCount().ToString(), 
                                        Color.FromArgb(255, 224, 224), startX + (cardWidth + margin) * 2, startY, cardWidth, cardHeight);
            parent.Controls.Add(fishCard);

            // بطاقة التنبيهات
            var alertsCard = CreateInfoCard("التنبيهات", GetAlertsCount().ToString(), 
                                          Color.FromArgb(255, 235, 204), startX + (cardWidth + margin) * 3, startY, cardWidth, cardHeight);
            parent.Controls.Add(alertsCard);
        }

        private Panel CreateInfoCard(string title, string value, Color color, int x, int y, int width, int height)
        {
            var card = new Panel();
            card.Size = new Size(width, height);
            card.Location = new Point(x, y);
            card.BackColor = color;
            card.BorderStyle = BorderStyle.FixedSingle;

            var titleLabel = new Label();
            titleLabel.Text = title;
            titleLabel.Font = new Font("Arial", 12, FontStyle.Bold);
            titleLabel.Location = new Point(10, 10);
            titleLabel.AutoSize = true;

            var valueLabel = new Label();
            valueLabel.Text = value;
            valueLabel.Font = new Font("Arial", 24, FontStyle.Bold);
            valueLabel.Location = new Point(10, 40);
            valueLabel.AutoSize = true;

            card.Controls.Add(titleLabel);
            card.Controls.Add(valueLabel);

            return card;
        }

        private void CreateStatusBar()
        {
            var statusStrip = new StatusStrip();
            var statusLabel = new ToolStripStatusLabel("جاهز");
            var dateLabel = new ToolStripStatusLabel(DateTime.Now.ToString("yyyy/MM/dd HH:mm"));
            
            statusStrip.Items.AddRange(new ToolStripItem[] { statusLabel, dateLabel });
            this.Controls.Add(statusStrip);
        }

        // Event Handlers
        private void TreatmentRecord_Click(object? sender, EventArgs e)
        {
            var form = new TreatmentRecordForm(_context);
            ShowChildForm(form);
        }
        private void EnvironmentalRecord_Click(object? sender, EventArgs e)
        {
            var form = new EnvironmentalRecordForm(_context);
            ShowChildForm(form);
        }
        private void FishHealthRecord_Click(object? sender, EventArgs e)
        {
            var form = new FishHealthRecordForm(_context);
            ShowChildForm(form);
        }
        private void BatchRecord_Click(object? sender, EventArgs e)
        {
            var form = new BatchRecordForm(_context);
            ShowChildForm(form);
        }
        private void CertificationRecord_Click(object? sender, EventArgs e)
        {
            var form = new CertificationRecordForm(_context);
            ShowChildForm(form);
        }
        private void StaffRecord_Click(object? sender, EventArgs e)
        {
            var form = new StaffRecordForm(_context);
            ShowChildForm(form);
        }
        private void ManagePonds_Click(object? sender, EventArgs e)
        {
            var form = _serviceProvider.GetRequiredService<PondManagementForm>();
            ShowChildForm(form);
            LoadDashboardData(); // تحديث البيانات
        }

        private void ManageCycles_Click(object? sender, EventArgs e)
        {
            var form = _serviceProvider.GetRequiredService<ProductionCycleForm>();
            ShowChildForm(form);
            LoadDashboardData();
        }

        private void RecordFeeding_Click(object? sender, EventArgs e)
        {
            var form = _serviceProvider.GetRequiredService<FeedingRecordForm>();
            ShowChildForm(form);
        }

        private void MonitorWaterQuality_Click(object? sender, EventArgs e)
        {
            var form = _serviceProvider.GetRequiredService<WaterQualityForm>();
            ShowChildForm(form);
        }

        private void MonitorHaccp_Click(object? sender, EventArgs e)
        {
            var form = _serviceProvider.GetRequiredService<HACCPRecordForm>();
            ShowChildForm(form);
        }

        private void RecordMortality_Click(object? sender, EventArgs e)
        {
            var form = _serviceProvider.GetRequiredService<MortalityRecordForm>();
            ShowChildForm(form);
        }

        private void ShowDashboard_Click(object? sender, EventArgs e)
        {
            var form = _serviceProvider.GetRequiredService<DashboardForm>();
            ShowChildForm(form);
        }

        private async void BackupData_Click(object? sender, EventArgs e)
        {
            try
            {
                var result = await _backupService.CreateBackup();
                if (!_cancellationTokenSource.IsCancellationRequested)
                {
                    MessageBox.Show(result ? "تم إنشاء النسخة الاحتياطية بنجاح" : "فشل في إنشاء النسخة الاحتياطية", 
                                  "نسخ احتياطي", MessageBoxButtons.OK, 
                                  result ? MessageBoxIcon.Information : MessageBoxIcon.Error);
                }
            }
            catch (OperationCanceledException)
            {
                // Operation was cancelled - no action needed
            }
        }

        private async void RestoreData_Click(object? sender, EventArgs e)
        {
            try
            {
                var openFileDialog = new OpenFileDialog();
                openFileDialog.Filter = "AquaFarm encrypted backup (*.afbackup)|*.afbackup|Legacy database backup (*.db)|*.db";
                if (openFileDialog.ShowDialog() == DialogResult.OK)
                {
                    var result = await _backupService.RestoreBackup(openFileDialog.FileName);
                    if (!_cancellationTokenSource.IsCancellationRequested)
                    {
                        MessageBox.Show(result ? "تم استعادة البيانات بنجاح" : "فشل في استعادة البيانات", 
                                      "استعادة", MessageBoxButtons.OK, 
                                      result ? MessageBoxIcon.Information : MessageBoxIcon.Error);
                        if (result) LoadDashboardData();
                    }
                }
            }
            catch (OperationCanceledException)
            {
                // Operation was cancelled - no action needed
            }
        }

        private async void ExportData_Click(object? sender, EventArgs e)
        {
            try
            {
                var result = await _backupService.ExportToCSV();
                if (!_cancellationTokenSource.IsCancellationRequested)
                {
                    MessageBox.Show(result ? "تم تصدير البيانات بنجاح" : "فشل في تصدير البيانات", 
                                  "تصدير", MessageBoxButtons.OK, 
                                  result ? MessageBoxIcon.Information : MessageBoxIcon.Error);
                }
            }
            catch (OperationCanceledException)
            {
                // Operation was cancelled - no action needed
            }
        }

        private void Exit_Click(object? sender, EventArgs e)
        {
            Application.Exit();
        }

        private void PerformanceReport_Click(object? sender, EventArgs e)
        {
            var form = new PerformanceReportForm(_context);
            ShowChildForm(form);
        }

        private void WaterQualityReport_Click(object? sender, EventArgs e)
        {
            var form = new WaterQualityReportForm(_context);
            ShowChildForm(form);
        }
        
        private void CostReport_Click2(object? sender, EventArgs e)
        {
            var form = new CostReportForm(_context);
            ShowChildForm(form);
        }
        
        private void CostReport_Click(object? sender, EventArgs e)
        {
            var form = new CostReportForm(_context);
            ShowChildForm(form);
        }
        
        private void MortalityReport_Click(object? sender, EventArgs e)
        {
            var form = new MortalityReportForm(_context);
            ShowChildForm(form);
        }
        
        private void TreatmentReport_Click(object? sender, EventArgs e)
        {
            var form = new TreatmentReportForm(_context);
            ShowChildForm(form);
        }
        
        private void FeedingReport_Click(object? sender, EventArgs e)
        {
            var form = new FeedingReportForm(_context);
            ShowChildForm(form);
        }
        
        private void FishHealthReport_Click(object? sender, EventArgs e)
        {
            var form = new FishHealthReportForm(_context);
            ShowChildForm(form);
        }
        
        private void ProductionReport_Click(object? sender, EventArgs e)
        {
            var form = new ProductionReportForm(_context);
            ShowChildForm(form);
        }
        
        private void EnvironmentalReport_Click(object? sender, EventArgs e)
        {
            var form = new EnvironmentalReportForm(_context);
            ShowChildForm(form);
        }
        
        private void CertificationReport_Click(object? sender, EventArgs e)
        {
            var form = new CertificationReportForm(_context);
            ShowChildForm(form);
        }
        
        private void PondPerformanceReport_Click(object? sender, EventArgs e)
        {
            var form = new PondPerformanceReportForm(_context);
            ShowChildForm(form);
        }
        
        private void InventoryReport_Click(object? sender, EventArgs e)
        {
            var form = new InventoryReportForm(_context);
            ShowChildForm(form);
        }

        // Inventory menu handlers
        private void ManageInventoryItems_Click(object? sender, EventArgs e)
        {
            var form = _serviceProvider.GetRequiredService<InventoryItemForm>();
            ShowChildForm(form);
        }

        private void ManageStockMovements_Click(object? sender, EventArgs e)
        {
            try
            {
                var stockMovementForm = new StockMovementForm(_context);
                stockMovementForm.ShowDialog();
            }
            catch (Exception ex)
            {
                LoggingService.LogError(ex, "خطأ في فتح نموذج حركة المخزون");
                MessageBox.Show($"حدث خطأ: {ex.Message}", "خطأ", 
                    MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void ManageStockAdjustments_Click(object? sender, EventArgs e)
        {
            var form = _serviceProvider.GetRequiredService<InventoryCountForm>();
            ShowChildForm(form);
        }

        private void About_Click(object? sender, EventArgs e)
        {
            MessageBox.Show("نظام إدارة مزرعة الأسماك\nالإصدار 1.0\n\nتم تطويره لإدارة مزارع الأسماك بكفاءة", 
                          "حول البرنامج", MessageBoxButtons.OK, MessageBoxIcon.Information);
        }

        // Helper Methods
        private void LoadDashboardData()
        {
            // تحديث بيانات البطاقات
            var dashboardPanel = this.Controls.Find("DashboardPanel", true).FirstOrDefault() as Panel;
            if (dashboardPanel != null)
            {
                dashboardPanel.Controls.Clear();
                CreateInfoCards(dashboardPanel);
            }
        }

        private int GetActivePondsCount()
        {
            return _context.Ponds.Count(p => p.Status == Models.PondStatus.Active);
        }

        private int GetActiveCyclesCount()
        {
            return _context.ProductionCycles.Count(c => c.Status == Models.CycleStatus.Active);
        }

        private int GetTotalFishCount()
        {
            return _context.ProductionCycles
                .Where(c => c.Status == Models.CycleStatus.Active)
                .Sum(c => c.InitialFishCount);
        }

        private int GetAlertsCount()
        {
            try
            {
                return _notificationService.GetAllAlerts().Count;
            }
            catch
            {
                return 0;
            }
        }

        private void CheckAlerts()
        {
            try
            {
                var alerts = _notificationService.GetAllAlerts();
                if (alerts.Count > 0)
                {
                    var message = string.Join("\n", alerts.Take(5)); // عرض أول 5 تنبيهات
                    if (alerts.Count > 5)
                        message += $"\n... �� {alerts.Count - 5} تنبيهات أخرى";
                    
                    MessageBox.Show(message, "تنبيهات مهمة", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                }
            }
            catch
            {
                // تجاهل أخطاء التنبيهات لتجنب توقف التطبيق
            }
        }

        private async Task CheckForUpdatesAsync()
        {
            try
            {
                var client = _httpClientFactory.CreateClient();
                var response = await client.GetStringAsync("https://example.com/latest-version.txt"); // استبدل بالـ URL الفعلي
                
                if (_cancellationTokenSource.IsCancellationRequested)
                    return;
                    
                var latestVersion = Version.Parse(response.Trim());
                var currentVersion = new Version("1.0.0.0"); // استبدل بالإصدار الحالي

                if (latestVersion > currentVersion)
                {
                    MessageBox.Show("يتوفر إصدار جديد. يرجى التحديث.", "تحديث متوفر", MessageBoxButtons.OK, MessageBoxIcon.Information);
                }
            }
            catch (OperationCanceledException)
            {
                // Operation was cancelled - no action needed
            }
            catch
            {
                // تجاهل الأخطاء (مثل عدم الاتصال بالإنترنت)
            }
        }

        // معالجات أحداث المبيعات
        private void ManageCustomers_Click(object? sender, EventArgs e)
        {
            var customerForm = _serviceProvider.GetRequiredService<CustomerForm>();
            ShowChildForm(customerForm);
        }

        private void ManageSalesOrders_Click(object? sender, EventArgs e)
        {
            var salesOrderForm = _serviceProvider.GetRequiredService<SalesOrderForm>();
            ShowChildForm(salesOrderForm);
        }

        private void ManagePayments_Click(object? sender, EventArgs e)
        {
            var customerPaymentForm = _serviceProvider.GetRequiredService<CustomerPaymentForm>();
            ShowChildForm(customerPaymentForm);
        }

        private void SalesReport_Click(object? sender, EventArgs e)
        {
            var salesReportForm = _serviceProvider.GetRequiredService<SalesReportForm>();
            ShowChildForm(salesReportForm);
        }

        // Week 2: Cost & Supplier Management Event Handlers
        private void ManageSuppliers_Click(object? sender, EventArgs e)
        {
            var supplierForm = _serviceProvider.GetRequiredService<SupplierForm>();
            ShowChildForm(supplierForm);
        }

        private void RecordCosts_Click(object? sender, EventArgs e)
        {
            var costRecordForm = _serviceProvider.GetRequiredService<CostRecordForm>();
            ShowChildForm(costRecordForm);
        }

        private void SupplierPayments_Click(object? sender, EventArgs e)
        {
            var supplierPaymentForm = _serviceProvider.GetRequiredService<SupplierPaymentForm>();
            ShowChildForm(supplierPaymentForm);
        }

        private void CostAnalysisReport_Click(object? sender, EventArgs e)
        {
            var costAnalysisReportForm = _serviceProvider.GetRequiredService<CostAnalysisReportForm>();
            ShowChildForm(costAnalysisReportForm);
        }

        // Week 3: HR Management Event Handlers
        private void ManageEmployees_Click(object? sender, EventArgs e)
        {
            var employeeForm = _serviceProvider.GetRequiredService<EmployeeForm>();
            ShowChildForm(employeeForm);
        }

        private void AttendanceRecord_Click(object? sender, EventArgs e)
        {
            var attendanceForm = _serviceProvider.GetRequiredService<AttendanceForm>();
            ShowChildForm(attendanceForm);
        }

        private void ProcessSalaries_Click(object? sender, EventArgs e)
        {
            var salaryForm = _serviceProvider.GetRequiredService<SalaryProcessingForm>();
            ShowChildForm(salaryForm);
        }

        private void ManageEquipment_Click(object? sender, EventArgs e)
        {
            var equipmentForm = new EquipmentForm(_context);
            ShowChildForm(equipmentForm);
        }

        private void ManageSchedules_Click(object? sender, EventArgs e)
        {
            var scheduleForm = new MaintenanceScheduleForm(_context);
            ShowChildForm(scheduleForm);
        }

        private void RecordMaintenance_Click(object? sender, EventArgs e)
        {
            var maintenanceForm = new MaintenanceRecordForm(_context);
            ShowChildForm(maintenanceForm);
        }

        private void ManageSpareParts_Click(object? sender, EventArgs e)
        {
            var sparePartForm = new SparePartForm(_context);
            ShowChildForm(sparePartForm);
        }

        private void ManageLeaves_Click(object? sender, EventArgs e)
        {
            var leaveForm = _serviceProvider.GetRequiredService<LeaveManagementForm>();
            ShowChildForm(leaveForm);
        }

        private void HRReports_Click(object? sender, EventArgs e)
        {
            var hrReportsForm = _serviceProvider.GetRequiredService<HRReportsForm>();
            ShowChildForm(hrReportsForm);
        }

        // ============================================
        // Tools Menu Event Handlers
        // ============================================

        private void ViewLogs_Click(object? sender, EventArgs e)
        {
            LoggingService.LogUserActivity(
                AuthenticationService.CurrentUsername,
                "فتح عارض السجلات",
                "عرض سجلات النظام"
            );

            try
            {
                var logViewerForm = _serviceProvider.GetRequiredService<LogViewerForm>();
                logViewerForm.ShowDialog();
            }
            catch (Exception ex)
            {
                LoggingService.LogError(ex, "خطأ في فتح عارض السجلات");
                MessageBox.Show($"حدث خطأ: {ex.Message}", "خطأ", 
                    MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void ManageUsers_Click(object? sender, EventArgs e)
        {
            LoggingService.LogInfo("محاولة الوصول لإدارة المستخدمين - المستخدم: {User}", 
                AuthenticationService.CurrentUsername);

            // التحقق من الصلاحيات
            if (!AuthenticationService.IsAdmin)
            {
                LoggingService.LogWarning("رفض الوصول - المستخدم {User} ليس مديراً", 
                    AuthenticationService.CurrentUsername);
                
                MessageBox.Show(
                    "عذراً، هذه الصفحة متاحة للمدير فقط",
                    "صلاحيات غير كافية",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Warning
                );
                return;
            }

            try
            {
                var userManagementForm = _serviceProvider.GetRequiredService<UserManagementForm>();
                userManagementForm.ShowDialog();
            }
            catch (Exception ex)
            {
                LoggingService.LogError(ex, "خطأ في فتح إدارة المستخدمين");
                MessageBox.Show($"حدث خطأ: {ex.Message}", "خطأ", 
                    MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private async void Backup_Click(object? sender, EventArgs e)
        {
            LoggingService.LogUserActivity(
                AuthenticationService.CurrentUsername,
                "بدء عملية النسخ الاحتياطي",
                "نسخ احتياطي يدوي"
            );

            try
            {
                var result = MessageBox.Show(
                    "هل تريد إنشاء نسخة احتياطية من قاعدة البيانات؟\n\n" +
                    "سيتم حفظ النسخة في مجلد Backups",
                    "تأكيد النسخ الاحتياطي",
                    MessageBoxButtons.YesNo,
                    MessageBoxIcon.Question
                );

                if (result == DialogResult.Yes)
                {
                    var stopwatch = System.Diagnostics.Stopwatch.StartNew();
                    
                    bool success = await _backupService.CreateBackup();
                    
                    stopwatch.Stop();
                    LoggingService.LogPerformance("النسخ الاحتياطي", stopwatch.Elapsed);

                    if (success)
                    {
                        LoggingService.LogInfo("تم إنشاء النسخة الاحتياطية بنجاح");
                        MessageBox.Show(
                            "تم إنشاء النسخة الاحتياطية بنجاح!",
                            "نجح",
                            MessageBoxButtons.OK,
                            MessageBoxIcon.Information
                        );
                    }
                    else
                    {
                        LoggingService.LogError("فشل إنشاء النسخة الاحتياطية");
                        MessageBox.Show(
                            "فشل إنشاء النسخة الاحتياطية",
                            "خطأ",
                            MessageBoxButtons.OK,
                            MessageBoxIcon.Error
                        );
                    }
                }
            }
            catch (Exception ex)
            {
                LoggingService.LogError(ex, "خطأ في عملية النسخ الاحتياطي");
                MessageBox.Show($"حدث خطأ: {ex.Message}", "خطأ", 
                    MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void Settings_Click(object? sender, EventArgs e)
        {
            LoggingService.LogUserActivity(
                AuthenticationService.CurrentUsername,
                "فتح الإعدادات",
                "عرض إعدادات النظام"
            );

            try
            {
                var settingsForm = _serviceProvider.GetRequiredService<SettingsForm>();
                settingsForm.ShowDialog();
                LoggingService.LogInfo("تم فتح نموذج الإعدادات بنجاح");
            }
            catch (Exception ex)
            {
                LoggingService.LogError(ex, "خطأ في فتح الإعدادات");
                MessageBox.Show($"حدث خطأ: {ex.Message}", "خطأ", 
                    MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        // ============================================
        // Event Handlers for New Financial Modules
        // ============================================

        private void ManagePurchaseOrders_Click(object? sender, EventArgs e)
        {
            LoggingService.LogUserActivity(
                AuthenticationService.CurrentUsername,
                "فتح إدارة أوامر الشراء",
                "عرض أوامر الشراء"
            );

            try
            {
                var form = _serviceProvider.GetRequiredService<PurchaseOrderForm>();
                ShowChildForm(form);
                LoggingService.LogInfo("تم فتح نموذج أوامر الشراء بنجاح");
            }
            catch (Exception ex)
            {
                LoggingService.LogError(ex, "خطأ في فتح أوامر الشراء");
                MessageBox.Show($"حدث خطأ: {ex.Message}", "خطأ", 
                    MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void ManagePurchaseReceiving_Click(object? sender, EventArgs e)
        {
            LoggingService.LogUserActivity(
                AuthenticationService.CurrentUsername,
                "فتح استلام البضائع",
                "عرض استلام البضائع"
            );

            try
            {
                var form = _serviceProvider.GetRequiredService<PurchaseReceivingForm>();
                ShowChildForm(form);
                LoggingService.LogInfo("تم فتح نموذج استلام البضائع بنجاح");
            }
            catch (Exception ex)
            {
                LoggingService.LogError(ex, "خطأ في فتح استلام البضائع");
                MessageBox.Show($"حدث خطأ: {ex.Message}", "خطأ", 
                    MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void PurchaseReports_Click(object? sender, EventArgs e)
        {
            LoggingService.LogUserActivity(
                AuthenticationService.CurrentUsername,
                "فتح تقارير المشتريات",
                "عرض تقارير المشتريات"
            );

            try
            {
                var form = _serviceProvider.GetRequiredService<PurchaseReportsForm>();
                ShowChildForm(form);
                LoggingService.LogInfo("تم فتح تقارير المشتريات بنجاح");
            }
            catch (Exception ex)
            {
                LoggingService.LogError(ex, "خطأ في فتح تقارير المشتريات");
                MessageBox.Show($"حدث خطأ: {ex.Message}", "خطأ", 
                    MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void FinancialDashboard_Click(object? sender, EventArgs e)
        {
            LoggingService.LogUserActivity(
                AuthenticationService.CurrentUsername,
                "فتح لوحة التحكم المالية",
                "عرض لوحة التحكم المالية"
            );

            try
            {
                var form = _serviceProvider.GetRequiredService<FinancialDashboardForm>();
                ShowChildForm(form);
                LoggingService.LogInfo("تم فتح لوحة التحكم المالية بنجاح");
            }
            catch (Exception ex)
            {
                LoggingService.LogError(ex, "خطأ في فتح لوحة التحكم المالية");
                MessageBox.Show($"حدث خطأ: {ex.Message}", "خطأ", 
                    MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void ManageOperationalTraceability_Click(object? sender, EventArgs e)
        {
            var form = _serviceProvider.GetRequiredService<OperationalTraceabilityForm>();
            ShowChildForm(form);
        }

        private void ProductionCosting_Click(object? sender, EventArgs e)
        {
            if (!AuthenticationService.HasPermission(UserRole.Admin, UserRole.Manager, UserRole.Accountant, UserRole.ProductionStaff))
            {
                MessageBox.Show("هذه الشاشة متاحة للإنتاج والمحاسبة والإدارة فقط.", "صلاحيات غير كافية",
                    MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }
            ShowChildForm(_serviceProvider.GetRequiredService<ProductionCostingForm>());
        }

        private void AccountingManagement_Click(object? sender, EventArgs e)
        {
            if (!AuthenticationService.HasPermission(UserRole.Admin, UserRole.Manager, UserRole.Accountant))
            {
                MessageBox.Show("هذه الشاشة متاحة للمدير والمحاسب فقط.", "صلاحيات غير كافية",
                    MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }
            try
            {
                LoggingService.LogUserActivity(AuthenticationService.CurrentUsername,
                    "فتح إدارة المحاسبة", "تشغيل النواة المحاسبية والعملات الأجنبية");
                ShowChildForm(_serviceProvider.GetRequiredService<AccountingManagementForm>());
            }
            catch (Exception ex)
            {
                LoggingService.LogError(ex, "خطأ في فتح إدارة المحاسبة");
                MessageBox.Show($"تعذر فتح الشاشة: {ex.Message}", "خطأ",
                    MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void LocalizationManager_CultureChanged(object? sender, EventArgs e)
        {
            if (IsDisposed) return;
            if (InvokeRequired)
            {
                BeginInvoke(new Action(() => LocalizationManager_CultureChanged(sender, e)));
                return;
            }

            ThemeManager.ApplyCultureDirection(this);
            LocalizationManager.ApplyResources(this);
            PerformLayout();
        }

        private void InventoryReconciliation_Click(object? sender, EventArgs e)
        {
            if (!AuthenticationService.HasPermission(UserRole.Admin, UserRole.Manager, UserRole.Accountant))
            {
                MessageBox.Show("هذه الشاشة متاحة للمدير والمحاسب فقط.", "صلاحيات غير كافية",
                    MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }
            ShowChildForm(_serviceProvider.GetRequiredService<InventoryReconciliationForm>());
        }

        private void IncomeStatement_Click(object? sender, EventArgs e)
        {
            LoggingService.LogUserActivity(
                AuthenticationService.CurrentUsername,
                "فتح قائمة الدخل",
                "عرض قائمة الدخل"
            );

            try
            {
                var form = new IncomeStatementForm(_context);
                ShowChildForm(form);
                LoggingService.LogInfo("تم فتح قائمة الدخل بنجاح");
            }
            catch (Exception ex)
            {
                LoggingService.LogError(ex, "خطأ في فتح قائمة الدخل");
                MessageBox.Show($"حدث خطأ: {ex.Message}", "خطأ", 
                    MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void BalanceSheet_Click(object? sender, EventArgs e)
        {
            LoggingService.LogUserActivity(
                AuthenticationService.CurrentUsername,
                "فتح الميزانية العمومية",
                "عرض الميزانية العمومية"
            );

            try
            {
                var form = new BalanceSheetForm(_context);
                ShowChildForm(form);
                LoggingService.LogInfo("تم فتح الميزانية العمومية بنجاح");
            }
            catch (Exception ex)
            {
                LoggingService.LogError(ex, "خطأ في فتح الميزانية العمومية");
                MessageBox.Show($"حدث خطأ: {ex.Message}", "خطأ", 
                    MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void CashFlow_Click(object? sender, EventArgs e)
        {
            LoggingService.LogUserActivity(
                AuthenticationService.CurrentUsername,
                "فتح قائمة التدفق النقدي",
                "عرض قائمة التدفق النقدي"
            );

            try
            {
                var form = new CashFlowForm(_context);
                ShowChildForm(form);
                LoggingService.LogInfo("تم فتح قائمة التدفق النقدي بنجاح");
            }
            catch (Exception ex)
            {
                LoggingService.LogError(ex, "خطأ في فتح قائمة التدفق النقدي");
                MessageBox.Show($"حدث خطأ: {ex.Message}", "خطأ", 
                    MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void TrialBalance_Click(object? sender, EventArgs e)
        {
            LoggingService.LogUserActivity(
                AuthenticationService.CurrentUsername,
                "فتح الميزان التجريبي",
                "عرض الميزان التجريبي"
            );

            try
            {
                var form = new TrialBalanceForm(_context);
                ShowChildForm(form);
                LoggingService.LogInfo("تم فتح الميزان التجريبي بنجاح");
            }
            catch (Exception ex)
            {
                LoggingService.LogError(ex, "خطأ في فتح الميزان التجريبي");
                MessageBox.Show($"حدث خطأ: {ex.Message}", "خطأ", 
                    MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void ManageFixedAssets_Click(object? sender, EventArgs e)
        {
            LoggingService.LogUserActivity(
                AuthenticationService.CurrentUsername,
                "فتح إدارة الأصول الثابتة",
                "عرض الأصول الثابتة"
            );

            try
            {
                var form = new FixedAssetForm(_context);
                ShowChildForm(form);
                LoggingService.LogInfo("تم فتح إدارة الأصول الثابتة بنجاح");
            }
            catch (Exception ex)
            {
                LoggingService.LogError(ex, "خطأ في فتح إدارة الأصول الثابتة");
                MessageBox.Show($"حدث خطأ: {ex.Message}", "خطأ", 
                    MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void ManageDepreciation_Click(object? sender, EventArgs e)
        {
            LoggingService.LogUserActivity(
                AuthenticationService.CurrentUsername,
                "فتح إدارة الإهلاك",
                "عرض الإهلاك"
            );

            try
            {
                var form = new DepreciationForm(_context);
                ShowChildForm(form);
                LoggingService.LogInfo("تم فتح إدارة الإهلاك بنجاح");
            }
            catch (Exception ex)
            {
                LoggingService.LogError(ex, "خطأ في فتح إدارة الإهلاك");
                MessageBox.Show($"حدث خطأ: {ex.Message}", "خطأ", 
                    MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void ManageTaxInvoices_Click(object? sender, EventArgs e)
        {
            LoggingService.LogUserActivity(
                AuthenticationService.CurrentUsername,
                "فتح إدارة الفواتير الضريبية",
                "عرض الفواتير الضريبية"
            );

            try
            {
                var form = new TaxInvoiceForm(_context);
                ShowChildForm(form);
                LoggingService.LogInfo("تم فتح إدارة الفواتير الضريبية بنجاح");
            }
            catch (Exception ex)
            {
                LoggingService.LogError(ex, "خطأ في فتح إدارة الفواتير الضريبية");
                MessageBox.Show($"حدث خطأ: {ex.Message}", "خطأ", 
                    MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void ManageVATReturns_Click(object? sender, EventArgs e)
        {
            LoggingService.LogUserActivity(
                AuthenticationService.CurrentUsername,
                "فتح إدارة إقرارات ضريبة القيمة المضافة",
                "عرض إقرارات ضريبة القيمة المضافة"
            );

            try
            {
                var form = new VATReturnForm(_context);
                ShowChildForm(form);
                LoggingService.LogInfo("تم فتح إدارة إقرارات ضريبة القيمة المضافة بنجاح");
            }
            catch (Exception ex)
            {
                LoggingService.LogError(ex, "خطأ في فتح إدارة إقرارات ضريبة القيمة المضافة");
                MessageBox.Show($"حدث خطأ: {ex.Message}", "خطأ", 
                    MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void VATReports_Click(object? sender, EventArgs e)
        {
            LoggingService.LogUserActivity(
                AuthenticationService.CurrentUsername,
                "فتح تقارير ضريبة القيمة المضافة",
                "عرض تقارير ضريبة القيمة المضافة"
            );

            try
            {
                var form = new VATReportsForm(_context);
                ShowChildForm(form);
                LoggingService.LogInfo("تم فتح تقارير ضريبة القيمة المضافة بنجاح");
            }
            catch (Exception ex)
            {
                LoggingService.LogError(ex, "خطأ في فتح تقارير ضريبة القيمة المضافة");
                MessageBox.Show($"حدث خطأ: {ex.Message}", "خطأ", 
                    MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void ZATCAIntegration_Click(object? sender, EventArgs e)
        {
            LoggingService.LogUserActivity(
                AuthenticationService.CurrentUsername,
                "فتح نظام التكامل مع ZATCA",
                "الفوترة الإلكترونية - ZATCA Integration"
            );

            try
            {
                var form = new EInvoicingIntegrationForm(_context);
                ShowChildForm(form);
                LoggingService.LogInfo("تم فتح نظام التكامل مع ZATCA بنجاح");
            }
            catch (Exception ex)
            {
                LoggingService.LogError(ex, "خطأ في فتح نظام التكامل مع ZATCA");
                MessageBox.Show($"حدث خطأ: {ex.Message}", "خطأ", 
                    MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }
}
}
