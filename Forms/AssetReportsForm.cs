using System;
using System.Drawing;
using System.Windows.Forms;
using FishFarmManager.Data;
using FishFarmManager.Models;
using FishFarmManager.Services;

namespace FishFarmManager.Forms
{
    /// <summary>
    /// نموذج تقارير الأصول الثابتة
    /// Asset Reports Form
    /// </summary>
    public partial class AssetReportsForm : Form
    {
        private readonly FishFarmContext _context;
        private TabControl _tabControl = null!;

        public AssetReportsForm(FishFarmContext context)
        {
            _context = context ?? throw new ArgumentNullException(nameof(context));

            if (!AuthenticationService.HasPermission(UserRole.Admin, UserRole.Manager, UserRole.Accountant))
            {
                MessageBox.Show("ليس لديك صلاحية لعرض تقارير الأصول", "تحذير",
                    MessageBoxButtons.OK, MessageBoxIcon.Warning);
                this.Load += (s, e) => this.Close();
                return;
            }

            InitializeComponent();
            InitializeForm();
        }

        private void InitializeComponent()
        {
            this.Text = "تقارير الأصول الثابتة";
            this.Size = new Size(1600, 900);
            this.StartPosition = FormStartPosition.CenterScreen;
            this.RightToLeft = RightToLeft.Yes;
            this.RightToLeftLayout = true;
            this.Font = new Font("Cairo", 10F);
            this.WindowState = FormWindowState.Maximized;

            var mainPanel = new Panel { Dock = DockStyle.Fill, Padding = new Padding(10), BackColor = Color.FromArgb(240, 240, 240) };

            var titleLabel = new Label
            {
                Text = "تقارير الأصول الثابتة الشاملة",
                Location = new Point(10, 10),
                Size = new Size(1560, 40),
                Font = new Font("Cairo", 14F, FontStyle.Bold),
                ForeColor = Color.FromArgb(46, 92, 138),
                TextAlign = ContentAlignment.MiddleRight
            };
            mainPanel.Controls.Add(titleLabel);

            _tabControl = new TabControl { Location = new Point(10, 60), Size = new Size(1560, 800), Font = new Font("Cairo", 10F) };

            CreateTabs();

            mainPanel.Controls.Add(_tabControl);
            this.Controls.Add(mainPanel);
        }

        private void CreateTabs()
        {
            _tabControl.TabPages.Add(CreateTabWithPanel("سجل الأصول", "سجل الأصول الثابتة"));
            _tabControl.TabPages.Add(CreateTabWithPanel("تقرير الإهلاك", "تقرير الإهلاك الشامل"));
            _tabControl.TabPages.Add(CreateTabWithPanel("القيمة الدفترية", "تقييم الأصول"));
            _tabControl.TabPages.Add(CreateTabWithPanel("لوحة التحكم", "لوحة تحكم الأصول"));
        }

        private TabPage CreateTabWithPanel(string tabName, string reportTitle)
        {
            var tab = new TabPage(tabName);
            var panel = new Panel { Dock = DockStyle.Fill, Padding = new Padding(20), BackColor = Color.White };

            var grid = new DataGridView { Location = new Point(20, 80), Size = new Size(1000, 650), Font = new Font("Cairo", 9F), BackgroundColor = Color.White, ReadOnly = true };
            panel.Controls.Add(grid);

            var summaryLabel = new Label 
            { 
                Text = $"ملخص {reportTitle}", 
                Location = new Point(1030, 80), 
                Size = new Size(490, 650), 
                BackColor = Color.White,
                Font = new Font("Cairo", 12F),
                TextAlign = ContentAlignment.MiddleCenter,
                BorderStyle = BorderStyle.FixedSingle
            };
            panel.Controls.Add(summaryLabel);

            var btn = new Button { Text = "إنشاء", Location = new Point(1300, 30), Size = new Size(120, 35), Font = new Font("Cairo", 10F, FontStyle.Bold) };
            btn.Click += (s, e) => MessageBox.Show($"{reportTitle} - جاهز", "معلومات", MessageBoxButtons.OK, MessageBoxIcon.Information);
            panel.Controls.Add(btn);

            tab.Controls.Add(panel);
            return tab;
        }

        private void InitializeForm()
        {
            LoggingService.LogInfo("AssetReportsForm initialized");
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing) { }
            base.Dispose(disposing);
        }
    }
}
