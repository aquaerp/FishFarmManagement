using System;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Windows.Forms;
using Microsoft.EntityFrameworkCore;
using FishFarmManager.Data;
using FishFarmManager.Models;
using FishFarmManager.Controls;
using FishFarmManager.Services;

namespace FishFarmManager.Forms
{
    /// <summary>
    /// نموذج إدارة بنود المخزون - CRUD كامل لعناصر المخزون
    /// Inventory Item Management Form - Complete CRUD for inventory items
    /// </summary>
    public partial class InventoryItemForm : Form
    {
        private readonly FishFarmContext _context;
        private readonly AutocompleteService _autocompleteService;
        private InventoryItem? _currentItem = null;
        private bool _isEditing = false;

        // Controls
        private TabControl _mainTabControl = null!;
        private TabPage _itemDetailsTab = null!;
        private TabPage _itemListTab = null!;
        
        // Tab 1: تفاصيل البند
        private TextBox _itemCodeTextBox = null!;
        private SmartAutoCompleteTextBox _itemNameTextBox = null!;
        private TextBox _descriptionTextBox = null!;
        private ComboBox _categoryComboBox = null!;
        private TextBox _unitOfMeasureTextBox = null!;
        private NumericUpDown _currentStockNumeric = null!;
        private NumericUpDown _minimumStockNumeric = null!;
        private NumericUpDown _maximumStockNumeric = null!;
        private NumericUpDown _reorderPointNumeric = null!;
        private NumericUpDown _reorderQuantityNumeric = null!;
        private NumericUpDown _unitCostNumeric = null!;
        private NumericUpDown _sellingPriceNumeric = null!;
        private DateTimePicker _expiryDatePicker = null!;
        private TextBox _batchNumberTextBox = null!;
        private TextBox _storageLocationTextBox = null!;
        private NumericUpDown _requiredTemperatureNumeric = null!;
        private NumericUpDown _requiredHumidityNumeric = null!;
        private CheckBox _hasExpiryDateCheckBox = null!;
        private CheckBox _isPerishableCheckBox = null!;
        private CheckBox _requiresRefrigerationCheckBox = null!;
        private CheckBox _isActiveCheckBox = null!;
        private TextBox _notesTextBox = null!;
        
        // Labels for calculated values
        private Label _stockValueLabel = null!;
        private Label _stockStatusLabel = null!;
        private Label _stockPercentageLabel = null!;
        private Label _needsReorderLabel = null!;
        private Label _daysToExpiryLabel = null!;
        
        // Buttons
        private Button _saveButton = null!;
        private Button _newButton = null!;
        private Button _editButton = null!;
        private Button _deleteButton = null!;
        private Button _cancelButton = null!;
        
        // Tab 2: قائمة البنود
        private DataGridView _itemsGrid = null!;
        private ComboBox _categoryFilterComboBox = null!;
        private TextBox _searchTextBox = null!;
        private CheckBox _lowStockOnlyCheckBox = null!;
        private CheckBox _expiredOnlyCheckBox = null!;
        private Button _refreshButton = null!;
        private Button _exportButton = null!;

        public InventoryItemForm(FishFarmContext context)
        {
            _context = context;
            _autocompleteService = new AutocompleteService(_context);
            InitializeComponent();
            SetupForm();
            LoadCategories();
            LoadInventoryItems();
            SetFormMode(false);
        }

        private void InitializeComponent()
        {
            this.Size = new Size(1000, 700);
            this.Text = "إدارة بنود المخزون - Inventory Items Management";
            this.StartPosition = FormStartPosition.CenterScreen;
            this.RightToLeft = RightToLeft.Yes;
            this.RightToLeftLayout = true;

            // Main TabControl
            _mainTabControl = new TabControl
            {
                Dock = DockStyle.Fill,
                Font = new Font("Segoe UI", 9F)
            };

            // Tab 1: تفاصيل البند
            _itemDetailsTab = new TabPage("تفاصيل البند");
            CreateItemDetailsTab();

            // Tab 2: قائمة البنود
            _itemListTab = new TabPage("قائمة البنود");
            CreateItemListTab();

            _mainTabControl.TabPages.Add(_itemDetailsTab);
            _mainTabControl.TabPages.Add(_itemListTab);

            this.Controls.Add(_mainTabControl);
        }

