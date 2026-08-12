using System;
using System.Linq;
using System.Windows.Forms;
using FishFarmManager.Data;
using FishFarmManager.Models;
using FishFarmManager.Controls;
using FishFarmManager.Services;
using Microsoft.EntityFrameworkCore;

namespace FishFarmManager.Forms
{
    /// <summary>
    /// نموذج إدارة أوامر المبيعات
    /// Form for managing sales orders
    /// </summary>
    public partial class SalesOrderForm : Form
    {
        private readonly FishFarmContext _context;
        private readonly AutocompleteService _autocompleteService;
        private int _selectedOrderId = 0;
        private int _selectedCustomerId = 0;
        private const decimal VAT_RATE = 0.15m; // 15% ضريبة القيمة المضافة

        // Controls - Header
        private TextBox _orderNumberTextBox = null!;
        private SmartAutoCompleteTextBox _customerSearchTextBox = null!;
        private DateTimePicker _orderDatePicker = null!;
        private DateTimePicker _expectedDeliveryDatePicker = null!;
        private DateTimePicker _deliveryDatePicker = null!;
        private ComboBox _statusComboBox = null!;
        private TextBox _deliveryAddressTextBox = null!;
        private TextBox _deliveryInstructionsTextBox = null!;
        private TextBox _notesTextBox = null!;

        // Controls - Items Grid
        private DataGridView _itemsGrid = null!;
        private Button _addItemButton = null!;
        private Button _removeItemButton = null!;

        // Controls - Item Entry
        private ComboBox _productionCycleComboBox = null!;
        private SmartAutoCompleteTextBox _productNameTextBox = null!;
        private ComboBox _gradeComboBox = null!;
        private NumericUpDown _quantityNumeric = null!;
        private NumericUpDown _unitPriceNumeric = null!;
        private NumericUpDown _discountNumeric = null!;

        // Controls - Totals
        private Label _subTotalLabel = null!;
        private Label _vatAmountLabel = null!;
        private Label _discountAmountLabel = null!;
        private Label _totalAmountLabel = null!;
        private Label _paidAmountLabel = null!;
        private Label _remainingAmountLabel = null!;

        // Controls - Buttons
        private Button _saveButton = null!;
        private Button _deleteButton = null!;
        private Button _newButton = null!;
        private Button _printInvoiceButton = null!;

        // Controls - Orders List
        private DataGridView _ordersGrid = null!;
        private TextBox _searchTextBox = null!;
        private DateTimePicker _filterFromDatePicker = null!;
        private DateTimePicker _filterToDatePicker = null!;
        private ComboBox _filterStatusComboBox = null!;

        public SalesOrderForm(FishFarmContext context)
        {
            _context = context;
            _autocompleteService = new AutocompleteService(_context);
            InitializeComponent();
            LoadProductionCycles();
            LoadOrders();
        }

        private void InitializeComponent()
        {
            this.Text = "إدارة أوامر المبيعات - Sales Order Management";
            this.Size = new System.Drawing.Size(1400, 900);
            this.RightToLeft = RightToLeft.Yes;
            this.RightToLeftLayout = true;
            this.StartPosition = FormStartPosition.CenterScreen;

            // Create main layout
            var mainPanel = new TableLayoutPanel
            {
                Dock = DockStyle.Fill,
                ColumnCount = 1,
                RowCount = 2,
                Padding = new Padding(10)
            };
            mainPanel.RowStyles.Add(new RowStyle(SizeType.Percent, 70F));
            mainPanel.RowStyles.Add(new RowStyle(SizeType.Percent, 30F));

            // Top panel - Order details and items
            var topPanel = CreateTopPanel();
            mainPanel.Controls.Add(topPanel, 0, 0);

            // Bottom panel - Orders list
            var bottomPanel = CreateBottomPanel();
            mainPanel.Controls.Add(bottomPanel, 0, 1);

            this.Controls.Add(mainPanel);
        }

        private Panel CreateTopPanel()
        {
            var panel = new Panel { Dock = DockStyle.Fill };
            var tabControl = new TabControl { Dock = DockStyle.Fill };

            // Tab 1: Order Header
            var headerTab = new TabPage("بيانات الطلب");
            headerTab.Controls.Add(CreateHeaderPanel());
            tabControl.TabPages.Add(headerTab);

            // Tab 2: Order Items
            var itemsTab = new TabPage("بنود الطلب");
            itemsTab.Controls.Add(CreateItemsPanel());
            tabControl.TabPages.Add(itemsTab);

            panel.Controls.Add(tabControl);
            return panel;
        }

