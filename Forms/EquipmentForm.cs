using System;
using System.Drawing;
using System.Linq;
using System.Windows.Forms;
using Microsoft.EntityFrameworkCore;
using FishFarmManager.Data;
using FishFarmManager.Models;

namespace FishFarmManager.Forms
{
    public partial class EquipmentForm : Form
    {
        private readonly FishFarmContext _context;
        private int _selectedEquipmentId = 0;

        // Controls
        private DataGridView _equipmentGrid = null!;
        private TextBox _equipmentNumberTextBox = null!;
        private TextBox _nameTextBox = null!;
        private ComboBox _categoryComboBox = null!;
        private TextBox _manufacturerTextBox = null!;
        private TextBox _modelTextBox = null!;
        private TextBox _serialNumberTextBox = null!;
        private DateTimePicker _purchaseDatePicker = null!;
        private NumericUpDown _purchasePriceNumeric = null!;
        private TextBox _supplierTextBox = null!;
        private TextBox _warrantyPeriodTextBox = null!;
        private DateTimePicker _warrantyExpiryDatePicker = null!;
        private DateTimePicker _installationDatePicker = null!;
        private ComboBox _pondComboBox = null!;
        private TextBox _locationTextBox = null!;
        private ComboBox _statusComboBox = null!;
        private NumericUpDown _operatingHoursNumeric = null!;
        private DateTimePicker _lastMaintenanceDatePicker = null!;
        private DateTimePicker _nextMaintenanceDatePicker = null!;
        private NumericUpDown _maintenanceIntervalNumeric = null!;
        private NumericUpDown _annualMaintenanceCostNumeric = null!;
        private TextBox _specificationsTextBox = null!;
        private TextBox _safetyNotesTextBox = null!;
        private TextBox _notesTextBox = null!;
        private Button _saveButton = null!;
        private Button _deleteButton = null!;
        private Button _newButton = null!;
        private TextBox _searchTextBox = null!;
        private ComboBox _filterCategoryComboBox = null!;
        private ComboBox _filterStatusComboBox = null!;

        public EquipmentForm(FishFarmContext context)
        {
            _context = context;
            InitializeComponent();
            LoadPonds();
            LoadEquipment();
        }

        private void InitializeComponent()
        {
            this.Text = "إدارة المعدات - Equipment Management";
            this.Size = new Size(1500, 850);
            this.RightToLeft = RightToLeft.Yes;
            this.RightToLeftLayout = true;
            this.StartPosition = FormStartPosition.CenterScreen;

            var mainPanel = new TableLayoutPanel
            {
                Dock = DockStyle.Fill,
                ColumnCount = 1,
                RowCount = 2,
                Padding = new Padding(10)
            };
            mainPanel.RowStyles.Add(new RowStyle(SizeType.Percent, 60F));
            mainPanel.RowStyles.Add(new RowStyle(SizeType.Percent, 40F));

            // Top panel - Equipment details
            var topPanel = CreateTopPanel();
            mainPanel.Controls.Add(topPanel, 0, 0);

            // Bottom panel - Equipment list
            var bottomPanel = CreateBottomPanel();
            mainPanel.Controls.Add(bottomPanel, 0, 1);

            this.Controls.Add(mainPanel);
        }

        private Panel CreateTopPanel()
        {
            var panel = new Panel
            {
                Dock = DockStyle.Fill,
                Padding = new Padding(5)
            };

            var groupBox = new GroupBox
            {
                Text = "بيانات المعدات - Equipment Details",
                Dock = DockStyle.Fill,
                Padding = new Padding(10)
            };

            var layout = new TableLayoutPanel
            {
                Dock = DockStyle.Fill,
                ColumnCount = 6,
                RowCount = 8,
                AutoScroll = true
            };

            // Set column widths
            for (int i = 0; i < 6; i++)
                layout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 16.67F));

            // Row 1: Equipment Number, Name, Category
            layout.Controls.Add(new Label { Text = "رقم المعدة:", TextAlign = ContentAlignment.MiddleRight, Dock = DockStyle.Fill }, 0, 0);
            _equipmentNumberTextBox = new TextBox { Dock = DockStyle.Fill };
            layout.Controls.Add(_equipmentNumberTextBox, 1, 0);

