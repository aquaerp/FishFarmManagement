using System;
using FishFarmManager.Services;
using System.Windows.Forms;
using FishFarmManager.Models;
using FishFarmManager.Data;
using System.Linq;

namespace FishFarmManager.Forms
{
    public partial class HealthInspectionForm : Form
    {
        private readonly FishFarmContext _context;
        private HealthInspection? _currentInspection;

        // Designer controls
        private TabControl mainTabControl = null!;
        private TabPage basicTabPage = null!;
        private TabPage physicalTabPage = null!;
        private TabPage behaviorTabPage = null!;
        private TabPage labTabPage = null!;
        private TabPage diagnosisTabPage = null!;
        private TabPage treatmentTabPage = null!;
        
        private ComboBox cycleComboBox = null!;
        private ComboBox pondComboBox = null!;
        private ComboBox inspectionTypeComboBox = null!;
        private ComboBox healthStatusComboBox = null!;
        
        private TextBox inspectionNumberTextBox = null!;
        private TextBox parasiteTypeTextBox = null!;
        private TextBox bacteriaTypeTextBox = null!;
        private TextBox virusTypeTextBox = null!;
        private TextBox fungusTypeTextBox = null!;
        private TextBox primaryDiagnosisTextBox = null!;
        private TextBox secondaryDiagnosisTextBox = null!;
        private TextBox treatmentTextBox = null!;
        private TextBox preventiveTextBox = null!;
        private TextBox followUpNotesTextBox = null!;
        private TextBox inspectorTextBox = null!;
        private TextBox vetTextBox = null!;
        private TextBox labTextBox = null!;
        private TextBox generalTextBox = null!;
        
        private NumericUpDown sampleSizeNumeric = null!;
        private NumericUpDown samplePercentageNumeric = null!;
        private NumericUpDown healthyNumeric = null!;
        private NumericUpDown sickNumeric = null!;
        private NumericUpDown deadNumeric = null!;
        private NumericUpDown bodyConditionNumeric = null!;
        private NumericUpDown mortalityRateNumeric = null!;
        
        private DateTimePicker inspectionDatePicker = null!;
        private DateTimePicker followUpDatePicker = null!;
        
        private CheckBox skinLesionsCheckBox = null!;
        private CheckBox finDamageCheckBox = null!;
        private CheckBox eyeProblemsCheckBox = null!;
        private CheckBox gillProblemsCheckBox = null!;
        private CheckBox bloatingCheckBox = null!;
        private CheckBox discolorationCheckBox = null!;
        private CheckBox normalSwimmingCheckBox = null!;
        private CheckBox normalFeedingCheckBox = null!;
        private CheckBox lethargyCheckBox = null!;
        private CheckBox gatheringCheckBox = null!;
        private CheckBox gaspingCheckBox = null!;
        private CheckBox parasiteCheckBox = null!;
        private CheckBox bacteriaCheckBox = null!;
        private CheckBox virusCheckBox = null!;
        private CheckBox fungusCheckBox = null!;
        private CheckBox treatmentRequiredCheckBox = null!;
        private CheckBox isolationCheckBox = null!;
        private CheckBox cullingCheckBox = null!;
        
        private Button saveButton = null!;
        private Button clearButton = null!;

        public HealthInspectionForm(FishFarmContext context)
        {
            InitializeComponent();
            _context = context;
            LoadCycles();
            LoadPonds();
            LoadInspectionTypes();
            LoadHealthStatuses();
        }

