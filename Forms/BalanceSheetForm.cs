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
    /// نموذج الميزانية العمومية
    /// Balance Sheet Form
    /// General-ledger based; subject to external accounting review.
    /// </summary>
    public partial class BalanceSheetForm : Form
    {
        private readonly FishFarmContext _context;
        private readonly FinancialService _financialService;
        private readonly PrintService _printService;

        private DateTimePicker _asOfDatePicker = null!;
        private Button _generateButton = null!;
        private Button _printButton = null!;
        private Panel _reportPanel = null!;
        private Label _balanceStatusLabel = null!;

        public BalanceSheetForm(FishFarmContext context)
        {
            _context = context ?? throw new ArgumentNullException(nameof(context));
            _financialService = new FinancialService(_context);
            _printService = new PrintService();

            if (!AuthenticationService.HasPermission(UserRole.Admin, UserRole.Manager, UserRole.Accountant))
            {
                MessageBox.Show("ليس لديك صلاحية لعرض الميزانية العمومية", "تحذير",
                    MessageBoxButtons.OK, MessageBoxIcon.Warning);
                this.Load += (s, e) => this.Close();
                return;
            }

            InitializeComponent();
            InitializeForm();
        }

        private void InitializeComponent()
        {
            this.Text = "الميزانية العمومية - Balance Sheet";
            this.Size = new Size(1400, 900);
            this.StartPosition = FormStartPosition.CenterScreen;
            this.RightToLeft = RightToLeft.Yes;
            this.RightToLeftLayout = true;
            this.Font = new Font("Cairo", 10F);
            this.WindowState = FormWindowState.Maximized;

            var mainPanel = new Panel { Dock = DockStyle.Fill, Padding = new Padding(20), BackColor = Color.FromArgb(240, 240, 240) };

            var titleLabel = new Label
            {
                Text = "الميزانية العمومية",
                Location = new Point(20, 10),
                Size = new Size(1340, 40),
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
            var filterPanel = new GroupBox
            {
                Text = "التاريخ",
                Location = new Point(20, startY),
                Size = new Size(1340, 60),
                Font = new Font("Cairo", 10F, FontStyle.Bold)
            };

            int x = 1050;

            var dateLabel = new Label
            {
                Text = "كما في:",
                Location = new Point(x + 150, 28),
                Size = new Size(70, 25),
                TextAlign = ContentAlignment.MiddleRight
            };
            filterPanel.Controls.Add(dateLabel);

            _asOfDatePicker = new DateTimePicker
            {
                Location = new Point(x, 25),
                Size = new Size(140, 30),
                Format = DateTimePickerFormat.Short,
                Font = new Font("Cairo", 9F)
            };
            filterPanel.Controls.Add(_asOfDatePicker);

            x -= 170;

            _generateButton = new Button
            {
                Text = "إنشاء الميزانية",
                Location = new Point(x, 23),
                Size = new Size(140, 35),
                Font = new Font("Cairo", 10F, FontStyle.Bold),
                BackColor = Color.FromArgb(38, 166, 154),
                ForeColor = Color.White,
                FlatStyle = FlatStyle.Flat,
                Cursor = Cursors.Hand
            };
            _generateButton.Click += async (s, e) => await GenerateButton_ClickAsync();
            filterPanel.Controls.Add(_generateButton);

            x -= 150;

            _printButton = new Button
            {
                Text = "طباعة",
                Location = new Point(x, 23),
                Size = new Size(120, 35),
                Font = new Font("Cairo", 10F, FontStyle.Bold),
                BackColor = Color.FromArgb(46, 92, 138),
                ForeColor = Color.White,
                FlatStyle = FlatStyle.Flat,
                Cursor = Cursors.Hand
            };
            _printButton.Click += PrintButton_Click;
            filterPanel.Controls.Add(_printButton);

            _balanceStatusLabel = new Label
            {
                Location = new Point(50, 28),
                Size = new Size(400, 30),
                Font = new Font("Cairo", 11F, FontStyle.Bold),
                TextAlign = ContentAlignment.MiddleLeft
            };
            filterPanel.Controls.Add(_balanceStatusLabel);

            parent.Controls.Add(filterPanel);
        }

        private void CreateReportSection(Panel parent, int startY)
        {
            _reportPanel = new Panel
            {
                Location = new Point(20, startY),
                Size = new Size(1340, 700),
                BackColor = Color.White,
                BorderStyle = BorderStyle.FixedSingle,
                AutoScroll = true
            };

            parent.Controls.Add(_reportPanel);
        }

        private void InitializeForm()
        {
            _asOfDatePicker.Value = DateTime.Now.Date;
            LoggingService.LogInfo("BalanceSheetForm initialized");
        }

        private async Task GenerateButton_ClickAsync()
        {
            try
            {
                _generateButton.Enabled = false;
                Cursor = Cursors.WaitCursor;

                var asOfDate = _asOfDatePicker.Value.Date;
                var data = await _financialService.GenerateBalanceSheetAsync(asOfDate);
                DisplayReport(data);

                // Update balance status
                if (data.IsBalanced)
                {
                    _balanceStatusLabel.Text = "✓ الميزانية متوازنة";
                    _balanceStatusLabel.ForeColor = Color.FromArgb(38, 166, 154);
                }
                else
                {
                    _balanceStatusLabel.Text = "✗ الميزانية غير متوازنة!";
                    _balanceStatusLabel.ForeColor = Color.FromArgb(229, 57, 53);
                }

                LoggingService.LogInfo($"Generated balance sheet as of {asOfDate:yyyy-MM-dd}");
            }
            catch (Exception ex)
            {
                LoggingService.LogError("Error generating balance sheet", ex);
                MessageBox.Show($"حدث خطأ: {ex.Message}", "خطأ", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
            finally
            {
                _generateButton.Enabled = true;
                Cursor = Cursors.Default;
            }
        }

        private void DisplayReport(BalanceSheetData data)
        {
            _reportPanel.Controls.Clear();

            int y = 20;
            int col1X = 950; // الأصول
            int col2X = 350; // الخصوم وحقوق الملكية
            int labelWidth = 300;
            int valueWidth = 150;

            // Header
            AddTitle(_reportPanel, "الميزانية العمومية", y);
            y += 50;

            AddSubtitle(_reportPanel, $"كما في {data.AsOfDate:yyyy-MM-dd}", y);
            y += 60;

            // Column Headers
            AddColumnHeader(_reportPanel, "الأصول", col1X, y);
            AddColumnHeader(_reportPanel, "الخصوم وحقوق الملكية", col2X, y);
            y += 50;

            int leftY = y; // Track left column position
            int rightY = y; // Track right column position

            // ========== الأصول (Left Column) ==========
            
            // الأصول المتداولة
            AddSectionHeader(_reportPanel, "الأصول المتداولة", col1X - 30, leftY);
            leftY += 40;

            AddLine(_reportPanel, "النقدية", data.Cash, col1X, leftY, labelWidth, valueWidth);
            leftY += 32;

            AddLine(_reportPanel, "العملاء (مدينون)", data.AccountsReceivable, col1X, leftY, labelWidth, valueWidth);
            leftY += 32;

            AddLine(_reportPanel, "المخزون", data.Inventory, col1X, leftY, labelWidth, valueWidth);
            leftY += 32;

            AddLine(_reportPanel, "أصول متداولة أخرى", data.OtherCurrentAssets, col1X, leftY, labelWidth, valueWidth);
            leftY += 38;

            AddTotalLine(_reportPanel, "إجمالي الأصول المتداولة", data.TotalCurrentAssets, col1X, leftY, labelWidth, valueWidth);
            leftY += 50;

            // الأصول الثابتة
            AddSectionHeader(_reportPanel, "الأصول الثابتة", col1X - 30, leftY);
            leftY += 40;

            AddLine(_reportPanel, "الأصول الثابتة (إجمالي)", data.FixedAssetsGross, col1X, leftY, labelWidth, valueWidth);
            leftY += 32;

            AddLine(_reportPanel, "الإهلاك المتراكم", -data.AccumulatedDepreciation, col1X, leftY, labelWidth, valueWidth, Color.FromArgb(229, 57, 53));
            leftY += 38;

            AddTotalLine(_reportPanel, "صافي الأصول الثابتة", data.FixedAssetsNet, col1X, leftY, labelWidth, valueWidth);
            leftY += 32;

            AddLine(_reportPanel, "أصول غير متداولة أخرى", data.OtherNonCurrentAssets, col1X, leftY, labelWidth, valueWidth);
            leftY += 50;

            // إجمالي الأصول
            AddGrandTotalLine(_reportPanel, "إجمالي الأصول", data.TotalAssets, col1X, leftY, labelWidth, valueWidth);
            leftY += 60;

            // ========== الخصوم وحقوق الملكية (Right Column) ==========
            
            // الخصوم المتداولة
            AddSectionHeader(_reportPanel, "الخصوم المتداولة", col2X - 30, rightY);
            rightY += 40;

            AddLine(_reportPanel, "الموردون (دائنون)", data.AccountsPayable, col2X, rightY, labelWidth, valueWidth);
            rightY += 32;

            AddLine(_reportPanel, "رواتب مستحقة", data.SalariesPayable, col2X, rightY, labelWidth, valueWidth);
            rightY += 32;

            AddLine(_reportPanel, "ضرائب مستحقة", data.TaxesPayable, col2X, rightY, labelWidth, valueWidth);
            rightY += 32;

            AddLine(_reportPanel, "خصوم متداولة أخرى", data.OtherCurrentLiabilities, col2X, rightY, labelWidth, valueWidth);
            rightY += 38;

            AddTotalLine(_reportPanel, "إجمالي الخصوم المتداولة", data.TotalCurrentLiabilities, col2X, rightY, labelWidth, valueWidth);
            rightY += 50;

            // الخصوم طويلة الأجل
            AddSectionHeader(_reportPanel, "الخصوم طويلة الأجل", col2X - 30, rightY);
            rightY += 40;

            AddLine(_reportPanel, "قروض طويلة الأجل", data.LongTermLoans, col2X, rightY, labelWidth, valueWidth);
            rightY += 32;

            AddLine(_reportPanel, "خصوم أخرى طويلة الأجل", data.OtherLongTermLiabilities, col2X, rightY, labelWidth, valueWidth);
            rightY += 38;

            AddTotalLine(_reportPanel, "إجمالي الخصوم طويلة الأجل", data.TotalLongTermLiabilities, col2X, rightY, labelWidth, valueWidth);
            rightY += 40;

            AddTotalLine(_reportPanel, "إجمالي الخصوم", data.TotalLiabilities, col2X, rightY, labelWidth, valueWidth, Color.FromArgb(229, 57, 53));
            rightY += 50;

            // حقوق الملكية
            AddSectionHeader(_reportPanel, "حقوق الملكية", col2X - 30, rightY);
            rightY += 40;

            AddLine(_reportPanel, "رأس المال", data.Capital, col2X, rightY, labelWidth, valueWidth);
            rightY += 32;

            AddLine(_reportPanel, "الأرباح المحتجزة", data.RetainedEarnings, col2X, rightY, labelWidth, valueWidth);
            rightY += 32;

            AddLine(_reportPanel, "حقوق ملكية أخرى", data.OtherEquity, col2X, rightY, labelWidth, valueWidth);
            rightY += 32;

            AddLine(_reportPanel, "ربح العام الحالي", data.CurrentYearProfit, col2X, rightY, labelWidth, valueWidth);
            rightY += 38;

            AddTotalLine(_reportPanel, "إجمالي حقوق الملكية", data.TotalEquity, col2X, rightY, labelWidth, valueWidth, Color.FromArgb(38, 166, 154));
            rightY += 50;

            // إجمالي الخصوم وحقوق الملكية
            AddGrandTotalLine(_reportPanel, "إجمالي الخصوم وحقوق الملكية", data.TotalLiabilitiesAndEquity, col2X, rightY, labelWidth, valueWidth);
            rightY += 60;

            // Footer
            int footerY = Math.Max(leftY, rightY);
            AddFooter(_reportPanel, $"تم الإنشاء: {data.GeneratedDate:yyyy-MM-dd HH:mm}", footerY);
        }

        private void AddTitle(Panel panel, string title, int y)
        {
            var label = new Label
            {
                Text = title,
                Location = new Point(20, y),
                Size = new Size(1300, 40),
                Font = new Font("Cairo", 16F, FontStyle.Bold),
                ForeColor = Color.FromArgb(46, 92, 138),
                TextAlign = ContentAlignment.MiddleCenter
            };
            panel.Controls.Add(label);
        }

        private void AddSubtitle(Panel panel, string subtitle, int y)
        {
            var label = new Label
            {
                Text = subtitle,
                Location = new Point(20, y),
                Size = new Size(1300, 30),
                Font = new Font("Cairo", 11F),
                ForeColor = Color.Gray,
                TextAlign = ContentAlignment.MiddleCenter
            };
            panel.Controls.Add(label);
        }

        private void AddColumnHeader(Panel panel, string header, int x, int y)
        {
            var label = new Label
            {
                Text = header,
                Location = new Point(x - 280, y),
                Size = new Size(450, 35),
                Font = new Font("Cairo", 12F, FontStyle.Bold),
                ForeColor = Color.White,
                BackColor = Color.FromArgb(46, 92, 138),
                TextAlign = ContentAlignment.MiddleCenter
            };
            panel.Controls.Add(label);
        }

        private void AddSectionHeader(Panel panel, string header, int x, int y)
        {
            var label = new Label
            {
                Text = header,
                Location = new Point(x, y),
                Size = new Size(300, 28),
                Font = new Font("Cairo", 11F, FontStyle.Bold),
                ForeColor = Color.FromArgb(46, 92, 138),
                TextAlign = ContentAlignment.MiddleRight
            };
            panel.Controls.Add(label);
        }

        private void AddLine(Panel panel, string label, decimal value, int x, int y, int labelWidth, int valueWidth, Color? textColor = null)
        {
            var color = textColor ?? Color.Black;

            var labelControl = new Label
            {
                Text = label,
                Location = new Point(x - labelWidth, y),
                Size = new Size(labelWidth, 25),
                Font = new Font("Cairo", 9F),
                ForeColor = color,
                TextAlign = ContentAlignment.MiddleRight
            };
            panel.Controls.Add(labelControl);

            var valueControl = new Label
            {
                Text = value.ToString("N2"),
                Location = new Point(x, y),
                Size = new Size(valueWidth, 25),
                Font = new Font("Cairo", 9F),
                ForeColor = color,
                TextAlign = ContentAlignment.MiddleLeft
            };
            panel.Controls.Add(valueControl);
        }

        private void AddTotalLine(Panel panel, string label, decimal value, int x, int y, int labelWidth, int valueWidth, Color? lineColor = null)
        {
            var color = lineColor ?? Color.FromArgb(46, 92, 138);

            var labelControl = new Label
            {
                Text = label,
                Location = new Point(x - labelWidth, y),
                Size = new Size(labelWidth, 28),
                Font = new Font("Cairo", 10F, FontStyle.Bold),
                ForeColor = color,
                TextAlign = ContentAlignment.MiddleRight
            };
            panel.Controls.Add(labelControl);

            var valueControl = new Label
            {
                Text = value.ToString("N2"),
                Location = new Point(x, y),
                Size = new Size(valueWidth, 28),
                Font = new Font("Cairo", 10F, FontStyle.Bold),
                ForeColor = color,
                TextAlign = ContentAlignment.MiddleLeft
            };
            panel.Controls.Add(valueControl);

            var line = new Panel
            {
                Location = new Point(x, y + 30),
                Size = new Size(labelWidth + valueWidth, 1),
                BackColor = color
            };
            panel.Controls.Add(line);
        }

        private void AddGrandTotalLine(Panel panel, string label, decimal value, int x, int y, int labelWidth, int valueWidth)
        {
            var color = Color.FromArgb(46, 92, 138);

            var labelControl = new Label
            {
                Text = label,
                Location = new Point(x - labelWidth, y),
                Size = new Size(labelWidth, 32),
                Font = new Font("Cairo", 12F, FontStyle.Bold),
                ForeColor = color,
                TextAlign = ContentAlignment.MiddleRight
            };
            panel.Controls.Add(labelControl);

            var valueControl = new Label
            {
                Text = value.ToString("N2"),
                Location = new Point(x, y),
                Size = new Size(valueWidth, 32),
                Font = new Font("Cairo", 12F, FontStyle.Bold),
                ForeColor = color,
                TextAlign = ContentAlignment.MiddleLeft
            };
            panel.Controls.Add(valueControl);

            var topLine = new Panel
            {
                Location = new Point(x, y - 2),
                Size = new Size(labelWidth + valueWidth, 2),
                BackColor = color
            };
            panel.Controls.Add(topLine);

            var bottomLine = new Panel
            {
                Location = new Point(x, y + 34),
                Size = new Size(labelWidth + valueWidth, 2),
                BackColor = color
            };
            panel.Controls.Add(bottomLine);
        }

        private void AddFooter(Panel panel, string footer, int y)
        {
            var label = new Label
            {
                Text = footer,
                Location = new Point(20, y),
                Size = new Size(1300, 25),
                Font = new Font("Cairo", 9F),
                ForeColor = Color.Gray,
                TextAlign = ContentAlignment.MiddleCenter
            };
            panel.Controls.Add(label);
        }

        private void PrintButton_Click(object? sender, EventArgs e)
        {
            try
            {
                var dataTable = new System.Data.DataTable();
                dataTable.Columns.Add("البند", typeof(string));
                dataTable.Columns.Add("المبلغ", typeof(string));
                dataTable.Rows.Add("الأصول", "0.00");
                dataTable.Rows.Add("الخصوم", "0.00");
                dataTable.Rows.Add("حقوق الملكية", "0.00");

                var pdfPath = _printService.CreateFinancialReportPDF(
                    "الميزانية العمومية - Balance Sheet",
                    "AquaFarm Pro",
                    dataTable,
                    _asOfDatePicker.Value,
                    _asOfDatePicker.Value
                );

                var result = MessageBox.Show(
                    $"تم إنشاء ملف PDF بنجاح:\n{pdfPath}\n\nهل تريد فتحه؟",
                    "نجح", MessageBoxButtons.YesNo, MessageBoxIcon.Information
                );

                if (result == DialogResult.Yes)
                    _printService.PrintPDF(pdfPath);

                LoggingService.LogInfo("Balance sheet printed");
            }
            catch (Exception ex)
            {
                LoggingService.LogError(ex, "Error printing balance sheet");
                MessageBox.Show($"خطأ: {ex.Message}", "خطأ", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing) { }
            base.Dispose(disposing);
        }
    }
}