            layout.Controls.Add(new Label { Text = "اسم المعدة:", TextAlign = ContentAlignment.MiddleRight, Dock = DockStyle.Fill }, 2, 0);
            _nameTextBox = new TextBox { Dock = DockStyle.Fill };
            layout.Controls.Add(_nameTextBox, 3, 0);

            layout.Controls.Add(new Label { Text = "الفئة:", TextAlign = ContentAlignment.MiddleRight, Dock = DockStyle.Fill }, 4, 0);
            _categoryComboBox = new ComboBox { Dock = DockStyle.Fill, DropDownStyle = ComboBoxStyle.DropDownList };
            _categoryComboBox.Items.AddRange(new object[] { "مضخات", "أجهزة تهوية", "مولدات", "معدات تغذية", "أنظمة ترشيح", "أجهزة قياس", "معدات حصاد", "أخرى" });
            layout.Controls.Add(_categoryComboBox, 5, 0);

            // Row 2: Manufacturer, Model, Serial Number
            layout.Controls.Add(new Label { Text = "الشركة المصنعة:", TextAlign = ContentAlignment.MiddleRight, Dock = DockStyle.Fill }, 0, 1);
            _manufacturerTextBox = new TextBox { Dock = DockStyle.Fill };
            layout.Controls.Add(_manufacturerTextBox, 1, 1);

            layout.Controls.Add(new Label { Text = "الموديل:", TextAlign = ContentAlignment.MiddleRight, Dock = DockStyle.Fill }, 2, 1);
            _modelTextBox = new TextBox { Dock = DockStyle.Fill };
            layout.Controls.Add(_modelTextBox, 3, 1);

            layout.Controls.Add(new Label { Text = "الرقم التسلسلي:", TextAlign = ContentAlignment.MiddleRight, Dock = DockStyle.Fill }, 4, 1);
            _serialNumberTextBox = new TextBox { Dock = DockStyle.Fill };
            layout.Controls.Add(_serialNumberTextBox, 5, 1);

            // Row 3: Purchase Date, Price, Supplier
            layout.Controls.Add(new Label { Text = "تاريخ الشراء:", TextAlign = ContentAlignment.MiddleRight, Dock = DockStyle.Fill }, 0, 2);
            _purchaseDatePicker = new DateTimePicker { Dock = DockStyle.Fill, Format = DateTimePickerFormat.Short };
            layout.Controls.Add(_purchaseDatePicker, 1, 2);

            layout.Controls.Add(new Label { Text = "سعر الشراء:", TextAlign = ContentAlignment.MiddleRight, Dock = DockStyle.Fill }, 2, 2);
            _purchasePriceNumeric = new NumericUpDown { Dock = DockStyle.Fill, Maximum = 999999999, DecimalPlaces = 2 };
            layout.Controls.Add(_purchasePriceNumeric, 3, 2);

            layout.Controls.Add(new Label { Text = "المورد:", TextAlign = ContentAlignment.MiddleRight, Dock = DockStyle.Fill }, 4, 2);
            _supplierTextBox = new TextBox { Dock = DockStyle.Fill };
            layout.Controls.Add(_supplierTextBox, 5, 2);

            // Row 4: Warranty Period, Warranty Expiry, Installation Date
            layout.Controls.Add(new Label { Text = "فترة الضمان (أشهر):", TextAlign = ContentAlignment.MiddleRight, Dock = DockStyle.Fill }, 0, 3);
            _warrantyPeriodTextBox = new TextBox { Dock = DockStyle.Fill };
            layout.Controls.Add(_warrantyPeriodTextBox, 1, 3);

            layout.Controls.Add(new Label { Text = "انتهاء الضمان:", TextAlign = ContentAlignment.MiddleRight, Dock = DockStyle.Fill }, 2, 3);
            _warrantyExpiryDatePicker = new DateTimePicker { Dock = DockStyle.Fill, Format = DateTimePickerFormat.Short };
            layout.Controls.Add(_warrantyExpiryDatePicker, 3, 3);