        private Panel CreateHeaderPanel()
        {
            var panel = new Panel { Dock = DockStyle.Fill, AutoScroll = true };
            int yPos = 20;

            // Title
            var titleLabel = new Label
            {
                Text = "بيانات أمر المبيعات",
                Font = new System.Drawing.Font("Segoe UI", 12, System.Drawing.FontStyle.Bold),
                Location = new System.Drawing.Point(20, yPos),
                AutoSize = true
            };
            panel.Controls.Add(titleLabel);

            // Row 1
            yPos += 40;
            panel.Controls.Add(new Label { Text = "رقم الطلب:", Location = new System.Drawing.Point(1200, yPos), AutoSize = true });
            _orderNumberTextBox = new TextBox 
            { 
                Location = new System.Drawing.Point(1000, yPos), 
                Width = 180,
                ReadOnly = true,
                BackColor = System.Drawing.Color.LightGray
            };
            panel.Controls.Add(_orderNumberTextBox);

            panel.Controls.Add(new Label { Text = "العميل:", Location = new System.Drawing.Point(900, yPos), AutoSize = true });
            _customerSearchTextBox = new SmartAutoCompleteTextBox(_autocompleteService)
            {
                Location = new System.Drawing.Point(600, yPos),
                Width = 280,
                EntityType = "Customer",
                SearchField = "All",
                MinCharacters = 2,
                AutoFillDetails = true,
                RecordUsage = true
            };
            _customerSearchTextBox.ItemSelected += CustomerSearch_ItemSelected;
            panel.Controls.Add(_customerSearchTextBox);

            panel.Controls.Add(new Label { Text = "الحالة:", Location = new System.Drawing.Point(500, yPos), AutoSize = true });
            _statusComboBox = new ComboBox
            {
                Location = new System.Drawing.Point(320, yPos),
                Width = 160,
                DropDownStyle = ComboBoxStyle.DropDownList
            };
            _statusComboBox.Items.AddRange(new object[]
            {
                "معلق", "مؤكد", "قيد التنفيذ", "جاهز للتسليم", "تم التسليم", "ملغى", "مرتجع"
            });
            _statusComboBox.SelectedIndex = 0;
            panel.Controls.Add(_statusComboBox);

            // Row 2
            yPos += 40;
            panel.Controls.Add(new Label { Text = "تاريخ الطلب:", Location = new System.Drawing.Point(1200, yPos), AutoSize = true });
            _orderDatePicker = new DateTimePicker
            {
                Location = new System.Drawing.Point(1000, yPos),
                Width = 180,
                Format = DateTimePickerFormat.Short
            };
            panel.Controls.Add(_orderDatePicker);

            panel.Controls.Add(new Label { Text = "تاريخ التسليم المتوقع:", Location = new System.Drawing.Point(820, yPos), AutoSize = true });
            _expectedDeliveryDatePicker = new DateTimePicker
            {
                Location = new System.Drawing.Point(600, yPos),
                Width = 200,
                Format = DateTimePickerFormat.Short
            };
            panel.Controls.Add(_expectedDeliveryDatePicker);

            panel.Controls.Add(new Label { Text = "تاريخ التسليم الفعلي:", Location = new System.Drawing.Point(440, yPos), AutoSize = true });
            _deliveryDatePicker = new DateTimePicker
            {
                Location = new System.Drawing.Point(220, yPos),
                Width = 200,
                Format = DateTimePickerFormat.Short,
                ShowCheckBox = true,
                Checked = false
            };
            panel.Controls.Add(_deliveryDatePicker);

            // Row 3
            yPos += 40;
            panel.Controls.Add(new Label { Text = "عنوان التسليم:", Location = new System.Drawing.Point(1200, yPos), AutoSize = true });
            _deliveryAddressTextBox = new TextBox
            {
                Location = new System.Drawing.Point(600, yPos),
                Width = 580,
                Multiline = true,
                Height = 60
            };
            panel.Controls.Add(_deliveryAddressTextBox);

            // Row 4
            yPos += 70;
            panel.Controls.Add(new Label { Text = "تعليمات التسليم:", Location = new System.Drawing.Point(1200, yPos), AutoSize = true });
            _deliveryInstructionsTextBox = new TextBox
            {
                Location = new System.Drawing.Point(600, yPos),
                Width = 580,
                Multiline = true,
                Height = 60
            };
            panel.Controls.Add(_deliveryInstructionsTextBox);

            // Row 5
            yPos += 70;
            panel.Controls.Add(new Label { Text = "ملاحظات:", Location = new System.Drawing.Point(1200, yPos), AutoSize = true });
            _notesTextBox = new TextBox
            {
                Location = new System.Drawing.Point(600, yPos),
                Width = 580,
                Multiline = true,
                Height = 60
            };
            panel.Controls.Add(_notesTextBox);

            // Buttons
            yPos += 80;
            _saveButton = new Button
            {
                Text = "حفظ الطلب",
                Location = new System.Drawing.Point(1100, yPos),
                Width = 120,
                Height = 40,
                BackColor = ThemeManager.SecondarySkyBlue,
                ForeColor = System.Drawing.Color.White,
                FlatStyle = FlatStyle.Flat
            };
            _saveButton.Click += SaveButton_Click;
            panel.Controls.Add(_saveButton);

            _newButton = new Button
            {
                Text = "طلب جديد",
                Location = new System.Drawing.Point(970, yPos),
                Width = 120,
                Height = 40,
                BackColor = ThemeManager.SuccessGreen,
                ForeColor = System.Drawing.Color.White,
                FlatStyle = FlatStyle.Flat
            };
            _newButton.Click += (s, e) => ClearForm();
            panel.Controls.Add(_newButton);

            _deleteButton = new Button
            {
                Text = "حذف",
                Location = new System.Drawing.Point(840, yPos),
                Width = 120,
                Height = 40,
                BackColor = ThemeManager.ErrorRed,
                ForeColor = System.Drawing.Color.White,
                FlatStyle = FlatStyle.Flat
            };
            _deleteButton.Click += DeleteButton_Click;
            panel.Controls.Add(_deleteButton);

            _printInvoiceButton = new Button
            {
                Text = "طباعة الفاتورة",
                Location = new System.Drawing.Point(700, yPos),
                Width = 130,
                Height = 40,
                BackColor = ThemeManager.SecondarySkyBlue,
                ForeColor = System.Drawing.Color.White,
                FlatStyle = FlatStyle.Flat
            };
            _printInvoiceButton.Click += PrintInvoiceButton_Click;
            panel.Controls.Add(_printInvoiceButton);

            return panel;
        }

