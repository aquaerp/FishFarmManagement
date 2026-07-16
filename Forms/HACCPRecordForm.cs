using System;
using FishFarmManager.Services;
using System.Windows.Forms;
using FishFarmManager.Models;
using FishFarmManager.Data;
using System.Linq;
using Microsoft.EntityFrameworkCore;

namespace FishFarmManager.Forms
{
    public partial class HACCPRecordForm : Form
    {
        private readonly FishFarmContext _context;
        private readonly HaccpMonitoringService _monitoringService;
        private HACCPRecord? _currentRecord;

        // Designer controls
        private TabControl mainTabControl = null!;
        private TabPage detailsTabPage = null!;
        private TabPage hazardTabPage = null!;
        private TabPage monitoringTabPage = null!;
        private TabPage actionTabPage = null!;
        
        private ComboBox controlPointComboBox = null!;
        private ComboBox hazardTypeComboBox = null!;
        private ComboBox frequencyComboBox = null!;
        private ComboBox statusComboBox = null!;
        private ComboBox controlMeasureTypeComboBox = null!;
        private ComboBox pondComboBox = null!;
        
        private TextBox recordNumberTextBox = null!;
        private TextBox controlPointDescTextBox = null!;
        private TextBox hazardDescTextBox = null!;
        private TextBox monitoringMethodTextBox = null!;
        private TextBox unitTextBox = null!;
        private TextBox acceptanceTextBox = null!;
        private TextBox deviationDescTextBox = null!;
        private TextBox correctiveTextBox = null!;
        private TextBox actionByTextBox = null!;
        private TextBox verifiedByTextBox = null!;
        private TextBox referenceTextBox = null!;
        private TextBox notesTextBox = null!;
        private TextBox equipmentTextBox = null!;
        private TextBox locationTextBox = null!;
        private TextBox recordedByTextBox = null!;
        private TextBox reviewedByTextBox = null!;
        private TextBox lifecycleStatusTextBox = null!;
        private TextBox rootCauseTextBox = null!;
        private TextBox correctiveOwnerTextBox = null!;
        private TextBox workflowReasonTextBox = null!;
        
        private NumericUpDown severityNumeric = null!;
        private NumericUpDown likelihoodNumeric = null!;
        private NumericUpDown minLimitNumeric = null!;
        private NumericUpDown maxLimitNumeric = null!;
        private NumericUpDown targetValueNumeric = null!;
        private NumericUpDown actualValueNumeric = null!;
        
        private DateTimePicker recordDatePicker = null!;
        private DateTimePicker measurementDatePicker = null!;
        private DateTimePicker actionDatePicker = null!;
        private DateTimePicker verificationDatePicker = null!;
        private DateTimePicker reviewDatePicker = null!;
        private DateTimePicker correctiveDueDatePicker = null!;
        
        private CheckBox withinLimitsCheckBox = null!;
        private CheckBox deviationCheckBox = null!;
        private CheckBox verifiedCheckBox = null!;
        
        private Button saveButton = null!;
        private Button clearButton = null!;
        private Button loadButton = null!;
        private Button applyActionButton = null!;
        private Button verifyEffectiveButton = null!;
        private Button verifyFailedButton = null!;

        public HACCPRecordForm(FishFarmContext context)
        {
            InitializeComponent();
            _context = context;
            _monitoringService = new HaccpMonitoringService(context);
            LoadControlPoints();
            LoadHazardTypes();
            LoadMonitoringFrequencies();
            LoadComplianceStatuses();
            controlMeasureTypeComboBox.DataSource = Enum.GetValues(typeof(FoodSafetyControlMeasureType));
            LoadPonds();
            SetWorkflowState(null);
        }

