using System;
using FishFarmManager.Services;
using System.Windows.Forms;
using FishFarmManager.Models;
using FishFarmManager.Data;
using System.Linq;

namespace FishFarmManager.Forms
{
    public partial class QualityTestForm : Form
    {
        private readonly FishFarmContext _context;
        private QualityTest? _currentTest;

        // Designer controls
        private TabControl mainTabControl = null!;
        private TabPage sampleTabPage = null!;
        private TabPage physicalTabPage = null!;
        private TabPage chemicalTabPage = null!;
        private TabPage microTabPage = null!;
        private TabPage sensoryTabPage = null!;
        private TabPage metalsTabPage = null!;
        private TabPage resultTabPage = null!;
        
        private ComboBox cycleComboBox = null!;
        private ComboBox pondComboBox = null!;
        private ComboBox sampleTypeComboBox = null!;
        private ComboBox testResultComboBox = null!;
        
        private TextBox testNumberTextBox = null!;
        private TextBox sampleLocationTextBox = null!;
        private TextBox appearanceTextBox = null!;
        private TextBox standardsTextBox = null!;
        private TextBox testedByTextBox = null!;
        private TextBox approvedByTextBox = null!;
        private TextBox laboratoryTextBox = null!;
        private TextBox certificateTextBox = null!;
        
        private NumericUpDown sampleSizeNumeric = null!;
        private NumericUpDown weightNumeric = null!;
        private NumericUpDown lengthNumeric = null!;
        private NumericUpDown uniformityNumeric = null!;
        private NumericUpDown moistureNumeric = null!;
        private NumericUpDown proteinNumeric = null!;
        private NumericUpDown fatNumeric = null!;
        private NumericUpDown ashNumeric = null!;
        private NumericUpDown pHNumeric = null!;
        private NumericUpDown bacteriaNumeric = null!;
        private NumericUpDown coliformNumeric = null!;
        private NumericUpDown colorScoreNumeric = null!;
        private NumericUpDown odorScoreNumeric = null!;
        private NumericUpDown textureScoreNumeric = null!;
        private NumericUpDown tasteScoreNumeric = null!;
        private NumericUpDown mercuryNumeric = null!;
        private NumericUpDown leadNumeric = null!;
        private NumericUpDown cadmiumNumeric = null!;
        private NumericUpDown arsenicNumeric = null!;
        private NumericUpDown overallScoreNumeric = null!;
        
        private DateTimePicker testDatePicker = null!;
        
        private CheckBox salmonellaCheckBox = null!;
        private CheckBox ecoliCheckBox = null!;
        private CheckBox meetsStandardsCheckBox = null!;
        
        private Button saveButton = null!;
        private Button clearButton = null!;

        public QualityTestForm(FishFarmContext context)
        {
            InitializeComponent();
            _context = context;
            LoadCycles();
            LoadPonds();
            LoadSampleTypes();
            LoadTestResults();
        }

