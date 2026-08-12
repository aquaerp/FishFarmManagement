using System;
using System.Windows.Forms;
using System.Linq;
using System.Drawing;
using FishFarmManager.Data;
using FishFarmManager.Models;
using FishFarmManager.Controls;
using Microsoft.EntityFrameworkCore;

namespace FishFarmManager.Forms
{
    public partial class QualityHealthReportsForm : Form
    {
        private readonly FishFarmContext _context;
        private TabControl reportsTabControl = null!;
        private ComboBox reportCycleComboBox = null!;
        private DateTimePicker reportStartDatePicker = null!;
        private DateTimePicker reportEndDatePicker = null!;
        private Button generateReportButton = null!;
        private Panel reportResultPanel = null!;

        public QualityHealthReportsForm(FishFarmContext context)
        {
            InitializeComponent();
            _context = context;
            InitializeCustomComponents();
            LoadCycles();
        }

        private void InitializeCustomComponents()
        {
            this.Text = "تقارير الجودة والصحة - Quality & Health Reports";
            this.Size = new Size(1400, 900);
            this.StartPosition = FormStartPosition.CenterScreen;

            // Main layout
            TableLayoutPanel mainLayout = new TableLayoutPanel
            {
                Dock = DockStyle.Fill,
                RowCount = 2,
                ColumnCount = 1,
                Padding = new Padding(10)
            };
            mainLayout.RowStyles.Add(new RowStyle(SizeType.Absolute, 100F));
            mainLayout.RowStyles.Add(new RowStyle(SizeType.Percent, 100F));

            // Filter Panel
            Panel filterPanel = new Panel { Dock = DockStyle.Fill };
            Label cycleLabel = new Label { Text = "دورة الإنتاج - Production Cycle:", Location = new Point(10, 15), Width = 150 };
            reportCycleComboBox = new ComboBox { Location = new Point(170, 12), Width = 200, DropDownStyle = ComboBoxStyle.DropDownList };
            Label startDateLabel = new Label { Text = "من تاريخ - From Date:", Location = new Point(400, 15), Width = 100 };
            reportStartDatePicker = new DateTimePicker { Location = new Point(510, 12), Width = 150 };
            Label endDateLabel = new Label { Text = "إلى تاريخ - To Date:", Location = new Point(680, 15), Width = 100 };
            reportEndDatePicker = new DateTimePicker { Location = new Point(790, 12), Width = 150 };
            generateReportButton = new Button { Text = "إنشاء التقرير - Generate Report", Location = new Point(970, 10), Width = 150, Height = 35 };
            generateReportButton.Click += GenerateReportButton_Click;

            filterPanel.Controls.AddRange(new Control[] { cycleLabel, reportCycleComboBox, startDateLabel, reportStartDatePicker, endDateLabel, reportEndDatePicker, generateReportButton });

            // Tabs for different reports
            reportsTabControl = new TabControl { Dock = DockStyle.Fill };
            
            // Tab 1: Quality Test Summary
            TabPage qualityTestTab = new TabPage("ملخص اختبارات الجودة - Quality Tests");
            reportsTabControl.TabPages.Add(qualityTestTab);

            // Tab 2: Health Inspection Report
            TabPage healthInspectionTab = new TabPage("تقرير الفحص الصحي - Health Inspections");
            reportsTabControl.TabPages.Add(healthInspectionTab);

            // Tab 3: HACCP Compliance Report
            TabPage haccpTab = new TabPage("تقرير الامتثال HACCP - HACCP Compliance");
            reportsTabControl.TabPages.Add(haccpTab);

            // Tab 4: Certifications Status
            TabPage certificationsTab = new TabPage("حالة الشهادات - Certifications Status");
            reportsTabControl.TabPages.Add(certificationsTab);

            // Tab 5: Quality Trends Analysis
            TabPage trendsTab = new TabPage("تحليل اتجاهات الجودة - Quality Trends");
            reportsTabControl.TabPages.Add(trendsTab);

            // Result panel for each tab
            reportResultPanel = new Panel { Dock = DockStyle.Fill, AutoScroll = true };

            mainLayout.Controls.Add(filterPanel, 0, 0);
            mainLayout.Controls.Add(reportsTabControl, 0, 1);

            this.Controls.Add(mainLayout);
        }

