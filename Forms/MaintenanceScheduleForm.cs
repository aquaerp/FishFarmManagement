using System;
using FishFarmManager.Services;
using System.Drawing;
using System.Linq;
using System.Windows.Forms;
using Microsoft.EntityFrameworkCore;
using FishFarmManager.Data;
using FishFarmManager.Models;

namespace FishFarmManager.Forms
{
    public partial class MaintenanceScheduleForm : Form
    {
        private readonly FishFarmContext _context;
        private int _selectedScheduleId = 0;

        // Controls
        private DataGridView _scheduleGrid = null!;
        private ComboBox _equipmentComboBox = null!;
        private TextBox _maintenanceTypeTextBox = null!;
        private ComboBox _frequencyComboBox = null!;
        private NumericUpDown _frequencyDaysNumeric = null!;
        private DateTimePicker _lastMaintenanceDatePicker = null!;
        private DateTimePicker _nextMaintenanceDatePicker = null!;
        private ComboBox _priorityComboBox = null!;
        private NumericUpDown _estimatedCostNumeric = null!;
        private NumericUpDown _estimatedDurationNumeric = null!;
        private TextBox _assignedToTextBox = null!;
        private TextBox _requiredPartsTextBox = null!;
        private TextBox _maintenanceInstructionsTextBox = null!;
        private ComboBox _statusComboBox = null!;
        private CheckBox _sendNotificationCheckBox = null!;
        private NumericUpDown _notificationDaysNumeric = null!;
        private CheckBox _isActiveCheckBox = null!;
        private TextBox _notesTextBox = null!;
        private Button _saveButton = null!;
        private Button _deleteButton = null!;
        private Button _newButton = null!;
        private Button _generateScheduleButton = null!;
        private TextBox _searchTextBox = null!;
        private ComboBox _filterStatusComboBox = null!;
        private ComboBox _filterEquipmentComboBox = null!;

        public MaintenanceScheduleForm(FishFarmContext context)
        {
            _context = context;
            InitializeComponent();
            LoadEquipment();
            LoadSchedules();
        }

        private void InitializeComponent()
        {
            this.Text = "جدولة الصيانة - Maintenance Schedule";
            this.Size = new Size(1600, 900);
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
            mainPanel.RowStyles.Add(new RowStyle(SizeType.Percent, 55F));
            mainPanel.RowStyles.Add(new RowStyle(SizeType.Percent, 45F));

            // Top panel - Schedule details
            var topPanel = CreateTopPanel();
            mainPanel.Controls.Add(topPanel, 0, 0);

            // Bottom panel - Schedule list
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
                Text = "تفاصيل جدولة الصيانة - Schedule Details",
                Dock = DockStyle.Fill,
                Padding = new Padding(10)
            };

            var layout = new TableLayoutPanel
            {
                Dock = DockStyle.Fill,
                ColumnCount = 6,
                RowCount = 6,
                AutoScroll = true
            };