        private void InitializeComponent()
        {
            // Initialize form
            this.Text = "سجل HACCP - HACCP Record";
            this.Size = new System.Drawing.Size(1000, 700);
            this.StartPosition = FormStartPosition.CenterScreen;
            this.RightToLeft = RightToLeft.Yes;
            this.RightToLeftLayout = true;

            // Initialize TabControl
            mainTabControl = new TabControl
            {
                Dock = DockStyle.Fill,
                RightToLeftLayout = true
            };

            // Details Tab
            detailsTabPage = new TabPage("التفاصيل الأساسية - Basic Details");
            var detailsPanel = new TableLayoutPanel
            {
                Dock = DockStyle.Fill,
                ColumnCount = 4,
                RowCount = 6,
                Padding = new Padding(10),
                AutoScroll = true
            };

            // Row 1: Record Number & Date
            detailsPanel.Controls.Add(new Label { Text = "رقم السجل - Record #:", AutoSize = true }, 0, 0);
            recordNumberTextBox = new TextBox { Dock = DockStyle.Fill };
            detailsPanel.Controls.Add(recordNumberTextBox, 1, 0);

            detailsPanel.Controls.Add(new Label { Text = "التاريخ - Date:", AutoSize = true }, 2, 0);
            recordDatePicker = new DateTimePicker { Dock = DockStyle.Fill, Format = DateTimePickerFormat.Short };
            detailsPanel.Controls.Add(recordDatePicker, 3, 0);

            // Row 2: Pond & Location
            detailsPanel.Controls.Add(new Label { Text = "الحوض - Pond:", AutoSize = true }, 0, 1);
            pondComboBox = new ComboBox { Dock = DockStyle.Fill, DropDownStyle = ComboBoxStyle.DropDownList };
            detailsPanel.Controls.Add(pondComboBox, 1, 1);

            detailsPanel.Controls.Add(new Label { Text = "الموقع - Location:", AutoSize = true }, 2, 1);
            locationTextBox = new TextBox { Dock = DockStyle.Fill };
            detailsPanel.Controls.Add(locationTextBox, 3, 1);

            // Row 3: Equipment
            detailsPanel.Controls.Add(new Label { Text = "المعدات - Equipment:", AutoSize = true }, 0, 2);
            equipmentTextBox = new TextBox { Dock = DockStyle.Fill, Multiline = true, Height = 60 };
            detailsPanel.Controls.Add(equipmentTextBox, 1, 2);
            detailsPanel.SetColumnSpan(equipmentTextBox, 3);

            // Row 4: Recorded By & Reviewed By
            detailsPanel.Controls.Add(new Label { Text = "المسجل - Recorded By:", AutoSize = true }, 0, 3);
            recordedByTextBox = new TextBox { Dock = DockStyle.Fill };
            detailsPanel.Controls.Add(recordedByTextBox, 1, 3);

            detailsPanel.Controls.Add(new Label { Text = "المراجع - Reviewed By:", AutoSize = true }, 2, 3);
            reviewedByTextBox = new TextBox { Dock = DockStyle.Fill };
            detailsPanel.Controls.Add(reviewedByTextBox, 3, 3);

            // Row 5: Review Date & Reference
            detailsPanel.Controls.Add(new Label { Text = "تاريخ المراجعة - Review Date:", AutoSize = true }, 0, 4);
            reviewDatePicker = new DateTimePicker { Dock = DockStyle.Fill, Format = DateTimePickerFormat.Short };
            detailsPanel.Controls.Add(reviewDatePicker, 1, 4);

            detailsPanel.Controls.Add(new Label { Text = "المرجع - Reference:", AutoSize = true }, 2, 4);
            referenceTextBox = new TextBox { Dock = DockStyle.Fill };
            detailsPanel.Controls.Add(referenceTextBox, 3, 4);

            // Row 6: Notes
            detailsPanel.Controls.Add(new Label { Text = "ملاحظات - Notes:", AutoSize = true }, 0, 5);
            notesTextBox = new TextBox { Dock = DockStyle.Fill, Multiline = true, Height = 80 };
            detailsPanel.Controls.Add(notesTextBox, 1, 5);
            detailsPanel.SetColumnSpan(notesTextBox, 3);

            detailsTabPage.Controls.Add(detailsPanel);

            // Hazard Tab
            hazardTabPage = new TabPage("نقطة الخطر - Hazard Point");
            var hazardPanel = new TableLayoutPanel
            {
                Dock = DockStyle.Fill,
                ColumnCount = 4,
                RowCount = 6,
                Padding = new Padding(10),
                AutoScroll = true
            };

            // Row 1: Control Point
            hazardPanel.Controls.Add(new Label { Text = "نقطة التحكم - Control Point:", AutoSize = true }, 0, 0);
            controlPointComboBox = new ComboBox { Dock = DockStyle.Fill, DropDownStyle = ComboBoxStyle.DropDownList };
            hazardPanel.Controls.Add(controlPointComboBox, 1, 0);
            hazardPanel.SetColumnSpan(controlPointComboBox, 3);

            // Row 2: Control Point Description
            hazardPanel.Controls.Add(new Label { Text = "وصف النقطة - Description:", AutoSize = true }, 0, 1);
            controlPointDescTextBox = new TextBox { Dock = DockStyle.Fill, Multiline = true, Height = 60 };
            hazardPanel.Controls.Add(controlPointDescTextBox, 1, 1);
            hazardPanel.SetColumnSpan(controlPointDescTextBox, 3);

            // Row 3: Hazard Type
            hazardPanel.Controls.Add(new Label { Text = "نوع الخطر - Hazard Type:", AutoSize = true }, 0, 2);
            hazardTypeComboBox = new ComboBox { Dock = DockStyle.Fill, DropDownStyle = ComboBoxStyle.DropDownList };
            hazardPanel.Controls.Add(hazardTypeComboBox, 1, 2);
            hazardPanel.SetColumnSpan(hazardTypeComboBox, 3);

            // Row 4: Hazard Description
            hazardPanel.Controls.Add(new Label { Text = "وصف الخطر - Hazard Description:", AutoSize = true }, 0, 3);
            hazardDescTextBox = new TextBox { Dock = DockStyle.Fill, Multiline = true, Height = 60 };
            hazardPanel.Controls.Add(hazardDescTextBox, 1, 3);
            hazardPanel.SetColumnSpan(hazardDescTextBox, 3);

            // Row 5: Severity & Likelihood
            hazardPanel.Controls.Add(new Label { Text = "الشدة - Severity (1-5):", AutoSize = true }, 0, 4);
            severityNumeric = new NumericUpDown { Dock = DockStyle.Fill, Minimum = 1, Maximum = 4, Value = 1 };
            hazardPanel.Controls.Add(severityNumeric, 1, 4);

            hazardPanel.Controls.Add(new Label { Text = "الاحتمالية - Likelihood (1-5):", AutoSize = true }, 2, 4);
            likelihoodNumeric = new NumericUpDown { Dock = DockStyle.Fill, Minimum = 1, Maximum = 5, Value = 1 };
            hazardPanel.Controls.Add(likelihoodNumeric, 3, 4);

            hazardPanel.Controls.Add(new Label { Text = "تصنيف الإجراء - PRP/OPRP/CCP:", AutoSize = true }, 0, 5);
            controlMeasureTypeComboBox = new ComboBox { Dock = DockStyle.Fill, DropDownStyle = ComboBoxStyle.DropDownList };
            hazardPanel.Controls.Add(controlMeasureTypeComboBox, 1, 5);
            hazardPanel.SetColumnSpan(controlMeasureTypeComboBox, 3);

            hazardTabPage.Controls.Add(hazardPanel);

            // Monitoring Tab
            monitoringTabPage = new TabPage("المراقبة - Monitoring");
            var monitoringPanel = new TableLayoutPanel
            {
                Dock = DockStyle.Fill,
                ColumnCount = 4,
                RowCount = 7,
                Padding = new Padding(10),
                AutoScroll = true
            };

            // Row 1: Method & Frequency
            monitoringPanel.Controls.Add(new Label { Text = "طريقة المراقبة - Method:", AutoSize = true }, 0, 0);
            monitoringMethodTextBox = new TextBox { Dock = DockStyle.Fill };
            monitoringPanel.Controls.Add(monitoringMethodTextBox, 1, 0);

            monitoringPanel.Controls.Add(new Label { Text = "التكرار - Frequency:", AutoSize = true }, 2, 0);
            frequencyComboBox = new ComboBox { Dock = DockStyle.Fill, DropDownStyle = ComboBoxStyle.DropDownList };
            monitoringPanel.Controls.Add(frequencyComboBox, 3, 0);

            // Row 2: Measurement Date & Unit
            monitoringPanel.Controls.Add(new Label { Text = "تاريخ القياس - Measurement Date:", AutoSize = true }, 0, 1);
            measurementDatePicker = new DateTimePicker { Dock = DockStyle.Fill, Format = DateTimePickerFormat.Short };
            monitoringPanel.Controls.Add(measurementDatePicker, 1, 1);

            monitoringPanel.Controls.Add(new Label { Text = "الوحدة - Unit:", AutoSize = true }, 2, 1);
            unitTextBox = new TextBox { Dock = DockStyle.Fill };
            monitoringPanel.Controls.Add(unitTextBox, 3, 1);

            // Row 3: Critical Limits
            monitoringPanel.Controls.Add(new Label { Text = "الحد الأدنى - Min Limit:", AutoSize = true }, 0, 2);
            minLimitNumeric = new NumericUpDown { Dock = DockStyle.Fill, DecimalPlaces = 2, Minimum = -1000, Maximum = 10000 };
            monitoringPanel.Controls.Add(minLimitNumeric, 1, 2);

            monitoringPanel.Controls.Add(new Label { Text = "الحد الأقصى - Max Limit:", AutoSize = true }, 2, 2);
            maxLimitNumeric = new NumericUpDown { Dock = DockStyle.Fill, DecimalPlaces = 2, Minimum = -1000, Maximum = 10000 };
            monitoringPanel.Controls.Add(maxLimitNumeric, 3, 2);

            // Row 4: Target & Actual Value
            monitoringPanel.Controls.Add(new Label { Text = "القيمة المستهدفة - Target:", AutoSize = true }, 0, 3);
            targetValueNumeric = new NumericUpDown { Dock = DockStyle.Fill, DecimalPlaces = 2, Minimum = -1000, Maximum = 10000 };
            monitoringPanel.Controls.Add(targetValueNumeric, 1, 3);

            monitoringPanel.Controls.Add(new Label { Text = "القيمة الفعلية - Actual:", AutoSize = true }, 2, 3);
            actualValueNumeric = new NumericUpDown { Dock = DockStyle.Fill, DecimalPlaces = 2, Minimum = -1000, Maximum = 10000 };
            monitoringPanel.Controls.Add(actualValueNumeric, 3, 3);

            // Row 5: Within Limits & Acceptance
            monitoringPanel.Controls.Add(new Label { Text = "ضمن الحدود - Within Limits:", AutoSize = true }, 0, 4);
            withinLimitsCheckBox = new CheckBox { Dock = DockStyle.Fill, Enabled = false };
            monitoringPanel.Controls.Add(withinLimitsCheckBox, 1, 4);

            monitoringPanel.Controls.Add(new Label { Text = "معايير القبول - Acceptance:", AutoSize = true }, 2, 4);
            acceptanceTextBox = new TextBox { Dock = DockStyle.Fill };
            monitoringPanel.Controls.Add(acceptanceTextBox, 3, 4);

            // Row 6: Deviation Check & Status
            monitoringPanel.Controls.Add(new Label { Text = "انحراف - Deviation:", AutoSize = true }, 0, 5);
            deviationCheckBox = new CheckBox { Dock = DockStyle.Fill, Enabled = false };
            monitoringPanel.Controls.Add(deviationCheckBox, 1, 5);

            monitoringPanel.Controls.Add(new Label { Text = "الحالة - Status:", AutoSize = true }, 2, 5);
            statusComboBox = new ComboBox { Dock = DockStyle.Fill, DropDownStyle = ComboBoxStyle.DropDownList, Enabled = false };
            monitoringPanel.Controls.Add(statusComboBox, 3, 5);

            // Row 7: Deviation Description
            monitoringPanel.Controls.Add(new Label { Text = "وصف الانحراف - Deviation Description:", AutoSize = true }, 0, 6);
            deviationDescTextBox = new TextBox { Dock = DockStyle.Fill, Multiline = true, Height = 60 };
            monitoringPanel.Controls.Add(deviationDescTextBox, 1, 6);
            monitoringPanel.SetColumnSpan(deviationDescTextBox, 3);

            monitoringTabPage.Controls.Add(monitoringPanel);

            // Action Tab
            actionTabPage = new TabPage("الإجراءات - Actions");
            var actionPanel = new TableLayoutPanel
            {
                Dock = DockStyle.Fill,
                ColumnCount = 4,
                RowCount = 8,
                Padding = new Padding(10),
                AutoScroll = true
            };

            actionPanel.Controls.Add(new Label { Text = "السبب الجذري - Root Cause:", AutoSize = true }, 0, 0);
            rootCauseTextBox = new TextBox { Dock = DockStyle.Fill, Multiline = true, Height = 60 };
            actionPanel.Controls.Add(rootCauseTextBox, 1, 0);
            actionPanel.SetColumnSpan(rootCauseTextBox, 3);

            // Row 2: Corrective Action
            actionPanel.Controls.Add(new Label { Text = "الإجراء التصحيحي - Corrective Action:", AutoSize = true }, 0, 1);
            correctiveTextBox = new TextBox { Dock = DockStyle.Fill, Multiline = true, Height = 80 };
            actionPanel.Controls.Add(correctiveTextBox, 1, 1);
            actionPanel.SetColumnSpan(correctiveTextBox, 3);

            // Row 3: Action Date & Action By
            actionPanel.Controls.Add(new Label { Text = "تاريخ الإجراء - Action Date:", AutoSize = true }, 0, 2);
            actionDatePicker = new DateTimePicker { Dock = DockStyle.Fill, Format = DateTimePickerFormat.Short };
            actionPanel.Controls.Add(actionDatePicker, 1, 2);

            actionPanel.Controls.Add(new Label { Text = "القائم بالإجراء - Action By:", AutoSize = true }, 2, 2);
            actionByTextBox = new TextBox { Dock = DockStyle.Fill };
            actionPanel.Controls.Add(actionByTextBox, 3, 2);

            actionPanel.Controls.Add(new Label { Text = "مالك الإجراء - Owner:", AutoSize = true }, 0, 3);
            correctiveOwnerTextBox = new TextBox { Dock = DockStyle.Fill };
            actionPanel.Controls.Add(correctiveOwnerTextBox, 1, 3);
            actionPanel.Controls.Add(new Label { Text = "موعد الإغلاق - Due Date:", AutoSize = true }, 2, 3);
            correctiveDueDatePicker = new DateTimePicker { Dock = DockStyle.Fill, Format = DateTimePickerFormat.Short };
            actionPanel.Controls.Add(correctiveDueDatePicker, 3, 3);

            // Row 5: Verified Check & Verified By
            actionPanel.Controls.Add(new Label { Text = "تم التحقق - Verified:", AutoSize = true }, 0, 4);
            verifiedCheckBox = new CheckBox { Dock = DockStyle.Fill, Enabled = false };
            actionPanel.Controls.Add(verifiedCheckBox, 1, 4);

            actionPanel.Controls.Add(new Label { Text = "المحقق المستقل - Verified By:", AutoSize = true }, 2, 4);
            verifiedByTextBox = new TextBox { Dock = DockStyle.Fill };
            actionPanel.Controls.Add(verifiedByTextBox, 3, 4);

            // Row 6: Verification Date and lifecycle
            actionPanel.Controls.Add(new Label { Text = "تاريخ التحقق - Verification Date:", AutoSize = true }, 0, 5);
            verificationDatePicker = new DateTimePicker { Dock = DockStyle.Fill, Format = DateTimePickerFormat.Short };
            actionPanel.Controls.Add(verificationDatePicker, 1, 5);
            actionPanel.Controls.Add(new Label { Text = "دورة السجل - Lifecycle:", AutoSize = true }, 2, 5);
            lifecycleStatusTextBox = new TextBox { Dock = DockStyle.Fill, ReadOnly = true };
            actionPanel.Controls.Add(lifecycleStatusTextBox, 3, 5);

            actionPanel.Controls.Add(new Label { Text = "سبب العملية - Reason:", AutoSize = true }, 0, 6);
            workflowReasonTextBox = new TextBox { Dock = DockStyle.Fill };
            actionPanel.Controls.Add(workflowReasonTextBox, 1, 6);
            actionPanel.SetColumnSpan(workflowReasonTextBox, 3);

            var workflowButtons = new FlowLayoutPanel { Dock = DockStyle.Fill, FlowDirection = FlowDirection.RightToLeft };
            applyActionButton = new Button { Text = "تطبيق الإجراء التصحيحي", AutoSize = true };
            verifyEffectiveButton = new Button { Text = "تحقق فعال وإغلاق", AutoSize = true };
            verifyFailedButton = new Button { Text = "تحقق غير فعال", AutoSize = true };
            applyActionButton.Click += ApplyActionButton_Click;
            verifyEffectiveButton.Click += (_, _) => VerifyEffectiveness(true);
            verifyFailedButton.Click += (_, _) => VerifyEffectiveness(false);
            workflowButtons.Controls.Add(applyActionButton);
            workflowButtons.Controls.Add(verifyEffectiveButton);
            workflowButtons.Controls.Add(verifyFailedButton);
            actionPanel.Controls.Add(workflowButtons, 0, 7);
            actionPanel.SetColumnSpan(workflowButtons, 4);

            actionTabPage.Controls.Add(actionPanel);

            // Add all tabs to TabControl
            mainTabControl.TabPages.Add(detailsTabPage);
            mainTabControl.TabPages.Add(hazardTabPage);
            mainTabControl.TabPages.Add(monitoringTabPage);
            mainTabControl.TabPages.Add(actionTabPage);

            // Button Panel
            var buttonPanel = new FlowLayoutPanel
            {
                Dock = DockStyle.Bottom,
                Height = 50,
                FlowDirection = FlowDirection.RightToLeft,
                Padding = new Padding(10)
            };

            saveButton = new Button
            {
                Text = "حفظ - Save",
                Width = 120,
                Height = 35,
                BackColor = ThemeManager.SecondarySkyBlue,
                ForeColor = System.Drawing.Color.White,
                FlatStyle = FlatStyle.Flat
            };
            saveButton.Click += saveButton_Click;

            clearButton = new Button
            {
                Text = "مسح - Clear",
                Width = 120,
                Height = 35,
                BackColor = System.Drawing.Color.Gray,
                ForeColor = System.Drawing.Color.White,
                FlatStyle = FlatStyle.Flat
            };
            clearButton.Click += ClearButton_Click;

            loadButton = new Button
            {
                Text = "تحميل برقم السجل",
                Width = 150,
                Height = 35,
                FlatStyle = FlatStyle.Flat
            };
            loadButton.Click += LoadButton_Click;

            buttonPanel.Controls.Add(saveButton);
            buttonPanel.Controls.Add(loadButton);
            buttonPanel.Controls.Add(clearButton);

            // Add controls to form
            this.Controls.Add(mainTabControl);
            this.Controls.Add(buttonPanel);
        }

