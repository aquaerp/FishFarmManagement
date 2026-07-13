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
    /// نموذج إدارة الإجازات
    /// Leave Management Form
    /// </summary>
    public partial class LeaveManagementForm : Form
    {
        #region Fields

        private readonly FishFarmContext _context;
        private int? _currentLeaveId;

        // UI Controls
        private ComboBox _employeeComboBox = null!;
        private ComboBox _leaveTypeComboBox = null!;
        private DateTimePicker _startDatePicker = null!;
        private DateTimePicker _endDatePicker = null!;
        private NumericUpDown _daysNumeric = null!;
        private TextBox _reasonTextBox = null!;
        private ComboBox _statusComboBox = null!;
        private Label _availableLeaveLabel = null!;
        private DataGridView _leaveGrid = null!;

        // Filter Controls
        private ComboBox _filterEmployeeComboBox = null!;
        private ComboBox _filterTypeComboBox = null!;
        private ComboBox _filterStatusComboBox = null!;

        // Buttons
        private Button _addButton = null!;
        private Button _updateButton = null!;
        private Button _deleteButton = null!;
        private Button _approveButton = null!;
        private Button _rejectButton = null!;
        private Button _clearButton = null!;
        private Button _searchButton = null!;

        // Leave Type Mappings
        private static readonly Dictionary<string, LeaveType> LeaveTypeMap = new Dictionary<string, LeaveType>
        {
            { "إجازة سنوية", LeaveType.Annual },
            { "إجازة مرضية", LeaveType.Sick },
            { "إجازة شخصية", LeaveType.Personal },
            { "إجازة أمومة", LeaveType.Maternity },
            { "إجازة أبوية", LeaveType.Paternity },
            { "إجازة طوارئ", LeaveType.Emergency },
            { "إجازة بدون راتب", LeaveType.Unpaid },
            { "إجازة حج", LeaveType.Hajj },
            { "إجازة زواج", LeaveType.Marriage },
            { "إجازة عزاء", LeaveType.Bereavement },
            { "إجازة دراسية", LeaveType.Study },
            { "أخرى", LeaveType.Other }
        };

        private static readonly Dictionary<LeaveType, string> LeaveTypeDisplayMap =
            LeaveTypeMap.ToDictionary(x => x.Value, x => x.Key);

        private static readonly Dictionary<string, LeaveStatus> LeaveStatusMap = new Dictionary<string, LeaveStatus>
        {
            { "معلقة", LeaveStatus.Pending },
            { "موافق عليها", LeaveStatus.Approved },
            { "مرفوضة", LeaveStatus.Rejected },
            { "ملغاة", LeaveStatus.Cancelled }
        };

        private static readonly Dictionary<LeaveStatus, string> LeaveStatusDisplayMap =
            LeaveStatusMap.ToDictionary(x => x.Value, x => x.Key);

        #endregion

        #region Constructor

        public LeaveManagementForm(FishFarmContext context)
        {
            _context = context ?? throw new ArgumentNullException(nameof(context));

            if (!AuthenticationService.HasPermission(UserRole.Admin, UserRole.Manager, UserRole.HRStaff))
            {
                MessageBox.Show("ليس لديك صلاحية لإدارة الإجازات", "تحذير", MessageBoxButtons.OK, MessageBoxIcon.Warning);
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
            this.Text = "إدارة الإجازات";
            this.Size = new Size(1500, 850);
            this.StartPosition = FormStartPosition.CenterScreen;
            this.RightToLeft = RightToLeft.Yes;
            this.RightToLeftLayout = true;
            this.Font = new Font("Cairo", 10F);

            var mainPanel = new Panel
            {
                Dock = DockStyle.Fill,
                Padding = new Padding(20),
                BackColor = Color.FromArgb(240, 240, 240)
            };

            var titleLabel = new Label
            {
                Text = "نظام إدارة الإجازات",
                Location = new Point(20, 10),
                Size = new Size(1440, 40),
                Font = new Font("Cairo", 14F, FontStyle.Bold),
                ForeColor = Color.FromArgb(46, 92, 138),
                TextAlign = ContentAlignment.MiddleRight
            };
            mainPanel.Controls.Add(titleLabel);

            CreateInputSection(mainPanel, 60);
            CreateFilterSection(mainPanel, 360);
            CreateGridSection(mainPanel, 440);
            CreateButtonsSection(mainPanel);

            this.Controls.Add(mainPanel);
        }

        private void CreateInputSection(Panel parent, int startY)
        {
            var inputPanel = new GroupBox
            {
                Text = "بيانات الإجازة",
                Location = new Point(20, startY),
                Size = new Size(1440, 280),
                Font = new Font("Cairo", 10F, FontStyle.Bold)
            };

            int col1X = 1000;
            int col2X = 450;
            int labelWidth = 130;
            int controlWidth = 280;
            int y = 35;
            int spacing = 45;

            // Column 1
            var employeeLabel = new Label { Text = "الموظف:", Location = new Point(col1X + controlWidth, y), Size = new Size(labelWidth, 25), TextAlign = ContentAlignment.MiddleRight };
            inputPanel.Controls.Add(employeeLabel);

            _employeeComboBox = new ComboBox
            {
                Location = new Point(col1X, y),
                Size = new Size(controlWidth, 30),
                DropDownStyle = ComboBoxStyle.DropDownList,
                Font = new Font("Cairo", 10F),
                AutoCompleteMode = AutoCompleteMode.None,
                AutoCompleteSource = AutoCompleteSource.ListItems
            };
            _employeeComboBox.SelectedIndexChanged += Employee_Changed;
            inputPanel.Controls.Add(_employeeComboBox);

            y += spacing;

            var typeLabel = new Label { Text = "نوع الإجازة:", Location = new Point(col1X + controlWidth, y), Size = new Size(labelWidth, 25), TextAlign = ContentAlignment.MiddleRight };
            inputPanel.Controls.Add(typeLabel);

            _leaveTypeComboBox = new ComboBox
            {
                Location = new Point(col1X, y),
                Size = new Size(controlWidth, 30),
                DropDownStyle = ComboBoxStyle.DropDownList,
                Font = new Font("Cairo", 10F)
            };
            LoadLeaveTypes();
            inputPanel.Controls.Add(_leaveTypeComboBox);

            y += spacing;

            var startLabel = new Label { Text = "من تاريخ:", Location = new Point(col1X + controlWidth, y), Size = new Size(labelWidth, 25), TextAlign = ContentAlignment.MiddleRight };
            inputPanel.Controls.Add(startLabel);

            _startDatePicker = new DateTimePicker
            {
                Location = new Point(col1X, y),
                Size = new Size(controlWidth, 30),
                Format = DateTimePickerFormat.Short,
                Font = new Font("Cairo", 10F)
            };
            _startDatePicker.ValueChanged += DateChanged;
            inputPanel.Controls.Add(_startDatePicker);

            y += spacing;

            var endLabel = new Label { Text = "إلى تاريخ:", Location = new Point(col1X + controlWidth, y), Size = new Size(labelWidth, 25), TextAlign = ContentAlignment.MiddleRight };
            inputPanel.Controls.Add(endLabel);

            _endDatePicker = new DateTimePicker
            {
                Location = new Point(col1X, y),
                Size = new Size(controlWidth, 30),
                Format = DateTimePickerFormat.Short,
                Font = new Font("Cairo", 10F)
            };
            _endDatePicker.ValueChanged += DateChanged;
            inputPanel.Controls.Add(_endDatePicker);

            // Column 2
            y = 35;

            var daysLabel = new Label { Text = "عدد الأيام:", Location = new Point(col2X + controlWidth, y), Size = new Size(labelWidth, 25), TextAlign = ContentAlignment.MiddleRight };
            inputPanel.Controls.Add(daysLabel);

            _daysNumeric = new NumericUpDown
            {
                Location = new Point(col2X, y),
                Size = new Size(controlWidth, 30),
                Font = new Font("Cairo", 10F),
                Minimum = 1,
                Maximum = 365,
                Value = 1,
                ReadOnly = true
            };
            inputPanel.Controls.Add(_daysNumeric);

            y += spacing;

            var availableLabel = new Label { Text = "الرصيد المتاح:", Location = new Point(col2X + controlWidth, y), Size = new Size(labelWidth, 25), TextAlign = ContentAlignment.MiddleRight };
            inputPanel.Controls.Add(availableLabel);

            _availableLeaveLabel = new Label
            {
                Text = "0 يوم",
                Location = new Point(col2X, y),
                Size = new Size(controlWidth, 30),
                TextAlign = ContentAlignment.MiddleCenter,
                Font = new Font("Cairo", 11F, FontStyle.Bold),
                ForeColor = Color.FromArgb(38, 166, 154),
                BorderStyle = BorderStyle.FixedSingle,
                BackColor = Color.White
            };
            inputPanel.Controls.Add(_availableLeaveLabel);

            y += spacing;

            var statusLabel = new Label { Text = "الحالة:", Location = new Point(col2X + controlWidth, y), Size = new Size(labelWidth, 25), TextAlign = ContentAlignment.MiddleRight };
            inputPanel.Controls.Add(statusLabel);

            _statusComboBox = new ComboBox
            {
                Location = new Point(col2X, y),
                Size = new Size(controlWidth, 30),
                DropDownStyle = ComboBoxStyle.DropDownList,
                Font = new Font("Cairo", 10F)
            };
            LoadLeaveStatuses();
            inputPanel.Controls.Add(_statusComboBox);

            y += spacing;

            var reasonLabel = new Label { Text = "السبب:", Location = new Point(col2X + controlWidth, y), Size = new Size(labelWidth, 25), TextAlign = ContentAlignment.MiddleRight };
            inputPanel.Controls.Add(reasonLabel);

            _reasonTextBox = new TextBox
            {
                Location = new Point(col2X, y),
                Size = new Size(controlWidth, 80),
                Font = new Font("Cairo", 10F),
                Multiline = true,
                ScrollBars = ScrollBars.Vertical
            };
            inputPanel.Controls.Add(_reasonTextBox);

            parent.Controls.Add(inputPanel);
        }

        private void CreateFilterSection(Panel parent, int startY)
        {
            var filterPanel = new GroupBox
            {
                Text = "البحث والفلترة",
                Location = new Point(20, startY),
                Size = new Size(1440, 70),
                Font = new Font("Cairo", 10F, FontStyle.Bold)
            };

            int x = 1150;

            var empLabel = new Label { Text = "الموظف:", Location = new Point(x + 200, 28), Size = new Size(70, 25), TextAlign = ContentAlignment.MiddleRight };
            filterPanel.Controls.Add(empLabel);

            _filterEmployeeComboBox = new ComboBox
            {
                Location = new Point(x, 28),
                Size = new Size(190, 30),
                DropDownStyle = ComboBoxStyle.DropDownList,
                Font = new Font("Cairo", 9F)
            };
            filterPanel.Controls.Add(_filterEmployeeComboBox);

            x -= 250;

            var typeLabel = new Label { Text = "النوع:", Location = new Point(x + 150, 28), Size = new Size(60, 25), TextAlign = ContentAlignment.MiddleRight };
            filterPanel.Controls.Add(typeLabel);

            _filterTypeComboBox = new ComboBox
            {
                Location = new Point(x, 28),
                Size = new Size(140, 30),
                DropDownStyle = ComboBoxStyle.DropDownList,
                Font = new Font("Cairo", 9F)
            };
            filterPanel.Controls.Add(_filterTypeComboBox);

            x -= 220;

            var statusLabel = new Label { Text = "الحالة:", Location = new Point(x + 150, 28), Size = new Size(60, 25), TextAlign = ContentAlignment.MiddleRight };
            filterPanel.Controls.Add(statusLabel);

            _filterStatusComboBox = new ComboBox
            {
                Location = new Point(x, 28),
                Size = new Size(140, 30),
                DropDownStyle = ComboBoxStyle.DropDownList,
                Font = new Font("Cairo", 9F)
            };
            filterPanel.Controls.Add(_filterStatusComboBox);

            parent.Controls.Add(filterPanel);
        }

        private void CreateGridSection(Panel parent, int startY)
        {
            _leaveGrid = new DataGridView
            {
                Location = new Point(20, startY),
                Size = new Size(1440, 280),
                Font = new Font("Cairo", 9F),
                AllowUserToAddRows = false,
                AllowUserToDeleteRows = false,
                ReadOnly = true,
                MultiSelect = false,
                SelectionMode = DataGridViewSelectionMode.FullRowSelect,
                AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill,
                BackgroundColor = Color.White,
                BorderStyle = BorderStyle.Fixed3D
            };

            _leaveGrid.CellDoubleClick += Grid_CellDoubleClick;

            parent.Controls.Add(_leaveGrid);
        }

        private void CreateButtonsSection(Panel parent)
        {
            var buttonPanel = new Panel
            {
                Location = new Point(20, 730),
                Size = new Size(1440, 50),
                BackColor = Color.Transparent
            };

            int x = 1200;
            int buttonWidth = 110;
            int spacing = 10;

            _addButton = CreateButton("طلب إجازة", x, 10, buttonWidth);
            _addButton.Click += async (s, e) => await AddButton_ClickAsync();
            buttonPanel.Controls.Add(_addButton);

            x -= (buttonWidth + spacing);

            _updateButton = CreateButton("تعديل", x, 10, buttonWidth);
            _updateButton.Click += async (s, e) => await UpdateButton_ClickAsync();
            buttonPanel.Controls.Add(_updateButton);

            x -= (buttonWidth + spacing);

            _approveButton = CreateButton("موافقة", x, 10, buttonWidth);
            _approveButton.Click += async (s, e) => await ApproveButton_ClickAsync();
            _approveButton.BackColor = Color.FromArgb(38, 166, 154);
            buttonPanel.Controls.Add(_approveButton);

            x -= (buttonWidth + spacing);

            _rejectButton = CreateButton("رفض", x, 10, buttonWidth);
            _rejectButton.Click += async (s, e) => await RejectButton_ClickAsync();
            _rejectButton.BackColor = Color.FromArgb(229, 57, 53);
            buttonPanel.Controls.Add(_rejectButton);

            x -= (buttonWidth + spacing);

            _deleteButton = CreateButton("حذف", x, 10, buttonWidth);
            _deleteButton.Click += async (s, e) => await DeleteButton_ClickAsync();
            buttonPanel.Controls.Add(_deleteButton);

            x -= (buttonWidth + spacing);

            _clearButton = CreateButton("مسح", x, 10, buttonWidth);
            _clearButton.Click += ClearButton_Click;
            buttonPanel.Controls.Add(_clearButton);

            x -= (buttonWidth + spacing);

            _searchButton = CreateButton("بحث", x, 10, buttonWidth);
            _searchButton.Click += async (s, e) => await SearchButton_ClickAsync();
            buttonPanel.Controls.Add(_searchButton);

            parent.Controls.Add(buttonPanel);

            ApplyPermissions();
        }

        private Button CreateButton(string text, int x, int y, int width)
        {
            return new Button
            {
                Text = text,
                Location = new Point(x, y),
                Size = new Size(width, 35),
                Font = new Font("Cairo", 9F, FontStyle.Bold),
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
                _startDatePicker.Value = DateTime.Today;
                _endDatePicker.Value = DateTime.Today.AddDays(1);

                _ = LoadEmployeesAsync();
                _ = LoadDataAsync();

                LoggingService.LogInfo("LeaveManagementForm initialized");
            }
            catch (Exception ex)
            {
                LoggingService.LogError("Error initializing LeaveManagementForm", ex);
                MessageBox.Show($"حدث خطأ: {ex.Message}", "خطأ", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        #endregion

        #region Data Loading

        private async Task LoadEmployeesAsync()
        {
            try
            {
                var employees = await _context.Employees
                .Where(e => e.Status == EmployeeStatus.Active)
                .OrderBy(e => e.FullName)
                    .Select(e => new { e.Id, DisplayName = e.FullName + " - " + e.EmployeeNumber })
                    .ToListAsync();

                _employeeComboBox.DataSource = new List<dynamic>(employees);
                _employeeComboBox.DisplayMember = "DisplayName";
            _employeeComboBox.ValueMember = "Id";

                var allEmployees = new List<dynamic> { new { Id = 0, DisplayName = "الكل" } };
                allEmployees.AddRange(employees);
                _filterEmployeeComboBox.DataSource = allEmployees;
                _filterEmployeeComboBox.DisplayMember = "DisplayName";
            _filterEmployeeComboBox.ValueMember = "Id";

                if (_employeeComboBox.Items.Count > 0)
                    _employeeComboBox.SelectedIndex = -1;
            }
            catch (Exception ex)
            {
                LoggingService.LogError("Error loading employees", ex);
                throw;
            }
        }

        private void LoadLeaveTypes()
        {
            var types = LeaveTypeMap.Select(t => new { Value = t.Value, Display = t.Key }).ToList();

            _leaveTypeComboBox.DataSource = new List<dynamic>(types);
            _leaveTypeComboBox.DisplayMember = "Display";
            _leaveTypeComboBox.ValueMember = "Value";

            var allTypes = new List<dynamic> { new { Value = (LeaveType?)null, Display = "الكل" } };
            allTypes.AddRange(types);
            _filterTypeComboBox.DataSource = allTypes;
            _filterTypeComboBox.DisplayMember = "Display";
            _filterTypeComboBox.ValueMember = "Value";

            if (_leaveTypeComboBox.Items.Count > 0)
                _leaveTypeComboBox.SelectedIndex = 0;
        }

        private void LoadLeaveStatuses()
        {
            var statuses = LeaveStatusMap.Select(s => new { Value = s.Value, Display = s.Key }).ToList();

            _statusComboBox.DataSource = new List<dynamic>(statuses);
            _statusComboBox.DisplayMember = "Display";
            _statusComboBox.ValueMember = "Value";

            var allStatuses = new List<dynamic> { new { Value = (LeaveStatus?)null, Display = "الكل" } };
            allStatuses.AddRange(statuses);
            _filterStatusComboBox.DataSource = allStatuses;
            _filterStatusComboBox.DisplayMember = "Display";
            _filterStatusComboBox.ValueMember = "Value";

            if (_statusComboBox.Items.Count > 0)
                _statusComboBox.SelectedIndex = 0;
        }

        private async Task LoadDataAsync()
        {
            try
        {
            var query = _context.EmployeeLeaves
                .Include(l => l.Employee)
                .AsQueryable();

                if (_filterEmployeeComboBox.SelectedValue is int empId && empId > 0)
                {
                    query = query.Where(l => l.EmployeeId == empId);
                }

                if (_filterTypeComboBox.SelectedValue is LeaveType filterType)
                {
                    query = query.Where(l => l.LeaveType == filterType);
                }

                if (_filterStatusComboBox.SelectedValue is LeaveStatus filterStatus)
                {
                    query = query.Where(l => l.Status == filterStatus);
                }

                var leaves = await query
                    .OrderByDescending(l => l.StartDate)
                    .Select(l => new
                    {
                        l.Id,
                        الموظف = l.Employee!.FullName,
                        الرقم_الوظيفي = l.Employee.EmployeeNumber,
                        نوع_الإجازة = LeaveTypeDisplayMap.ContainsKey(l.LeaveType) ? LeaveTypeDisplayMap[l.LeaveType] : l.LeaveType.ToString(),
                        من_تاريخ = l.StartDate.ToString("yyyy-MM-dd"),
                        إلى_تاريخ = l.EndDate.ToString("yyyy-MM-dd"),
                        عدد_الأيام = l.DaysCount,
                        الحالة = LeaveStatusDisplayMap.ContainsKey(l.Status) ? LeaveStatusDisplayMap[l.Status] : l.Status.ToString(),
                        السبب = l.Reason ?? ""
                    })
                    .ToListAsync();

                _leaveGrid.DataSource = leaves;

                if (_leaveGrid.Columns.Count > 0)
                {
                    _leaveGrid.Columns["Id"].Visible = false;

                    foreach (DataGridViewColumn column in _leaveGrid.Columns)
                    {
                        column.DefaultCellStyle.Alignment = DataGridViewContentAlignment.MiddleCenter;
                    }

                    // Color code status
                    foreach (DataGridViewRow row in _leaveGrid.Rows)
                    {
                        var status = row.Cells["الحالة"].Value?.ToString();
                        if (status == "موافق عليها")
                            row.DefaultCellStyle.BackColor = Color.FromArgb(230, 255, 230);
                        else if (status == "مرفوضة")
                            row.DefaultCellStyle.BackColor = Color.FromArgb(255, 230, 230);
                        else if (status == "معلقة")
                            row.DefaultCellStyle.BackColor = Color.FromArgb(255, 250, 220);
                    }
                }

                LoggingService.LogInfo($"Loaded {leaves.Count} leave records");
            }
            catch (Exception ex)
            {
                LoggingService.LogError("Error loading leaves", ex);
                MessageBox.Show($"حدث خطأ: {ex.Message}", "خطأ", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        #endregion

        #region Event Handlers

        private async void Employee_Changed(object? sender, EventArgs e)
        {
            await UpdateAvailableLeave();
        }

        private void DateChanged(object? sender, EventArgs e)
        {
            CalculateDays();
        }

        private void Grid_CellDoubleClick(object? sender, DataGridViewCellEventArgs e)
        {
            if (e.RowIndex >= 0)
            {
                LoadRecordToEdit(e.RowIndex);
            }
        }

        #endregion

        #region Business Logic

        private void CalculateDays()
        {
            try
            {
                var start = _startDatePicker.Value.Date;
                var end = _endDatePicker.Value.Date;

                if (end >= start)
                {
                    int days = (end - start).Days + 1;
                    _daysNumeric.Value = days;
                }
                else
                {
                    _daysNumeric.Value = 0;
                    MessageBox.Show("تاريخ النهاية يجب أن يكون بعد تاريخ البداية", "تحذير",
                        MessageBoxButtons.OK, MessageBoxIcon.Warning);
                }
            }
            catch (Exception ex)
            {
                LoggingService.LogError("Error calculating days", ex);
            }
        }

        private async Task UpdateAvailableLeave()
        {
            try
            {
                if (_employeeComboBox.SelectedValue is int empId && empId > 0)
                {
                    var employee = await _context.Employees.FindAsync(empId);
                    if (employee != null)
                    {
                        var usedDays = await _context.EmployeeLeaves
                            .Where(l => l.EmployeeId == empId &&
                               l.Status == LeaveStatus.Approved &&
                               l.StartDate.Year == DateTime.Now.Year)
                            .SumAsync(l => l.DaysCount);

                        var available = employee.AnnualLeaveDays - usedDays;
                        _availableLeaveLabel.Text = $"{available} يوم";
                        _availableLeaveLabel.ForeColor = available > 0 
                            ? Color.FromArgb(38, 166, 154) 
                            : Color.FromArgb(229, 57, 53);
                    }
                }
            }
            catch (Exception ex)
            {
                LoggingService.LogError("Error updating available leave", ex);
            }
        }

        private void LoadRecordToEdit(int rowIndex)
        {
            try
            {
                var leaveId = Convert.ToInt32(_leaveGrid.Rows[rowIndex].Cells["Id"].Value);
                var leave = _context.EmployeeLeaves
                    .Include(l => l.Employee)
                    .FirstOrDefault(l => l.Id == leaveId);

                if (leave != null)
                {
                    _currentLeaveId = leave.Id;
                    _employeeComboBox.SelectedValue = leave.EmployeeId;
                    _leaveTypeComboBox.SelectedValue = leave.LeaveType;
                    _startDatePicker.Value = leave.StartDate;
                    _endDatePicker.Value = leave.EndDate;
                    _daysNumeric.Value = leave.DaysCount;
                    _statusComboBox.SelectedValue = leave.Status;
                    _reasonTextBox.Text = leave.Reason ?? "";

                    LoggingService.LogInfo($"Loaded leave {leaveId} for editing");
                }
            }
            catch (Exception ex)
            {
                LoggingService.LogError("Error loading record", ex);
                MessageBox.Show($"حدث خطأ: {ex.Message}", "خطأ", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        #endregion

        #region CRUD Operations

        private async Task AddButton_ClickAsync()
        {
            try
            {
                if (!ValidateInput())
                    return;

                var employeeId = (int)_employeeComboBox.SelectedValue!;

                var leave = new EmployeeLeave
                {
                    EmployeeId = employeeId,
                    LeaveNumber = $"LV-{DateTime.Now:yyyyMMdd}-{employeeId:D4}",
                    RequestDate = DateTime.Now,
                    RequestedBy = AuthenticationService.CurrentUser?.Username ?? "System",
                    LeaveType = (LeaveType)_leaveTypeComboBox.SelectedValue!,
                    StartDate = _startDatePicker.Value.Date,
                    EndDate = _endDatePicker.Value.Date,
                    DaysCount = (int)_daysNumeric.Value,
                    IsPaid = true,
                    Reason = _reasonTextBox.Text.Trim(),
                    Status = LeaveStatus.Pending,
                    CreatedAt = DateTime.Now
                };

                _context.EmployeeLeaves.Add(leave);
                await _context.SaveChangesAsync();

                LoggingService.LogInfo($"طلب إجازة للموظف {_employeeComboBox.Text}");

                MessageBox.Show("تم تقديم طلب الإجازة بنجاح", "نجح", MessageBoxButtons.OK, MessageBoxIcon.Information);

                ClearForm();
                await LoadDataAsync();
            }
            catch (Exception ex)
            {
                LoggingService.LogError("Error adding leave", ex);
                MessageBox.Show($"حدث خطأ: {ex.Message}", "خطأ", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private async Task UpdateButton_ClickAsync()
        {
            try
            {
                if (_currentLeaveId == null)
                {
                    MessageBox.Show("الرجاء اختيار طلب للتعديل", "تنبيه", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    return;
                }

                if (!ValidateInput())
                    return;

                var leave = await _context.EmployeeLeaves.FindAsync(_currentLeaveId);

                if (leave == null)
                {
                    MessageBox.Show("الطلب غير موجود", "خطأ", MessageBoxButtons.OK, MessageBoxIcon.Error);
                    return;
                }

                leave.EmployeeId = (int)_employeeComboBox.SelectedValue!;
                leave.LeaveType = (LeaveType)_leaveTypeComboBox.SelectedValue!;
                leave.StartDate = _startDatePicker.Value.Date;
                leave.EndDate = _endDatePicker.Value.Date;
                leave.DaysCount = (int)_daysNumeric.Value;
                leave.Reason = _reasonTextBox.Text.Trim();

                await _context.SaveChangesAsync();

                LoggingService.LogInfo($"تعديل طلب إجازة {_currentLeaveId}");

                MessageBox.Show("تم تعديل طلب الإجازة بنجاح", "نجح", MessageBoxButtons.OK, MessageBoxIcon.Information);

                ClearForm();
                await LoadDataAsync();
            }
            catch (Exception ex)
            {
                LoggingService.LogError("Error updating leave", ex);
                MessageBox.Show($"حدث خطأ: {ex.Message}", "خطأ", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private async Task ApproveButton_ClickAsync()
        {
            try
        {
            if (_currentLeaveId == null)
            {
                    MessageBox.Show("الرجاء اختيار طلب للموافقة", "تنبيه", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

                var leave = await _context.EmployeeLeaves.FindAsync(_currentLeaveId);

                if (leave != null)
                {
                    leave.Status = LeaveStatus.Approved;
                    leave.ApprovalDate = DateTime.Now;
                    leave.ApprovedBy = AuthenticationService.CurrentUser?.Username ?? "System";

                    await _context.SaveChangesAsync();

                    LoggingService.LogInfo($"الموافقة على طلب إجازة {_currentLeaveId}");

                    MessageBox.Show("تمت الموافقة على طلب الإجازة", "نجح", MessageBoxButtons.OK, MessageBoxIcon.Information);

                    ClearForm();
                    await LoadDataAsync();
                }
            }
            catch (Exception ex)
            {
                LoggingService.LogError("Error approving leave", ex);
                MessageBox.Show($"حدث خطأ: {ex.Message}", "خطأ", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private async Task RejectButton_ClickAsync()
        {
            try
            {
                if (_currentLeaveId == null)
                {
                    MessageBox.Show("الرجاء اختيار طلب للرفض", "تنبيه", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

                var leave = await _context.EmployeeLeaves.FindAsync(_currentLeaveId);

                if (leave != null)
            {
                    leave.Status = LeaveStatus.Rejected;
                leave.ApprovalDate = DateTime.Now;
                    leave.ApprovedBy = AuthenticationService.CurrentUser?.Username ?? "System";

                    await _context.SaveChangesAsync();

                    LoggingService.LogInfo($"رفض طلب إجازة {_currentLeaveId}");

                    MessageBox.Show("تم رفض طلب الإجازة", "تم", MessageBoxButtons.OK, MessageBoxIcon.Information);

                    ClearForm();
                    await LoadDataAsync();
                }
            }
            catch (Exception ex)
            {
                LoggingService.LogError("Error rejecting leave", ex);
                MessageBox.Show($"حدث خطأ: {ex.Message}", "خطأ", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private async Task DeleteButton_ClickAsync()
        {
            try
            {
                if (_currentLeaveId == null)
                {
                    MessageBox.Show("الرجاء اختيار طلب للحذف", "تنبيه", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

                var result = MessageBox.Show("هل أنت متأكد من حذف طلب الإجازة؟", "تأكيد الحذف",
                    MessageBoxButtons.YesNo, MessageBoxIcon.Question);

                if (result != DialogResult.Yes)
                    return;

                var leave = await _context.EmployeeLeaves.FindAsync(_currentLeaveId);

                if (leave != null)
                {
                    _context.EmployeeLeaves.Remove(leave);
                    await _context.SaveChangesAsync();

                    LoggingService.LogInfo($"حذف طلب إجازة {_currentLeaveId}");

                    MessageBox.Show("تم حذف طلب الإجازة بنجاح", "نجح", MessageBoxButtons.OK, MessageBoxIcon.Information);

                    ClearForm();
                    await LoadDataAsync();
                }
            }
            catch (Exception ex)
            {
                LoggingService.LogError("Error deleting leave", ex);
                MessageBox.Show($"حدث خطأ: {ex.Message}", "خطأ", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        #endregion

        #region Validation

        private bool ValidateInput()
        {
            if (_employeeComboBox.SelectedValue == null || (int)_employeeComboBox.SelectedValue <= 0)
            {
                MessageBox.Show("الرجاء اختيار الموظف", "تحذير", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                _employeeComboBox.Focus();
                return false;
            }

            if (_leaveTypeComboBox.SelectedValue == null)
            {
                MessageBox.Show("الرجاء اختيار نوع الإجازة", "تحذير", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                _leaveTypeComboBox.Focus();
                return false;
            }

            if (_endDatePicker.Value < _startDatePicker.Value)
            {
                MessageBox.Show("تاريخ النهاية يجب أن يكون بعد تاريخ البداية", "تحذير",
                    MessageBoxButtons.OK, MessageBoxIcon.Warning);
                _endDatePicker.Focus();
                return false;
            }

            if (string.IsNullOrWhiteSpace(_reasonTextBox.Text))
            {
                MessageBox.Show("الرجاء إدخال سبب الإجازة", "تحذير", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                _reasonTextBox.Focus();
                return false;
            }

            return true;
        }

        #endregion

        #region Helper Methods

        private void ClearButton_Click(object? sender, EventArgs e)
        {
            ClearForm();
        }

        private async Task SearchButton_ClickAsync()
        {
            await LoadDataAsync();
        }

        private void ClearForm()
        {
            _currentLeaveId = null;
            _employeeComboBox.SelectedIndex = -1;
            if (_leaveTypeComboBox.Items.Count > 0)
                _leaveTypeComboBox.SelectedIndex = 0;
            _startDatePicker.Value = DateTime.Today;
            _endDatePicker.Value = DateTime.Today.AddDays(1);
            _daysNumeric.Value = 1;
            if (_statusComboBox.Items.Count > 0)
                _statusComboBox.SelectedIndex = 0;
            _reasonTextBox.Clear();
            _availableLeaveLabel.Text = "0 يوم";
        }

        private void ApplyPermissions()
        {
            bool canModify = AuthenticationService.HasPermission(UserRole.Admin, UserRole.Manager, UserRole.HRStaff);
            bool canApprove = AuthenticationService.HasPermission(UserRole.Admin, UserRole.Manager);
            bool canDelete = AuthenticationService.HasPermission(UserRole.Admin, UserRole.Manager);

            _addButton.Enabled = canModify;
            _updateButton.Enabled = canModify;
            _approveButton.Enabled = canApprove;
            _rejectButton.Enabled = canApprove;
            _deleteButton.Enabled = canDelete;
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
