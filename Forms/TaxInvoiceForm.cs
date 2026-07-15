using System;
using System.Drawing;
using System.Linq;
using System.Threading.Tasks;
using System.Windows.Forms;
using Microsoft.EntityFrameworkCore;
using QRCoder;
using FishFarmManager.Data;
using FishFarmManager.Models;
using FishFarmManager.Services;

namespace FishFarmManager.Forms
{
    /// <summary>
    /// نموذج الفواتير الضريبية
    /// Tax Invoice Form
    /// Tax invoice entry. Phase-2 ZATCA compliance remains controlled by gate G4.
    /// </summary>
    public partial class TaxInvoiceForm : Form
    {
        private readonly FishFarmContext _context;
        private readonly PrintService _printService;

        private TextBox _invoiceNumberTextBox = null!;
        private DateTimePicker _issueDatePicker = null!;
        private ComboBox _customerComboBox = null!;
        private ComboBox _invoiceTypeComboBox = null!;
        private ComboBox _salesOrderComboBox = null!;
        private Button _loadFromSalesOrderButton = null!;
        private DataGridView _itemsGrid = null!;
        private Label _subTotalLabel = null!;
        private Label _vatLabel = null!;
        private Label _totalLabel = null!;
        private PictureBox _qrCodePictureBox = null!;
        private Button _generateQRButton = null!;
        private Button _saveButton = null!;
        private Button _printButton = null!;
        
        private int? _selectedSalesOrderId = null;

        public TaxInvoiceForm(FishFarmContext context)
        {
            _context = context ?? throw new ArgumentNullException(nameof(context));
            _printService = new PrintService();

            if (!AuthenticationService.HasPermission(UserRole.Admin, UserRole.Manager, UserRole.Accountant, UserRole.SalesStaff))
            {
                MessageBox.Show("ليس لديك صلاحية لإصدار الفواتير الضريبية", "تحذير",
                    MessageBoxButtons.OK, MessageBoxIcon.Warning);
                this.Load += (s, e) => this.Close();
                return;
            }

            InitializeComponent();
            InitializeForm();
        }

        private void InitializeComponent()
        {
            this.Text = "الفاتورة الضريبية - Tax Invoice";
            this.Size = new Size(1400, 900);
            this.StartPosition = FormStartPosition.CenterScreen;
            this.RightToLeft = RightToLeft.Yes;
            this.RightToLeftLayout = true;
            this.Font = new Font("Cairo", 10F);

            var mainPanel = new Panel { Dock = DockStyle.Fill, Padding = new Padding(20), BackColor = Color.FromArgb(240, 240, 240) };

            var titleLabel = new Label
            {
                Text = "إصدار فاتورة ضريبية",
                Location = new Point(20, 10),
                Size = new Size(1340, 40),
                Font = new Font("Cairo", 14F, FontStyle.Bold),
                ForeColor = Color.FromArgb(46, 92, 138),
                TextAlign = ContentAlignment.MiddleRight
            };
            mainPanel.Controls.Add(titleLabel);

            CreateHeaderSection(mainPanel, 60);
            CreateItemsSection(mainPanel, 260);
            CreateTotalsSection(mainPanel, 580);
            CreateQRSection(mainPanel, 660);
            CreateButtonsSection(mainPanel, 840);

            this.Controls.Add(mainPanel);
        }

