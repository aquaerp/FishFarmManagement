using System;
using System.Drawing;
using System.Linq;
using System.Threading.Tasks;
using System.Windows.Forms;
using FishFarmManager.Data;
using FishFarmManager.Models;
using FishFarmManager.Services;
using Microsoft.EntityFrameworkCore;

namespace FishFarmManager.Forms
{
    /// <summary>
    /// نموذج إقرار ضريبة القيمة المضافة
    /// VAT Return Form - Saudi Arabia
    /// </summary>
    public partial class VATReturnForm : Form
    {
        private readonly FishFarmContext _context;
        private readonly PrintService _printService;
        private VATReturn? _currentVATReturn;

        // Controls
        private TabControl _tabControl = null!;
        private ComboBox _periodComboBox = null!;
        private DateTimePicker _startDatePicker = null!;
        private DateTimePicker _endDatePicker = null!;
        private TextBox _taxRegistrationTextBox = null!;
        private ComboBox _statusComboBox = null!;
        private Button _calculateButton = null!;
        private Button _saveButton = null!;
        private Button _submitButton = null!;
        private Button _printButton = null!;
        private Label _statusLabel = null!;

        // VAT Return Boxes TextBoxes
        private TextBox[] _vatBoxes = new TextBox[16]; // Box 0-15 (Box 0 not used)

        public VATReturnForm(FishFarmContext context, VATReturn? vatReturn = null)
        {
            _context = context ?? throw new ArgumentNullException(nameof(context));
            _printService = new PrintService();
            _currentVATReturn = vatReturn;

            if (!AuthenticationService.HasPermission(UserRole.Admin, UserRole.Manager, UserRole.Accountant))
            {
                MessageBox.Show("ليس لديك صلاحية لإدارة إقرارات ضريبة القيمة المضافة", "تحذير",
                    MessageBoxButtons.OK, MessageBoxIcon.Warning);
                this.Load += (s, e) => this.Close();
                return;
            }

            InitializeComponent();
            InitializeForm();
        }

        private void InitializeComponent()
        {
            this.Text = "إقرار ضريبة القيمة المضافة - VAT Return";
            this.Size = new Size(1400, 900);
            this.StartPosition = FormStartPosition.CenterScreen;
            this.RightToLeft = RightToLeft.Yes;
            this.RightToLeftLayout = true;
            this.Font = new Font("Cairo", 10F);
            this.WindowState = FormWindowState.Maximized;

            var mainPanel = new Panel { Dock = DockStyle.Fill, Padding = new Padding(20), BackColor = Color.FromArgb(240, 240, 240) };

            var titleLabel = new Label
            {
                Text = "إقرار ضريبة القيمة المضافة",
                Location = new Point(20, 10),
                Size = new Size(1340, 40),
                Font = new Font("Cairo", 14F, FontStyle.Bold),
                ForeColor = Color.FromArgb(46, 92, 138),
                TextAlign = ContentAlignment.MiddleRight
            };
            mainPanel.Controls.Add(titleLabel);

            CreateHeaderSection(mainPanel, 60);
            CreateTabSection(mainPanel, 150);
            CreateButtonSection(mainPanel, 780);

            this.Controls.Add(mainPanel);
        }