        private void InitializeComponent()
        {
            // Initialize form
            this.Text = "فحص الصحة - Health Inspection";
            this.Size = new System.Drawing.Size(1200, 800);
            this.StartPosition = FormStartPosition.CenterScreen;
            this.RightToLeft = RightToLeft.Yes;
            this.RightToLeftLayout = true;

            // Initialize TabControl
            mainTabControl = new TabControl
            {
                Dock = DockStyle.Fill,
                RightToLeftLayout = true
            };

            // Basic Tab
            basicTabPage = new TabPage("المعلومات الأساسية - Basic Info");
            var basicPanel = new TableLayoutPanel
            {
                Dock = DockStyle.Fill,
                ColumnCount = 4,
                RowCount = 5,
                Padding = new Padding(10),
                AutoScroll = true
            };

            basicPanel.Controls.Add(new Label { Text = "رقم الفحص - Inspection #:", AutoSize = true }, 0, 0);
            inspectionNumberTextBox = new TextBox { Dock = DockStyle.Fill };
            basicPanel.Controls.Add(inspectionNumberTextBox, 1, 0);

            basicPanel.Controls.Add(new Label { Text = "التاريخ - Date:", AutoSize = true }, 2, 0);
            inspectionDatePicker = new DateTimePicker { Dock = DockStyle.Fill, Format = DateTimePickerFormat.Short };
            basicPanel.Controls.Add(inspectionDatePicker, 3, 0);

            basicPanel.Controls.Add(new Label { Text = "دورة الإنتاج - Cycle:", AutoSize = true }, 0, 1);
            cycleComboBox = new ComboBox { Dock = DockStyle.Fill, DropDownStyle = ComboBoxStyle.DropDownList };
            basicPanel.Controls.Add(cycleComboBox, 1, 1);

            basicPanel.Controls.Add(new Label { Text = "الحوض - Pond:", AutoSize = true }, 2, 1);
            pondComboBox = new ComboBox { Dock = DockStyle.Fill, DropDownStyle = ComboBoxStyle.DropDownList };
            basicPanel.Controls.Add(pondComboBox, 3, 1);

            basicPanel.Controls.Add(new Label { Text = "نوع الفحص - Type:", AutoSize = true }, 0, 2);
            inspectionTypeComboBox = new ComboBox { Dock = DockStyle.Fill, DropDownStyle = ComboBoxStyle.DropDownList };
            basicPanel.Controls.Add(inspectionTypeComboBox, 1, 2);
            basicPanel.SetColumnSpan(inspectionTypeComboBox, 3);

            basicPanel.Controls.Add(new Label { Text = "حجم العينة - Sample Size:", AutoSize = true }, 0, 3);
            sampleSizeNumeric = new NumericUpDown { Dock = DockStyle.Fill, Maximum = 100000 };
            basicPanel.Controls.Add(sampleSizeNumeric, 1, 3);

            basicPanel.Controls.Add(new Label { Text = "نسبة العينة % - Sample %:", AutoSize = true }, 2, 3);
            samplePercentageNumeric = new NumericUpDown { Dock = DockStyle.Fill, DecimalPlaces = 2, Maximum = 100 };
            basicPanel.Controls.Add(samplePercentageNumeric, 3, 3);

            basicPanel.Controls.Add(new Label { Text = "الفاحص - Inspector:", AutoSize = true }, 0, 4);
            inspectorTextBox = new TextBox { Dock = DockStyle.Fill };
            basicPanel.Controls.Add(inspectorTextBox, 1, 4);

            basicPanel.Controls.Add(new Label { Text = "الطبيب البيطري - Vet:", AutoSize = true }, 2, 4);
            vetTextBox = new TextBox { Dock = DockStyle.Fill };
            basicPanel.Controls.Add(vetTextBox, 3, 4);

            basicTabPage.Controls.Add(basicPanel);

            // Physical Examination Tab
            physicalTabPage = new TabPage("الفحص الجسدي - Physical Exam");
            var physicalPanel = new TableLayoutPanel
            {
                Dock = DockStyle.Fill,
                ColumnCount = 4,
                RowCount = 8,
                Padding = new Padding(10),
                AutoScroll = true
            };

            physicalPanel.Controls.Add(new Label { Text = "عدد الأسماك الصحيحة - Healthy:", AutoSize = true }, 0, 0);
            healthyNumeric = new NumericUpDown { Dock = DockStyle.Fill, Maximum = 1000000 };
            physicalPanel.Controls.Add(healthyNumeric, 1, 0);

            physicalPanel.Controls.Add(new Label { Text = "عدد المريضة - Sick:", AutoSize = true }, 2, 0);
            sickNumeric = new NumericUpDown { Dock = DockStyle.Fill, Maximum = 1000000 };
            physicalPanel.Controls.Add(sickNumeric, 3, 0);

            physicalPanel.Controls.Add(new Label { Text = "عدد الميتة - Dead:", AutoSize = true }, 0, 1);
            deadNumeric = new NumericUpDown { Dock = DockStyle.Fill, Maximum = 1000000 };
            physicalPanel.Controls.Add(deadNumeric, 1, 1);

            physicalPanel.Controls.Add(new Label { Text = "درجة الحالة الجسدية (1-5):", AutoSize = true }, 2, 1);
            bodyConditionNumeric = new NumericUpDown { Dock = DockStyle.Fill, Minimum = 1, Maximum = 5, Value = 3 };
            physicalPanel.Controls.Add(bodyConditionNumeric, 3, 1);

            physicalPanel.Controls.Add(new Label { Text = "العلامات الجسدية - Physical Signs:", AutoSize = true, Font = new System.Drawing.Font("Arial", 10, System.Drawing.FontStyle.Bold) }, 0, 2);
            physicalPanel.SetColumnSpan(physicalPanel.Controls[physicalPanel.Controls.Count - 1], 4);

            physicalPanel.Controls.Add(new Label { Text = "آفات جلدية - Skin Lesions:", AutoSize = true }, 0, 3);
            skinLesionsCheckBox = new CheckBox { Dock = DockStyle.Fill };
            physicalPanel.Controls.Add(skinLesionsCheckBox, 1, 3);

            physicalPanel.Controls.Add(new Label { Text = "تلف الزعانف - Fin Damage:", AutoSize = true }, 2, 3);
            finDamageCheckBox = new CheckBox { Dock = DockStyle.Fill };
            physicalPanel.Controls.Add(finDamageCheckBox, 3, 3);

            physicalPanel.Controls.Add(new Label { Text = "مشاكل العيون - Eye Problems:", AutoSize = true }, 0, 4);
            eyeProblemsCheckBox = new CheckBox { Dock = DockStyle.Fill };
            physicalPanel.Controls.Add(eyeProblemsCheckBox, 1, 4);

            physicalPanel.Controls.Add(new Label { Text = "مشاكل الخياشيم - Gill Problems:", AutoSize = true }, 2, 4);
            gillProblemsCheckBox = new CheckBox { Dock = DockStyle.Fill };
            physicalPanel.Controls.Add(gillProblemsCheckBox, 3, 4);

            physicalPanel.Controls.Add(new Label { Text = "انتفاخ - Bloating:", AutoSize = true }, 0, 5);
            bloatingCheckBox = new CheckBox { Dock = DockStyle.Fill };
            physicalPanel.Controls.Add(bloatingCheckBox, 1, 5);

            physicalPanel.Controls.Add(new Label { Text = "تغير اللون - Discoloration:", AutoSize = true }, 2, 5);
            discolorationCheckBox = new CheckBox { Dock = DockStyle.Fill };
            physicalPanel.Controls.Add(discolorationCheckBox, 3, 5);

            physicalTabPage.Controls.Add(physicalPanel);

            // Behavior Tab
            behaviorTabPage = new TabPage("السلوك - Behavior");
            var behaviorPanel = new TableLayoutPanel
            {
                Dock = DockStyle.Fill,
                ColumnCount = 4,
                RowCount = 3,
                Padding = new Padding(10),
                AutoScroll = true
            };

            behaviorPanel.Controls.Add(new Label { Text = "سباحة طبيعية - Normal Swimming:", AutoSize = true }, 0, 0);
            normalSwimmingCheckBox = new CheckBox { Dock = DockStyle.Fill };
            behaviorPanel.Controls.Add(normalSwimmingCheckBox, 1, 0);

            behaviorPanel.Controls.Add(new Label { Text = "تغذية طبيعية - Normal Feeding:", AutoSize = true }, 2, 0);
            normalFeedingCheckBox = new CheckBox { Dock = DockStyle.Fill };
            behaviorPanel.Controls.Add(normalFeedingCheckBox, 3, 0);

            behaviorPanel.Controls.Add(new Label { Text = "خمول - Lethargy:", AutoSize = true }, 0, 1);
            lethargyCheckBox = new CheckBox { Dock = DockStyle.Fill };
            behaviorPanel.Controls.Add(lethargyCheckBox, 1, 1);

            behaviorPanel.Controls.Add(new Label { Text = "تجمع غير طبيعي - Gathering:", AutoSize = true }, 2, 1);
            gatheringCheckBox = new CheckBox { Dock = DockStyle.Fill };
            behaviorPanel.Controls.Add(gatheringCheckBox, 3, 1);

            behaviorPanel.Controls.Add(new Label { Text = "لهاث سطحي - Surface Gasping:", AutoSize = true }, 0, 2);
            gaspingCheckBox = new CheckBox { Dock = DockStyle.Fill };
            behaviorPanel.Controls.Add(gaspingCheckBox, 1, 2);

            behaviorTabPage.Controls.Add(behaviorPanel);

            // Laboratory Tests Tab
            labTabPage = new TabPage("الفحوصات المخبرية - Lab Tests");
            var labPanel = new TableLayoutPanel
            {
                Dock = DockStyle.Fill,
                ColumnCount = 4,
                RowCount = 6,
                Padding = new Padding(10),
                AutoScroll = true
            };

            labPanel.Controls.Add(new Label { Text = "المختبر - Laboratory:", AutoSize = true }, 0, 0);
            labTextBox = new TextBox { Dock = DockStyle.Fill };
            labPanel.Controls.Add(labTextBox, 1, 0);
            labPanel.SetColumnSpan(labTextBox, 3);

            labPanel.Controls.Add(new Label { Text = "طفيليات - Parasites:", AutoSize = true }, 0, 1);
            parasiteCheckBox = new CheckBox { Dock = DockStyle.Fill };
            labPanel.Controls.Add(parasiteCheckBox, 1, 1);

            labPanel.Controls.Add(new Label { Text = "الأنواع - Types:", AutoSize = true }, 2, 1);
            parasiteTypeTextBox = new TextBox { Dock = DockStyle.Fill };
            labPanel.Controls.Add(parasiteTypeTextBox, 3, 1);

            labPanel.Controls.Add(new Label { Text = "بكتيريا - Bacteria:", AutoSize = true }, 0, 2);
            bacteriaCheckBox = new CheckBox { Dock = DockStyle.Fill };
            labPanel.Controls.Add(bacteriaCheckBox, 1, 2);

            labPanel.Controls.Add(new Label { Text = "الأنواع - Types:", AutoSize = true }, 2, 2);
            bacteriaTypeTextBox = new TextBox { Dock = DockStyle.Fill };
            labPanel.Controls.Add(bacteriaTypeTextBox, 3, 2);

            labPanel.Controls.Add(new Label { Text = "فيروسات - Virus:", AutoSize = true }, 0, 3);
            virusCheckBox = new CheckBox { Dock = DockStyle.Fill };
            labPanel.Controls.Add(virusCheckBox, 1, 3);

            labPanel.Controls.Add(new Label { Text = "الأنواع - Types:", AutoSize = true }, 2, 3);
            virusTypeTextBox = new TextBox { Dock = DockStyle.Fill };
            labPanel.Controls.Add(virusTypeTextBox, 3, 3);

            labPanel.Controls.Add(new Label { Text = "فطريات - Fungus:", AutoSize = true }, 0, 4);
            fungusCheckBox = new CheckBox { Dock = DockStyle.Fill };
            labPanel.Controls.Add(fungusCheckBox, 1, 4);

            labPanel.Controls.Add(new Label { Text = "الأنواع - Types:", AutoSize = true }, 2, 4);
            fungusTypeTextBox = new TextBox { Dock = DockStyle.Fill };
            labPanel.Controls.Add(fungusTypeTextBox, 3, 4);

            labTabPage.Controls.Add(labPanel);

            // Diagnosis Tab
            diagnosisTabPage = new TabPage("التشخيص - Diagnosis");
            var diagnosisPanel = new TableLayoutPanel
            {
                Dock = DockStyle.Fill,
                ColumnCount = 4,
                RowCount = 5,
                Padding = new Padding(10),
                AutoScroll = true
            };

            diagnosisPanel.Controls.Add(new Label { Text = "التشخيص الأساسي - Primary:", AutoSize = true }, 0, 0);
            primaryDiagnosisTextBox = new TextBox { Dock = DockStyle.Fill, Multiline = true, Height = 60 };
            diagnosisPanel.Controls.Add(primaryDiagnosisTextBox, 1, 0);
            diagnosisPanel.SetColumnSpan(primaryDiagnosisTextBox, 3);

            diagnosisPanel.Controls.Add(new Label { Text = "التشخيص الثانوي - Secondary:", AutoSize = true }, 0, 1);
            secondaryDiagnosisTextBox = new TextBox { Dock = DockStyle.Fill, Multiline = true, Height = 60 };
            diagnosisPanel.Controls.Add(secondaryDiagnosisTextBox, 1, 1);
            diagnosisPanel.SetColumnSpan(secondaryDiagnosisTextBox, 3);

            diagnosisPanel.Controls.Add(new Label { Text = "الحالة الصحية - Health Status:", AutoSize = true }, 0, 2);
            healthStatusComboBox = new ComboBox { Dock = DockStyle.Fill, DropDownStyle = ComboBoxStyle.DropDownList };
            diagnosisPanel.Controls.Add(healthStatusComboBox, 1, 2);

            diagnosisPanel.Controls.Add(new Label { Text = "معدل الوفيات % - Mortality Rate:", AutoSize = true }, 2, 2);
            mortalityRateNumeric = new NumericUpDown { Dock = DockStyle.Fill, DecimalPlaces = 2, Maximum = 100 };
            diagnosisPanel.Controls.Add(mortalityRateNumeric, 3, 2);

            diagnosisPanel.Controls.Add(new Label { Text = "ملاحظات عامة - General Notes:", AutoSize = true }, 0, 3);
            generalTextBox = new TextBox { Dock = DockStyle.Fill, Multiline = true, Height = 80 };
            diagnosisPanel.Controls.Add(generalTextBox, 1, 3);
            diagnosisPanel.SetColumnSpan(generalTextBox, 3);

            diagnosisTabPage.Controls.Add(diagnosisPanel);

            // Treatment Tab
            treatmentTabPage = new TabPage("العلاج والمتابعة - Treatment");
            var treatmentPanel = new TableLayoutPanel
            {
                Dock = DockStyle.Fill,
                ColumnCount = 4,
                RowCount = 6,
                Padding = new Padding(10),
                AutoScroll = true
            };

            treatmentPanel.Controls.Add(new Label { Text = "علاج مطلوب - Treatment Required:", AutoSize = true }, 0, 0);
            treatmentRequiredCheckBox = new CheckBox { Dock = DockStyle.Fill };
            treatmentPanel.Controls.Add(treatmentRequiredCheckBox, 1, 0);

            treatmentPanel.Controls.Add(new Label { Text = "العلاج الموصى به - Recommended:", AutoSize = true }, 0, 1);
            treatmentTextBox = new TextBox { Dock = DockStyle.Fill, Multiline = true, Height = 80 };
            treatmentPanel.Controls.Add(treatmentTextBox, 1, 1);
            treatmentPanel.SetColumnSpan(treatmentTextBox, 3);

            treatmentPanel.Controls.Add(new Label { Text = "عزل مطلوب - Isolation Required:", AutoSize = true }, 0, 2);
            isolationCheckBox = new CheckBox { Dock = DockStyle.Fill };
            treatmentPanel.Controls.Add(isolationCheckBox, 1, 2);

            treatmentPanel.Controls.Add(new Label { Text = "إعدام مطلوب - Culling Required:", AutoSize = true }, 2, 2);
            cullingCheckBox = new CheckBox { Dock = DockStyle.Fill };
            treatmentPanel.Controls.Add(cullingCheckBox, 3, 2);

            treatmentPanel.Controls.Add(new Label { Text = "الإجراءات الوقائية - Preventive:", AutoSize = true }, 0, 3);
            preventiveTextBox = new TextBox { Dock = DockStyle.Fill, Multiline = true, Height = 80 };
            treatmentPanel.Controls.Add(preventiveTextBox, 1, 3);
            treatmentPanel.SetColumnSpan(preventiveTextBox, 3);

            treatmentPanel.Controls.Add(new Label { Text = "تاريخ المتابعة - Follow-up Date:", AutoSize = true }, 0, 4);
            followUpDatePicker = new DateTimePicker { Dock = DockStyle.Fill, Format = DateTimePickerFormat.Short };
            treatmentPanel.Controls.Add(followUpDatePicker, 1, 4);

            treatmentPanel.Controls.Add(new Label { Text = "ملاحظات المتابعة - Follow-up Notes:", AutoSize = true }, 0, 5);
            followUpNotesTextBox = new TextBox { Dock = DockStyle.Fill, Multiline = true, Height = 80 };
            treatmentPanel.Controls.Add(followUpNotesTextBox, 1, 5);
            treatmentPanel.SetColumnSpan(followUpNotesTextBox, 3);

            treatmentTabPage.Controls.Add(treatmentPanel);

            // Add all tabs
            mainTabControl.TabPages.Add(basicTabPage);
            mainTabControl.TabPages.Add(physicalTabPage);
            mainTabControl.TabPages.Add(behaviorTabPage);
            mainTabControl.TabPages.Add(labTabPage);
            mainTabControl.TabPages.Add(diagnosisTabPage);
            mainTabControl.TabPages.Add(treatmentTabPage);

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

            buttonPanel.Controls.Add(saveButton);
            buttonPanel.Controls.Add(clearButton);

            // Add to form
            this.Controls.Add(mainTabControl);
            this.Controls.Add(buttonPanel);
        }

