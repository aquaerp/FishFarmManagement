using System;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Windows.Forms;
using Microsoft.EntityFrameworkCore;
using FishFarmManager.Data;
using FishFarmManager.Models;
using ClosedXML.Excel;
using PdfSharpCore.Pdf;
using PdfSharpCore.Drawing;

namespace FishFarmManager.Forms
{
    /// <summary>
    /// نموذج تعديلات المخزون والجرد - إدارة تعديلات الكميات والجرد الفعلي
    /// Stock Adjustment Form - Manage quantity adjustments and physical inventory
    /// </summary>
    public partial class StockAdjustmentForm : Form
    {
        private readonly FishFarmContext _context;
        private StockMovement? _currentAdjustment = null;
        private bool _isEditing = false;

        // Controls
        private TabControl _mainTabControl = null!;
        private TabPage _adjustmentTab = null!;
        private TabPage _historyTab = null!;
        
        // Tab 1: تعديل المخزون
        private ComboBox _inventoryItemComboBox = null!;
        private ComboBox _adjustmentTypeComboBox = null!;
        private TextBox _currentStockTextBox = null!;
        private NumericUpDown _actualQuantityNumeric = null!;
        private NumericUpDown _adjustmentQuantityNumeric = null!;
        private NumericUpDown _unitCostNumeric = null!;
        private ComboBox _reasonComboBox = null!;
        private TextBox _referenceNumberTextBox = null!;
        private TextBox _notesTextBox = null!;
        private DateTimePicker _adjustmentDatePicker = null!;
        private ComboBox _employeeComboBox = null!;
        private CheckBox _requiresApprovalCheckBox = null!;
        private ComboBox _approverComboBox = null!;
        
        // Variance Analysis Controls
        private Label _varianceAmountLabel = null!;
        private Label _variancePercentageLabel = null!;
        private Label _varianceReasonLabel = null!;
        private Label _financialImpactLabel = null!;
        
        // Buttons
        private Button _calculateButton = null!;
        private Button _saveButton = null!;
        private Button _newButton = null!;
        private Button _approveButton = null!;
        private Button _rejectButton = null!;
        private Button _cancelButton = null!;
        
        // Tab 2: تاريخ التعديلات
        private DataGridView _adjustmentsGrid = null!;
        private ComboBox _itemFilterComboBox = null!;
        private ComboBox _typeFilterComboBox = null!;
        private ComboBox _statusFilterComboBox = null!;
        private DateTimePicker _fromDatePicker = null!;
        private DateTimePicker _toDatePicker = null!;
        private TextBox _searchTextBox = null!;
        private Button _refreshButton = null!;
        private Button _exportButton = null!;
    private Button _exportCsvButton = null!;
    private Button _exportPdfButton = null!;

        // Enums for adjustment
        public enum AdjustmentType
        {
            PhysicalCount = 1,      // جرد فعلي
            DamageWrite = 2,        // إتلاف
            Expiry = 3,             // انتهاء صلاحية
            Loss = 4,               // فقدان
            Found = 5,              // عثور على مخزون
            TransferIn = 6,         // تحويل داخلي - استلام
            TransferOut = 7,        // تحويل داخلي - إرسال
            SystemCorrection = 8,   // تصحيح نظام
            Theft = 9,              // سرقة
            QualityReject = 10      // رفض جودة
        }

        public enum AdjustmentReason
        {
            PhysicalInventory = 1,      // جرد فعلي
            SystemError = 2,            // خطأ في النظام
            Damaged = 3,                // تالف
            Expired = 4,                // منتهي الصلاحية
            Stolen = 5,                 // مسروق
            Lost = 6,                   // مفقود
            Found = 7,                  // موجود
            QualityIssue = 8,           // مشكلة جودة
            TransferError = 9,          // خطأ تحويل
            CountingError = 10,         // خطأ عد
            SupplierReturn = 11,        // إرجاع مورد
            CustomerReturn = 12,        // إرجاع عميل
            ManagementDecision = 13     // قرار إداري
        }

        public StockAdjustmentForm(FishFarmContext context)
        {
            _context = context;
            InitializeComponent();
            SetupForm();
            LoadComboBoxes();
            LoadAdjustmentHistory();
            SetFormMode(false);
        }

        private void InitializeComponent()
        {
            this.Size = new Size(1100, 750);
            this.Text = "تعديلات المخزون والجرد - Stock Adjustments & Inventory Count";
            this.StartPosition = FormStartPosition.CenterScreen;
            this.RightToLeft = RightToLeft.Yes;
            this.RightToLeftLayout = true;

            // Main TabControl
            _mainTabControl = new TabControl
            {
                Dock = DockStyle.Fill,
                Font = new Font("Segoe UI", 9F)
            };

            // Tab 1: تعديل المخزون
            _adjustmentTab = new TabPage("تعديل المخزون");
            CreateAdjustmentTab();

            // Tab 2: تاريخ التعديلات
            _historyTab = new TabPage("تاريخ التعديلات");
            CreateHistoryTab();

            _mainTabControl.TabPages.Add(_adjustmentTab);
            _mainTabControl.TabPages.Add(_historyTab);

            this.Controls.Add(_mainTabControl);
        }