        private void CreateHeaderSection(Panel parent, int startY)
        {
            var headerPanel = new GroupBox { Text = "بيانات الفاتورة", Location = new Point(20, startY), Size = new Size(1340, 180), Font = new Font("Cairo", 10F, FontStyle.Bold) };

            int x = 1050;
            int y = 35;

            // Row 1: Invoice Number and Date
            AddLabelAndControl(headerPanel, "رقم الفاتورة:", ref _invoiceNumberTextBox, x, y, 140, 280, true);
            x -= 450;

            AddLabelAndDatePicker(headerPanel, "التاريخ:", ref _issueDatePicker, x, y, 80, 200);

            // Row 2: Sales Order Selection
            y += 45;
            x = 1050;

            AddLabelAndCombo(headerPanel, "فاتورة المبيعات:", ref _salesOrderComboBox, x, y, 140, 280);
            x -= 380;

            _loadFromSalesOrderButton = new Button
            {
                Text = "تحميل من فاتورة المبيعات",
                Location = new Point(x, y),
                Size = new Size(200, 30),
                Font = new Font("Cairo", 9F, FontStyle.Bold),
                BackColor = Color.FromArgb(0, 121, 140),
                ForeColor = Color.White,
                FlatStyle = FlatStyle.Flat,
                Cursor = Cursors.Hand
            };
            _loadFromSalesOrderButton.Click += LoadFromSalesOrderButton_Click;
            headerPanel.Controls.Add(_loadFromSalesOrderButton);

            // Row 3: Customer and Invoice Type
            y += 45;
            x = 1050;

            AddLabelAndCombo(headerPanel, "العميل:", ref _customerComboBox, x, y, 140, 280);
            x -= 450;

            AddLabelAndCombo(headerPanel, "نوع الفاتورة:", ref _invoiceTypeComboBox, x, y, 120, 200);
            LoadInvoiceTypes();

            parent.Controls.Add(headerPanel);
        }

        private void CreateItemsSection(Panel parent, int startY)
        {
            var itemsPanel = new GroupBox { Text = "البنود", Location = new Point(20, startY), Size = new Size(1340, 300), Font = new Font("Cairo", 10F, FontStyle.Bold) };

            _itemsGrid = new DataGridView
            {
                Location = new Point(20, 35),
                Size = new Size(1300, 250),
                Font = new Font("Cairo", 9F),
                BackgroundColor = Color.White,
                AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill,
                AutoGenerateColumns = false,
                AllowUserToAddRows = false,
                AllowUserToDeleteRows = false,
                ReadOnly = true,
                SelectionMode = DataGridViewSelectionMode.FullRowSelect
            };

            // Define columns
            _itemsGrid.Columns.Add(new DataGridViewTextBoxColumn 
            { 
                Name = "ItemName", 
                HeaderText = "اسم المنتج", 
                DataPropertyName = "ItemName",
                Width = 200 
            });
            _itemsGrid.Columns.Add(new DataGridViewTextBoxColumn 
            { 
                Name = "Description", 
                HeaderText = "الوصف", 
                DataPropertyName = "Description",
                Width = 200 
            });
            _itemsGrid.Columns.Add(new DataGridViewTextBoxColumn 
            { 
                Name = "Quantity", 
                HeaderText = "الكمية", 
                DataPropertyName = "Quantity",
                Width = 100,
                DefaultCellStyle = new DataGridViewCellStyle { Format = "N2" }
            });
            _itemsGrid.Columns.Add(new DataGridViewTextBoxColumn 
            { 
                Name = "UnitPrice", 
                HeaderText = "السعر", 
                DataPropertyName = "UnitPrice",
                Width = 120,
                DefaultCellStyle = new DataGridViewCellStyle { Format = "N2" }
            });
            _itemsGrid.Columns.Add(new DataGridViewTextBoxColumn 
            { 
                Name = "DiscountAmount", 
                HeaderText = "الخصم", 
                DataPropertyName = "DiscountAmount",
                Width = 100,
                DefaultCellStyle = new DataGridViewCellStyle { Format = "N2" }
            });
            _itemsGrid.Columns.Add(new DataGridViewTextBoxColumn 
            { 
                Name = "VATAmount", 
                HeaderText = "الضريبة", 
                DataPropertyName = "VATAmount",
                Width = 120,
                DefaultCellStyle = new DataGridViewCellStyle { Format = "N2" }
            });
            _itemsGrid.Columns.Add(new DataGridViewTextBoxColumn 
            { 
                Name = "TotalAmount", 
                HeaderText = "الإجمالي", 
                DataPropertyName = "TotalAmount",
                Width = 150,
                DefaultCellStyle = new DataGridViewCellStyle { Format = "N2" }
            });

            itemsPanel.Controls.Add(_itemsGrid);
            parent.Controls.Add(itemsPanel);
        }