        private void LoadCycles()
        {
            var cycles = _context.ProductionCycles
                .Select(c => new ComboItem { Id = c.Id, Name = c.Name })
                .ToList();
            cycles.Insert(0, new ComboItem { Id = 0, Name = "-- جميع الدورات - All Cycles --" });
            reportCycleComboBox.DataSource = cycles;
            reportCycleComboBox.DisplayMember = "Name";
            reportCycleComboBox.ValueMember = "Id";
        }

        private void GenerateReportButton_Click(object? sender, EventArgs e)
        {
            int selectedTab = reportsTabControl.SelectedIndex;
            
            switch (selectedTab)
            {
                case 0:
                    GenerateQualityTestReport();
                    break;
                case 1:
                    GenerateHealthInspectionReport();
                    break;
                case 2:
                    GenerateHACCPComplianceReport();
                    break;
                case 3:
                    GenerateCertificationsReport();
                    break;
                case 4:
                    GenerateQualityTrendsReport();
                    break;
            }
        }

        private void GenerateQualityTestReport()
        {
            var startDate = reportStartDatePicker.Value.Date;
            var endDate = reportEndDatePicker.Value.Date;
            int? cycleId = reportCycleComboBox.SelectedValue is int id && id > 0 ? id : (int?)null;

            var query = _context.QualityTests
                .Include(qt => qt.ProductionCycle)
                .Include(qt => qt.Pond)
                .Where(qt => qt.TestDate >= startDate && qt.TestDate <= endDate);

            if (cycleId.HasValue)
                query = query.Where(qt => qt.ProductionCycleId == cycleId.Value);

            var tests = query.ToList();

            // Create report panel
            Panel reportPanel = new Panel { Dock = DockStyle.Fill, AutoScroll = true };
            int yPos = 10;

            // Title
            Label titleLabel = new Label
            {
                Text = "تقرير ملخص اختبارات الجودة - Quality Test Summary Report",
                Location = new Point(10, yPos),
                Size = new Size(800, 30),
                Font = new Font("Arial", 14, FontStyle.Bold),
                TextAlign = ContentAlignment.MiddleCenter
            };
            reportPanel.Controls.Add(titleLabel);
            yPos += 40;

            // Summary Statistics
            GroupBox summaryBox = new GroupBox
            {
                Text = "الإحصائيات - Statistics",
                Location = new Point(10, yPos),
                Size = new Size(1300, 150)
            };

            int totalTests = tests.Count;
            int passedTests = tests.Count(t => t.TestResult == QualityTestResult.Passed);
            int failedTests = tests.Count(t => t.TestResult == QualityTestResult.Failed);
            int conditionalTests = tests.Count(t => t.TestResult == QualityTestResult.Conditional);
            int pendingTests = tests.Count(t => t.TestResult == QualityTestResult.Pending);
            double passRate = totalTests > 0 ? (passedTests * 100.0 / totalTests) : 0;
            decimal avgScore = tests.Any(t => t.OverallScore.HasValue) ? tests.Where(t => t.OverallScore.HasValue).Average(t => t.OverallScore!.Value) : 0;

            Label statsLabel = new Label
            {
                Text = $@"
إجمالي الاختبارات - Total Tests: {totalTests}
ناجح - Passed: {passedTests} ({passRate:F1}%)
راسب - Failed: {failedTests}
مشروط - Conditional: {conditionalTests}
معلق - Pending: {pendingTests}
متوسط الدرجة - Average Score: {avgScore:F1}%
",
                Location = new Point(20, 30),
                Size = new Size(600, 110),
                Font = new Font("Arial", 10)
            };
            summaryBox.Controls.Add(statsLabel);

            // Sample Type Distribution
            var sampleTypeStats = tests.GroupBy(t => t.SampleType)
                .Select(g => new { Type = g.Key, Count = g.Count() })
                .ToList();

            string sampleTypeText = "توزيع أنواع العينات - Sample Type Distribution:\n";
            foreach (var stat in sampleTypeStats)
            {
                sampleTypeText += $"{stat.Type}: {stat.Count}\n";
            }

            Label sampleTypeLabel = new Label
            {
                Text = sampleTypeText,
                Location = new Point(700, 30),
                Size = new Size(500, 110),
                Font = new Font("Arial", 10)
            };
            summaryBox.Controls.Add(sampleTypeLabel);

            reportPanel.Controls.Add(summaryBox);
            yPos += 160;

            // Detailed Table
            DataGridView detailsGrid = new DataGridView
            {
                Location = new Point(10, yPos),
                Size = new Size(1300, 400),
                AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill,
                ReadOnly = true,
                AllowUserToAddRows = false
            };

            var gridData = tests.Select(t => new
            {
                t.TestNumber,
                TestDate = t.TestDate.ToString("yyyy-MM-dd"),
                Cycle = t.ProductionCycle?.Name ?? "N/A",
                Pond = t.Pond?.Name ?? "N/A",
                SampleType = t.SampleType.ToString(),
                Result = t.Result.ToString(),
                Score = t.OverallScore?.ToString("F1") ?? "N/A",
                MeetsStandards = (t.MeetsStandards == true) ? "✓" : "✗",
                TestedBy = t.TestedBy,
                ApprovedBy = t.ApprovedBy ?? "Pending"
            }).ToList();

            detailsGrid.DataSource = gridData;
            reportPanel.Controls.Add(detailsGrid);

            // Color code by result
            foreach (DataGridViewRow row in detailsGrid.Rows)
            {
                string result = row.Cells["Result"].Value?.ToString() ?? "";
                if (result == "Passed")
                    row.DefaultCellStyle.BackColor = Color.LightGreen;
                else if (result == "Failed")
                    row.DefaultCellStyle.BackColor = Color.LightCoral;
                else if (result == "Conditional")
                    row.DefaultCellStyle.BackColor = Color.LightYellow;
            }

            // Display report
            if (reportsTabControl.SelectedTab != null)
            {
                if (reportsTabControl.SelectedTab != null)
            {
                if (reportsTabControl.SelectedTab != null)
            {
                if (reportsTabControl.SelectedTab != null)
            {
                if (reportsTabControl.SelectedTab != null)
            {
                reportsTabControl.SelectedTab.Controls.Clear();
                                reportsTabControl.SelectedTab.Controls.Add(reportPanel);
            }
            }
            }
            }
            }
        }