            // Set column widths
            for (int i = 0; i < 6; i++)
                layout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 16.67F));

            // Row 1: Equipment, Maintenance Type, Frequency
            layout.Controls.Add(new Label { Text = "المعدة:", TextAlign = ContentAlignment.MiddleRight, Dock = DockStyle.Fill }, 0, 0);
            _equipmentComboBox = new ComboBox { Dock = DockStyle.Fill, DropDownStyle = ComboBoxStyle.DropDownList };
            layout.Controls.Add(_equipmentComboBox, 1, 0);

            layout.Controls.Add(new Label { Text = "نوع الصيانة:", TextAlign = ContentAlignment.MiddleRight, Dock = DockStyle.Fill }, 2, 0);
            _maintenanceTypeTextBox = new TextBox { Dock = DockStyle.Fill };
            layout.Controls.Add(_maintenanceTypeTextBox, 3, 0);

            layout.Controls.Add(new Label { Text = "التكرار:", TextAlign = ContentAlignment.MiddleRight, Dock = DockStyle.Fill }, 4, 0);
            _frequencyComboBox = new ComboBox { Dock = DockStyle.Fill, DropDownStyle = ComboBoxStyle.DropDownList };
            _frequencyComboBox.Items.AddRange(new object[] { "يومي", "أسبوعي", "شهري", "ربع سنوي", "نصف سنوي", "سنوي", "مخصص" });
            _frequencyComboBox.SelectedIndexChanged += FrequencyComboBox_SelectedIndexChanged;
            layout.Controls.Add(_frequencyComboBox, 5, 0);

            // Row 2: Frequency Days, Last Maintenance, Next Maintenance
            layout.Controls.Add(new Label { Text = "الفترة بالأيام:", TextAlign = ContentAlignment.MiddleRight, Dock = DockStyle.Fill }, 0, 1);
            _frequencyDaysNumeric = new NumericUpDown { Dock = DockStyle.Fill, Maximum = 365, Minimum = 1, Value = 30 };
            layout.Controls.Add(_frequencyDaysNumeric, 1, 1);

            layout.Controls.Add(new Label { Text = "آخر صيانة:", TextAlign = ContentAlignment.MiddleRight, Dock = DockStyle.Fill }, 2, 1);
            _lastMaintenanceDatePicker = new DateTimePicker { Dock = DockStyle.Fill, Format = DateTimePickerFormat.Short };
            _lastMaintenanceDatePicker.ValueChanged += LastMaintenanceDatePicker_ValueChanged;
            layout.Controls.Add(_lastMaintenanceDatePicker, 3, 1);

            layout.Controls.Add(new Label { Text = "الصيانة القادمة:", TextAlign = ContentAlignment.MiddleRight, Dock = DockStyle.Fill }, 4, 1);
            _nextMaintenanceDatePicker = new DateTimePicker { Dock = DockStyle.Fill, Format = DateTimePickerFormat.Short };
            layout.Controls.Add(_nextMaintenanceDatePicker, 5, 1);

            // Row 3: Priority, Estimated Cost, Estimated Duration
            layout.Controls.Add(new Label { Text = "الأولوية:", TextAlign = ContentAlignment.MiddleRight, Dock = DockStyle.Fill }, 0, 2);
            _priorityComboBox = new ComboBox { Dock = DockStyle.Fill, DropDownStyle = ComboBoxStyle.DropDownList };
            _priorityComboBox.Items.AddRange(new object[] { "عالية", "متوسطة", "منخفضة" });
            layout.Controls.Add(_priorityComboBox, 1, 2);

            layout.Controls.Add(new Label { Text = "التكلفة المقدرة (ريال):", TextAlign = ContentAlignment.MiddleRight, Dock = DockStyle.Fill }, 2, 2);
            _estimatedCostNumeric = new NumericUpDown { Dock = DockStyle.Fill, Maximum = 999999, DecimalPlaces = 2 };
            layout.Controls.Add(_estimatedCostNumeric, 3, 2);

            layout.Controls.Add(new Label { Text = "المدة المقدرة (دقيقة):", TextAlign = ContentAlignment.MiddleRight, Dock = DockStyle.Fill }, 4, 2);
            _estimatedDurationNumeric = new NumericUpDown { Dock = DockStyle.Fill, Maximum = 9999 };
            layout.Controls.Add(_estimatedDurationNumeric, 5, 2);

            // Row 4: Assigned To, Required Parts
            layout.Controls.Add(new Label { Text = "المسؤول:", TextAlign = ContentAlignment.MiddleRight, Dock = DockStyle.Fill }, 0, 3);
            _assignedToTextBox = new TextBox { Dock = DockStyle.Fill };
            layout.Controls.Add(_assignedToTextBox, 1, 3);

            layout.Controls.Add(new Label { Text = "القطع المطلوبة:", TextAlign = ContentAlignment.MiddleRight, Dock = DockStyle.Fill }, 2, 3);
            _requiredPartsTextBox = new TextBox { Dock = DockStyle.Fill, Multiline = true, Height = 60 };
            layout.SetColumnSpan(_requiredPartsTextBox, 4);
            layout.Controls.Add(_requiredPartsTextBox, 2, 3);

            // Row 5: Maintenance Instructions
            layout.Controls.Add(new Label { Text = "تعليمات الصيانة:", TextAlign = ContentAlignment.MiddleRight, Dock = DockStyle.Top }, 0, 4);
            _maintenanceInstructionsTextBox = new TextBox { Dock = DockStyle.Fill, Multiline = true, Height = 80, ScrollBars = ScrollBars.Vertical };
            layout.SetColumnSpan(_maintenanceInstructionsTextBox, 6);
            layout.Controls.Add(_maintenanceInstructionsTextBox, 0, 4);

            // Row 6: Status, Notifications, Active, Notes
            layout.Controls.Add(new Label { Text = "الحالة:", TextAlign = ContentAlignment.MiddleRight, Dock = DockStyle.Fill }, 0, 5);
            _statusComboBox = new ComboBox { Dock = DockStyle.Fill, DropDownStyle = ComboBoxStyle.DropDownList };
            _statusComboBox.Items.AddRange(new object[] { "مجدولة", "قيد التنفيذ", "مكتملة", "متأخرة", "ملغاة" });
            layout.Controls.Add(_statusComboBox, 1, 5);

            _sendNotificationCheckBox = new CheckBox { Text = "إرسال تنبيه", Dock = DockStyle.Fill, Checked = true };
            layout.Controls.Add(_sendNotificationCheckBox, 2, 5);

            layout.Controls.Add(new Label { Text = "قبل (أيام):", TextAlign = ContentAlignment.MiddleRight, Dock = DockStyle.Fill }, 3, 5);
            _notificationDaysNumeric = new NumericUpDown { Dock = DockStyle.Fill, Maximum = 90, Value = 7 };
            layout.Controls.Add(_notificationDaysNumeric, 4, 5);

            _isActiveCheckBox = new CheckBox { Text = "نشط", Dock = DockStyle.Fill, Checked = true };
            layout.Controls.Add(_isActiveCheckBox, 5, 5);

            groupBox.Controls.Add(layout);

            // Buttons panel
            var buttonsPanel = new FlowLayoutPanel
            {
                Dock = DockStyle.Bottom,
                Height = 50,
                FlowDirection = FlowDirection.RightToLeft,
                Padding = new Padding(5)
            };

            _saveButton = new Button { Text = "حفظ", Width = 100, Height = 35, BackColor = ThemeManager.SuccessGreen, ForeColor = Color.White };
            _saveButton.Click += SaveButton_Click;
            buttonsPanel.Controls.Add(_saveButton);

            _deleteButton = new Button { Text = "حذف", Width = 100, Height = 35, BackColor = ThemeManager.ErrorRed, ForeColor = Color.White };
            _deleteButton.Click += DeleteButton_Click;
            buttonsPanel.Controls.Add(_deleteButton);

            _newButton = new Button { Text = "جديد", Width = 100, Height = 35, BackColor = ThemeManager.SecondarySkyBlue, ForeColor = Color.White };
            _newButton.Click += NewButton_Click;
            buttonsPanel.Controls.Add(_newButton);

            _generateScheduleButton = new Button { Text = "توليد جدول آلي", Width = 120, Height = 35, BackColor = ThemeManager.SecondarySkyBlue, ForeColor = Color.White };
            _generateScheduleButton.Click += GenerateScheduleButton_Click;
            buttonsPanel.Controls.Add(_generateScheduleButton);

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
                Text = "جدول الصيانة - Maintenance Schedule List",
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
            _searchTextBox.TextChanged += (s, e) => LoadSchedules();
            filterPanel.Controls.Add(_searchTextBox);

            filterPanel.Controls.Add(new Label { Text = "الحالة:", TextAlign = ContentAlignment.MiddleCenter, AutoSize = true, Margin = new Padding(5) });
            _filterStatusComboBox = new ComboBox { Width = 120, DropDownStyle = ComboBoxStyle.DropDownList };
            _filterStatusComboBox.Items.AddRange(new object[] { "الكل", "مجدولة", "قيد التنفيذ", "مكتملة", "متأخرة", "ملغاة" });
            _filterStatusComboBox.SelectedIndex = 0;
            _filterStatusComboBox.SelectedIndexChanged += (s, e) => LoadSchedules();
            filterPanel.Controls.Add(_filterStatusComboBox);

            filterPanel.Controls.Add(new Label { Text = "المعدة:", TextAlign = ContentAlignment.MiddleCenter, AutoSize = true, Margin = new Padding(5) });
            _filterEquipmentComboBox = new ComboBox { Width = 200, DropDownStyle = ComboBoxStyle.DropDownList };
            _filterEquipmentComboBox.Items.Add(new { Id = 0, Name = "الكل" });
            _filterEquipmentComboBox.DisplayMember = "Name";
            _filterEquipmentComboBox.ValueMember = "Id";
            _filterEquipmentComboBox.SelectedIndex = 0;
            _filterEquipmentComboBox.SelectedIndexChanged += (s, e) => LoadSchedules();
            filterPanel.Controls.Add(_filterEquipmentComboBox);

            // Grid
            _scheduleGrid = new DataGridView
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

            _scheduleGrid.Columns.Add(new DataGridViewTextBoxColumn { Name = "Id", HeaderText = "Id", DataPropertyName = "Id", Width = 50, Visible = false });
            _scheduleGrid.Columns.Add(new DataGridViewTextBoxColumn { Name = "EquipmentName", HeaderText = "المعدة", DataPropertyName = "EquipmentName", Width = 150 });
            _scheduleGrid.Columns.Add(new DataGridViewTextBoxColumn { Name = "MaintenanceType", HeaderText = "نوع الصيانة", DataPropertyName = "MaintenanceType", Width = 150 });
            _scheduleGrid.Columns.Add(new DataGridViewTextBoxColumn { Name = "Frequency", HeaderText = "التكرار", DataPropertyName = "Frequency", Width = 100 });
            _scheduleGrid.Columns.Add(new DataGridViewTextBoxColumn { Name = "LastMaintenanceDate", HeaderText = "أخر صيانة", DataPropertyName = "LastMaintenanceDate", Width = 100, DefaultCellStyle = new DataGridViewCellStyle { Format = "yyyy-MM-dd" } });
            _scheduleGrid.Columns.Add(new DataGridViewTextBoxColumn { Name = "NextMaintenanceDate", HeaderText = "الصيانة القادمة", DataPropertyName = "NextMaintenanceDate", Width = 110, DefaultCellStyle = new DataGridViewCellStyle { Format = "yyyy-MM-dd" } });
            _scheduleGrid.Columns.Add(new DataGridViewTextBoxColumn { Name = "Priority", HeaderText = "الأولوية", DataPropertyName = "Priority", Width = 80 });
            _scheduleGrid.Columns.Add(new DataGridViewTextBoxColumn { Name = "EstimatedCost", HeaderText = "التكلفة المقدرة", DataPropertyName = "EstimatedCost", Width = 100, DefaultCellStyle = new DataGridViewCellStyle { Format = "N2" } });
            _scheduleGrid.Columns.Add(new DataGridViewTextBoxColumn { Name = "EstimatedDurationMinutes", HeaderText = "المدة (دقيقة)", DataPropertyName = "EstimatedDurationMinutes", Width = 90 });
            _scheduleGrid.Columns.Add(new DataGridViewTextBoxColumn { Name = "AssignedTo", HeaderText = "المسؤول", DataPropertyName = "AssignedTo", Width = 120 });
            _scheduleGrid.Columns.Add(new DataGridViewTextBoxColumn { Name = "Status", HeaderText = "الحالة", DataPropertyName = "Status", Width = 100 });
            _scheduleGrid.Columns.Add(new DataGridViewCheckBoxColumn { Name = "IsActive", HeaderText = "نشط", DataPropertyName = "IsActive", Width = 60 });

            _scheduleGrid.SelectionChanged += ScheduleGrid_SelectionChanged;

            groupBox.Controls.Add(_scheduleGrid);
            groupBox.Controls.Add(filterPanel);

            panel.Controls.Add(groupBox);

            return panel;
        }

        private void LoadEquipment()
        {
            var equipment = _context.Equipment.OrderBy(e => e.Name).ToList();
            
            // Main equipment combobox - use DataSource for proper binding
            _equipmentComboBox.DataSource = null;
            _equipmentComboBox.DisplayMember = "Name";
            _equipmentComboBox.ValueMember = "Id";
            _equipmentComboBox.DataSource = equipment;

            // Load filter equipment
            var filterEquipment = new List<object> { new { Id = 0, Name = "الكل" } };
            filterEquipment.AddRange(equipment.Select(eq => new { Id = eq.Id, Name = eq.Name }));
            
            _filterEquipmentComboBox.DataSource = null;
            _filterEquipmentComboBox.DisplayMember = "Name";
            _filterEquipmentComboBox.ValueMember = "Id";
            _filterEquipmentComboBox.DataSource = filterEquipment;
            _filterEquipmentComboBox.SelectedIndex = 0;
        }

        private void LoadSchedules()
        {
            // Temporarily disable SelectionChanged event to prevent errors during loading
            _scheduleGrid.SelectionChanged -= ScheduleGrid_SelectionChanged;
            
            try
            {
                var query = _context.MaintenanceSchedules
                    .Include(s => s.Equipment)
                    .AsQueryable();

                // Apply filters
                if (!string.IsNullOrWhiteSpace(_searchTextBox.Text))
                {
                    var search = _searchTextBox.Text.ToLower();
                    query = query.Where(s => s.Equipment.Name.ToLower().Contains(search) ||
                                            s.MaintenanceType.ToLower().Contains(search) ||
                                            (s.AssignedTo != null && s.AssignedTo.ToLower().Contains(search)));
                }

                if (_filterStatusComboBox.SelectedIndex > 0)
                {
                    var status = _filterStatusComboBox.SelectedItem?.ToString();
                    query = query.Where(s => s.Status == status);
                }

                if (_filterEquipmentComboBox.SelectedIndex > 0 && _filterEquipmentComboBox.SelectedValue != null)
                {
                    var equipmentId = _filterEquipmentComboBox.SelectedValue is int id ? id : 0;
                    query = query.Where(s => s.EquipmentId == equipmentId);
                }

                var schedules = query.OrderBy(s => s.NextMaintenanceDate).ToList();

                // Create anonymous type for display
                var displayData = schedules.Select(s => new
                {
                    s.Id,
                    EquipmentName = s.Equipment.Name,
                    s.MaintenanceType,
                    s.Frequency,
                    s.LastMaintenanceDate,
                    s.NextMaintenanceDate,
                    s.Priority,
                    s.EstimatedCost,
                    s.EstimatedDurationMinutes,
                    s.AssignedTo,
                    s.Status,
                    s.IsActive
                }).ToList();

                _scheduleGrid.DataSource = displayData;

                // Highlight overdue schedules
                foreach (DataGridViewRow row in _scheduleGrid.Rows)
                {
                    if (row.Cells["NextMaintenanceDate"] != null && row.Cells["NextMaintenanceDate"].Value != null)
                    {
                        var nextDate = row.Cells["NextMaintenanceDate"].Value as DateTime?;
                        if (nextDate.HasValue && nextDate.Value < DateTime.Now)
                        {
                            row.DefaultCellStyle.BackColor = Color.FromArgb(255, 230, 230);
                        }
                    }
                }
            }
            finally
            {
                // Re-enable SelectionChanged event
                _scheduleGrid.SelectionChanged += ScheduleGrid_SelectionChanged;
            }
        }

        private void ScheduleGrid_SelectionChanged(object? sender, EventArgs e)
        {
            try
            {
                // Early return if context is not ready
                if (_context == null)
                    return;

                if (_scheduleGrid.SelectedRows.Count > 0)
                {
                    var selectedRow = _scheduleGrid.SelectedRows[0];
                    
                    // Check if Id cell exists and has value
                    if (selectedRow.Cells["Id"] == null || selectedRow.Cells["Id"].Value == null)
                        return;
                    
                    _selectedScheduleId = (int)selectedRow.Cells["Id"].Value;

                    var schedule = _context.MaintenanceSchedules
                        .Include(s => s.Equipment)
                        .FirstOrDefault(s => s.Id == _selectedScheduleId);

                    if (schedule != null)
                    {
                        // Ensure ALL controls are initialized before setting values
                        if (_equipmentComboBox == null || _maintenanceTypeTextBox == null || 
                            _frequencyComboBox == null || _frequencyDaysNumeric == null ||
                            _lastMaintenanceDatePicker == null || _nextMaintenanceDatePicker == null ||
                            _priorityComboBox == null || _estimatedCostNumeric == null ||
                            _estimatedDurationNumeric == null || _assignedToTextBox == null ||
                            _requiredPartsTextBox == null || _maintenanceInstructionsTextBox == null ||
                            _statusComboBox == null || _sendNotificationCheckBox == null ||
                            _notificationDaysNumeric == null || _isActiveCheckBox == null ||
                            _notesTextBox == null)
                            return;

                        // Set equipment using SelectedValue instead of looping through Items
                        if (_equipmentComboBox.DataSource != null)
                        {
                            _equipmentComboBox.SelectedValue = schedule.EquipmentId;
                        }

                        _maintenanceTypeTextBox.Text = schedule.MaintenanceType;
                        _frequencyComboBox.Text = schedule.Frequency;
                        _frequencyDaysNumeric.Value = schedule.FrequencyDays ?? 30;
                        _lastMaintenanceDatePicker.Value = schedule.LastMaintenanceDate ?? DateTime.Now;
                        _nextMaintenanceDatePicker.Value = schedule.NextMaintenanceDate ?? DateTime.Now.AddDays(30);
                        _priorityComboBox.SelectedIndex = schedule.Priority - 1;
                        _estimatedCostNumeric.Value = schedule.EstimatedCost ?? 0;
                        _estimatedDurationNumeric.Value = schedule.EstimatedDurationMinutes.HasValue ? schedule.EstimatedDurationMinutes.Value : 0;
                        _assignedToTextBox.Text = schedule.AssignedTo ?? "";
                        _requiredPartsTextBox.Text = schedule.RequiredParts ?? "";
                        _maintenanceInstructionsTextBox.Text = schedule.MaintenanceInstructions ?? "";
                        _statusComboBox.Text = schedule.Status;
                        _sendNotificationCheckBox.Checked = schedule.SendNotification;
                        _notificationDaysNumeric.Value = schedule.NotificationDaysBefore ?? 7;
                        _isActiveCheckBox.Checked = schedule.IsActive;
                        _notesTextBox.Text = schedule.Notes ?? "";
                    }
                }
            }
            catch (Exception ex)
            {
                // Silently handle initialization-time errors
                // Only log if this is not during form initialization
                if (_maintenanceTypeTextBox != null)
                {
                    MessageBox.Show($"خطأ في تحميل بيانات الجدولة: {ex.Message}", "خطأ", 
                        MessageBoxButtons.OK, MessageBoxIcon.Error);
                }
            }
        }

        private void FrequencyComboBox_SelectedIndexChanged(object? sender, EventArgs e)
        {
            var frequency = _frequencyComboBox.SelectedItem?.ToString();
            switch (frequency)
            {
                case "يومي":
                    _frequencyDaysNumeric.Value = 1;
                    break;
                case "أسبوعي":
                    _frequencyDaysNumeric.Value = 7;
                    break;
                case "شهري":
                    _frequencyDaysNumeric.Value = 30;
                    break;
                case "ربع سنوي":
                    _frequencyDaysNumeric.Value = 90;
                    break;
                case "نصف سنوي":
                    _frequencyDaysNumeric.Value = 180;
                    break;
                case "سنوي":
                    _frequencyDaysNumeric.Value = 365;
                    break;
            }
        }

        private void LastMaintenanceDatePicker_ValueChanged(object? sender, EventArgs e)
        {
            _nextMaintenanceDatePicker.Value = _lastMaintenanceDatePicker.Value.AddDays((double)_frequencyDaysNumeric.Value);
        }

        private void SaveButton_Click(object? sender, EventArgs e)
        {
            // Validation
            if (_equipmentComboBox.SelectedItem == null || _equipmentComboBox.SelectedValue == null)
            {
                MessageBox.Show("الرجاء اختيار المعدة\nPlease select equipment", "تحذير", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            if (string.IsNullOrWhiteSpace(_maintenanceTypeTextBox.Text))
            {
                MessageBox.Show("الرجاء إدخال نوع الصيانة\nPlease enter maintenance type", "تحذير", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                _maintenanceTypeTextBox.Focus();
                return;
            }

            if (string.IsNullOrWhiteSpace(_frequencyComboBox.Text))
            {
                MessageBox.Show("الرجاء اختيار التكرار\nPlease select frequency", "تحذير", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                _frequencyComboBox.Focus();
                return;
            }

            if (string.IsNullOrWhiteSpace(_priorityComboBox.Text))
            {
                MessageBox.Show("الرجاء اختيار الأولوية\nPlease select priority", "تحذير", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                _priorityComboBox.Focus();
                return;
            }

            if (string.IsNullOrWhiteSpace(_statusComboBox.Text))
            {
                MessageBox.Show("الرجاء اختيار الحالة\nPlease select status", "تحذير", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                _statusComboBox.Focus();
                return;
            }

            try
            {
                MaintenanceSchedule? schedule;
                if (_selectedScheduleId > 0)
                {
                    schedule = _context.MaintenanceSchedules.Find(_selectedScheduleId);
                    if (schedule == null) 
                    {
                        MessageBox.Show("لم يتم العثور على الجدولة\nSchedule not found", "خطأ", MessageBoxButtons.OK, MessageBoxIcon.Error);
                        return;
                    }
                    schedule.ModifiedDate = DateTime.Now;
                }
                else
                {
                    schedule = new MaintenanceSchedule();
                    _context.MaintenanceSchedules.Add(schedule);
                }

                // Get equipment ID from SelectedValue (safer than casting SelectedItem)
                int equipmentId;
                if (_equipmentComboBox.SelectedValue is int selectedId)
                {
                    equipmentId = selectedId;
                }
                else
                {
                    MessageBox.Show("خطأ في اختيار المعدة\nError selecting equipment", "خطأ", MessageBoxButtons.OK, MessageBoxIcon.Error);
                    return;
                }

                schedule.EquipmentId = equipmentId;
                schedule.MaintenanceType = _maintenanceTypeTextBox.Text;
                schedule.Frequency = _frequencyComboBox.Text;
                schedule.FrequencyDays = (int?)_frequencyDaysNumeric.Value;
                schedule.LastMaintenanceDate = _lastMaintenanceDatePicker.Value;
                schedule.NextMaintenanceDate = _nextMaintenanceDatePicker.Value;
                schedule.Priority = _priorityComboBox.SelectedIndex + 1;
                schedule.EstimatedCost = _estimatedCostNumeric.Value;
                schedule.EstimatedDurationMinutes = (int)_estimatedDurationNumeric.Value;
                schedule.AssignedTo = _assignedToTextBox.Text;
                schedule.RequiredParts = _requiredPartsTextBox.Text;
                schedule.MaintenanceInstructions = _maintenanceInstructionsTextBox.Text;
                schedule.Status = _statusComboBox.Text;
                schedule.SendNotification = _sendNotificationCheckBox.Checked;
                schedule.NotificationDaysBefore = (int)_notificationDaysNumeric.Value;
                schedule.IsActive = _isActiveCheckBox.Checked;
                schedule.Notes = _notesTextBox.Text;

                // Update equipment's next maintenance date
                var equipment = _context.Equipment.Find(equipmentId);
                if (equipment != null)
                {
                    equipment.NextMaintenanceDate = schedule.NextMaintenanceDate;
                    equipment.MaintenanceIntervalDays = schedule.FrequencyDays ?? 30;
                }

                _context.SaveChanges();
                MessageBox.Show("تم حفظ الجدولة بنجاح", "نجاح", MessageBoxButtons.OK, MessageBoxIcon.Information);

                LoadSchedules();
                ClearForm();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"خطأ في الحفظ\nError saving: {ex.Message}", "خطأ", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void DeleteButton_Click(object? sender, EventArgs e)
        {
            if (_selectedScheduleId == 0)
            {
                MessageBox.Show("الرجاء اختيار جدولة للحذف", "تحذير", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            var result = MessageBox.Show("هل أنت متأكد من حذف هذه الجدولة؟", "تأكيد الحذف", MessageBoxButtons.YesNo, MessageBoxIcon.Question);
            if (result == DialogResult.Yes)
            {
                var schedule = _context.MaintenanceSchedules.Find(_selectedScheduleId);
                if (schedule != null)
                {
                    _context.MaintenanceSchedules.Remove(schedule);
                    _context.SaveChanges();
                    MessageBox.Show("تم حذف الجدولة بنجاح", "نجاح", MessageBoxButtons.OK, MessageBoxIcon.Information);

                    LoadSchedules();
                    ClearForm();
                }
            }
        }

        private void NewButton_Click(object? sender, EventArgs e)
        {
            ClearForm();
        }

        private void GenerateScheduleButton_Click(object? sender, EventArgs e)
        {
            var result = MessageBox.Show(
                "هل تريد توليد جدول صيانة آلي لجميع المعدات بناءً على فترات الصيانة المحددة؟",
                "توليد جدول آلي",
                MessageBoxButtons.YesNo,
                MessageBoxIcon.Question);

            if (result == DialogResult.Yes)
            {
                int created = 0;
                var equipmentList = _context.Equipment
                    .Where(e => e.MaintenanceIntervalDays > 0)
                    .ToList();

                foreach (var equipment in equipmentList)
                {
                    if (equipment.MaintenanceIntervalDays <= 0) continue;

                    // Check if schedule already exists
                    var existingSchedule = _context.MaintenanceSchedules
                        .Any(s => s.EquipmentId == equipment.Id && s.IsActive);

                    if (!existingSchedule)
                    {
                        var intervalDays = equipment.MaintenanceIntervalDays;
                        var schedule = new MaintenanceSchedule
                        {
                            EquipmentId = equipment.Id,
                            MaintenanceType = "صيانة دورية",
                            Frequency = GetFrequencyName(intervalDays),
                            FrequencyDays = intervalDays,
                            LastMaintenanceDate = equipment.LastMaintenanceDate ?? DateTime.Now,
                            NextMaintenanceDate = (equipment.LastMaintenanceDate ?? DateTime.Now).AddDays(intervalDays),
                            Priority = 2,
                            EstimatedCost = equipment.AnnualMaintenanceCost / (365 / intervalDays),
                            Status = "مجدولة",
                            SendNotification = true,
                            NotificationDaysBefore = 7,
                            IsActive = true
                        };

                        _context.MaintenanceSchedules.Add(schedule);
                        created++;
                    }
                }

                if (created > 0)
                {
                    _context.SaveChanges();
                    MessageBox.Show($"تم توليد {created} جدولة صيانة جديدة\n{created} schedules created", "نجاح", MessageBoxButtons.OK, MessageBoxIcon.Information);
                    LoadSchedules();
                }
                else
                {
                    MessageBox.Show(
                        "لم يتم توليد أي جدولة.\n" +
                        "الأسباب المحتملة:\n" +
                        "1. لا توجد معدات في النظام\n" +
                        "2. المعدات لا تحتوي على فترة صيانة محددة (MaintenanceIntervalDays)\n" +
                        "3. جميع المعدات لديها جدولات نشطة بالفعل\n\n" +
                        "No schedules generated.\n" +
                        "Possible reasons:\n" +
                        "1. No equipment in system\n" +
                        "2. Equipment has no maintenance interval set\n" +
                        "3. All equipment already has active schedules",
                        "تنبيه",
                        MessageBoxButtons.OK,
                        MessageBoxIcon.Information);
                }
            }
        }

        private string GetFrequencyName(int days)
        {
            if (days == 1) return "يومي";
            if (days == 7) return "أسبوعي";
            if (days == 30) return "شهري";
            if (days == 90) return "ربع سنوي";
            if (days == 180) return "نصف سنوي";
            if (days == 365) return "سنوي";
            return "مخصص";
        }

        private void ClearForm()
        {
            _selectedScheduleId = 0;
            _equipmentComboBox.SelectedIndex = -1;
            _maintenanceTypeTextBox.Clear();
            _frequencyComboBox.SelectedIndex = -1;
            _frequencyDaysNumeric.Value = 30;
            _lastMaintenanceDatePicker.Value = DateTime.Now;
            _nextMaintenanceDatePicker.Value = DateTime.Now.AddDays(30);
            _priorityComboBox.SelectedIndex = 1; // متوسطة
            _estimatedCostNumeric.Value = 0;
            _estimatedDurationNumeric.Value = 0;
            _assignedToTextBox.Clear();
            _requiredPartsTextBox.Clear();
            _maintenanceInstructionsTextBox.Clear();
            _statusComboBox.SelectedIndex = 0; // مجدولة
            _sendNotificationCheckBox.Checked = true;
            _notificationDaysNumeric.Value = 7;
            _isActiveCheckBox.Checked = true;
        }
    }
}
