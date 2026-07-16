using System;
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
    /// نموذج إدارة الأصول الثابتة
    /// Fixed Assets Management Form
    /// </summary>
    public partial class FixedAssetForm : Form
    {
        #region Fields

        private readonly FishFarmContext _context;
        private int? _currentAssetId;

        // UI Controls
        private TextBox _assetNumberTextBox = null!;
        private TextBox _assetNameTextBox = null!;
        private TextBox _descriptionTextBox = null!;
        private ComboBox _categoryComboBox = null!;
        private DateTimePicker _purchaseDatePicker = null!;
        private NumericUpDown _purchaseCostNumeric = null!;
        private NumericUpDown _residualValueNumeric = null!;
        private NumericUpDown _usefulLifeNumeric = null!;
        private ComboBox _depreciationMethodComboBox = null!;
        private NumericUpDown _depreciationRateNumeric = null!;
        private TextBox _locationTextBox = null!;
        private ComboBox _departmentComboBox = null!;
        private ComboBox _responsibleEmployeeComboBox = null!;
        private ComboBox _supplierComboBox = null!;
        private TextBox _serialNumberTextBox = null!;
        private ComboBox _statusComboBox = null!;
        private Label _bookValueLabel = null!;
        private Label _accumulatedDepreciationLabel = null!;
        private Label _ageLabel = null!;
        private DataGridView _assetsGrid = null!;

        // Buttons
        private Button _addButton = null!;
        private Button _updateButton = null!;
        private Button _deleteButton = null!;
        private Button _clearButton = null!;
        private Button _calculateDepreciationButton = null!;

        #endregion

        #region Constructor

        public FixedAssetForm(FishFarmContext context)
        {
            _context = context ?? throw new ArgumentNullException(nameof(context));

            if (!AuthenticationService.HasPermission(UserRole.Admin, UserRole.Manager, UserRole.Accountant))
            {
                MessageBox.Show("ليس لديك صلاحية لإدارة الأصول الثابتة", "تحذير",
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
            this.Text = "إدارة الأصول الثابتة";
            this.Size = new Size(1500, 900);
            this.StartPosition = FormStartPosition.CenterScreen;
            this.RightToLeft = RightToLeft.Yes;
            this.RightToLeftLayout = true;
            this.Font = new Font("Cairo", 10F);
            this.WindowState = FormWindowState.Maximized;

            var mainPanel = new Panel
            {
                Dock = DockStyle.Fill,
                Padding = new Padding(20),
                BackColor = Color.FromArgb(240, 240, 240),
                AutoScroll = true
            };

            var titleLabel = new Label
            {
                Text = "نظام إدارة الأصول الثابتة",
                Location = new Point(20, 10),
                Size = new Size(1440, 40),
                Font = new Font("Cairo", 14F, FontStyle.Bold),
                ForeColor = Color.FromArgb(46, 92, 138),
                TextAlign = ContentAlignment.MiddleRight
            };
            mainPanel.Controls.Add(titleLabel);

            CreateInputSection(mainPanel, 60);
            CreateCalculatedValuesSection(mainPanel, 460);
            CreateGridSection(mainPanel, 540);
            CreateButtonsSection(mainPanel);

            this.Controls.Add(mainPanel);
        }

        private void CreateInputSection(Panel parent, int startY)
        {
            var inputPanel = new GroupBox
            {
                Text = "بيانات الأصل",
                Location = new Point(20, startY),
                Size = new Size(1440, 380),
                Font = new Font("Cairo", 10F, FontStyle.Bold)
            };

            int col1X = 1050;
            int col2X = 520;
            int labelWidth = 140;
            int controlWidth = 280;
            int y = 35;
            int spacing = 42;

            // Column 1
            CreateLabelAndControl(inputPanel, "رقم الأصل:", ref _assetNumberTextBox, col1X, y, labelWidth, controlWidth, true);
            y += spacing;

            CreateLabelAndControl(inputPanel, "اسم الأصل:", ref _assetNameTextBox, col1X, y, labelWidth, controlWidth);
            y += spacing;

            CreateLabelAndCombo(inputPanel, "الفئة:", ref _categoryComboBox, col1X, y, labelWidth, controlWidth);
            LoadCategories();
            y += spacing;

            CreateLabelAndDatePicker(inputPanel, "تاريخ الشراء:", ref _purchaseDatePicker, col1X, y, labelWidth, controlWidth);
            y += spacing;

            CreateLabelAndNumeric(inputPanel, "تكلفة الشراء:", ref _purchaseCostNumeric, col1X, y, labelWidth, controlWidth, 0, 999999999, 2);
            y += spacing;

            CreateLabelAndNumeric(inputPanel, "القيمة المتبقية:", ref _residualValueNumeric, col1X, y, labelWidth, controlWidth, 0, 999999999, 2);
            y += spacing;

            CreateLabelAndNumeric(inputPanel, "العمر الافتراضي (سنوات):", ref _usefulLifeNumeric, col1X, y, labelWidth, controlWidth, 1, 100, 0);
            y += spacing;

            CreateLabelAndCombo(inputPanel, "طريقة الإهلاك:", ref _depreciationMethodComboBox, col1X, y, labelWidth, controlWidth);
            LoadDepreciationMethods();
            y += spacing;

            CreateLabelAndNumeric(inputPanel, "نسبة الإهلاك %:", ref _depreciationRateNumeric, col1X, y, labelWidth, controlWidth, 0, 100, 2);

            // Column 2
            y = 35;

            CreateLabelAndControl(inputPanel, "الموقع:", ref _locationTextBox, col2X, y, labelWidth, controlWidth);
            y += spacing;

            CreateLabelAndCombo(inputPanel, "القسم:", ref _departmentComboBox, col2X, y, labelWidth, controlWidth);
            LoadDepartments();
            y += spacing;

            CreateLabelAndCombo(inputPanel, "الموظف المسؤول:", ref _responsibleEmployeeComboBox, col2X, y, labelWidth, controlWidth);
            y += spacing;

            CreateLabelAndCombo(inputPanel, "المورد:", ref _supplierComboBox, col2X, y, labelWidth, controlWidth);
            y += spacing;

            CreateLabelAndControl(inputPanel, "الرقم التسلسلي:", ref _serialNumberTextBox, col2X, y, labelWidth, controlWidth);
            y += spacing;

            CreateLabelAndCombo(inputPanel, "الحالة:", ref _statusComboBox, col2X, y, labelWidth, controlWidth);
            LoadAssetStatuses();
            y += spacing;

            var descLabel = new Label { Text = "الوصف:", Location = new Point(col2X + controlWidth, y), Size = new Size(labelWidth, 25), TextAlign = ContentAlignment.MiddleRight };
            inputPanel.Controls.Add(descLabel);

            _descriptionTextBox = new TextBox
            {
                Location = new Point(col2X, y),
                Size = new Size(controlWidth, 60),
                Multiline = true,
                ScrollBars = ScrollBars.Vertical,
                Font = new Font("Cairo", 9F)
            };
            inputPanel.Controls.Add(_descriptionTextBox);

            parent.Controls.Add(inputPanel);
        }

        private void CreateCalculatedValuesSection(Panel parent, int startY)
        {
            var calcPanel = new GroupBox
            {
                Text = "القيم المحسوبة",
                Location = new Point(20, startY),
                Size = new Size(1440, 70),
                Font = new Font("Cairo", 10F, FontStyle.Bold)
            };

            int x = 1100;

            CreateValueLabel(calcPanel, "القيمة الدفترية:", ref _bookValueLabel, x, 30);
            x -= 350;
            CreateValueLabel(calcPanel, "الإهلاك المتراكم:", ref _accumulatedDepreciationLabel, x, 30);
            _accumulatedDepreciationLabel.ForeColor = Color.FromArgb(229, 57, 53);
            x -= 350;
            CreateValueLabel(calcPanel, "عمر الأصل:", ref _ageLabel, x, 30);

            parent.Controls.Add(calcPanel);
        }

        private void CreateGridSection(Panel parent, int startY)
        {
            _assetsGrid = new DataGridView
            {
                Location = new Point(20, startY),
                Size = new Size(1440, 260),
                Font = new Font("Cairo", 9F),
                AllowUserToAddRows = false,
                AllowUserToDeleteRows = false,
                ReadOnly = true,
                SelectionMode = DataGridViewSelectionMode.FullRowSelect,
                AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill,
                BackgroundColor = Color.White
            };

            _assetsGrid.CellDoubleClick += Grid_CellDoubleClick;

            parent.Controls.Add(_assetsGrid);
        }

        private void CreateButtonsSection(Panel parent)
        {
            var buttonPanel = new Panel { Location = new Point(20, 810), Size = new Size(1440, 50), BackColor = Color.Transparent };

            int x = 1200;
            int buttonWidth = 120;
            int spacing = 10;

            _addButton = CreateButton("إضافة", x, 10, buttonWidth);
            _addButton.Click += async (s, e) => await AddButton_ClickAsync();
            buttonPanel.Controls.Add(_addButton);

            x -= (buttonWidth + spacing);

            _updateButton = CreateButton("تعديل", x, 10, buttonWidth);
            _updateButton.Click += async (s, e) => await UpdateButton_ClickAsync();
            buttonPanel.Controls.Add(_updateButton);

            x -= (buttonWidth + spacing);

            _deleteButton = CreateButton("حذف", x, 10, buttonWidth);
            _deleteButton.Click += async (s, e) => await DeleteButton_ClickAsync();
            buttonPanel.Controls.Add(_deleteButton);

            x -= (buttonWidth + spacing);

            _clearButton = CreateButton("مسح", x, 10, buttonWidth);
            _clearButton.Click += ClearButton_Click;
            buttonPanel.Controls.Add(_clearButton);

            x -= (buttonWidth + spacing);

            _calculateDepreciationButton = CreateButton("حساب الإهلاك", x, 10, 140);
            _calculateDepreciationButton.BackColor = Color.FromArgb(38, 166, 154);
            _calculateDepreciationButton.Click += CalculateDepreciation_Click;
            buttonPanel.Controls.Add(_calculateDepreciationButton);

            parent.Controls.Add(buttonPanel);
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
                _assetNumberTextBox.Text = GenerateAssetNumber();
                _purchaseDatePicker.Value = DateTime.Today;
                _usefulLifeNumeric.Value = 5; // Default 5 years

                _ = LoadEmployeesAsync();
                _ = LoadSuppliersAsync();
                _ = LoadDataAsync();

                LoggingService.LogInfo("FixedAssetForm initialized");
            }
            catch (Exception ex)
            {
                LoggingService.LogError("Error initializing FixedAssetForm", ex);
                MessageBox.Show($"حدث خطأ: {ex.Message}", "خطأ", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        #endregion

        #region Helper Methods for UI Creation

        private void CreateLabelAndControl(GroupBox panel, string labelText, ref TextBox control, int x, int y, int labelWidth, int controlWidth, bool readOnly = false)
        {
            var label = new Label { Text = labelText, Location = new Point(x + controlWidth, y), Size = new Size(labelWidth, 25), TextAlign = ContentAlignment.MiddleRight };
            panel.Controls.Add(label);

            control = new TextBox { Location = new Point(x, y), Size = new Size(controlWidth, 30), Font = new Font("Cairo", 10F), ReadOnly = readOnly };
            panel.Controls.Add(control);
        }

        private void CreateLabelAndCombo(GroupBox panel, string labelText, ref ComboBox control, int x, int y, int labelWidth, int controlWidth)
        {
            var label = new Label { Text = labelText, Location = new Point(x + controlWidth, y), Size = new Size(labelWidth, 25), TextAlign = ContentAlignment.MiddleRight };
            panel.Controls.Add(label);

            control = new ComboBox { Location = new Point(x, y), Size = new Size(controlWidth, 30), DropDownStyle = ComboBoxStyle.DropDownList, Font = new Font("Cairo", 10F) };
            panel.Controls.Add(control);
        }

        private void CreateLabelAndDatePicker(GroupBox panel, string labelText, ref DateTimePicker control, int x, int y, int labelWidth, int controlWidth)
        {
            var label = new Label { Text = labelText, Location = new Point(x + controlWidth, y), Size = new Size(labelWidth, 25), TextAlign = ContentAlignment.MiddleRight };
            panel.Controls.Add(label);

            control = new DateTimePicker { Location = new Point(x, y), Size = new Size(controlWidth, 30), Format = DateTimePickerFormat.Short, Font = new Font("Cairo", 10F) };
            panel.Controls.Add(control);
        }

        private void CreateLabelAndNumeric(GroupBox panel, string labelText, ref NumericUpDown control, int x, int y, int labelWidth, int controlWidth, decimal min, decimal max, int decimals)
        {
            var label = new Label { Text = labelText, Location = new Point(x + controlWidth, y), Size = new Size(labelWidth, 25), TextAlign = ContentAlignment.MiddleRight };
            panel.Controls.Add(label);

            control = new NumericUpDown { Location = new Point(x, y), Size = new Size(controlWidth, 30), Font = new Font("Cairo", 10F), Minimum = min, Maximum = max, DecimalPlaces = decimals };
            panel.Controls.Add(control);
        }

        private void CreateValueLabel(GroupBox panel, string labelText, ref Label valueLabel, int x, int y)
        {
            var label = new Label { Text = labelText, Location = new Point(x + 180, y), Size = new Size(140, 25), TextAlign = ContentAlignment.MiddleRight };
            panel.Controls.Add(label);

            valueLabel = new Label
            {
                Text = "0.00",
                Location = new Point(x, y),
                Size = new Size(170, 30),
                TextAlign = ContentAlignment.MiddleCenter,
                Font = new Font("Cairo", 11F, FontStyle.Bold),
                ForeColor = Color.FromArgb(46, 92, 138),
                BorderStyle = BorderStyle.FixedSingle,
                BackColor = Color.White
            };
            panel.Controls.Add(valueLabel);
        }

        #endregion

        #region Data Loading

        private void LoadCategories()
        {
            var categories = Enum.GetValues(typeof(AssetCategory))
                .Cast<AssetCategory>()
                .Select(c => new { Value = c, Display = GetCategoryDisplay(c) })
                .ToList();

            _categoryComboBox.DataSource = categories;
            _categoryComboBox.DisplayMember = "Display";
            _categoryComboBox.ValueMember = "Value";
        }

        private string GetCategoryDisplay(AssetCategory category)
        {
            return category switch
            {
                AssetCategory.Buildings => "مباني وإنشاءات",
                AssetCategory.Ponds => "أحواض",
                AssetCategory.ProductionEquipment => "معدات إنتاج",
                AssetCategory.AerationEquipment => "معدات تهوية",
                AssetCategory.FiltrationEquipment => "معدات تصفية",
                AssetCategory.Generators => "مولدات كهرباء",
                AssetCategory.Pumps => "مضخات",
                AssetCategory.Vehicles => "سيارات ومركبات",
                AssetCategory.Computers => "أجهزة كمبيوتر",
                AssetCategory.Furniture => "أثاث",
                AssetCategory.OfficeEquipment => "معدات مكتبية",
                AssetCategory.LaboratoryEquipment => "معدات مخبرية",
                _ => "أخرى"
            };
        }

        private void LoadDepreciationMethods()
        {
            var methods = new System.Collections.Generic.List<dynamic>
            {
                new { Value = DepreciationMethod.StraightLine, Display = "القسط الثابت" },
                new { Value = DepreciationMethod.DecliningBalance, Display = "القسط المتناقص" }
            };

            _depreciationMethodComboBox.DataSource = methods;
            _depreciationMethodComboBox.DisplayMember = "Display";
            _depreciationMethodComboBox.ValueMember = "Value";
        }

        private void LoadDepartments()
        {
            var depts = new System.Collections.Generic.List<string>
            {
                "الإدارة", "الإنتاج", "الصيانة", "المالية", "المبيعات", "المخزن", "أخرى"
            };

            _departmentComboBox.DataSource = depts;
        }

        private void LoadAssetStatuses()
        {
            var statuses = new System.Collections.Generic.List<dynamic>
            {
                new { Value = AssetStatus.Active, Display = "نشط" },
                new { Value = AssetStatus.UnderMaintenance, Display = "قيد الصيانة" },
                new { Value = AssetStatus.Inactive, Display = "متوقف" },
                new { Value = AssetStatus.Sold, Display = "مُباع" },
                new { Value = AssetStatus.Disposed, Display = "مُستبعد" }
            };

            _statusComboBox.DataSource = statuses;
            _statusComboBox.DisplayMember = "Display";
            _statusComboBox.ValueMember = "Value";
        }

        private async Task LoadEmployeesAsync()
        {
            try
            {
                var employees = await _context.Employees
                    .Where(e => e.Status == EmployeeStatus.Active)
                    .OrderBy(e => e.FullName)
                    .Select(e => new { e.Id, DisplayName = e.FullName })
                    .ToListAsync();

                var allEmployees = new System.Collections.Generic.List<dynamic> { new { Id = 0, DisplayName = "- غير محدد -" } };
                allEmployees.AddRange(employees);

                _responsibleEmployeeComboBox.DataSource = allEmployees;
                _responsibleEmployeeComboBox.DisplayMember = "DisplayName";
                _responsibleEmployeeComboBox.ValueMember = "Id";
            }
            catch (Exception ex)
            {
                LoggingService.LogError("Error loading employees", ex);
            }
        }

        private async Task LoadSuppliersAsync()
        {
            try
            {
                var suppliers = await _context.Suppliers
                    .Where(s => s.Status == SupplierStatus.Active)
                    .OrderBy(s => s.Name)
                    .Select(s => new { s.Id, DisplayName = s.Name })
                    .ToListAsync();

                var allSuppliers = new System.Collections.Generic.List<dynamic> { new { Id = 0, DisplayName = "- غير محدد -" } };
                allSuppliers.AddRange(suppliers);

                _supplierComboBox.DataSource = allSuppliers;
                _supplierComboBox.DisplayMember = "DisplayName";
                _supplierComboBox.ValueMember = "Id";
            }
            catch (Exception ex)
            {
                LoggingService.LogError("Error loading suppliers", ex);
            }
        }

        private async Task LoadDataAsync()
        {
            try
            {
                var assets = await _context.Set<FixedAsset>()
                    .OrderByDescending(a => a.CreatedAt)
                    .Select(a => new
                    {
                        a.Id,
                        رقم_الأصل = a.AssetNumber,
                        اسم_الأصل = a.AssetName,
                        الفئة = a.Category.ToString(),
                        تاريخ_الشراء = a.PurchaseDate.ToString("yyyy-MM-dd"),
                        التكلفة = a.PurchaseCost.ToString("N2"),
                        القيمة_الدفترية = a.BookValue.ToString("N2"),
                        الإهلاك_المتراكم = a.AccumulatedDepreciation.ToString("N2"),
                        الحالة = a.Status.ToString()
                    })
                    .ToListAsync();

                _assetsGrid.DataSource = assets;

                if (_assetsGrid.Columns.Count > 0)
                {
                    _assetsGrid.Columns["Id"].Visible = false;
                }

                LoggingService.LogInfo($"Loaded {assets.Count} fixed assets");
            }
            catch (Exception ex)
            {
                LoggingService.LogError("Error loading assets", ex);
                MessageBox.Show($"حدث خطأ: {ex.Message}", "خطأ", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        #endregion

        #region CRUD Operations

        private async Task AddButton_ClickAsync()
        {
            try
            {
                // التحقق من الحقول المطلوبة
                if (string.IsNullOrWhiteSpace(_assetNameTextBox.Text))
                {
                    MessageBox.Show("الرجاء إدخال اسم الأصل", "تنبيه", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    _assetNameTextBox.Focus();
                    return;
                }

                if (_purchaseCostNumeric.Value <= 0)
                {
                    MessageBox.Show("الرجاء إدخال تكلفة شراء صحيحة", "تنبيه", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    _purchaseCostNumeric.Focus();
                    return;
                }

                // إنشاء الأصل الجديد
                var asset = new FixedAsset
                {
                    AssetNumber = _assetNumberTextBox.Text,
                    AssetName = _assetNameTextBox.Text,
                    Description = _descriptionTextBox.Text,
                    Category = _categoryComboBox.SelectedValue is AssetCategory category ? category : throw new InvalidOperationException("اختر فئة الأصل."),
                    PurchaseDate = _purchaseDatePicker.Value,
                    PurchaseCost = _purchaseCostNumeric.Value,
                    ResidualValue = _residualValueNumeric.Value,
                    UsefulLifeYears = (int)_usefulLifeNumeric.Value,
                    DepreciationMethod = _depreciationMethodComboBox.SelectedValue is DepreciationMethod method ? method : throw new InvalidOperationException("اختر طريقة الإهلاك."),
                    AnnualDepreciationRate = _depreciationRateNumeric.Value,
                    Location = _locationTextBox.Text,
                    Department = _departmentComboBox.SelectedItem?.ToString(),
                    ResponsibleEmployeeId = _responsibleEmployeeComboBox.SelectedValue as int?,
                    SupplierId = _supplierComboBox.SelectedValue as int?,
                    SerialNumber = _serialNumberTextBox.Text,
                    Status = _statusComboBox.SelectedValue is AssetStatus status ? status : throw new InvalidOperationException("اختر حالة الأصل."),
                    InServiceDate = _purchaseDatePicker.Value,
                    AccumulatedDepreciation = 0,
                    BookValue = _purchaseCostNumeric.Value,
                    CreatedBy = AuthenticationService.CurrentUsername,
                    CreatedAt = DateTime.Now
                };

                _context.FixedAssets.Add(asset);
                await _context.SaveChangesAsync();

                LoggingService.LogInfo($"Fixed asset added: {asset.AssetName} by {AuthenticationService.CurrentUsername}");
                MessageBox.Show($"تم إضافة الأصل بنجاح\n{asset.AssetName}", "نجح", MessageBoxButtons.OK, MessageBoxIcon.Information);

                ClearForm();
                await LoadDataAsync();
            }
            catch (Exception ex)
            {
                LoggingService.LogError(ex, "Error adding asset");
                MessageBox.Show($"حدث خطأ: {ex.Message}", "خطأ", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private async Task UpdateButton_ClickAsync()
        {
            try
            {
                if (_currentAssetId == null)
                {
                    MessageBox.Show("الرجاء اختيار أصل للتعديل", "تنبيه", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    return;
                }

                var asset = await _context.FixedAssets.FindAsync(_currentAssetId.Value);
                if (asset == null)
                {
                    MessageBox.Show("لم يتم العثور على الأصل", "خطأ", MessageBoxButtons.OK, MessageBoxIcon.Error);
                    return;
                }

                // تحديث البيانات
                asset.AssetName = _assetNameTextBox.Text;
                asset.Description = _descriptionTextBox.Text;
                asset.Category = _categoryComboBox.SelectedValue is AssetCategory category ? category : throw new InvalidOperationException("اختر فئة الأصل.");
                asset.PurchaseDate = _purchaseDatePicker.Value;
                asset.PurchaseCost = _purchaseCostNumeric.Value;
                asset.ResidualValue = _residualValueNumeric.Value;
                asset.UsefulLifeYears = (int)_usefulLifeNumeric.Value;
                asset.DepreciationMethod = _depreciationMethodComboBox.SelectedValue is DepreciationMethod method ? method : throw new InvalidOperationException("اختر طريقة الإهلاك.");
                asset.AnnualDepreciationRate = _depreciationRateNumeric.Value;
                asset.Location = _locationTextBox.Text;
                asset.Department = _departmentComboBox.SelectedItem?.ToString();
                asset.ResponsibleEmployeeId = _responsibleEmployeeComboBox.SelectedValue as int?;
                asset.SupplierId = _supplierComboBox.SelectedValue as int?;
                asset.SerialNumber = _serialNumberTextBox.Text;
                asset.Status = _statusComboBox.SelectedValue is AssetStatus status ? status : throw new InvalidOperationException("اختر حالة الأصل.");
                asset.UpdatedBy = AuthenticationService.CurrentUsername;
                asset.UpdatedAt = DateTime.Now;

                await _context.SaveChangesAsync();

                LoggingService.LogInfo($"Fixed asset updated: {asset.AssetName}");
                MessageBox.Show("تم تحديث الأصل بنجاح", "نجح", MessageBoxButtons.OK, MessageBoxIcon.Information);

                ClearForm();
                await LoadDataAsync();
            }
            catch (Exception ex)
            {
                LoggingService.LogError(ex, "Error updating asset");
                MessageBox.Show($"حدث خطأ: {ex.Message}", "خطأ", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private async Task DeleteButton_ClickAsync()
        {
            try
            {
                if (_currentAssetId == null)
                {
                    MessageBox.Show("الرجاء اختيار أصل للحذف", "تنبيه", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    return;
                }

                var asset = await _context.FixedAssets.FindAsync(_currentAssetId.Value);
                if (asset == null)
                {
                    MessageBox.Show("لم يتم العثور على الأصل", "خطأ", MessageBoxButtons.OK, MessageBoxIcon.Error);
                    return;
                }

                var confirmResult = MessageBox.Show(
                    $"هل أنت متأكد من حذف الأصل:\n{asset.AssetName}؟\n\nهذا الإجراء لا يمكن التراجع عنه.",
                    "تأكيد الحذف",
                    MessageBoxButtons.YesNo,
                    MessageBoxIcon.Warning
                );

                if (confirmResult != DialogResult.Yes)
                    return;

                _context.FixedAssets.Remove(asset);
                await _context.SaveChangesAsync();

                LoggingService.LogInfo($"Fixed asset deleted: {asset.AssetName} by {AuthenticationService.CurrentUsername}");
                MessageBox.Show("تم حذف الأصل بنجاح", "نجح", MessageBoxButtons.OK, MessageBoxIcon.Information);

                ClearForm();
                await LoadDataAsync();
            }
            catch (Exception ex)
            {
                LoggingService.LogError(ex, "Error deleting asset");
                MessageBox.Show($"حدث خطأ: {ex.Message}", "خطأ", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        #endregion

        #region Event Handlers

        private void Grid_CellDoubleClick(object? sender, DataGridViewCellEventArgs e)
        {
            // Load to edit
        }

        private void ClearButton_Click(object? sender, EventArgs e)
        {
            ClearForm();
        }

        private void CalculateDepreciation_Click(object? sender, EventArgs e)
        {
            CalculateDepreciationValues();
        }

        #endregion

        #region Business Logic

        private void CalculateDepreciationValues()
        {
            try
            {
                var cost = _purchaseCostNumeric.Value;
                var residual = _residualValueNumeric.Value;
                var years = (int)_usefulLifeNumeric.Value;
                var method = _depreciationMethodComboBox.SelectedValue is DepreciationMethod selectedMethod
                    ? selectedMethod
                    : throw new InvalidOperationException("اختر طريقة الإهلاك.");

                var annualDepreciation = method switch
                {
                    DepreciationMethod.StraightLine => (cost - residual) / years,
                    DepreciationMethod.DecliningBalance => cost * (_depreciationRateNumeric.Value / 100),
                    _ => 0
                };

                // Calculate age
                var ageYears = (DateTime.Now - _purchaseDatePicker.Value).Days / 365.25;
                var accumulated = (decimal)ageYears * annualDepreciation;

                if (accumulated > (cost - residual))
                    accumulated = cost - residual;

                var bookValue = cost - accumulated;

                _accumulatedDepreciationLabel.Text = accumulated.ToString("N2") + " ريال";
                _bookValueLabel.Text = bookValue.ToString("N2") + " ريال";
                _ageLabel.Text = ageYears.ToString("N1") + " سنة";

                LoggingService.LogInfo("Calculated depreciation values");
            }
            catch (Exception ex)
            {
                LoggingService.LogError("Error calculating depreciation", ex);
                MessageBox.Show($"حدث خطأ في الحساب: {ex.Message}", "خطأ", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void ClearForm()
        {
            _currentAssetId = null;
            _assetNumberTextBox.Text = GenerateAssetNumber();
            _assetNameTextBox.Clear();
            _descriptionTextBox.Clear();
            if (_categoryComboBox.Items.Count > 0)
                _categoryComboBox.SelectedIndex = 0;
            _purchaseDatePicker.Value = DateTime.Today;
            _purchaseCostNumeric.Value = 0;
            _residualValueNumeric.Value = 0;
            _usefulLifeNumeric.Value = 5;
            if (_depreciationMethodComboBox.Items.Count > 0)
                _depreciationMethodComboBox.SelectedIndex = 0;
            _depreciationRateNumeric.Value = 0;
            _locationTextBox.Clear();
            if (_departmentComboBox.Items.Count > 0)
                _departmentComboBox.SelectedIndex = 0;
            _responsibleEmployeeComboBox.SelectedIndex = 0;
            _supplierComboBox.SelectedIndex = 0;
            _serialNumberTextBox.Clear();
            if (_statusComboBox.Items.Count > 0)
                _statusComboBox.SelectedIndex = 0;
            
            _bookValueLabel.Text = "0.00";
            _accumulatedDepreciationLabel.Text = "0.00";
            _ageLabel.Text = "0 سنة";
        }

        private string GenerateAssetNumber()
        {
            return $"FA-{DateTime.Now:yyyyMMdd}-{new Random().Next(100, 999)}";
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