            layout.Controls.Add(new Label { Text = "تاريخ التركيب:", TextAlign = ContentAlignment.MiddleRight, Dock = DockStyle.Fill }, 4, 3);
            _installationDatePicker = new DateTimePicker { Dock = DockStyle.Fill, Format = DateTimePickerFormat.Short };
            layout.Controls.Add(_installationDatePicker, 5, 3);

            // Row 5: Pond, Location, Status
            layout.Controls.Add(new Label { Text = "الحوض:", TextAlign = ContentAlignment.MiddleRight, Dock = DockStyle.Fill }, 0, 4);
            _pondComboBox = new ComboBox { Dock = DockStyle.Fill, DropDownStyle = ComboBoxStyle.DropDownList };
            layout.Controls.Add(_pondComboBox, 1, 4);

            layout.Controls.Add(new Label { Text = "الموقع:", TextAlign = ContentAlignment.MiddleRight, Dock = DockStyle.Fill }, 2, 4);
            _locationTextBox = new TextBox { Dock = DockStyle.Fill };
            layout.Controls.Add(_locationTextBox, 3, 4);

            layout.Controls.Add(new Label { Text = "الحالة:", TextAlign = ContentAlignment.MiddleRight, Dock = DockStyle.Fill }, 4, 4);
            _statusComboBox = new ComboBox { Dock = DockStyle.Fill, DropDownStyle = ComboBoxStyle.DropDownList };
            _statusComboBox.Items.AddRange(new object[] { "تشغيل", "صيانة", "عطل", "متوقف" });
            layout.Controls.Add(_statusComboBox, 5, 4);

            // Row 6: Operating Hours, Last Maintenance, Next Maintenance
            layout.Controls.Add(new Label { Text = "ساعات التشغيل:", TextAlign = ContentAlignment.MiddleRight, Dock = DockStyle.Fill }, 0, 5);
            _operatingHoursNumeric = new NumericUpDown { Dock = DockStyle.Fill, Maximum = 999999 };
            layout.Controls.Add(_operatingHoursNumeric, 1, 5);

            layout.Controls.Add(new Label { Text = "آخر صيانة:", TextAlign = ContentAlignment.MiddleRight, Dock = DockStyle.Fill }, 2, 5);
            _lastMaintenanceDatePicker = new DateTimePicker { Dock = DockStyle.Fill, Format = DateTimePickerFormat.Short };
            layout.Controls.Add(_lastMaintenanceDatePicker, 3, 5);

            layout.Controls.Add(new Label { Text = "الصيانة القادمة:", TextAlign = ContentAlignment.MiddleRight, Dock = DockStyle.Fill }, 4, 5);
            _nextMaintenanceDatePicker = new DateTimePicker { Dock = DockStyle.Fill, Format = DateTimePickerFormat.Short };
            layout.Controls.Add(_nextMaintenanceDatePicker, 5, 5);

            // Row 7: Maintenance Interval, Annual Cost, Specifications
            layout.Controls.Add(new Label { Text = "فترة الصيانة (أيام):", TextAlign = ContentAlignment.MiddleRight, Dock = DockStyle.Fill }, 0, 6);
            _maintenanceIntervalNumeric = new NumericUpDown { Dock = DockStyle.Fill, Maximum = 365 };
            layout.Controls.Add(_maintenanceIntervalNumeric, 1, 6);

            layout.Controls.Add(new Label { Text = "تكلفة الصيانة السنوية:", TextAlign = ContentAlignment.MiddleRight, Dock = DockStyle.Fill }, 2, 6);
            _annualMaintenanceCostNumeric = new NumericUpDown { Dock = DockStyle.Fill, Maximum = 999999999, DecimalPlaces = 2 };
            layout.Controls.Add(_annualMaintenanceCostNumeric, 3, 6);

