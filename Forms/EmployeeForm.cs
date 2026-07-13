using System;
using System.Linq;
using System.Windows.Forms;
using System.Drawing;
using Microsoft.EntityFrameworkCore;
using FishFarmManager.Data;
using FishFarmManager.Models;
using static FishFarmManager.Models.Employee;

namespace FishFarmManager.Forms
{
    public partial class EmployeeForm : Form
    {
        private readonly FishFarmContext _context;
        private int? _currentEmployeeId;

        // Mapping dictionaries for enum conversion
        private static readonly Dictionary<string, EmployeePosition> PositionMap = new Dictionary<string, EmployeePosition>
        {
            { "مدير عام", EmployeePosition.Manager },
            { "مدير مساعد", EmployeePosition.Assistant_Manager },
            { "مشرف", EmployeePosition.Supervisor },
            { "فني", EmployeePosition.Technician },
            { "عامل أحواض", EmployeePosition.Worker },
            { "حارس", EmployeePosition.Guard },
            { "سائق", EmployeePosition.Driver },
            { "محاسب", EmployeePosition.Accountant },
            { "سكرتير", EmployeePosition.Secretary },
            { "صيانة", EmployeePosition.Maintenance },
            { "مراقب جودة", EmployeePosition.Quality_Control },
            { "مبيعات", EmployeePosition.Sales },
            { "أخرى", EmployeePosition.Other }
        };

        private static readonly Dictionary<string, EmployeeDepartment> DepartmentMap = new Dictionary<string, EmployeeDepartment>
        {
            { "الإدارة", EmployeeDepartment.Management },
            { "الإنتاج", EmployeeDepartment.Production },
            { "الصيانة", EmployeeDepartment.Maintenance },
            { "المبيعات", EmployeeDepartment.Sales },
            { "المحاسبة", EmployeeDepartment.Accounting },
            { "مراقبة الجودة", EmployeeDepartment.Quality_Control },
            { "الأمن", EmployeeDepartment.Security },
            { "اللوجستيات", EmployeeDepartment.Logistics },
            { "أخرى", EmployeeDepartment.Other }
        };

        private static readonly Dictionary<string, EmploymentType> EmploymentTypeMap = new Dictionary<string, EmploymentType>
        {
            { "دوام كامل", EmploymentType.FullTime },
            { "دوام جزئي", EmploymentType.PartTime },
            { "عقد مؤقت", EmploymentType.Contract },
            { "يومي", EmploymentType.Daily },
            { "موسمي", EmploymentType.Seasonal }
        };

        private static readonly Dictionary<string, EmployeeStatus> StatusMap = new Dictionary<string, EmployeeStatus>
        {
            { "نشط", EmployeeStatus.Active },
            { "في إجازة", EmployeeStatus.OnLeave },
            { "موقوف", EmployeeStatus.Suspended },
            { "منتهي الخدمة", EmployeeStatus.Terminated }
        };

        // Reverse mappings for display
        private static readonly Dictionary<EmployeePosition, string> PositionDisplayMap = PositionMap.ToDictionary(x => x.Value, x => x.Key);
        private static readonly Dictionary<EmployeeDepartment, string> DepartmentDisplayMap = DepartmentMap.ToDictionary(x => x.Value, x => x.Key);
        private static readonly Dictionary<EmploymentType, string> EmploymentTypeDisplayMap = EmploymentTypeMap.ToDictionary(x => x.Value, x => x.Key);
        private static readonly Dictionary<EmployeeStatus, string> StatusDisplayMap = StatusMap.ToDictionary(x => x.Value, x => x.Key);

        // Form Controls
        private TextBox _employeeNumberTextBox = null!;
        private TextBox _fullNameTextBox = null!;
        private TextBox _nationalIdTextBox = null!;
        private DateTimePicker _birthDatePicker = null!;
        private ComboBox _genderComboBox = null!;
        private ComboBox _positionComboBox = null!;
        private ComboBox _departmentComboBox = null!;
        private ComboBox _employmentTypeComboBox = null!;
        private ComboBox _statusComboBox = null!;
        private DateTimePicker _hireDatePicker = null!;
        private DateTimePicker _contractStartDatePicker = null!;
        private DateTimePicker _contractEndDatePicker = null!;
        private NumericUpDown _basicSalaryNumeric = null!;
        private NumericUpDown _housingAllowanceNumeric = null!;
        private NumericUpDown _transportationAllowanceNumeric = null!;
        private NumericUpDown _foodAllowanceNumeric = null!;
        private NumericUpDown _otherAllowancesNumeric = null!;
        private Label _totalSalaryLabel = null!;
        private CheckBox _socialInsuranceCheckBox = null!;
        private TextBox _socialInsuranceNumberTextBox = null!;
        private NumericUpDown _socialInsurancePercentageNumeric = null!;
        private Label _socialInsuranceDeductionLabel = null!;
        private CheckBox _healthInsuranceCheckBox = null!;
        private TextBox _healthInsuranceNumberTextBox = null!;
        private NumericUpDown _annualLeaveDaysNumeric = null!;
        private NumericUpDown _usedLeaveDaysNumeric = null!;
        private NumericUpDown _sickLeaveDaysNumeric = null!;
        private Label _remainingLeaveDaysLabel = null!;
        private TextBox _phoneTextBox = null!;
        private TextBox _emailTextBox = null!;
        private TextBox _addressTextBox = null!;
        private TextBox _bankNameTextBox = null!;
        private TextBox _bankAccountNumberTextBox = null!;
        private TextBox _ibanTextBox = null!;
        private TextBox _emergencyContactNameTextBox = null!;
        private TextBox _emergencyContactPhoneTextBox = null!;
        private TextBox _emergencyContactRelationTextBox = null!;
        private TextBox _notesTextBox = null!;
        private DataGridView _employeesGrid = null!;
        private Button _saveButton = null!;
        private Button _deleteButton = null!;
        private Button _newButton = null!;
        private TextBox _searchTextBox = null!;
        private ComboBox _filterDepartmentComboBox = null!;
        private ComboBox _filterStatusComboBox = null!;

