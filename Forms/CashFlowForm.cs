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
    /// نموذج قائمة التدفقات النقدية
    /// Cash Flow Statement Form
    /// General-ledger based; subject to external accounting review.
    /// </summary>
    public partial class CashFlowForm : Form
    {
        private readonly FishFarmContext _context;
        private readonly FinancialService _financialService;

        private DateTimePicker _startDatePicker = null!;
        private DateTimePicker _endDatePicker = null!;
        private Button _generateButton = null!;
        private Panel _reportPanel = null!;

        public CashFlowForm(FishFarmContext context)
        {
            _context = context ?? throw new ArgumentNullException(nameof(context));
            _financialService = new FinancialService(_context);

            if (!AuthenticationService.HasPermission(UserRole.Admin, UserRole.Manager, UserRole.Accountant))
            {
                MessageBox.Show("ليس لديك صلاحية لعرض قائمة التدفقات النقدية", "تحذير",
                    MessageBoxButtons.OK, MessageBoxIcon.Warning);
                this.Load += (s, e) => this.Close();
                return;
            }

            InitializeComponent();
            InitializeForm();
        }

        private void InitializeComponent()
        {
            this.Text = "قائمة التدفقات النقدية - Cash Flow Statement";
            this.Size = new Size(1200, 900);
            this.StartPosition = FormStartPosition.CenterScreen;
            this.RightToLeft = RightToLeft.Yes;
            this.RightToLeftLayout = true;
            this.Font = new Font("Cairo", 10F);
            this.WindowState = FormWindowState.Maximized;

            var mainPanel = new Panel { Dock = DockStyle.Fill, Padding = new Padding(20), BackColor = Color.FromArgb(240, 240, 240) };

            var titleLabel = new Label
            {
                Text = "قائمة التدفقات النقدية",
                Location = new Point(20, 10),
                Size = new Size(1140, 40),
                Font = new Font("Cairo", 14F, FontStyle.Bold),
                ForeColor = Color.FromArgb(46, 92, 138),
                TextAlign = ContentAlignment.MiddleRight
            };
            mainPanel.Controls.Add(titleLabel);

            CreateFilterSection(mainPanel, 60);
            CreateReportSection(mainPanel, 130);

            this.Controls.Add(mainPanel);
        }

        private void CreateFilterSection(Panel parent, int startY)
        {
            var filterPanel = new GroupBox { Text = "الفترة", Location = new Point(20, startY), Size = new Size(1140, 60), Font = new Font("Cairo", 10F, FontStyle.Bold) };

            _startDatePicker = new DateTimePicker { Location = new Point(850, 25), Size = new Size(150, 30), Format = DateTimePickerFormat.Short };
            filterPanel.Controls.Add(_startDatePicker);

            _endDatePicker = new DateTimePicker { Location = new Point(670, 25), Size = new Size(150, 30), Format = DateTimePickerFormat.Short };
            filterPanel.Controls.Add(_endDatePicker);

            _generateButton = new Button { Text = "إنشاء التقرير", Location = new Point(500, 23), Size = new Size(140, 35), Font = new Font("Cairo", 10F, FontStyle.Bold), BackColor = Color.FromArgb(38, 166, 154), ForeColor = Color.White, FlatStyle = FlatStyle.Flat, Cursor = Cursors.Hand };
            _generateButton.Click += async (s, e) => await GenerateButton_ClickAsync();
            filterPanel.Controls.Add(_generateButton);

            parent.Controls.Add(filterPanel);
        }

        private void CreateReportSection(Panel parent, int startY)
        {
            _reportPanel = new Panel { Location = new Point(20, startY), Size = new Size(1140, 700), BackColor = Color.White, BorderStyle = BorderStyle.FixedSingle, AutoScroll = true };
            parent.Controls.Add(_reportPanel);
        }

        private void InitializeForm()
        {
            _startDatePicker.Value = new DateTime(DateTime.Now.Year, 1, 1);
            _endDatePicker.Value = DateTime.Now;
            LoggingService.LogInfo("CashFlowForm initialized");
        }

        private async Task GenerateButton_ClickAsync()
        {
            try
            {
                _generateButton.Enabled = false;
                Cursor = Cursors.WaitCursor;

                var data = await _financialService.GenerateCashFlowStatementAsync(_startDatePicker.Value.Date, _endDatePicker.Value.Date);
                DisplayReport(data);

                LoggingService.LogInfo($"Generated cash flow statement");
            }
            catch (Exception ex)
            {
                LoggingService.LogError("Error generating cash flow", ex);
                MessageBox.Show($"حدث خطأ: {ex.Message}", "خطأ", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
            finally
            {
                _generateButton.Enabled = true;
                Cursor = Cursors.Default;
            }
        }

        private void DisplayReport(CashFlowData data)
        {
            _reportPanel.Controls.Clear();

            int y = 20;
            int labelWidth = 400;
            int valueWidth = 150;
            int rightX = 700;
            int valueX = rightX - valueWidth - 10;

            AddTitle(_reportPanel, "قائمة التدفقات النقدية", y);
            y += 50;

            AddSubtitle(_reportPanel, $"من {data.StartDate:yyyy-MM-dd} إلى {data.EndDate:yyyy-MM-dd}", y);
            y += 60;

            // الأنشطة التشغيلية
            AddSectionHeader(_reportPanel, "التدفقات النقدية من الأنشطة التشغيلية", y);
            y += 40;

            AddLine(_reportPanel, "صافي الربح", data.NetProfit, y, rightX, valueX, labelWidth, valueWidth);
            y += 35;

            AddLine(_reportPanel, "الإهلاك (إضافة غير نقدية)", data.DepreciationAddBack, y, rightX, valueX, labelWidth, valueWidth);
            y += 35;

            AddLine(_reportPanel, "التغير في المدينين", data.ChangeInReceivables, y, rightX, valueX, labelWidth, valueWidth);
            y += 35;

            AddLine(_reportPanel, "التغير في المخزون", data.ChangeInInventory, y, rightX, valueX, labelWidth, valueWidth);
            y += 35;

            AddLine(_reportPanel, "التغير في الدائنين", data.ChangeInPayables, y, rightX, valueX, labelWidth, valueWidth);
            y += 35;

            AddLine(_reportPanel, "تسويات تشغيلية أخرى من الأستاذ", data.OtherOperatingAdjustments, y, rightX, valueX, labelWidth, valueWidth);
            y += 40;

            AddTotalLine(_reportPanel, "صافي النقد من الأنشطة التشغيلية", data.NetCashFromOperating, y, rightX, valueX, labelWidth, valueWidth, Color.FromArgb(38, 166, 154));
            y += 60;

            // الأنشطة الاستثمارية
            AddSectionHeader(_reportPanel, "التدفقات النقدية من الأنشطة الاستثمارية", y);
            y += 40;

            AddLine(_reportPanel, "شراء أصول ثابتة", data.PurchaseOfFixedAssets, y, rightX, valueX, labelWidth, valueWidth, Color.FromArgb(229, 57, 53));
            y += 35;

            AddLine(_reportPanel, "بيع أصول ثابتة", data.SaleOfFixedAssets, y, rightX, valueX, labelWidth, valueWidth);
            y += 40;

            AddTotalLine(_reportPanel, "صافي النقد من الأنشطة الاستثمارية", data.NetCashFromInvesting, y, rightX, valueX, labelWidth, valueWidth, Color.FromArgb(255, 152, 0));
            y += 60;

            // الأنشطة التمويلية
            AddSectionHeader(_reportPanel, "التدفقات النقدية من الأنشطة التمويلية", y);
            y += 40;

            AddLine(_reportPanel, "مساهمات رأس المال", data.CapitalContributions, y, rightX, valueX, labelWidth, valueWidth);
            y += 35;

            AddLine(_reportPanel, "قروض جديدة", data.LoanProceeds, y, rightX, valueX, labelWidth, valueWidth);
            y += 35;

            AddLine(_reportPanel, "سداد قروض", data.LoanRepayments, y, rightX, valueX, labelWidth, valueWidth, Color.FromArgb(229, 57, 53));
            y += 35;

            AddLine(_reportPanel, "توزيعات أرباح", data.Dividends, y, rightX, valueX, labelWidth, valueWidth, Color.FromArgb(229, 57, 53));
            y += 40;

            AddTotalLine(_reportPanel, "صافي النقد من الأنشطة التمويلية", data.NetCashFromFinancing, y, rightX, valueX, labelWidth, valueWidth, Color.FromArgb(46, 92, 138));
            y += 60;

            // صافي التغير في النقدية
            AddGrandTotalLine(_reportPanel, "صافي التغير في النقدية", data.NetCashChange, y, rightX, valueX, labelWidth, valueWidth);
            y += 50;

            AddLine(_reportPanel, "النقدية في بداية الفترة", data.BeginningCash, y, rightX, valueX, labelWidth, valueWidth);
            y += 40;

            AddGrandTotalLine(_reportPanel, "النقدية في نهاية الفترة", data.EndingCash, y, rightX, valueX, labelWidth, valueWidth);
            y += 60;

            AddSubtitle(_reportPanel, data.IsReconciled ? "✓ التدفقات مطابقة لحركة النقدية في الأستاذ" : "✗ يوجد فرق في مطابقة حركة النقدية", y);
            y += 35;

            AddFooter(_reportPanel, $"تم الإنشاء: {data.GeneratedDate:yyyy-MM-dd HH:mm}", y);
        }

        private void AddTitle(Panel panel, string title, int y)
        {
            panel.Controls.Add(new Label { Text = title, Location = new Point(20, y), Size = new Size(1100, 40), Font = new Font("Cairo", 16F, FontStyle.Bold), ForeColor = Color.FromArgb(46, 92, 138), TextAlign = ContentAlignment.MiddleCenter });
        }

        private void AddSubtitle(Panel panel, string subtitle, int y)
        {
            panel.Controls.Add(new Label { Text = subtitle, Location = new Point(20, y), Size = new Size(1100, 30), Font = new Font("Cairo", 11F), ForeColor = Color.Gray, TextAlign = ContentAlignment.MiddleCenter });
        }

        private void AddSectionHeader(Panel panel, string header, int y)
        {
            panel.Controls.Add(new Label { Text = header, Location = new Point(320, y), Size = new Size(400, 30), Font = new Font("Cairo", 12F, FontStyle.Bold), ForeColor = Color.FromArgb(46, 92, 138), TextAlign = ContentAlignment.MiddleRight });
        }

        private void AddLine(Panel panel, string label, decimal value, int y, int rightX, int valueX, int labelWidth, int valueWidth, Color? color = null)
        {
            panel.Controls.Add(new Label { Text = label, Location = new Point(rightX - labelWidth, y), Size = new Size(labelWidth, 25), Font = new Font("Cairo", 10F), ForeColor = color ?? Color.Black, TextAlign = ContentAlignment.MiddleRight });
            panel.Controls.Add(new Label { Text = value.ToString("N2"), Location = new Point(valueX, y), Size = new Size(valueWidth, 25), Font = new Font("Cairo", 10F), ForeColor = color ?? Color.Black, TextAlign = ContentAlignment.MiddleLeft });
        }

        private void AddTotalLine(Panel panel, string label, decimal value, int y, int rightX, int valueX, int labelWidth, int valueWidth, Color color)
        {
            panel.Controls.Add(new Label { Text = label, Location = new Point(rightX - labelWidth, y), Size = new Size(labelWidth, 30), Font = new Font("Cairo", 11F, FontStyle.Bold), ForeColor = color, TextAlign = ContentAlignment.MiddleRight });
            panel.Controls.Add(new Label { Text = value.ToString("N2"), Location = new Point(valueX, y), Size = new Size(valueWidth, 30), Font = new Font("Cairo", 11F, FontStyle.Bold), ForeColor = color, TextAlign = ContentAlignment.MiddleLeft });
            panel.Controls.Add(new Panel { Location = new Point(valueX, y + 32), Size = new Size(labelWidth + valueWidth + 10, 1), BackColor = color });
        }

        private void AddGrandTotalLine(Panel panel, string label, decimal value, int y, int rightX, int valueX, int labelWidth, int valueWidth)
        {
            var color = Color.FromArgb(46, 92, 138);
            panel.Controls.Add(new Label { Text = label, Location = new Point(rightX - labelWidth, y), Size = new Size(labelWidth, 32), Font = new Font("Cairo", 12F, FontStyle.Bold), ForeColor = color, TextAlign = ContentAlignment.MiddleRight });
            panel.Controls.Add(new Label { Text = value.ToString("N2"), Location = new Point(valueX, y), Size = new Size(valueWidth, 32), Font = new Font("Cairo", 12F, FontStyle.Bold), ForeColor = color, TextAlign = ContentAlignment.MiddleLeft });
            panel.Controls.Add(new Panel { Location = new Point(valueX, y - 2), Size = new Size(labelWidth + valueWidth + 10, 2), BackColor = color });
            panel.Controls.Add(new Panel { Location = new Point(valueX, y + 34), Size = new Size(labelWidth + valueWidth + 10, 2), BackColor = color });
        }

        private void AddFooter(Panel panel, string footer, int y)
        {
            panel.Controls.Add(new Label { Text = footer, Location = new Point(320, y), Size = new Size(400, 25), Font = new Font("Cairo", 9F), ForeColor = Color.Gray, TextAlign = ContentAlignment.MiddleCenter });
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing) { }
            base.Dispose(disposing);
        }
    }
}