        private Panel CreateItemsPanel()
        {
            var panel = new Panel { Dock = DockStyle.Fill };

            // Item entry section
            var entryGroup = new GroupBox
            {
                Text = "إضافة بند جديد",
                Location = new System.Drawing.Point(20, 20),
                Width = 1320,
                Height = 120
            };

            int xPos = 20;
            entryGroup.Controls.Add(new Label { Text = "دورة الإنتاج:", Location = new System.Drawing.Point(xPos, 30), AutoSize = true });
            _productionCycleComboBox = new ComboBox
            {
                Location = new System.Drawing.Point(xPos, 55),
                Width = 180,
                DropDownStyle = ComboBoxStyle.DropDownList
            };
            _productionCycleComboBox.SelectedIndexChanged += ProductionCycleComboBox_SelectedIndexChanged;
            entryGroup.Controls.Add(_productionCycleComboBox);

            xPos += 200;
            entryGroup.Controls.Add(new Label { Text = "اسم المنتج:", Location = new System.Drawing.Point(xPos, 30), AutoSize = true });
            _productNameTextBox = new SmartAutoCompleteTextBox(_autocompleteService)
            {
                Location = new System.Drawing.Point(xPos, 55),
                Width = 200,
                EntityType = "InventoryItem",
                SearchField = "All",
                MinCharacters = 2,
                RecordUsage = true
            };
            _productNameTextBox.ItemSelected += ProductName_ItemSelected;
            entryGroup.Controls.Add(_productNameTextBox);

            xPos += 180;
            entryGroup.Controls.Add(new Label { Text = "الجودة:", Location = new System.Drawing.Point(xPos, 30), AutoSize = true });
            _gradeComboBox = new ComboBox
            {
                Location = new System.Drawing.Point(xPos, 55),
                Width = 120,
                DropDownStyle = ComboBoxStyle.DropDownList
            };
            _gradeComboBox.Items.AddRange(new object[] { "ممتاز", "درجة أولى", "درجة ثانية", "درجة ثالثة", "مرفوض" });
            _gradeComboBox.SelectedIndex = 0;
            entryGroup.Controls.Add(_gradeComboBox);

            xPos += 140;
            entryGroup.Controls.Add(new Label { Text = "الكمية (كجم):", Location = new System.Drawing.Point(xPos, 30), AutoSize = true });
            _quantityNumeric = new NumericUpDown
            {
                Location = new System.Drawing.Point(xPos, 55),
                Width = 100,
                DecimalPlaces = 2,
                Maximum = 999999,
                Minimum = 0.01m
            };
            _quantityNumeric.ValueChanged += (s, e) => CalculateTotals();
            entryGroup.Controls.Add(_quantityNumeric);

            xPos += 120;
            entryGroup.Controls.Add(new Label { Text = "سعر الكيلو:", Location = new System.Drawing.Point(xPos, 30), AutoSize = true });
            _unitPriceNumeric = new NumericUpDown
            {
                Location = new System.Drawing.Point(xPos, 55),
                Width = 100,
                DecimalPlaces = 2,
                Maximum = 999999,
                Minimum = 0.01m,
                Value = 15
            };
            _unitPriceNumeric.ValueChanged += (s, e) => CalculateTotals();
            entryGroup.Controls.Add(_unitPriceNumeric);

            xPos += 120;
            entryGroup.Controls.Add(new Label { Text = "الخصم:", Location = new System.Drawing.Point(xPos, 30), AutoSize = true });
            _discountNumeric = new NumericUpDown
            {
                Location = new System.Drawing.Point(xPos, 55),
                Width = 100,
                DecimalPlaces = 2,
                Maximum = 999999,
                Minimum = 0
            };
            _discountNumeric.ValueChanged += (s, e) => CalculateTotals();
            entryGroup.Controls.Add(_discountNumeric);

            xPos += 120;
            _addItemButton = new Button
            {
                Text = "إضافة",
                Location = new System.Drawing.Point(xPos, 50),
                Width = 100,
                Height = 35,
                BackColor = ThemeManager.SuccessGreen,
                ForeColor = System.Drawing.Color.White,
                FlatStyle = FlatStyle.Flat
            };
            _addItemButton.Click += AddItemButton_Click;
            entryGroup.Controls.Add(_addItemButton);

            xPos += 110;
            _removeItemButton = new Button
            {
                Text = "حذف البند",
                Location = new System.Drawing.Point(xPos, 50),
                Width = 100,
                Height = 35,
                BackColor = ThemeManager.ErrorRed,
                ForeColor = System.Drawing.Color.White,
                FlatStyle = FlatStyle.Flat
            };
            _removeItemButton.Click += RemoveItemButton_Click;
            entryGroup.Controls.Add(_removeItemButton);

            panel.Controls.Add(entryGroup);

            // Items grid
            _itemsGrid = new DataGridView
            {
                Location = new System.Drawing.Point(20, 150),
                Width = 1320,
                Height = 250,
                AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill,
                SelectionMode = DataGridViewSelectionMode.FullRowSelect,
                MultiSelect = false,
                ReadOnly = false,
                AllowUserToAddRows = false,
                AllowUserToDeleteRows = false // منع الحذف عبر Delete key - استخدم الزر
            };
            _itemsGrid.Columns.Add(new DataGridViewTextBoxColumn { Name = "ProductionCycleId", HeaderText = "رقم الدورة", Visible = false, ReadOnly = true });
            _itemsGrid.Columns.Add(new DataGridViewTextBoxColumn { Name = "ProductName", HeaderText = "المنتج", ReadOnly = false });
            _itemsGrid.Columns.Add(new DataGridViewComboBoxColumn 
            { 
                Name = "Grade", 
                HeaderText = "الجودة",
                Items = { "Premium", "GradeA", "GradeB", "GradeC", "Rejected" },
                ReadOnly = false
            });
            _itemsGrid.Columns.Add(new DataGridViewTextBoxColumn { Name = "Quantity", HeaderText = "الكمية (كجم)", ReadOnly = false });
            _itemsGrid.Columns.Add(new DataGridViewTextBoxColumn { Name = "UnitPrice", HeaderText = "سعر الكيلو", ReadOnly = false });
            _itemsGrid.Columns.Add(new DataGridViewTextBoxColumn { Name = "SubTotal", HeaderText = "المجموع", ReadOnly = true });
            _itemsGrid.Columns.Add(new DataGridViewTextBoxColumn { Name = "DiscountAmount", HeaderText = "الخصم", ReadOnly = false });
            _itemsGrid.Columns.Add(new DataGridViewTextBoxColumn { Name = "TotalPrice", HeaderText = "الإجمالي", ReadOnly = true });
            _itemsGrid.CellEndEdit += ItemsGrid_CellEndEdit;
            _itemsGrid.CellValidating += ItemsGrid_CellValidating;
            panel.Controls.Add(_itemsGrid);

            // Totals section
            var totalsGroup = new GroupBox
            {
                Text = "الإجماليات",
                Location = new System.Drawing.Point(20, 410),
                Width = 500,
                Height = 180
            };

            int yPos = 30;
            totalsGroup.Controls.Add(new Label { Text = "المجموع الفرعي:", Location = new System.Drawing.Point(350, yPos), AutoSize = true });
            _subTotalLabel = new Label
            {
                Text = "0.00 ريال",
                Location = new System.Drawing.Point(150, yPos),
                Width = 180,
                Font = new System.Drawing.Font("Segoe UI", 10, System.Drawing.FontStyle.Bold),
                TextAlign = System.Drawing.ContentAlignment.MiddleRight
            };
            totalsGroup.Controls.Add(_subTotalLabel);

            yPos += 30;
            totalsGroup.Controls.Add(new Label { Text = "الخصم:", Location = new System.Drawing.Point(350, yPos), AutoSize = true });
            _discountAmountLabel = new Label
            {
                Text = "0.00 ريال",
                Location = new System.Drawing.Point(150, yPos),
                Width = 180,
                Font = new System.Drawing.Font("Segoe UI", 10),
                TextAlign = System.Drawing.ContentAlignment.MiddleRight
            };
            totalsGroup.Controls.Add(_discountAmountLabel);

            yPos += 30;
            totalsGroup.Controls.Add(new Label { Text = "ضريبة القيمة المضافة (15%):", Location = new System.Drawing.Point(200, yPos), AutoSize = true });
            _vatAmountLabel = new Label
            {
                Text = "0.00 ريال",
                Location = new System.Drawing.Point(150, yPos),
                Width = 180,
                Font = new System.Drawing.Font("Segoe UI", 10),
                TextAlign = System.Drawing.ContentAlignment.MiddleRight,
                ForeColor = System.Drawing.Color.DarkGreen
            };
            totalsGroup.Controls.Add(_vatAmountLabel);

            yPos += 30;
            totalsGroup.Controls.Add(new Label { Text = "الإجمالي:", Location = new System.Drawing.Point(350, yPos), AutoSize = true, Font = new System.Drawing.Font("Segoe UI", 10, System.Drawing.FontStyle.Bold) });
            _totalAmountLabel = new Label
            {
                Text = "0.00 ريال",
                Location = new System.Drawing.Point(150, yPos),
                Width = 180,
                Font = new System.Drawing.Font("Segoe UI", 12, System.Drawing.FontStyle.Bold),
                TextAlign = System.Drawing.ContentAlignment.MiddleRight,
                ForeColor = System.Drawing.Color.DarkBlue
            };
            totalsGroup.Controls.Add(_totalAmountLabel);

            yPos += 35;
            totalsGroup.Controls.Add(new Label { Text = "المدفوع:", Location = new System.Drawing.Point(350, yPos), AutoSize = true });
            _paidAmountLabel = new Label
            {
                Text = "0.00 ريال",
                Location = new System.Drawing.Point(150, yPos),
                Width = 180,
                Font = new System.Drawing.Font("Segoe UI", 10),
                TextAlign = System.Drawing.ContentAlignment.MiddleRight
            };
            totalsGroup.Controls.Add(_paidAmountLabel);

            yPos += 30;
            totalsGroup.Controls.Add(new Label { Text = "المتبقي:", Location = new System.Drawing.Point(350, yPos), AutoSize = true, Font = new System.Drawing.Font("Segoe UI", 10, System.Drawing.FontStyle.Bold) });
            _remainingAmountLabel = new Label
            {
                Text = "0.00 ريال",
                Location = new System.Drawing.Point(150, yPos),
                Width = 180,
                Font = new System.Drawing.Font("Segoe UI", 10, System.Drawing.FontStyle.Bold),
                TextAlign = System.Drawing.ContentAlignment.MiddleRight,
                ForeColor = System.Drawing.Color.Red
            };
            totalsGroup.Controls.Add(_remainingAmountLabel);

            panel.Controls.Add(totalsGroup);

            return panel;
        }

