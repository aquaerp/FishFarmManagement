using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Threading.Tasks;
using System.Windows.Forms;
using Microsoft.EntityFrameworkCore;
using FishFarmManager.Data;
using FishFarmManager.Models;
using FishFarmManager.Services;

namespace FishFarmManager.Forms
{
    /// <summary>
    /// نموذج تقارير المشتريات
    /// Purchase Reports Form
    /// </summary>
    public partial class PurchaseReportsForm : Form
    {
        #region Fields

        private readonly FishFarmContext _context;
        private TabControl _tabControl = null!;

        #endregion

        #region Constructor

        public PurchaseReportsForm(FishFarmContext context)
        {
            _context = context ?? throw new ArgumentNullException(nameof(context));

            if (!AuthenticationService.HasPermission(UserRole.Admin, UserRole.Manager, UserRole.InventoryStaff, UserRole.Accountant))
            {
                MessageBox.Show("ليس لديك صلاحية لعرض تقارير المشتريات", "تحذير",
                    MessageBoxButtons.OK, MessageBoxIcon.Warning);
                this.Load += (s, e) => this.Close();
                return;
            }

            InitializeComponent();
            InitializeForm();
        }

        #endregion

        #region Initialization

        private void InitializeComponent()
        {
            this.Text = "تقارير المشتريات";
            this.Size = new Size(1600, 900);
            this.StartPosition = FormStartPosition.CenterScreen;
            this.RightToLeft = RightToLeft.Yes;
            this.RightToLeftLayout = true;
            this.Font = new Font("Cairo", 10F);
            this.WindowState = FormWindowState.Maximized;

            var mainPanel = new Panel
            {
                Dock = DockStyle.Fill,
                Padding = new Padding(10),
                BackColor = Color.FromArgb(240, 240, 240)
            };

            var titleLabel = new Label
            {
                Text = "تقارير المشتريات الشاملة",
                Location = new Point(10, 10),
                Size = new Size(1560, 40),
                Font = new Font("Cairo", 14F, FontStyle.Bold),
                ForeColor = Color.FromArgb(46, 92, 138),
                TextAlign = ContentAlignment.MiddleRight
            };
            mainPanel.Controls.Add(titleLabel);

            _tabControl = new TabControl
            {
                Location = new Point(10, 60),
                Size = new Size(1560, 800),
                Font = new Font("Cairo", 10F)
            };

            CreateTabs();

            mainPanel.Controls.Add(_tabControl);
            this.Controls.Add(mainPanel);
        }

        private void CreateTabs()
        {
            // Tab 1: Purchase Orders Summary
            var ordersTab = new TabPage("ملخص أوامر الشراء");
            ordersTab.Controls.Add(CreateOrdersSummaryPanel());
            _tabControl.TabPages.Add(ordersTab);

            // Tab 2: Supplier Performance
            var suppliersTab = new TabPage("أداء الموردين");
            suppliersTab.Controls.Add(CreateSupplierPerformancePanel());
            _tabControl.TabPages.Add(suppliersTab);

            // Tab 3: Receiving Report
            var receivingTab = new TabPage("تقرير الاستلام");
            receivingTab.Controls.Add(CreateReceivingReportPanel());
            _tabControl.TabPages.Add(receivingTab);

            // Tab 4: Purchase Analysis
            var analysisTab = new TabPage("تحليل المشتريات");
            analysisTab.Controls.Add(CreatePurchaseAnalysisPanel());
            _tabControl.TabPages.Add(analysisTab);

            // Tab 5: Dashboard
            var dashboardTab = new TabPage("لوحة التحكم");
            dashboardTab.Controls.Add(CreateDashboardPanel());
            _tabControl.TabPages.Add(dashboardTab);
        }

