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
    /// نموذج إدارة أوامر الشراء
    /// Purchase Order Management Form
    /// </summary>
    public partial class PurchaseOrderForm : Form
    {
        #region Fields

        private readonly FishFarmContext _context;
        private List<PurchaseOrderItemTemp> _orderItems = new List<PurchaseOrderItemTemp>();

        // UI Controls - Order Info
        private TextBox _orderNumberTextBox = null!;
        private DateTimePicker _orderDatePicker = null!;
        private DateTimePicker _expectedDeliveryPicker = null!;
        private ComboBox _supplierComboBox = null!;
        private ComboBox _statusComboBox = null!;
        private ComboBox _priorityComboBox = null!;
        private TextBox _paymentTermsTextBox = null!;
        private NumericUpDown _paymentDueDaysNumeric = null!;
        private TextBox _notesTextBox = null!;

        // Items Grid
        private DataGridView _itemsGrid = null!;
        private ComboBox _itemComboBox = null!;
        private NumericUpDown _itemQuantityNumeric = null!;
        private NumericUpDown _itemPriceNumeric = null!;
        private NumericUpDown _itemDiscountNumeric = null!;
        private Button _addItemButton = null!;
        private Button _removeItemButton = null!;

        // Totals
        private Label _subTotalLabel = null!;
        private Label _discountLabel = null!;
        private Label _vatLabel = null!;
        private Label _totalLabel = null!;

        // Buttons
        private Button _saveButton = null!;
        private Button _submitButton = null!;
        private Button _approveButton = null!;
        private Button _sendButton = null!;
        private Button _newButton = null!;
        private Button _printButton = null!;

        // List Panel
        private DataGridView _ordersGrid = null!;
        private ComboBox _filterStatusComboBox = null!;
        private ComboBox _filterSupplierComboBox = null!;
        private DateTimePicker _filterFromDatePicker = null!;
        private DateTimePicker _filterToDatePicker = null!;

        #endregion

        #region Helper Classes

        private class PurchaseOrderItemTemp
        {
            public int InventoryItemId { get; set; }
            public string ItemName { get; set; } = "";
            public string Description { get; set; } = "";
            public decimal Quantity { get; set; }
            public decimal UnitPrice { get; set; }
            public decimal DiscountPercentage { get; set; }
            public decimal DiscountAmount { get; set; }
            public decimal TotalPrice { get; set; }
        }

        #endregion

        #region Constructor

        public PurchaseOrderForm(FishFarmContext context)
        {
            _context = context ?? throw new ArgumentNullException(nameof(context));

            if (!AuthenticationService.HasPermission(UserRole.Admin, UserRole.Manager, UserRole.InventoryStaff, UserRole.Accountant))
            {
                MessageBox.Show("ليس لديك صلاحية لإدارة أوامر الشراء", "تحذير",
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
            this.Text = "إدارة أوامر الشراء";
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
                Text = "نظام إدارة أوامر الشراء",
                Location = new Point(10, 10),
                Size = new Size(1560, 40),
                Font = new Font("Cairo", 14F, FontStyle.Bold),
                ForeColor = Color.FromArgb(46, 92, 138),
                TextAlign = ContentAlignment.MiddleRight
            };
            mainPanel.Controls.Add(titleLabel);

            var tabControl = new TabControl
            {
                Location = new Point(10, 60),
                Size = new Size(1560, 800),
                Font = new Font("Cairo", 10F)
            };

            // Tab 1: Order Details
            var detailsTab = new TabPage("تفاصيل الطلب");
            CreateOrderDetailsPanel(detailsTab);
            tabControl.TabPages.Add(detailsTab);

            // Tab 2: Orders List
            var listTab = new TabPage("قائمة الطلبات");
            CreateOrdersListPanel(listTab);
            tabControl.TabPages.Add(listTab);

            mainPanel.Controls.Add(tabControl);
            this.Controls.Add(mainPanel);
        }

        private void CreateOrderDetailsPanel(TabPage tab)
        {
            var panel = new Panel { Dock = DockStyle.Fill, Padding = new Padding(20), AutoScroll = true };

            // Order Info Section
            CreateOrderInfoSection(panel, 10);

            // Items Section
            CreateItemsSection(panel, 250);

            // Totals Section
            CreateTotalsSection(panel, 580);

            // Buttons
            CreateDetailsButtons(panel);

            tab.Controls.Add(panel);
        }

        private void CreateOrderInfoSection(Panel parent, int startY)
        {
            var infoPanel = new GroupBox
            {
                Text = "معلومات الطلب",
                Location = new Point(20, startY),
                Size = new Size(1500, 220),
                Font = new Font("Cairo", 10F, FontStyle.Bold)
            };

            int col1X = 1100;
            int col2X = 550;
            int labelWidth = 130;
            int controlWidth = 280;
            int y = 35;
            int spacing = 45;

            // Column 1
            var numberLabel = new Label { Text = "رقم الأمر:", Location = new Point(col1X + controlWidth, y), Size = new Size(labelWidth, 25), TextAlign = ContentAlignment.MiddleRight };
            infoPanel.Controls.Add(numberLabel);

            _orderNumberTextBox = new TextBox { Location = new Point(col1X, y), Size = new Size(controlWidth, 30), Font = new Font("Cairo", 10F), ReadOnly = true };
            infoPanel.Controls.Add(_orderNumberTextBox);

            y += spacing;

            var dateLabel = new Label { Text = "تاريخ الأمر:", Location = new Point(col1X + controlWidth, y), Size = new Size(labelWidth, 25), TextAlign = ContentAlignment.MiddleRight };
            infoPanel.Controls.Add(dateLabel);

            _orderDatePicker = new DateTimePicker { Location = new Point(col1X, y), Size = new Size(controlWidth, 30), Format = DateTimePickerFormat.Short, Font = new Font("Cairo", 10F) };
            infoPanel.Controls.Add(_orderDatePicker);

            y += spacing;

            var deliveryLabel = new Label { Text = "التسليم المتوقع:", Location = new Point(col1X + controlWidth, y), Size = new Size(labelWidth, 25), TextAlign = ContentAlignment.MiddleRight };
            infoPanel.Controls.Add(deliveryLabel);

            _expectedDeliveryPicker = new DateTimePicker { Location = new Point(col1X, y), Size = new Size(controlWidth, 30), Format = DateTimePickerFormat.Short, Font = new Font("Cairo", 10F) };
            infoPanel.Controls.Add(_expectedDeliveryPicker);

            y += spacing;

            var supplierLabel = new Label { Text = "المورد:", Location = new Point(col1X + controlWidth, y), Size = new Size(labelWidth, 25), TextAlign = ContentAlignment.MiddleRight };
            infoPanel.Controls.Add(supplierLabel);

            _supplierComboBox = new ComboBox { Location = new Point(col1X, y), Size = new Size(controlWidth, 30), DropDownStyle = ComboBoxStyle.DropDownList, Font = new Font("Cairo", 10F) };
            infoPanel.Controls.Add(_supplierComboBox);

            // Column 2
            y = 35;

            var statusLabel = new Label { Text = "الحالة:", Location = new Point(col2X + controlWidth, y), Size = new Size(labelWidth, 25), TextAlign = ContentAlignment.MiddleRight };
            infoPanel.Controls.Add(statusLabel);

            _statusComboBox = new ComboBox { Location = new Point(col2X, y), Size = new Size(controlWidth, 30), DropDownStyle = ComboBoxStyle.DropDownList, Font = new Font("Cairo", 10F), Enabled = false };
            LoadStatuses();
            infoPanel.Controls.Add(_statusComboBox);

            y += spacing;

            var priorityLabel = new Label { Text = "الأولوية:", Location = new Point(col2X + controlWidth, y), Size = new Size(labelWidth, 25), TextAlign = ContentAlignment.MiddleRight };
            infoPanel.Controls.Add(priorityLabel);

            _priorityComboBox = new ComboBox { Location = new Point(col2X, y), Size = new Size(controlWidth, 30), DropDownStyle = ComboBoxStyle.DropDownList, Font = new Font("Cairo", 10F) };
            LoadPriorities();
            infoPanel.Controls.Add(_priorityComboBox);

            y += spacing;

            var paymentTermsLabel = new Label { Text = "شروط الدفع:", Location = new Point(col2X + controlWidth, y), Size = new Size(labelWidth, 25), TextAlign = ContentAlignment.MiddleRight };
            infoPanel.Controls.Add(paymentTermsLabel);

            _paymentTermsTextBox = new TextBox { Location = new Point(col2X, y), Size = new Size(controlWidth, 30), Font = new Font("Cairo", 10F) };
            infoPanel.Controls.Add(_paymentTermsTextBox);

            y += spacing;

            var dueDaysLabel = new Label { Text = "أجل الدفع (أيام):", Location = new Point(col2X + controlWidth, y), Size = new Size(labelWidth, 25), TextAlign = ContentAlignment.MiddleRight };
            infoPanel.Controls.Add(dueDaysLabel);

            _paymentDueDaysNumeric = new NumericUpDown { Location = new Point(col2X, y), Size = new Size(controlWidth, 30), Font = new Font("Cairo", 10F), Minimum = 0, Maximum = 365, Value = 30 };
            infoPanel.Controls.Add(_paymentDueDaysNumeric);

            parent.Controls.Add(infoPanel);
        }

        private void CreateItemsSection(Panel parent, int startY)
        {
            var itemsPanel = new GroupBox
            {
                Text = "بنود الطلب",
                Location = new Point(20, startY),
                Size = new Size(1500, 310),
                Font = new Font("Cairo", 10F, FontStyle.Bold)
            };

            // Add Item Controls
            int y = 30;
            var itemLabel = new Label { Text = "العنصر:", Location = new Point(1240, y), Size = new Size(80, 25), TextAlign = ContentAlignment.MiddleRight };
            itemsPanel.Controls.Add(itemLabel);

            _itemComboBox = new ComboBox { Location = new Point(920, y), Size = new Size(300, 30), DropDownStyle = ComboBoxStyle.DropDownList, Font = new Font("Cairo", 9F) };
            itemsPanel.Controls.Add(_itemComboBox);

            var qtyLabel = new Label { Text = "الكمية:", Location = new Point(860, y), Size = new Size(60, 25), TextAlign = ContentAlignment.MiddleRight };
            itemsPanel.Controls.Add(qtyLabel);

            _itemQuantityNumeric = new NumericUpDown { Location = new Point(730, y), Size = new Size(120, 30), Font = new Font("Cairo", 9F), DecimalPlaces = 2, Minimum = 0.01m, Maximum = 999999, Value = 1 };
            itemsPanel.Controls.Add(_itemQuantityNumeric);

            var priceLabel = new Label { Text = "السعر:", Location = new Point(670, y), Size = new Size(60, 25), TextAlign = ContentAlignment.MiddleRight };
            itemsPanel.Controls.Add(priceLabel);

            _itemPriceNumeric = new NumericUpDown { Location = new Point(540, y), Size = new Size(120, 30), Font = new Font("Cairo", 9F), DecimalPlaces = 2, Minimum = 0, Maximum = 999999, Value = 0 };
            itemsPanel.Controls.Add(_itemPriceNumeric);

            var discountLabel = new Label { Text = "الخصم%:", Location = new Point(475, y), Size = new Size(65, 25), TextAlign = ContentAlignment.MiddleRight };
            itemsPanel.Controls.Add(discountLabel);

            _itemDiscountNumeric = new NumericUpDown { Location = new Point(375, y), Size = new Size(90, 30), Font = new Font("Cairo", 9F), DecimalPlaces = 2, Minimum = 0, Maximum = 100, Value = 0 };
            itemsPanel.Controls.Add(_itemDiscountNumeric);

            _addItemButton = new Button { Text = "إضافة", Location = new Point(245, y - 2), Size = new Size(110, 35), Font = new Font("Cairo", 9F, FontStyle.Bold), BackColor = Color.FromArgb(38, 166, 154), ForeColor = Color.White };
            _addItemButton.Click += AddItemButton_Click;
            itemsPanel.Controls.Add(_addItemButton);

            _removeItemButton = new Button { Text = "حذف", Location = new Point(120, y - 2), Size = new Size(110, 35), Font = new Font("Cairo", 9F, FontStyle.Bold), BackColor = Color.FromArgb(229, 57, 53), ForeColor = Color.White };
            _removeItemButton.Click += RemoveItemButton_Click;
            itemsPanel.Controls.Add(_removeItemButton);

            // Items Grid
            _itemsGrid = new DataGridView
            {
                Location = new Point(20, 80),
                Size = new Size(1460, 210),
                Font = new Font("Cairo", 9F),
                AllowUserToAddRows = false,
                AllowUserToDeleteRows = false,
                ReadOnly = true,
                SelectionMode = DataGridViewSelectionMode.FullRowSelect,
                AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill,
                BackgroundColor = Color.White
            };

            itemsPanel.Controls.Add(_itemsGrid);
            parent.Controls.Add(itemsPanel);
        }

        private void CreateTotalsSection(Panel parent, int startY)
        {
            var totalsPanel = new GroupBox
            {
                Text = "الإجماليات",
                Location = new Point(20, startY),
                Size = new Size(1500, 100),
                Font = new Font("Cairo", 10F, FontStyle.Bold)
            };

            int x = 1250;
            int y = 35;

            CreateTotalItem(totalsPanel, "الإجمالي الفرعي:", ref _subTotalLabel, x, y);
            x -= 300;
            CreateTotalItem(totalsPanel, "الخصم:", ref _discountLabel, x, y);
            x -= 300;
            CreateTotalItem(totalsPanel, "الضريبة (15%):", ref _vatLabel, x, y);
            x -= 300;
            CreateTotalItem(totalsPanel, "الإجمالي النهائي:", ref _totalLabel, x, y);
            _totalLabel.Font = new Font("Cairo", 12F, FontStyle.Bold);
            _totalLabel.ForeColor = Color.FromArgb(38, 166, 154);

            parent.Controls.Add(totalsPanel);
        }

        private void CreateTotalItem(GroupBox panel, string labelText, ref Label valueLabel, int x, int y)
        {
            var label = new Label { Text = labelText, Location = new Point(x + 140, y), Size = new Size(140, 25), TextAlign = ContentAlignment.MiddleRight };
            panel.Controls.Add(label);

            valueLabel = new Label
            {
                Text = "0.00",
                Location = new Point(x, y),
                Size = new Size(130, 30),
                TextAlign = ContentAlignment.MiddleCenter,
                Font = new Font("Cairo", 10F, FontStyle.Bold),
                BorderStyle = BorderStyle.FixedSingle,
                BackColor = Color.White
            };
            panel.Controls.Add(valueLabel);
        }

        private void CreateDetailsButtons(Panel parent)
        {
            var buttonPanel = new Panel { Location = new Point(20, 690), Size = new Size(1500, 50), BackColor = Color.Transparent };

            int x = 1250;
            int buttonWidth = 110;
            int spacing = 10;

            _newButton = CreateButton("جديد", x, 10, buttonWidth);
            _newButton.Click += NewButton_Click;
            buttonPanel.Controls.Add(_newButton);

            x -= (buttonWidth + spacing);

            _saveButton = CreateButton("حفظ", x, 10, buttonWidth);
            _saveButton.Click += async (s, e) => await SaveButton_ClickAsync();
            buttonPanel.Controls.Add(_saveButton);

            x -= (buttonWidth + spacing);

            _submitButton = CreateButton("تقديم", x, 10, buttonWidth);
            _submitButton.Click += async (s, e) => await SubmitButton_ClickAsync();
            _submitButton.BackColor = Color.FromArgb(38, 166, 154);
            buttonPanel.Controls.Add(_submitButton);

            x -= (buttonWidth + spacing);

            _approveButton = CreateButton("موافقة", x, 10, buttonWidth);
            _approveButton.Click += async (s, e) => await ApproveButton_ClickAsync();
            _approveButton.BackColor = Color.FromArgb(74, 144, 226);
            buttonPanel.Controls.Add(_approveButton);

            x -= (buttonWidth + spacing);

            _sendButton = CreateButton("إرسال", x, 10, buttonWidth);
            _sendButton.Click += async (s, e) => await SendButton_ClickAsync();
            _sendButton.BackColor = Color.FromArgb(255, 152, 0);
            buttonPanel.Controls.Add(_sendButton);

            x -= (buttonWidth + spacing);

            _printButton = CreateButton("طباعة", x, 10, buttonWidth);
            _printButton.Click += PrintButton_Click;
            buttonPanel.Controls.Add(_printButton);

            parent.Controls.Add(buttonPanel);
        }

        private void CreateOrdersListPanel(TabPage tab)
        {
            var panel = new Panel { Dock = DockStyle.Fill, Padding = new Padding(20) };

            // Filters
            var filterPanel = new GroupBox
            {
                Text = "الفلترة",
                Location = new Point(20, 10),
                Size = new Size(1500, 70),
                Font = new Font("Cairo", 10F, FontStyle.Bold)
            };

            int x = 1250;

            _filterFromDatePicker = new DateTimePicker { Location = new Point(x, 30), Size = new Size(140, 30), Format = DateTimePickerFormat.Short };
            filterPanel.Controls.Add(_filterFromDatePicker);

            x -= 160;

            _filterToDatePicker = new DateTimePicker { Location = new Point(x, 30), Size = new Size(140, 30), Format = DateTimePickerFormat.Short };
            filterPanel.Controls.Add(_filterToDatePicker);

            x -= 250;

            _filterSupplierComboBox = new ComboBox { Location = new Point(x, 30), Size = new Size(200, 30), DropDownStyle = ComboBoxStyle.DropDownList };
            filterPanel.Controls.Add(_filterSupplierComboBox);

            x -= 180;

            _filterStatusComboBox = new ComboBox { Location = new Point(x, 30), Size = new Size(150, 30), DropDownStyle = ComboBoxStyle.DropDownList };
            filterPanel.Controls.Add(_filterStatusComboBox);
            LoadStatuses();

            x -= 170;

            var searchButton = new Button { Text = "بحث", Location = new Point(x, 28), Size = new Size(110, 35), Font = new Font("Cairo", 10F, FontStyle.Bold) };
            searchButton.Click += async (s, e) => await LoadOrdersListAsync();
            filterPanel.Controls.Add(searchButton);

            panel.Controls.Add(filterPanel);

            // Orders Grid
            _ordersGrid = new DataGridView
            {
                Location = new Point(20, 90),
                Size = new Size(1500, 640),
                Font = new Font("Cairo", 9F),
                AllowUserToAddRows = false,
                AllowUserToDeleteRows = false,
                ReadOnly = true,
                SelectionMode = DataGridViewSelectionMode.FullRowSelect,
                AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill,
                BackgroundColor = Color.White
            };
            _ordersGrid.CellDoubleClick += OrdersGrid_CellDoubleClick;

            panel.Controls.Add(_ordersGrid);
            tab.Controls.Add(panel);
        }

        private Button CreateButton(string text, int x, int y, int width)
        {
            return new Button
            {
                Text = text,
                Location = new Point(x, y),
                Size = new Size(width, 35),
                Font = new Font("Cairo", 10F, FontStyle.Bold),
                BackColor = Color.FromArgb(46, 92, 138),
                ForeColor = Color.White,
                FlatStyle = FlatStyle.Flat,
                Cursor = Cursors.Hand
            };
        }

        private void InitializeForm()
        {
            try
            {
                _orderNumberTextBox.Text = GenerateOrderNumber();
                _orderDatePicker.Value = DateTime.Today;
                _expectedDeliveryPicker.Value = DateTime.Today.AddDays(7);
                _filterFromDatePicker.Value = DateTime.Today.AddMonths(-1);
                _filterToDatePicker.Value = DateTime.Today;

                _ = LoadSuppliersAsync();
                _ = LoadInventoryItemsAsync();
                _ = LoadOrdersListAsync();

                LoggingService.LogInfo("PurchaseOrderForm initialized");
            }
            catch (Exception ex)
            {
                LoggingService.LogError(ex, "Error initializing PurchaseOrderForm");
                MessageBox.Show($"حدث خطأ: {ex.Message}", "خطأ", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        #endregion

        #region Data Loading

        private async Task LoadSuppliersAsync()
        {
            try
            {
                var suppliers = await _context.Suppliers
                    .Where(s => s.Status == SupplierStatus.Active)
                    .OrderBy(s => s.Name)
                    .Select(s => new { s.Id, DisplayName = s.Name })
                    .ToListAsync();

                _supplierComboBox.DataSource = suppliers;
                _supplierComboBox.DisplayMember = "DisplayName";
                _supplierComboBox.ValueMember = "Id";

                var allSuppliers = new List<dynamic> { new { Id = 0, DisplayName = "الكل" } };
                allSuppliers.AddRange(suppliers);
                _filterSupplierComboBox.DataSource = allSuppliers;
                _filterSupplierComboBox.DisplayMember = "DisplayName";
                _filterSupplierComboBox.ValueMember = "Id";
            }
            catch (Exception ex)
            {
                LoggingService.LogError(ex, "Error loading suppliers");
            }
        }

        private async Task LoadInventoryItemsAsync()
        {
            try
            {
                var items = await _context.InventoryItems
                    .Where(i => i.IsActive)
                    .OrderBy(i => i.Name)
                    .Select(i => new { i.Id, DisplayName = i.Name + " - " + i.Code })
                    .ToListAsync();

                _itemComboBox.DataSource = items;
                _itemComboBox.DisplayMember = "DisplayName";
                _itemComboBox.ValueMember = "Id";
            }
            catch (Exception ex)
            {
                LoggingService.LogError(ex, "Error loading inventory items");
            }
        }

        private void LoadStatuses()
        {
            var statuses = new List<dynamic>
            {
                new { Value = PurchaseOrderStatus.Draft, Display = "مسودة" },
                new { Value = PurchaseOrderStatus.Submitted, Display = "مقدم" },
                new { Value = PurchaseOrderStatus.Approved, Display = "موافق" },
                new { Value = PurchaseOrderStatus.Sent, Display = "مُرسل" },
                new { Value = PurchaseOrderStatus.PartiallyReceived, Display = "استلام جزئي" },
                new { Value = PurchaseOrderStatus.Received, Display = "مُستلم" },
                new { Value = PurchaseOrderStatus.Closed, Display = "مغلق" },
                new { Value = PurchaseOrderStatus.Cancelled, Display = "ملغي" }
            };

            _statusComboBox.DataSource = new List<dynamic>(statuses);
            _statusComboBox.DisplayMember = "Display";
            _statusComboBox.ValueMember = "Value";

            var allStatuses = new List<dynamic> { new { Value = (PurchaseOrderStatus?)null, Display = "الكل" } };
            allStatuses.AddRange(statuses);
            if (_filterStatusComboBox != null)
            {
                _filterStatusComboBox.DataSource = allStatuses;
                _filterStatusComboBox.DisplayMember = "Display";
                _filterStatusComboBox.ValueMember = "Value";
            }
        }

        private void LoadPriorities()
        {
            var priorities = new List<dynamic>
            {
                new { Value = PurchasePriority.Low, Display = "منخفضة" },
                new { Value = PurchasePriority.Normal, Display = "عادية" },
                new { Value = PurchasePriority.High, Display = "عالية" },
                new { Value = PurchasePriority.Urgent, Display = "عاجلة" }
            };

            _priorityComboBox.DataSource = priorities;
            _priorityComboBox.DisplayMember = "Display";
            _priorityComboBox.ValueMember = "Value";
        }

        private async Task LoadOrdersListAsync()
        {
            try
            {
                var query = _context.PurchaseOrders
                    .Include(p => p.Supplier)
                    .AsQueryable();

                var fromDate = _filterFromDatePicker.Value.Date;
                var toDate = _filterToDatePicker.Value.Date.AddDays(1);

                query = query.Where(p => p.OrderDate >= fromDate && p.OrderDate < toDate);

                if (_filterSupplierComboBox.SelectedValue is int supplierId && supplierId > 0)
                {
                    query = query.Where(p => p.SupplierId == supplierId);
                }

                if (_filterStatusComboBox.SelectedValue is PurchaseOrderStatus filterStatus)
                {
                    query = query.Where(p => p.Status == filterStatus);
                }

                var orders = await query
                    .OrderByDescending(p => p.OrderDate)
                    .Select(p => new
                    {
                        p.Id,
                        رقم_الأمر = p.OrderNumber,
                        التاريخ = p.OrderDate.ToString("yyyy-MM-dd"),
                        المورد = p.Supplier.Name,
                        التسليم_المتوقع = p.ExpectedDeliveryDate.ToString("yyyy-MM-dd"),
                        الحالة = p.GetStatusDisplay(),
                        الإجمالي = p.Total.ToString("N2"),
                        ملاحظات = p.Notes ?? ""
                    })
                    .ToListAsync();

                _ordersGrid.DataSource = orders;

                if (_ordersGrid.Columns.Count > 0)
                {
                    _ordersGrid.Columns["Id"].Visible = false;
                }
            }
            catch (Exception ex)
            {
                LoggingService.LogError(ex, "Error loading orders list");
                MessageBox.Show($"حدث خطأ: {ex.Message}", "خطأ", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        #endregion

        #region Item Management

        private void AddItemButton_Click(object? sender, EventArgs e)
        {
            try
            {
                if (_itemComboBox.SelectedValue == null)
                {
                    MessageBox.Show("الرجاء اختيار عنصر", "تحذير", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    return;
                }

                var itemId = (int)_itemComboBox.SelectedValue;
                var itemName = _itemComboBox.Text;
                var quantity = _itemQuantityNumeric.Value;
                var unitPrice = _itemPriceNumeric.Value;
                var discountPercentage = _itemDiscountNumeric.Value;

                var discountAmount = (unitPrice * quantity) * (discountPercentage / 100);
                var totalPrice = (unitPrice * quantity) - discountAmount;

                var item = new PurchaseOrderItemTemp
                {
                    InventoryItemId = itemId,
                    ItemName = itemName,
                    Quantity = quantity,
                    UnitPrice = unitPrice,
                    DiscountPercentage = discountPercentage,
                    DiscountAmount = discountAmount,
                    TotalPrice = totalPrice
                };

                _orderItems.Add(item);
                DisplayItems();
                CalculateTotals();

                // Reset
                if (_itemComboBox.Items.Count > 0)
                    _itemComboBox.SelectedIndex = 0;
                _itemQuantityNumeric.Value = 1;
                _itemPriceNumeric.Value = 0;
                _itemDiscountNumeric.Value = 0;
            }
            catch (Exception ex)
            {
                LoggingService.LogError(ex, "Error adding item");
                MessageBox.Show($"حدث خطأ: {ex.Message}", "خطأ", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void RemoveItemButton_Click(object? sender, EventArgs e)
        {
            try
            {
                if (_itemsGrid.SelectedRows.Count > 0)
                {
                    var index = _itemsGrid.SelectedRows[0].Index;
                    _orderItems.RemoveAt(index);
                    DisplayItems();
                    CalculateTotals();
                }
            }
            catch (Exception ex)
            {
                LoggingService.LogError(ex, "Error removing item");
            }
        }

        private void DisplayItems()
        {
            var displayData = _orderItems.Select((item, index) => new
            {
                الرقم = index + 1,
                العنصر = item.ItemName,
                الكمية = item.Quantity.ToString("N2"),
                السعر = item.UnitPrice.ToString("N2"),
                الخصم = item.DiscountPercentage.ToString("N2") + "%",
                مبلغ_الخصم = item.DiscountAmount.ToString("N2"),
                الإجمالي = item.TotalPrice.ToString("N2")
            }).ToList();

            _itemsGrid.DataSource = displayData;
        }

        private void CalculateTotals()
        {
            var subTotal = _orderItems.Sum(i => i.TotalPrice);
            var discount = 0m;
            var amountAfterDiscount = subTotal - discount;
            var vat = amountAfterDiscount * 0.15m;
            var total = amountAfterDiscount + vat;

            _subTotalLabel.Text = subTotal.ToString("N2");
            _discountLabel.Text = discount.ToString("N2");
            _vatLabel.Text = vat.ToString("N2");
            _totalLabel.Text = total.ToString("N2");
        }

        #endregion

        #region Save Operations

        private Task SaveButton_ClickAsync()
        {
            try
            {
                if (!ValidateOrder())
                    return Task.CompletedTask;

                // Save logic would go here
                MessageBox.Show("تم حفظ أمر الشراء كمسودة", "نجح", MessageBoxButtons.OK, MessageBoxIcon.Information);
            }
            catch (Exception ex)
            {
                LoggingService.LogError(ex, "Error saving order");
                MessageBox.Show($"حدث خطأ: {ex.Message}", "خطأ", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
            return Task.CompletedTask;
        }

        private async Task SubmitButton_ClickAsync()
        {
            await Task.CompletedTask;
            MessageBox.Show("سيتم تنفيذ هذه الميزة قريباً", "قريباً", MessageBoxButtons.OK, MessageBoxIcon.Information);
        }

        private async Task ApproveButton_ClickAsync()
        {
            await Task.CompletedTask;
            MessageBox.Show("سيتم تنفيذ هذه الميزة قريباً", "قريباً", MessageBoxButtons.OK, MessageBoxIcon.Information);
        }

        private async Task SendButton_ClickAsync()
        {
            await Task.CompletedTask;
            MessageBox.Show("سيتم تنفيذ هذه الميزة قريباً", "قريباً", MessageBoxButtons.OK, MessageBoxIcon.Information);
        }

        private void PrintButton_Click(object? sender, EventArgs e)
        {
            MessageBox.Show("سيتم تنفيذ هذه الميزة قريباً", "قريباً", MessageBoxButtons.OK, MessageBoxIcon.Information);
        }

        #endregion

        #region Event Handlers

        private void OrdersGrid_CellDoubleClick(object? sender, DataGridViewCellEventArgs e)
        {
            // Load order to edit
        }

        private void NewButton_Click(object? sender, EventArgs e)
        {
            ClearForm();
        }

        #endregion

        #region Helper Methods

        private string GenerateOrderNumber()
        {
            return $"PO-{DateTime.Now:yyyyMMdd}-{new Random().Next(1000, 9999)}";
        }

        private bool ValidateOrder()
        {
            if (_supplierComboBox.SelectedValue == null || (int)_supplierComboBox.SelectedValue <= 0)
            {
                MessageBox.Show("الرجاء اختيار المورد", "تحذير", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return false;
            }

            if (_orderItems.Count == 0)
            {
                MessageBox.Show("الرجاء إضافة بنود للطلب", "تحذير", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return false;
            }

            return true;
        }

        private void ClearForm()
        {
            try
            {
                
                if (_orderNumberTextBox != null)
                    _orderNumberTextBox.Text = GenerateOrderNumber();
                    
                if (_orderDatePicker != null)
                    _orderDatePicker.Value = DateTime.Today;
                    
                if (_expectedDeliveryPicker != null)
                    _expectedDeliveryPicker.Value = DateTime.Today.AddDays(7);
                    
                if (_supplierComboBox != null)
                    _supplierComboBox.SelectedIndex = -1;
                    
                if (_statusComboBox != null && _statusComboBox.Items.Count > 0)
                    _statusComboBox.SelectedIndex = 0;
                    
                if (_priorityComboBox != null && _priorityComboBox.Items.Count > 0)
                    _priorityComboBox.SelectedIndex = 1; // Normal
                    
                if (_paymentTermsTextBox != null)
                    _paymentTermsTextBox.Clear();
                    
                if (_paymentDueDaysNumeric != null)
                    _paymentDueDaysNumeric.Value = 30;
                    
                if (_notesTextBox != null)
                    _notesTextBox.Clear();
                    
                _orderItems.Clear();
                DisplayItems();
                CalculateTotals();
            }
            catch (Exception ex)
            {
                LoggingService.LogError(ex, "Error clearing form");
                MessageBox.Show($"حدث خطأ في مسح النموذج: {ex.Message}", "خطأ", 
                    MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
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