        private void CreateHeaderSection(Panel parent, int startY)
        {
            var headerPanel = new GroupBox
            {
                Text = "معلومات الإقرار",
                Location = new Point(20, startY),
                Size = new Size(1340, 80),
                Font = new Font("Cairo", 10F, FontStyle.Bold)
            };

            int x = 1050;

            // Period
            var periodLabel = new Label { Text = "الفترة:", Location = new Point(x + 150, 25), Size = new Size(70, 25), TextAlign = ContentAlignment.MiddleRight };
            headerPanel.Controls.Add(periodLabel);

            _periodComboBox = new ComboBox
            {
                Location = new Point(x, 22),
                Size = new Size(140, 30),
                DropDownStyle = ComboBoxStyle.DropDownList,
                Font = new Font("Cairo", 9F)
            };
            _periodComboBox.Items.AddRange(new[] { "شهري", "ربع سنوي", "سنوي" });
            _periodComboBox.SelectedIndex = 0;
            headerPanel.Controls.Add(_periodComboBox);

            x -= 200;

            // Start Date
            var startDateLabel = new Label { Text = "من:", Location = new Point(x + 150, 25), Size = new Size(30, 25), TextAlign = ContentAlignment.MiddleRight };
            headerPanel.Controls.Add(startDateLabel);

            _startDatePicker = new DateTimePicker
            {
                Location = new Point(x, 22),
                Size = new Size(140, 30),
                Format = DateTimePickerFormat.Short,
                Font = new Font("Cairo", 9F)
            };
            headerPanel.Controls.Add(_startDatePicker);

            x -= 200;

            // End Date
            var endDateLabel = new Label { Text = "إلى:", Location = new Point(x + 150, 25), Size = new Size(30, 25), TextAlign = ContentAlignment.MiddleRight };
            headerPanel.Controls.Add(endDateLabel);

            _endDatePicker = new DateTimePicker
            {
                Location = new Point(x, 22),
                Size = new Size(140, 30),
                Format = DateTimePickerFormat.Short,
                Font = new Font("Cairo", 9F)
            };
            headerPanel.Controls.Add(_endDatePicker);

            x -= 200;

            // Tax Registration Number
            var taxRegLabel = new Label { Text = "الرقم الضريبي:", Location = new Point(x + 150, 25), Size = new Size(100, 25), TextAlign = ContentAlignment.MiddleRight };
            headerPanel.Controls.Add(taxRegLabel);

            _taxRegistrationTextBox = new TextBox
            {
                Location = new Point(x, 22),
                Size = new Size(140, 30),
                Font = new Font("Cairo", 9F),
                Text = "300000000000003" // Default Saudi VAT number format
            };
            headerPanel.Controls.Add(_taxRegistrationTextBox);

            x -= 200;

            // Status
            var statusLabel = new Label { Text = "الحالة:", Location = new Point(x + 150, 25), Size = new Size(50, 25), TextAlign = ContentAlignment.MiddleRight };
            headerPanel.Controls.Add(statusLabel);

            _statusComboBox = new ComboBox
            {
                Location = new Point(x, 22),
                Size = new Size(140, 30),
                DropDownStyle = ComboBoxStyle.DropDownList,
                Font = new Font("Cairo", 9F)
            };
            foreach (VATReturnStatus status in Enum.GetValues(typeof(VATReturnStatus)))
            {
                _statusComboBox.Items.Add(status.ToString());
            }
            _statusComboBox.SelectedIndex = 0;
            headerPanel.Controls.Add(_statusComboBox);

            parent.Controls.Add(headerPanel);
        }

        private void CreateTabSection(Panel parent, int startY)
        {
            _tabControl = new TabControl
            {
                Location = new Point(20, startY),
                Size = new Size(1340, 620),
                Font = new Font("Cairo", 10F)
            };

            // Sales Tab
            var salesTab = new TabPage("المبيعات") { BackColor = Color.White };
            CreateSalesSection(salesTab);
            _tabControl.TabPages.Add(salesTab);

            // Purchases Tab
            var purchasesTab = new TabPage("المشتريات") { BackColor = Color.White };
            CreatePurchasesSection(purchasesTab);
            _tabControl.TabPages.Add(purchasesTab);

            // Summary Tab
            var summaryTab = new TabPage("الملخص") { BackColor = Color.White };
            CreateSummarySection(summaryTab);
            _tabControl.TabPages.Add(summaryTab);

            parent.Controls.Add(_tabControl);
        }

        private void CreateSalesSection(TabPage tab)
        {
            int y = 30;
            int labelWidth = 400;
            int textBoxWidth = 200;
            int rightX = 900;

            // Sales Boxes (1-6)
            AddVATBox(tab, 1, "المبيعات المحلية الخاضعة لضريبة القيمة المضافة", y, rightX, labelWidth, textBoxWidth);
            y += 40;

            AddVATBox(tab, 2, "مبيعات بسعر الصفر", y, rightX, labelWidth, textBoxWidth);
            y += 40;

            AddVATBox(tab, 3, "الصادرات", y, rightX, labelWidth, textBoxWidth);
            y += 40;

            AddVATBox(tab, 4, "المبيعات المعفاة", y, rightX, labelWidth, textBoxWidth);
            y += 40;

            AddVATBox(tab, 5, "إجمالي المبيعات", y, rightX, labelWidth, textBoxWidth, true);
            y += 40;

            AddVATBox(tab, 6, "ضريبة القيمة المضافة على المبيعات", y, rightX, labelWidth, textBoxWidth, true);
            y += 60;

            // Calculate Sales Button
            var calculateSalesButton = new Button
            {
                Text = "حساب ضريبة المبيعات",
                Location = new Point(600, y),
                Size = new Size(200, 35),
                Font = new Font("Cairo", 10F, FontStyle.Bold),
                BackColor = Color.FromArgb(38, 166, 154),
                ForeColor = Color.White,
                FlatStyle = FlatStyle.Flat,
                Cursor = Cursors.Hand
            };
            calculateSalesButton.Click += CalculateSalesButton_Click;
            tab.Controls.Add(calculateSalesButton);
        }

