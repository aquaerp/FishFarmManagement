using System;
using System.Windows.Forms;
using FishFarmManager.Models;
using FishFarmManager.Data;
using System.Linq;
using Microsoft.EntityFrameworkCore;

namespace FishFarmManager.Forms
{
    public partial class CertificationForm : Form
    {
        private readonly FishFarmContext _context;
        private Certification? _currentCertification;
        private DataGridView certificationsGridView = null!;
        private TextBox certificateNumberTextBox = null!;
        private ComboBox typeComboBox = null!;
        private TextBox certificateNameTextBox = null!;
        private TextBox issuingAuthorityTextBox = null!;
        private TextBox authorityCountryTextBox = null!;
        private TextBox authorityWebsiteTextBox = null!;
        private DateTimePicker issueDatePicker = null!;
        private DateTimePicker expiryDatePicker = null!;
        private ComboBox statusComboBox = null!;
        private TextBox scopeTextBox = null!;
        private TextBox applicableProductsTextBox = null!;
        private ComboBox cycleComboBox = null!;
        private TextBox standardVersionTextBox = null!;
        private TextBox complianceRequirementsTextBox = null!;
        private DateTimePicker lastAuditDatePicker = null!;
        private DateTimePicker nextAuditDatePicker = null!;
        private TextBox auditorNameTextBox = null!;
        private TextBox auditorOrganizationTextBox = null!;
        private ComboBox auditResultComboBox = null!;
        private NumericUpDown auditScoreNumeric = null!;
        private NumericUpDown minorNonConformitiesNumeric = null!;
        private NumericUpDown majorNonConformitiesNumeric = null!;
        private NumericUpDown criticalNonConformitiesNumeric = null!;
        private TextBox nonConformityDetailsTextBox = null!;
        private CheckBox correctiveActionsRequiredCheckBox = null!;
        private TextBox correctiveActionsPlanTextBox = null!;
        private DateTimePicker correctiveActionsDeadlinePicker = null!;
        private CheckBox correctiveActionsCompletedCheckBox = null!;
        private DateTimePicker correctiveActionsCompletionDatePicker = null!;
        private CheckBox renewalInProgressCheckBox = null!;
        private DateTimePicker renewalApplicationDatePicker = null!;
        private DateTimePicker renewalInspectionDatePicker = null!;
        private TextBox renewalStatusTextBox = null!;
        private NumericUpDown certificationCostNumeric = null!;
        private NumericUpDown annualMaintenanceCostNumeric = null!;
        private NumericUpDown renewalCostNumeric = null!;
        private TextBox certificateFilePathTextBox = null!;
        private TextBox auditReportFilePathTextBox = null!;
        private TextBox notesTextBox = null!;
        private TextBox contactPersonTextBox = null!;
        private TextBox contactEmailTextBox = null!;
        private TextBox contactPhoneTextBox = null!;
        private TextBox responsiblePersonTextBox = null!;
        private TextBox departmentTextBox = null!;
        private Button saveButton = null!;
        private Button newButton = null!;
        private Button deleteButton = null!;
        private Button searchButton = null!;
        private Button browseFileButton = null!;
        private Button browseAuditReportButton = null!;
        private Label validityDaysLabel = null!;
        private Label daysUntilExpiryLabel = null!;
        private Label expiryStatusLabel = null!;
        private GroupBox basicInfoGroupBox = null!;
        private GroupBox auditInfoGroupBox = null!;
        private GroupBox correctiveActionsGroupBox = null!;
        private GroupBox renewalGroupBox = null!;
        private GroupBox costsGroupBox = null!;
        private GroupBox documentationGroupBox = null!;
        private GroupBox contactGroupBox = null!;

        public CertificationForm(FishFarmContext context)
        {
            InitializeComponent();
            _context = context;
            InitializeCustomComponents();
            LoadCertificationTypes();
            LoadCertificationStatuses();
            LoadAuditResults();
            LoadCycles();
            LoadCertifications();
        }