        private void CreateItemDetailsTab()
        {
            var panel = new Panel { Dock = DockStyle.Fill, Padding = new Padding(10) };

            // معلومات أساسية
            var basicInfoGroup = new GroupBox
            {
                Text = "المعلومات الأساسية",
                Location = new Point(10, 10),
                Size = new Size(460, 200),
                Font = new Font("Segoe UI", 9F, FontStyle.Bold)
            };

            // كود البند
            var lblItemCode = new Label { Text = "كود البند:", Location = new Point(350, 25), Size = new Size(80, 20) };
            _itemCodeTextBox = new TextBox { Location = new Point(200, 25), Size = new Size(140, 20) };

            // اسم البند
            var lblItemName = new Label { Text = "اسم البند:", Location = new Point(350, 55), Size = new Size(80, 20) };
            _itemNameTextBox = new SmartAutoCompleteTextBox(_autocompleteService)
            {
                Location = new Point(50, 55),
                Size = new Size(290, 20),
                EntityType = "InventoryItem",
                SearchField = "All",
                MinCharacters = 2,
                AutoFillDetails = true,
                RecordUsage = true
            };
            _itemNameTextBox.DataFilled += ItemNameTextBox_DataFilled;

            // الوصف
            var lblDescription = new Label { Text = "الوصف:", Location = new Point(350, 85), Size = new Size(80, 20) };
            _descriptionTextBox = new TextBox 
            { 
                Location = new Point(50, 85), 
                Size = new Size(290, 60),
                Multiline = true,
                ScrollBars = ScrollBars.Vertical
            };

            // فئة المخزون
            var lblCategory = new Label { Text = "فئة المخزون:", Location = new Point(350, 155), Size = new Size(80, 20) };
            _categoryComboBox = new ComboBox 
            { 
                Location = new Point(200, 155), 
                Size = new Size(140, 20),
                DropDownStyle = ComboBoxStyle.DropDownList
            };

            // وحدة القياس
            var lblUnitOfMeasure = new Label { Text = "وحدة القياس:", Location = new Point(80, 155), Size = new Size(80, 20) };
            _unitOfMeasureTextBox = new TextBox { Location = new Point(10, 155), Size = new Size(60, 20) };

            basicInfoGroup.Controls.AddRange(new Control[] {
                lblItemCode, _itemCodeTextBox,
                lblItemName, _itemNameTextBox,
                lblDescription, _descriptionTextBox,
                lblCategory, _categoryComboBox,
                lblUnitOfMeasure, _unitOfMeasureTextBox
            });

            // معلومات المخزون
            var stockInfoGroup = new GroupBox
            {
                Text = "معلومات المخزون",
                Location = new Point(480, 10),
                Size = new Size(460, 200),
                Font = new Font("Segoe UI", 9F, FontStyle.Bold)
            };

            // الرصيد الحالي
            var lblCurrentStock = new Label { Text = "الرصيد الحالي:", Location = new Point(350, 25), Size = new Size(80, 20) };
            _currentStockNumeric = new NumericUpDown 
            { 
                Location = new Point(200, 25), 
                Size = new Size(140, 20),
                DecimalPlaces = 3,
                Maximum = 999999999
            };

            // الحد الأدنى
            var lblMinStock = new Label { Text = "الحد الأدنى:", Location = new Point(350, 55), Size = new Size(80, 20) };
            _minimumStockNumeric = new NumericUpDown 
            { 
                Location = new Point(200, 55), 
                Size = new Size(140, 20),
                DecimalPlaces = 3,
                Maximum = 999999999
            };

            // الحد الأقصى
            var lblMaxStock = new Label { Text = "الحد الأقصى:", Location = new Point(350, 85), Size = new Size(80, 20) };
            _maximumStockNumeric = new NumericUpDown 
            { 
                Location = new Point(200, 85), 
                Size = new Size(140, 20),
                DecimalPlaces = 3,
                Maximum = 999999999
            };

            // نقطة إعادة الطلب
            var lblReorderPoint = new Label { Text = "نقطة إعادة الطلب:", Location = new Point(90, 25), Size = new Size(90, 20) };
            _reorderPointNumeric = new NumericUpDown 
            { 
                Location = new Point(10, 25), 
                Size = new Size(70, 20),
                DecimalPlaces = 3,
                Maximum = 999999999
            };

            // كمية إعادة الطلب
            var lblReorderQty = new Label { Text = "كمية إعادة الطلب:", Location = new Point(90, 55), Size = new Size(90, 20) };
            _reorderQuantityNumeric = new NumericUpDown 
            { 
                Location = new Point(10, 55), 
                Size = new Size(70, 20),
                DecimalPlaces = 3,
                Maximum = 999999999
            };

            // تكلفة الوحدة
            var lblUnitCost = new Label { Text = "تكلفة الوحدة:", Location = new Point(350, 115), Size = new Size(80, 20) };
            _unitCostNumeric = new NumericUpDown 
            { 
                Location = new Point(200, 115), 
                Size = new Size(140, 20),
                DecimalPlaces = 2,
                Maximum = 999999999
            };

            // سعر البيع
            var lblSellingPrice = new Label { Text = "سعر البيع:", Location = new Point(90, 115), Size = new Size(80, 20) };
            _sellingPriceNumeric = new NumericUpDown 
            { 
                Location = new Point(10, 115), 
                Size = new Size(70, 20),
                DecimalPlaces = 2,
                Maximum = 999999999
            };

            // قيمة المخزون
            var lblStockValue = new Label { Text = "قيمة المخزون:", Location = new Point(350, 145), Size = new Size(80, 20) };
            _stockValueLabel = new Label 
            { 
                Location = new Point(200, 145), 
                Size = new Size(140, 20),
                ForeColor = Color.Blue,
                Font = new Font("Segoe UI", 9F, FontStyle.Bold)
            };

            // حالة المخزون
            var lblStockStatus = new Label { Text = "حالة المخزون:", Location = new Point(90, 145), Size = new Size(80, 20) };
            _stockStatusLabel = new Label 
            { 
                Location = new Point(10, 145), 
                Size = new Size(70, 20),
                Font = new Font("Segoe UI", 9F, FontStyle.Bold)
            };

            // نسبة المخزون
            var lblStockPercentage = new Label { Text = "نسبة المخزون:", Location = new Point(350, 175), Size = new Size(80, 20) };
            _stockPercentageLabel = new Label 
            { 
                Location = new Point(200, 175), 
                Size = new Size(140, 20),
                ForeColor = Color.Green
            };

            // يحتاج إعادة طلب
            var lblNeedsReorder = new Label { Text = "يحتاج إعادة طلب:", Location = new Point(90, 175), Size = new Size(80, 20) };
            _needsReorderLabel = new Label 
            { 
                Location = new Point(10, 175), 
                Size = new Size(70, 20),
                ForeColor = Color.Red,
                Font = new Font("Segoe UI", 9F, FontStyle.Bold)
            };

            stockInfoGroup.Controls.AddRange(new Control[] {
                lblCurrentStock, _currentStockNumeric,
                lblMinStock, _minimumStockNumeric,
                lblMaxStock, _maximumStockNumeric,
                lblReorderPoint, _reorderPointNumeric,
                lblReorderQty, _reorderQuantityNumeric,
                lblUnitCost, _unitCostNumeric,
                lblSellingPrice, _sellingPriceNumeric,
                lblStockValue, _stockValueLabel,
                lblStockStatus, _stockStatusLabel,
                lblStockPercentage, _stockPercentageLabel,
                lblNeedsReorder, _needsReorderLabel
            });

            // معلومات إضافية
            var additionalInfoGroup = new GroupBox
            {
                Text = "معلومات إضافية",
                Location = new Point(10, 220),
                Size = new Size(460, 200),
                Font = new Font("Segoe UI", 9F, FontStyle.Bold)
            };

            // تاريخ انتهاء الصلاحية
            var lblExpiryDate = new Label { Text = "تاريخ الصلاحية:", Location = new Point(350, 25), Size = new Size(80, 20) };
            _expiryDatePicker = new DateTimePicker 
            { 
                Location = new Point(200, 25), 
                Size = new Size(140, 20),
                Format = DateTimePickerFormat.Short
            };

            // رقم الدفعة
            var lblBatchNumber = new Label { Text = "رقم الدفعة:", Location = new Point(90, 25), Size = new Size(80, 20) };
            _batchNumberTextBox = new TextBox { Location = new Point(10, 25), Size = new Size(70, 20) };

            // موقع التخزين
            var lblStorageLocation = new Label { Text = "موقع التخزين:", Location = new Point(350, 55), Size = new Size(80, 20) };
            _storageLocationTextBox = new TextBox { Location = new Point(200, 55), Size = new Size(140, 20) };

            // درجة الحرارة المطلوبة
            var lblRequiredTemp = new Label { Text = "درجة الحرارة:", Location = new Point(90, 55), Size = new Size(80, 20) };
            _requiredTemperatureNumeric = new NumericUpDown 
            { 
                Location = new Point(10, 55), 
                Size = new Size(70, 20),
                DecimalPlaces = 1,
                Minimum = -50,
                Maximum = 100
            };

            // الرطوبة المطلوبة
            var lblRequiredHumidity = new Label { Text = "الرطوبة (%):", Location = new Point(350, 85), Size = new Size(80, 20) };
            _requiredHumidityNumeric = new NumericUpDown 
            { 
                Location = new Point(200, 85), 
                Size = new Size(140, 20),
                DecimalPlaces = 1,
                Maximum = 100
            };

            // أيام حتى انتهاء الصلاحية
            var lblDaysToExpiry = new Label { Text = "أيام للصلاحية:", Location = new Point(90, 85), Size = new Size(80, 20) };
            _daysToExpiryLabel = new Label 
            { 
                Location = new Point(10, 85), 
                Size = new Size(70, 20),
                ForeColor = Color.Orange
            };

            // CheckBoxes
            _hasExpiryDateCheckBox = new CheckBox 
            { 
                Text = "له تاريخ صلاحية",
                Location = new Point(300, 115), 
                Size = new Size(120, 20)
            };

            _isPerishableCheckBox = new CheckBox 
            { 
                Text = "قابل للتلف",
                Location = new Point(200, 115), 
                Size = new Size(90, 20)
            };

            _requiresRefrigerationCheckBox = new CheckBox 
            { 
                Text = "يتطلب تبريد",
                Location = new Point(100, 115), 
                Size = new Size(90, 20)
            };

            _isActiveCheckBox = new CheckBox 
            { 
                Text = "نشط",
                Location = new Point(10, 115), 
                Size = new Size(80, 20),
                Checked = true
            };

            // ملاحظات
            var lblNotes = new Label { Text = "ملاحظات:", Location = new Point(350, 145), Size = new Size(80, 20) };
            _notesTextBox = new TextBox 
            { 
                Location = new Point(10, 145), 
                Size = new Size(330, 45),
                Multiline = true,
                ScrollBars = ScrollBars.Vertical
            };

            additionalInfoGroup.Controls.AddRange(new Control[] {
                lblExpiryDate, _expiryDatePicker,
                lblBatchNumber, _batchNumberTextBox,
                lblStorageLocation, _storageLocationTextBox,
                lblRequiredTemp, _requiredTemperatureNumeric,
                lblRequiredHumidity, _requiredHumidityNumeric,
                lblDaysToExpiry, _daysToExpiryLabel,
                _hasExpiryDateCheckBox, _isPerishableCheckBox,
                _requiresRefrigerationCheckBox, _isActiveCheckBox,
                lblNotes, _notesTextBox
            });

            // أزرار العمليات
            var buttonsPanel = new Panel
            {
                Location = new Point(480, 220),
                Size = new Size(460, 200),
                BackColor = Color.LightGray
            };

            _newButton = new Button 
            { 
                Text = "جديد", 
                Location = new Point(370, 20), 
                Size = new Size(80, 30),
                BackColor = Color.LightBlue
            };

            _saveButton = new Button 
            { 
                Text = "حفظ", 
                Location = new Point(280, 20), 
                Size = new Size(80, 30),
                BackColor = Color.LightGreen
            };

            _editButton = new Button 
            { 
                Text = "تعديل", 
                Location = new Point(190, 20), 
                Size = new Size(80, 30),
                BackColor = Color.LightYellow
            };

            _deleteButton = new Button 
            { 
                Text = "حذف", 
                Location = new Point(100, 20), 
                Size = new Size(80, 30),
                BackColor = Color.LightCoral
            };

            _cancelButton = new Button 
            { 
                Text = "إلغاء", 
                Location = new Point(10, 20), 
                Size = new Size(80, 30),
                BackColor = Color.LightGray
            };

            buttonsPanel.Controls.AddRange(new Control[] {
                _newButton, _saveButton, _editButton, _deleteButton, _cancelButton
            });

            // Event handlers
            _newButton.Click += NewButton_Click;
            _saveButton.Click += SaveButton_Click;
            _editButton.Click += EditButton_Click;
            _deleteButton.Click += DeleteButton_Click;
            _cancelButton.Click += CancelButton_Click;

            _currentStockNumeric.ValueChanged += (s, e) => CalculateStockMetrics();
            _minimumStockNumeric.ValueChanged += (s, e) => CalculateStockMetrics();
            _maximumStockNumeric.ValueChanged += (s, e) => CalculateStockMetrics();
            _reorderPointNumeric.ValueChanged += (s, e) => CalculateStockMetrics();
            _unitCostNumeric.ValueChanged += (s, e) => CalculateStockMetrics();
            _expiryDatePicker.ValueChanged += (s, e) => CalculateStockMetrics();
            _hasExpiryDateCheckBox.CheckedChanged += (s, e) => _expiryDatePicker.Enabled = _hasExpiryDateCheckBox.Checked;

            panel.Controls.AddRange(new Control[] {
                basicInfoGroup, stockInfoGroup, additionalInfoGroup, buttonsPanel
            });

            _itemDetailsTab.Controls.Add(panel);
        }