        private void GenerateHealthInspectionReport()
        {
            var startDate = reportStartDatePicker.Value.Date;
            var endDate = reportEndDatePicker.Value.Date;
            int? cycleId = reportCycleComboBox.SelectedValue is int id && id > 0 ? id : (int?)null;

            var query = _context.HealthInspections
                .Include(hi => hi.ProductionCycle)
                .Include(hi => hi.Pond)
                .Where(hi => hi.InspectionDate >= startDate && hi.InspectionDate <= endDate);

            if (cycleId.HasValue)
                query = query.Where(hi => hi.ProductionCycleId == cycleId.Value);

            var inspections = query.ToList();

            Panel reportPanel = new Panel { Dock = DockStyle.Fill, AutoScroll = true };
            int yPos = 10;

            // Title
            Label titleLabel = new Label
            {
                Text = "تقرير الفحص الصحي - Health Inspection Report",
                Location = new Point(10, yPos),
                Size = new Size(800, 30),
                Font = new Font("Arial", 14, FontStyle.Bold),
                TextAlign = ContentAlignment.MiddleCenter
            };
            reportPanel.Controls.Add(titleLabel);
            yPos += 40;

            // Summary Statistics
            GroupBox summaryBox = new GroupBox
            {
                Text = "الإحصائيات - Statistics",
                Location = new Point(10, yPos),
                Size = new Size(1300, 200)
            };

            int totalInspections = inspections.Count;
            var healthStatusStats = inspections.GroupBy(i => i.OverallHealthStatus).Select(g => new { Status = g.Key, Count = g.Count() }).ToList();
            double avgMortalityRate = inspections.Any() ? (double)inspections.Average(i => i.MortalityRate) : 0;
            int treatmentRequiredCount = inspections.Count(i => i.TreatmentRequired);
            int isolationRequiredCount = inspections.Count(i => i.IsolationRequired);
            int cullingRequiredCount = inspections.Count(i => i.CullingRequired);

            string statsText = $@"
إجمالي الفحوصات - Total Inspections: {totalInspections}
معدل الوفيات - Avg Mortality Rate: {avgMortalityRate:F2}%
تتطلب علاج - Treatment Required: {treatmentRequiredCount}
تتطلب عزل - Isolation Required: {isolationRequiredCount}
تتطلب إعدام - Culling Required: {cullingRequiredCount}

الحالة الصحية - Health Status Distribution:";

            foreach (var stat in healthStatusStats)
            {
                statsText += $"\n{stat.Status}: {stat.Count}";
            }

            Label statsLabel = new Label
            {
                Text = statsText,
                Location = new Point(20, 30),
                Size = new Size(600, 160),
                Font = new Font("Arial", 10)
            };
            summaryBox.Controls.Add(statsLabel);

            // Disease Detection
            int parasiteCount = inspections.Count(i => i.ParasiteDetection == true);
            int bacterialCount = inspections.Count(i => i.BacterialInfection == true);
            int viralCount = inspections.Count(i => i.ViralInfection == true);
            int fungalCount = inspections.Count(i => i.FungalInfection == true);

            string diseaseText = $@"
الأمراض المكتشفة - Detected Diseases:

طفيليات - Parasites: {parasiteCount}
بكتيريا - Bacterial: {bacterialCount}
فيروسات - Viral: {viralCount}
فطريات - Fungal: {fungalCount}
";

            Label diseaseLabel = new Label
            {
                Text = diseaseText,
                Location = new Point(700, 30),
                Size = new Size(500, 160),
                Font = new Font("Arial", 10)
            };
            summaryBox.Controls.Add(diseaseLabel);

            reportPanel.Controls.Add(summaryBox);
            yPos += 210;

            // Detailed Table
            DataGridView detailsGrid = new DataGridView
            {
                Location = new Point(10, yPos),
                Size = new Size(1300, 400),
                AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill,
                ReadOnly = true,
                AllowUserToAddRows = false
            };

            var gridData = inspections.Select(i => new
            {
                i.InspectionNumber,
                Date = i.InspectionDate.ToString("yyyy-MM-dd"),
                Type = i.InspectionType.ToString(),
                Cycle = i.ProductionCycle?.Name ?? "N/A",
                Pond = i.Pond?.Name ?? "N/A",
                HealthStatus = i.OverallHealthStatus.ToString(),
                BodyCondition = i.BodyConditionScore,
                MortalityRate = i.MortalityRate.ToString("F2") + "%",
                TreatmentRequired = i.TreatmentRequired ? "✓" : "",
                Inspector = i.InspectorName,
                Veterinarian = i.VeterinarianName ?? "N/A"
            }).ToList();

            detailsGrid.DataSource = gridData;
            reportPanel.Controls.Add(detailsGrid);

            // Color code by health status
            foreach (DataGridViewRow row in detailsGrid.Rows)
            {
                string status = row.Cells["HealthStatus"].Value?.ToString() ?? "";
                if (status == "Excellent" || status == "Good")
                    row.DefaultCellStyle.BackColor = Color.LightGreen;
                else if (status == "Fair")
                    row.DefaultCellStyle.BackColor = Color.LightYellow;
                else if (status == "Poor" || status == "Critical")
                    row.DefaultCellStyle.BackColor = Color.LightCoral;
            }

            if (reportsTabControl.SelectedTab != null)
            {
                reportsTabControl.SelectedTab.Controls.Clear();
                reportsTabControl.SelectedTab.Controls.Add(reportPanel);
            }
        }