        private void CreateTotalsSection(Panel parent, int startY)
        {
            var totalsPanel = new GroupBox { Text = "الإجماليات", Location = new Point(20, startY), Size = new Size(900, 70), Font = new Font("Cairo", 10F, FontStyle.Bold) };

            int x = 650;
            CreateTotalLabel(totalsPanel, "المجموع:", ref _subTotalLabel, x, 30);
            _subTotalLabel.Text = "0.00 ريال";
            
            x -= 250;
            CreateTotalLabel(totalsPanel, "الضريبة (15%):", ref _vatLabel, x, 30);
            _vatLabel.Text = "0.00 ريال";
            
            x -= 250;
            CreateTotalLabel(totalsPanel, "الإجمالي:", ref _totalLabel, x, 30);
            _totalLabel.Font = new Font("Cairo", 12F, FontStyle.Bold);
            _totalLabel.ForeColor = Color.FromArgb(38, 166, 154);
            _totalLabel.Text = "0.00 ريال";

            parent.Controls.Add(totalsPanel);
        }

        private void CreateQRSection(Panel parent, int startY)
        {
            var qrPanel = new GroupBox { Text = "رمز QR - ZATCA", Location = new Point(930, startY), Size = new Size(430, 160), Font = new Font("Cairo", 10F, FontStyle.Bold) };

            _qrCodePictureBox = new PictureBox
            {
                Location = new Point(150, 30),
                Size = new Size(120, 120),
                BorderStyle = BorderStyle.FixedSingle,
                BackColor = Color.White,
                SizeMode = PictureBoxSizeMode.CenterImage
            };
            qrPanel.Controls.Add(_qrCodePictureBox);

            _generateQRButton = new Button
            {
                Text = "إنشاء QR",
                Location = new Point(20, 60),
                Size = new Size(110, 35),
                Font = new Font("Cairo", 9F, FontStyle.Bold),
                BackColor = Color.FromArgb(46, 92, 138),
                ForeColor = Color.White
            };
            _generateQRButton.Click += GenerateQRButton_Click;
            qrPanel.Controls.Add(_generateQRButton);

            parent.Controls.Add(qrPanel);
        }

        private void CreateButtonsSection(Panel parent, int startY)
        {
            var buttonPanel = new Panel { Location = new Point(20, startY), Size = new Size(1340, 50), BackColor = Color.Transparent };

            int x = 1050;

            _saveButton = CreateButton("حفظ", x, 10, 120);
            _saveButton.Click += async (s, e) => await SaveButton_ClickAsync();
            _saveButton.BackColor = Color.FromArgb(38, 166, 154);
            buttonPanel.Controls.Add(_saveButton);

            x -= 140;

            _printButton = CreateButton("طباعة", x, 10, 120);
            _printButton.Click += PrintButton_Click;
            buttonPanel.Controls.Add(_printButton);

            parent.Controls.Add(buttonPanel);
        }

        private Button CreateButton(string text, int x, int y, int width)
        {
            return new Button { Text = text, Location = new Point(x, y), Size = new Size(width, 35), Font = new Font("Cairo", 10F, FontStyle.Bold), BackColor = Color.FromArgb(46, 92, 138), ForeColor = Color.White, FlatStyle = FlatStyle.Flat, Cursor = Cursors.Hand };
        }

        private void AddLabelAndControl(GroupBox panel, string labelText, ref TextBox control, int x, int y, int labelWidth, int controlWidth, bool readOnly = false)
        {
            panel.Controls.Add(new Label { Text = labelText, Location = new Point(x + controlWidth, y), Size = new Size(labelWidth, 25), TextAlign = ContentAlignment.MiddleRight });
            control = new TextBox { Location = new Point(x, y), Size = new Size(controlWidth, 30), ReadOnly = readOnly };
            panel.Controls.Add(control);
        }

        private void AddLabelAndCombo(GroupBox panel, string labelText, ref ComboBox control, int x, int y, int labelWidth, int controlWidth)
        {
            panel.Controls.Add(new Label { Text = labelText, Location = new Point(x + controlWidth, y), Size = new Size(labelWidth, 25), TextAlign = ContentAlignment.MiddleRight });
            control = new ComboBox { Location = new Point(x, y), Size = new Size(controlWidth, 30), DropDownStyle = ComboBoxStyle.DropDownList };
            panel.Controls.Add(control);
        }

        private void AddLabelAndDatePicker(GroupBox panel, string labelText, ref DateTimePicker control, int x, int y, int labelWidth, int controlWidth)
        {
            panel.Controls.Add(new Label { Text = labelText, Location = new Point(x + controlWidth, y), Size = new Size(labelWidth, 25), TextAlign = ContentAlignment.MiddleRight });
            control = new DateTimePicker { Location = new Point(x, y), Size = new Size(controlWidth, 30), Format = DateTimePickerFormat.Short };
            panel.Controls.Add(control);
        }