        private void ClearButton_Click(object? sender, EventArgs e)
        {
            ClearForm();
        }

        private void LoadControlPoints()
        {
            controlPointComboBox.DataSource = Enum.GetValues(typeof(HACCPControlPoint));
        }

        private void LoadHazardTypes()
        {
            hazardTypeComboBox.DataSource = Enum.GetValues(typeof(HazardType));
        }

        private void LoadMonitoringFrequencies()
        {
            frequencyComboBox.DataSource = Enum.GetValues(typeof(MonitoringFrequency));
        }

        private void LoadComplianceStatuses()
        {
            statusComboBox.DataSource = Enum.GetValues(typeof(ComplianceStatus));
        }

        private void LoadPonds()
        {
            var ponds = _context.Ponds
                .Select(p => new ComboItem { Id = p.Id, Name = p.Name })
                .ToList();
            pondComboBox.DataSource = ponds;
            pondComboBox.DisplayMember = "Name";
            pondComboBox.ValueMember = "Id";
        }

        private void saveButton_Click(object? sender, EventArgs e)
        {
            try
            {
                if (_currentRecord != null)
                    throw new InvalidOperationException("السجل محمّل بالفعل. استخدم مسار الإجراء أو التحقق، أو امسح النموذج لإنشاء قياس جديد.");
                if (pondComboBox.SelectedValue is not int pondId || pondId <= 0)
                    throw new InvalidOperationException("يجب اختيار الحوض.");

                var actor = string.IsNullOrWhiteSpace(recordedByTextBox.Text)
                    ? Environment.UserName
                    : recordedByTextBox.Text.Trim();
                var request = new HaccpMeasurementRequest(
                    pondId,
                    recordNumberTextBox.Text,
                    recordDatePicker.Value,
                    hazardTypeComboBox.SelectedItem?.ToString() ?? string.Empty,
                    hazardDescTextBox.Text,
                    (HazardSeverity)(int)severityNumeric.Value,
                    (HazardLikelihood)(int)likelihoodNumeric.Value,
                    controlPointComboBox.SelectedItem?.ToString() ?? string.Empty,
                    (FoodSafetyControlMeasureType)(controlMeasureTypeComboBox.SelectedItem
                        ?? FoodSafetyControlMeasureType.Prp),
                    monitoringMethodTextBox.Text,
                    (MonitoringFrequency)(frequencyComboBox.SelectedItem ?? MonitoringFrequency.Daily),
                    unitTextBox.Text,
                    minLimitNumeric.Value,
                    maxLimitNumeric.Value,
                    targetValueNumeric.Value,
                    actualValueNumeric.Value,
                    measurementDatePicker.Value,
                    acceptanceTextBox.Text,
                    deviationDescTextBox.Text,
                    referenceTextBox.Text,
                    equipmentTextBox.Text,
                    locationTextBox.Text,
                    notesTextBox.Text,
                    controlPointDescTextBox.Text);

                _currentRecord = _monitoringService.RecordMeasurement(
                    request, actor, "إدخال قياس HACCP عبر الشاشة المحكومة");
                SetWorkflowState(_currentRecord);
                MessageBox.Show(
                    $"تم تسجيل القياس. المطابقة: {(_currentRecord.IsWithinLimits ? "ضمن الحدود" : "انحراف")}. الحالة: {_currentRecord.LifecycleStatus}",
                    "HACCP", MessageBoxButtons.OK, MessageBoxIcon.Information);
            }
            catch (Exception exception)
            {
                MessageBox.Show(exception.Message, "تعذر حفظ قياس HACCP", MessageBoxButtons.OK, MessageBoxIcon.Warning);
            }
        }