        private void ClearButton_Click(object? sender, EventArgs e)
        {
            ClearForm();
        }

        private void LoadCycles()
        {
            var cycles = _context.ProductionCycles
                .Select(c => new ComboItem { Id = c.Id, Name = c.Name })
                .ToList();
            cycleComboBox.DataSource = cycles;
            cycleComboBox.DisplayMember = "Name";
            cycleComboBox.ValueMember = "Id";
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

        private void LoadInspectionTypes()
        {
            inspectionTypeComboBox.DataSource = Enum.GetValues(typeof(HealthInspectionType));
        }

        private void LoadHealthStatuses()
        {
            healthStatusComboBox.DataSource = Enum.GetValues(typeof(HealthStatus));
        }

        private void saveButton_Click(object? sender, EventArgs e)
        {
            if (_currentInspection == null)
                _currentInspection = new HealthInspection();

            // Basic Info
            _currentInspection.InspectionNumber = inspectionNumberTextBox.Text.Trim();
            _currentInspection.InspectionDate = inspectionDatePicker.Value;
            if (cycleComboBox.SelectedValue is int cycleId && cycleId > 0)
                _currentInspection.ProductionCycleId = cycleId;
            if (pondComboBox.SelectedValue is int pondId && pondId > 0)
                _currentInspection.PondId = pondId;
            if (inspectionTypeComboBox.SelectedItem != null)
                _currentInspection.InspectionType = (HealthInspectionType)inspectionTypeComboBox.SelectedItem;
            _currentInspection.SampleSize = (int)sampleSizeNumeric.Value;
            _currentInspection.SamplePercentage = (decimal)samplePercentageNumeric.Value;

            // Physical Examination
            _currentInspection.HealthyCount = (int)healthyNumeric.Value;
            _currentInspection.SickCount = (int)sickNumeric.Value;
            _currentInspection.DeadCount = (int)deadNumeric.Value;
            _currentInspection.BodyConditionScore = (int)bodyConditionNumeric.Value;
            _currentInspection.HasSkinLesions = skinLesionsCheckBox.Checked;
            _currentInspection.HasFinDamage = finDamageCheckBox.Checked;
            _currentInspection.HasEyeProblems = eyeProblemsCheckBox.Checked;
            _currentInspection.HasGillProblems = gillProblemsCheckBox.Checked;
            _currentInspection.HasBloating = bloatingCheckBox.Checked;
            _currentInspection.HasDiscoloration = discolorationCheckBox.Checked;

            // Behavioral Observations
            _currentInspection.NormalSwimming = normalSwimmingCheckBox.Checked;
            _currentInspection.NormalFeeding = normalFeedingCheckBox.Checked;
            _currentInspection.Lethargy = lethargyCheckBox.Checked;
            _currentInspection.AbnormalGathering = gatheringCheckBox.Checked;
            _currentInspection.SurfaceGasping = gaspingCheckBox.Checked;

            // Laboratory Tests
            _currentInspection.ParasiteDetection = parasiteCheckBox.Checked;
            _currentInspection.ParasiteTypes = parasiteTypeTextBox.Text.Trim();
            _currentInspection.BacterialInfection = bacteriaCheckBox.Checked;
            _currentInspection.BacteriaTypes = bacteriaTypeTextBox.Text.Trim();
            _currentInspection.ViralInfection = virusCheckBox.Checked;
            _currentInspection.VirusTypes = virusTypeTextBox.Text.Trim();
            _currentInspection.FungalInfection = fungusCheckBox.Checked;
            _currentInspection.FungusTypes = fungusTypeTextBox.Text.Trim();

            // Diagnosis
            _currentInspection.PrimaryDiagnosis = primaryDiagnosisTextBox.Text.Trim();
            _currentInspection.SecondaryDiagnosis = secondaryDiagnosisTextBox.Text.Trim();
            if (healthStatusComboBox.SelectedItem != null)
                _currentInspection.OverallHealthStatus = (HealthStatus)healthStatusComboBox.SelectedItem;
            _currentInspection.MortalityRate = (decimal)mortalityRateNumeric.Value;

            // Treatment
            _currentInspection.TreatmentRequired = treatmentRequiredCheckBox.Checked;
            _currentInspection.RecommendedTreatment = treatmentTextBox.Text.Trim();
            _currentInspection.IsolationRequired = isolationCheckBox.Checked;
            _currentInspection.CullingRequired = cullingCheckBox.Checked;

            // Preventive Measures
            _currentInspection.PreventiveMeasures = preventiveTextBox.Text.Trim();

            // Follow-up
            _currentInspection.FollowUpDate = followUpDatePicker.Value;
            _currentInspection.FollowUpNotes = followUpNotesTextBox.Text.Trim();

            // Personnel
            _currentInspection.InspectorName = inspectorTextBox.Text.Trim();
            _currentInspection.VeterinarianName = vetTextBox.Text.Trim();
            _currentInspection.LaboratoryName = labTextBox.Text.Trim();

            // General
            _currentInspection.GeneralObservations = generalTextBox.Text.Trim();

            // Audit Trail
            _currentInspection.UpdatedAt = DateTime.Now;
            _currentInspection.UpdatedBy = Environment.UserName;

            if (_currentInspection.Id == 0)
            {
                _currentInspection.CreatedAt = DateTime.Now;
                _currentInspection.CreatedBy = Environment.UserName;
                _context.HealthInspections.Add(_currentInspection);
            }
            _context.SaveChanges();
            MessageBox.Show("تم حفظ فحص الصحة بنجاح!", "حفظ", MessageBoxButtons.OK, MessageBoxIcon.Information);
            ClearForm();
        }

        private void ClearForm()
        {
            inspectionNumberTextBox.Clear();
            parasiteTypeTextBox.Clear();
            bacteriaTypeTextBox.Clear();
            virusTypeTextBox.Clear();
            fungusTypeTextBox.Clear();
            primaryDiagnosisTextBox.Clear();
            secondaryDiagnosisTextBox.Clear();
            treatmentTextBox.Clear();
            preventiveTextBox.Clear();
            followUpNotesTextBox.Clear();
            inspectorTextBox.Clear();
            vetTextBox.Clear();
            labTextBox.Clear();
            generalTextBox.Clear();
            // ...reset all numeric and checkbox controls...
        }

        // Helper class for ComboBox binding
        private class ComboItem
        {
            public int Id { get; set; }
            public string Name { get; set; } = string.Empty;
        }
    }
}