        private void InitializeCustomComponents()
        {
            this.Text = "إدارة الشهادات - Certifications Management";
            this.Size = new System.Drawing.Size(1400, 900);
            this.StartPosition = FormStartPosition.CenterScreen;

            // Main layout
            TableLayoutPanel mainLayout = new TableLayoutPanel
            {
                Dock = DockStyle.Fill,
                ColumnCount = 2,
                RowCount = 1,
                Padding = new Padding(10)
            };
            mainLayout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 60F));
            mainLayout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 40F));

            // Right Panel - Form
            Panel rightPanel = new Panel { Dock = DockStyle.Fill, AutoScroll = true };
            TableLayoutPanel formLayout = new TableLayoutPanel
            {
                Dock = DockStyle.Top,
                AutoSize = true,
                ColumnCount = 2,
                Padding = new Padding(5)
            };

            int row = 0;

            // Basic Info Group
            basicInfoGroupBox = new GroupBox { Text = "المعلومات الأساسية - Basic Information", Dock = DockStyle.Top, Height = 350 };
            TableLayoutPanel basicLayout = new TableLayoutPanel { Dock = DockStyle.Fill, ColumnCount = 4, AutoSize = true, Padding = new Padding(10) };

            AddFormField(basicLayout, row++, "رقم الشهادة - Certificate Number:", certificateNumberTextBox = new TextBox { Width = 200 });
            AddFormField(basicLayout, row++, "نوع الشهادة - Type:", typeComboBox = new ComboBox { Width = 200, DropDownStyle = ComboBoxStyle.DropDownList });
            AddFormField(basicLayout, row++, "اسم الشهادة - Name:", certificateNameTextBox = new TextBox { Width = 200 });
            AddFormField(basicLayout, row++, "الجهة المصدرة - Issuing Authority:", issuingAuthorityTextBox = new TextBox { Width = 200 });
            AddFormField(basicLayout, row++, "الدولة - Country:", authorityCountryTextBox = new TextBox { Width = 200 });
            AddFormField(basicLayout, row++, "الموقع - Website:", authorityWebsiteTextBox = new TextBox { Width = 200 });
            AddFormField(basicLayout, row++, "تاريخ الإصدار - Issue Date:", issueDatePicker = new DateTimePicker { Width = 200 });
            AddFormField(basicLayout, row++, "تاريخ الانتهاء - Expiry Date:", expiryDatePicker = new DateTimePicker { Width = 200 });
            AddFormField(basicLayout, row++, "الحالة - Status:", statusComboBox = new ComboBox { Width = 200, DropDownStyle = ComboBoxStyle.DropDownList });
            AddFormField(basicLayout, row++, "النطاق - Scope:", scopeTextBox = new TextBox { Width = 200 });
            AddFormField(basicLayout, row++, "المنتجات - Products:", applicableProductsTextBox = new TextBox { Width = 200 });
            AddFormField(basicLayout, row++, "دورة الإنتاج - Cycle:", cycleComboBox = new ComboBox { Width = 200, DropDownStyle = ComboBoxStyle.DropDownList });
            AddFormField(basicLayout, row++, "إصدار المعيار - Standard Version:", standardVersionTextBox = new TextBox { Width = 200 });

            Panel validityPanel = new Panel { Height = 80, Width = 450 };
            validityDaysLabel = new Label { Text = "مدة الصلاحية: - يوم", Location = new System.Drawing.Point(10, 10), AutoSize = true };
            daysUntilExpiryLabel = new Label { Text = "المتبقي: - يوم", Location = new System.Drawing.Point(10, 35), AutoSize = true };
            expiryStatusLabel = new Label { Text = "الحالة: --", Location = new System.Drawing.Point(10, 60), AutoSize = true, Font = new System.Drawing.Font("Arial", 10, System.Drawing.FontStyle.Bold) };
            validityPanel.Controls.AddRange(new Control[] { validityDaysLabel, daysUntilExpiryLabel, expiryStatusLabel });
            basicLayout.Controls.Add(validityPanel);

            basicInfoGroupBox.Controls.Add(basicLayout);
            formLayout.Controls.Add(basicInfoGroupBox);
            formLayout.SetColumnSpan(basicInfoGroupBox, 2);

            // Audit Info Group
            auditInfoGroupBox = new GroupBox { Text = "معلومات التدقيق - Audit Information", Dock = DockStyle.Top, Height = 300 };
            TableLayoutPanel auditLayout = new TableLayoutPanel { Dock = DockStyle.Fill, ColumnCount = 4, AutoSize = true, Padding = new Padding(10) };
            row = 0;

            AddFormField(auditLayout, row++, "تاريخ آخر تدقيق - Last Audit:", lastAuditDatePicker = new DateTimePicker { Width = 200 });
            AddFormField(auditLayout, row++, "تاريخ التدقيق القادم - Next Audit:", nextAuditDatePicker = new DateTimePicker { Width = 200 });
            AddFormField(auditLayout, row++, "اسم المدقق - Auditor Name:", auditorNameTextBox = new TextBox { Width = 200 });
            AddFormField(auditLayout, row++, "مؤسسة التدقيق - Organization:", auditorOrganizationTextBox = new TextBox { Width = 200 });
            AddFormField(auditLayout, row++, "نتيجة التدقيق - Result:", auditResultComboBox = new ComboBox { Width = 200, DropDownStyle = ComboBoxStyle.DropDownList });
            AddFormField(auditLayout, row++, "درجة التدقيق - Score %:", auditScoreNumeric = new NumericUpDown { Width = 200, Maximum = 100, DecimalPlaces = 2 });
            AddFormField(auditLayout, row++, "عدم مطابقة بسيط - Minor:", minorNonConformitiesNumeric = new NumericUpDown { Width = 200, Maximum = 1000 });
            AddFormField(auditLayout, row++, "عدم مطابقة رئيسي - Major:", majorNonConformitiesNumeric = new NumericUpDown { Width = 200, Maximum = 1000 });
            AddFormField(auditLayout, row++, "عدم مطابقة حرج - Critical:", criticalNonConformitiesNumeric = new NumericUpDown { Width = 200, Maximum = 1000 });
            AddFormField(auditLayout, row++, "تفاصيل عدم المطابقة - Details:", nonConformityDetailsTextBox = new TextBox { Width = 200, Multiline = true, Height = 60 });

            auditInfoGroupBox.Controls.Add(auditLayout);
            formLayout.Controls.Add(auditInfoGroupBox);
            formLayout.SetColumnSpan(auditInfoGroupBox, 2);

            // Corrective Actions Group
            correctiveActionsGroupBox = new GroupBox { Text = "الإجراءات التصحيحية - Corrective Actions", Dock = DockStyle.Top, Height = 200 };
            TableLayoutPanel correctiveLayout = new TableLayoutPanel { Dock = DockStyle.Fill, ColumnCount = 4, AutoSize = true, Padding = new Padding(10) };
            row = 0;

            AddFormField(correctiveLayout, row++, "مطلوب - Required:", correctiveActionsRequiredCheckBox = new CheckBox { Width = 200 });
            AddFormField(correctiveLayout, row++, "الخطة - Plan:", correctiveActionsPlanTextBox = new TextBox { Width = 200, Multiline = true, Height = 60 });
            AddFormField(correctiveLayout, row++, "الموعد النهائي - Deadline:", correctiveActionsDeadlinePicker = new DateTimePicker { Width = 200 });
            AddFormField(correctiveLayout, row++, "مكتملة - Completed:", correctiveActionsCompletedCheckBox = new CheckBox { Width = 200 });
            AddFormField(correctiveLayout, row++, "تاريخ الإكمال - Completion Date:", correctiveActionsCompletionDatePicker = new DateTimePicker { Width = 200 });

            correctiveActionsGroupBox.Controls.Add(correctiveLayout);
            formLayout.Controls.Add(correctiveActionsGroupBox);
            formLayout.SetColumnSpan(correctiveActionsGroupBox, 2);

            // Renewal Group
            renewalGroupBox = new GroupBox { Text = "التجديد - Renewal", Dock = DockStyle.Top, Height = 150 };
            TableLayoutPanel renewalLayout = new TableLayoutPanel { Dock = DockStyle.Fill, ColumnCount = 4, AutoSize = true, Padding = new Padding(10) };
            row = 0;

            AddFormField(renewalLayout, row++, "قيد التجديد - In Progress:", renewalInProgressCheckBox = new CheckBox { Width = 200 });
            AddFormField(renewalLayout, row++, "تاريخ التقديم - Application Date:", renewalApplicationDatePicker = new DateTimePicker { Width = 200 });
            AddFormField(renewalLayout, row++, "تاريخ الفحص - Inspection Date:", renewalInspectionDatePicker = new DateTimePicker { Width = 200 });
            AddFormField(renewalLayout, row++, "الحالة - Status:", renewalStatusTextBox = new TextBox { Width = 200 });

            renewalGroupBox.Controls.Add(renewalLayout);
            formLayout.Controls.Add(renewalGroupBox);
            formLayout.SetColumnSpan(renewalGroupBox, 2);

            // Costs Group
            costsGroupBox = new GroupBox { Text = "التكاليف - Costs", Dock = DockStyle.Top, Height = 120 };
            TableLayoutPanel costsLayout = new TableLayoutPanel { Dock = DockStyle.Fill, ColumnCount = 4, AutoSize = true, Padding = new Padding(10) };
            row = 0;

            AddFormField(costsLayout, row++, "تكلفة الشهادة - Certification:", certificationCostNumeric = new NumericUpDown { Width = 200, Maximum = 1000000, DecimalPlaces = 2 });
            AddFormField(costsLayout, row++, "صيانة سنوية - Annual Maintenance:", annualMaintenanceCostNumeric = new NumericUpDown { Width = 200, Maximum = 1000000, DecimalPlaces = 2 });
            AddFormField(costsLayout, row++, "تكلفة التجديد - Renewal:", renewalCostNumeric = new NumericUpDown { Width = 200, Maximum = 1000000, DecimalPlaces = 2 });

            costsGroupBox.Controls.Add(costsLayout);
            formLayout.Controls.Add(costsGroupBox);
            formLayout.SetColumnSpan(costsGroupBox, 2);

            // Documentation Group
            documentationGroupBox = new GroupBox { Text = "المستندات - Documentation", Dock = DockStyle.Top, Height = 120 };
            TableLayoutPanel documentationLayout = new TableLayoutPanel { Dock = DockStyle.Fill, ColumnCount = 3, AutoSize = true, Padding = new Padding(10) };
            row = 0;

            Label certificateFileLabel = new Label { Text = "ملف الشهادة - Certificate File:", Width = 200, TextAlign = System.Drawing.ContentAlignment.MiddleLeft };
            certificateFilePathTextBox = new TextBox { Width = 300, ReadOnly = true };
            browseFileButton = new Button { Text = "استعراض - Browse", Width = 100 };
            browseFileButton.Click += BrowseFileButton_Click;
            documentationLayout.Controls.Add(certificateFileLabel);
            documentationLayout.Controls.Add(certificateFilePathTextBox);
            documentationLayout.Controls.Add(browseFileButton);

            Label auditReportLabel = new Label { Text = "تقرير التدقيق - Audit Report:", Width = 200, TextAlign = System.Drawing.ContentAlignment.MiddleLeft };
            auditReportFilePathTextBox = new TextBox { Width = 300, ReadOnly = true };
            browseAuditReportButton = new Button { Text = "استعراض - Browse", Width = 100 };
            browseAuditReportButton.Click += BrowseAuditReportButton_Click;
            documentationLayout.Controls.Add(auditReportLabel);
            documentationLayout.Controls.Add(auditReportFilePathTextBox);
            documentationLayout.Controls.Add(browseAuditReportButton);

            AddFormField(documentationLayout, row++, "ملاحظات - Notes:", notesTextBox = new TextBox { Width = 300, Multiline = true, Height = 60 });

            documentationGroupBox.Controls.Add(documentationLayout);
            formLayout.Controls.Add(documentationGroupBox);
            formLayout.SetColumnSpan(documentationGroupBox, 2);

            // Contact Group
            contactGroupBox = new GroupBox { Text = "معلومات الاتصال - Contact Information", Dock = DockStyle.Top, Height = 150 };
            TableLayoutPanel contactLayout = new TableLayoutPanel { Dock = DockStyle.Fill, ColumnCount = 4, AutoSize = true, Padding = new Padding(10) };
            row = 0;

            AddFormField(contactLayout, row++, "جهة الاتصال - Contact Person:", contactPersonTextBox = new TextBox { Width = 200 });
            AddFormField(contactLayout, row++, "البريد - Email:", contactEmailTextBox = new TextBox { Width = 200 });
            AddFormField(contactLayout, row++, "الهاتف - Phone:", contactPhoneTextBox = new TextBox { Width = 200 });
            AddFormField(contactLayout, row++, "المسؤول - Responsible Person:", responsiblePersonTextBox = new TextBox { Width = 200 });
            AddFormField(contactLayout, row++, "القسم - Department:", departmentTextBox = new TextBox { Width = 200 });

            contactGroupBox.Controls.Add(contactLayout);
            formLayout.Controls.Add(contactGroupBox);
            formLayout.SetColumnSpan(contactGroupBox, 2);

            // Buttons
            FlowLayoutPanel buttonPanel = new FlowLayoutPanel
            {
                Dock = DockStyle.Top,
                Height = 50,
                FlowDirection = FlowDirection.RightToLeft,
                Padding = new Padding(5)
            };

            saveButton = new Button { Text = "حفظ - Save", Width = 100, Height = 35 };
            saveButton.Click += SaveButton_Click;
            newButton = new Button { Text = "جديد - New", Width = 100, Height = 35 };
            newButton.Click += NewButton_Click;
            deleteButton = new Button { Text = "حذف - Delete", Width = 100, Height = 35 };
            deleteButton.Click += DeleteButton_Click;

            buttonPanel.Controls.AddRange(new Control[] { saveButton, newButton, deleteButton });
            formLayout.Controls.Add(buttonPanel);
            formLayout.SetColumnSpan(buttonPanel, 2);

            rightPanel.Controls.Add(formLayout);

            // Left Panel - Grid
            Panel leftPanel = new Panel { Dock = DockStyle.Fill };
            
            Label gridLabel = new Label
            {
                Text = "قائمة الشهادات - Certifications List",
                Dock = DockStyle.Top,
                Height = 30,
                TextAlign = System.Drawing.ContentAlignment.MiddleCenter,
                Font = new System.Drawing.Font("Arial", 12, System.Drawing.FontStyle.Bold)
            };

            certificationsGridView = new DataGridView
            {
                Dock = DockStyle.Fill,
                AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill,
                SelectionMode = DataGridViewSelectionMode.FullRowSelect,
                MultiSelect = false,
                ReadOnly = true,
                AllowUserToAddRows = false
            };
            certificationsGridView.CellClick += CertificationsGridView_CellClick;

            Panel searchPanel = new Panel { Dock = DockStyle.Top, Height = 40 };
            searchButton = new Button { Text = "بحث - Search", Width = 100, Location = new System.Drawing.Point(10, 5) };
            searchButton.Click += SearchButton_Click;
            searchPanel.Controls.Add(searchButton);

            leftPanel.Controls.Add(certificationsGridView);
            leftPanel.Controls.Add(searchPanel);
            leftPanel.Controls.Add(gridLabel);

            mainLayout.Controls.Add(leftPanel, 0, 0);
            mainLayout.Controls.Add(rightPanel, 1, 0);

            this.Controls.Add(mainLayout);

            // Event handlers for date changes
            issueDatePicker.ValueChanged += DatePicker_ValueChanged;
            expiryDatePicker.ValueChanged += DatePicker_ValueChanged;
        }

        private void AddFormField(TableLayoutPanel layout, int row, string labelText, Control control)
        {
            Label label = new Label
            {
                Text = labelText,
                AutoSize = true,
                TextAlign = System.Drawing.ContentAlignment.MiddleLeft,
                Padding = new Padding(5)
            };
            layout.Controls.Add(label, 0, row);
            layout.Controls.Add(control, 1, row);
        }

        private void LoadCertificationTypes()
        {
            typeComboBox.DataSource = Enum.GetValues(typeof(CertificationType));
        }

        private void LoadCertificationStatuses()
        {
            statusComboBox.DataSource = Enum.GetValues(typeof(CertificationStatus));
        }

        private void LoadAuditResults()
        {
            auditResultComboBox.Items.Add("");
            foreach (AuditResult result in Enum.GetValues(typeof(AuditResult)))
            {
                auditResultComboBox.Items.Add(result);
            }
        }

        private void LoadCycles()
        {
            var cycles = _context.ProductionCycles
                .Select(c => new ComboItem { Id = c.Id, Name = c.Name })
                .ToList();
            cycles.Insert(0, new ComboItem { Id = 0, Name = "-- اختر دورة إنتاج --" });
            cycleComboBox.DataSource = cycles;
            cycleComboBox.DisplayMember = "Name";
            cycleComboBox.ValueMember = "Id";
        }

        private void LoadCertifications()
        {
            var certifications = _context.Certifications
                .Include(c => c.ProductionCycle)
                .Select(c => new
                {
                    c.Id,
                    c.CertificateNumber,
                    Type = c.Type.ToString(),
                    c.CertificateName,
                    c.IssuingAuthority,
                    c.IssueDate,
                    c.ExpiryDate,
                    Status = c.Status.ToString(),
                    DaysUntilExpiry = c.DaysUntilExpiry(),
                    IsExpired = c.IsExpired(),
                    AuditScore = c.AuditScore
                })
                .ToList();

            certificationsGridView.DataSource = certifications;
            
            // Color code expiring certifications
            foreach (DataGridViewRow row in certificationsGridView.Rows)
            {
                if (row.Cells["IsExpired"].Value != null && (bool)row.Cells["IsExpired"].Value)
                {
                    row.DefaultCellStyle.BackColor = System.Drawing.Color.LightCoral;
                }
                else if (row.Cells["DaysUntilExpiry"].Value != null)
                {
                    int daysUntilExpiry = (int)row.Cells["DaysUntilExpiry"].Value;
                    if (daysUntilExpiry <= 30)
                    {
                        row.DefaultCellStyle.BackColor = System.Drawing.Color.LightYellow;
                    }
                }
            }
        }

        private void CertificationsGridView_CellClick(object? sender, DataGridViewCellEventArgs e)
        {
            if (e.RowIndex >= 0)
            {
                int id = (int)certificationsGridView.Rows[e.RowIndex].Cells["Id"].Value;
                _currentCertification = _context.Certifications
                    .Include(c => c.ProductionCycle)
                    .FirstOrDefault(c => c.Id == id);
                if (_currentCertification != null)
                    PopulateForm(_currentCertification);
            }
        }

        private void PopulateForm(Certification cert)
        {
            certificateNumberTextBox.Text = cert.CertificateNumber;
            typeComboBox.SelectedItem = cert.Type;
            certificateNameTextBox.Text = cert.CertificateName;
            issuingAuthorityTextBox.Text = cert.IssuingAuthority;
            authorityCountryTextBox.Text = cert.AuthorityCountry ?? "";
            authorityWebsiteTextBox.Text = cert.AuthorityWebsite ?? "";
            issueDatePicker.Value = cert.IssueDate;
            expiryDatePicker.Value = cert.ExpiryDate ?? DateTime.Now.AddYears(1);
            statusComboBox.SelectedItem = cert.Status;
            scopeTextBox.Text = cert.Scope ?? "";
            applicableProductsTextBox.Text = cert.ApplicableProducts ?? "";
            if (cert.ProductionCycleId.HasValue)
                cycleComboBox.SelectedValue = cert.ProductionCycleId.Value;
            standardVersionTextBox.Text = cert.StandardVersion ?? "";
            complianceRequirementsTextBox.Text = cert.ComplianceRequirements ?? "";
            
            if (cert.LastAuditDate.HasValue)
                lastAuditDatePicker.Value = cert.LastAuditDate.Value;
            if (cert.NextAuditDate.HasValue)
                nextAuditDatePicker.Value = cert.NextAuditDate.Value;
            auditorNameTextBox.Text = cert.AuditorName ?? "";
            auditorOrganizationTextBox.Text = cert.AuditorOrganization ?? "";
            if (!string.IsNullOrEmpty(cert.LastAuditResult))
                auditResultComboBox.SelectedItem = cert.LastAuditResult;
            if (cert.AuditScore.HasValue)
                auditScoreNumeric.Value = (decimal)cert.AuditScore.Value;
            if (cert.MinorNonConformities.HasValue)
                minorNonConformitiesNumeric.Value = cert.MinorNonConformities.Value;
            if (cert.MajorNonConformities.HasValue)
                majorNonConformitiesNumeric.Value = cert.MajorNonConformities.Value;
            if (cert.CriticalNonConformities.HasValue)
                criticalNonConformitiesNumeric.Value = cert.CriticalNonConformities.Value;
            nonConformityDetailsTextBox.Text = cert.NonConformityDetails ?? "";
            
            correctiveActionsRequiredCheckBox.Checked = cert.CorrectiveActionsRequired;
            correctiveActionsPlanTextBox.Text = cert.CorrectiveActionsPlan ?? "";
            if (cert.CorrectiveActionsDeadline.HasValue)
                correctiveActionsDeadlinePicker.Value = cert.CorrectiveActionsDeadline.Value;
            correctiveActionsCompletedCheckBox.Checked = cert.CorrectiveActionsCompleted;
            if (cert.CorrectiveActionsCompletionDate.HasValue)
                correctiveActionsCompletionDatePicker.Value = cert.CorrectiveActionsCompletionDate.Value;
            
            renewalInProgressCheckBox.Checked = cert.RenewalInProgress;
            if (cert.RenewalApplicationDate.HasValue)
                renewalApplicationDatePicker.Value = cert.RenewalApplicationDate.Value;
            if (cert.RenewalInspectionDate.HasValue)
                renewalInspectionDatePicker.Value = cert.RenewalInspectionDate.Value;
            renewalStatusTextBox.Text = cert.RenewalStatus ?? "";
            
            if (cert.CertificationCost.HasValue)
                certificationCostNumeric.Value = (decimal)cert.CertificationCost.Value;
            if (cert.AnnualMaintenanceCost.HasValue)
                annualMaintenanceCostNumeric.Value = (decimal)cert.AnnualMaintenanceCost.Value;
            if (cert.RenewalCost.HasValue)
                renewalCostNumeric.Value = (decimal)cert.RenewalCost.Value;
            
            certificateFilePathTextBox.Text = cert.CertificateFilePath ?? "";
            auditReportFilePathTextBox.Text = cert.AuditReportFilePath ?? "";
            notesTextBox.Text = cert.Notes ?? "";
            
            contactPersonTextBox.Text = cert.ContactPerson ?? "";
            contactEmailTextBox.Text = cert.ContactEmail ?? "";
            contactPhoneTextBox.Text = cert.ContactPhone ?? "";
            responsiblePersonTextBox.Text = cert.ResponsiblePerson;
            departmentTextBox.Text = cert.Department ?? "";
            
            UpdateValidityLabels();
        }

        private void SaveButton_Click(object? sender, EventArgs e)
        {
            if (string.IsNullOrWhiteSpace(certificateNumberTextBox.Text))
            {
                MessageBox.Show("يرجى إدخال رقم الشهادة - Please enter certificate number", "خطأ - Error", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            if (_currentCertification == null)
                _currentCertification = new Certification();

            _currentCertification.CertificateNumber = certificateNumberTextBox.Text.Trim();
            if (typeComboBox.SelectedItem is CertificationType ctype)
                _currentCertification.Type = ctype;
            _currentCertification.CertificateName = certificateNameTextBox.Text.Trim();
            _currentCertification.IssuingAuthority = issuingAuthorityTextBox.Text.Trim();
            _currentCertification.AuthorityCountry = authorityCountryTextBox.Text.Trim();
            _currentCertification.AuthorityWebsite = authorityWebsiteTextBox.Text.Trim();
            _currentCertification.IssueDate = issueDatePicker.Value;
            _currentCertification.ExpiryDate = expiryDatePicker.Value;
            if (statusComboBox.SelectedItem is CertificationStatus cstatus)
                _currentCertification.Status = cstatus;
            _currentCertification.Scope = scopeTextBox.Text.Trim();
            _currentCertification.ApplicableProducts = applicableProductsTextBox.Text.Trim();
            
            if (cycleComboBox.SelectedValue is int cycleId && cycleId > 0)
                _currentCertification.ProductionCycleId = cycleId;
            else
                _currentCertification.ProductionCycleId = null;
                
            _currentCertification.StandardVersion = standardVersionTextBox.Text.Trim();
            _currentCertification.ComplianceRequirements = complianceRequirementsTextBox.Text.Trim();
            _currentCertification.LastAuditDate = lastAuditDatePicker.Value;
            _currentCertification.NextAuditDate = nextAuditDatePicker.Value;
            _currentCertification.AuditorName = auditorNameTextBox.Text.Trim();
            _currentCertification.AuditorOrganization = auditorOrganizationTextBox.Text.Trim();
            
            if (auditResultComboBox.SelectedItem is AuditResult ar)
                _currentCertification.LastAuditResult = ar.ToString();
                
            _currentCertification.AuditScore = auditScoreNumeric.Value > 0 ? (int)auditScoreNumeric.Value : null;
            _currentCertification.MinorNonConformities = minorNonConformitiesNumeric.Value > 0 ? (int)minorNonConformitiesNumeric.Value : 0;
            _currentCertification.MajorNonConformities = (int)majorNonConformitiesNumeric.Value;
            _currentCertification.CriticalNonConformities = (int)criticalNonConformitiesNumeric.Value;
            _currentCertification.NonConformityDetails = nonConformityDetailsTextBox.Text.Trim();
            _currentCertification.CorrectiveActionsRequired = correctiveActionsRequiredCheckBox.Checked;
            _currentCertification.CorrectiveActionsPlan = correctiveActionsPlanTextBox.Text.Trim();
            _currentCertification.CorrectiveActionsDeadline = correctiveActionsDeadlinePicker.Value;
            _currentCertification.CorrectiveActionsCompleted = correctiveActionsCompletedCheckBox.Checked;
            _currentCertification.CorrectiveActionsCompletionDate = correctiveActionsCompletionDatePicker.Value;
            _currentCertification.RenewalInProgress = renewalInProgressCheckBox.Checked;
            _currentCertification.RenewalApplicationDate = renewalApplicationDatePicker.Value;
            _currentCertification.RenewalInspectionDate = renewalInspectionDatePicker.Value;
            _currentCertification.RenewalStatus = renewalStatusTextBox.Text.Trim();
            _currentCertification.CertificationCost = certificationCostNumeric.Value > 0 ? (decimal?)certificationCostNumeric.Value : null;
            _currentCertification.AnnualMaintenanceCost = annualMaintenanceCostNumeric.Value > 0 ? (decimal?)annualMaintenanceCostNumeric.Value : null;
            _currentCertification.RenewalCost = renewalCostNumeric.Value > 0 ? (decimal?)renewalCostNumeric.Value : null;
            _currentCertification.CertificateFilePath = certificateFilePathTextBox.Text.Trim();
            _currentCertification.AuditReportFilePath = auditReportFilePathTextBox.Text.Trim();
            _currentCertification.Notes = notesTextBox.Text.Trim();
            _currentCertification.ContactPerson = contactPersonTextBox.Text.Trim();
            _currentCertification.ContactEmail = contactEmailTextBox.Text.Trim();
            _currentCertification.ContactPhone = contactPhoneTextBox.Text.Trim();
            _currentCertification.ResponsiblePerson = responsiblePersonTextBox.Text.Trim();
            _currentCertification.Department = departmentTextBox.Text.Trim();
            _currentCertification.UpdatedAt = DateTime.Now;
            _currentCertification.UpdatedBy = Environment.UserName;

            if (_currentCertification.Id == 0)
            {
                _currentCertification.CreatedAt = DateTime.Now;
                _currentCertification.CreatedBy = Environment.UserName;
                _context.Certifications.Add(_currentCertification);
            }

            _context.SaveChanges();
            MessageBox.Show("تم حفظ الشهادة بنجاح! - Certification saved successfully!", "نجاح - Success", MessageBoxButtons.OK, MessageBoxIcon.Information);
            LoadCertifications();
            ClearForm();
        }

        private void NewButton_Click(object? sender, EventArgs e)
        {
            _currentCertification = null;
            ClearForm();
        }

        private void DeleteButton_Click(object? sender, EventArgs e)
        {
            if (_currentCertification == null || _currentCertification.Id == 0)
            {
                MessageBox.Show("يرجى اختيار شهادة للحذف - Please select a certification to delete", "تحذير - Warning", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            var result = MessageBox.Show("هل أنت متأكد من حذف هذه الشهادة؟ - Are you sure you want to delete this certification?", "تأكيد الحذف - Confirm Delete", MessageBoxButtons.YesNo, MessageBoxIcon.Question);
            if (result == DialogResult.Yes)
            {
                _context.Certifications.Remove(_currentCertification);
                _context.SaveChanges();
                MessageBox.Show("تم حذف الشهادة بنجاح - Certification deleted successfully", "نجاح - Success", MessageBoxButtons.OK, MessageBoxIcon.Information);
                LoadCertifications();
                ClearForm();
            }
        }

        private void SearchButton_Click(object? sender, EventArgs e)
        {
            // Implement search functionality
            LoadCertifications();
        }

        private void BrowseFileButton_Click(object? sender, EventArgs e)
        {
            using (OpenFileDialog openFileDialog = new OpenFileDialog())
            {
                openFileDialog.Filter = "PDF Files|*.pdf|All Files|*.*";
                if (openFileDialog.ShowDialog() == DialogResult.OK)
                {
                    certificateFilePathTextBox.Text = openFileDialog.FileName;
                }
            }
        }

        private void BrowseAuditReportButton_Click(object? sender, EventArgs e)
        {
            using (OpenFileDialog openFileDialog = new OpenFileDialog())
            {
                openFileDialog.Filter = "PDF Files|*.pdf|All Files|*.*";
                if (openFileDialog.ShowDialog() == DialogResult.OK)
                {
                    auditReportFilePathTextBox.Text = openFileDialog.FileName;
                }
            }
        }

        private void DatePicker_ValueChanged(object? sender, EventArgs e)
        {
            UpdateValidityLabels();
        }

        private void UpdateValidityLabels()
        {
            // DateTimePicker.Value is non-nullable; guard only that controls are initialized
            if (issueDatePicker == null || expiryDatePicker == null) return;

            var issueDate = issueDatePicker.Value;
            var expiryDate = expiryDatePicker.Value;
            int validityDays = (expiryDate - issueDate).Days;
            int daysUntilExpiry = (expiryDate - DateTime.Today).Days;

            validityDaysLabel.Text = $"مدة الصلاحية: {validityDays} يوم - Validity: {validityDays} days";
            daysUntilExpiryLabel.Text = $"المتبقي: {daysUntilExpiry} يوم - Remaining: {daysUntilExpiry} days";

            if (daysUntilExpiry < 0)
            {
                expiryStatusLabel.Text = "الحالة: منتهية - Status: EXPIRED";
                expiryStatusLabel.ForeColor = System.Drawing.Color.Red;
            }
            else if (daysUntilExpiry <= 30)
            {
                expiryStatusLabel.Text = "الحالة: قريبة من الانتهاء - Status: EXPIRING SOON";
                expiryStatusLabel.ForeColor = System.Drawing.Color.Orange;
            }
            else
            {
                expiryStatusLabel.Text = "الحالة: سارية - Status: ACTIVE";
                expiryStatusLabel.ForeColor = System.Drawing.Color.Green;
            }
        }

        private void ClearForm()
        {
            _currentCertification = null;
            certificateNumberTextBox.Clear();
            certificateNameTextBox.Clear();
            issuingAuthorityTextBox.Clear();
            authorityCountryTextBox.Clear();
            authorityWebsiteTextBox.Clear();
            issueDatePicker.Value = DateTime.Today;
            expiryDatePicker.Value = DateTime.Today.AddYears(1);
            scopeTextBox.Clear();
            applicableProductsTextBox.Clear();
            cycleComboBox.SelectedIndex = 0;
            standardVersionTextBox.Clear();
            complianceRequirementsTextBox.Clear();
            auditorNameTextBox.Clear();
            auditorOrganizationTextBox.Clear();
            auditScoreNumeric.Value = 0;
            minorNonConformitiesNumeric.Value = 0;
            majorNonConformitiesNumeric.Value = 0;
            criticalNonConformitiesNumeric.Value = 0;
            nonConformityDetailsTextBox.Clear();
            correctiveActionsRequiredCheckBox.Checked = false;
            correctiveActionsPlanTextBox.Clear();
            correctiveActionsCompletedCheckBox.Checked = false;
            renewalInProgressCheckBox.Checked = false;
            renewalStatusTextBox.Clear();
            certificationCostNumeric.Value = 0;
            annualMaintenanceCostNumeric.Value = 0;
            renewalCostNumeric.Value = 0;
            certificateFilePathTextBox.Clear();
            auditReportFilePathTextBox.Clear();
            notesTextBox.Clear();
            contactPersonTextBox.Clear();
            contactEmailTextBox.Clear();
            contactPhoneTextBox.Clear();
            responsiblePersonTextBox.Clear();
            departmentTextBox.Clear();
            UpdateValidityLabels();
        }

        private void InitializeComponent()
        {
            this.SuspendLayout();
            this.AutoScaleDimensions = new System.Drawing.SizeF(8F, 16F);
            this.AutoScaleMode = AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(1400, 900);
            this.Name = "CertificationForm";
            this.Text = "إدارة الشهادات - Certifications Management";
            this.ResumeLayout(false);
        }

        private class ComboItem
        {
            public int Id { get; set; }
            public string Name { get; set; } = string.Empty;
        }
    }
}