        private Panel CreateBottomPanel()
        {
            var panel = new Panel { Dock = DockStyle.Fill };

            // Filter section
            var filterGroup = new GroupBox
            {
                Text = "تصفية وبحث",
                Location = new System.Drawing.Point(20, 10),
                Width = 1340,
                Height = 70
            };

            filterGroup.Controls.Add(new Label { Text = "بحث:", Location = new System.Drawing.Point(1200, 25), AutoSize = true });
            _searchTextBox = new TextBox
            {
                Location = new System.Drawing.Point(1000, 22),
                Width = 180
            };
            _searchTextBox.TextChanged += (s, e) => LoadOrders();
            filterGroup.Controls.Add(_searchTextBox);

            filterGroup.Controls.Add(new Label { Text = "من تاريخ:", Location = new System.Drawing.Point(900, 25), AutoSize = true });
            _filterFromDatePicker = new DateTimePicker
            {
                Location = new System.Drawing.Point(720, 22),
                Width = 160,
                Format = DateTimePickerFormat.Short
            };
            _filterFromDatePicker.Value = DateTime.Now.AddMonths(-1);
            _filterFromDatePicker.ValueChanged += (s, e) => LoadOrders();
            filterGroup.Controls.Add(_filterFromDatePicker);

            filterGroup.Controls.Add(new Label { Text = "إلى تاريخ:", Location = new System.Drawing.Point(630, 25), AutoSize = true });
            _filterToDatePicker = new DateTimePicker
            {
                Location = new System.Drawing.Point(450, 22),
                Width = 160,
                Format = DateTimePickerFormat.Short
            };
            _filterToDatePicker.ValueChanged += (s, e) => LoadOrders();
            filterGroup.Controls.Add(_filterToDatePicker);

            filterGroup.Controls.Add(new Label { Text = "الحالة:", Location = new System.Drawing.Point(360, 25), AutoSize = true });
            _filterStatusComboBox = new ComboBox
            {
                Location = new System.Drawing.Point(180, 22),
                Width = 160,
                DropDownStyle = ComboBoxStyle.DropDownList
            };
            _filterStatusComboBox.Items.AddRange(new object[] 
            { 
                "الكل", "معلق", "مؤكد", "قيد التنفيذ", "جاهز للتسليم", "تم التسليم", "ملغى", "مرتجع" 
            });
            _filterStatusComboBox.SelectedIndex = 0;
            _filterStatusComboBox.SelectedIndexChanged += (s, e) => LoadOrders();
            filterGroup.Controls.Add(_filterStatusComboBox);

            panel.Controls.Add(filterGroup);

            // Orders grid
            _ordersGrid = new DataGridView
            {
                Location = new System.Drawing.Point(20, 90),
                Width = 1340,
                Height = 140,
                AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill,
                SelectionMode = DataGridViewSelectionMode.FullRowSelect,
                MultiSelect = false,
                ReadOnly = true,
                AllowUserToAddRows = false
            };
            _ordersGrid.SelectionChanged += OrdersGrid_SelectionChanged;
            panel.Controls.Add(_ordersGrid);

            return panel;
        }