        private void CreateAdjustmentTab()
        {
            var panel = new Panel { Dock = DockStyle.Fill, Padding = new Padding(10) };

            // معلومات التعديل الأساسية
            var basicInfoGroup = new GroupBox
            {
                Text = "معلومات التعديل الأساسية",
                Location = new Point(10, 10),
                Size = new Size(520, 180),
                Font = new Font("Segoe UI", 9F, FontStyle.Bold)
            };

            // البند
            var lblInventoryItem = new Label { Text = "البند:", Location = new Point(430, 25), Size = new Size(80, 20) };
            _inventoryItemComboBox = new ComboBox 
            { 
                Location = new Point(250, 25), 
                Size = new Size(170, 20),
                DropDownStyle = ComboBoxStyle.DropDownList
            };

            // نوع التعديل
            var lblAdjustmentType = new Label { Text = "نوع التعديل:", Location = new Point(170, 25), Size = new Size(70, 20) };
            _adjustmentTypeComboBox = new ComboBox 
            { 
                Location = new Point(50, 25), 
                Size = new Size(110, 20),
                DropDownStyle = ComboBoxStyle.DropDownList
            };

            // الرصيد الحالي
            var lblCurrentStock = new Label { Text = "الرصيد الحالي:", Location = new Point(430, 55), Size = new Size(80, 20) };
            _currentStockTextBox = new TextBox 
            { 
                Location = new Point(350, 55), 
                Size = new Size(70, 20),
                ReadOnly = true,
                BackColor = Color.LightGray
            };

            // الكمية الفعلية
            var lblActualQuantity = new Label { Text = "الكمية الفعلية:", Location = new Point(250, 55), Size = new Size(90, 20) };
            _actualQuantityNumeric = new NumericUpDown 
            { 
                Location = new Point(150, 55), 
                Size = new Size(90, 20),
                DecimalPlaces = 3,
                Maximum = 999999999
            };

            // كمية التعديل
            var lblAdjustmentQuantity = new Label { Text = "كمية التعديل:", Location = new Point(60, 55), Size = new Size(80, 20) };
            _adjustmentQuantityNumeric = new NumericUpDown 
            { 
                Location = new Point(10, 55), 
                Size = new Size(80, 20),
                DecimalPlaces = 3,
                Minimum = -999999999,
                Maximum = 999999999,
                ReadOnly = true,
                BackColor = Color.LightBlue
            };

            // تكلفة الوحدة
            var lblUnitCost = new Label { Text = "تكلفة الوحدة:", Location = new Point(430, 85), Size = new Size(80, 20) };
            _unitCostNumeric = new NumericUpDown 
            { 
                Location = new Point(250, 85), 
                Size = new Size(170, 20),
                DecimalPlaces = 2,
                Maximum = 999999999
            };

            // سبب التعديل
            var lblReason = new Label { Text = "سبب التعديل:", Location = new Point(170, 85), Size = new Size(70, 20) };
            _reasonComboBox = new ComboBox 
            { 
                Location = new Point(50, 85), 
                Size = new Size(110, 20),
                DropDownStyle = ComboBoxStyle.DropDownList
            };

            // رقم المرجع
            var lblReferenceNumber = new Label { Text = "رقم المرجع:", Location = new Point(430, 115), Size = new Size(80, 20) };
            _referenceNumberTextBox = new TextBox { Location = new Point(300, 115), Size = new Size(120, 20) };

            // تاريخ التعديل
            var lblAdjustmentDate = new Label { Text = "تاريخ التعديل:", Location = new Point(210, 115), Size = new Size(80, 20) };
            _adjustmentDatePicker = new DateTimePicker 
            { 
                Location = new Point(90, 115), 
                Size = new Size(110, 20),
                Format = DateTimePickerFormat.Short,
                Value = DateTime.Now
            };

            // الموظف المسؤول
            var lblEmployee = new Label { Text = "الموظف المسؤول:", Location = new Point(430, 145), Size = new Size(80, 20) };
            _employeeComboBox = new ComboBox 
            { 
                Location = new Point(250, 145), 
                Size = new Size(170, 20),
                DropDownStyle = ComboBoxStyle.DropDownList
            };

            // ملاحظات
            var lblNotes = new Label { Text = "ملاحظات:", Location = new Point(170, 145), Size = new Size(70, 20) };
            _notesTextBox = new TextBox 
            { 
                Location = new Point(10, 145), 
                Size = new Size(150, 20)
            };

            basicInfoGroup.Controls.AddRange(new Control[] {
                lblInventoryItem, _inventoryItemComboBox,
                lblAdjustmentType, _adjustmentTypeComboBox,
                lblCurrentStock, _currentStockTextBox,
                lblActualQuantity, _actualQuantityNumeric,
                lblAdjustmentQuantity, _adjustmentQuantityNumeric,
                lblUnitCost, _unitCostNumeric,
                lblReason, _reasonComboBox,
                lblReferenceNumber, _referenceNumberTextBox,
                lblAdjustmentDate, _adjustmentDatePicker,
                lblEmployee, _employeeComboBox,
                lblNotes, _notesTextBox
            });

            // تحليل التباين
            var varianceGroup = new GroupBox
            {
                Text = "تحليل التباين والأثر المالي",
                Location = new Point(540, 10),
                Size = new Size(520, 180),
                Font = new Font("Segoe UI", 9F, FontStyle.Bold)
            };

            // مقدار التباين
            var lblVarianceAmount = new Label { Text = "مقدار التباين:", Location = new Point(430, 25), Size = new Size(80, 20) };
            _varianceAmountLabel = new Label 
            { 
                Location = new Point(250, 25), 
                Size = new Size(170, 20),
                ForeColor = Color.Blue,
                Font = new Font("Segoe UI", 9F, FontStyle.Bold),
                BorderStyle = BorderStyle.FixedSingle
            };

            // نسبة التباين
            var lblVariancePercentage = new Label { Text = "نسبة التباين:", Location = new Point(170, 25), Size = new Size(70, 20) };
            _variancePercentageLabel = new Label 
            { 
                Location = new Point(50, 25), 
                Size = new Size(110, 20),
                ForeColor = Color.Green,
                Font = new Font("Segoe UI", 9F, FontStyle.Bold),
                BorderStyle = BorderStyle.FixedSingle
            };

            // تصنيف السبب
            var lblVarianceReason = new Label { Text = "تصنيف السبب:", Location = new Point(430, 55), Size = new Size(80, 20) };
            _varianceReasonLabel = new Label 
            { 
                Location = new Point(250, 55), 
                Size = new Size(170, 20),
                ForeColor = Color.Orange,
                BorderStyle = BorderStyle.FixedSingle
            };

            // الأثر المالي
            var lblFinancialImpact = new Label { Text = "الأثر المالي:", Location = new Point(170, 55), Size = new Size(70, 20) };
            _financialImpactLabel = new Label 
            { 
                Location = new Point(50, 55), 
                Size = new Size(110, 20),
                ForeColor = Color.Red,
                Font = new Font("Segoe UI", 9F, FontStyle.Bold),
                BorderStyle = BorderStyle.FixedSingle
            };

            // زر الحساب
            _calculateButton = new Button 
            { 
                Text = "احسب التباين", 
                Location = new Point(350, 85), 
                Size = new Size(100, 30),
                BackColor = Color.LightBlue
            };

            varianceGroup.Controls.AddRange(new Control[] {
                lblVarianceAmount, _varianceAmountLabel,
                lblVariancePercentage, _variancePercentageLabel,
                lblVarianceReason, _varianceReasonLabel,
                lblFinancialImpact, _financialImpactLabel,
                _calculateButton
            });

            // معلومات الموافقة
            var approvalGroup = new GroupBox
            {
                Text = "معلومات الموافقة",
                Location = new Point(10, 200),
                Size = new Size(520, 120),
                Font = new Font("Segoe UI", 9F, FontStyle.Bold)
            };

            // يتطلب موافقة
            _requiresApprovalCheckBox = new CheckBox 
            { 
                Text = "يتطلب موافقة",
                Location = new Point(400, 25), 
                Size = new Size(100, 20),
                Checked = true
            };

            // المعتمد
            var lblApprover = new Label { Text = "المعتمد:", Location = new Point(300, 25), Size = new Size(60, 20) };
            _approverComboBox = new ComboBox 
            { 
                Location = new Point(150, 25), 
                Size = new Size(140, 20),
                DropDownStyle = ComboBoxStyle.DropDownList
            };

            // أزرار الموافقة
            _approveButton = new Button 
            { 
                Text = "اعتماد", 
                Location = new Point(350, 55), 
                Size = new Size(80, 30),
                BackColor = Color.LightGreen
            };

            _rejectButton = new Button 
            { 
                Text = "رفض", 
                Location = new Point(260, 55), 
                Size = new Size(80, 30),
                BackColor = Color.LightCoral
            };

            // ملاحظة الموافقة
            var lblApprovalNotes = new Label { Text = "ملاحظة الموافقة:", Location = new Point(430, 90), Size = new Size(80, 20) };
            var approvalNotesTextBox = new TextBox 
            { 
                Location = new Point(50, 90), 
                Size = new Size(370, 20)
            };

            approvalGroup.Controls.AddRange(new Control[] {
                _requiresApprovalCheckBox,
                lblApprover, _approverComboBox,
                _approveButton, _rejectButton,
                lblApprovalNotes, approvalNotesTextBox
            });

            // أزرار العمليات
            var buttonsPanel = new Panel
            {
                Location = new Point(540, 200),
                Size = new Size(520, 120),
                BackColor = Color.LightGray
            };

            _newButton = new Button 
            { 
                Text = "جديد", 
                Location = new Point(420, 20), 
                Size = new Size(80, 30),
                BackColor = Color.LightBlue
            };

            _saveButton = new Button 
            { 
                Text = "حفظ", 
                Location = new Point(330, 20), 
                Size = new Size(80, 30),
                BackColor = Color.LightGreen
            };

            _cancelButton = new Button 
            { 
                Text = "إلغاء", 
                Location = new Point(240, 20), 
                Size = new Size(80, 30),
                BackColor = Color.LightGray
            };

            var previewButton = new Button 
            { 
                Text = "معاينة", 
                Location = new Point(150, 20), 
                Size = new Size(80, 30),
                BackColor = Color.LightYellow
            };

            var printButton = new Button 
            { 
                Text = "طباعة", 
                Location = new Point(60, 20), 
                Size = new Size(80, 30),
                BackColor = Color.LightCyan
            };

            buttonsPanel.Controls.AddRange(new Control[] {
                _newButton, _saveButton, _cancelButton, previewButton, printButton
            });

            // Event handlers
            _inventoryItemComboBox.SelectedIndexChanged += InventoryItemComboBox_SelectedIndexChanged;
            _actualQuantityNumeric.ValueChanged += ActualQuantityNumeric_ValueChanged;
            _calculateButton.Click += CalculateButton_Click;
            _newButton.Click += NewButton_Click;
            _saveButton.Click += SaveButton_Click;
            _cancelButton.Click += CancelButton_Click;
            _approveButton.Click += ApproveButton_Click;
            _rejectButton.Click += RejectButton_Click;

            panel.Controls.AddRange(new Control[] {
                basicInfoGroup, varianceGroup, approvalGroup, buttonsPanel
            });

            _adjustmentTab.Controls.Add(panel);
        }