        private void CreateItemListTab()
        {
            var panel = new Panel { Dock = DockStyle.Fill, Padding = new Padding(10) };

            // شريط البحث والفلترة
            var filterPanel = new Panel
            {
                Dock = DockStyle.Top,
                Height = 80,
                BackColor = Color.LightBlue
            };

            var lblSearch = new Label { Text = "البحث:", Location = new Point(850, 15), Size = new Size(50, 20) };
            _searchTextBox = new TextBox { Location = new Point(700, 15), Size = new Size(140, 20) };

            var lblCategoryFilter = new Label { Text = "الفئة:", Location = new Point(620, 15), Size = new Size(50, 20) };
            _categoryFilterComboBox = new ComboBox 
            { 
                Location = new Point(470, 15), 
                Size = new Size(140, 20),
                DropDownStyle = ComboBoxStyle.DropDownList
            };

            _lowStockOnlyCheckBox = new CheckBox 
            { 
                Text = "مخزون منخفض فقط",
                Location = new Point(300, 15), 
                Size = new Size(150, 20)
            };

            _expiredOnlyCheckBox = new CheckBox 
            { 
                Text = "منتهي الصلاحية فقط",
                Location = new Point(120, 15), 
                Size = new Size(150, 20)
            };

            _refreshButton = new Button 
            { 
                Text = "تحديث", 
                Location = new Point(30, 15), 
                Size = new Size(80, 25),
                BackColor = Color.LightGreen
            };

            _exportButton = new Button 
            { 
                Text = "تصدير", 
                Location = new Point(700, 45), 
                Size = new Size(80, 25),
                BackColor = Color.LightYellow
            };

            var lblResults = new Label 
            { 
                Text = "عدد النتائج: 0",
                Location = new Point(30, 45), 
                Size = new Size(200, 20),
                ForeColor = Color.Blue
            };

            filterPanel.Controls.AddRange(new Control[] {
                lblSearch, _searchTextBox,
                lblCategoryFilter, _categoryFilterComboBox,
                _lowStockOnlyCheckBox, _expiredOnlyCheckBox,
                _refreshButton, _exportButton, lblResults
            });

            // DataGridView
            _itemsGrid = new DataGridView
            {
                Dock = DockStyle.Fill,
                AutoGenerateColumns = false,
                AllowUserToAddRows = false,
                AllowUserToDeleteRows = false,
                ReadOnly = true,
                SelectionMode = DataGridViewSelectionMode.FullRowSelect,
                MultiSelect = false,
                BackgroundColor = Color.White,
                GridColor = Color.LightGray
            };

            // تكوين الأعمدة
            _itemsGrid.Columns.AddRange(new DataGridViewColumn[]
            {
                new DataGridViewTextBoxColumn { Name = "ItemCode", HeaderText = "كود البند", Width = 80 },
                new DataGridViewTextBoxColumn { Name = "ItemName", HeaderText = "اسم البند", Width = 150 },
                new DataGridViewTextBoxColumn { Name = "CategoryDisplay", HeaderText = "الفئة", Width = 120 },
                new DataGridViewTextBoxColumn { Name = "UnitOfMeasure", HeaderText = "الوحدة", Width = 60 },
                new DataGridViewTextBoxColumn { Name = "CurrentStock", HeaderText = "الرصيد", Width = 80 },
                new DataGridViewTextBoxColumn { Name = "UnitCost", HeaderText = "التكلفة", Width = 80 },
                new DataGridViewTextBoxColumn { Name = "StockValue", HeaderText = "القيمة", Width = 80 },
                new DataGridViewTextBoxColumn { Name = "StockStatusDisplay", HeaderText = "الحالة", Width = 80 },
                new DataGridViewTextBoxColumn { Name = "NeedsReorder", HeaderText = "يحتاج طلب", Width = 80 },
                new DataGridViewTextBoxColumn { Name = "DaysToExpiry", HeaderText = "أيام للصلاحية", Width = 100 },
                new DataGridViewCheckBoxColumn { Name = "IsActive", HeaderText = "نشط", Width = 50 }
            });

            // Event handlers
            _searchTextBox.TextChanged += (s, e) => FilterInventoryItems();
            _categoryFilterComboBox.SelectedIndexChanged += (s, e) => FilterInventoryItems();
            _lowStockOnlyCheckBox.CheckedChanged += (s, e) => FilterInventoryItems();
            _expiredOnlyCheckBox.CheckedChanged += (s, e) => FilterInventoryItems();
            _refreshButton.Click += (s, e) => LoadInventoryItems();
            _exportButton.Click += ExportButton_Click;

            _itemsGrid.CellDoubleClick += ItemsGrid_CellDoubleClick;
            _itemsGrid.DataBindingComplete += (s, e) => 
            {
                lblResults.Text = $"عدد النتائج: {_itemsGrid.Rows.Count}";
                ColorizeGrid();
            };

            panel.Controls.Add(_itemsGrid);
            panel.Controls.Add(filterPanel);

            _itemListTab.Controls.Add(panel);
        }