        private void CreateTotalLabel(GroupBox panel, string labelText, ref Label valueLabel, int x, int y)
        {
            panel.Controls.Add(new Label { Text = labelText, Location = new Point(x + 140, y), Size = new Size(120, 25), TextAlign = ContentAlignment.MiddleRight });
            valueLabel = new Label { Text = "0.00", Location = new Point(x, y), Size = new Size(130, 30), TextAlign = ContentAlignment.MiddleCenter, Font = new Font("Cairo", 11F, FontStyle.Bold), BorderStyle = BorderStyle.FixedSingle, BackColor = Color.White };
            panel.Controls.Add(valueLabel);
        }

        private void LoadInvoiceTypes()
        {
            var types = new System.Collections.Generic.List<dynamic>
            {
                new { Value = InvoiceType.Standard, Display = "قياسية" },
                new { Value = InvoiceType.Simplified, Display = "مبسطة" }
            };

            _invoiceTypeComboBox.DataSource = types;
            _invoiceTypeComboBox.DisplayMember = "Display";
            _invoiceTypeComboBox.ValueMember = "Value";
        }

        private void InitializeForm()
        {
            _invoiceNumberTextBox.Text = GenerateInvoiceNumber();
            _issueDatePicker.Value = DateTime.Now;

            _ = LoadCustomersAsync();
            _ = LoadSalesOrdersAsync();

            LoggingService.LogInfo("TaxInvoiceForm initialized");
        }

        private async Task LoadCustomersAsync()
        {
            try
            {
                var customers = await _context.Customers
                    .OrderBy(c => c.Name)
                    .Select(c => new { c.Id, c.Name })
                    .ToListAsync();

                _customerComboBox.DataSource = customers;
                _customerComboBox.DisplayMember = "Name";
                _customerComboBox.ValueMember = "Id";
            }
            catch (Exception ex)
            {
                LoggingService.LogError("Error loading customers", ex);
            }
        }