        private void CreatePurchasesSection(TabPage tab)
        {
            int y = 30;
            int labelWidth = 400;
            int textBoxWidth = 200;
            int rightX = 900;

            // Purchase Boxes (7-10)
            AddVATBox(tab, 7, "إجمالي المشتريات", y, rightX, labelWidth, textBoxWidth);
            y += 40;

            AddVATBox(tab, 8, "مشتريات من دول مجلس التعاون الخليجي", y, rightX, labelWidth, textBoxWidth);
            y += 40;

            AddVATBox(tab, 9, "الواردات الخاضعة للضريبة", y, rightX, labelWidth, textBoxWidth);
            y += 40;

            AddVATBox(tab, 10, "ضريبة القيمة المضافة على المشتريات", y, rightX, labelWidth, textBoxWidth, true);
            y += 60;

            // Calculate Purchases Button
            var calculatePurchasesButton = new Button
            {
                Text = "حساب ضريبة المشتريات",
                Location = new Point(600, y),
                Size = new Size(200, 35),
                Font = new Font("Cairo", 10F, FontStyle.Bold),
                BackColor = Color.FromArgb(74, 144, 226),
                ForeColor = Color.White,
                FlatStyle = FlatStyle.Flat,
                Cursor = Cursors.Hand
            };
            calculatePurchasesButton.Click += CalculatePurchasesButton_Click;
            tab.Controls.Add(calculatePurchasesButton);
        }

        private void CreateSummarySection(TabPage tab)
        {
            int y = 30;
            int labelWidth = 400;
            int textBoxWidth = 200;
            int rightX = 900;

            // Summary Boxes (11-15)
            AddVATBox(tab, 11, "صافي ضريبة القيمة المضافة المستحقة", y, rightX, labelWidth, textBoxWidth, true, Color.FromArgb(229, 57, 53));
            y += 40;

            AddVATBox(tab, 12, "التعديلات", y, rightX, labelWidth, textBoxWidth);
            y += 40;

            AddVATBox(tab, 13, "إجمالي ضريبة القيمة المضافة المستحقة", y, rightX, labelWidth, textBoxWidth, true, Color.FromArgb(46, 92, 138));
            y += 40;

            AddVATBox(tab, 14, "المبلغ المسترد من الفترة السابقة", y, rightX, labelWidth, textBoxWidth);
            y += 40;

            AddVATBox(tab, 15, "صافي ضريبة القيمة المضافة المستحقة للفترة", y, rightX, labelWidth, textBoxWidth, true, Color.FromArgb(38, 166, 154));
            y += 60;

            // Status Label
            _statusLabel = new Label
            {
                Location = new Point(300, y),
                Size = new Size(600, 40),
                Font = new Font("Cairo", 12F, FontStyle.Bold),
                ForeColor = Color.FromArgb(46, 92, 138),
                TextAlign = ContentAlignment.MiddleCenter,
                BorderStyle = BorderStyle.FixedSingle,
                BackColor = Color.FromArgb(240, 240, 240)
            };
            tab.Controls.Add(_statusLabel);
        }