        private void SetupForm()
        {
            // إعداد النموذج الأولي
            _expiryDatePicker.Enabled = false;
        }

        private void LoadCategories()
        {
            var categories = Enum.GetValues(typeof(InventoryCategory))
                .Cast<InventoryCategory>()
                .Select(c => new { Value = c, Text = c.GetCategoryDisplay() })
                .ToList();

            _categoryComboBox.DataSource = categories.ToList();
            _categoryComboBox.DisplayMember = "Text";
            _categoryComboBox.ValueMember = "Value";

            // للفلترة
            var filterCategories = new List<dynamic>();
            filterCategories.Add(new { Value = (InventoryCategory?)null, Text = "جميع الفئات" });
            filterCategories.AddRange(categories);

            _categoryFilterComboBox.DataSource = filterCategories;
            _categoryFilterComboBox.DisplayMember = "Text";
            _categoryFilterComboBox.ValueMember = "Value";
        }

        private void LoadInventoryItems()
        {
            try
            {
                var items = _context.InventoryItems
                    .OrderBy(i => i.ItemName)
                    .Select(i => new
                    {
                        i.Id,
                        i.ItemCode,
                        i.ItemName,
                        CategoryDisplay = i.Category.GetCategoryDisplay(),
                        i.UnitOfMeasure,
                        i.CurrentStock,
                        i.UnitCost,
                        StockValue = i.StockValue,
                        StockStatusDisplay = i.GetStockStatusDisplay(),
                        NeedsReorder = i.NeedsReorder ? "نعم" : "لا",
                        DaysToExpiry = i.DaysToExpiry.HasValue ? i.DaysToExpiry.Value.ToString() : "غير محدد",
                        i.IsActive,
                        Item = i
                    })
                    .ToList();

                _itemsGrid.DataSource = items;
            }
            catch (Exception ex)
            {
                MessageBox.Show($"خطأ في تحميل بنود المخزون: {ex.Message}", "خطأ", 
                    MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void FilterInventoryItems()
        {
            try
            {
                var query = _context.InventoryItems.AsQueryable();

                // البحث النصي
                if (!string.IsNullOrWhiteSpace(_searchTextBox.Text))
                {
                    var searchText = _searchTextBox.Text.Trim().ToLower();
                    query = query.Where(i => 
                        (i.ItemCode != null && i.ItemCode.ToLower().Contains(searchText)) ||
                        (i.ItemName != null && i.ItemName.ToLower().Contains(searchText)) ||
                        (i.Description != null && i.Description.ToLower().Contains(searchText)));
                }

                // فلترة الفئة
                if (_categoryFilterComboBox.SelectedValue is InventoryCategory category)
                {
                    query = query.Where(i => i.Category == category);
                }

                // فلترة المخزون المنخفض
                if (_lowStockOnlyCheckBox.Checked)
                {
                    query = query.Where(i => i.CurrentStock <= i.ReorderPoint);
                }

                // فلترة منتهي الصلاحية
                if (_expiredOnlyCheckBox.Checked)
                {
                    var today = DateTime.Now;
                    query = query.Where(i => i.ExpiryDate.HasValue && i.ExpiryDate.Value < today);
                }

                var items = query
                    .OrderBy(i => i.ItemName)
                    .Select(i => new
                    {
                        i.Id,
                        i.ItemCode,
                        i.ItemName,
                        CategoryDisplay = i.Category.GetCategoryDisplay(),
                        i.UnitOfMeasure,
                        i.CurrentStock,
                        i.UnitCost,
                        StockValue = i.StockValue,
                        StockStatusDisplay = i.GetStockStatusDisplay(),
                        NeedsReorder = i.NeedsReorder ? "نعم" : "لا",
                        DaysToExpiry = i.DaysToExpiry.HasValue ? i.DaysToExpiry.Value.ToString() : "غير محدد",
                        i.IsActive,
                        Item = i
                    })
                    .ToList();

                _itemsGrid.DataSource = items;
            }
            catch (Exception ex)
            {
                MessageBox.Show($"خطأ في فلترة البيانات: {ex.Message}", "خطأ", 
                    MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void ColorizeGrid()
        {
            foreach (DataGridViewRow row in _itemsGrid.Rows)
            {
                if (row.DataBoundItem != null)
                {
                    dynamic item = row.DataBoundItem;
                    
                    // تلوين الصفوف حسب حالة المخزون
                    if (item.NeedsReorder == "نعم")
                    {
                        row.DefaultCellStyle.BackColor = Color.LightCoral;
                    }
                    else if (item.DaysToExpiry != "غير محدد")
                    {
                        if (int.TryParse(item.DaysToExpiry, out int days) && days <= 30 && days >= 0)
                        {
                            row.DefaultCellStyle.BackColor = Color.LightYellow;
                        }
                    }
                    else if (!item.IsActive)
                    {
                        row.DefaultCellStyle.BackColor = Color.LightGray;
                    }
                }
            }
        }

        private void CalculateStockMetrics()
        {
            try
            {
                // قيمة المخزون
                var stockValue = _currentStockNumeric.Value * _unitCostNumeric.Value;
                _stockValueLabel.Text = $"{stockValue:N2} ريال";

                // نسبة المخزون
                if (_maximumStockNumeric.Value > 0)
                {
                    var percentage = (_currentStockNumeric.Value / _maximumStockNumeric.Value) * 100;
                    _stockPercentageLabel.Text = $"{percentage:F1}%";
                    
                    if (percentage < 20)
                        _stockPercentageLabel.ForeColor = Color.Red;
                    else if (percentage < 50)
                        _stockPercentageLabel.ForeColor = Color.Orange;
                    else
                        _stockPercentageLabel.ForeColor = Color.Green;
                }

                // حالة المخزون
                if (_currentStockNumeric.Value <= 0)
                {
                    _stockStatusLabel.Text = "نفد المخزون";
                    _stockStatusLabel.ForeColor = Color.Red;
                }
                else if (_currentStockNumeric.Value <= _reorderPointNumeric.Value)
                {
                    _stockStatusLabel.Text = "مخزون منخفض";
                    _stockStatusLabel.ForeColor = Color.Orange;
                }
                else if (_currentStockNumeric.Value >= _maximumStockNumeric.Value)
                {
                    _stockStatusLabel.Text = "مخزون زائد";
                    _stockStatusLabel.ForeColor = Color.Purple;
                }
                else
                {
                    _stockStatusLabel.Text = "متوفر";
                    _stockStatusLabel.ForeColor = Color.Green;
                }

                // يحتاج إعادة طلب
                var needsReorder = _currentStockNumeric.Value <= _reorderPointNumeric.Value;
                _needsReorderLabel.Text = needsReorder ? "نعم" : "لا";
                _needsReorderLabel.ForeColor = needsReorder ? Color.Red : Color.Green;

                // أيام حتى انتهاء الصلاحية
                if (_hasExpiryDateCheckBox.Checked)
                {
                    var daysToExpiry = (_expiryDatePicker.Value - DateTime.Now).Days;
                    _daysToExpiryLabel.Text = daysToExpiry.ToString();
                    
                    if (daysToExpiry < 0)
                        _daysToExpiryLabel.ForeColor = Color.Red;
                    else if (daysToExpiry <= 30)
                        _daysToExpiryLabel.ForeColor = Color.Orange;
                    else
                        _daysToExpiryLabel.ForeColor = Color.Green;
                }
                else
                {
                    _daysToExpiryLabel.Text = "غير محدد";
                    _daysToExpiryLabel.ForeColor = Color.Gray;
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"خطأ في حساب مؤشرات المخزون: {ex.Message}", "خطأ", 
                    MessageBoxButtons.OK, MessageBoxIcon.Warning);
            }
        }

        private void SetFormMode(bool isEditing)
        {
            _isEditing = isEditing;
            
            _itemCodeTextBox.Enabled = isEditing;
            _itemNameTextBox.Enabled = isEditing;
            _descriptionTextBox.Enabled = isEditing;
            _categoryComboBox.Enabled = isEditing;
            _unitOfMeasureTextBox.Enabled = isEditing;
            _currentStockNumeric.Enabled = isEditing;
            _minimumStockNumeric.Enabled = isEditing;
            _maximumStockNumeric.Enabled = isEditing;
            _reorderPointNumeric.Enabled = isEditing;
            _reorderQuantityNumeric.Enabled = isEditing;
            _unitCostNumeric.Enabled = isEditing;
            _sellingPriceNumeric.Enabled = isEditing;
            _expiryDatePicker.Enabled = isEditing && _hasExpiryDateCheckBox.Checked;
            _batchNumberTextBox.Enabled = isEditing;
            _storageLocationTextBox.Enabled = isEditing;
            _requiredTemperatureNumeric.Enabled = isEditing;
            _requiredHumidityNumeric.Enabled = isEditing;
            _hasExpiryDateCheckBox.Enabled = isEditing;
            _isPerishableCheckBox.Enabled = isEditing;
            _requiresRefrigerationCheckBox.Enabled = isEditing;
            _isActiveCheckBox.Enabled = isEditing;
            _notesTextBox.Enabled = isEditing;

            _saveButton.Enabled = isEditing;
            _cancelButton.Enabled = isEditing;
            _newButton.Enabled = !isEditing;
            _editButton.Enabled = !isEditing && _currentItem != null;
            _deleteButton.Enabled = !isEditing && _currentItem != null;
        }

        private void LoadItemToForm(InventoryItem item)
        {
            _currentItem = item;

            _itemCodeTextBox.Text = item.ItemCode;
            _itemNameTextBox.Text = item.ItemName;
            _descriptionTextBox.Text = item.Description ?? string.Empty;
            _categoryComboBox.SelectedValue = item.Category;
            _unitOfMeasureTextBox.Text = item.UnitOfMeasure;
            _currentStockNumeric.Value = item.CurrentStock;
            _minimumStockNumeric.Value = item.MinimumStock;
            _maximumStockNumeric.Value = item.MaximumStock;
            _reorderPointNumeric.Value = item.ReorderPoint ?? 0m;
            _reorderQuantityNumeric.Value = item.ReorderQuantity ?? 0m;
            _unitCostNumeric.Value = item.UnitCost;
            _sellingPriceNumeric.Value = item.SellingPrice;
            
            if (item.ExpiryDate.HasValue)
                _expiryDatePicker.Value = item.ExpiryDate.Value;
            
            _batchNumberTextBox.Text = item.BatchNumber ?? string.Empty;
            _storageLocationTextBox.Text = item.StorageLocation ?? string.Empty;
            
            if (item.RequiredTemperature.HasValue)
                _requiredTemperatureNumeric.Value = item.RequiredTemperature.Value;
            
            if (item.RequiredHumidity.HasValue)
                _requiredHumidityNumeric.Value = item.RequiredHumidity.Value;
            
            _hasExpiryDateCheckBox.Checked = item.HasExpiryDate;
            _isPerishableCheckBox.Checked = item.IsPerishable;
            _requiresRefrigerationCheckBox.Checked = item.RequiresRefrigeration;
            _isActiveCheckBox.Checked = item.IsActive;
            _notesTextBox.Text = item.Notes ?? string.Empty;

            CalculateStockMetrics();
        }

        private void ClearForm()
        {
            _currentItem = null;

            _itemCodeTextBox.Clear();
            _itemNameTextBox.Clear();
            _descriptionTextBox.Clear();
            _categoryComboBox.SelectedIndex = 0;
            _unitOfMeasureTextBox.Clear();
            _currentStockNumeric.Value = 0;
            _minimumStockNumeric.Value = 0;
            _maximumStockNumeric.Value = 0;
            _reorderPointNumeric.Value = 0;
            _reorderQuantityNumeric.Value = 0;
            _unitCostNumeric.Value = 0;
            _sellingPriceNumeric.Value = 0;
            _expiryDatePicker.Value = DateTime.Now.AddMonths(1);
            _batchNumberTextBox.Clear();
            _storageLocationTextBox.Clear();
            _requiredTemperatureNumeric.Value = 0;
            _requiredHumidityNumeric.Value = 0;
            _hasExpiryDateCheckBox.Checked = false;
            _isPerishableCheckBox.Checked = false;
            _requiresRefrigerationCheckBox.Checked = false;
            _isActiveCheckBox.Checked = true;
            _notesTextBox.Clear();

            CalculateStockMetrics();
        }

        private bool ValidateForm()
        {
            if (string.IsNullOrWhiteSpace(_itemCodeTextBox.Text))
            {
                MessageBox.Show("يجب إدخال كود البند", "خطأ في البيانات", 
                    MessageBoxButtons.OK, MessageBoxIcon.Warning);
                _itemCodeTextBox.Focus();
                return false;
            }

            if (string.IsNullOrWhiteSpace(_itemNameTextBox.Text))
            {
                MessageBox.Show("يجب إدخال اسم البند", "خطأ في البيانات", 
                    MessageBoxButtons.OK, MessageBoxIcon.Warning);
                _itemNameTextBox.Focus();
                return false;
            }

            if (string.IsNullOrWhiteSpace(_unitOfMeasureTextBox.Text))
            {
                MessageBox.Show("يجب إدخال وحدة القياس", "خطأ في البيانات", 
                    MessageBoxButtons.OK, MessageBoxIcon.Warning);
                _unitOfMeasureTextBox.Focus();
                return false;
            }

            if (_maximumStockNumeric.Value <= _minimumStockNumeric.Value)
            {
                MessageBox.Show("الحد الأقصى يجب أن يكون أكبر من الحد الأدنى", "خطأ في البيانات", 
                    MessageBoxButtons.OK, MessageBoxIcon.Warning);
                _maximumStockNumeric.Focus();
                return false;
            }

            if (_reorderPointNumeric.Value > _maximumStockNumeric.Value)
            {
                MessageBox.Show("نقطة إعادة الطلب يجب أن تكون أقل من أو مساوية للحد الأقصى", "خطأ في البيانات", 
                    MessageBoxButtons.OK, MessageBoxIcon.Warning);
                _reorderPointNumeric.Focus();
                return false;
            }

            // التحقق من عدم تكرار كود البند
            var existingItem = _context.InventoryItems
                .FirstOrDefault(i => i.ItemCode == _itemCodeTextBox.Text && 
                               (_currentItem == null || i.Id != _currentItem.Id));
            
            if (existingItem != null)
            {
                MessageBox.Show("كود البند موجود مسبقاً", "خطأ في البيانات", 
                    MessageBoxButtons.OK, MessageBoxIcon.Warning);
                _itemCodeTextBox.Focus();
                return false;
            }

            return true;
        }

        private InventoryItem CreateItemFromForm()
        {
            var item = _currentItem ?? new InventoryItem();

            item.ItemCode = _itemCodeTextBox.Text.Trim();
            item.ItemName = _itemNameTextBox.Text.Trim();
            item.Description = string.IsNullOrWhiteSpace(_descriptionTextBox.Text) ? null : _descriptionTextBox.Text.Trim();
            item.Category = _categoryComboBox.SelectedValue != null ? 
                (InventoryCategory)_categoryComboBox.SelectedValue : InventoryCategory.Others;
            item.UnitOfMeasure = _unitOfMeasureTextBox.Text.Trim();
            item.CurrentStock = _currentStockNumeric.Value;
            item.MinimumStock = _minimumStockNumeric.Value;
            item.MaximumStock = _maximumStockNumeric.Value;
            item.ReorderPoint = _reorderPointNumeric.Value;
            item.ReorderQuantity = _reorderQuantityNumeric.Value;
            item.UnitCost = _unitCostNumeric.Value;
            item.SellingPrice = _sellingPriceNumeric.Value;
            item.ExpiryDate = _hasExpiryDateCheckBox.Checked ? _expiryDatePicker.Value : null;
            item.BatchNumber = string.IsNullOrWhiteSpace(_batchNumberTextBox.Text) ? null : _batchNumberTextBox.Text.Trim();
            item.StorageLocation = string.IsNullOrWhiteSpace(_storageLocationTextBox.Text) ? null : _storageLocationTextBox.Text.Trim();
            item.RequiredTemperature = _requiredTemperatureNumeric.Value == 0 ? null : _requiredTemperatureNumeric.Value;
            item.RequiredHumidity = _requiredHumidityNumeric.Value == 0 ? null : _requiredHumidityNumeric.Value;
            item.HasExpiryDate = _hasExpiryDateCheckBox.Checked;
            item.IsPerishable = _isPerishableCheckBox.Checked;
            item.RequiresRefrigeration = _requiresRefrigerationCheckBox.Checked;
            item.IsActive = _isActiveCheckBox.Checked;
            item.Notes = string.IsNullOrWhiteSpace(_notesTextBox.Text) ? null : _notesTextBox.Text.Trim();

            if (_currentItem == null)
            {
                item.CreatedAt = DateTime.Now;
                item.CreatedBy = "النظام"; // يمكن تطوير نظام المستخدمين لاحقاً
            }
            
            item.UpdatedAt = DateTime.Now;
            item.UpdatedBy = "النظام";

            return item;
        }

        // Event Handlers
        private void NewButton_Click(object? sender, EventArgs e)
        {
            ClearForm();
            SetFormMode(true);
            _itemCodeTextBox.Focus();
        }

        private void SaveButton_Click(object? sender, EventArgs e)
        {
            if (!ValidateForm()) return;

            try
            {
                var item = CreateItemFromForm();

                if (_currentItem == null)
                {
                    _context.InventoryItems.Add(item);
                }
                else
                {
                    _context.InventoryItems.Update(item);
                }

                _context.SaveChanges();

                MessageBox.Show("تم حفظ البند بنجاح", "نجح الحفظ", 
                    MessageBoxButtons.OK, MessageBoxIcon.Information);

                LoadInventoryItems();
                SetFormMode(false);
                _currentItem = item;
            }
            catch (Exception ex)
            {
                MessageBox.Show($"خطأ في حفظ البند: {ex.Message}", "خطأ", 
                    MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void EditButton_Click(object? sender, EventArgs e)
        {
            if (_currentItem != null)
            {
                SetFormMode(true);
                _itemCodeTextBox.Focus();
            }
        }

        private void DeleteButton_Click(object? sender, EventArgs e)
        {
            if (_currentItem == null) return;

            var result = MessageBox.Show(
                $"هل أنت متأكد من حذف البند '{_currentItem.ItemName}'؟\nهذا الإجراء لا يمكن التراجع عنه.",
                "تأكيد الحذف",
                MessageBoxButtons.YesNo,
                MessageBoxIcon.Question);

            if (result == DialogResult.Yes)
            {
                try
                {
                    // التحقق من وجود حركات مخزون مرتبطة
                    var hasMovements = _context.StockMovements.Any(sm => sm.InventoryItemId == _currentItem.Id);
                    if (hasMovements)
                    {
                        MessageBox.Show("لا يمكن حذف هذا البند لأنه يحتوي على حركات مخزون", 
                            "خطأ", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                        return;
                    }

                    _context.InventoryItems.Remove(_currentItem);
                    _context.SaveChanges();

                    MessageBox.Show("تم حذف البند بنجاح", "نجح الحذف", 
                        MessageBoxButtons.OK, MessageBoxIcon.Information);

                    ClearForm();
                    LoadInventoryItems();
                    SetFormMode(false);
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"خطأ في حذف البند: {ex.Message}", "خطأ", 
                        MessageBoxButtons.OK, MessageBoxIcon.Error);
                }
            }
        }

        private void CancelButton_Click(object? sender, EventArgs e)
        {
            if (_currentItem != null)
            {
                LoadItemToForm(_currentItem);
            }
            else
            {
                ClearForm();
            }
            
            SetFormMode(false);
        }

        private void ItemsGrid_CellDoubleClick(object? sender, DataGridViewCellEventArgs e)
        {
            if (e.RowIndex >= 0)
            {
                dynamic selectedRow = _itemsGrid.Rows[e.RowIndex].DataBoundItem;
                if (selectedRow?.Item != null)
                {
                    var item = _context.InventoryItems.Find(selectedRow.Item.Id);
                    if (item != null)
                    {
                        LoadItemToForm(item);
                        _mainTabControl.SelectedTab = _itemDetailsTab;
                    }
                }
            }
        }

        private void ExportButton_Click(object? sender, EventArgs e)
        {
            try
            {
                if (_itemsGrid.Rows.Count == 0)
                {
                    MessageBox.Show("لا توجد بيانات ظاهرة للتصدير", "تنبيه", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    return;
                }

                var table = new DataTable("Inventory");
                var columns = _itemsGrid.Columns.Cast<DataGridViewColumn>()
                    .Where(column => column.Visible && column.Name != "Item")
                    .ToList();
                foreach (var column in columns)
                    table.Columns.Add(string.IsNullOrWhiteSpace(column.HeaderText) ? column.Name : column.HeaderText);
                foreach (DataGridViewRow row in _itemsGrid.Rows)
                {
                    if (row.IsNewRow) continue;
                    table.Rows.Add(columns.Select(column => row.Cells[column.Index].Value?.ToString() ?? string.Empty).ToArray());
                }

                var fileName = $"Inventory_{DateTime.Now:yyyyMMdd_HHmmss}.xlsx";
                var path = new ExcelExportService().ExportDataTableToExcel(table, fileName, "المخزون");
                if (MessageBox.Show($"تم تصدير النتائج الظاهرة إلى:\n{path}\n\nفتح الملف؟", "نجح التصدير",
                        MessageBoxButtons.YesNo, MessageBoxIcon.Information) == DialogResult.Yes)
                    new ExcelExportService().OpenExcelFile(path);
            }
            catch (Exception ex)
            {
                LoggingService.LogError(ex, "Error exporting inventory items");
                MessageBox.Show("تعذر تصدير المخزون. راجع السجل الفني.", "خطأ", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        protected override void OnFormClosed(FormClosedEventArgs e)
        {
            _context?.Dispose();
            base.OnFormClosed(e);
        }

        /// <summary>
        /// معالج حدث تعبئة البيانات تلقائياً من الإكمال الذكي
        /// </summary>
        private void ItemNameTextBox_DataFilled(object? sender, AutocompleteResult e)
        {
            try
            {
                if (e.AdditionalData.ContainsKey("Id"))
                {
                    int itemId = Convert.ToInt32(e.AdditionalData["Id"]);
                    
                    // تحميل البند الكامل من قاعدة البيانات
                    var item = _context.InventoryItems.Find(itemId);
                    if (item != null)
                    {
                        _currentItem = item;
                        LoadItemToForm(item);
                    }
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error in ItemNameTextBox_DataFilled: {ex.Message}");
            }
        }
    }

    // Extension methods لعرض القيم
    public static class InventoryCategoryExtensions
    {
        public static string GetCategoryDisplay(this InventoryCategory category)
        {
            return category switch
            {
                InventoryCategory.FishFeed => "أعلاف الأسماك",
                InventoryCategory.Chemicals => "مواد كيميائية",
                InventoryCategory.Medications => "أدوية وعلاجات",
                InventoryCategory.Equipment => "معدات",
                InventoryCategory.SpareParts => "قطع غيار",
                InventoryCategory.OfficeSupplies => "مستلزمات مكتبية",
                InventoryCategory.CleaningSupplies => "مواد تنظيف",
                InventoryCategory.SafetyEquipment => "معدات السلامة",
                InventoryCategory.LaboratorySupplies => "مستلزمات المختبر",
                InventoryCategory.PackagingMaterials => "مواد التعبئة",
                InventoryCategory.FrozenFish => "أسماك مجمدة",
                InventoryCategory.FreshFish => "أسماك طازجة",
                InventoryCategory.Others => "أخرى",
                _ => "غير محدد"
            };
        }
    }
}