            layout.Controls.Add(new Label { Text = "المواصفات:", TextAlign = ContentAlignment.MiddleRight, Dock = DockStyle.Top }, 4, 6);
            _specificationsTextBox = new TextBox { Dock = DockStyle.Fill, Multiline = true, Height = 60 };
            layout.SetColumnSpan(_specificationsTextBox, 2);
            layout.Controls.Add(_specificationsTextBox, 4, 6);

            // Row 8: Safety Notes, Notes
            layout.Controls.Add(new Label { Text = "ملاحظات السلامة:", TextAlign = ContentAlignment.MiddleRight, Dock = DockStyle.Top }, 0, 7);
            _safetyNotesTextBox = new TextBox { Dock = DockStyle.Fill, Multiline = true, Height = 60 };
            layout.SetColumnSpan(_safetyNotesTextBox, 2);
            layout.Controls.Add(_safetyNotesTextBox, 0, 7);

            layout.Controls.Add(new Label { Text = "ملاحظات:", TextAlign = ContentAlignment.MiddleRight, Dock = DockStyle.Top }, 3, 7);
            _notesTextBox = new TextBox { Dock = DockStyle.Fill, Multiline = true, Height = 60 };
            layout.SetColumnSpan(_notesTextBox, 3);
            layout.Controls.Add(_notesTextBox, 3, 7);

            groupBox.Controls.Add(layout);

            // Buttons panel
            var buttonsPanel = new FlowLayoutPanel
            {
                Dock = DockStyle.Bottom,
                Height = 50,
                FlowDirection = FlowDirection.RightToLeft,
                Padding = new Padding(5)
            };

            _saveButton = new Button { Text = "حفظ", Width = 100, Height = 35 };
            _saveButton.Click += SaveButton_Click;
            buttonsPanel.Controls.Add(_saveButton);

            _deleteButton = new Button { Text = "حذف", Width = 100, Height = 35 };
            _deleteButton.Click += DeleteButton_Click;
            buttonsPanel.Controls.Add(_deleteButton);

            _newButton = new Button { Text = "جديد", Width = 100, Height = 35 };
            _newButton.Click += NewButton_Click;
            buttonsPanel.Controls.Add(_newButton);

            panel.Controls.Add(groupBox);
            panel.Controls.Add(buttonsPanel);