        private void AddVATBox(TabPage tab, int boxNumber, string label, int y, int rightX, int labelWidth, int textBoxWidth, bool readOnly = false, Color? textColor = null)
        {
            var labelControl = new Label
            {
                Text = $"({boxNumber}) {label}:",
                Location = new Point(rightX - labelWidth, y),
                Size = new Size(labelWidth, 30),
                Font = new Font("Cairo", 10F),
                ForeColor = textColor ?? Color.Black,
                TextAlign = ContentAlignment.MiddleRight
            };
            tab.Controls.Add(labelControl);

            _vatBoxes[boxNumber] = new TextBox
            {
                Location = new Point(rightX, y),
                Size = new Size(textBoxWidth, 30),
                Font = new Font("Cairo", 10F),
                Text = "0.00",
                TextAlign = HorizontalAlignment.Right,
                ReadOnly = readOnly,
                BackColor = readOnly ? Color.FromArgb(240, 240, 240) : Color.White,
                ForeColor = textColor ?? Color.Black
            };

            if (!readOnly)
            {
                _vatBoxes[boxNumber].KeyPress += VATBox_KeyPress;
                _vatBoxes[boxNumber].Leave += VATBox_Leave;
            }

            tab.Controls.Add(_vatBoxes[boxNumber]);
        }

        private void CreateButtonSection(Panel parent, int startY)
        {
            var buttonPanel = new Panel
            {
                Location = new Point(20, startY),
                Size = new Size(1340, 60),
                BackColor = Color.Transparent
            };

            int x = 1100;

            _calculateButton = new Button
            {
                Text = "حساب تلقائي",
                Location = new Point(x, 15),
                Size = new Size(120, 35),
                Font = new Font("Cairo", 10F, FontStyle.Bold),
                BackColor = Color.FromArgb(255, 152, 0),
                ForeColor = Color.White,
                FlatStyle = FlatStyle.Flat,
                Cursor = Cursors.Hand
            };
            _calculateButton.Click += async (s, e) => await CalculateButton_ClickAsync();
            buttonPanel.Controls.Add(_calculateButton);

            x -= 140;

            _saveButton = new Button
            {
                Text = "حفظ",
                Location = new Point(x, 15),
                Size = new Size(120, 35),
                Font = new Font("Cairo", 10F, FontStyle.Bold),
                BackColor = Color.FromArgb(38, 166, 154),
                ForeColor = Color.White,
                FlatStyle = FlatStyle.Flat,
                Cursor = Cursors.Hand
            };
            _saveButton.Click += async (s, e) => await SaveButton_ClickAsync();
            buttonPanel.Controls.Add(_saveButton);

            x -= 140;

            _submitButton = new Button
            {
                Text = "تقديم",
                Location = new Point(x, 15),
                Size = new Size(120, 35),
                Font = new Font("Cairo", 10F, FontStyle.Bold),
                BackColor = Color.FromArgb(46, 92, 138),
                ForeColor = Color.White,
                FlatStyle = FlatStyle.Flat,
                Cursor = Cursors.Hand
            };
            _submitButton.Click += SubmitButton_Click;
            buttonPanel.Controls.Add(_submitButton);

            x -= 140;

            _printButton = new Button
            {
                Text = "طباعة",
                Location = new Point(x, 15),
                Size = new Size(120, 35),
                Font = new Font("Cairo", 10F, FontStyle.Bold),
                BackColor = Color.FromArgb(156, 39, 176),
                ForeColor = Color.White,
                FlatStyle = FlatStyle.Flat,
                Cursor = Cursors.Hand
            };
            _printButton.Click += PrintButton_Click;
            buttonPanel.Controls.Add(_printButton);

            parent.Controls.Add(buttonPanel);
        }

        private void InitializeForm()
        {
            // Set default dates for current month
            var now = DateTime.Now;
            _startDatePicker.Value = new DateTime(now.Year, now.Month, 1);
            _endDatePicker.Value = _startDatePicker.Value.AddMonths(1).AddDays(-1);

            // Load existing VAT return if provided
            if (_currentVATReturn != null)
            {
                LoadVATReturnData();
            }

            LoggingService.LogInfo("VATReturnForm initialized");
        }