        private void GenerateHACCPComplianceReport()
        {
            var startDate = reportStartDatePicker.Value.Date;
            var endDate = reportEndDatePicker.Value.Date;

            var records = _context.HACCPRecords
                .Include(hr => hr.Pond)
                .Where(hr => hr.RecordDate >= startDate && hr.RecordDate <= endDate)
                .ToList();

            Panel reportPanel = new Panel { Dock = DockStyle.Fill, AutoScroll = true };
            int yPos = 10;

            // Title
            Label titleLabel = new Label
            {
                Text = "تقرير الامتثال لمعايير HACCP - HACCP Compliance Report",
                Location = new Point(10, yPos),
                Size = new Size(800, 30),
                Font = new Font("Arial", 14, FontStyle.Bold),
                TextAlign = ContentAlignment.MiddleCenter
            };
            reportPanel.Controls.Add(titleLabel);
            yPos += 40;

            // Summary Statistics
            GroupBox summaryBox = new GroupBox
            {
                Text = "الإحصائيات - Statistics",
                Location = new Point(10, yPos),
                Size = new Size(1300, 220)
            };

            int totalRecords = records.Count;
            int compliantRecords = records.Count(r => r.Status == ComplianceStatus.Compliant);
            int nonCompliantRecords = records.Count(r => r.Status == ComplianceStatus.NonCompliant);
            int correctedRecords = records.Count(r => r.Status == ComplianceStatus.Corrected);
            double complianceRate = totalRecords > 0 ? (compliantRecords * 100.0 / totalRecords) : 0;
            int deviations = records.Count(r => r.DeviationOccurred);
            int verifiedRecords = records.Count(r => r.Verified);

            string statsText = $@"
إجمالي السجلات - Total Records: {totalRecords}
مطابق - Compliant: {compliantRecords} ({complianceRate:F1}%)
غير مطابق - Non-Compliant: {nonCompliantRecords}
تم التصحيح - Corrected: {correctedRecords}
انحرافات - Deviations: {deviations}
تم التحقق - Verified: {verifiedRecords}
";

            Label statsLabel = new Label
            {
                Text = statsText,
                Location = new Point(20, 30),
                Size = new Size(600, 180),
                Font = new Font("Arial", 10)
            };
            summaryBox.Controls.Add(statsLabel);

            // Control Points Distribution
            var controlPointStats = records.GroupBy(r => r.ControlPoint)
                .Select(g => new { Point = g.Key, Count = g.Count(), NonCompliant = g.Count(r => r.Status == ComplianceStatus.NonCompliant) })
                .OrderByDescending(x => x.NonCompliant)
                .Take(10)
                .ToList();

            string controlPointText = "نقاط التحكم الأكثر مشكلة - Top Issue Control Points:\n\n";
            foreach (var stat in controlPointStats)
            {
                controlPointText += $"{stat.Point}: {stat.Count} total, {stat.NonCompliant} non-compliant\n";
            }

            Label controlPointLabel = new Label
            {
                Text = controlPointText,
                Location = new Point(700, 30),
                Size = new Size(500, 180),
                Font = new Font("Arial", 9)
            };
            summaryBox.Controls.Add(controlPointLabel);

            reportPanel.Controls.Add(summaryBox);
            yPos += 230;

            // Detailed Table
            DataGridView detailsGrid = new DataGridView
            {
                Location = new Point(10, yPos),
                Size = new Size(1300, 400),
                AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill,
                ReadOnly = true,
                AllowUserToAddRows = false
            };

            var gridData = records.Select(r => new
            {
                r.RecordNumber,
                Date = r.RecordDate.ToString("yyyy-MM-dd"),
                ControlPoint = r.ControlPoint.ToString(),
                HazardType = r.HazardType.ToString(),
                RiskLevel = r.RiskLevel,
                Status = r.Status.ToString(),
                WithinLimits = r.IsWithinLimits ? "✓" : "✗",
                Deviation = r.DeviationOccurred ? "✓" : "",
                Verified = r.Verified ? "✓" : "",
                RecordedBy = r.RecordedBy
            }).ToList();

            detailsGrid.DataSource = gridData;
            reportPanel.Controls.Add(detailsGrid);

            // Color code by status
            foreach (DataGridViewRow row in detailsGrid.Rows)
            {
                string status = row.Cells["Status"].Value?.ToString() ?? "";
                if (status == "Compliant")
                    row.DefaultCellStyle.BackColor = Color.LightGreen;
                else if (status == "NonCompliant")
                    row.DefaultCellStyle.BackColor = Color.LightCoral;
                else if (status == "Corrected")
                    row.DefaultCellStyle.BackColor = Color.LightYellow;
            }

            if (reportsTabControl.SelectedTab != null)
            {
                reportsTabControl.SelectedTab.Controls.Clear();
                reportsTabControl.SelectedTab.Controls.Add(reportPanel);
            }
        }