        private void LoadProductionCycles()
        {
            try
            {
                var cycles = _context.ProductionCycles
                    .Where(c => c.Status == CycleStatus.Active || c.Status == CycleStatus.Completed)
                    .OrderByDescending(c => c.StartDate)
                    .ToList()
                    .Select(c => new 
                    { 
                        c.Id, 
                        DisplayName = $"{c.Name} - {c.StartDate:yyyy/MM/dd}"
                    })
                    .ToList();

                cycles.Insert(0, new { Id = 0, DisplayName = "-- اختر دورة إنتاج --" });

                _productionCycleComboBox.DataSource = cycles;
                _productionCycleComboBox.DisplayMember = "DisplayName";
                _productionCycleComboBox.ValueMember = "Id";
                _productionCycleComboBox.SelectedIndex = 0;
            }
            catch (Exception ex)
            {
                MessageBox.Show($"خطأ في تحميل دورات الإنتاج: {ex.Message}\n\nDetails: {ex.InnerException?.Message}", "خطأ",
                    MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void LoadOrders()
        {
            try
            {
                var query = _context.SalesOrders
                    .Include(so => so.Customer)
                    .Include(so => so.Items)
                    .AsQueryable();

                // Apply filters
                if (!string.IsNullOrWhiteSpace(_searchTextBox?.Text))
                {
                    var searchTerm = _searchTextBox.Text.ToLower();
                    query = query.Where(so =>
                        so.OrderNumber.ToLower().Contains(searchTerm) ||
                        so.Customer.Name.ToLower().Contains(searchTerm));
                }

                if (_filterFromDatePicker != null)
                {
                    query = query.Where(so => so.OrderDate >= _filterFromDatePicker.Value.Date);
                }

                if (_filterToDatePicker != null)
                {
                    query = query.Where(so => so.OrderDate <= _filterToDatePicker.Value.Date);
                }

                if (_filterStatusComboBox?.SelectedIndex > 0)
                {
                    var status = (SalesOrderStatus)_filterStatusComboBox.SelectedIndex;
                    query = query.Where(so => so.Status == status);
                }

                var orders = query
                    .OrderByDescending(so => so.OrderDate)
                    .Select(so => new
                    {
                        so.Id,
                        رقم_الطلب = so.OrderNumber,
                        العميل = so.Customer.Name,
                        التاريخ = so.OrderDate,
                        عدد_البنود = so.Items.Count,
                        الإجمالي = so.TotalAmount,
                        المدفوع = so.PaidAmount,
                        المتبقي = so.RemainingAmount,
                        الحالة = so.Status.ToString()
                    })
                    .ToList();

                _ordersGrid.DataSource = orders;
                _ordersGrid.Columns["Id"].Visible = false;
            }
            catch (Exception ex)
            {
                MessageBox.Show($"خطأ في تحميل الطلبات: {ex.Message}", "خطأ",
                    MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void OrdersGrid_SelectionChanged(object? sender, EventArgs e)
        {
            if (_ordersGrid.SelectedRows.Count > 0)
            {
                var selectedRow = _ordersGrid.SelectedRows[0];
                _selectedOrderId = (int)selectedRow.Cells["Id"].Value;
                LoadOrderDetails(_selectedOrderId);
            }
        }

        private void LoadOrderDetails(int orderId)
        {
            try
            {
                var order = _context.SalesOrders
                    .Include(so => so.Items)
                    .Include(so => so.Customer)
                    .FirstOrDefault(so => so.Id == orderId);

                if (order != null)
                {
                    _orderNumberTextBox.Text = order.OrderNumber;
                    _customerSearchTextBox.Text = order.Customer?.Name ?? "";
                    _selectedCustomerId = order.CustomerId;
                    _orderDatePicker.Value = order.OrderDate;
                    _expectedDeliveryDatePicker.Value = order.ExpectedDeliveryDate ?? DateTime.Now.AddDays(7);
                    
                    if (order.DeliveryDate.HasValue)
                    {
                        _deliveryDatePicker.Checked = true;
                        _deliveryDatePicker.Value = order.DeliveryDate.Value;
                    }
                    else
                    {
                        _deliveryDatePicker.Checked = false;
                    }

                    _statusComboBox.SelectedIndex = (int)order.Status - 1;
                    _deliveryAddressTextBox.Text = order.DeliveryAddress;
                    _deliveryInstructionsTextBox.Text = order.DeliveryInstructions;
                    _notesTextBox.Text = order.Notes;

                    // Load items
                    _itemsGrid.Rows.Clear();
                    foreach (var item in order.Items)
                    {
                        _itemsGrid.Rows.Add(
                            item.ProductionCycleId,
                            item.ProductName,
                            item.Grade.ToString(),
                            item.Quantity,
                            item.UnitPrice,
                            item.SubTotal,
                            item.DiscountAmount,
                            item.TotalPrice
                        );
                    }

                    CalculateTotals();
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"خطأ في تحميل تفاصيل الطلب: {ex.Message}", "خطأ",
                    MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void ProductionCycleComboBox_SelectedIndexChanged(object? sender, EventArgs e)
        {
            if (_productionCycleComboBox.SelectedValue is int cycleId && cycleId > 0)
            {
                {
                    var cycle = _context.ProductionCycles.Find(cycleId);
                    if (cycle != null)
                    {
                        _productNameTextBox.Text = cycle.Name;
                    }
                }
            }
        }

        private void AddItemButton_Click(object? sender, EventArgs e)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(_productNameTextBox.Text))
                {
                    MessageBox.Show("يرجى إدخال اسم المنتج", "تنبيه",
                        MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    return;
                }

                if (_quantityNumeric.Value <= 0)
                {
                    MessageBox.Show("يرجى إدخال كمية صحيحة", "تنبيه",
                        MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    return;
                }

                int? cycleId = null;
                if (_productionCycleComboBox.SelectedValue is int selectedCycleId && selectedCycleId > 0)
                {
                    cycleId = selectedCycleId;
                }

                var quantity = (decimal?)_quantityNumeric.Value;
                var unitPrice = _unitPriceNumeric.Value;
                var discount = _discountNumeric.Value;

                var subTotal = (decimal)quantity * unitPrice;
                var totalPrice = subTotal - discount;

                _itemsGrid.Rows.Add(
                    cycleId,
                    _productNameTextBox.Text,
                    GetGradeEnum(_gradeComboBox.SelectedIndex),
                    quantity,
                    unitPrice,
                    subTotal,
                    discount,
                    totalPrice
                );

                // Clear item entry
                _productionCycleComboBox.SelectedIndex = 0;
                _productNameTextBox.Clear();
                _gradeComboBox.SelectedIndex = 0;
                _quantityNumeric.Value = 1m; // تعيين 1 بدلاً من 0
                _unitPriceNumeric.Value = 15m; // إعادة ضبط السعر الافتراضي
                _discountNumeric.Value = 0;
                _productNameTextBox.Focus();

                CalculateTotals();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"خطأ في إضافة البند: {ex.Message}", "خطأ",
                    MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void RemoveItemButton_Click(object? sender, EventArgs e)
        {
            if (_itemsGrid.SelectedRows.Count > 0)
            {
                _itemsGrid.Rows.Remove(_itemsGrid.SelectedRows[0]);
                CalculateTotals();
            }
        }

        private void ItemsGrid_CellEndEdit(object? sender, DataGridViewCellEventArgs e)
        {
            try
            {
                var row = _itemsGrid.Rows[e.RowIndex];
                
                // إعادة حساب المجموع الفرعي والإجمالي عند تعديل الكمية أو السعر أو الخصم
                if (e.ColumnIndex == _itemsGrid.Columns["Quantity"].Index ||
                    e.ColumnIndex == _itemsGrid.Columns["UnitPrice"].Index ||
                    e.ColumnIndex == _itemsGrid.Columns["DiscountAmount"].Index)
                {
                    double quantity = Convert.ToDouble(row.Cells["Quantity"].Value ?? 0);
                    decimal unitPrice = Convert.ToDecimal(row.Cells["UnitPrice"].Value ?? 0);
                    decimal discount = Convert.ToDecimal(row.Cells["DiscountAmount"].Value ?? 0);
                    
                    decimal subTotal = (decimal)quantity * unitPrice;
                    decimal totalPrice = subTotal - discount;
                    
                    row.Cells["SubTotal"].Value = subTotal;
                    row.Cells["TotalPrice"].Value = totalPrice;
                }
                
                CalculateTotals();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"خطأ في تحديث البند: {ex.Message}", "خطأ",
                    MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }
        
        private void ItemsGrid_CellValidating(object? sender, DataGridViewCellValidatingEventArgs e)
        {
            // التحقق من صحة القيم المدخلة
            if (e.ColumnIndex == _itemsGrid.Columns["Quantity"].Index ||
                e.ColumnIndex == _itemsGrid.Columns["UnitPrice"].Index ||
                e.ColumnIndex == _itemsGrid.Columns["DiscountAmount"].Index)
            {
                if (!decimal.TryParse(e.FormattedValue?.ToString(), out decimal value) || value < 0)
                {
                    MessageBox.Show("يرجى إدخال قيمة رقمية صحيحة", "تنبيه",
                        MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    e.Cancel = true;
                }
            }
        }

        private void CalculateTotals()
        {
            try
            {
                decimal subTotal = 0;
                decimal discountAmount = 0;

                foreach (DataGridViewRow row in _itemsGrid.Rows)
                {
                    if (row.Cells["SubTotal"].Value != null)
                    {
                        subTotal += Convert.ToDecimal(row.Cells["SubTotal"].Value);
                    }
                    if (row.Cells["DiscountAmount"].Value != null)
                    {
                        discountAmount += Convert.ToDecimal(row.Cells["DiscountAmount"].Value);
                    }
                }

                var subtotalAfterDiscount = subTotal - discountAmount;
                var vatAmount = subtotalAfterDiscount * VAT_RATE;
                var totalAmount = subtotalAfterDiscount + vatAmount;

                _subTotalLabel.Text = $"{subTotal:N2} ريال";
                _discountAmountLabel.Text = $"{discountAmount:N2} ريال";
                _vatAmountLabel.Text = $"{vatAmount:N2} ريال";
                _totalAmountLabel.Text = $"{totalAmount:N2} ريال";

                // For now, assuming no payments yet
                _paidAmountLabel.Text = "0.00 ريال";
                _remainingAmountLabel.Text = $"{totalAmount:N2} ريال";
            }
            catch (Exception ex)
            {
                MessageBox.Show($"خطأ في حساب الإجماليات: {ex.Message}", "خطأ",
                    MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private async void SaveButton_Click(object? sender, EventArgs e)
        {
            try
            {
                if (_selectedCustomerId == 0 || string.IsNullOrWhiteSpace(_customerSearchTextBox.Text))
                {
                    MessageBox.Show("يرجى اختيار العميل", "تنبيه",
                        MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    return;
                }

                if (_itemsGrid.Rows.Count == 0)
                {
                    MessageBox.Show("يرجى إضافة بند واحد على الأقل", "تنبيه",
                        MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    return;
                }

                SalesOrder? order;
                if (_selectedOrderId > 0)
                {
                    order = _context.SalesOrders
                        .Include(so => so.Items)
                        .FirstOrDefault(so => so.Id == _selectedOrderId);
                    
                    if (order == null) return;

                    // Clear existing items
                    _context.SalesOrderItems.RemoveRange(order.Items);
                }
                else
                {
                    order = new SalesOrder
                    {
                        OrderNumber = GenerateOrderNumber(),
                        CreatedAt = DateTime.Now,
                        CreatedBy = Environment.UserName
                    };
                    _context.SalesOrders.Add(order);
                }

                // Update order header
                order!.CustomerId = _selectedCustomerId;
                order.OrderDate = _orderDatePicker.Value.Date;
                order.ExpectedDeliveryDate = _expectedDeliveryDatePicker.Value.Date;
                order.DeliveryDate = _deliveryDatePicker.Checked ? (DateTime?)_deliveryDatePicker.Value.Date : null;
                order.Status = (SalesOrderStatus)(_statusComboBox.SelectedIndex + 1);
                order.DeliveryAddress = _deliveryAddressTextBox.Text ?? string.Empty;
                order.DeliveryInstructions = _deliveryInstructionsTextBox.Text ?? string.Empty;
                order.Notes = _notesTextBox.Text ?? string.Empty;

                // Add items
                decimal subTotal = 0;
                decimal totalDiscount = 0;

                foreach (DataGridViewRow row in _itemsGrid.Rows)
                {
                    // Parse ProductionCycleId safely
                    int? cycleId = null;
                    if (row.Cells["ProductionCycleId"].Value != null && 
                        row.Cells["ProductionCycleId"].Value != DBNull.Value)
                    {
                        int tempId = Convert.ToInt32(row.Cells["ProductionCycleId"].Value);
                        if (tempId > 0)
                            cycleId = tempId;
                    }

                    // Parse Grade safely
                    string gradeValue = row.Cells["Grade"].Value?.ToString() ?? "GradeA";
                    
                    var item = new SalesOrderItem
                    {
                        ProductionCycleId = cycleId,
                        ProductName = row.Cells["ProductName"].Value?.ToString() ?? "غير محدد",
                        Grade = ParseGrade(gradeValue),
                        Quantity = Convert.ToDecimal(row.Cells["Quantity"].Value ?? 0),
                        UnitPrice = Convert.ToDecimal(row.Cells["UnitPrice"].Value ?? 0),
                        SubTotal = Convert.ToDecimal(row.Cells["SubTotal"].Value ?? 0),
                        DiscountAmount = Convert.ToDecimal(row.Cells["DiscountAmount"].Value ?? 0),
                        TotalPrice = Convert.ToDecimal(row.Cells["TotalPrice"].Value ?? 0)
                    };

                    subTotal += item.SubTotal;
                    totalDiscount += item.DiscountAmount;

                    order.Items.Add(item);
                }

                var subtotalAfterDiscount = subTotal - totalDiscount;
                order.SubTotal = subTotal;
                order.DiscountAmount = totalDiscount;
                order.VATAmount = subtotalAfterDiscount * VAT_RATE;
                order.TotalAmount = subtotalAfterDiscount + order.VATAmount;
                order.PaidAmount = 0; // Will be updated through payments
                order.RemainingAmount = order.TotalAmount;

                // Update customer balance
                UpdateCustomerBalance(_selectedCustomerId, order.TotalAmount, isNewOrder: _selectedOrderId == 0);

                _context.SaveChanges();

                // Create tax invoice automatically if this is a new order
                if (_selectedOrderId == 0)
                {
                    await CreateTaxInvoiceForSalesOrderAsync(order.Id);
                }

                MessageBox.Show("تم حفظ أمر المبيعات بنجاح", "نجح",
                    MessageBoxButtons.OK, MessageBoxIcon.Information);

                LoadOrders();
                ClearForm();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"خطأ في حفظ البيانات: {ex.Message}\n{ex.InnerException?.Message}", "خطأ",
                    MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void DeleteButton_Click(object? sender, EventArgs e)
        {
            if (_selectedOrderId == 0)
            {
                MessageBox.Show("يرجى اختيار طلب للحذف", "تنبيه",
                    MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            var result = MessageBox.Show("هل أنت متأكد من حذف هذا الطلب؟",
                "تأكيد الحذف", MessageBoxButtons.YesNo, MessageBoxIcon.Question);

            if (result == DialogResult.Yes)
            {
                try
                {
                    var order = _context.SalesOrders
                        .Include(so => so.Items)
                        .FirstOrDefault(so => so.Id == _selectedOrderId);
                    
                    if (order != null)
                    {
                        _context.SalesOrders.Remove(order);
                        _context.SaveChanges();

                        MessageBox.Show("تم حذف الطلب بنجاح", "نجح",
                            MessageBoxButtons.OK, MessageBoxIcon.Information);

                        LoadOrders();
                        ClearForm();
                    }
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"خطأ في حذف البيانات: {ex.Message}", "خطأ",
                        MessageBoxButtons.OK, MessageBoxIcon.Error);
                }
            }
        }

        private void PrintInvoiceButton_Click(object? sender, EventArgs e)
        {
            if (_selectedOrderId == 0)
            {
                MessageBox.Show("يرجى اختيار طلب لطباعة فاتورته", "تنبيه",
                    MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            try
            {
                var order = _context.SalesOrders.AsNoTracking()
                    .Include(value => value.Customer).Include(value => value.Items)
                    .SingleOrDefault(value => value.Id == _selectedOrderId)
                    ?? throw new InvalidOperationException("Sales order does not exist.");
                var lines = new SalesOrderDocumentService().BuildInvoiceLines(order);
                using var document = new System.Drawing.Printing.PrintDocument();
                var lineIndex = 0;
                document.DocumentName = $"SalesInvoice-{order.OrderNumber}";
                document.PrintPage += (_, args) =>
                {
                    if (args.Graphics == null) return;
                    using var font = new Font("Arial", 11);
                    using var titleFont = new Font("Arial", 16, FontStyle.Bold);
                    using var format = new StringFormat { Alignment = StringAlignment.Far, FormatFlags = StringFormatFlags.DirectionRightToLeft };
                    var y = args.MarginBounds.Top;
                    while (lineIndex < lines.Count)
                    {
                        var currentFont = lineIndex == 0 ? titleFont : font;
                        var height = currentFont.GetHeight(args.Graphics) + 7;
                        if (y + height > args.MarginBounds.Bottom) { args.HasMorePages = true; return; }
                        args.Graphics.DrawString(lines[lineIndex++], currentFont, Brushes.Black,
                            new RectangleF(args.MarginBounds.Left, y, args.MarginBounds.Width, height), format);
                        y += (int)height;
                    }
                };
                using var preview = new PrintPreviewDialog { Document = document, Width = 1100, Height = 800 };
                preview.ShowDialog(this);
            }
            catch (Exception ex)
            {
                LoggingService.LogError(ex, "Error printing sales invoice");
                MessageBox.Show("تعذرت معاينة فاتورة المبيعات. راجع السجل الفني.", "خطأ", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void ClearForm()
        {
            _selectedOrderId = 0;
            _selectedCustomerId = 0;
            _orderNumberTextBox.Clear();
            _customerSearchTextBox.ClearSelection();
            _orderDatePicker.Value = DateTime.Now;
            _expectedDeliveryDatePicker.Value = DateTime.Now.AddDays(7);
            _deliveryDatePicker.Checked = false;
            _statusComboBox.SelectedIndex = 0;
            _deliveryAddressTextBox.Clear();
            _deliveryInstructionsTextBox.Clear();
            _notesTextBox.Clear();
            _itemsGrid.Rows.Clear();
            
            _productionCycleComboBox.SelectedIndex = 0;
            _productNameTextBox.ClearSelection();
            _gradeComboBox.SelectedIndex = 0;
            _quantityNumeric.Value = 1m; // تعيين 1 بدلاً من 0 لتجنب خطأ Minimum
            _unitPriceNumeric.Value = 15m;
            _discountNumeric.Value = 0;

            CalculateTotals();
        }

        private string GenerateOrderNumber()
        {
            var date = DateTime.Now;
            var count = _context.SalesOrders.Count(so => so.OrderDate.Date == date.Date) + 1;
            return $"SO-{date:yyyyMMdd}-{count:D4}";
        }

        private string GetGradeEnum(int index)
        {
            return index switch
            {
                0 => "Premium",
                1 => "GradeA",
                2 => "GradeB",
                3 => "GradeC",
                4 => "Rejected",
                _ => "GradeA"
            };
        }

        private FishGrade ParseGrade(string gradeString)
        {
            return gradeString switch
            {
                "Premium" => FishGrade.Premium,
                "GradeA" => FishGrade.GradeA,
                "GradeB" => FishGrade.GradeB,
                "GradeC" => FishGrade.GradeC,
                "Rejected" => FishGrade.Rejected,
                _ => FishGrade.GradeA
            };
        }

        // Helper class for ComboBox items
        private class ComboItem
        {
            public int Id { get; set; }
            public string Name { get; set; } = string.Empty;
            public override string ToString() => Name;
        }

        /// <summary>
        /// معالج اختيار العميل من الإكمال الذكي
        /// </summary>
        private void CustomerSearch_ItemSelected(object? sender, AutocompleteSelectedEventArgs e)
        {
            try
            {
                if (e.SelectedItem?.AdditionalData.ContainsKey("Id") == true)
                {
                    _selectedCustomerId = Convert.ToInt32(e.SelectedItem.AdditionalData["Id"]);
                    
                    // تعبئة عنوان التسليم تلقائياً
                    if (e.SelectedItem.AdditionalData.ContainsKey("Address"))
                    {
                        var address = e.SelectedItem.AdditionalData["Address"]?.ToString();
                        if (!string.IsNullOrEmpty(address))
                            _deliveryAddressTextBox.Text = address;
                    }
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error in CustomerSearch_ItemSelected: {ex.Message}");
            }
        }

        /// <summary>
        /// معالج اختيار المنتج من الإكمال الذكي
        /// </summary>
        private void ProductName_ItemSelected(object? sender, AutocompleteSelectedEventArgs e)
        {
            try
            {
                if (e.SelectedItem?.AdditionalData.ContainsKey("UnitCost") == true)
                {
                    // تعبئة السعر تلقائياً من سعر الصنف
                    var unitCost = Convert.ToDecimal(e.SelectedItem.AdditionalData["UnitCost"]);
                    _unitPriceNumeric.Value = unitCost * 1.3m; // إضافة هامش ربح 30%
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error in ProductName_ItemSelected: {ex.Message}");
            }
        }

        /// <summary>
        /// تحديث رصيد العميل عند إنشاء أو تعديل طلب مبيعات
        /// </summary>
        private void UpdateCustomerBalance(int customerId, decimal amount, bool isNewOrder)
        {
            try
            {
                var customer = _context.Customers.Find(customerId);
                if (customer != null)
                {
                    if (isNewOrder)
                    {
                        // Add to customer balance for new order
                        customer.CurrentBalance += amount;
                        customer.LastTransactionDate = DateTime.Now;
                    }
                    else
                    {
                        // For existing order, we might need to recalculate the balance
                        // This is a simplified approach - in production, you might want to recalculate from all orders
                        customer.LastTransactionDate = DateTime.Now;
                    }
                    
                    customer.UpdatedAt = DateTime.Now;
                }
            }
            catch (Exception ex)
            {
                // Log the error but don't stop the order creation
                MessageBox.Show($"تحذير: لم يتم تحديث رصيد العميل: {ex.Message}", "تحذير",
                    MessageBoxButtons.OK, MessageBoxIcon.Warning);
            }
        }

        /// <summary>
        /// إنشاء فاتورة ضريبية تلقائياً لطلب المبيعات
        /// </summary>
        private async Task CreateTaxInvoiceForSalesOrderAsync(int salesOrderId)
        {
            try
            {
                var taxInvoiceService = new TaxInvoiceService(_context);
                var taxInvoice = await taxInvoiceService.CreateTaxInvoiceFromSalesOrderAsync(salesOrderId);
                
                if (taxInvoice != null)
                {
                    System.Diagnostics.Debug.WriteLine($"تم إنشاء الفاتورة الضريبية رقم: {taxInvoice.InvoiceNumber}");
                }
            }
            catch (Exception ex)
            {
                // Log the error but don't stop the order creation
                System.Diagnostics.Debug.WriteLine($"تحذير: لم يتم إنشاء الفاتورة الضريبية: {ex.Message}");
            }
        }
    }
}