        private void LoadVATReturnData()
        {
            if (_currentVATReturn == null) return;

            _startDatePicker.Value = _currentVATReturn.PeriodStartDate;
            _endDatePicker.Value = _currentVATReturn.PeriodEndDate;
            _taxRegistrationTextBox.Text = _currentVATReturn.TaxRegistrationNumber;
            _statusComboBox.SelectedItem = _currentVATReturn.Status.ToString();

            // Load VAT amounts
            _vatBoxes[1].Text = _currentVATReturn.Box1_TaxableSalesInKSA.ToString("N2");
            _vatBoxes[2].Text = _currentVATReturn.Box2_ZeroRatedSales.ToString("N2");
            _vatBoxes[3].Text = _currentVATReturn.Box3_Exports.ToString("N2");
            _vatBoxes[4].Text = _currentVATReturn.Box4_ExemptSales.ToString("N2");
            _vatBoxes[5].Text = _currentVATReturn.Box5_TotalSales.ToString("N2");
            _vatBoxes[6].Text = _currentVATReturn.Box6_VATOnSales.ToString("N2");
            _vatBoxes[7].Text = _currentVATReturn.Box7_TotalPurchases.ToString("N2");
            _vatBoxes[8].Text = _currentVATReturn.Box8_GCCPurchases.ToString("N2");
            _vatBoxes[9].Text = _currentVATReturn.Box9_TaxableImports.ToString("N2");
            _vatBoxes[10].Text = _currentVATReturn.Box10_VATOnPurchases.ToString("N2");
            _vatBoxes[11].Text = _currentVATReturn.Box11_NetVATDue.ToString("N2");
            _vatBoxes[12].Text = _currentVATReturn.Box12_Adjustments.ToString("N2");
            _vatBoxes[13].Text = _currentVATReturn.Box13_TotalVATDue.ToString("N2");
            _vatBoxes[14].Text = _currentVATReturn.Box14_RecoverablePreviousPeriod.ToString("N2");
            _vatBoxes[15].Text = _currentVATReturn.Box15_NetVATDueForPeriod.ToString("N2");

            UpdateStatusLabel();
        }

        private void VATBox_KeyPress(object? sender, KeyPressEventArgs e)
        {
            if (!char.IsControl(e.KeyChar) && !char.IsDigit(e.KeyChar) && e.KeyChar != '.' && e.KeyChar != '-')
            {
                e.Handled = true;
            }
        }

        private void VATBox_Leave(object? sender, EventArgs e)
        {
            if (sender is TextBox textBox)
            {
                if (decimal.TryParse(textBox.Text, out decimal value))
                {
                    textBox.Text = value.ToString("N2");
                }
                else
                {
                    textBox.Text = "0.00";
                }
            }
        }

        private void CalculateSalesButton_Click(object? sender, EventArgs e)
        {
            // Calculate Box 5 (Total Sales)
            decimal box1 = decimal.Parse(_vatBoxes[1].Text);
            decimal box2 = decimal.Parse(_vatBoxes[2].Text);
            decimal box3 = decimal.Parse(_vatBoxes[3].Text);
            decimal box4 = decimal.Parse(_vatBoxes[4].Text);
            
            _vatBoxes[5].Text = (box1 + box2 + box3 + box4).ToString("N2");

            // Calculate Box 6 (VAT on Sales) - 15% of taxable sales
            _vatBoxes[6].Text = (box1 * 0.15m).ToString("N2");

            CalculateSummary();
        }

        private void CalculatePurchasesButton_Click(object? sender, EventArgs e)
        {
            // Box 10 calculation can be based on actual purchase data
            // For now, we'll allow manual entry
            CalculateSummary();
        }

        private void CalculateSummary()
        {
            decimal box6 = decimal.Parse(_vatBoxes[6].Text);
            decimal box10 = decimal.Parse(_vatBoxes[10].Text);
            decimal box12 = decimal.Parse(_vatBoxes[12].Text);
            decimal box14 = decimal.Parse(_vatBoxes[14].Text);

            // Box 11: Net VAT Due
            decimal box11 = box6 - box10;
            _vatBoxes[11].Text = box11.ToString("N2");

            // Box 13: Total VAT Due
            decimal box13 = box11 + box12;
            _vatBoxes[13].Text = box13.ToString("N2");

            // Box 15: Net VAT Due for Period
            decimal box15 = box13 - box14;
            _vatBoxes[15].Text = box15.ToString("N2");

            UpdateStatusLabel();
        }