            return panel;
        }

        private Panel CreateBottomPanel()
        {
            var panel = new Panel
            {
                Dock = DockStyle.Fill,
                Padding = new Padding(5)
            };

            var groupBox = new GroupBox
            {
                Text = "قائمة المعدات - Equipment List",
                Dock = DockStyle.Fill,
                Padding = new Padding(10)
            };

            // Filter panel
            var filterPanel = new FlowLayoutPanel
            {
                Dock = DockStyle.Top,
                Height = 40,
                FlowDirection = FlowDirection.RightToLeft,
                Padding = new Padding(5)
            };

            filterPanel.Controls.Add(new Label { Text = "بحث:", TextAlign = ContentAlignment.MiddleCenter, AutoSize = true, Margin = new Padding(5) });
            _searchTextBox = new TextBox { Width = 200 };
            _searchTextBox.TextChanged += (s, e) => LoadEquipment();
            filterPanel.Controls.Add(_searchTextBox);

            filterPanel.Controls.Add(new Label { Text = "الفئة:", TextAlign = ContentAlignment.MiddleCenter, AutoSize = true, Margin = new Padding(5) });
            _filterCategoryComboBox = new ComboBox { Width = 150, DropDownStyle = ComboBoxStyle.DropDownList };
            _filterCategoryComboBox.Items.Add("الكل");
            _filterCategoryComboBox.Items.AddRange(new object[] { "مضخات", "أجهزة تهوية", "مولدات", "معدات تغذية", "أنظمة ترشيح", "أجهزة قياس", "معدات حصاد", "أخرى" });
            _filterCategoryComboBox.SelectedIndex = 0;
            _filterCategoryComboBox.SelectedIndexChanged += (s, e) => LoadEquipment();
            filterPanel.Controls.Add(_filterCategoryComboBox);

            filterPanel.Controls.Add(new Label { Text = "الحالة:", TextAlign = ContentAlignment.MiddleCenter, AutoSize = true, Margin = new Padding(5) });
            _filterStatusComboBox = new ComboBox { Width = 120, DropDownStyle = ComboBoxStyle.DropDownList };
            _filterStatusComboBox.Items.AddRange(new object[] { "الكل", "تشغيل", "صيانة", "عطل", "متوقف" });
            _filterStatusComboBox.SelectedIndex = 0;
            _filterStatusComboBox.SelectedIndexChanged += (s, e) => LoadEquipment();
            filterPanel.Controls.Add(_filterStatusComboBox);

            // Grid
            _equipmentGrid = new DataGridView
            {
                Dock = DockStyle.Fill,
                AutoGenerateColumns = false,
                SelectionMode = DataGridViewSelectionMode.FullRowSelect,
                MultiSelect = false,
                ReadOnly = true,
                AllowUserToAddRows = false,
                AllowUserToDeleteRows = false,
                RightToLeft = RightToLeft.Yes
            };

            _equipmentGrid.Columns.Add(new DataGridViewTextBoxColumn { HeaderText = "الرقم", DataPropertyName = "EquipmentNumber", Width = 80 });
            _equipmentGrid.Columns.Add(new DataGridViewTextBoxColumn { HeaderText = "الاسم", DataPropertyName = "Name", Width = 150 });
            _equipmentGrid.Columns.Add(new DataGridViewTextBoxColumn { HeaderText = "الفئة", DataPropertyName = "Category", Width = 120 });
            _equipmentGrid.Columns.Add(new DataGridViewTextBoxColumn { HeaderText = "الشركة المصنعة", DataPropertyName = "Manufacturer", Width = 120 });
            _equipmentGrid.Columns.Add(new DataGridViewTextBoxColumn { HeaderText = "الموديل", DataPropertyName = "Model", Width = 100 });
            _equipmentGrid.Columns.Add(new DataGridViewTextBoxColumn { HeaderText = "الموقع", DataPropertyName = "Location", Width = 100 });
            _equipmentGrid.Columns.Add(new DataGridViewTextBoxColumn { HeaderText = "الحالة", DataPropertyName = "Status", Width = 80 });
            _equipmentGrid.Columns.Add(new DataGridViewTextBoxColumn { HeaderText = "ساعات التشغيل", DataPropertyName = "OperatingHours", Width = 100 });
            _equipmentGrid.Columns.Add(new DataGridViewTextBoxColumn { HeaderText = "آخر صيانة", DataPropertyName = "LastMaintenanceDate", Width = 100, DefaultCellStyle = new DataGridViewCellStyle { Format = "yyyy-MM-dd" } });
            _equipmentGrid.Columns.Add(new DataGridViewTextBoxColumn { HeaderText = "الصيانة القادمة", DataPropertyName = "NextMaintenanceDate", Width = 100, DefaultCellStyle = new DataGridViewCellStyle { Format = "yyyy-MM-dd" } });

            _equipmentGrid.SelectionChanged += EquipmentGrid_SelectionChanged;

            groupBox.Controls.Add(_equipmentGrid);
            groupBox.Controls.Add(filterPanel);

            panel.Controls.Add(groupBox);

            return panel;
        }

        private void LoadPonds()
        {
            var ponds = _context.Ponds.OrderBy(p => p.Name).ToList();
            _pondComboBox.Items.Clear();
            _pondComboBox.Items.Add(new { Id = (int?)null, Name = "-- بدون حوض --" });
            foreach (var pond in ponds)
            {
                _pondComboBox.Items.Add(new { Id = (int?)pond.Id, Name = pond.Name });
            }
            _pondComboBox.DisplayMember = "Name";
            _pondComboBox.ValueMember = "Id";
            _pondComboBox.SelectedIndex = 0;
        }

        private void LoadEquipment()
        {
            var query = _context.Equipment.Include(e => e.Pond).AsQueryable();

            // Apply filters
            if (!string.IsNullOrWhiteSpace(_searchTextBox.Text))
            {
                var search = _searchTextBox.Text.ToLower();
                query = query.Where(e => e.EquipmentNumber.ToLower().Contains(search) ||
                                        e.Name.ToLower().Contains(search) ||
                                        (e.Manufacturer != null && e.Manufacturer.ToLower().Contains(search)));
            }

            if (_filterCategoryComboBox.SelectedIndex > 0)
            {
                var category = _filterCategoryComboBox.SelectedItem?.ToString();
                query = query.Where(e => e.Category == category);
            }

            if (_filterStatusComboBox.SelectedIndex > 0)
            {
                var status = _filterStatusComboBox.SelectedItem?.ToString();
                query = query.Where(e => e.Status == status);
            }

            var equipment = query.OrderBy(e => e.EquipmentNumber).ToList();
            _equipmentGrid.DataSource = equipment;
        }

        private void EquipmentGrid_SelectionChanged(object? sender, EventArgs e)
        {
            if (_equipmentGrid.SelectedRows.Count > 0)
            {
                var equipment = (Equipment)_equipmentGrid.SelectedRows[0].DataBoundItem;
                _selectedEquipmentId = equipment.Id;

                _equipmentNumberTextBox.Text = equipment.EquipmentNumber;
                _nameTextBox.Text = equipment.Name;
                _categoryComboBox.Text = equipment.Category ?? "";
                _manufacturerTextBox.Text = equipment.Manufacturer ?? "";
                _modelTextBox.Text = equipment.Model ?? "";
                _serialNumberTextBox.Text = equipment.SerialNumber ?? "";
                _purchaseDatePicker.Value = equipment.PurchaseDate ?? DateTime.Now;
                _purchasePriceNumeric.Value = equipment.PurchasePrice;
                _supplierTextBox.Text = equipment.Supplier ?? "";
                _warrantyPeriodTextBox.Text = equipment.WarrantyPeriod.ToString();
                _warrantyExpiryDatePicker.Value = equipment.WarrantyExpiryDate ?? DateTime.Now;
                _installationDatePicker.Value = equipment.InstallationDate ?? DateTime.Now;

                // Set pond
                if (equipment.PondId.HasValue)
                {
                    for (int i = 0; i < _pondComboBox.Items.Count; i++)
                    {
                        var item = (dynamic)_pondComboBox.Items[i]!;
                        if (item.Id == equipment.PondId)
                        {
                            _pondComboBox.SelectedIndex = i;
                            break;
                        }
                    }
                }
                else
                {
                    _pondComboBox.SelectedIndex = 0;
                }

                _locationTextBox.Text = equipment.Location ?? "";
                _statusComboBox.Text = equipment.Status;
                _operatingHoursNumeric.Value = equipment.OperatingHours;
                _lastMaintenanceDatePicker.Value = equipment.LastMaintenanceDate ?? DateTime.Now;
                _nextMaintenanceDatePicker.Value = equipment.NextMaintenanceDate ?? DateTime.Now;
                _maintenanceIntervalNumeric.Value = equipment.MaintenanceIntervalDays;
                _annualMaintenanceCostNumeric.Value = equipment.AnnualMaintenanceCost;
                _specificationsTextBox.Text = equipment.Specifications ?? "";
                _safetyNotesTextBox.Text = equipment.SafetyNotes ?? "";
                _notesTextBox.Text = equipment.Notes ?? "";
            }
        }

        private void SaveButton_Click(object? sender, EventArgs e)
        {
            if (string.IsNullOrWhiteSpace(_equipmentNumberTextBox.Text) || string.IsNullOrWhiteSpace(_nameTextBox.Text))
            {
                MessageBox.Show("الرجاء إدخال رقم واسم المعدة", "تحذير", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            Equipment? equipment;
            if (_selectedEquipmentId > 0)
            {
                equipment = _context.Equipment.Find(_selectedEquipmentId);
                if (equipment == null) return;
                equipment.ModifiedDate = DateTime.Now;
            }
            else
            {
                equipment = new Equipment();
                _context.Equipment.Add(equipment);
            }

            equipment.EquipmentNumber = _equipmentNumberTextBox.Text;
            equipment.Name = _nameTextBox.Text;
            equipment.Category = _categoryComboBox.Text;
            equipment.Manufacturer = _manufacturerTextBox.Text;
            equipment.Model = _modelTextBox.Text;
            equipment.SerialNumber = _serialNumberTextBox.Text;
            equipment.PurchaseDate = _purchaseDatePicker.Value;
            equipment.PurchasePrice = _purchasePriceNumeric.Value;
            equipment.Supplier = _supplierTextBox.Text;
            equipment.WarrantyPeriod = int.TryParse(_warrantyPeriodTextBox.Text, out var warranty) ? warranty : 0;
            equipment.WarrantyExpiryDate = _warrantyExpiryDatePicker.Value;
            equipment.InstallationDate = _installationDatePicker.Value;

            var selectedPond = (dynamic?)_pondComboBox.SelectedItem;
            equipment.PondId = selectedPond?.Id;

            equipment.Location = _locationTextBox.Text;
            equipment.Status = _statusComboBox.Text;
            equipment.OperatingHours = (int)_operatingHoursNumeric.Value;
            equipment.LastMaintenanceDate = _lastMaintenanceDatePicker.Value;
            equipment.NextMaintenanceDate = _nextMaintenanceDatePicker.Value;
            equipment.MaintenanceIntervalDays = (int)_maintenanceIntervalNumeric.Value;
            equipment.AnnualMaintenanceCost = _annualMaintenanceCostNumeric.Value;
            equipment.Specifications = _specificationsTextBox.Text;
            equipment.SafetyNotes = _safetyNotesTextBox.Text;
            equipment.Notes = _notesTextBox.Text;

            _context.SaveChanges();
            MessageBox.Show("تم حفظ البيانات بنجاح", "نجاح", MessageBoxButtons.OK, MessageBoxIcon.Information);

            LoadEquipment();
            ClearForm();
        }

        private void DeleteButton_Click(object? sender, EventArgs e)
        {
            if (_selectedEquipmentId == 0)
            {
                MessageBox.Show("الرجاء اختيار معدة للحذف", "تحذير", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            var result = MessageBox.Show("هل أنت متأكد من حذف هذه المعدة؟", "تأكيد الحذف", MessageBoxButtons.YesNo, MessageBoxIcon.Question);
            if (result == DialogResult.Yes)
            {
                var equipment = _context.Equipment.Find(_selectedEquipmentId);
                if (equipment != null)
                {
                    _context.Equipment.Remove(equipment);
                    _context.SaveChanges();
                    MessageBox.Show("تم حذف المعدة بنجاح", "نجاح", MessageBoxButtons.OK, MessageBoxIcon.Information);

                    LoadEquipment();
                    ClearForm();
                }
            }
        }

        private void NewButton_Click(object? sender, EventArgs e)
        {
            ClearForm();
        }

        private void ClearForm()
        {
            _selectedEquipmentId = 0;
            _equipmentNumberTextBox.Clear();
            _nameTextBox.Clear();
            _categoryComboBox.SelectedIndex = -1;
            _manufacturerTextBox.Clear();
            _modelTextBox.Clear();
            _serialNumberTextBox.Clear();
            _purchaseDatePicker.Value = DateTime.Now;
            _purchasePriceNumeric.Value = 0;
            _supplierTextBox.Clear();
            _warrantyPeriodTextBox.Clear();
            _warrantyExpiryDatePicker.Value = DateTime.Now;
            _installationDatePicker.Value = DateTime.Now;
            _pondComboBox.SelectedIndex = 0;
            _locationTextBox.Clear();
            _statusComboBox.SelectedIndex = 0;
            _operatingHoursNumeric.Value = 0;
            _lastMaintenanceDatePicker.Value = DateTime.Now;
            _nextMaintenanceDatePicker.Value = DateTime.Now;
            _maintenanceIntervalNumeric.Value = 0;
            _annualMaintenanceCostNumeric.Value = 0;
            _specificationsTextBox.Clear();
            _safetyNotesTextBox.Clear();
            _notesTextBox.Clear();
        }
    }
}