        private void GenerateCertificationsReport()
        {
            var certifications = _context.Certifications
                .Include(c => c.ProductionCycle)
                .ToList();

            Panel reportPanel = new Panel { Dock = DockStyle.Fill, AutoScroll = true };
            int yPos = 10;

            // Title
            Label titleLabel = new Label
            {
                Text = "تقرير حالة الشهادات - Certifications Status Report",
                Location = new Point(10, yPos),
                Size = new Size(800, 30),
                Font = new Font("Arial", 14, FontStyle.Bold),
                TextAlign = ContentAlignment.MiddleCenter
            };
            reportPanel.Controls.Add(titleLabel);
            yPos += 40;

            // Summary Statistics
            GroupBox summaryBox = new GroupBox
            {
                Text = "الإحصائيات - Statistics",
                Location = new Point(10, yPos),
                Size = new Size(1300, 180)
            };

            int totalCerts = certifications.Count;
            int activeCerts = certifications.Count(c => c.Status == CertificationStatus.Active);
            int expiredCerts = certifications.Count(c => c.IsExpired());
            int expiringSoon = certifications.Count(c => c.ExpiringWithin30Days());
            int underRenewal = certifications.Count(c => c.RenewalInProgress);
            double avgAuditScore = certifications.Any(c => c.AuditScore.HasValue) ? certifications.Where(c => c.AuditScore.HasValue).Average(c => c.AuditScore!.Value) : 0;

            string statsText = $@"
إجمالي الشهادات - Total Certifications: {totalCerts}
سارية - Active: {activeCerts}
منتهية - Expired: {expiredCerts}
تنتهي قريباً - Expiring Soon (30 days): {expiringSoon}
قيد التجديد - Under Renewal: {underRenewal}
متوسط درجة التدقيق - Avg Audit Score: {avgAuditScore:F1}%
";

            Label statsLabel = new Label
            {
                Text = statsText,
                Location = new Point(20, 30),
                Size = new Size(600, 140),
                Font = new Font("Arial", 10)
            };
            summaryBox.Controls.Add(statsLabel);

            // Type Distribution
            var typeStats = certifications.GroupBy(c => c.Type)
                .Select(g => new { Type = g.Key, Count = g.Count() })
                .ToList();

            string typeText = "أنواع الشهادات - Certification Types:\n\n";
            foreach (var stat in typeStats)
            {
                typeText += $"{stat.Type}: {stat.Count}\n";
            }

            Label typeLabel = new Label
            {
                Text = typeText,
                Location = new Point(700, 30),
                Size = new Size(500, 140),
                Font = new Font("Arial", 9)
            };
            summaryBox.Controls.Add(typeLabel);

            reportPanel.Controls.Add(summaryBox);
            yPos += 190;

            // Detailed Table
            DataGridView detailsGrid = new DataGridView
            {
                Location = new Point(10, yPos),
                Size = new Size(1300, 400),
                AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill,
                ReadOnly = true,
                AllowUserToAddRows = false
            };

            var gridData = certifications.Select(c => new
            {
                c.CertificateNumber,
                Type = c.Type.ToString(),
                c.CertificateName,
                c.IssuingAuthority,
                IssueDate = c.IssueDate.ToString("yyyy-MM-dd"),
                ExpiryDate = c.ExpiryDate.HasValue ? c.ExpiryDate.Value.ToString("yyyy-MM-dd") : "N/A",
                Status = c.Status.ToString(),
                DaysRemaining = c.DaysUntilExpiry(),
                AuditScore = c.AuditScore.HasValue ? c.AuditScore.Value : (int?)null,
                Cycle = c.ProductionCycle?.Name ?? "N/A"
            }).ToList();

            detailsGrid.DataSource = gridData;
            reportPanel.Controls.Add(detailsGrid);

            // Color code by expiry status
            foreach (DataGridViewRow row in detailsGrid.Rows)
            {
                if (row.Cells["DaysRemaining"].Value != null)
                {
                    int daysRemaining = Convert.ToInt32(row.Cells["DaysRemaining"].Value);
                    if (daysRemaining < 0)
                        row.DefaultCellStyle.BackColor = Color.LightCoral;
                    else if (daysRemaining <= 30)
                        row.DefaultCellStyle.BackColor = Color.LightYellow;
                    else
                        row.DefaultCellStyle.BackColor = Color.LightGreen;
                }
            }

            if (reportsTabControl.SelectedTab != null)
            {
                reportsTabControl.SelectedTab.Controls.Clear();
                reportsTabControl.SelectedTab.Controls.Add(reportPanel);
            }
        }