        private void UpdateStatusLabel()
        {
            decimal netVAT = decimal.Parse(_vatBoxes[15].Text);
            
            if (netVAT > 0)
            {
                _statusLabel.Text = $"مستحق للحكومة: {netVAT:N2} ريال";
                _statusLabel.ForeColor = Color.FromArgb(229, 57, 53);
                _statusLabel.BackColor = Color.FromArgb(255, 235, 238);
            }
            else if (netVAT < 0)
            {
                _statusLabel.Text = $"مستحق للشركة: {Math.Abs(netVAT):N2} ريال";
                _statusLabel.ForeColor = Color.FromArgb(38, 166, 154);
                _statusLabel.BackColor = Color.FromArgb(224, 247, 250);
            }
            else
            {
                _statusLabel.Text = "لا يوجد مبلغ مستحق";
                _statusLabel.ForeColor = Color.FromArgb(117, 117, 117);
                _statusLabel.BackColor = Color.FromArgb(245, 245, 245);
            }
        }

        private async Task CalculateButton_ClickAsync()
        {
            try
            {
                _calculateButton.Enabled = false;
                Cursor = Cursors.WaitCursor;

                var startDate = _startDatePicker.Value.Date;
                var endDate = _endDatePicker.Value.Date;

                // Calculate sales from actual data
                var taxableSales = await _context.TaxInvoices
                    .Where(t => t.IssueDate >= startDate && t.IssueDate <= endDate && t.InvoiceType == InvoiceType.Standard)
                    .SumAsync(t => t.SubTotal - t.DiscountAmount);

                var vatOnSales = await _context.TaxInvoices
                    .Where(t => t.IssueDate >= startDate && t.IssueDate <= endDate && t.InvoiceType == InvoiceType.Standard)
                    .SumAsync(t => t.VATAmount);

                // Calculate purchases from actual data
                var totalPurchases = await _context.PurchaseOrders
                    .Where(p => p.OrderDate >= startDate && p.OrderDate <= endDate)
                    .SumAsync(p => p.Total - p.VATAmount);

                var vatOnPurchases = await _context.PurchaseOrders
                    .Where(p => p.OrderDate >= startDate && p.OrderDate <= endDate)
                    .SumAsync(p => p.VATAmount);

                // Update boxes
                _vatBoxes[1].Text = taxableSales.ToString("N2");
                _vatBoxes[6].Text = vatOnSales.ToString("N2");
                _vatBoxes[7].Text = totalPurchases.ToString("N2");
                _vatBoxes[10].Text = vatOnPurchases.ToString("N2");

                CalculateSalesButton_Click(null, EventArgs.Empty);

                MessageBox.Show("تم حساب البيانات تلقائياً من السجلات", "نجح", MessageBoxButtons.OK, MessageBoxIcon.Information);
                LoggingService.LogInfo("VAT return calculated automatically");
            }
            catch (Exception ex)
            {
                LoggingService.LogError("Error calculating VAT return", ex);
                MessageBox.Show($"حدث خطأ: {ex.Message}", "خطأ", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
            finally
            {
                _calculateButton.Enabled = true;
                Cursor = Cursors.Default;
            }
        }

        private async Task SaveButton_ClickAsync()
        {
            try
            {
                _saveButton.Enabled = false;
                Cursor = Cursors.WaitCursor;

                if (_currentVATReturn == null)
                {
                    _currentVATReturn = new VATReturn();
                    _context.VATReturns.Add(_currentVATReturn);
                }

                // Update VAT return data
                _currentVATReturn.PeriodStartDate = _startDatePicker.Value.Date;
                _currentVATReturn.PeriodEndDate = _endDatePicker.Value.Date;
                _currentVATReturn.TaxRegistrationNumber = _taxRegistrationTextBox.Text;
                _currentVATReturn.Status = Enum.Parse<VATReturnStatus>(_statusComboBox.SelectedItem?.ToString() ?? "Draft");
                
                _currentVATReturn.Box1_TaxableSalesInKSA = decimal.Parse(_vatBoxes[1].Text);
                _currentVATReturn.Box2_ZeroRatedSales = decimal.Parse(_vatBoxes[2].Text);
                _currentVATReturn.Box3_Exports = decimal.Parse(_vatBoxes[3].Text);
                _currentVATReturn.Box4_ExemptSales = decimal.Parse(_vatBoxes[4].Text);
                _currentVATReturn.Box5_TotalSales = decimal.Parse(_vatBoxes[5].Text);
                _currentVATReturn.Box6_VATOnSales = decimal.Parse(_vatBoxes[6].Text);
                _currentVATReturn.Box7_TotalPurchases = decimal.Parse(_vatBoxes[7].Text);
                _currentVATReturn.Box8_GCCPurchases = decimal.Parse(_vatBoxes[8].Text);
                _currentVATReturn.Box9_TaxableImports = decimal.Parse(_vatBoxes[9].Text);
                _currentVATReturn.Box10_VATOnPurchases = decimal.Parse(_vatBoxes[10].Text);
                _currentVATReturn.Box11_NetVATDue = decimal.Parse(_vatBoxes[11].Text);
                _currentVATReturn.Box12_Adjustments = decimal.Parse(_vatBoxes[12].Text);
                _currentVATReturn.Box13_TotalVATDue = decimal.Parse(_vatBoxes[13].Text);
                _currentVATReturn.Box14_RecoverablePreviousPeriod = decimal.Parse(_vatBoxes[14].Text);
                _currentVATReturn.Box15_NetVATDueForPeriod = decimal.Parse(_vatBoxes[15].Text);

                _currentVATReturn.GeneratePeriodNumber();
                _currentVATReturn.SetDueDate();
                _currentVATReturn.CreatedById = AuthenticationService.CurrentUser?.UserId ?? 1;
                _currentVATReturn.ModifiedById = AuthenticationService.CurrentUser?.UserId ?? 1;
                _currentVATReturn.ModifiedDate = DateTime.Now;

                await _context.SaveChangesAsync();

                MessageBox.Show("تم حفظ إقرار ضريبة القيمة المضافة بنجاح", "نجح", MessageBoxButtons.OK, MessageBoxIcon.Information);
                LoggingService.LogInfo($"VAT return saved: {_currentVATReturn.PeriodNumber}");
            }
            catch (Exception ex)
            {
                LoggingService.LogError("Error saving VAT return", ex);
                MessageBox.Show($"حدث خطأ: {ex.Message}", "خطأ", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
            finally
            {
                _saveButton.Enabled = true;
                Cursor = Cursors.Default;
            }
        }

        private void SubmitButton_Click(object? sender, EventArgs e)
        {
            if (_currentVATReturn == null)
            {
                MessageBox.Show("يجب حفظ الإقرار أولاً", "تحذير", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            var result = MessageBox.Show("هل أنت متأكد من تقديم الإقرار؟ لن يمكن تعديله بعد التقديم", "تأكيد",
                MessageBoxButtons.YesNo, MessageBoxIcon.Question);

            if (result == DialogResult.Yes)
            {
                _currentVATReturn.Status = VATReturnStatus.Submitted;
                _currentVATReturn.SubmissionDate = DateTime.Now;
                _statusComboBox.SelectedItem = VATReturnStatus.Submitted.ToString();
                
                MessageBox.Show("تم تقديم الإقرار بنجاح", "نجح", MessageBoxButtons.OK, MessageBoxIcon.Information);
                LoggingService.LogInfo($"VAT return submitted: {_currentVATReturn.PeriodNumber}");
            }
        }

        private void PrintButton_Click(object? sender, EventArgs e)
        {
            try
            {
                if (_currentVATReturn == null)
                {
                    MessageBox.Show("لا يوجد إقرار لطباعته. الرجاء إنشاء أو تحميل إقرار أولاً.", 
                        "تنبيه", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    return;
                }

                // إنشاء PDF
                var pdfPath = _printService.CreateVATReturnPDF(_currentVATReturn);

                // سؤال المستخدم
                var result = MessageBox.Show(
                    $"تم إنشاء ملف PDF بنجاح:\n{pdfPath}\n\nهل تريد فتح الملف؟",
                    "نجح",
                    MessageBoxButtons.YesNo,
                    MessageBoxIcon.Information
                );

                if (result == DialogResult.Yes)
                {
                    _printService.PrintPDF(pdfPath);
                }

                LoggingService.LogInfo($"VAT Return printed: {_currentVATReturn.PeriodNumber}");
            }
            catch (Exception ex)
            {
                LoggingService.LogError(ex, "Error printing VAT return");
                MessageBox.Show($"حدث خطأ أثناء الطباعة: {ex.Message}", 
                    "خطأ", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing) { }
            base.Dispose(disposing);
        }
    }
}