        private void InitializeComponent()
        {
            // Initialize form
            this.Text = "اختبار الجودة - Quality Test";
            this.Size = new System.Drawing.Size(1200, 800);
            this.StartPosition = FormStartPosition.CenterScreen;
            this.RightToLeft = RightToLeft.Yes;
            this.RightToLeftLayout = true;

            // Initialize TabControl
            mainTabControl = new TabControl { Dock = DockStyle.Fill, RightToLeftLayout = true };

            // Sample Tab
            sampleTabPage = new TabPage("معلومات العينة - Sample Info");
            var samplePanel = new TableLayoutPanel { Dock = DockStyle.Fill, ColumnCount = 4, RowCount = 4, Padding = new Padding(10), AutoScroll = true };
            
            samplePanel.Controls.Add(new Label { Text = "رقم الاختبار - Test #:", AutoSize = true }, 0, 0);
            testNumberTextBox = new TextBox { Dock = DockStyle.Fill };
            samplePanel.Controls.Add(testNumberTextBox, 1, 0);
            
            samplePanel.Controls.Add(new Label { Text = "التاريخ - Date:", AutoSize = true }, 2, 0);
            testDatePicker = new DateTimePicker { Dock = DockStyle.Fill, Format = DateTimePickerFormat.Short };
            samplePanel.Controls.Add(testDatePicker, 3, 0);
            
            samplePanel.Controls.Add(new Label { Text = "الدورة - Cycle:", AutoSize = true }, 0, 1);
            cycleComboBox = new ComboBox { Dock = DockStyle.Fill, DropDownStyle = ComboBoxStyle.DropDownList };
            samplePanel.Controls.Add(cycleComboBox, 1, 1);
            
            samplePanel.Controls.Add(new Label { Text = "الحوض - Pond:", AutoSize = true }, 2, 1);
            pondComboBox = new ComboBox { Dock = DockStyle.Fill, DropDownStyle = ComboBoxStyle.DropDownList };
            samplePanel.Controls.Add(pondComboBox, 3, 1);
            
            samplePanel.Controls.Add(new Label { Text = "نوع العينة - Sample Type:", AutoSize = true }, 0, 2);
            sampleTypeComboBox = new ComboBox { Dock = DockStyle.Fill, DropDownStyle = ComboBoxStyle.DropDownList };
            samplePanel.Controls.Add(sampleTypeComboBox, 1, 2);
            
            samplePanel.Controls.Add(new Label { Text = "حجم العينة - Sample Size:", AutoSize = true }, 2, 2);
            sampleSizeNumeric = new NumericUpDown { Dock = DockStyle.Fill, Maximum = 10000 };
            samplePanel.Controls.Add(sampleSizeNumeric, 3, 2);
            
            samplePanel.Controls.Add(new Label { Text = "الموقع - Location:", AutoSize = true }, 0, 3);
            sampleLocationTextBox = new TextBox { Dock = DockStyle.Fill };
            samplePanel.Controls.Add(sampleLocationTextBox, 1, 3);
            samplePanel.SetColumnSpan(sampleLocationTextBox, 3);
            
            sampleTabPage.Controls.Add(samplePanel);

            // Physical Tests Tab
            physicalTabPage = new TabPage("الفحوصات الفيزيائية - Physical");
            var physicalPanel = new TableLayoutPanel { Dock = DockStyle.Fill, ColumnCount = 4, RowCount = 3, Padding = new Padding(10), AutoScroll = true };
            
            physicalPanel.Controls.Add(new Label { Text = "متوسط الوزن (جم) - Avg Weight:", AutoSize = true }, 0, 0);
            weightNumeric = new NumericUpDown { Dock = DockStyle.Fill, DecimalPlaces = 2, Maximum = 10000 };
            physicalPanel.Controls.Add(weightNumeric, 1, 0);
            
            physicalPanel.Controls.Add(new Label { Text = "متوسط الطول (سم) - Avg Length:", AutoSize = true }, 2, 0);
            lengthNumeric = new NumericUpDown { Dock = DockStyle.Fill, DecimalPlaces = 2, Maximum = 1000 };
            physicalPanel.Controls.Add(lengthNumeric, 3, 0);
            
            physicalPanel.Controls.Add(new Label { Text = "التوحيد % - Uniformity:", AutoSize = true }, 0, 1);
            uniformityNumeric = new NumericUpDown { Dock = DockStyle.Fill, DecimalPlaces = 2, Maximum = 100 };
            physicalPanel.Controls.Add(uniformityNumeric, 1, 1);
            
            physicalPanel.Controls.Add(new Label { Text = "المظهر - Appearance:", AutoSize = true }, 0, 2);
            appearanceTextBox = new TextBox { Dock = DockStyle.Fill, Multiline = true, Height = 60 };
            physicalPanel.Controls.Add(appearanceTextBox, 1, 2);
            physicalPanel.SetColumnSpan(appearanceTextBox, 3);
            
            physicalTabPage.Controls.Add(physicalPanel);

            // Chemical Tests Tab
            chemicalTabPage = new TabPage("الفحوصات الكيميائية - Chemical");
            var chemicalPanel = new TableLayoutPanel { Dock = DockStyle.Fill, ColumnCount = 4, RowCount = 3, Padding = new Padding(10), AutoScroll = true };
            
            chemicalPanel.Controls.Add(new Label { Text = "الرطوبة % - Moisture:", AutoSize = true }, 0, 0);
            moistureNumeric = new NumericUpDown { Dock = DockStyle.Fill, DecimalPlaces = 2, Maximum = 100 };
            chemicalPanel.Controls.Add(moistureNumeric, 1, 0);
            
            chemicalPanel.Controls.Add(new Label { Text = "البروتين % - Protein:", AutoSize = true }, 2, 0);
            proteinNumeric = new NumericUpDown { Dock = DockStyle.Fill, DecimalPlaces = 2, Maximum = 100 };
            chemicalPanel.Controls.Add(proteinNumeric, 3, 0);
            
            chemicalPanel.Controls.Add(new Label { Text = "الدهون % - Fat:", AutoSize = true }, 0, 1);
            fatNumeric = new NumericUpDown { Dock = DockStyle.Fill, DecimalPlaces = 2, Maximum = 100 };
            chemicalPanel.Controls.Add(fatNumeric, 1, 1);
            
            chemicalPanel.Controls.Add(new Label { Text = "الرماد % - Ash:", AutoSize = true }, 2, 1);
            ashNumeric = new NumericUpDown { Dock = DockStyle.Fill, DecimalPlaces = 2, Maximum = 100 };
            chemicalPanel.Controls.Add(ashNumeric, 3, 1);
            
            chemicalPanel.Controls.Add(new Label { Text = "pH - pH:", AutoSize = true }, 0, 2);
            pHNumeric = new NumericUpDown { Dock = DockStyle.Fill, DecimalPlaces = 2, Minimum = 0, Maximum = 14 };
            chemicalPanel.Controls.Add(pHNumeric, 1, 2);
            
            chemicalTabPage.Controls.Add(chemicalPanel);

            // Microbiological Tab
            microTabPage = new TabPage("الفحوصات المجهرية - Micro");
            var microPanel = new TableLayoutPanel { Dock = DockStyle.Fill, ColumnCount = 4, RowCount = 3, Padding = new Padding(10), AutoScroll = true };
            
            microPanel.Controls.Add(new Label { Text = "عدد البكتيريا - Bacteria Count:", AutoSize = true }, 0, 0);
            bacteriaNumeric = new NumericUpDown { Dock = DockStyle.Fill, Maximum = 1000000 };
            microPanel.Controls.Add(bacteriaNumeric, 1, 0);
            
            microPanel.Controls.Add(new Label { Text = "القولونيات - Coliform Count:", AutoSize = true }, 2, 0);
            coliformNumeric = new NumericUpDown { Dock = DockStyle.Fill, Maximum = 100000 };
            microPanel.Controls.Add(coliformNumeric, 3, 0);
            
            microPanel.Controls.Add(new Label { Text = "وجود السالمونيلا - Salmonella:", AutoSize = true }, 0, 1);
            salmonellaCheckBox = new CheckBox { Dock = DockStyle.Fill };
            microPanel.Controls.Add(salmonellaCheckBox, 1, 1);
            
            microPanel.Controls.Add(new Label { Text = "وجود إي كولاي - E.Coli:", AutoSize = true }, 2, 1);
            ecoliCheckBox = new CheckBox { Dock = DockStyle.Fill };
            microPanel.Controls.Add(ecoliCheckBox, 3, 1);
            
            microTabPage.Controls.Add(microPanel);

            // Sensory Tab
            sensoryTabPage = new TabPage("التقييم الحسي - Sensory");
            var sensoryPanel = new TableLayoutPanel { Dock = DockStyle.Fill, ColumnCount = 4, RowCount = 2, Padding = new Padding(10), AutoScroll = true };
            
            sensoryPanel.Controls.Add(new Label { Text = "درجة اللون (1-10):", AutoSize = true }, 0, 0);
            colorScoreNumeric = new NumericUpDown { Dock = DockStyle.Fill, Minimum = 1, Maximum = 10, Value = 5 };
            sensoryPanel.Controls.Add(colorScoreNumeric, 1, 0);
            
            sensoryPanel.Controls.Add(new Label { Text = "درجة الرائحة (1-10):", AutoSize = true }, 2, 0);
            odorScoreNumeric = new NumericUpDown { Dock = DockStyle.Fill, Minimum = 1, Maximum = 10, Value = 5 };
            sensoryPanel.Controls.Add(odorScoreNumeric, 3, 0);
            
            sensoryPanel.Controls.Add(new Label { Text = "درجة القوام (1-10):", AutoSize = true }, 0, 1);
            textureScoreNumeric = new NumericUpDown { Dock = DockStyle.Fill, Minimum = 1, Maximum = 10, Value = 5 };
            sensoryPanel.Controls.Add(textureScoreNumeric, 1, 1);
            
            sensoryPanel.Controls.Add(new Label { Text = "درجة الطعم (1-10):", AutoSize = true }, 2, 1);
            tasteScoreNumeric = new NumericUpDown { Dock = DockStyle.Fill, Minimum = 1, Maximum = 10, Value = 5 };
            sensoryPanel.Controls.Add(tasteScoreNumeric, 3, 1);
            
            sensoryTabPage.Controls.Add(sensoryPanel);

            // Heavy Metals Tab
            metalsTabPage = new TabPage("المعادن الثقيلة - Metals");
            var metalsPanel = new TableLayoutPanel { Dock = DockStyle.Fill, ColumnCount = 4, RowCount = 2, Padding = new Padding(10), AutoScroll = true };
            
            metalsPanel.Controls.Add(new Label { Text = "الزئبق (ppm) - Mercury:", AutoSize = true }, 0, 0);
            mercuryNumeric = new NumericUpDown { Dock = DockStyle.Fill, DecimalPlaces = 4, Maximum = 10 };
            metalsPanel.Controls.Add(mercuryNumeric, 1, 0);
            
            metalsPanel.Controls.Add(new Label { Text = "الرصاص (ppm) - Lead:", AutoSize = true }, 2, 0);
            leadNumeric = new NumericUpDown { Dock = DockStyle.Fill, DecimalPlaces = 4, Maximum = 10 };
            metalsPanel.Controls.Add(leadNumeric, 3, 0);
            
            metalsPanel.Controls.Add(new Label { Text = "الكادميوم (ppm) - Cadmium:", AutoSize = true }, 0, 1);
            cadmiumNumeric = new NumericUpDown { Dock = DockStyle.Fill, DecimalPlaces = 4, Maximum = 10 };
            metalsPanel.Controls.Add(cadmiumNumeric, 1, 1);
            
            metalsPanel.Controls.Add(new Label { Text = "الزرنيخ (ppm) - Arsenic:", AutoSize = true }, 2, 1);
            arsenicNumeric = new NumericUpDown { Dock = DockStyle.Fill, DecimalPlaces = 4, Maximum = 10 };
            metalsPanel.Controls.Add(arsenicNumeric, 3, 1);
            
            metalsTabPage.Controls.Add(metalsPanel);

            // Result Tab
            resultTabPage = new TabPage("النتيجة - Result");
            var resultPanel = new TableLayoutPanel { Dock = DockStyle.Fill, ColumnCount = 4, RowCount = 6, Padding = new Padding(10), AutoScroll = true };
            
            resultPanel.Controls.Add(new Label { Text = "النتيجة - Result:", AutoSize = true }, 0, 0);
            testResultComboBox = new ComboBox { Dock = DockStyle.Fill, DropDownStyle = ComboBoxStyle.DropDownList };
            resultPanel.Controls.Add(testResultComboBox, 1, 0);
            
            resultPanel.Controls.Add(new Label { Text = "الدرجة الكلية - Overall Score:", AutoSize = true }, 2, 0);
            overallScoreNumeric = new NumericUpDown { Dock = DockStyle.Fill, DecimalPlaces = 2, Maximum = 100 };
            resultPanel.Controls.Add(overallScoreNumeric, 3, 0);
            
            resultPanel.Controls.Add(new Label { Text = "يطابق المعايير - Meets Standards:", AutoSize = true }, 0, 1);
            meetsStandardsCheckBox = new CheckBox { Dock = DockStyle.Fill };
            resultPanel.Controls.Add(meetsStandardsCheckBox, 1, 1);
            
            resultPanel.Controls.Add(new Label { Text = "مرجع المعايير - Standards Reference:", AutoSize = true }, 0, 2);
            standardsTextBox = new TextBox { Dock = DockStyle.Fill };
            resultPanel.Controls.Add(standardsTextBox, 1, 2);
            resultPanel.SetColumnSpan(standardsTextBox, 3);
            
            resultPanel.Controls.Add(new Label { Text = "الفاحص - Tested By:", AutoSize = true }, 0, 3);
            testedByTextBox = new TextBox { Dock = DockStyle.Fill };
            resultPanel.Controls.Add(testedByTextBox, 1, 3);
            
            resultPanel.Controls.Add(new Label { Text = "المعتمد - Approved By:", AutoSize = true }, 2, 3);
            approvedByTextBox = new TextBox { Dock = DockStyle.Fill };
            resultPanel.Controls.Add(approvedByTextBox, 3, 3);
            
            resultPanel.Controls.Add(new Label { Text = "المختبر - Laboratory:", AutoSize = true }, 0, 4);
            laboratoryTextBox = new TextBox { Dock = DockStyle.Fill };
            resultPanel.Controls.Add(laboratoryTextBox, 1, 4);
            
            resultPanel.Controls.Add(new Label { Text = "رقم الشهادة - Certificate #:", AutoSize = true }, 2, 4);
            certificateTextBox = new TextBox { Dock = DockStyle.Fill };
            resultPanel.Controls.Add(certificateTextBox, 3, 4);
            
            resultTabPage.Controls.Add(resultPanel);

            // Add tabs
            mainTabControl.TabPages.Add(sampleTabPage);
            mainTabControl.TabPages.Add(physicalTabPage);
            mainTabControl.TabPages.Add(chemicalTabPage);
            mainTabControl.TabPages.Add(microTabPage);
            mainTabControl.TabPages.Add(sensoryTabPage);
            mainTabControl.TabPages.Add(metalsTabPage);
            mainTabControl.TabPages.Add(resultTabPage);

            // Buttons
            var buttonPanel = new FlowLayoutPanel { Dock = DockStyle.Bottom, Height = 50, FlowDirection = FlowDirection.RightToLeft, Padding = new Padding(10) };
            
            saveButton = new Button { Text = "حفظ - Save", Width = 120, Height = 35, BackColor = ThemeManager.SecondarySkyBlue, ForeColor = System.Drawing.Color.White, FlatStyle = FlatStyle.Flat };
            saveButton.Click += saveButton_Click;
            
            clearButton = new Button { Text = "مسح - Clear", Width = 120, Height = 35, BackColor = System.Drawing.Color.Gray, ForeColor = System.Drawing.Color.White, FlatStyle = FlatStyle.Flat };
            clearButton.Click += ClearButton_Click;
            
            buttonPanel.Controls.Add(saveButton);
            buttonPanel.Controls.Add(clearButton);
            
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

        private void LoadSampleTypes()
        {
            sampleTypeComboBox.DataSource = Enum.GetValues(typeof(QualitySampleType));
        }

        private void LoadTestResults()
        {
            testResultComboBox.DataSource = Enum.GetValues(typeof(QualityTestResult));
        }

        private void saveButton_Click(object? sender, EventArgs e)
        {
            if (_currentTest == null)
                _currentTest = new QualityTest();

            // Sample Info
            _currentTest.TestNumber = testNumberTextBox.Text.Trim();
            _currentTest.TestDate = testDatePicker.Value;
            if (cycleComboBox.SelectedValue is int cycleId && cycleId > 0)
                _currentTest.ProductionCycleId = cycleId;
            if (pondComboBox.SelectedValue is int pondId && pondId > 0)
                _currentTest.PondId = pondId;
            if (sampleTypeComboBox.SelectedItem != null)
                _currentTest.SampleType = (QualitySampleType)sampleTypeComboBox.SelectedItem;
            _currentTest.SampleSize = (int)sampleSizeNumeric.Value;
            _currentTest.SampleLocation = sampleLocationTextBox.Text.Trim();

            // Physical Tests
            _currentTest.AverageWeight = weightNumeric.Value > 0 ? (decimal?)weightNumeric.Value : null;
            _currentTest.AverageLength = lengthNumeric.Value > 0 ? (decimal?)lengthNumeric.Value : null;
            _currentTest.UniformityPercentage = uniformityNumeric.Value > 0 ? (decimal?)uniformityNumeric.Value : null;
            _currentTest.Appearance = appearanceTextBox.Text.Trim();

            // Chemical Tests
            _currentTest.Moisture = moistureNumeric.Value > 0 ? (decimal?)moistureNumeric.Value : null;
            _currentTest.Protein = proteinNumeric.Value > 0 ? (decimal?)proteinNumeric.Value : null;
            _currentTest.Fat = fatNumeric.Value > 0 ? (decimal?)fatNumeric.Value : null;
            _currentTest.Ash = ashNumeric.Value > 0 ? (decimal?)ashNumeric.Value : null;
            _currentTest.pH = pHNumeric.Value > 0 ? (decimal?)pHNumeric.Value : null;

            // Microbiological Tests
            _currentTest.TotalBacteriaCount = (int)bacteriaNumeric.Value;
            _currentTest.ColiformCount = (int)coliformNumeric.Value;
            _currentTest.SalmonellaPresence = salmonellaCheckBox.Checked;
            _currentTest.EColiPresence = ecoliCheckBox.Checked;

            // Sensory Evaluation
            _currentTest.ColorScore = (int)colorScoreNumeric.Value;
            _currentTest.OdorScore = (int)odorScoreNumeric.Value;
            _currentTest.TextureScore = (int)textureScoreNumeric.Value;
            _currentTest.TasteScore = (int)tasteScoreNumeric.Value;

            // Heavy Metals
            _currentTest.Mercury = mercuryNumeric.Value > 0 ? (decimal?)mercuryNumeric.Value : null;
            _currentTest.Lead = leadNumeric.Value > 0 ? (decimal?)leadNumeric.Value : null;
            _currentTest.Cadmium = cadmiumNumeric.Value > 0 ? (decimal?)cadmiumNumeric.Value : null;
            _currentTest.Arsenic = arsenicNumeric.Value > 0 ? (decimal?)arsenicNumeric.Value : null;

            // Result & Compliance
            if (testResultComboBox.SelectedItem != null)
                _currentTest.TestResult = (QualityTestResult)testResultComboBox.SelectedItem;
            _currentTest.OverallScore = overallScoreNumeric.Value > 0 ? (decimal?)overallScoreNumeric.Value : null;
            _currentTest.MeetsStandards = meetsStandardsCheckBox.Checked;
            _currentTest.StandardsReference = standardsTextBox.Text.Trim();

            // Testing Info
            _currentTest.TestedBy = testedByTextBox.Text.Trim();
            _currentTest.ApprovedBy = approvedByTextBox.Text.Trim();
            _currentTest.Laboratory = laboratoryTextBox.Text.Trim();
            _currentTest.CertificateNumber = certificateTextBox.Text.Trim();

            // Audit Trail
            _currentTest.UpdatedAt = DateTime.Now;
            _currentTest.UpdatedBy = Environment.UserName;

            if (_currentTest.Id == 0)
            {
                _currentTest.CreatedAt = DateTime.Now;
                _currentTest.CreatedBy = Environment.UserName;
                _context.QualityTests.Add(_currentTest);
            }
            _context.SaveChanges();
            MessageBox.Show("تم حفظ اختبار الجودة بنجاح!", "حفظ", MessageBoxButtons.OK, MessageBoxIcon.Information);
            ClearForm();
        }

        private void ClearForm()
        {
            testNumberTextBox.Clear();
            sampleLocationTextBox.Clear();
            appearanceTextBox.Clear();
            standardsTextBox.Clear();
            testedByTextBox.Clear();
            approvedByTextBox.Clear();
            laboratoryTextBox.Clear();
            certificateTextBox.Clear();
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
