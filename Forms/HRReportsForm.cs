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
    /// نموذج تقارير الموارد البشرية
    /// HR Reports Form
    /// </summary>
    public partial class HRReportsForm : Form
    {
        #region Fields

        private readonly FishFarmContext _context;

        // UI Controls
        private TabControl _tabControl = null!;
        private Panel _attendanceReportPanel = null!;
        private Panel _salaryReportPanel = null!;
        private Panel _leaveReportPanel = null!;
        private Panel _employeeReportPanel = null!;

        #endregion

        #region Constructor

        public HRReportsForm(FishFarmContext context)
        {
            _context = context ?? throw new ArgumentNullException(nameof(context));

            if (!AuthenticationService.HasPermission(UserRole.Admin, UserRole.Manager, UserRole.HRStaff))
            {
                MessageBox.Show("ليس لديك صلاحية لعرض تقارير الموارد البشرية", "تحذير",
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
            this.Text = "تقارير الموارد البشرية";
            this.Size = new Size(1600, 900);
            this.StartPosition = FormStartPosition.CenterScreen;
            this.RightToLeft = RightToLeft.Yes;
            this.RightToLeftLayout = true;
            this.Font = new Font("Cairo", 10F);
            this.WindowState = FormWindowState.Maximized;

            var mainPanel = new Panel
            {
                Dock = DockStyle.Fill,
                Padding = new Padding(10),
                BackColor = Color.FromArgb(240, 240, 240)
            };

            var titleLabel = new Label
            {
                Text = "تقارير الموارد البشرية الشاملة",
                Location = new Point(10, 10),
                Size = new Size(1560, 40),
                Font = new Font("Cairo", 14F, FontStyle.Bold),
                ForeColor = Color.FromArgb(46, 92, 138),
                TextAlign = ContentAlignment.MiddleRight
            };
            mainPanel.Controls.Add(titleLabel);

            _tabControl = new TabControl
            {
                Location = new Point(10, 60),
                Size = new Size(1560, 800),
                Font = new Font("Cairo", 10F)
            };

            CreateTabs();

            mainPanel.Controls.Add(_tabControl);
            this.Controls.Add(mainPanel);
        }

        private void CreateTabs()
        {
            // Dashboard is intentionally excluded from the first commercial release;
            // its underlying detailed reports remain available in the following tabs.
            // Tab 1: Attendance Report
            var attendanceTab = new TabPage("تقرير الحضور");
            _attendanceReportPanel = CreateAttendanceReportPanel();
            attendanceTab.Controls.Add(_attendanceReportPanel);
            _tabControl.TabPages.Add(attendanceTab);

            // Tab 3: Salary Report
            var salaryTab = new TabPage("تقرير الرواتب");
            _salaryReportPanel = CreateSalaryReportPanel();
            salaryTab.Controls.Add(_salaryReportPanel);
            _tabControl.TabPages.Add(salaryTab);

            // Tab 4: Leave Report
            var leaveTab = new TabPage("تقرير الإجازات");
            _leaveReportPanel = CreateLeaveReportPanel();
            leaveTab.Controls.Add(_leaveReportPanel);
            _tabControl.TabPages.Add(leaveTab);

            // Tab 5: Employee Report
            var employeeTab = new TabPage("تقرير الموظفين");
            _employeeReportPanel = CreateEmployeeReportPanel();
            employeeTab.Controls.Add(_employeeReportPanel);
            _tabControl.TabPages.Add(employeeTab);
        }

        private void InitializeForm()
        {
            try
            {
                _ = LoadDashboardDataAsync();
                LoggingService.LogInfo("HRReportsForm initialized");
            }
            catch (Exception ex)
            {
                LoggingService.LogError("Error initializing HRReportsForm", ex);
                MessageBox.Show($"حدث خطأ: {ex.Message}", "خطأ", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        #endregion

        #region Dashboard

        private Panel CreateDashboardPanel()
        {
            var panel = new Panel { Dock = DockStyle.Fill, Padding = new Padding(20), AutoScroll = true };

            // KPI Cards
            var cardsPanel = new FlowLayoutPanel
            {
                Location = new Point(20, 20),
                Size = new Size(1500, 150),
                FlowDirection = FlowDirection.RightToLeft,
                WrapContents = false
            };

            // KPI cards for the deferred dashboard implementation.
            cardsPanel.Controls.Add(CreateKPICard("إجمالي الموظفين", "0", Color.FromArgb(46, 92, 138)));
            cardsPanel.Controls.Add(CreateKPICard("نسبة الحضور", "0%", Color.FromArgb(38, 166, 154)));
            cardsPanel.Controls.Add(CreateKPICard("إجمالي الرواتب الشهري", "0 ريال", Color.FromArgb(74, 144, 226)));
            cardsPanel.Controls.Add(CreateKPICard("الإجازات هذا الشهر", "0", Color.FromArgb(255, 152, 0)));

            panel.Controls.Add(cardsPanel);

            // Charts
            var chartsPanel = new Panel
            {
                Location = new Point(20, 180),
                Size = new Size(1500, 550)
            };

            var attendanceLabel = new Label
            {
                Text = "ملخص توزيع الحضور",
                Location = new Point(800, 0),
                Size = new Size(680, 270),
                BackColor = Color.White,
                Font = new Font("Cairo", 12F),
                TextAlign = ContentAlignment.MiddleCenter,
                BorderStyle = BorderStyle.FixedSingle
            };
            chartsPanel.Controls.Add(attendanceLabel);

            var departmentLabel = new Label
            {
                Text = "ملخص توزيع الموظفين حسب القسم",
                Location = new Point(800, 280),
                Size = new Size(680, 270),
                BackColor = Color.White,
                Font = new Font("Cairo", 12F),
                TextAlign = ContentAlignment.MiddleCenter,
                BorderStyle = BorderStyle.FixedSingle
            };
            chartsPanel.Controls.Add(departmentLabel);

            panel.Controls.Add(chartsPanel);

            return panel;
        }

        private Panel CreateKPICard(string title, string value, Color color)
        {
            var card = new Panel
            {
                Size = new Size(280, 130),
                BackColor = Color.White,
                BorderStyle = BorderStyle.FixedSingle,
                Margin = new Padding(10)
            };

            var titleLabel = new Label
            {
                Text = title,
                Location = new Point(10, 15),
                Size = new Size(260, 30),
                Font = new Font("Cairo", 10F),
                ForeColor = Color.Gray,
                TextAlign = ContentAlignment.MiddleCenter
            };
            card.Controls.Add(titleLabel);

            var valueLabel = new Label
            {
                Text = value,
                Location = new Point(10, 50),
                Size = new Size(260, 50),
                Font = new Font("Cairo", 20F, FontStyle.Bold),
                ForeColor = color,
                TextAlign = ContentAlignment.MiddleCenter
            };
            card.Controls.Add(valueLabel);

            return card;
        }

        private async Task LoadDashboardDataAsync()
        {
            try
            {
                // Reserved for the deferred dashboard implementation.
                await Task.CompletedTask;
                LoggingService.LogInfo("Dashboard data loaded");
            }
            catch (Exception ex)
            {
                LoggingService.LogError("Error loading dashboard", ex);
            }
        }

        #endregion

        #region Attendance Report

        private Panel CreateAttendanceReportPanel()
        {
            var panel = new Panel { Dock = DockStyle.Fill, Padding = new Padding(20) };

            // Filters
            var filterPanel = new GroupBox
            {
                Text = "الفلترة",
                Location = new Point(20, 20),
                Size = new Size(1500, 70),
                Font = new Font("Cairo", 10F, FontStyle.Bold)
            };

            var fromDate = new DateTimePicker { Location = new Point(1200, 30), Size = new Size(150, 30), Format = DateTimePickerFormat.Short };
            var toDate = new DateTimePicker { Location = new Point(1000, 30), Size = new Size(150, 30), Format = DateTimePickerFormat.Short };
            var employeeCombo = new ComboBox { Location = new Point(750, 30), Size = new Size(200, 30), DropDownStyle = ComboBoxStyle.DropDownList };
            var generateBtn = new Button { Text = "إنشاء التقرير", Location = new Point(500, 28), Size = new Size(150, 35), Font = new Font("Cairo", 10F, FontStyle.Bold) };

            filterPanel.Controls.AddRange(new Control[] { fromDate, toDate, employeeCombo, generateBtn });
            panel.Controls.Add(filterPanel);

            // Report Grid
            var grid = new DataGridView
            {
                Location = new Point(20, 100),
                Size = new Size(1000, 620),
                Font = new Font("Cairo", 9F),
                BackgroundColor = Color.White,
                ReadOnly = true
            };
            panel.Controls.Add(grid);

            // Summary Label
            var summaryLabel = new Label
            {
                Text = "ملخص إحصائيات الحضور",
                Location = new Point(1030, 100),
                Size = new Size(490, 620),
                BackColor = Color.White,
                Font = new Font("Cairo", 12F),
                TextAlign = ContentAlignment.MiddleCenter,
                BorderStyle = BorderStyle.FixedSingle
            };
            panel.Controls.Add(summaryLabel);

            return panel;
        }

        #endregion

        #region Salary Report

        private Panel CreateSalaryReportPanel()
        {
            var panel = new Panel { Dock = DockStyle.Fill, Padding = new Padding(20) };

            var filterPanel = new GroupBox
            {
                Text = "الفلترة",
                Location = new Point(20, 20),
                Size = new Size(1500, 70),
                Font = new Font("Cairo", 10F, FontStyle.Bold)
            };

            var monthCombo = new ComboBox { Location = new Point(1200, 30), Size = new Size(120, 30), DropDownStyle = ComboBoxStyle.DropDownList };
            var yearCombo = new ComboBox { Location = new Point(1050, 30), Size = new Size(100, 30), DropDownStyle = ComboBoxStyle.DropDownList };
            var deptCombo = new ComboBox { Location = new Point(850, 30), Size = new Size(150, 30), DropDownStyle = ComboBoxStyle.DropDownList };
            var generateBtn = new Button { Text = "إنشاء التقرير", Location = new Point(650, 28), Size = new Size(150, 35), Font = new Font("Cairo", 10F, FontStyle.Bold) };

            filterPanel.Controls.AddRange(new Control[] { monthCombo, yearCombo, deptCombo, generateBtn });
            panel.Controls.Add(filterPanel);

            var grid = new DataGridView
            {
                Location = new Point(20, 100),
                Size = new Size(1000, 620),
                Font = new Font("Cairo", 9F),
                BackgroundColor = Color.White,
                ReadOnly = true
            };
            panel.Controls.Add(grid);

            var summaryLabel = new Label
            {
                Text = "ملخص توزيع الرواتب",
                Location = new Point(1030, 100),
                Size = new Size(490, 620),
                BackColor = Color.White,
                Font = new Font("Cairo", 12F),
                TextAlign = ContentAlignment.MiddleCenter,
                BorderStyle = BorderStyle.FixedSingle
            };
            panel.Controls.Add(summaryLabel);

            return panel;
        }

        #endregion

        #region Leave Report

        private Panel CreateLeaveReportPanel()
        {
            var panel = new Panel { Dock = DockStyle.Fill, Padding = new Padding(20) };

            var filterPanel = new GroupBox
            {
                Text = "الفلترة",
                Location = new Point(20, 20),
                Size = new Size(1500, 70),
                Font = new Font("Cairo", 10F, FontStyle.Bold)
            };

            var fromDate = new DateTimePicker { Location = new Point(1200, 30), Size = new Size(150, 30), Format = DateTimePickerFormat.Short };
            var toDate = new DateTimePicker { Location = new Point(1000, 30), Size = new Size(150, 30), Format = DateTimePickerFormat.Short };
            var typeCombo = new ComboBox { Location = new Point(800, 30), Size = new Size(150, 30), DropDownStyle = ComboBoxStyle.DropDownList };
            var statusCombo = new ComboBox { Location = new Point(600, 30), Size = new Size(150, 30), DropDownStyle = ComboBoxStyle.DropDownList };
            var generateBtn = new Button { Text = "إنشاء التقرير", Location = new Point(400, 28), Size = new Size(150, 35), Font = new Font("Cairo", 10F, FontStyle.Bold) };

            filterPanel.Controls.AddRange(new Control[] { fromDate, toDate, typeCombo, statusCombo, generateBtn });
            panel.Controls.Add(filterPanel);

            var grid = new DataGridView
            {
                Location = new Point(20, 100),
                Size = new Size(1000, 620),
                Font = new Font("Cairo", 9F),
                BackgroundColor = Color.White,
                ReadOnly = true
            };
            panel.Controls.Add(grid);

            var summaryLabel = new Label
            {
                Text = "ملخص إحصائيات الإجازات",
                Location = new Point(1030, 100),
                Size = new Size(490, 620),
                BackColor = Color.White,
                Font = new Font("Cairo", 12F),
                TextAlign = ContentAlignment.MiddleCenter,
                BorderStyle = BorderStyle.FixedSingle
            };
            panel.Controls.Add(summaryLabel);

            return panel;
        }

        #endregion

        #region Employee Report

        private Panel CreateEmployeeReportPanel()
        {
            var panel = new Panel { Dock = DockStyle.Fill, Padding = new Padding(20) };

            var filterPanel = new GroupBox
            {
                Text = "الفلترة",
                Location = new Point(20, 20),
                Size = new Size(1500, 70),
                Font = new Font("Cairo", 10F, FontStyle.Bold)
            };

            var deptCombo = new ComboBox { Location = new Point(1200, 30), Size = new Size(200, 30), DropDownStyle = ComboBoxStyle.DropDownList };
            var statusCombo = new ComboBox { Location = new Point(950, 30), Size = new Size(200, 30), DropDownStyle = ComboBoxStyle.DropDownList };
            var positionCombo = new ComboBox { Location = new Point(700, 30), Size = new Size(200, 30), DropDownStyle = ComboBoxStyle.DropDownList };
            var generateBtn = new Button { Text = "إنشاء التقرير", Location = new Point(500, 28), Size = new Size(150, 35), Font = new Font("Cairo", 10F, FontStyle.Bold) };

            filterPanel.Controls.AddRange(new Control[] { deptCombo, statusCombo, positionCombo, generateBtn });
            panel.Controls.Add(filterPanel);

            var grid = new DataGridView
            {
                Location = new Point(20, 100),
                Size = new Size(1000, 620),
                Font = new Font("Cairo", 9F),
                BackgroundColor = Color.White,
                ReadOnly = true
            };
            panel.Controls.Add(grid);

            var summaryLabel = new Label
            {
                Text = "ملخص توزيع الموظفين",
                Location = new Point(1030, 100),
                Size = new Size(490, 620),
                BackColor = Color.White,
                Font = new Font("Cairo", 12F),
                TextAlign = ContentAlignment.MiddleCenter,
                BorderStyle = BorderStyle.FixedSingle
            };
            panel.Controls.Add(summaryLabel);

            return panel;
        }

        #endregion

        #region Reports Generation

        private async Task GenerateAttendanceReportAsync(DateTime fromDate, DateTime toDate, int? employeeId)
        {
            try
            {
                var query = _context.Attendances
                    .Include(a => a.Employee)
                    .Where(a => a.Date >= fromDate && a.Date <= toDate);

                if (employeeId.HasValue && employeeId.Value > 0)
                {
                    query = query.Where(a => a.EmployeeId == employeeId.Value);
                }

                var data = await query
                    .GroupBy(a => new { a.EmployeeId, a.Employee!.FullName, a.Employee.EmployeeNumber })
                    .Select(g => new
                    {
                        الرقم_الوظيفي = g.Key.EmployeeNumber,
                        الموظف = g.Key.FullName,
                        أيام_حضور = g.Count(a => a.Status == AttendanceStatus.Present || a.Status == AttendanceStatus.Late),
                        أيام_غياب = g.Count(a => a.Status == AttendanceStatus.Absent),
                        أيام_إجازة = g.Count(a => a.Status == AttendanceStatus.OnLeave),
                        مرات_تأخير = g.Count(a => a.Status == AttendanceStatus.Late),
                        إجمالي_ساعات_إضافي = g.Sum(a => a.OvertimeHours) ?? 0,
                        نسبة_الحضور = g.Count(a => a.Status == AttendanceStatus.Present || a.Status == AttendanceStatus.Late) * 100.0 / g.Count()
                    })
                    .ToListAsync();

                // Display in grid (would need to get grid from panel)
                LoggingService.LogInfo($"Generated attendance report: {data.Count} employees");
            }
            catch (Exception ex)
            {
                LoggingService.LogError("Error generating attendance report", ex);
                MessageBox.Show($"حدث خطأ: {ex.Message}", "خطأ", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private async Task GenerateSalaryReportAsync(int month, int year, EmployeeDepartment? department)
        {
            try
        {
            var query = _context.Salaries
                .Include(s => s.Employee)
                    .Where(s => s.Month == month && s.Year == year);

                if (department.HasValue)
                {
                    query = query.Where(s => s.Employee!.Department == department.Value);
                }

                var data = await query
                    .Select(s => new
                    {
                        الرقم_الوظيفي = s.Employee!.EmployeeNumber,
                الموظف = s.Employee.FullName,
                        القسم = s.Employee.Department.ToString(),
                        الراتب_الأساسي = s.BasicSalary,
                        البدلات = s.HousingAllowance + s.TransportationAllowance + s.FoodAllowance + s.OtherAllowances,
                        الحوافز = s.TotalBonuses,
                        الخصومات = s.TotalDeductions,
                        التأمينات = s.SocialInsuranceDeduction,
                        صافي_الراتب = s.NetSalary
                    })
                    .ToListAsync();

                LoggingService.LogInfo($"Generated salary report: {data.Count} employees");
            }
            catch (Exception ex)
            {
                LoggingService.LogError("Error generating salary report", ex);
                MessageBox.Show($"حدث خطأ: {ex.Message}", "خطأ", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private async Task GenerateLeaveReportAsync(DateTime fromDate, DateTime toDate, LeaveType? type, LeaveStatus? status)
        {
            try
            {
                var query = _context.EmployeeLeaves
                    .Include(l => l.Employee)
                    .Where(l => l.StartDate >= fromDate && l.EndDate <= toDate);

                if (type.HasValue)
                {
                    query = query.Where(l => l.LeaveType == type.Value);
                }

                if (status.HasValue)
                {
                    query = query.Where(l => l.Status == status.Value);
                }

                var data = await query
                    .Select(l => new
                    {
                        الموظف = l.Employee!.FullName,
                        النوع = l.LeaveType.ToString(),
                        من = l.StartDate.ToString("yyyy-MM-dd"),
                        إلى = l.EndDate.ToString("yyyy-MM-dd"),
                        الأيام = l.DaysCount,
                        الحالة = l.Status.ToString(),
                        السبب = l.Reason
                    })
                    .ToListAsync();

                LoggingService.LogInfo($"Generated leave report: {data.Count} leaves");
            }
            catch (Exception ex)
            {
                LoggingService.LogError("Error generating leave report", ex);
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