        private void CreateHistoryTab()
        {
            var panel = new Panel { Dock = DockStyle.Fill, Padding = new Padding(10) };

            // شريط البحث والفلترة
            var filterPanel = new Panel
            {
                Dock = DockStyle.Top,
                Height = 100,
                BackColor = Color.LightBlue
            };

            // الصف الأول من الفلاتر
            var lblItemFilter = new Label { Text = "البند:", Location = new Point(950, 15), Size = new Size(50, 20) };
            _itemFilterComboBox = new ComboBox 
            { 
                Location = new Point(800, 15), 
                Size = new Size(140, 20),
                DropDownStyle = ComboBoxStyle.DropDownList
            };

            var lblTypeFilter = new Label { Text = "النوع:", Location = new Point(750, 15), Size = new Size(40, 20) };
            _typeFilterComboBox = new ComboBox 
            { 
                Location = new Point(600, 15), 
                Size = new Size(140, 20),
                DropDownStyle = ComboBoxStyle.DropDownList
            };

            var lblStatusFilter = new Label { Text = "الحالة:", Location = new Point(550, 15), Size = new Size(40, 20) };
            _statusFilterComboBox = new ComboBox 
            { 
                Location = new Point(400, 15), 
                Size = new Size(140, 20),
                DropDownStyle = ComboBoxStyle.DropDownList
            };

            // الصف الثاني من الفلاتر
            var lblFromDate = new Label { Text = "من تاريخ:", Location = new Point(950, 45), Size = new Size(50, 20) };
            _fromDatePicker = new DateTimePicker 
            { 
                Location = new Point(800, 45), 
                Size = new Size(140, 20),
                Format = DateTimePickerFormat.Short,
                Value = DateTime.Now.AddMonths(-1)
            };

            var lblToDate = new Label { Text = "إلى تاريخ:", Location = new Point(750, 45), Size = new Size(50, 20) };
            _toDatePicker = new DateTimePicker 
            { 
                Location = new Point(600, 45), 
                Size = new Size(140, 20),
                Format = DateTimePickerFormat.Short,
                Value = DateTime.Now
            };

            var lblSearch = new Label { Text = "البحث:", Location = new Point(550, 45), Size = new Size(40, 20) };
            _searchTextBox = new TextBox { Location = new Point(400, 45), Size = new Size(140, 20) };

            _refreshButton = new Button 
            { 
                Text = "تحديث", 
                Location = new Point(300, 45), 
                Size = new Size(80, 25),
                BackColor = Color.LightGreen
            };

            _exportButton = new Button 
            { 
                Text = "تصدير", 
                Location = new Point(210, 45), 
                Size = new Size(80, 25),
                BackColor = Color.LightYellow
            };

            var summaryButton = new Button 
            { 
                Text = "ملخص", 
                Location = new Point(120, 45), 
                Size = new Size(80, 25),
                BackColor = Color.LightCyan
            };

            _exportCsvButton = new Button
            {
                Text = "CSV",
                Location = new Point(210, 15),
                Size = new Size(80, 25),
                BackColor = Color.LightYellow
            };

            _exportPdfButton = new Button
            {
                Text = "PDF",
                Location = new Point(120, 15),
                Size = new Size(80, 25),
                BackColor = Color.LightYellow
            };

            var lblResults = new Label 
            { 
                Text = "عدد النتائج: 0",
                Location = new Point(30, 75), 
                Size = new Size(200, 20),
                ForeColor = Color.Blue
            };

            filterPanel.Controls.AddRange(new Control[] {
                lblItemFilter, _itemFilterComboBox,
                lblTypeFilter, _typeFilterComboBox,
                lblStatusFilter, _statusFilterComboBox,
                lblFromDate, _fromDatePicker,
                lblToDate, _toDatePicker,
                lblSearch, _searchTextBox,
                _refreshButton, _exportButton, summaryButton, lblResults,
                _exportCsvButton, _exportPdfButton
            });

            // DataGridView
            _adjustmentsGrid = new DataGridView
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
            _adjustmentsGrid.Columns.AddRange(new DataGridViewColumn[]
            {
                new DataGridViewTextBoxColumn { Name = "MovementDate", HeaderText = "التاريخ", Width = 80 },
                new DataGridViewTextBoxColumn { Name = "ReferenceNumber", HeaderText = "المرجع", Width = 100 },
                new DataGridViewTextBoxColumn { Name = "ItemName", HeaderText = "البند", Width = 150 },
                new DataGridViewTextBoxColumn { Name = "MovementTypeDisplay", HeaderText = "النوع", Width = 120 },
                new DataGridViewTextBoxColumn { Name = "PreviousStock", HeaderText = "الرصيد السابق", Width = 90 },
                new DataGridViewTextBoxColumn { Name = "ActualQuantity", HeaderText = "الكمية الفعلية", Width = 90 },
                new DataGridViewTextBoxColumn { Name = "Quantity", HeaderText = "التعديل", Width = 80 },
                new DataGridViewTextBoxColumn { Name = "NewStock", HeaderText = "الرصيد الجديد", Width = 90 },
                new DataGridViewTextBoxColumn { Name = "UnitPrice", HeaderText = "سعر الوحدة", Width = 80 },
                new DataGridViewTextBoxColumn { Name = "TotalValue", HeaderText = "القيمة الإجمالية", Width = 100 },
                new DataGridViewTextBoxColumn { Name = "EmployeeName", HeaderText = "الموظف", Width = 100 },
                new DataGridViewTextBoxColumn { Name = "ApprovalStatus", HeaderText = "حالة الاعتماد", Width = 100 },
                new DataGridViewTextBoxColumn { Name = "Notes", HeaderText = "ملاحظات", Width = 150 }
            });

            // Event handlers
            _itemFilterComboBox.SelectedIndexChanged += (s, e) => FilterAdjustments();
            _typeFilterComboBox.SelectedIndexChanged += (s, e) => FilterAdjustments();
            _statusFilterComboBox.SelectedIndexChanged += (s, e) => FilterAdjustments();
            _fromDatePicker.ValueChanged += (s, e) => FilterAdjustments();
            _toDatePicker.ValueChanged += (s, e) => FilterAdjustments();
            _searchTextBox.TextChanged += (s, e) => FilterAdjustments();
            _refreshButton.Click += (s, e) => LoadAdjustmentHistory();
            _exportButton.Click += ExportButton_Click;
            _exportCsvButton.Click += ExportCsvButton_Click;
            _exportPdfButton.Click += ExportPdfButton_Click;

            _adjustmentsGrid.CellDoubleClick += AdjustmentsGrid_CellDoubleClick;
            _adjustmentsGrid.DataBindingComplete += (s, e) => 
            {
                lblResults.Text = $"عدد النتائج: {_adjustmentsGrid.Rows.Count}";
                ColorizeAdjustmentGrid();
            };

            panel.Controls.Add(_adjustmentsGrid);
            panel.Controls.Add(filterPanel);

            _historyTab.Controls.Add(panel);
        }