        private void LoadButton_Click(object? sender, EventArgs e)
        {
            var number = recordNumberTextBox.Text.Trim();
            if (string.IsNullOrWhiteSpace(number))
            {
                MessageBox.Show("أدخل رقم السجل أولاً.", "HACCP", MessageBoxButtons.OK, MessageBoxIcon.Information);
                return;
            }

            var record = _context.HACCPRecords.AsNoTracking()
                .OrderByDescending(value => value.Id)
                .FirstOrDefault(value => value.RecordNumber == number);
            if (record == null)
            {
                MessageBox.Show("لم يتم العثور على السجل.", "HACCP", MessageBoxButtons.OK, MessageBoxIcon.Information);
                return;
            }

            _currentRecord = record;
            PopulateRecord(record);
            SetWorkflowState(record);
        }

        private void ApplyActionButton_Click(object? sender, EventArgs e)
        {
            try
            {
                if (_currentRecord == null) throw new InvalidOperationException("احفظ القياس أو حمّل سجلاً أولاً.");
                var actor = string.IsNullOrWhiteSpace(actionByTextBox.Text) ? Environment.UserName : actionByTextBox.Text.Trim();
                _currentRecord = _monitoringService.ApplyCorrectiveAction(
                    _currentRecord.Id, rootCauseTextBox.Text, correctiveTextBox.Text,
                    correctiveOwnerTextBox.Text, actionDatePicker.Value, correctiveDueDatePicker.Value,
                    actor, RequiredWorkflowReason());
                SetWorkflowState(_currentRecord);
                MessageBox.Show("تم توثيق الإجراء وأصبح السجل بانتظار تحقق مستقل.", "HACCP", MessageBoxButtons.OK, MessageBoxIcon.Information);
            }
            catch (Exception exception)
            {
                MessageBox.Show(exception.Message, "تعذر تطبيق الإجراء", MessageBoxButtons.OK, MessageBoxIcon.Warning);
            }
        }