        public EmployeeForm(FishFarmContext context)
        {
            _context = context;
            InitializeComponent();
            LoadComboBoxData();
            LoadEmployees();
            SetupNewEmployee();
        }

        private void InitializeComponent()
        {
            this.Text = "إدارة الموظفين";
            this.Size = new Size(1400, 800);
            this.RightToLeft = RightToLeft.Yes;
            this.RightToLeftLayout = true;
            this.StartPosition = FormStartPosition.CenterScreen;

            // Main Layout
            var mainLayout = new TableLayoutPanel
            {
                Dock = DockStyle.Fill,
                ColumnCount = 2,
                RowCount = 1,
                Padding = new Padding(10)
            };
            mainLayout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 60F));
            mainLayout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 40F));

            // Right Panel - Employee List
            var rightPanel = CreateEmployeeListPanel();
            mainLayout.Controls.Add(rightPanel, 1, 0);

            // Left Panel - Employee Details
            var leftPanel = CreateEmployeeDetailsPanel();
            mainLayout.Controls.Add(leftPanel, 0, 0);

            this.Controls.Add(mainLayout);
        }

        private Panel CreateEmployeeListPanel()
        {
            var panel = new Panel { Dock = DockStyle.Fill };
            var layout = new TableLayoutPanel
            {
                Dock = DockStyle.Fill,
                RowCount = 4,
                ColumnCount = 1
            };
            layout.RowStyles.Add(new RowStyle(SizeType.Absolute, 40F));
            layout.RowStyles.Add(new RowStyle(SizeType.Absolute, 80F));
            layout.RowStyles.Add(new RowStyle(SizeType.Percent, 100F));
            layout.RowStyles.Add(new RowStyle(SizeType.Absolute, 50F));

            // Title
            var titleLabel = new Label
            {
                Text = "قائمة الموظفين",
                Font = new Font("Arial", 12, FontStyle.Bold),
                Dock = DockStyle.Fill,
                TextAlign = ContentAlignment.MiddleCenter
            };
            layout.Controls.Add(titleLabel, 0, 0);

            // Filter Panel
            var filterPanel = new Panel { Dock = DockStyle.Fill, Padding = new Padding(5) };
            var filterLayout = new TableLayoutPanel
            {
                Dock = DockStyle.Fill,
                ColumnCount = 2,
                RowCount = 3
            };
            filterLayout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 30F));
            filterLayout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 70F));

            filterLayout.Controls.Add(new Label { Text = "بحث:", TextAlign = ContentAlignment.MiddleRight, Dock = DockStyle.Fill }, 0, 0);
            _searchTextBox = new TextBox { Dock = DockStyle.Fill };
            _searchTextBox.TextChanged += (s, e) => LoadEmployees();
            filterLayout.Controls.Add(_searchTextBox, 1, 0);

            filterLayout.Controls.Add(new Label { Text = "القسم:", TextAlign = ContentAlignment.MiddleRight, Dock = DockStyle.Fill }, 0, 1);
            _filterDepartmentComboBox = new ComboBox { Dock = DockStyle.Fill, DropDownStyle = ComboBoxStyle.DropDownList };
            _filterDepartmentComboBox.SelectedIndexChanged += (s, e) => LoadEmployees();
            filterLayout.Controls.Add(_filterDepartmentComboBox, 1, 1);

            filterLayout.Controls.Add(new Label { Text = "الحالة:", TextAlign = ContentAlignment.MiddleRight, Dock = DockStyle.Fill }, 0, 2);
            _filterStatusComboBox = new ComboBox { Dock = DockStyle.Fill, DropDownStyle = ComboBoxStyle.DropDownList };
            _filterStatusComboBox.SelectedIndexChanged += (s, e) => LoadEmployees();
            filterLayout.Controls.Add(_filterStatusComboBox, 1, 2);

            filterPanel.Controls.Add(filterLayout);
            layout.Controls.Add(filterPanel, 0, 1);

            // Employees Grid
            _employeesGrid = new DataGridView
            {
                Dock = DockStyle.Fill,
                ReadOnly = true,
                AllowUserToAddRows = false,
                AllowUserToDeleteRows = false,
                SelectionMode = DataGridViewSelectionMode.FullRowSelect,
                MultiSelect = false,
                AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill
            };
            _employeesGrid.SelectionChanged += EmployeesGrid_SelectionChanged;
            layout.Controls.Add(_employeesGrid, 0, 2);

            // Buttons Panel
            var buttonsPanel = new FlowLayoutPanel
            {
                Dock = DockStyle.Fill,
                FlowDirection = FlowDirection.RightToLeft,
                Padding = new Padding(5)
            };

            _newButton = new Button { Text = "جديد", Width = 100, Height = 35 };
            _newButton.Click += NewButton_Click;
            buttonsPanel.Controls.Add(_newButton);

            _saveButton = new Button { Text = "حفظ", Width = 100, Height = 35 };
            _saveButton.Click += SaveButton_Click;
            buttonsPanel.Controls.Add(_saveButton);

            _deleteButton = new Button { Text = "حذف", Width = 100, Height = 35 };
            _deleteButton.Click += DeleteButton_Click;
            buttonsPanel.Controls.Add(_deleteButton);

            layout.Controls.Add(buttonsPanel, 0, 3);

            panel.Controls.Add(layout);
            return panel;
        }

        private Panel CreateEmployeeDetailsPanel()
        {
            var panel = new Panel { Dock = DockStyle.Fill, AutoScroll = true };
            var layout = new TableLayoutPanel
            {
                Dock = DockStyle.Top,
                ColumnCount = 4,
                AutoSize = true,
                Padding = new Padding(5)
            };
            layout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 15F));
            layout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 35F));
            layout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 15F));
            layout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 35F));

            int row = 0;

            // Title
            var titleLabel = new Label
            {
                Text = "بيانات الموظف",
                Font = new Font("Arial", 12, FontStyle.Bold),
                Dock = DockStyle.Fill,
                TextAlign = ContentAlignment.MiddleCenter,
                Margin = new Padding(0, 0, 0, 10)
            };
            layout.Controls.Add(titleLabel, 0, row);
            layout.SetColumnSpan(titleLabel, 4);
            row++;

            // Basic Information Section
            AddSectionHeader(layout, ref row, "المعلومات الأساسية");

            AddFormRow(layout, ref row, "رقم الموظف:", _employeeNumberTextBox = new TextBox { ReadOnly = true }, 
                      "الاسم الكامل:", _fullNameTextBox = new TextBox());

            AddFormRow(layout, ref row, "رقم الهوية:", _nationalIdTextBox = new TextBox { MaxLength = 10 },
                      "تاريخ الميلاد:", _birthDatePicker = new DateTimePicker { Format = DateTimePickerFormat.Short });

            AddFormRow(layout, ref row, "الجنس:", _genderComboBox = new ComboBox { DropDownStyle = ComboBoxStyle.DropDownList },
                      "الجوال:", _phoneTextBox = new TextBox());

            AddFormRow(layout, ref row, "البريد الإلكتروني:", _emailTextBox = new TextBox(), null, null);

            AddFormRow(layout, ref row, "العنوان:", _addressTextBox = new TextBox { Multiline = true, Height = 60 }, null, null);

            // Employment Information
            AddSectionHeader(layout, ref row, "معلومات التوظيف");

            AddFormRow(layout, ref row, "المنصب:", _positionComboBox = new ComboBox { DropDownStyle = ComboBoxStyle.DropDownList },
                      "القسم:", _departmentComboBox = new ComboBox { DropDownStyle = ComboBoxStyle.DropDownList });

            AddFormRow(layout, ref row, "نوع التوظيف:", _employmentTypeComboBox = new ComboBox { DropDownStyle = ComboBoxStyle.DropDownList },
                      "الحالة:", _statusComboBox = new ComboBox { DropDownStyle = ComboBoxStyle.DropDownList });

            AddFormRow(layout, ref row, "تاريخ التعيين:", _hireDatePicker = new DateTimePicker { Format = DateTimePickerFormat.Short },
                      "بداية العقد:", _contractStartDatePicker = new DateTimePicker { Format = DateTimePickerFormat.Short });

            AddFormRow(layout, ref row, "نهاية العقد:", _contractEndDatePicker = new DateTimePicker { Format = DateTimePickerFormat.Short }, null, null);

            // Salary Information
            AddSectionHeader(layout, ref row, "معلومات الراتب");

            _basicSalaryNumeric = new NumericUpDown { Maximum = 1000000, DecimalPlaces = 2, Dock = DockStyle.Fill };
            _basicSalaryNumeric.ValueChanged += CalculateTotalSalary;
            _housingAllowanceNumeric = new NumericUpDown { Maximum = 1000000, DecimalPlaces = 2, Dock = DockStyle.Fill };
            _housingAllowanceNumeric.ValueChanged += CalculateTotalSalary;
            AddFormRow(layout, ref row, "الراتب الأساسي:", _basicSalaryNumeric,
                      "بدل السكن:", _housingAllowanceNumeric);

            _transportationAllowanceNumeric = new NumericUpDown { Maximum = 1000000, DecimalPlaces = 2, Dock = DockStyle.Fill };
            _transportationAllowanceNumeric.ValueChanged += CalculateTotalSalary;
            _foodAllowanceNumeric = new NumericUpDown { Maximum = 1000000, DecimalPlaces = 2, Dock = DockStyle.Fill };
            _foodAllowanceNumeric.ValueChanged += CalculateTotalSalary;
            AddFormRow(layout, ref row, "بدل النقل:", _transportationAllowanceNumeric,
                      "بدل الطعام:", _foodAllowanceNumeric);

            _otherAllowancesNumeric = new NumericUpDown { Maximum = 1000000, DecimalPlaces = 2, Dock = DockStyle.Fill };
            _otherAllowancesNumeric.ValueChanged += CalculateTotalSalary;
            AddFormRow(layout, ref row, "بدلات أخرى:", _otherAllowancesNumeric, null, null);

            _totalSalaryLabel = new Label { Font = new Font("Arial", 10, FontStyle.Bold), ForeColor = Color.Green };
            AddFormRow(layout, ref row, "إجمالي الراتب:", _totalSalaryLabel, null, null);

            // Insurance Information
            AddSectionHeader(layout, ref row, "معلومات التأمينات");

            _socialInsuranceCheckBox = new CheckBox { Text = "مشترك في التأمينات الاجتماعية", Dock = DockStyle.Fill };
            _socialInsuranceCheckBox.CheckedChanged += (s, e) => {
                _socialInsuranceNumberTextBox.Enabled = _socialInsuranceCheckBox.Checked;
                _socialInsurancePercentageNumeric.Enabled = _socialInsuranceCheckBox.Checked;
                CalculateSocialInsuranceDeduction(null, null);
            };
            layout.Controls.Add(_socialInsuranceCheckBox, 0, row);
            layout.SetColumnSpan(_socialInsuranceCheckBox, 4);
            row++;

            AddFormRow(layout, ref row, "رقم التأمينات:", _socialInsuranceNumberTextBox = new TextBox { Enabled = false },
                      "نسبة الاستقطاع:", _socialInsurancePercentageNumeric = new NumericUpDown { 
                          Value = 9.75M, DecimalPlaces = 2, Minimum = 0, Maximum = 100, Enabled = false 
                      });
            _socialInsurancePercentageNumeric.ValueChanged += CalculateSocialInsuranceDeduction;

            _socialInsuranceDeductionLabel = new Label { Font = new Font("Arial", 9, FontStyle.Bold), ForeColor = Color.Red };
            AddFormRow(layout, ref row, "قيمة الاستقطاع:", _socialInsuranceDeductionLabel, null, null);

            _healthInsuranceCheckBox = new CheckBox { Text = "لديه تأمين صحي", Dock = DockStyle.Fill };
            _healthInsuranceCheckBox.CheckedChanged += (s, e) => _healthInsuranceNumberTextBox.Enabled = _healthInsuranceCheckBox.Checked;
            layout.Controls.Add(_healthInsuranceCheckBox, 0, row);
            layout.SetColumnSpan(_healthInsuranceCheckBox, 4);
            row++;

            AddFormRow(layout, ref row, "رقم التأمين الصحي:", _healthInsuranceNumberTextBox = new TextBox { Enabled = false }, null, null);

            // Leave Information
            AddSectionHeader(layout, ref row, "معلومات الإجازات");

            _annualLeaveDaysNumeric = new NumericUpDown { Maximum = 365, Dock = DockStyle.Fill, Value = 21 };
            _annualLeaveDaysNumeric.ValueChanged += CalculateRemainingLeave;
            _usedLeaveDaysNumeric = new NumericUpDown { Maximum = 365, Dock = DockStyle.Fill };
            _usedLeaveDaysNumeric.ValueChanged += CalculateRemainingLeave;
            AddFormRow(layout, ref row, "أيام الإجازة السنوية:", _annualLeaveDaysNumeric,
                      "الأيام المستخدمة:", _usedLeaveDaysNumeric);

            AddFormRow(layout, ref row, "أيام الإجازة المرضية:", _sickLeaveDaysNumeric = new NumericUpDown { Maximum = 365, Dock = DockStyle.Fill },
                      null, null);

            _remainingLeaveDaysLabel = new Label { Font = new Font("Arial", 10, FontStyle.Bold), ForeColor = Color.Blue };
            AddFormRow(layout, ref row, "الرصيد المتبقي:", _remainingLeaveDaysLabel, null, null);

            // Banking Information
            AddSectionHeader(layout, ref row, "المعلومات البنكية");

            AddFormRow(layout, ref row, "اسم البنك:", _bankNameTextBox = new TextBox(),
                      "رقم الحساب:", _bankAccountNumberTextBox = new TextBox());

            AddFormRow(layout, ref row, "رقم الآيبان:", _ibanTextBox = new TextBox { MaxLength = 24 }, null, null);

            // Emergency Contact
            AddSectionHeader(layout, ref row, "جهة الاتصال للطوارئ");

            AddFormRow(layout, ref row, "الاسم:", _emergencyContactNameTextBox = new TextBox(),
                      "الجوال:", _emergencyContactPhoneTextBox = new TextBox());

            AddFormRow(layout, ref row, "صلة القرابة:", _emergencyContactRelationTextBox = new TextBox(), null, null);

            // Notes
            AddSectionHeader(layout, ref row, "ملاحظات");

            layout.Controls.Add(new Label { Text = "ملاحظات:", TextAlign = ContentAlignment.TopRight, Dock = DockStyle.Fill }, 0, row);
            _notesTextBox = new TextBox { Multiline = true, Height = 80, Dock = DockStyle.Fill, ScrollBars = ScrollBars.Vertical };
            layout.Controls.Add(_notesTextBox, 1, row);
            layout.SetColumnSpan(_notesTextBox, 3);

            panel.Controls.Add(layout);
            return panel;
        }

        private void AddSectionHeader(TableLayoutPanel layout, ref int row, string title)
        {
            var label = new Label
            {
                Text = title,
                Font = new Font("Arial", 10, FontStyle.Bold),
                BackColor = Color.LightBlue,
                Dock = DockStyle.Fill,
                TextAlign = ContentAlignment.MiddleCenter,
                Padding = new Padding(5),
                Margin = new Padding(0, 10, 0, 5)
            };
            layout.Controls.Add(label, 0, row);
            layout.SetColumnSpan(label, 4);
            row++;
        }

        private void AddFormRow(TableLayoutPanel layout, ref int row, string? label1, Control? control1, string? label2, Control? control2)
        {
            if (label1 != null && control1 != null)
            {
                var lbl1 = new Label { Text = label1, TextAlign = ContentAlignment.MiddleRight, Dock = DockStyle.Fill };
                layout.Controls.Add(lbl1, 0, row);
                control1.Dock = DockStyle.Fill;
                layout.Controls.Add(control1, 1, row);

                if (label2 != null && control2 != null)
                {
                    var lbl2 = new Label { Text = label2, TextAlign = ContentAlignment.MiddleRight, Dock = DockStyle.Fill };
                    layout.Controls.Add(lbl2, 2, row);
                    control2.Dock = DockStyle.Fill;
                    layout.Controls.Add(control2, 3, row);
                }
                else
                {
                    layout.SetColumnSpan(control1, 3);
                }
            }
            row++;
        }

        private void LoadComboBoxData()
        {
            // Gender
            _genderComboBox.Items.AddRange(new[] { "ذكر", "أنثى" });

            // Position
            _positionComboBox.Items.AddRange(new[] {
                "مدير عام", "مدير عمليات", "مدير مالي", "محاسب", "مهندس استزراع سمكي",
                "فني استزراع", "عامل أحواض", "مراقب جودة", "سائق", "حارس أمن",
                "عامل نظافة", "موظف مبيعات", "موظف مشتريات"
            });

            // Department
            _departmentComboBox.Items.AddRange(new[] {
                "الإدارة", "العمليات", "المالية", "الإنتاج", "الجودة",
                "المبيعات", "المشتريات", "الصيانة", "الأمن"
            });

            // Employment Type
            _employmentTypeComboBox.Items.AddRange(new[] {
                "دوام كامل", "دوام جزئي", "عقد مؤقت", "موسمي", "تدريب"
            });

            // Status
            _statusComboBox.Items.AddRange(new[] {
                "نشط", "إجازة", "معلق", "منتهي الخدمة"
            });

            // Filter ComboBoxes
            _filterDepartmentComboBox.Items.Add("الكل");
            foreach (var item in _departmentComboBox.Items)
                _filterDepartmentComboBox.Items.Add(item);
            _filterDepartmentComboBox.SelectedIndex = 0;

            _filterStatusComboBox.Items.Add("الكل");
            foreach (var item in _statusComboBox.Items)
                _filterStatusComboBox.Items.Add(item);
            _filterStatusComboBox.SelectedIndex = 0;
        }

        private void LoadEmployees()
        {
            var query = _context.Employees.AsQueryable();

            // Apply filters
            if (!string.IsNullOrWhiteSpace(_searchTextBox.Text))
            {
                var searchTerm = _searchTextBox.Text.Trim();
                query = query.Where(e => e.FullName.Contains(searchTerm) ||
                                       e.EmployeeNumber.Contains(searchTerm) ||
                                       e.NationalId.Contains(searchTerm) ||
                                       (e.Phone != null && e.Phone.Contains(searchTerm)));
            }

            if (_filterDepartmentComboBox.SelectedIndex > 0)
            {
                var deptText = _filterDepartmentComboBox.SelectedItem?.ToString();
                if (deptText != null && DepartmentMap.ContainsKey(deptText))
                {
                    var deptEnum = DepartmentMap[deptText];
                    query = query.Where(e => e.Department == deptEnum);
                }
            }

            if (_filterStatusComboBox.SelectedIndex > 0)
            {
                var statusText = _filterStatusComboBox.SelectedItem?.ToString();
                if (statusText != null && StatusMap.ContainsKey(statusText))
                {
                    var statusEnum = StatusMap[statusText];
                    query = query.Where(e => e.Status == statusEnum);
                }
            }

            var employees = query.OrderByDescending(e => e.Id).ToList();

            _employeesGrid.DataSource = employees.Select(e => new
            {
                e.Id,
                رقم_الموظف = e.EmployeeNumber,
                الاسم = e.FullName,
                المنصب = e.Position,
                القسم = e.Department,
                الراتب = e.CalculateTotalSalary().ToString("N2"),
                الحالة = e.Status,
                تاريخ_التعيين = e.HireDate.ToString("yyyy-MM-dd")
            }).ToList();

            _employeesGrid.Columns["Id"].Visible = false;
        }

        private void EmployeesGrid_SelectionChanged(object? sender, EventArgs e)
        {
            if (_employeesGrid.SelectedRows.Count > 0)
            {
                var employeeId = (int)_employeesGrid.SelectedRows[0].Cells["Id"].Value;
                LoadEmployee(employeeId);
            }
        }

        private void LoadEmployee(int employeeId)
        {
            var employee = _context.Employees.Find(employeeId);
            if (employee == null) return;

            _currentEmployeeId = employee.Id;
            _employeeNumberTextBox.Text = employee.EmployeeNumber;
            _fullNameTextBox.Text = employee.FullName;
            _nationalIdTextBox.Text = employee.NationalId;
            _birthDatePicker.Value = employee.BirthDate;
            _genderComboBox.SelectedIndex = 0; // Gender is not in model
            _phoneTextBox.Text = employee.Phone;
            _emailTextBox.Text = employee.Email;
            _addressTextBox.Text = employee.Address;
            
            // Use display maps to show Arabic values
            _positionComboBox.SelectedItem = PositionDisplayMap.ContainsKey(employee.Position) 
                ? PositionDisplayMap[employee.Position] : "عامل أحواض";
            _departmentComboBox.SelectedItem = DepartmentDisplayMap.ContainsKey(employee.Department)
                ? DepartmentDisplayMap[employee.Department] : "الإنتاج";
            _employmentTypeComboBox.SelectedItem = EmploymentTypeDisplayMap.ContainsKey(employee.EmploymentType)
                ? EmploymentTypeDisplayMap[employee.EmploymentType] : "دوام كامل";
            _statusComboBox.SelectedItem = StatusDisplayMap.ContainsKey(employee.Status)
                ? StatusDisplayMap[employee.Status] : "نشط";
            
            _hireDatePicker.Value = employee.HireDate;
            _contractStartDatePicker.Value = DateTime.Now; // ContractStartDate not in model
            _contractEndDatePicker.Value = DateTime.Now.AddYears(1); // ContractEndDate not in model
            _basicSalaryNumeric.Value = employee.BasicSalary;
            _housingAllowanceNumeric.Value = employee.HousingAllowance.HasValue ? employee.HousingAllowance.Value : 0m;
            _transportationAllowanceNumeric.Value = employee.TransportationAllowance.HasValue ? employee.TransportationAllowance.Value : 0m;
            _foodAllowanceNumeric.Value = employee.FoodAllowance.HasValue ? employee.FoodAllowance.Value : 0m;
            _otherAllowancesNumeric.Value = employee.OtherAllowances.HasValue ? employee.OtherAllowances.Value : 0m;
            _socialInsuranceCheckBox.Checked = employee.SocialInsuranceEnrolled;
            _socialInsuranceNumberTextBox.Text = employee.SocialInsuranceNumber;
            _socialInsurancePercentageNumeric.Value = employee.SocialInsurancePercentage;
            _healthInsuranceCheckBox.Checked = employee.HealthInsuranceEnrolled;
            _healthInsuranceNumberTextBox.Text = ""; // HealthInsuranceNumber not in model
            _annualLeaveDaysNumeric.Value = employee.AnnualLeaveDays;
            _usedLeaveDaysNumeric.Value = employee.UsedLeaveDays;
            _sickLeaveDaysNumeric.Value = employee.SickLeaveDays;
            _bankNameTextBox.Text = employee.BankName;
            _bankAccountNumberTextBox.Text = employee.BankAccountNumber;
            _ibanTextBox.Text = employee.IBAN;
            _emergencyContactNameTextBox.Text = employee.EmergencyContactName;
            _emergencyContactPhoneTextBox.Text = employee.EmergencyContactPhone;
            _emergencyContactRelationTextBox.Text = employee.EmergencyContactRelation;
            _notesTextBox.Text = employee.Notes;

            CalculateTotalSalary(null, null);
            CalculateSocialInsuranceDeduction(null, null);
            CalculateRemainingLeave(null, null);
        }

        private void SetupNewEmployee()
        {
            _currentEmployeeId = null;
            _employeeNumberTextBox.Text = GenerateEmployeeNumber();
            _fullNameTextBox.Clear();
            _nationalIdTextBox.Clear();
            _birthDatePicker.Value = DateTime.Now.AddYears(-25);
            _genderComboBox.SelectedIndex = 0;
            _phoneTextBox.Clear();
            _emailTextBox.Clear();
            _addressTextBox.Clear();
            _positionComboBox.SelectedIndex = 0;
            _departmentComboBox.SelectedIndex = 0;
            _employmentTypeComboBox.SelectedIndex = 0;
            _statusComboBox.SelectedIndex = 0;
            _hireDatePicker.Value = DateTime.Now;
            _contractStartDatePicker.Value = DateTime.Now;
            _contractEndDatePicker.Value = DateTime.Now.AddYears(1);
            _basicSalaryNumeric.Value = 0;
            _housingAllowanceNumeric.Value = 0;
            _transportationAllowanceNumeric.Value = 0;
            _foodAllowanceNumeric.Value = 0;
            _otherAllowancesNumeric.Value = 0;
            _socialInsuranceCheckBox.Checked = true;
            _socialInsuranceNumberTextBox.Clear();
            _socialInsurancePercentageNumeric.Value = 9.75M;
            _healthInsuranceCheckBox.Checked = false;
            _healthInsuranceNumberTextBox.Clear();
            _annualLeaveDaysNumeric.Value = 21;
            _usedLeaveDaysNumeric.Value = 0;
            _sickLeaveDaysNumeric.Value = 0;
            _bankNameTextBox.Clear();
            _bankAccountNumberTextBox.Clear();
            _ibanTextBox.Clear();
            _emergencyContactNameTextBox.Clear();
            _emergencyContactPhoneTextBox.Clear();
            _emergencyContactRelationTextBox.Clear();
            _notesTextBox.Clear();

            _fullNameTextBox.Focus();
        }

        private string GenerateEmployeeNumber()
        {
            var lastEmployee = _context.Employees
                .OrderByDescending(e => e.Id)
                .FirstOrDefault();

            int nextNumber = 1;
            if (lastEmployee != null)
            {
                var parts = lastEmployee.EmployeeNumber.Split('-');
                if (parts.Length == 2 && int.TryParse(parts[1], out int lastNum))
                {
                    nextNumber = lastNum + 1;
                }
            }

            return $"EMP-{nextNumber:D4}";
        }

        private void CalculateTotalSalary(object? sender, EventArgs? e)
        {
            var total = _basicSalaryNumeric.Value +
                       _housingAllowanceNumeric.Value +
                       _transportationAllowanceNumeric.Value +
                       _foodAllowanceNumeric.Value +
                       _otherAllowancesNumeric.Value;
            _totalSalaryLabel.Text = $"{total:N2} ريال";
            CalculateSocialInsuranceDeduction(null, null);
        }

        private void CalculateSocialInsuranceDeduction(object? sender, EventArgs? e)
        {
            if (_socialInsuranceCheckBox.Checked)
            {
                var total = _basicSalaryNumeric.Value +
                           _housingAllowanceNumeric.Value;
                var deduction = total * (_socialInsurancePercentageNumeric.Value / 100);
                _socialInsuranceDeductionLabel.Text = $"{deduction:N2} ريال";
            }
            else
            {
                _socialInsuranceDeductionLabel.Text = "0.00 ريال";
            }
        }

        private void CalculateRemainingLeave(object? sender, EventArgs? e)
        {
            var remaining = _annualLeaveDaysNumeric.Value - _usedLeaveDaysNumeric.Value;
            _remainingLeaveDaysLabel.Text = $"{remaining} يوم";
            _remainingLeaveDaysLabel.ForeColor = remaining < 5 ? Color.Red : Color.Blue;
        }

        private void NewButton_Click(object? sender, EventArgs e)
        {
            SetupNewEmployee();
        }

        private void SaveButton_Click(object? sender, EventArgs e)
        {
            try
            {
                // Validation
                if (string.IsNullOrWhiteSpace(_fullNameTextBox.Text))
                {
                    MessageBox.Show("الرجاء إدخال اسم الموظف", "تحذير", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    _fullNameTextBox.Focus();
                    return;
                }

                if (string.IsNullOrWhiteSpace(_nationalIdTextBox.Text) || _nationalIdTextBox.Text.Length != 10)
                {
                    MessageBox.Show("الرجاء إدخال رقم هوية صحيح (10 أرقام)", "تحذير", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    _nationalIdTextBox.Focus();
                    return;
                }

                if (_positionComboBox.SelectedIndex < 0 || _departmentComboBox.SelectedIndex < 0)
                {
                    MessageBox.Show("الرجاء اختيار المنصب والقسم", "تحذير", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    return;
                }

                if (_basicSalaryNumeric.Value <= 0)
                {
                    MessageBox.Show("الرجاء إدخال الراتب الأساسي", "تحذير", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    _basicSalaryNumeric.Focus();
                    return;
                }

                Employee employee;
                if (_currentEmployeeId.HasValue)
                {
                    employee = _context.Employees.Find(_currentEmployeeId.Value)!;
                }
                else
                {
                    employee = new Employee();
                    _context.Employees.Add(employee);
                }

                employee.EmployeeNumber = _employeeNumberTextBox.Text;
                employee.FullName = _fullNameTextBox.Text;
                employee.NationalId = _nationalIdTextBox.Text;
                employee.BirthDate = _birthDatePicker.Value;
                // Gender is not in model
                employee.Phone = _phoneTextBox.Text;
                employee.Email = _emailTextBox.Text;
                employee.Address = _addressTextBox.Text;
                
                // Use maps to convert Arabic values to enums
                var positionText = _positionComboBox.SelectedItem?.ToString() ?? "عامل أحواض";
                employee.Position = PositionMap.ContainsKey(positionText) ? PositionMap[positionText] : EmployeePosition.Worker;
                
                var departmentText = _departmentComboBox.SelectedItem?.ToString() ?? "الإنتاج";
                employee.Department = DepartmentMap.ContainsKey(departmentText) ? DepartmentMap[departmentText] : EmployeeDepartment.Production;
                
                var employmentTypeText = _employmentTypeComboBox.SelectedItem?.ToString() ?? "دوام كامل";
                employee.EmploymentType = EmploymentTypeMap.ContainsKey(employmentTypeText) ? EmploymentTypeMap[employmentTypeText] : EmploymentType.FullTime;
                
                var statusText = _statusComboBox.SelectedItem?.ToString() ?? "نشط";
                employee.Status = StatusMap.ContainsKey(statusText) ? StatusMap[statusText] : EmployeeStatus.Active;
                
                employee.HireDate = _hireDatePicker.Value;
                // ContractStartDate and ContractEndDate not in model
                employee.BasicSalary = _basicSalaryNumeric.Value;
                employee.HousingAllowance = _housingAllowanceNumeric.Value;
                employee.TransportationAllowance = _transportationAllowanceNumeric.Value;
                employee.FoodAllowance = _foodAllowanceNumeric.Value;
                employee.OtherAllowances = _otherAllowancesNumeric.Value;
                employee.SocialInsuranceEnrolled = _socialInsuranceCheckBox.Checked;
                employee.SocialInsuranceNumber = _socialInsuranceNumberTextBox.Text;
                employee.SocialInsurancePercentage = _socialInsurancePercentageNumeric.Value;
                employee.HealthInsuranceEnrolled = _healthInsuranceCheckBox.Checked;
                // HealthInsuranceNumber not in model
                employee.AnnualLeaveDays = (int)_annualLeaveDaysNumeric.Value;
                employee.UsedLeaveDays = (int)_usedLeaveDaysNumeric.Value;
                employee.SickLeaveDays = (int)_sickLeaveDaysNumeric.Value;
                employee.BankName = _bankNameTextBox.Text;
                employee.BankAccountNumber = _bankAccountNumberTextBox.Text;
                employee.IBAN = _ibanTextBox.Text;
                employee.EmergencyContactName = _emergencyContactNameTextBox.Text;
                employee.EmergencyContactPhone = _emergencyContactPhoneTextBox.Text;
                employee.EmergencyContactRelation = _emergencyContactRelationTextBox.Text;
                employee.Notes = _notesTextBox.Text;

                _context.SaveChanges();

                MessageBox.Show("تم حفظ بيانات الموظف بنجاح", "نجاح", MessageBoxButtons.OK, MessageBoxIcon.Information);
                LoadEmployees();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"خطأ في حفظ البيانات: {ex.Message}", "خطأ", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void DeleteButton_Click(object? sender, EventArgs e)
        {
            if (!_currentEmployeeId.HasValue)
            {
                MessageBox.Show("الرجاء اختيار موظف للحذف", "تحذير", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            var result = MessageBox.Show("هل أنت متأكد من حذف هذا الموظف؟\n\nتحذير: سيتم حذف جميع البيانات المرتبطة بهذا الموظف (الرواتب، الحضور، الإجازات)",
                "تأكيد الحذف", MessageBoxButtons.YesNo, MessageBoxIcon.Question);

            if (result == DialogResult.Yes)
            {
                try
                {
                    var employee = _context.Employees.Find(_currentEmployeeId.Value);
                    if (employee != null)
                    {
                        _context.Employees.Remove(employee);
                        _context.SaveChanges();
                        MessageBox.Show("تم حذف الموظف بنجاح", "نجاح", MessageBoxButtons.OK, MessageBoxIcon.Information);
                        LoadEmployees();
                        SetupNewEmployee();
                    }
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"خطأ في حذف الموظف: {ex.Message}", "خطأ", MessageBoxButtons.OK, MessageBoxIcon.Error);
                }
            }
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                _context?.Dispose();
            }
            base.Dispose(disposing);
        }
    }
}