        private async Task LoadSalesOrdersAsync()
        {
            try
            {
                // تحميل فواتير المبيعات التي لم يتم تحويلها لفواتير ضريبية بعد
                var salesOrders = await _context.SalesOrders
                    .Include(so => so.Customer)
                    .Where(so => !_context.TaxInvoices.Any(ti => ti.SalesOrderId == so.Id))
                    .Where(so => so.Status == SalesOrderStatus.Completed)
                    .OrderByDescending(so => so.OrderDate)
                    .Select(so => new
                    {
                        so.Id,
                        DisplayText = $"{so.OrderNumber} - {so.Customer.Name} - {so.OrderDate:yyyy/MM/dd} - {so.TotalAmount:N2} ريال",
                        so.OrderNumber,
                        so.CustomerId,
                        CustomerName = so.Customer.Name,
                        so.OrderDate,
                        so.SubTotal,
                        so.VATAmount,
                        so.TotalAmount
                    })
                    .ToListAsync();

                var emptyItem = new
                {
                    Id = 0,
                    DisplayText = "-- اختر فاتورة مبيعات --",
                    OrderNumber = "",
                    CustomerId = 0,
                    CustomerName = "",
                    OrderDate = DateTime.Now,
                    SubTotal = 0m,
                    VATAmount = 0m,
                    TotalAmount = 0m
                };

                var salesOrdersList = new[] { emptyItem }.Concat(salesOrders).ToList();

                _salesOrderComboBox.DataSource = salesOrdersList;
                _salesOrderComboBox.DisplayMember = "DisplayText";
                _salesOrderComboBox.ValueMember = "Id";

                LoggingService.LogInfo($"Loaded {salesOrders.Count} sales orders for tax invoice");
            }
            catch (Exception ex)
            {
                LoggingService.LogError("Error loading sales orders", ex);
                MessageBox.Show($"خطأ في تحميل فواتير المبيعات: {ex.Message}", "خطأ", 
                    MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private async void LoadFromSalesOrderButton_Click(object? sender, EventArgs e)
        {
            try
            {
                if (_salesOrderComboBox.SelectedValue == null || (int)_salesOrderComboBox.SelectedValue == 0)
                {
                    MessageBox.Show("الرجاء اختيار فاتورة مبيعات أولاً", "تنبيه", 
                        MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    return;
                }

                var salesOrderId = (int)_salesOrderComboBox.SelectedValue;
                _selectedSalesOrderId = salesOrderId;

                // تحميل فاتورة المبيعات مع البنود
                var salesOrder = await _context.SalesOrders
                    .Include(so => so.Customer)
                    .Include(so => so.Items)
                    .FirstOrDefaultAsync(so => so.Id == salesOrderId);

                if (salesOrder == null)
                {
                    MessageBox.Show("لم يتم العثور على فاتورة المبيعات", "خطأ", 
                        MessageBoxButtons.OK, MessageBoxIcon.Error);
                    return;
                }

                // ملء بيانات العميل
                _customerComboBox.SelectedValue = salesOrder.CustomerId;
                _customerComboBox.Enabled = false; // تعطيل التعديل

                // تحويل بنود المبيعات إلى بنود فاتورة ضريبية
                var taxInvoiceItems = salesOrder.Items.Select(item => new
                {
                    ItemName = item.ProductName ?? "منتج",
                    Description = item.Notes ?? $"درجة {item.Grade}",
                    Quantity = item.Quantity,
                    UnitPrice = item.UnitPrice,
                    DiscountAmount = item.DiscountAmount,
                    VATRate = 15m,
                    VATAmount = ((item.Quantity * item.UnitPrice) - item.DiscountAmount) * 0.15m,
                    TotalAmount = ((item.Quantity * item.UnitPrice) - item.DiscountAmount) * 1.15m
                }).ToList();

                // عرض البنود في الجدول
                _itemsGrid.DataSource = taxInvoiceItems;

                // تحديث الإجماليات
                UpdateTotals(salesOrder.SubTotal, salesOrder.VATAmount, salesOrder.TotalAmount);

                // تسجيل العملية
                LoggingService.LogInfo($"Loaded sales order {salesOrder.OrderNumber} into tax invoice");
                
                MessageBox.Show($"تم تحميل بيانات فاتورة المبيعات:\n{salesOrder.OrderNumber}\nالعميل: {salesOrder.Customer.Name}", 
                    "نجح", MessageBoxButtons.OK, MessageBoxIcon.Information);
            }
            catch (Exception ex)
            {
                LoggingService.LogError("Error loading sales order data", ex);
                MessageBox.Show($"حدث خطأ: {ex.Message}", "خطأ", 
                    MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void UpdateTotals(decimal subTotal, decimal vatAmount, decimal total)
        {
            _subTotalLabel.Text = $"{subTotal:N2} ريال";
            _vatLabel.Text = $"{vatAmount:N2} ريال";
            _totalLabel.Text = $"{total:N2} ريال";
        }

        private void GenerateQRButton_Click(object? sender, EventArgs e)
        {
            try
            {
                var content = $"Test|123456789|{DateTime.Now:yyyy-MM-dd}|1000|150";
                var qrContent = Convert.ToBase64String(System.Text.Encoding.UTF8.GetBytes(content));

                using var qrGenerator = new QRCodeGenerator();
                using var qrCodeData = qrGenerator.CreateQrCode(qrContent, QRCodeGenerator.ECCLevel.Q);
                using var qrCode = new QRCode(qrCodeData);
                var qrCodeImage = qrCode.GetGraphic(20);

                _qrCodePictureBox.Image = qrCodeImage;

                LoggingService.LogInfo("QR Code generated successfully");
                MessageBox.Show("تم إنشاء رمز QR بنجاح", "نجح", MessageBoxButtons.OK, MessageBoxIcon.Information);
            }
            catch (Exception ex)
            {
                LoggingService.LogError("Error generating QR code", ex);
                MessageBox.Show($"حدث خطأ: {ex.Message}", "خطأ", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private async Task SaveButton_ClickAsync()
        {
            try
            {
                // التحقق من البيانات الأساسية
                if (_customerComboBox.SelectedValue == null || (int)_customerComboBox.SelectedValue == 0)
                {
                    MessageBox.Show("الرجاء اختيار العميل", "تنبيه", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    return;
                }

                if (_itemsGrid.Rows.Count == 0)
                {
                    MessageBox.Show("الرجاء إضافة بنود للفاتورة", "تنبيه", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    return;
                }

                // تأكيد الحفظ
                var confirmResult = MessageBox.Show(
                    "هل أنت متأكد من حفظ الفاتورة الضريبية؟",
                    "تأكيد الحفظ",
                    MessageBoxButtons.YesNo,
                    MessageBoxIcon.Question
                );

                if (confirmResult != DialogResult.Yes)
                    return;

                // إنشاء الفاتورة الضريبية
                var taxInvoice = new TaxInvoice
                {
                    InvoiceNumber = _invoiceNumberTextBox.Text,
                    IssueDate = _issueDatePicker.Value,
                    SupplyDate = _issueDatePicker.Value,
                    InvoiceType = (InvoiceType)_invoiceTypeComboBox.SelectedValue,
                    
                    // بيانات البائع (من الإعدادات - يمكن تخصيصها لاحقًا)
                    SellerVATNumber = "310123456700003", // يجب استبداله بالرقم الفعلي من الإعدادات
                    SellerName = "AquaFarm Pro", // يجب استبداله بالاسم الفعلي من الإعدادات
                    SellerAddress = "المملكة العربية السعودية", // يجب استبداله بالعنوان الفعلي
                    
                    // بيانات المشتري
                    CustomerId = (int)_customerComboBox.SelectedValue,
                    BuyerName = _customerComboBox.Text,
                    
                    // ربط بفاتورة المبيعات إذا كانت موجودة
                    SalesOrderId = _selectedSalesOrderId,
                    
                    // سيتم حساب المبالغ من البنود
                    CreatedBy = AuthenticationService.CurrentUsername,
                    CreatedAt = DateTime.Now
                };

                // إضافة البنود
                foreach (DataGridViewRow row in _itemsGrid.Rows)
                {
                    if (row.DataBoundItem == null) continue;

                    dynamic item = row.DataBoundItem;
                    
                    var taxInvoiceItem = new TaxInvoiceItem
                    {
                        ItemName = item.ItemName?.ToString() ?? "",
                        Description = item.Description?.ToString() ?? "",
                        Quantity = Convert.ToDecimal(item.Quantity),
                        UnitPrice = Convert.ToDecimal(item.UnitPrice),
                        DiscountAmount = Convert.ToDecimal(item.DiscountAmount),
                        VATRate = 15m
                    };
                    
                    taxInvoiceItem.CalculateTotals();
                    taxInvoice.Items.Add(taxInvoiceItem);
                }

                // حساب الإجماليات
                taxInvoice.CalculateTotals();
                
                // إنشاء QR Code
                taxInvoice.QRCodeContent = taxInvoice.GenerateQRCodeContent();

                // حفظ في قاعدة البيانات
                _context.TaxInvoices.Add(taxInvoice);
                await _context.SaveChangesAsync();

                LoggingService.LogInfo($"Tax invoice {taxInvoice.InvoiceNumber} saved successfully by {AuthenticationService.CurrentUsername}");
                
                MessageBox.Show(
                    $"تم حفظ الفاتورة الضريبية بنجاح\nرقم الفاتورة: {taxInvoice.InvoiceNumber}",
                    "نجح",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Information
                );

                // إعادة تحميل قائمة فواتير المبيعات
                await LoadSalesOrdersAsync();
                
                // تفريغ النموذج
                ClearForm();
            }
            catch (Exception ex)
            {
                LoggingService.LogError("Error saving tax invoice", ex);
                MessageBox.Show($"حدث خطأ عند الحفظ: {ex.Message}", "خطأ", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void ClearForm()
        {
            _invoiceNumberTextBox.Text = GenerateInvoiceNumber();
            _issueDatePicker.Value = DateTime.Now;
            _salesOrderComboBox.SelectedIndex = 0;
            _customerComboBox.SelectedIndex = -1;
            _customerComboBox.Enabled = true;
            _invoiceTypeComboBox.SelectedIndex = 0;
            _itemsGrid.DataSource = null;
            _subTotalLabel.Text = "0.00 ريال";
            _vatLabel.Text = "0.00 ريال";
            _totalLabel.Text = "0.00 ريال";
            _qrCodePictureBox.Image = null;
            _selectedSalesOrderId = null;
        }

        private async void PrintButton_Click(object? sender, EventArgs e)
        {
            try
            {
                // التحقق من وجود بيانات للطباعة
                if (_itemsGrid.Rows.Count == 0)
                {
                    MessageBox.Show("لا توجد بنود للطباعة. الرجاء تحميل فاتورة أو إضافة بنود أولاً.", 
                        "تنبيه", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    return;
                }

                if (_customerComboBox.SelectedValue == null)
                {
                    MessageBox.Show("الرجاء اختيار العميل أولاً.", 
                        "تنبيه", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    return;
                }

                // إنشاء فاتورة ضريبية مؤقتة للطباعة
                var tempInvoice = new TaxInvoice
                {
                    InvoiceNumber = _invoiceNumberTextBox.Text,
                    IssueDate = _issueDatePicker.Value,
                    SupplyDate = _issueDatePicker.Value,
                    InvoiceType = (InvoiceType)_invoiceTypeComboBox.SelectedValue,
                    
                    // بيانات البائع
                    SellerVATNumber = "310123456700003",
                    SellerName = "AquaFarm Pro",
                    SellerAddress = "المملكة العربية السعودية",
                    
                    // بيانات المشتري
                    CustomerId = (int)_customerComboBox.SelectedValue,
                    BuyerName = _customerComboBox.Text,
                    
                    // المبالغ من Labels
                    SubTotal = ParseAmount(_subTotalLabel.Text),
                    VATAmount = ParseAmount(_vatLabel.Text),
                    TotalWithVAT = ParseAmount(_totalLabel.Text),
                    VATRate = 15m,
                    
                    // QR Code إذا كان موجوداً
                    QRCodeImage = ImageToByteArray(_qrCodePictureBox.Image)
                };

                // إضافة البنود
                foreach (DataGridViewRow row in _itemsGrid.Rows)
                {
                    if (row.DataBoundItem == null) continue;

                    dynamic item = row.DataBoundItem;
                    
                    var invoiceItem = new TaxInvoiceItem
                    {
                        ItemName = item.ItemName?.ToString() ?? "",
                        Description = item.Description?.ToString() ?? "",
                        Quantity = Convert.ToDecimal(item.Quantity),
                        UnitPrice = Convert.ToDecimal(item.UnitPrice),
                        DiscountAmount = Convert.ToDecimal(item.DiscountAmount),
                        VATAmount = Convert.ToDecimal(item.VATAmount),
                        TotalAmount = Convert.ToDecimal(item.TotalAmount),
                        VATRate = 15m
                    };
                    
                    tempInvoice.Items.Add(invoiceItem);
                }

                // إنشاء PDF
                var pdfPath = _printService.CreateTaxInvoicePDF(tempInvoice, includeQR: true);

                // سؤال المستخدم
                var result = MessageBox.Show(
                    $"تم إنشاء ملف PDF بنجاح:\n{pdfPath}\n\nهل تريد فتح الملف للطباعة؟",
                    "نجح",
                    MessageBoxButtons.YesNo,
                    MessageBoxIcon.Information
                );

                if (result == DialogResult.Yes)
                {
                    _printService.PrintPDF(pdfPath);
                }

                LoggingService.LogInfo($"Tax invoice printed: {tempInvoice.InvoiceNumber}");
            }
            catch (Exception ex)
            {
                LoggingService.LogError(ex, "Error printing tax invoice");
                MessageBox.Show($"حدث خطأ أثناء الطباعة: {ex.Message}", 
                    "خطأ", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private decimal ParseAmount(string text)
        {
            // إزالة "ريال" والمسافات
            text = text.Replace("ريال", "").Trim();
            
            if (decimal.TryParse(text, out decimal amount))
                return amount;
            
            return 0m;
        }

        private byte[]? ImageToByteArray(Image? image)
        {
            if (image == null) return null;

            try
            {
                using var ms = new MemoryStream();
                image.Save(ms, System.Drawing.Imaging.ImageFormat.Png);
                return ms.ToArray();
            }
            catch
            {
                return null;
            }
        }

        private string GenerateInvoiceNumber()
        {
            return $"INV-{DateTime.Now:yyyyMMdd}-{new Random().Next(1000, 9999)}";
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                _qrCodePictureBox?.Image?.Dispose();
            }
            base.Dispose(disposing);
        }
    }
}
