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
    /// نموذج تسجيل الحضور والغياب للموظفين
    /// Attendance Recording Form for Employees
    /// </summary>
    public partial class AttendanceForm : Form
    {
        #region Fields

        private readonly FishFarmContext _context;
        private int? _currentAttendanceId;

        // UI Controls
        private DateTimePicker _attendanceDatePicker = null!;
        private ComboBox _employeeComboBox = null!;
        private DateTimePicker _checkInTimePicker = null!;
        private DateTimePicker _checkOutTimePicker = null!;
        private ComboBox _statusComboBox = null!;
        private NumericUpDown _workedHoursNumeric = null!;
        private NumericUpDown _overtimeHoursNumeric = null!;
        private NumericUpDown _lateMinutesNumeric = null!;
        private TextBox _notesTextBox = null!;
        private DataGridView _attendanceGrid = null!;
        
        // Filter Controls
        private DateTimePicker _filterFromDatePicker = null!;
        private DateTimePicker _filterToDatePicker = null!;
        private ComboBox _filterEmployeeComboBox = null!;
        private ComboBox _filterStatusComboBox = null!;

        // Action Buttons
        private Button _addButton = null!;
        private Button _updateButton = null!;
        private Button _deleteButton = null!;
        private Button _clearButton = null!;
        private Button _searchButton = null!;

        // Status Mappings
        private static readonly Dictionary<string, AttendanceStatus> StatusMap = new Dictionary<string, AttendanceStatus>
        {
            { "حاضر", AttendanceStatus.Present },
            { "غائب", AttendanceStatus.Absent },
            { "متأخر", AttendanceStatus.Late },
            { "في إجازة", AttendanceStatus.OnLeave },
            { "إجازة مرضية", AttendanceStatus.Sick },
            { "مستأذن", AttendanceStatus.Excused },
            { "عطلة", AttendanceStatus.Holiday },
            { "يوم راحة", AttendanceStatus.DayOff }
        };

        private static readonly Dictionary<AttendanceStatus, string> StatusDisplayMap = 
            StatusMap.ToDictionary(x => x.Value, x => x.Key);

        #endregion

        #region Constructor

        public AttendanceForm(FishFarmContext context)
        {
            _context = context ?? throw new ArgumentNullException(nameof(context));

            // Check Permissions
            if (!AuthenticationService.HasPermission(
                UserRole.Admin,
                UserRole.Manager,
                UserRole.HRStaff))
            {
                MessageBox.Show(
                    "ليس لديك صلاحية لإدارة الحضور والغياب",
                    "تحذير",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Warning);

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
            this.Text = "تسجيل الحضور والغياب";
            this.Size = new Size(1400, 800);
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

            // Title
            var titleLabel = new Label
            {
                Text = "نظام تسجيل الحضور والغياب",
                Location = new Point(20, 10),
                Size = new Size(1340, 40),
                Font = new Font("Cairo", 14F, FontStyle.Bold),
                ForeColor = Color.FromArgb(46, 92, 138),
                TextAlign = ContentAlignment.MiddleRight
            };
            mainPanel.Controls.Add(titleLabel);

            // Create Sections
            CreateInputSection(mainPanel, 60);
            CreateFilterSection(mainPanel, 360);
            CreateGridSection(mainPanel, 450);
            CreateButtonsSection(mainPanel);

            this.Controls.Add(mainPanel);
        }

        private void CreateInputSection(Panel parent, int startY)
        {
            var inputPanel = new GroupBox
            {
                Text = "بيانات الحضور",
                Location = new Point(20, startY),
                Size = new Size(1340, 280),
                Font = new Font("Cairo", 10F, FontStyle.Bold)
            };

            int col1X = 900;
            int col2X = 400;
            int labelWidth = 120;
            int controlWidth = 280;
            int y = 35;
            int spacing = 45;

            // Column 1
            // التاريخ
            var dateLabel = new Label { Text = "التاريخ:", Location = new Point(col1X + controlWidth, y), Size = new Size(labelWidth, 25), TextAlign = ContentAlignment.MiddleRight };
            inputPanel.Controls.Add(dateLabel);

            _attendanceDatePicker = new DateTimePicker
            {
                Location = new Point(col1X, y),
                Size = new Size(controlWidth, 30),
                Format = DateTimePickerFormat.Short,
                RightToLeft = RightToLeft.No,
                Font = new Font("Cairo", 10F)
            };
            _attendanceDatePicker.ValueChanged += AttendanceDate_Changed;
            inputPanel.Controls.Add(_attendanceDatePicker);

            y += spacing;

            // الموظف
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

            // وقت الدخول
            var checkInLabel = new Label { Text = "وقت الدخول:", Location = new Point(col1X + controlWidth, y), Size = new Size(labelWidth, 25), TextAlign = ContentAlignment.MiddleRight };
            inputPanel.Controls.Add(checkInLabel);

            _checkInTimePicker = new DateTimePicker
            {
                Location = new Point(col1X, y),
                Size = new Size(controlWidth, 30),
                Format = DateTimePickerFormat.Time,
                ShowUpDown = true,
                Font = new Font("Cairo", 10F)
            };
            _checkInTimePicker.ValueChanged += TimeChanged;
            inputPanel.Controls.Add(_checkInTimePicker);

            y += spacing;

            // وقت الخروج
            var checkOutLabel = new Label { Text = "وقت الخروج:", Location = new Point(col1X + controlWidth, y), Size = new Size(labelWidth, 25), TextAlign = ContentAlignment.MiddleRight };
            inputPanel.Controls.Add(checkOutLabel);

            _checkOutTimePicker = new DateTimePicker
            {
                Location = new Point(col1X, y),
                Size = new Size(controlWidth, 30),
                Format = DateTimePickerFormat.Time,
                ShowUpDown = true,
                Font = new Font("Cairo", 10F)
            };
            _checkOutTimePicker.ValueChanged += TimeChanged;
            inputPanel.Controls.Add(_checkOutTimePicker);

            // Column 2
            y = 35;

            // الحالة
            var statusLabel = new Label { Text = "الحالة:", Location = new Point(col2X + controlWidth, y), Size = new Size(labelWidth, 25), TextAlign = ContentAlignment.MiddleRight };
            inputPanel.Controls.Add(statusLabel);

            _statusComboBox = new ComboBox
            {
                Location = new Point(col2X, y),
                Size = new Size(controlWidth, 30),
                DropDownStyle = ComboBoxStyle.DropDownList,
                Font = new Font("Cairo", 10F)
            };
            LoadAttendanceStatuses();
            inputPanel.Controls.Add(_statusComboBox);

            y += spacing;

            // ساعات العمل
            var workedHoursLabel = new Label { Text = "ساعات العمل:", Location = new Point(col2X + controlWidth, y), Size = new Size(labelWidth, 25), TextAlign = ContentAlignment.MiddleRight };
            inputPanel.Controls.Add(workedHoursLabel);

            _workedHoursNumeric = new NumericUpDown
            {
                Location = new Point(col2X, y),
                Size = new Size(controlWidth, 30),
                Font = new Font("Cairo", 10F),
                DecimalPlaces = 2,
                Minimum = 0,
                Maximum = 24,
                Value = 0,
                ReadOnly = true
            };
            inputPanel.Controls.Add(_workedHoursNumeric);

            y += spacing;

            // ساعات الإضافي
            var overtimeLabel = new Label { Text = "ساعات الإضافي:", Location = new Point(col2X + controlWidth, y), Size = new Size(labelWidth, 25), TextAlign = ContentAlignment.MiddleRight };
            inputPanel.Controls.Add(overtimeLabel);

            _overtimeHoursNumeric = new NumericUpDown
            {
                Location = new Point(col2X, y),
                Size = new Size(controlWidth, 30),
                Font = new Font("Cairo", 10F),
                DecimalPlaces = 2,
                Minimum = 0,
                Maximum = 12,
                Value = 0
            };
            inputPanel.Controls.Add(_overtimeHoursNumeric);

            y += spacing;

            // دقائق التأخير
            var lateLabel = new Label { Text = "دقائق التأخير:", Location = new Point(col2X + controlWidth, y), Size = new Size(labelWidth, 25), TextAlign = ContentAlignment.MiddleRight };
            inputPanel.Controls.Add(lateLabel);

            _lateMinutesNumeric = new NumericUpDown
            {
                Location = new Point(col2X, y),
                Size = new Size(controlWidth, 30),
                Font = new Font("Cairo", 10F),
                Minimum = 0,
                Maximum = 480,
                Value = 0
            };
            inputPanel.Controls.Add(_lateMinutesNumeric);

            // ملاحظات
            var notesLabel = new Label { Text = "ملاحظات:", Location = new Point(120, 205), Size = new Size(80, 25), TextAlign = ContentAlignment.MiddleRight };
            inputPanel.Controls.Add(notesLabel);

            _notesTextBox = new TextBox
            {
                Location = new Point(20, 205),
                Size = new Size(100, 60),
                Font = new Font("Cairo", 10F),
                Multiline = true,
                ScrollBars = ScrollBars.Vertical
            };
            inputPanel.Controls.Add(_notesTextBox);

            parent.Controls.Add(inputPanel);
        }

        private void CreateFilterSection(Panel parent, int startY)
        {
            var filterPanel = new GroupBox
            {
                Text = "البحث والفلترة",
                Location = new Point(20, startY),
                Size = new Size(1340, 80),
                Font = new Font("Cairo", 10F, FontStyle.Bold)
            };

            int x = 1080;

            // من تاريخ
            var fromLabel = new Label { Text = "من:", Location = new Point(x + 160, 30), Size = new Size(40, 25), TextAlign = ContentAlignment.MiddleRight };
            filterPanel.Controls.Add(fromLabel);

            _filterFromDatePicker = new DateTimePicker
            {
                Location = new Point(x, 30),
                Size = new Size(150, 30),
                Format = DateTimePickerFormat.Short,
                Font = new Font("Cairo", 9F)
            };
            filterPanel.Controls.Add(_filterFromDatePicker);

            x -= 220;

            // إلى تاريخ
            var toLabel = new Label { Text = "إلى:", Location = new Point(x + 160, 30), Size = new Size(40, 25), TextAlign = ContentAlignment.MiddleRight };
            filterPanel.Controls.Add(toLabel);

            _filterToDatePicker = new DateTimePicker
            {
                Location = new Point(x, 30),
                Size = new Size(150, 30),
                Format = DateTimePickerFormat.Short,
                Font = new Font("Cairo", 9F)
            };
            filterPanel.Controls.Add(_filterToDatePicker);

            x -= 320;

            // الموظف
            var empLabel = new Label { Text = "الموظف:", Location = new Point(x + 220, 30), Size = new Size(70, 25), TextAlign = ContentAlignment.MiddleRight };
            filterPanel.Controls.Add(empLabel);

            _filterEmployeeComboBox = new ComboBox
            {
                Location = new Point(x, 30),
                Size = new Size(200, 30),
                DropDownStyle = ComboBoxStyle.DropDownList,
                Font = new Font("Cairo", 9F)
            };
            filterPanel.Controls.Add(_filterEmployeeComboBox);

            x -= 230;

            // الحالة
            var statusLabel = new Label { Text = "الحالة:", Location = new Point(x + 160, 30), Size = new Size(60, 25), TextAlign = ContentAlignment.MiddleRight };
            filterPanel.Controls.Add(statusLabel);

            _filterStatusComboBox = new ComboBox
            {
                Location = new Point(x, 30),
                Size = new Size(150, 30),
                DropDownStyle = ComboBoxStyle.DropDownList,
                Font = new Font("Cairo", 9F)
            };
            filterPanel.Controls.Add(_filterStatusComboBox);

            parent.Controls.Add(filterPanel);
        }

        private void CreateGridSection(Panel parent, int startY)
        {
            _attendanceGrid = new DataGridView
            {
                Location = new Point(20, startY),
                Size = new Size(1340, 220),
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

            _attendanceGrid.CellDoubleClick += Grid_CellDoubleClick;

            parent.Controls.Add(_attendanceGrid);
        }

        private void CreateButtonsSection(Panel parent)
        {
            var buttonPanel = new Panel
            {
                Location = new Point(20, 680),
                Size = new Size(1340, 50),
                BackColor = Color.Transparent
            };

            int x = 1100;
            int buttonWidth = 100;
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
                _attendanceDatePicker.Value = DateTime.Today;
                _filterFromDatePicker.Value = DateTime.Today.AddDays(-30);
                _filterToDatePicker.Value = DateTime.Today;

                _checkInTimePicker.Value = DateTime.Today.AddHours(8);
                _checkOutTimePicker.Value = DateTime.Today.AddHours(17);

                _ = LoadEmployeesAsync();
                _ = LoadDataAsync();

                LoggingService.LogInfo("AttendanceForm initialized");
            }
            catch (Exception ex)
            {
                LoggingService.LogError("Error initializing AttendanceForm", ex);
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
                    .Select(e => new
                    {
                        e.Id,
                        DisplayName = e.FullName + " - " + e.EmployeeNumber
                    })
                    .ToListAsync();

                _employeeComboBox.DataSource = new List<dynamic>(employees);
                _employeeComboBox.DisplayMember = "DisplayName";
                _employeeComboBox.ValueMember = "Id";

                var allEmployees = new List<dynamic> { new { Id = 0, DisplayName = "الكل" } };
                allEmployees.AddRange(employees);
                _filterEmployeeComboBox.DataSource = allEmployees;
                _filterEmployeeComboBox.DisplayMember = "DisplayName";
                _filterEmployeeComboBox.ValueMember = "Id";

                if (_employeeComboBox != null && _employeeComboBox.Items.Count > 0)
                    _employeeComboBox.SelectedIndex = -1;
            }
            catch (Exception ex)
            {
                LoggingService.LogError("Error loading employees", ex);
                throw;
            }
        }

        private void LoadAttendanceStatuses()
        {
            var statuses = StatusMap.Select(s => new
            {
                Value = s.Value,
                Display = s.Key
            }).ToList();

            _statusComboBox.DataSource = new List<dynamic>(statuses);
            _statusComboBox.DisplayMember = "Display";
            _statusComboBox.ValueMember = "Value";

            var allStatuses = new List<dynamic> { new { Value = (AttendanceStatus?)null, Display = "الكل" } };
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
                var query = _context.Attendances
                    .Include(a => a.Employee)
                    .AsQueryable();

                var fromDate = _filterFromDatePicker.Value.Date;
                var toDate = _filterToDatePicker.Value.Date.AddDays(1).AddSeconds(-1);
                query = query.Where(a => a.Date >= fromDate && a.Date <= toDate);

                if (_filterEmployeeComboBox.SelectedValue is int empId && empId > 0)
                {
                    query = query.Where(a => a.EmployeeId == empId);
                }

                if (_filterStatusComboBox.SelectedValue is AttendanceStatus status)
                {
                    query = query.Where(a => a.Status == status);
                }

                var attendances = await query
                .OrderByDescending(a => a.Date)
                    .ThenBy(a => a.Employee!.FullName)
                    .Select(a => new
            {
                a.Id,
                التاريخ = a.Date.ToString("yyyy-MM-dd"),
                        الموظف = a.Employee!.FullName,
                الرقم_الوظيفي = a.Employee.EmployeeNumber,
                        وقت_الدخول = a.CheckInTime.HasValue ? a.CheckInTime.Value.ToString(@"hh\:mm") : "-",
                        وقت_الخروج = a.CheckOutTime.HasValue ? a.CheckOutTime.Value.ToString(@"hh\:mm") : "-",
                        ساعات_العمل = a.WorkedHours.HasValue ? a.WorkedHours.Value.ToString("0.00") : "0",
                        ساعات_الإضافي = a.OvertimeHours.HasValue ? a.OvertimeHours.Value.ToString("0.00") : "0",
                الحالة = StatusDisplayMap.ContainsKey(a.Status) ? StatusDisplayMap[a.Status] : a.Status.ToString(),
                        ملاحظات = a.Notes ?? ""
                    })
                    .ToListAsync();

                _attendanceGrid.DataSource = attendances;

                if (_attendanceGrid.Columns.Count > 0)
                {
                    _attendanceGrid.Columns["Id"].Visible = false;
                    
                    foreach (DataGridViewColumn column in _attendanceGrid.Columns)
                    {
                        column.DefaultCellStyle.Alignment = DataGridViewContentAlignment.MiddleCenter;
                    }
                }

                LoggingService.LogInfo($"Loaded {attendances.Count} attendance records");
            }
            catch (Exception ex)
            {
                LoggingService.LogError("Error loading attendance data", ex);
                MessageBox.Show($"حدث خطأ: {ex.Message}", "خطأ", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        #endregion

        #region Event Handlers

        private void AttendanceDate_Changed(object? sender, EventArgs e)
        {
            if (_employeeComboBox.SelectedValue is int employeeId && employeeId > 0)
            {
                CheckExistingAttendance(employeeId, _attendanceDatePicker.Value.Date);
            }
        }

        private void Employee_Changed(object? sender, EventArgs e)
        {
            if (_employeeComboBox.SelectedValue is int employeeId && employeeId > 0)
            {
                CheckExistingAttendance(employeeId, _attendanceDatePicker.Value.Date);
            }
        }

        private void TimeChanged(object? sender, EventArgs e)
        {
            CalculateWorkHours();
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

        private void CheckExistingAttendance(int employeeId, DateTime date)
        {
            var existing = _context.Attendances
                .FirstOrDefault(a => a.EmployeeId == employeeId && a.Date.Date == date.Date);

            if (existing != null && existing.Id != _currentAttendanceId)
            {
                MessageBox.Show(
                    "يوجد سجل حضور لهذا الموظف في هذا التاريخ",
                    "تنبيه",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Warning);
            }
        }

        private void CalculateWorkHours()
        {
            try
        {
            var checkIn = _checkInTimePicker.Value.TimeOfDay;
            var checkOut = _checkOutTimePicker.Value.TimeOfDay;

            if (checkOut > checkIn)
            {
                    var totalHours = (checkOut - checkIn).TotalHours;
                    var workedHours = Math.Min(totalHours, 8);
                    _workedHoursNumeric.Value = (decimal)workedHours;

                    var overtimeHours = Math.Max(0, totalHours - 8);
                    _overtimeHoursNumeric.Value = (decimal)overtimeHours;
                }
                else if (checkOut < checkIn)
                {
                    var totalHours = (TimeSpan.FromHours(24) - checkIn + checkOut).TotalHours;
                    var workedHours = Math.Min(totalHours, 8);
                    _workedHoursNumeric.Value = (decimal)workedHours;
                    var overtimeHours = Math.Max(0, totalHours - 8);
                    _overtimeHoursNumeric.Value = (decimal)overtimeHours;
                }
            }
            catch (Exception ex)
            {
                LoggingService.LogError("Error calculating work hours", ex);
            }
        }

        private void LoadRecordToEdit(int rowIndex)
        {
            try
            {
                var attendanceId = Convert.ToInt32(_attendanceGrid.Rows[rowIndex].Cells["Id"].Value);
                var attendance = _context.Attendances
                    .Include(a => a.Employee)
                    .FirstOrDefault(a => a.Id == attendanceId);

                if (attendance != null)
                {
                    _currentAttendanceId = attendance.Id;
                    _attendanceDatePicker.Value = attendance.Date;
                    _employeeComboBox.SelectedValue = attendance.EmployeeId;

                    if (attendance.CheckInTime.HasValue)
                    {
                        var checkInDt = attendance.CheckInTime.Value;
                        _checkInTimePicker.Value = new DateTime(DateTime.Today.Year, DateTime.Today.Month, DateTime.Today.Day,
                            checkInDt.Hour, checkInDt.Minute, checkInDt.Second);
                    }

                    if (attendance.CheckOutTime.HasValue)
                    {
                        var checkOutDt = attendance.CheckOutTime.Value;
                        _checkOutTimePicker.Value = new DateTime(DateTime.Today.Year, DateTime.Today.Month, DateTime.Today.Day,
                            checkOutDt.Hour, checkOutDt.Minute, checkOutDt.Second);
                    }

                    _statusComboBox.SelectedValue = attendance.Status;
                    _workedHoursNumeric.Value = attendance.WorkedHours ?? 0;
                    _overtimeHoursNumeric.Value = attendance.OvertimeHours ?? 0;
                    _lateMinutesNumeric.Value = attendance.LateMinutes ?? 0;
                    _notesTextBox.Text = attendance.Notes ?? "";

                    LoggingService.LogInfo($"Loaded attendance {attendanceId} for editing");
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
                var date = _attendanceDatePicker.Value.Date;

                var exists = await _context.Attendances
                    .AnyAsync(a => a.EmployeeId == employeeId && a.Date.Date == date);

                if (exists)
                {
                    MessageBox.Show("يوجد سجل حضور لهذا الموظف في هذا التاريخ", "تنبيه", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    return;
                }

                var checkIn = _checkInTimePicker.Value;
                var checkOut = _checkOutTimePicker.Value;

                var attendance = new Attendance
                {
                    EmployeeId = employeeId,
                    Date = date,
                    CheckInTime = new DateTime(date.Year, date.Month, date.Day, checkIn.Hour, checkIn.Minute, checkIn.Second),
                    CheckOutTime = new DateTime(date.Year, date.Month, date.Day, checkOut.Hour, checkOut.Minute, checkOut.Second),
                    Status = (AttendanceStatus)_statusComboBox.SelectedValue!,
                    WorkedHours = _workedHoursNumeric.Value,
                    OvertimeHours = _overtimeHoursNumeric.Value,
                    LateMinutes = (int)_lateMinutesNumeric.Value,
                    Notes = _notesTextBox.Text.Trim(),
                    CreatedAt = DateTime.Now
                };

                _context.Attendances.Add(attendance);
                await _context.SaveChangesAsync();

                // Audit logging
                LoggingService.LogInfo($"تسجيل حضور للموظف {_employeeComboBox.Text} بتاريخ {date:yyyy-MM-dd}");

                MessageBox.Show("تم إضافة سجل الحضور بنجاح", "نجح", MessageBoxButtons.OK, MessageBoxIcon.Information);

                ClearForm();
                await LoadDataAsync();

                LoggingService.LogInfo($"Added attendance for employee {employeeId}");
            }
            catch (Exception ex)
            {
                LoggingService.LogError("Error adding attendance", ex);
                MessageBox.Show($"حدث خطأ: {ex.Message}", "خطأ", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private async Task UpdateButton_ClickAsync()
        {
            try
            {
                if (_currentAttendanceId == null)
                {
                    MessageBox.Show("الرجاء اختيار سجل للتعديل", "تنبيه", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                        return;
                    }

                if (!ValidateInput())
                    return;

                var attendance = await _context.Attendances.FindAsync(_currentAttendanceId);

                if (attendance == null)
                {
                    MessageBox.Show("السجل غير موجود", "خطأ", MessageBoxButtons.OK, MessageBoxIcon.Error);
                    return;
                }

                var checkIn = _checkInTimePicker.Value;
                var checkOut = _checkOutTimePicker.Value;

                attendance.EmployeeId = (int)_employeeComboBox.SelectedValue!;
                attendance.Date = _attendanceDatePicker.Value.Date;
                attendance.CheckInTime = new DateTime(attendance.Date.Year, attendance.Date.Month, attendance.Date.Day, checkIn.Hour, checkIn.Minute, checkIn.Second);
                attendance.CheckOutTime = new DateTime(attendance.Date.Year, attendance.Date.Month, attendance.Date.Day, checkOut.Hour, checkOut.Minute, checkOut.Second);
                attendance.Status = (AttendanceStatus)_statusComboBox.SelectedValue!;
                attendance.WorkedHours = _workedHoursNumeric.Value;
                attendance.OvertimeHours = _overtimeHoursNumeric.Value;
                attendance.LateMinutes = (int)_lateMinutesNumeric.Value;
                attendance.Notes = _notesTextBox.Text.Trim();
                attendance.UpdatedAt = DateTime.Now;

                await _context.SaveChangesAsync();

                // Audit logging
                LoggingService.LogInfo($"تعديل سجل حضور بتاريخ {attendance.Date:yyyy-MM-dd}");

                MessageBox.Show("تم تعديل سجل الحضور بنجاح", "نجح", MessageBoxButtons.OK, MessageBoxIcon.Information);

                ClearForm();
                await LoadDataAsync();

                LoggingService.LogInfo($"Updated attendance {_currentAttendanceId}");
            }
            catch (Exception ex)
            {
                LoggingService.LogError("Error updating attendance", ex);
                MessageBox.Show($"حدث خطأ: {ex.Message}", "خطأ", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private async Task DeleteButton_ClickAsync()
        {
            try
            {
                if (_currentAttendanceId == null)
                {
                    MessageBox.Show("الرجاء اختيار سجل للحذف", "تنبيه", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

                var result = MessageBox.Show("هل أنت متأكد من حذف سجل الحضور؟", "تأكيد الحذف",
                    MessageBoxButtons.YesNo, MessageBoxIcon.Question);

                if (result != DialogResult.Yes)
                    return;

                var attendance = await _context.Attendances.FindAsync(_currentAttendanceId);

                    if (attendance != null)
                    {
                        _context.Attendances.Remove(attendance);
                    await _context.SaveChangesAsync();

                    // Audit logging
                    LoggingService.LogInfo($"حذف سجل حضور بتاريخ {attendance.Date:yyyy-MM-dd}");

                    MessageBox.Show("تم حذف سجل الحضور بنجاح", "نجح", MessageBoxButtons.OK, MessageBoxIcon.Information);

                    ClearForm();
                    await LoadDataAsync();

                    LoggingService.LogInfo($"Deleted attendance {_currentAttendanceId}");
                    }
                }
                catch (Exception ex)
                {
                LoggingService.LogError("Error deleting attendance", ex);
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

            if (_statusComboBox.SelectedValue == null)
            {
                MessageBox.Show("الرجاء اختيار الحالة", "تحذير", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                _statusComboBox.Focus();
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
            _currentAttendanceId = null;
            _attendanceDatePicker.Value = DateTime.Today;
            _employeeComboBox.SelectedIndex = -1;
            _checkInTimePicker.Value = DateTime.Today.AddHours(8);
            _checkOutTimePicker.Value = DateTime.Today.AddHours(17);
            if (_statusComboBox.Items.Count > 0)
                _statusComboBox.SelectedIndex = 0;
            _workedHoursNumeric.Value = 0;
            _overtimeHoursNumeric.Value = 0;
            _lateMinutesNumeric.Value = 0;
            _notesTextBox.Clear();
        }

        private void ApplyPermissions()
        {
            bool canModify = AuthenticationService.HasPermission(
                UserRole.Admin,
                UserRole.Manager,
                UserRole.HRStaff);

            bool canDelete = AuthenticationService.HasPermission(
                UserRole.Admin,
                UserRole.Manager);

            _addButton.Enabled = canModify;
            _updateButton.Enabled = canModify;
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