        private void SetupForm()
        {
            // إعداد النموذج الأولي
            _requiresApprovalCheckBox.Checked = true;
        }

        private void LoadComboBoxes()
        {
            try
            {
                // تحميل بنود المخزون
                var inventoryItems = _context.InventoryItems
                    .Where(i => i.IsActive)
                    .OrderBy(i => i.ItemName)
                    .Select(i => new { i.Id, Display = $"{i.ItemCode} - {i.ItemName}", Item = i })
                    .ToList();

                var allItemsEntry = new { Id = 0, Display = "جميع البنود", Item = (InventoryItem?)null };
                
                _inventoryItemComboBox.DataSource = inventoryItems.ToList();
                _inventoryItemComboBox.DisplayMember = "Display";
                _inventoryItemComboBox.ValueMember = "Id";

                // للفلترة
                var filterItems = new List<dynamic> { allItemsEntry };
                filterItems.AddRange(inventoryItems);
                _itemFilterComboBox.DataSource = filterItems;
                _itemFilterComboBox.DisplayMember = "Display";
                _itemFilterComboBox.ValueMember = "Id";

                // تحميل أنواع التعديل
                var adjustmentTypes = Enum.GetValues(typeof(AdjustmentType))
                    .Cast<AdjustmentType>()
                    .Select(t => new { Value = (int)t, Text = GetAdjustmentTypeDisplay(t) })
                    .ToList();

                _adjustmentTypeComboBox.DataSource = adjustmentTypes.ToList();
                _adjustmentTypeComboBox.DisplayMember = "Text";
                _adjustmentTypeComboBox.ValueMember = "Value";

                // للفلترة
                var allTypesEntry = new { Value = 0, Text = "جميع الأنواع" };
                var filterTypes = new List<dynamic> { allTypesEntry };
                filterTypes.AddRange(adjustmentTypes);
                _typeFilterComboBox.DataSource = filterTypes;
                _typeFilterComboBox.DisplayMember = "Text";
                _typeFilterComboBox.ValueMember = "Value";

                // تحميل أسباب التعديل
                var reasons = Enum.GetValues(typeof(AdjustmentReason))
                    .Cast<AdjustmentReason>()
                    .Select(r => new { Value = (int)r, Text = GetAdjustmentReasonDisplay(r) })
                    .ToList();

                _reasonComboBox.DataSource = reasons;
                _reasonComboBox.DisplayMember = "Text";
                _reasonComboBox.ValueMember = "Value";

                // تحميل الموظفين
                var employees = _context.Employees
                    .OrderBy(e => e.FullName)
                    .Select(e => new { e.Id, e.FullName })
                    .ToList();

                _employeeComboBox.DataSource = employees.ToList();
                _employeeComboBox.DisplayMember = "FullName";
                _employeeComboBox.ValueMember = "Id";

                _approverComboBox.DataSource = employees.ToList();
                _approverComboBox.DisplayMember = "FullName";
                _approverComboBox.ValueMember = "Id";

                // حالات الاعتماد للفلترة
                var statusOptions = new List<dynamic>
                {
                    new { Value = "", Text = "جميع الحالات" },
                    new { Value = "Pending", Text = "في انتظار الاعتماد" },
                    new { Value = "Approved", Text = "معتمد" },
                    new { Value = "Rejected", Text = "مرفوض" }
                };

                _statusFilterComboBox.DataSource = statusOptions;
                _statusFilterComboBox.DisplayMember = "Text";
                _statusFilterComboBox.ValueMember = "Value";

            }
            catch (Exception ex)
            {
                MessageBox.Show($"خطأ في تحميل البيانات: {ex.Message}", "خطأ", 
                    MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void LoadAdjustmentHistory()
        {
            try
            {
                var adjustments = _context.StockMovements
                    .Include(sm => sm.InventoryItem)
                    .Where(sm => sm.MovementType == StockMovementType.AdjustmentIncrease ||
                               sm.MovementType == StockMovementType.AdjustmentDecrease ||
                               sm.MovementType == StockMovementType.Waste ||
                               sm.MovementType == StockMovementType.Expired)
                    .OrderByDescending(sm => sm.MovementDate)
                    .Select(sm => new
                    {
                        sm.Id,
                        MovementDate = sm.MovementDate.ToString("yyyy-MM-dd"),
                        sm.ReferenceNumber,
                        ItemName = sm.InventoryItem.ItemName,
                        MovementTypeDisplay = sm.GetMovementTypeDisplay(),
                        PreviousStock = sm.BalanceBefore,
                        ActualQuantity = sm.BalanceAfter,
                        sm.Quantity,
                        NewStock = sm.BalanceAfter,
                        sm.UnitPrice,
                        TotalValue = sm.TotalCost,
                        EmployeeName = "النظام",
                        ApprovalStatus = sm.IsCancelled ? "ملغي" : (sm.IsRejected ? "مرفوض" : (sm.IsApproved ? "معتمد" : "في انتظار الاعتماد")),
                        sm.Notes,
                        Movement = sm
                    })
                    .ToList();

                _adjustmentsGrid.DataSource = adjustments;
            }
            catch (Exception ex)
            {
                MessageBox.Show($"خطأ في تحميل تاريخ التعديلات: {ex.Message}", "خطأ", 
                    MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void FilterAdjustments()
        {
            try
            {
                var query = _context.StockMovements
                    .Include(sm => sm.InventoryItem)
                    .Where(sm => sm.MovementType == StockMovementType.AdjustmentIncrease ||
                               sm.MovementType == StockMovementType.AdjustmentDecrease ||
                               sm.MovementType == StockMovementType.Waste ||
                               sm.MovementType == StockMovementType.Expired)
                    .AsQueryable();

                // فلترة البند
                if (_itemFilterComboBox.SelectedValue is int itemId && itemId > 0)
                {
                    query = query.Where(sm => sm.InventoryItemId == itemId);
                }

                // فلترة النوع
                if (_typeFilterComboBox.SelectedValue is int typeValue && typeValue > 0)
                {
                    var movementType = (StockMovementType)typeValue;
                    query = query.Where(sm => sm.MovementType == movementType);
                }

                // فلترة الحالة
                if (_statusFilterComboBox.SelectedValue is string status && !string.IsNullOrEmpty(status))
                {
                    if (status == "Approved")
                        query = query.Where(sm => sm.IsApproved);
                    else if (status == "Pending")
                        query = query.Where(sm => !sm.IsApproved);
                    else if (status == "Rejected")
                        query = query.Where(sm => sm.IsRejected);
                    else if (status == "Cancelled")
                        query = query.Where(sm => sm.IsCancelled);
                }

                // فلترة التاريخ
                query = query.Where(sm => sm.MovementDate >= _fromDatePicker.Value.Date &&
                                        sm.MovementDate <= _toDatePicker.Value.Date.AddDays(1));

                // البحث النصي
                if (!string.IsNullOrWhiteSpace(_searchTextBox.Text))
                {
                    var searchText = _searchTextBox.Text.Trim().ToLower();
                    query = query.Where(sm => 
                        (sm.ReferenceNumber != null && sm.ReferenceNumber.ToLower().Contains(searchText)) ||
                        (sm.InventoryItem != null && sm.InventoryItem.ItemName != null && sm.InventoryItem.ItemName.ToLower().Contains(searchText)) ||
                        (sm.Notes != null && sm.Notes.ToLower().Contains(searchText)));
                }

                var adjustments = query
                    .OrderByDescending(sm => sm.MovementDate)
                    .Select(sm => new
                    {
                        sm.Id,
                        MovementDate = sm.MovementDate.ToString("yyyy-MM-dd"),
                        sm.ReferenceNumber,
                        ItemName = sm.InventoryItem.ItemName,
                        MovementTypeDisplay = sm.GetMovementTypeDisplay(),
                        PreviousStock = sm.BalanceBefore,
                        ActualQuantity = sm.BalanceAfter,
                        sm.Quantity,
                        NewStock = sm.BalanceAfter,
                        sm.UnitPrice,
                        TotalValue = sm.TotalCost,
                        EmployeeName = "النظام",
                        ApprovalStatus = sm.IsCancelled ? "ملغي" : (sm.IsRejected ? "مرفوض" : (sm.IsApproved ? "معتمد" : "في انتظار الاعتماد")),
                        sm.Notes,
                        Movement = sm
                    })
                    .ToList();

                _adjustmentsGrid.DataSource = adjustments;
            }
            catch (Exception ex)
            {
                MessageBox.Show($"خطأ في فلترة البيانات: {ex.Message}", "خطأ", 
                    MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void ColorizeAdjustmentGrid()
        {
            foreach (DataGridViewRow row in _adjustmentsGrid.Rows)
            {
                if (row.DataBoundItem != null)
                {
                    dynamic adjustment = row.DataBoundItem;
                    
                    // تلوين الصفوف حسب حالة الاعتماد
                    if (adjustment.ApprovalStatus == "في انتظار الاعتماد")
                    {
                        row.DefaultCellStyle.BackColor = Color.LightYellow;
                    }
                    else if (adjustment.ApprovalStatus == "مرفوض")
                    {
                        row.DefaultCellStyle.BackColor = Color.LightCoral;
                    }
                    else if (adjustment.ApprovalStatus == "معتمد")
                    {
                        row.DefaultCellStyle.BackColor = Color.LightGreen;
                    }

                    // تلوين كمية التعديل حسب النوع
                    if (adjustment.Quantity > 0)
                    {
                        row.Cells["Quantity"].Style.ForeColor = Color.Green;
                    }
                    else if (adjustment.Quantity < 0)
                    {
                        row.Cells["Quantity"].Style.ForeColor = Color.Red;
                    }
                }
            }
        }

        private void SetFormMode(bool isEditing)
        {
            _isEditing = isEditing;
            
            _inventoryItemComboBox.Enabled = isEditing;
            _adjustmentTypeComboBox.Enabled = isEditing;
            _actualQuantityNumeric.Enabled = isEditing;
            _unitCostNumeric.Enabled = isEditing;
            _reasonComboBox.Enabled = isEditing;
            _referenceNumberTextBox.Enabled = isEditing;
            _adjustmentDatePicker.Enabled = isEditing;
            _employeeComboBox.Enabled = isEditing;
            _notesTextBox.Enabled = isEditing;
            _requiresApprovalCheckBox.Enabled = isEditing;
            _approverComboBox.Enabled = isEditing;

            _saveButton.Enabled = isEditing;
            _cancelButton.Enabled = isEditing;
            _newButton.Enabled = !isEditing;
            _calculateButton.Enabled = isEditing;
            
            _approveButton.Enabled = !isEditing && _currentAdjustment != null && !_currentAdjustment.IsApproved;
            _rejectButton.Enabled = !isEditing && _currentAdjustment != null && !_currentAdjustment.IsApproved;
        }

        // Event Handlers
        private void InventoryItemComboBox_SelectedIndexChanged(object? sender, EventArgs e)
        {
            if (_inventoryItemComboBox.SelectedValue is int itemId && itemId > 0)
            {
                var item = _context.InventoryItems.Find(itemId);
                if (item != null)
                {
                    _currentStockTextBox.Text = item.CurrentStock.ToString("N3");
                    _unitCostNumeric.Value = item.UnitCost;
                    
                    // إعداد رقم المرجع التلقائي
                    if (string.IsNullOrWhiteSpace(_referenceNumberTextBox.Text))
                    {
                        _referenceNumberTextBox.Text = GenerateReferenceNumber();
                    }
                }
            }
        }

        private void ActualQuantityNumeric_ValueChanged(object? sender, EventArgs e)
        {
            CalculateAdjustment();
        }

        private void CalculateButton_Click(object? sender, EventArgs e)
        {
            CalculateAdjustment();
        }

        private void CalculateAdjustment()
        {
            try
            {
                if (_inventoryItemComboBox.SelectedValue is int itemId && itemId > 0)
                {
                    var item = _context.InventoryItems.Find(itemId);
                    if (item != null)
                    {
                        decimal currentStock = item.CurrentStock;
                        decimal actualQuantity = _actualQuantityNumeric.Value;
                        decimal adjustmentQuantity = actualQuantity - currentStock;

                        _adjustmentQuantityNumeric.Value = adjustmentQuantity;

                        // حساب التباين
                        _varianceAmountLabel.Text = $"{Math.Abs(adjustmentQuantity):N3}";
                        
                        if (currentStock > 0)
                        {
                            decimal variancePercentage = (Math.Abs(adjustmentQuantity) / currentStock) * 100;
                            _variancePercentageLabel.Text = $"{variancePercentage:F2}%";
                        }
                        else
                        {
                            _variancePercentageLabel.Text = "N/A";
                        }

                        // الأثر المالي
                        decimal financialImpact = adjustmentQuantity * _unitCostNumeric.Value;
                        _financialImpactLabel.Text = $"{financialImpact:N2} ريال";
                        _financialImpactLabel.ForeColor = financialImpact >= 0 ? Color.Green : Color.Red;

                        // تصنيف السبب
                        if (adjustmentQuantity > 0)
                        {
                            _varianceReasonLabel.Text = "زيادة في المخزون";
                            _varianceReasonLabel.ForeColor = Color.Green;
                        }
                        else if (adjustmentQuantity < 0)
                        {
                            _varianceReasonLabel.Text = "نقص في المخزون";
                            _varianceReasonLabel.ForeColor = Color.Red;
                        }
                        else
                        {
                            _varianceReasonLabel.Text = "لا يوجد تباين";
                            _varianceReasonLabel.ForeColor = Color.Gray;
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"خطأ في حساب التعديل: {ex.Message}", "خطأ", 
                    MessageBoxButtons.OK, MessageBoxIcon.Warning);
            }
        }

        private string GenerateReferenceNumber()
        {
            var date = DateTime.Now;
            var sequence = _context.StockMovements
                .Where(sm => sm.MovementDate.Date == date.Date)
                .Count() + 1;
            
            return $"ADJ-{date:yyyyMMdd}-{sequence:D3}";
        }

        private void NewButton_Click(object? sender, EventArgs e)
        {
            ClearForm();
            SetFormMode(true);
            _inventoryItemComboBox.Focus();
        }

        private void SaveButton_Click(object? sender, EventArgs e)
        {
            if (!ValidateForm()) return;

            try
            {
                var adjustment = CreateAdjustmentFromForm();

                _context.StockMovements.Add(adjustment);

                // تحديث رصيد البند: إذا لا يتطلب موافقة، حدّث مباشرة إلى الرصيد الجديد
                if (!_requiresApprovalCheckBox.Checked && _inventoryItemComboBox.SelectedValue is int itemId)
                {
                    var item = _context.InventoryItems.Find(itemId);
                    if (item != null)
                    {
                        item.CurrentStock = adjustment.BalanceAfter;
                        item.UpdatedAt = DateTime.Now;
                        item.UpdatedBy = "النظام";
                    }
                }

                _context.SaveChanges();

                MessageBox.Show("تم حفظ التعديل بنجاح", "نجح الحفظ", 
                    MessageBoxButtons.OK, MessageBoxIcon.Information);

                LoadAdjustmentHistory();
                SetFormMode(false);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"خطأ في حفظ التعديل: {ex.Message}", "خطأ", 
                    MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void CancelButton_Click(object? sender, EventArgs e)
        {
            ClearForm();
            SetFormMode(false);
        }

        private void ApproveButton_Click(object? sender, EventArgs e)
        {
            if (_currentAdjustment == null) return;

            try
            {
                _currentAdjustment.IsApproved = true;
                _currentAdjustment.ApprovedAt = DateTime.Now;
                var employeeId = _approverComboBox.SelectedValue is int empId ? empId : (int?)null;
                _currentAdjustment.ApprovedBy = employeeId.HasValue ? _context.Employees.Find(employeeId.Value) : null;

                // مزامنة رصيد البند بعد الاعتماد
                var item = _context.InventoryItems.Find(_currentAdjustment.InventoryItemId);
                if (item != null)
                {
                    item.CurrentStock = _currentAdjustment.BalanceAfter;
                    item.UpdatedAt = DateTime.Now;
                    item.UpdatedBy = _approverComboBox.Text;
                }

                _context.SaveChanges();

                MessageBox.Show("تم اعتماد التعديل بنجاح", "تم الاعتماد", 
                    MessageBoxButtons.OK, MessageBoxIcon.Information);

                LoadAdjustmentHistory();
                SetFormMode(false);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"خطأ في اعتماد التعديل: {ex.Message}", "خطأ", 
                    MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void RejectButton_Click(object? sender, EventArgs e)
        {
            if (_currentAdjustment == null) return;

            var reason = Microsoft.VisualBasic.Interaction.InputBox(
                "يرجى إدخال سبب الرفض:",
                "سبب رفض التعديل",
                "");

            if (!string.IsNullOrWhiteSpace(reason))
            {
                try
                {
                    var approverId = _approverComboBox.SelectedValue is int empId ? empId : 0;
                    _currentAdjustment.Reject(reason, approverId);
                    _context.SaveChanges();

                    MessageBox.Show("تم رفض التعديل", "تم الرفض", 
                        MessageBoxButtons.OK, MessageBoxIcon.Information);

                    LoadAdjustmentHistory();
                    SetFormMode(false);
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"خطأ في رفض التعديل: {ex.Message}", "خطأ", 
                        MessageBoxButtons.OK, MessageBoxIcon.Error);
                }
            }
        }

        private void AdjustmentsGrid_CellDoubleClick(object? sender, DataGridViewCellEventArgs e)
        {
            if (e.RowIndex >= 0)
            {
                dynamic selectedRow = _adjustmentsGrid.Rows[e.RowIndex].DataBoundItem;
                if (selectedRow?.Movement != null)
                {
                    var adjustment = _context.StockMovements.Find(selectedRow.Movement.Id);
                    if (adjustment != null)
                    {
                        LoadAdjustmentToForm(adjustment);
                        _mainTabControl.SelectedTab = _adjustmentTab;
                    }
                }
            }
        }

        private void ExportButton_Click(object? sender, EventArgs e)
        {
            try
            {
                if (_adjustmentsGrid.Rows.Count == 0)
                {
                    MessageBox.Show("لا توجد بيانات للتصدير", "معلومات", MessageBoxButtons.OK, MessageBoxIcon.Information);
                    return;
                }

                using var sfd = new SaveFileDialog
                {
                    Filter = "Excel Files (*.xlsx)|*.xlsx",
                    FileName = $"StockAdjustments_{DateTime.Now:yyyyMMddHHmm}.xlsx"
                };

                if (sfd.ShowDialog() != DialogResult.OK) return;

                var dt = BuildDataTableFromGrid(_adjustmentsGrid);
                using var wb = new XLWorkbook();
                var ws = wb.Worksheets.Add("التعديلات");
                ws.Cell(1, 1).InsertTable(dt, "Adjustments", true);
                ws.Columns().AdjustToContents();
                wb.SaveAs(sfd.FileName);

                MessageBox.Show("تم تصدير البيانات إلى Excel بنجاح", "تم", MessageBoxButtons.OK, MessageBoxIcon.Information);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"خطأ في التصدير: {ex.Message}", "خطأ", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private static DataTable BuildDataTableFromGrid(DataGridView grid)
        {
            var dt = new DataTable();
            foreach (DataGridViewColumn col in grid.Columns)
            {
                if (col.Visible)
                    dt.Columns.Add(col.HeaderText);
            }

            foreach (DataGridViewRow row in grid.Rows)
            {
                if (row.IsNewRow) continue;
                var values = new System.Collections.Generic.List<object?>();
                foreach (DataGridViewColumn col in grid.Columns)
                {
                    if (col.Visible)
                        values.Add(row.Cells[col.Index].Value);
                }
                dt.Rows.Add(values.ToArray());
            }
            return dt;
        }

        private void ExportCsvButton_Click(object? sender, EventArgs e)
        {
            try
            {
                if (_adjustmentsGrid.Rows.Count == 0)
                {
                    MessageBox.Show("لا توجد بيانات للتصدير", "معلومات", MessageBoxButtons.OK, MessageBoxIcon.Information);
                    return;
                }

                using var sfd = new SaveFileDialog
                {
                    Filter = "CSV Files (*.csv)|*.csv",
                    FileName = $"StockAdjustments_{DateTime.Now:yyyyMMddHHmm}.csv"
                };
                if (sfd.ShowDialog() != DialogResult.OK) return;

                using var sw = new StreamWriter(sfd.FileName, false, System.Text.Encoding.UTF8);
                // Headers
                var headers = new System.Collections.Generic.List<string>();
                foreach (DataGridViewColumn col in _adjustmentsGrid.Columns)
                {
                    if (col.Visible) headers.Add(EscapeCsv(col.HeaderText));
                }
                sw.WriteLine(string.Join(",", headers));

                // Rows
                foreach (DataGridViewRow row in _adjustmentsGrid.Rows)
                {
                    if (row.IsNewRow) continue;
                    var cells = new System.Collections.Generic.List<string>();
                    foreach (DataGridViewColumn col in _adjustmentsGrid.Columns)
                    {
                        if (!col.Visible) continue;
                        var val = row.Cells[col.Index].Value?.ToString() ?? string.Empty;
                        cells.Add(EscapeCsv(val));
                    }
                    sw.WriteLine(string.Join(",", cells));
                }

                MessageBox.Show("تم تصدير البيانات إلى CSV بنجاح", "تم", MessageBoxButtons.OK, MessageBoxIcon.Information);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"خطأ في التصدير: {ex.Message}", "خطأ", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private static string EscapeCsv(string input)
        {
            if (input.Contains('"') || input.Contains(',') || input.Contains('\n') || input.Contains('\r'))
            {
                return '"' + input.Replace("\"", "\"\"") + '"';
            }
            return input;
        }

        private void ExportPdfButton_Click(object? sender, EventArgs e)
        {
            try
            {
                if (_adjustmentsGrid.Rows.Count == 0)
                {
                    MessageBox.Show("لا توجد بيانات للتصدير", "معلومات", MessageBoxButtons.OK, MessageBoxIcon.Information);
                    return;
                }

                using var sfd = new SaveFileDialog
                {
                    Filter = "PDF Files (*.pdf)|*.pdf",
                    FileName = $"StockAdjustments_{DateTime.Now:yyyyMMddHHmm}.pdf"
                };
                if (sfd.ShowDialog() != DialogResult.OK) return;

                ExportGridToPdf(_adjustmentsGrid, sfd.FileName, "تاريخ تعديلات المخزون");
                MessageBox.Show("تم تصدير البيانات إلى PDF بنجاح", "تم", MessageBoxButtons.OK, MessageBoxIcon.Information);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"خطأ في التصدير: {ex.Message}", "خطأ", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private static void ExportGridToPdf(DataGridView grid, string filePath, string title)
        {
            int originalHeight = grid.Height;
            int totalRowHeight = grid.ColumnHeadersHeight;
            foreach (DataGridViewRow r in grid.Rows) totalRowHeight += r.Height;
            int bmpHeight = Math.Min(totalRowHeight + 2, 20000);
            int bmpWidth = grid.Width;

            using var bmp = new Bitmap(bmpWidth, bmpHeight);
            grid.Height = bmpHeight;
            try
            {
                grid.DrawToBitmap(bmp, new Rectangle(0, 0, bmpWidth, bmpHeight));
            }
            finally
            {
                grid.Height = originalHeight;
            }

            using var ms = new MemoryStream();
            bmp.Save(ms, System.Drawing.Imaging.ImageFormat.Png);
            ms.Position = 0;

            var doc = new PdfDocument();
            var page = doc.AddPage();
            var gfx = XGraphics.FromPdfPage(page);

            double margin = 36;
            var pageWidth = page.Width - 2 * margin;
            var pageHeight = page.Height - 2 * margin;

            var font = new XFont("Tahoma", 12, XFontStyle.Bold);
            gfx.DrawString(title, font, XBrushes.Black, new XRect(margin, margin, pageWidth, 20), XStringFormats.TopRight);

            var img = XImage.FromStream(() => new MemoryStream(ms.ToArray()));

            double scale = Math.Min(pageWidth / img.PixelWidth * 72.0 / img.HorizontalResolution,
                                    (pageHeight - 24) / img.PixelHeight * 72.0 / img.VerticalResolution);
            if (double.IsInfinity(scale) || scale <= 0) scale = 1.0;

            double imgWidth = img.PixelWidth * 72.0 / img.HorizontalResolution * scale;
            double imgHeight = img.PixelHeight * 72.0 / img.VerticalResolution * scale;
            double x = margin;
            double y = margin;
        }

        private void LoadAdjustmentToForm(StockMovement adjustment)
        {
            _currentAdjustment = adjustment;
            
            _inventoryItemComboBox.SelectedValue = adjustment.InventoryItemId;
            _adjustmentDatePicker.Value = adjustment.MovementDate;
            _actualQuantityNumeric.Value = adjustment.BalanceAfter;
            _adjustmentQuantityNumeric.Value = adjustment.Quantity;
            _unitCostNumeric.Value = adjustment.UnitPrice;
            _referenceNumberTextBox.Text = adjustment.ReferenceNumber;
            _notesTextBox.Text = adjustment.Notes ?? string.Empty;
            _adjustmentDatePicker.Value = adjustment.MovementDate;
            _requiresApprovalCheckBox.Checked = !adjustment.IsApproved;

            CalculateAdjustment();
        }

        private bool ValidateForm()
        {
            if (_inventoryItemComboBox.SelectedValue == null)
            {
                MessageBox.Show("يرجى اختيار صنف المخزون", "تحذير", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                _inventoryItemComboBox.Focus();
                return false;
            }

            if (_adjustmentQuantityNumeric.Value == 0)
            {
                MessageBox.Show("يرجى إدخال كمية التعديل", "تحذير", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                _adjustmentQuantityNumeric.Focus();
                return false;
            }

            return true;
        }

        private StockMovement CreateAdjustmentFromForm()
        {
            var itemId = (int)_inventoryItemComboBox.SelectedValue!;
            var item = _context.InventoryItems.Find(itemId);
            
            var adjustment = new StockMovement
            {
                InventoryItemId = itemId,
                MovementDate = _adjustmentDatePicker.Value,
                MovementType = _adjustmentQuantityNumeric.Value > 0 
                    ? StockMovementType.AdjustmentIncrease 
                    : StockMovementType.AdjustmentDecrease,
                Quantity = Math.Abs(_adjustmentQuantityNumeric.Value),
                UnitPrice = _unitCostNumeric.Value,
                TotalAmount = Math.Abs(_adjustmentQuantityNumeric.Value) * _unitCostNumeric.Value,
                BalanceBefore = item?.CurrentStock ?? 0,
                BalanceAfter = (item?.CurrentStock ?? 0) + _adjustmentQuantityNumeric.Value,
                ReferenceNumber = _referenceNumberTextBox.Text,
                Notes = _notesTextBox.Text,
                IsApproved = !_requiresApprovalCheckBox.Checked,
                CreatedDate = DateTime.Now,
                CreatedBy = "النظام"
            };

            return adjustment;
        }

        private void ClearForm()
        {
            _currentAdjustment = null;
            
            _inventoryItemComboBox.SelectedIndex = -1;
            _adjustmentTypeComboBox.SelectedIndex = 0;
            _currentStockTextBox.Clear();
            _actualQuantityNumeric.Value = 0;
            _adjustmentQuantityNumeric.Value = 0;
            _unitCostNumeric.Value = 0;
            _reasonComboBox.SelectedIndex = 0;
            _referenceNumberTextBox.Clear();
            _notesTextBox.Clear();
            _adjustmentDatePicker.Value = DateTime.Now;
            _employeeComboBox.SelectedIndex = -1;
            _requiresApprovalCheckBox.Checked = true;
            _approverComboBox.SelectedIndex = -1;

            // مسح تحليل التباين
            _varianceAmountLabel.Text = "";
            _variancePercentageLabel.Text = "";
            _varianceReasonLabel.Text = "";
            _financialImpactLabel.Text = "";
        }

        // Helper methods
        private string GetAdjustmentTypeDisplay(AdjustmentType type)
        {
            return type switch
            {
                AdjustmentType.PhysicalCount => "جرد فعلي",
                AdjustmentType.DamageWrite => "إتلاف",
                AdjustmentType.Expiry => "انتهاء صلاحية",
                AdjustmentType.Loss => "فقدان",
                AdjustmentType.Found => "عثور على مخزون",
                AdjustmentType.TransferIn => "تحويل داخلي - استلام",
                AdjustmentType.TransferOut => "تحويل داخلي - إرسال",
                AdjustmentType.SystemCorrection => "تصحيح نظام",
                AdjustmentType.Theft => "سرقة",
                AdjustmentType.QualityReject => "رفض جودة",
                _ => "غير محدد"
            };
        }

        private string GetAdjustmentReasonDisplay(AdjustmentReason reason)
        {
            return reason switch
            {
                AdjustmentReason.PhysicalInventory => "جرد فعلي",
                AdjustmentReason.SystemError => "خطأ في النظام",
                AdjustmentReason.Damaged => "تالف",
                AdjustmentReason.Expired => "منتهي الصلاحية",
                AdjustmentReason.Stolen => "مسروق",
                AdjustmentReason.Lost => "مفقود",
                AdjustmentReason.Found => "موجود",
                AdjustmentReason.QualityIssue => "مشكلة جودة",
                AdjustmentReason.TransferError => "خطأ تحويل",
                AdjustmentReason.CountingError => "خطأ عد",
                AdjustmentReason.SupplierReturn => "إرجاع مورد",
                AdjustmentReason.CustomerReturn => "إرجاع عميل",
                AdjustmentReason.ManagementDecision => "قرار إداري",
                _ => "غير محدد"
            };
        }

        protected override void OnFormClosed(FormClosedEventArgs e)
        {
            _context?.Dispose();
            base.OnFormClosed(e);
        }
    }
}