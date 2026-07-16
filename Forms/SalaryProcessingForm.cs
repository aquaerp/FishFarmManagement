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
    /// نموذج معالجة الرواتب الشهرية
    /// Monthly Salary Processing Form
    /// </summary>
    public partial class SalaryProcessingForm : Form
    {
        #region Fields

        private readonly FishFarmContext _context;
        private readonly PrintService _printService;
        private readonly ExcelExportService _excelExportService;
        private List<SalaryCalculation> _salaryCalculations = new List<SalaryCalculation>();

        // UI Controls
        private ComboBox _monthComboBox = null!;
        private ComboBox _yearComboBox = null!;
        private ComboBox _departmentFilterComboBox = null!;
        private DataGridView _salaryGrid = null!;
        private Label _totalEmployeesLabel = null!;
        private Label _totalBasicSalaryLabel = null!;
        private Label _totalAllowancesLabel = null!;
        private Label _totalBonusesLabel = null!;
        private Label _totalDeductionsLabel = null!;
        private Label _totalInsuranceLabel = null!;
        private Label _totalNetSalaryLabel = null!;

        // Buttons
        private Button _calculateButton = null!;
        private Button _saveButton = null!;
        private Button _printButton = null!;
        private Button _exportButton = null!;

        #endregion

        #region Helper Classes

        private class SalaryCalculation
        {
            public int EmployeeId { get; set; }
            public string EmployeeNumber { get; set; } = "";
            public string EmployeeName { get; set; } = "";
            public string Department { get; set; } = "";
            public decimal BasicSalary { get; set; }
            public decimal HousingAllowance { get; set; }
            public decimal TransportationAllowance { get; set; }
            public decimal FoodAllowance { get; set; }
            public decimal OtherAllowances { get; set; }
            public decimal TotalAllowances => HousingAllowance + TransportationAllowance + FoodAllowance + OtherAllowances;
            public decimal Bonuses { get; set; }
            public decimal AbsenceDeduction { get; set; }
            public decimal LateDeduction { get; set; }
            public decimal OtherDeductions { get; set; }
            public decimal TotalDeductions => AbsenceDeduction + LateDeduction + OtherDeductions;
            public decimal SocialInsurance { get; set; }
            public decimal GrossSalary => BasicSalary + TotalAllowances + Bonuses;
            public decimal NetSalary => GrossSalary - TotalDeductions - SocialInsurance;
            public int WorkDays { get; set; }
            public int AbsentDays { get; set; }
            public decimal OvertimeHours { get; set; }
        }

        #endregion

        #region Constructor

        public SalaryProcessingForm(FishFarmContext context)
        {
            _context = context ?? throw new ArgumentNullException(nameof(context));
            _printService = new PrintService();
            _excelExportService = new ExcelExportService();

            if (!AuthenticationService.HasPermission(UserRole.Admin, UserRole.Manager, UserRole.HRStaff, UserRole.Accountant))
            {
                MessageBox.Show("ليس لديك صلاحية لمعالجة الرواتب", "تحذير", MessageBoxButtons.OK, MessageBoxIcon.Warning);
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
            this.Text = "معالجة الرواتب";
            this.Size = new Size(1600, 900);
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
                Text = "نظام معالجة الرواتب الشهرية",
                Location = new Point(20, 10),
                Size = new Size(1540, 40),
                Font = new Font("Cairo", 14F, FontStyle.Bold),
                ForeColor = Color.FromArgb(46, 92, 138),
                TextAlign = ContentAlignment.MiddleRight
            };
            mainPanel.Controls.Add(titleLabel);

            CreateFilterSection(mainPanel, 60);
            CreateGridSection(mainPanel, 140);
            CreateSummarySection(mainPanel, 600);
            CreateButtonsSection(mainPanel);

            this.Controls.Add(mainPanel);
        }

        private void CreateFilterSection(Panel parent, int startY)
        {
            var filterPanel = new GroupBox
            {
                Text = "الفترة والفلترة",
                Location = new Point(20, startY),
                Size = new Size(1540, 70),
                Font = new Font("Cairo", 10F, FontStyle.Bold)
            };

            int x = 1300;

            var monthLabel = new Label { Text = "الشهر:", Location = new Point(x + 150, 30), Size = new Size(60, 25), TextAlign = ContentAlignment.MiddleRight };
            filterPanel.Controls.Add(monthLabel);

            _monthComboBox = new ComboBox
            {
                Location = new Point(x, 30),
                Size = new Size(140, 30),
                DropDownStyle = ComboBoxStyle.DropDownList,
                Font = new Font("Cairo", 10F)
            };
            LoadMonths();
            filterPanel.Controls.Add(_monthComboBox);

            x -= 180;

            var yearLabel = new Label { Text = "السنة:", Location = new Point(x + 120, 30), Size = new Size(60, 25), TextAlign = ContentAlignment.MiddleRight };
            filterPanel.Controls.Add(yearLabel);

            _yearComboBox = new ComboBox
            {
                Location = new Point(x, 30),
                Size = new Size(110, 30),
                DropDownStyle = ComboBoxStyle.DropDownList,
                Font = new Font("Cairo", 10F)
            };
            LoadYears();
            filterPanel.Controls.Add(_yearComboBox);

            x -= 280;

            var deptLabel = new Label { Text = "القسم:", Location = new Point(x + 180, 30), Size = new Size(60, 25), TextAlign = ContentAlignment.MiddleRight };
            filterPanel.Controls.Add(deptLabel);

            _departmentFilterComboBox = new ComboBox
            {
                Location = new Point(x, 30),
                Size = new Size(170, 30),
                DropDownStyle = ComboBoxStyle.DropDownList,
                Font = new Font("Cairo", 10F)
            };
            LoadDepartments();
            filterPanel.Controls.Add(_departmentFilterComboBox);

            parent.Controls.Add(filterPanel);
        }

        private void CreateGridSection(Panel parent, int startY)
        {
            _salaryGrid = new DataGridView
            {
                Location = new Point(20, startY),
                Size = new Size(1540, 450),
                Font = new Font("Cairo", 9F),
                AllowUserToAddRows = false,
                AllowUserToDeleteRows = false,
                ReadOnly = true,
                MultiSelect = false,
                SelectionMode = DataGridViewSelectionMode.FullRowSelect,
                AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.AllCells,
                BackgroundColor = Color.White,
                BorderStyle = BorderStyle.Fixed3D
            };

            parent.Controls.Add(_salaryGrid);
        }

        private void CreateSummarySection(Panel parent, int startY)
        {
            var summaryPanel = new GroupBox
            {
                Text = "ملخص الرواتب",
                Location = new Point(20, startY),
                Size = new Size(1540, 120),
                Font = new Font("Cairo", 10F, FontStyle.Bold)
            };

            int x = 1350;
            int y = 30;
            int labelWidth = 150;
            int valueWidth = 120;
            int spacing = 200;

            // Row 1
            CreateSummaryItem(summaryPanel, "عدد الموظفين:", ref _totalEmployeesLabel, x, y, labelWidth, valueWidth);
            x -= spacing;
            CreateSummaryItem(summaryPanel, "إجمالي الرواتب الأساسية:", ref _totalBasicSalaryLabel, x, y, labelWidth, valueWidth);
            x -= spacing;
            CreateSummaryItem(summaryPanel, "إجمالي البدلات:", ref _totalAllowancesLabel, x, y, labelWidth, valueWidth);
            x -= spacing;
            CreateSummaryItem(summaryPanel, "إجمالي الحوافز:", ref _totalBonusesLabel, x, y, labelWidth, valueWidth);

            // Row 2
            x = 1350;
            y += 45;
            CreateSummaryItem(summaryPanel, "إجمالي الخصومات:", ref _totalDeductionsLabel, x, y, labelWidth, valueWidth);
            x -= spacing;
            CreateSummaryItem(summaryPanel, "إجمالي التأمينات:", ref _totalInsuranceLabel, x, y, labelWidth, valueWidth);
            x -= spacing;
            CreateSummaryItem(summaryPanel, "صافي الرواتب:", ref _totalNetSalaryLabel, x, y, labelWidth, valueWidth);
            _totalNetSalaryLabel.Font = new Font("Cairo", 11F, FontStyle.Bold);
            _totalNetSalaryLabel.ForeColor = Color.FromArgb(38, 166, 154);

            parent.Controls.Add(summaryPanel);
        }

        private void CreateSummaryItem(GroupBox panel, string labelText, ref Label valueLabel, int x, int y, int labelWidth, int valueWidth)
        {
            var label = new Label
            {
                Text = labelText,
                Location = new Point(x, y),
                Size = new Size(labelWidth, 25),
                TextAlign = ContentAlignment.MiddleRight,
                Font = new Font("Cairo", 9F)
            };
            panel.Controls.Add(label);

            valueLabel = new Label
            {
                Text = "0.00",
                Location = new Point(x - valueWidth - 10, y),
                Size = new Size(valueWidth, 25),
                TextAlign = ContentAlignment.MiddleCenter,
                Font = new Font("Cairo", 10F, FontStyle.Bold),
                ForeColor = Color.FromArgb(46, 92, 138),
                BorderStyle = BorderStyle.FixedSingle,
                BackColor = Color.White
            };
            panel.Controls.Add(valueLabel);
        }

        private void CreateButtonsSection(Panel parent)
        {
            var buttonPanel = new Panel
            {
                Location = new Point(20, 730),
                Size = new Size(1540, 50),
                BackColor = Color.Transparent
            };

            int x = 1300;
            int buttonWidth = 120;
            int spacing = 10;

            _calculateButton = CreateButton("حساب الرواتب", x, 10, buttonWidth);
            _calculateButton.Click += async (s, e) => await CalculateButton_ClickAsync();
            buttonPanel.Controls.Add(_calculateButton);

            x -= (buttonWidth + spacing);

            _saveButton = CreateButton("حفظ", x, 10, buttonWidth);
            _saveButton.Click += async (s, e) => await SaveButton_ClickAsync();
            _saveButton.Enabled = false;
            buttonPanel.Controls.Add(_saveButton);

            x -= (buttonWidth + spacing);

            _printButton = CreateButton("طباعة", x, 10, buttonWidth);
            _printButton.Click += PrintButton_Click;
            _printButton.Enabled = false;
            buttonPanel.Controls.Add(_printButton);

            x -= (buttonWidth + spacing);

            _exportButton = CreateButton("تصدير Excel", x, 10, buttonWidth);
            _exportButton.Click += ExportButton_Click;
            _exportButton.Enabled = false;
            buttonPanel.Controls.Add(_exportButton);

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
                if (_monthComboBox != null && _monthComboBox.Items.Count > 0)
                    _monthComboBox.SelectedIndex = Math.Min(DateTime.Now.Month - 1, _monthComboBox.Items.Count - 1);
                    
                if (_yearComboBox != null)
                    _yearComboBox.SelectedItem = DateTime.Now.Year;
                    
                if (_departmentFilterComboBox != null && _departmentFilterComboBox.Items.Count > 0)
                    _departmentFilterComboBox.SelectedIndex = 0;

                LoggingService.LogInfo("SalaryProcessingForm initialized");
            }
            catch (Exception ex)
            {
                LoggingService.LogError(ex, "Error initializing SalaryProcessingForm");
                MessageBox.Show($"حدث خطأ: {ex.Message}", "خطأ", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        #endregion

        #region Data Loading

        private void LoadMonths()
        {
            var months = new List<dynamic>
            {
                new { Value = 1, Display = "يناير - January" },
                new { Value = 2, Display = "فبراير - February" },
                new { Value = 3, Display = "مارس - March" },
                new { Value = 4, Display = "أبريل - April" },
                new { Value = 5, Display = "مايو - May" },
                new { Value = 6, Display = "يونيو - June" },
                new { Value = 7, Display = "يوليو - July" },
                new { Value = 8, Display = "أغسطس - August" },
                new { Value = 9, Display = "سبتمبر - September" },
                new { Value = 10, Display = "أكتوبر - October" },
                new { Value = 11, Display = "نوفمبر - November" },
                new { Value = 12, Display = "ديسمبر - December" }
            };

            _monthComboBox.DataSource = months;
            _monthComboBox.DisplayMember = "Display";
            _monthComboBox.ValueMember = "Value";
        }

        private void LoadYears()
        {
            var years = new List<int>();
            for (int i = DateTime.Now.Year - 2; i <= DateTime.Now.Year + 1; i++)
            {
                years.Add(i);
            }

            _yearComboBox.DataSource = years;
        }

        private void LoadDepartments()
        {
            var departments = Enum.GetValues(typeof(EmployeeDepartment))
                .Cast<EmployeeDepartment>()
                .Select(d => new { Value = d, Display = d.ToString() })
                .ToList();

            var all = new List<dynamic> { new { Value = (EmployeeDepartment?)null, Display = "جميع الأقسام" } };
            all.AddRange(departments);

            _departmentFilterComboBox.DataSource = all;
            _departmentFilterComboBox.DisplayMember = "Display";
            _departmentFilterComboBox.ValueMember = "Value";
        }

        #endregion

        #region Salary Calculation

        private async Task CalculateButton_ClickAsync()
        {
            try
            {
                var month = (int)_monthComboBox.SelectedValue!;
                var year = (int)_yearComboBox.SelectedValue!;

                var firstDay = new DateTime(year, month, 1);
                var lastDay = firstDay.AddMonths(1).AddDays(-1);

                _salaryCalculations.Clear();

                var employeesQuery = _context.Employees
                    .Where(e => e.Status == EmployeeStatus.Active || e.Status == EmployeeStatus.OnLeave);

                if (_departmentFilterComboBox.SelectedValue is EmployeeDepartment dept)
                {
                    employeesQuery = employeesQuery.Where(e => e.Department == dept);
                }

                var employees = await employeesQuery.ToListAsync();

                foreach (var employee in employees)
                {
                    var calculation = await CalculateEmployeeSalaryAsync(employee, firstDay, lastDay);
                    _salaryCalculations.Add(calculation);
                }

                DisplaySalaries();
                UpdateSummary();

                _saveButton.Enabled = _salaryCalculations.Count > 0;
                _printButton.Enabled = _salaryCalculations.Count > 0;
                _exportButton.Enabled = _salaryCalculations.Count > 0;

                MessageBox.Show($"تم حساب رواتب {_salaryCalculations.Count} موظف بنجاح", "نجح",
                    MessageBoxButtons.OK, MessageBoxIcon.Information);

                LoggingService.LogInfo($"Calculated salaries for {_salaryCalculations.Count} employees");
            }
            catch (Exception ex)
            {
                LoggingService.LogError("Error calculating salaries", ex);
                MessageBox.Show($"حدث خطأ: {ex.Message}", "خطأ", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private async Task<SalaryCalculation> CalculateEmployeeSalaryAsync(Employee employee, DateTime startDate, DateTime endDate)
        {
            var calculation = new SalaryCalculation
            {
                EmployeeId = employee.Id,
                EmployeeNumber = employee.EmployeeNumber,
                EmployeeName = employee.FullName,
                Department = employee.Department.ToString(),
                BasicSalary = employee.BasicSalary,
                HousingAllowance = employee.HousingAllowance ?? 0,
                TransportationAllowance = employee.TransportationAllowance ?? 0,
                FoodAllowance = employee.FoodAllowance ?? 0,
                OtherAllowances = employee.OtherAllowances ?? 0
            };

            // Calculate attendance
            var attendances = await _context.Attendances
                .Where(a => a.EmployeeId == employee.Id && a.Date >= startDate && a.Date <= endDate)
                .ToListAsync();

            calculation.WorkDays = attendances.Count(a => a.Status == AttendanceStatus.Present || a.Status == AttendanceStatus.Late);
            calculation.AbsentDays = attendances.Count(a => a.Status == AttendanceStatus.Absent);
            calculation.OvertimeHours = attendances.Sum(a => a.OvertimeHours ?? 0);

            // Calculate bonuses (overtime)
            var overtimePay = calculation.OvertimeHours * (employee.BasicSalary / 240) * 1.5m; // 1.5x for overtime
            calculation.Bonuses = overtimePay;

            // Calculate deductions for absence
            int workingDaysInMonth = GetWorkingDaysInMonth(startDate.Year, startDate.Month);
            if (calculation.AbsentDays > 0)
            {
                calculation.AbsenceDeduction = (calculation.BasicSalary / workingDaysInMonth) * calculation.AbsentDays;
            }

            // Social Insurance (9.75%)
            calculation.SocialInsurance = calculation.BasicSalary * 0.0975m;

            return calculation;
        }

        private int GetWorkingDaysInMonth(int year, int month)
        {
            var firstDay = new DateTime(year, month, 1);
            var lastDay = firstDay.AddMonths(1).AddDays(-1);
            int workingDays = 0;

            for (var day = firstDay; day <= lastDay; day = day.AddDays(1))
            {
                if (day.DayOfWeek != DayOfWeek.Friday && day.DayOfWeek != DayOfWeek.Saturday)
                {
                    workingDays++;
                }
            }

            return workingDays;
        }

        private void DisplaySalaries()
        {
            var displayData = _salaryCalculations.Select(s => new
            {
                الرقم_الوظيفي = s.EmployeeNumber,
                الموظف = s.EmployeeName,
                القسم = s.Department,
                الراتب_الأساسي = s.BasicSalary.ToString("N2"),
                البدلات = s.TotalAllowances.ToString("N2"),
                الحوافز = s.Bonuses.ToString("N2"),
                الإجمالي_قبل_الخصم = s.GrossSalary.ToString("N2"),
                خصم_الغياب = s.AbsenceDeduction.ToString("N2"),
                خصومات_أخرى = s.OtherDeductions.ToString("N2"),
                التأمينات_الاجتماعية = s.SocialInsurance.ToString("N2"),
                صافي_الراتب = s.NetSalary.ToString("N2"),
                أيام_عمل = s.WorkDays,
                أيام_غياب = s.AbsentDays,
                ساعات_إضافي = s.OvertimeHours.ToString("N2")
            }).ToList();

            _salaryGrid.DataSource = displayData;

            if (_salaryGrid.Columns.Count > 0)
            {
                foreach (DataGridViewColumn column in _salaryGrid.Columns)
                {
                    column.DefaultCellStyle.Alignment = DataGridViewContentAlignment.MiddleCenter;
                }

                _salaryGrid.Columns["صافي_الراتب"].DefaultCellStyle.Font = new Font("Cairo", 9F, FontStyle.Bold);
                _salaryGrid.Columns["صافي_الراتب"].DefaultCellStyle.ForeColor = Color.FromArgb(38, 166, 154);
            }
        }

        private void UpdateSummary()
        {
            _totalEmployeesLabel.Text = _salaryCalculations.Count.ToString();
            _totalBasicSalaryLabel.Text = _salaryCalculations.Sum(s => s.BasicSalary).ToString("N2");
            _totalAllowancesLabel.Text = _salaryCalculations.Sum(s => s.TotalAllowances).ToString("N2");
            _totalBonusesLabel.Text = _salaryCalculations.Sum(s => s.Bonuses).ToString("N2");
            _totalDeductionsLabel.Text = _salaryCalculations.Sum(s => s.TotalDeductions).ToString("N2");
            _totalInsuranceLabel.Text = _salaryCalculations.Sum(s => s.SocialInsurance).ToString("N2");
            _totalNetSalaryLabel.Text = _salaryCalculations.Sum(s => s.NetSalary).ToString("N2");
        }

        #endregion

        #region Save Operations

        private async Task SaveButton_ClickAsync()
        {
            try
            {
                var month = (int)_monthComboBox.SelectedValue!;
                var year = (int)_yearComboBox.SelectedValue!;

                var firstDay = new DateTime(year, month, 1);

                var result = MessageBox.Show(
                    $"هل أنت متأكد من حفظ رواتب {_salaryCalculations.Count} موظف لشهر {month}/{year}؟",
                    "تأكيد الحفظ",
                    MessageBoxButtons.YesNo,
                    MessageBoxIcon.Question);

                if (result != DialogResult.Yes)
                return;

                foreach (var calc in _salaryCalculations)
                {
                    var existingSalary = await _context.Salaries
                        .FirstOrDefaultAsync(s => s.EmployeeId == calc.EmployeeId && 
                                                  s.Month == month && 
                                                  s.Year == year);

                    if (existingSalary != null)
                    {
                        // Update existing
                        existingSalary.BasicSalary = calc.BasicSalary;
                        existingSalary.HousingAllowance = calc.HousingAllowance;
                        existingSalary.TransportationAllowance = calc.TransportationAllowance;
                        existingSalary.FoodAllowance = calc.FoodAllowance;
                        existingSalary.OtherAllowances = calc.OtherAllowances;
                        existingSalary.TotalBonuses = calc.Bonuses;
                        existingSalary.AbsenceDeduction = calc.AbsenceDeduction;
                        existingSalary.SocialInsuranceDeduction = calc.SocialInsurance;
                        existingSalary.TotalDeductions = calc.TotalDeductions + calc.SocialInsurance;
                        existingSalary.NetSalary = calc.NetSalary;
                        existingSalary.Status = SalaryStatus.Approved;
                        existingSalary.ApprovedBy = AuthenticationService.CurrentUser?.Username ?? "System";
                    }
                    else
                    {
                        // Create new
                        var periodStart = new DateTime(year, month, 1);
                        var periodEnd = periodStart.AddMonths(1).AddDays(-1);

                    var salary = new Salary
                    {
                            EmployeeId = calc.EmployeeId,
                            SalaryNumber = $"SAL-{year}{month:D2}-{calc.EmployeeId:D4}",
                        Month = month,
                        Year = year,
                            PayPeriodStart = periodStart,
                            PayPeriodEnd = periodEnd,
                            BasicSalary = calc.BasicSalary,
                            HousingAllowance = calc.HousingAllowance,
                            TransportationAllowance = calc.TransportationAllowance,
                            FoodAllowance = calc.FoodAllowance,
                            OtherAllowances = calc.OtherAllowances,
                            TotalBonuses = calc.Bonuses,
                            AbsenceDeduction = calc.AbsenceDeduction,
                            SocialInsuranceDeduction = calc.SocialInsurance,
                            TotalDeductions = calc.TotalDeductions + calc.SocialInsurance,
                            GrossSalary = calc.GrossSalary,
                            NetSalary = calc.NetSalary,
                            Status = SalaryStatus.Approved,
                        PaymentMethod = PaymentMethod.BankTransfer,
                            WorkDays = calc.WorkDays,
                            AbsenceDays = calc.AbsentDays,
                            OvertimeHours = calc.OvertimeHours,
                            CreatedAt = DateTime.Now,
                            PreparedBy = AuthenticationService.CurrentUser?.Username ?? "System",
                            ApprovedBy = AuthenticationService.CurrentUser?.Username ?? "System"
                        };

                    _context.Salaries.Add(salary);
                    }
                }

                await _context.SaveChangesAsync();

                MessageBox.Show("تم حفظ الرواتب بنجاح", "نجح", MessageBoxButtons.OK, MessageBoxIcon.Information);

                LoggingService.LogInfo($"Saved salaries for {_salaryCalculations.Count} employees - {month}/{year}");
            }
            catch (Exception ex)
            {
                LoggingService.LogError("Error saving salaries", ex);
                MessageBox.Show($"حدث خطأ: {ex.Message}", "خطأ", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        #endregion

        #region Print & Export

        private void PrintButton_Click(object? sender, EventArgs e)
        {
            try
            {
                if (_salaryCalculations == null || _salaryCalculations.Count == 0)
                {
                    MessageBox.Show("لا توجد رواتب لطباعتها. الرجاء حساب الرواتب أولاً.",
                        "تنبيه", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    return;
                }

                var dataTable = new System.Data.DataTable();
                dataTable.Columns.Add("الموظف", typeof(string));
                dataTable.Columns.Add("الراتب الأساسي", typeof(string));
                dataTable.Columns.Add("البدلات", typeof(string));
                dataTable.Columns.Add("الخصومات", typeof(string));
                dataTable.Columns.Add("الصافي", typeof(string));

                foreach (var salary in _salaryCalculations)
                {
                    dataTable.Rows.Add(
                        salary.EmployeeName,
                        $"{salary.BasicSalary:N2}",
                        $"{salary.TotalAllowances:N2}",
                        $"{salary.TotalDeductions:N2}",
                        $"{salary.NetSalary:N2}"
                    );
                }

                var pdfPath = _printService.CreateFinancialReportPDF(
                    "كشف الرواتب - Payroll Statement",
                    "AquaFarm Pro",
                    dataTable,
                    new DateTime(int.Parse(_yearComboBox.Text), _monthComboBox.SelectedIndex + 1, 1),
                    new DateTime(int.Parse(_yearComboBox.Text), _monthComboBox.SelectedIndex + 1, DateTime.DaysInMonth(int.Parse(_yearComboBox.Text), _monthComboBox.SelectedIndex + 1))
                );

                if (MessageBox.Show($"تم إنشاء PDF:\n{pdfPath}\n\nفتح؟", "نجح",
                    MessageBoxButtons.YesNo, MessageBoxIcon.Information) == DialogResult.Yes)
                    _printService.PrintPDF(pdfPath);

                LoggingService.LogInfo("Salary report printed");
            }
            catch (Exception ex)
            {
                LoggingService.LogError("Error printing salary report", ex);
                MessageBox.Show($"حدث خطأ: {ex.Message}", "خطأ", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void ExportButton_Click(object? sender, EventArgs e)
        {
            try
            {
                if (_salaryCalculations == null || _salaryCalculations.Count == 0)
                {
                    MessageBox.Show("لا توجد رواتب للتصدير. الرجاء حساب الرواتب أولاً.",
                        "تنبيه", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    return;
                }

                var month = _monthComboBox.SelectedIndex + 1;
                var year = int.Parse(_yearComboBox.Text);

                var dataTable = new System.Data.DataTable();
                dataTable.Columns.Add("الموظف", typeof(string));
                dataTable.Columns.Add("الراتب الأساسي", typeof(decimal));
                dataTable.Columns.Add("البدلات", typeof(decimal));
                dataTable.Columns.Add("الخصومات", typeof(decimal));
                dataTable.Columns.Add("الصافي", typeof(decimal));

                foreach (var salary in _salaryCalculations)
                {
                    dataTable.Rows.Add(
                        salary.EmployeeName,
                        salary.BasicSalary,
                        salary.TotalAllowances,
                        salary.TotalDeductions,
                        salary.NetSalary
                    );
                }

                var excelPath = _excelExportService.ExportSalaryToExcel(dataTable, month, year);

                if (MessageBox.Show($"تم تصدير الملف:\n{excelPath}\n\nفتح؟", "نجح",
                    MessageBoxButtons.YesNo, MessageBoxIcon.Information) == DialogResult.Yes)
                    _excelExportService.OpenExcelFile(excelPath);

                LoggingService.LogInfo("Salary exported to Excel");
            }
            catch (Exception ex)
            {
                LoggingService.LogError("Error exporting salary", ex);
                MessageBox.Show($"حدث خطأ: {ex.Message}", "خطأ", MessageBoxButtons.OK, MessageBoxIcon.Error);
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