        private void GenerateQualityTrendsReport()
        {
            var startDate = reportStartDatePicker.Value.Date.AddMonths(-6); // Last 6 months
            var endDate = reportEndDatePicker.Value.Date;

            var tests = _context.QualityTests
                .Where(qt => qt.TestDate >= startDate && qt.TestDate <= endDate)
                .OrderBy(qt => qt.TestDate)
                .ToList();

            Panel reportPanel = new Panel { Dock = DockStyle.Fill, AutoScroll = true };
            int yPos = 10;

            // Title
            Label titleLabel = new Label
            {
                Text = "تحليل اتجاهات الجودة - Quality Trends Analysis",
                Location = new Point(10, yPos),
                Size = new Size(800, 30),
                Font = new Font("Arial", 14, FontStyle.Bold),
                TextAlign = ContentAlignment.MiddleCenter
            };
            reportPanel.Controls.Add(titleLabel);
            yPos += 40;

            // Monthly Trends
            var monthlyTrends = tests
                .GroupBy(t => new { Year = t.TestDate.Year, Month = t.TestDate.Month })
                .Select(g => new
                {
                    Period = $"{g.Key.Year}-{g.Key.Month:D2}",
                    TotalTests = g.Count(),
                    PassedTests = g.Count(t => t.TestResult == QualityTestResult.Passed),
                    PassRate = g.Count() > 0 ? (g.Count(t => t.TestResult == QualityTestResult.Passed) * 100.0 / g.Count()) : 0,
                    AvgScore = g.Where(t => t.OverallScore.HasValue).Any() ? g.Where(t => t.OverallScore.HasValue).Average(t => t.OverallScore!.Value) : 0,
                    AvgWeight = g.Where(t => t.AverageWeight.HasValue).Any() ? g.Where(t => t.AverageWeight.HasValue).Average(t => t.AverageWeight!.Value) : 0
                })
                .OrderBy(x => x.Period)
                .ToList();

            DataGridView trendsGrid = new DataGridView
            {
                Location = new Point(10, yPos),
                Size = new Size(1300, 300),
                AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill,
                ReadOnly = true,
                AllowUserToAddRows = false
            };

            trendsGrid.DataSource = monthlyTrends;
            reportPanel.Controls.Add(trendsGrid);
            yPos += 310;

            // Monthly pass-rate chart
            Label chartLabel = new Label
            {
                Text = "الرسم البياني - Chart: معدل النجاح الشهري - Monthly Pass Rate Trend",
                Location = new Point(10, yPos),
                Size = new Size(600, 30),
                Font = new Font("Arial", 12, FontStyle.Bold)
            };
            reportPanel.Controls.Add(chartLabel);
            yPos += 40;

            // Create simple chart using ChartControl
            try
            {
                ChartControl chart = new ChartControl
                {
                    Location = new Point(10, yPos),
                    Size = new Size(1300, 400),
                    ChartTypeProperty = ChartControl.ChartType.Line
                };

                var chartData = monthlyTrends.Select(t => new { Label = t.Period, Value = t.PassRate }).ToList();
                chart.AddSeries(
                    "معدل النجاح - Pass Rate (%)",
                    chartData.Select(d => d.Value).ToArray(),
                    Color.Green
                );

                reportPanel.Controls.Add(chart);
            }
            catch
            {
                Label noChartLabel = new Label
                {
                    Text = "الرسم البياني غير متوفر - Chart not available",
                    Location = new Point(10, yPos),
                    Size = new Size(600, 30)
                };
                reportPanel.Controls.Add(noChartLabel);
            }

            if (reportsTabControl.SelectedTab != null)
            {
                reportsTabControl.SelectedTab.Controls.Clear();
                reportsTabControl.SelectedTab.Controls.Add(reportPanel);
            }
        }

        private void InitializeComponent()
        {
            this.SuspendLayout();
            this.AutoScaleDimensions = new SizeF(8F, 16F);
            this.AutoScaleMode = AutoScaleMode.Font;
            this.ClientSize = new Size(1400, 900);
            this.Name = "QualityHealthReportsForm";
            this.Text = "تقارير الجودة والصحة - Quality & Health Reports";
            this.ResumeLayout(false);
        }

        private class ComboItem
        {
            public int Id { get; set; }
            public string Name { get; set; } = string.Empty;
        }
    }
}