        private void InitializeForm()
        {
            try
            {
                LoggingService.LogInfo("PurchaseReportsForm initialized");
            }
            catch (Exception ex)
            {
                LoggingService.LogError("Error initializing PurchaseReportsForm", ex);
                MessageBox.Show($"حدث خطأ: {ex.Message}", "خطأ", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        #endregion

        #region Report Panels

        private Panel CreateOrdersSummaryPanel()
        {
            var panel = new Panel { Dock = DockStyle.Fill, Padding = new Padding(20), BackColor = Color.White };

            // Filters
            var filterPanel = CreateFilterPanel();
            panel.Controls.Add(filterPanel);

            // Grid
            var grid = new DataGridView
            {
                Location = new Point(20, 90),
                Size = new Size(1000, 650),
                Font = new Font("Cairo", 9F),
                BackgroundColor = Color.White,
                ReadOnly = true,
                AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill
            };
            panel.Controls.Add(grid);

            // Summary Label
            var summaryLabel = new Label
            {
                Text = "ملخص توزيع المشتريات",
                Location = new Point(1030, 90),
                Size = new Size(490, 650),
                BackColor = Color.White,
                Font = new Font("Cairo", 12F),
                TextAlign = ContentAlignment.MiddleCenter,
                BorderStyle = BorderStyle.FixedSingle
            };
            panel.Controls.Add(summaryLabel);

            return panel;
        }

        private Panel CreateSupplierPerformancePanel()
        {
            var panel = new Panel { Dock = DockStyle.Fill, Padding = new Padding(20), BackColor = Color.White };

            var filterPanel = CreateFilterPanel();
            panel.Controls.Add(filterPanel);

            var grid = new DataGridView
            {
                Location = new Point(20, 90),
                Size = new Size(1000, 650),
                Font = new Font("Cairo", 9F),
                BackgroundColor = Color.White,
                ReadOnly = true
            };
            panel.Controls.Add(grid);

            var summaryLabel = new Label
            {
                Text = "ملخص أداء الموردين",
                Location = new Point(1030, 90),
                Size = new Size(490, 650),
                BackColor = Color.White,
                Font = new Font("Cairo", 12F),
                TextAlign = ContentAlignment.MiddleCenter,
                BorderStyle = BorderStyle.FixedSingle
            };
            panel.Controls.Add(summaryLabel);

            return panel;
        }

        private Panel CreateReceivingReportPanel()
        {
            var panel = new Panel { Dock = DockStyle.Fill, Padding = new Padding(20), BackColor = Color.White };

            var filterPanel = CreateFilterPanel();
            panel.Controls.Add(filterPanel);

            var grid = new DataGridView
            {
                Location = new Point(20, 90),
                Size = new Size(1520, 650),
                Font = new Font("Cairo", 9F),
                BackgroundColor = Color.White,
                ReadOnly = true,
                AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill
            };
            panel.Controls.Add(grid);

            return panel;
        }

        private Panel CreatePurchaseAnalysisPanel()
        {
            var panel = new Panel { Dock = DockStyle.Fill, Padding = new Padding(20), BackColor = Color.White };

            var filterPanel = CreateFilterPanel();
            panel.Controls.Add(filterPanel);

            var grid = new DataGridView
            {
                Location = new Point(20, 90),
                Size = new Size(750, 650),
                Font = new Font("Cairo", 9F),
                BackgroundColor = Color.White,
                ReadOnly = true
            };
            panel.Controls.Add(grid);

            var summaryLabel1 = new Label
            {
                Text = "ملخص التوزيع حسب الفئة",
                Location = new Point(780, 90),
                Size = new Size(380, 320),
                BackColor = Color.White,
                Font = new Font("Cairo", 12F),
                TextAlign = ContentAlignment.MiddleCenter,
                BorderStyle = BorderStyle.FixedSingle
            };
            panel.Controls.Add(summaryLabel1);

            var summaryLabel2 = new Label
            {
                Text = "ملخص الاتجاهات الشهرية",
                Location = new Point(780, 420),
                Size = new Size(380, 320),
                BackColor = Color.White,
                Font = new Font("Cairo", 12F),
                TextAlign = ContentAlignment.MiddleCenter,
                BorderStyle = BorderStyle.FixedSingle
            };
            panel.Controls.Add(summaryLabel2);

            return panel;
        }

        private Panel CreateDashboardPanel()
        {
            var panel = new Panel { Dock = DockStyle.Fill, Padding = new Padding(20), BackColor = Color.White };

            // KPI Cards
            var cardsPanel = new FlowLayoutPanel
            {
                Location = new Point(20, 20),
                Size = new Size(1520, 150),
                FlowDirection = FlowDirection.RightToLeft,
                WrapContents = false
            };

            cardsPanel.Controls.Add(CreateKPICard("إجمالي المشتريات", "0 ريال", Color.FromArgb(46, 92, 138)));
            cardsPanel.Controls.Add(CreateKPICard("الطلبات المعلقة", "0", Color.FromArgb(255, 152, 0)));
            cardsPanel.Controls.Add(CreateKPICard("نسبة الاستلام", "0%", Color.FromArgb(38, 166, 154)));
            cardsPanel.Controls.Add(CreateKPICard("مشاكل الجودة", "0", Color.FromArgb(229, 57, 53)));

            panel.Controls.Add(cardsPanel);

            return panel;
        }

        private Panel CreateKPICard(string title, string value, Color color)
        {
            var card = new Panel
            {
                Size = new Size(280, 130),
                BackColor = Color.White,
                BorderStyle = BorderStyle.FixedSingle,
                Margin = new Padding(10)
            };

            var titleLabel = new Label
            {
                Text = title,
                Location = new Point(10, 15),
                Size = new Size(260, 30),
                Font = new Font("Cairo", 10F),
                ForeColor = Color.Gray,
                TextAlign = ContentAlignment.MiddleCenter
            };
            card.Controls.Add(titleLabel);

            var valueLabel = new Label
            {
                Text = value,
                Location = new Point(10, 50),
                Size = new Size(260, 50),
                Font = new Font("Cairo", 20F, FontStyle.Bold),
                ForeColor = color,
                TextAlign = ContentAlignment.MiddleCenter
            };
            card.Controls.Add(valueLabel);

            return card;
        }

        private GroupBox CreateFilterPanel()
        {
            var filterPanel = new GroupBox
            {
                Text = "الفلترة",
                Location = new Point(20, 20),
                Size = new Size(1520, 60),
                Font = new Font("Cairo", 10F, FontStyle.Bold)
            };

            var fromDate = new DateTimePicker { Location = new Point(1250, 25), Size = new Size(140, 30), Format = DateTimePickerFormat.Short };
            var toDate = new DateTimePicker { Location = new Point(1050, 25), Size = new Size(140, 30), Format = DateTimePickerFormat.Short };
            var generateBtn = new Button { Text = "إنشاء التقرير", Location = new Point(850, 23), Size = new Size(150, 35), Font = new Font("Cairo", 10F, FontStyle.Bold) };

            filterPanel.Controls.AddRange(new Control[] { fromDate, toDate, generateBtn });

            return filterPanel;
        }

        #endregion

        #region Dispose

        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                // تحرير الموارد
            }
            base.Dispose(disposing);
        }

        #endregion
    }
}