        private void VerifyEffectiveness(bool effective)
        {
            try
            {
                if (_currentRecord == null) throw new InvalidOperationException("احفظ القياس أو حمّل سجلاً أولاً.");
                _currentRecord = _monitoringService.VerifyEffectiveness(
                    _currentRecord.Id, effective, verificationDatePicker.Value,
                    verifiedByTextBox.Text, RequiredWorkflowReason());
                SetWorkflowState(_currentRecord);
                MessageBox.Show(effective ? "تم التحقق المستقل وإغلاق السجل." : "فشل التحقق وأعيد السجل للإجراء التصحيحي.",
                    "HACCP", MessageBoxButtons.OK, effective ? MessageBoxIcon.Information : MessageBoxIcon.Warning);
            }
            catch (Exception exception)
            {
                MessageBox.Show(exception.Message, "تعذر إتمام التحقق", MessageBoxButtons.OK, MessageBoxIcon.Warning);
            }
        }

        private string RequiredWorkflowReason()
        {
            var reason = workflowReasonTextBox.Text.Trim();
            if (string.IsNullOrWhiteSpace(reason)) throw new InvalidOperationException("سبب العملية إلزامي لسجل التدقيق.");
            return reason;
        }

        private void SetWorkflowState(HACCPRecord? record)
        {
            var hasRecord = record != null;
            var closed = record?.LifecycleStatus == HaccpRecordLifecycleStatus.Closed;
            saveButton.Enabled = !hasRecord;
            applyActionButton.Enabled = hasRecord && record!.DeviationOccurred && !closed;
            verifyEffectiveButton.Enabled = hasRecord && record!.LifecycleStatus == HaccpRecordLifecycleStatus.AwaitingVerification;
            verifyFailedButton.Enabled = verifyEffectiveButton.Enabled;
            lifecycleStatusTextBox.Text = record?.LifecycleStatus.ToString() ?? "قياس جديد";
            withinLimitsCheckBox.Checked = record?.IsWithinLimits ?? false;
            deviationCheckBox.Checked = record?.DeviationOccurred ?? false;
            verifiedCheckBox.Checked = record?.Verified ?? false;
            if (record != null) statusComboBox.SelectedItem = record.Status;
            if (record != null && actionDatePicker.Value < record.MeasurementTime)
                actionDatePicker.Value = record.MeasurementTime;
            if (record?.VerificationDate is DateTime verificationDate)
                verificationDatePicker.Value = verificationDate;
        }

        private void PopulateRecord(HACCPRecord record)
        {
            recordDatePicker.Value = record.RecordDate;
            pondComboBox.SelectedValue = record.PondId;
            locationTextBox.Text = record.Location;
            equipmentTextBox.Text = record.EquipmentUsed;
            recordedByTextBox.Text = record.RecordedBy;
            reviewedByTextBox.Text = record.ReviewedBy;
            referenceTextBox.Text = record.ReferenceDocument;
            notesTextBox.Text = record.Notes;
            if (Enum.TryParse<HACCPControlPoint>(record.ControlPoint, out var controlPoint)) controlPointComboBox.SelectedItem = controlPoint;
            controlPointDescTextBox.Text = record.ControlPointDescription;
            if (Enum.TryParse<HazardType>(record.HazardType, out var hazardType)) hazardTypeComboBox.SelectedItem = hazardType;
            hazardDescTextBox.Text = record.HazardDescription;
            severityNumeric.Value = (int)record.Severity;
            likelihoodNumeric.Value = (int)record.Likelihood;
            controlMeasureTypeComboBox.SelectedItem = record.ControlMeasureType;
            monitoringMethodTextBox.Text = record.MonitoringMethod;
            frequencyComboBox.SelectedItem = record.Frequency;
            measurementDatePicker.Value = record.MeasurementTime;
            unitTextBox.Text = record.MeasurementUnit;
            minLimitNumeric.Value = record.MinimumLimit;
            maxLimitNumeric.Value = record.MaximumLimit;
            targetValueNumeric.Value = record.TargetValue;
            actualValueNumeric.Value = record.ActualValue;
            acceptanceTextBox.Text = record.AcceptanceCriteria;
            deviationDescTextBox.Text = record.DeviationDescription;
            rootCauseTextBox.Text = record.RootCause;
            correctiveTextBox.Text = record.CorrectiveActions;
            correctiveOwnerTextBox.Text = record.CorrectiveActionOwner;
            actionByTextBox.Text = record.ActionTakenBy;
            verifiedByTextBox.Text = record.VerifiedBy;
            if (record.CorrectiveActionDueDate is DateTime dueDate) correctiveDueDatePicker.Value = dueDate;
            if (record.ActionTakenDate is DateTime actionDate) actionDatePicker.Value = actionDate;
        }

        private void ClearForm()
        {
            recordNumberTextBox.Clear();
            controlPointDescTextBox.Clear();
            hazardDescTextBox.Clear();
            monitoringMethodTextBox.Clear();
            unitTextBox.Clear();
            acceptanceTextBox.Clear();
            deviationDescTextBox.Clear();
            correctiveTextBox.Clear();
            actionByTextBox.Clear();
            verifiedByTextBox.Clear();
            referenceTextBox.Clear();
            notesTextBox.Clear();
            equipmentTextBox.Clear();
            locationTextBox.Clear();
            recordedByTextBox.Clear();
            reviewedByTextBox.Clear();
            rootCauseTextBox.Clear();
            correctiveOwnerTextBox.Clear();
            workflowReasonTextBox.Clear();
            minLimitNumeric.Value = 0;
            maxLimitNumeric.Value = 0;
            targetValueNumeric.Value = 0;
            actualValueNumeric.Value = 0;
            severityNumeric.Value = 1;
            likelihoodNumeric.Value = 1;
            _currentRecord = null;
            SetWorkflowState(null);
        }

        // Helper class for ComboBox binding
        private class ComboItem
        {
            public int Id { get; set; }
            public string Name { get; set; } = string.Empty;
        }
    }
}
