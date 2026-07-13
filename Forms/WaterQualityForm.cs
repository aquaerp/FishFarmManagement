using System;
using System.Drawing;
using System.Linq;
using System.Windows.Forms;
using Microsoft.EntityFrameworkCore;
using FishFarmManager.Data;
using FishFarmManager.Models;

namespace FishFarmManager.Forms
{
    public partial class WaterQualityForm : Form
    {
        private readonly FishFarmContext _context;
        private DataGridView _waterQualityGrid = null!;
        private ComboBox _cycleComboBox = null!;
        private DateTimePicker _measurementDatePicker = null!;
        private NumericUpDown _temperatureNumeric = null!;
        private NumericUpDown _dissolvedOxygenNumeric = null!;
        private NumericUpDown _phNumeric = null!;
        private NumericUpDown _ammoniaNumeric = null!;
        private NumericUpDown _nitriteNumeric = null!;
        private NumericUpDown _nitrateNumeric = null!;
        private NumericUpDown _salinityNumeric = null!;
        private NumericUpDown _turbidityNumeric = null!;
        private NumericUpDown _alkalinityNumeric = null!;
        private ComboBox _statusComboBox = null!;
        private TextBox _notesTextBox = null!;
        private TextBox _recordedByTextBox = null!;
        private Label _alertLabel = null!;
        private Button _addButton = null!;
        private Button _updateButton = null!;
        private Button _deleteButton = null!;
        private int _selectedRecordId = -1;

        public WaterQualityForm(FishFarmContext context)
        {
            _context = context;
            InitializeComponent();
            LoadWaterQualityRecords();
            LoadActiveCycles();
        }

        private void InitializeComponent()
        {
            this.SuspendLayout();
            
            this.Text = "مراقبة جودة المياه";
            this.Size = new Size(1400, 800);
            this.StartPosition = FormStartPosition.CenterScreen;
            this.RightToLeft = RightToLeft.Yes;
            this.RightToLeftLayout = true;
            try { this.Font = new Font("Cairo", 10F, FontStyle.Regular); } catch { }

            // إنشاء اللوحة الرئيسية
            var mainPanel = new TableLayoutPanel();
            mainPanel.Dock = DockStyle.Fill;
            mainPanel.ColumnCount = 2;
            mainPanel.RowCount = 1;
            mainPanel.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 65F));
            mainPanel.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 35F));

            // إنشاء جدول البيانات
            CreateDataGrid();
            mainPanel.Controls.Add(_waterQualityGrid, 0, 0);

            // إنشاء لوحة الإدخال
            var inputPanel = CreateInputPanel();
            mainPanel.Controls.Add(inputPanel, 1, 0);

            this.Controls.Add(mainPanel);
            this.ResumeLayout(false);
        }

        private void CreateDataGrid()
        {
            _waterQualityGrid = new DataGridView();
            _waterQualityGrid.Dock = DockStyle.Fill;
            _waterQualityGrid.AutoGenerateColumns = false;
            _waterQualityGrid.SelectionMode = DataGridViewSelectionMode.FullRowSelect;
            _waterQualityGrid.MultiSelect = false;
            _waterQualityGrid.ReadOnly = true;
            _waterQualityGrid.AllowUserToAddRows = false;

            // إضافة الأعمدة
            _waterQualityGrid.Columns.Add(new DataGridViewTextBoxColumn 
            { 
                DataPropertyName = "Id", 
                Name = "Id",
                HeaderText = "الرقم", 
                Width = 50 
            });
            
            _waterQualityGrid.Columns.Add(new DataGridViewTextBoxColumn 
            { 
                DataPropertyName = "CycleName", 
                HeaderText = "الدورة", 
                Width = 100 
            });
            
            _waterQualityGrid.Columns.Add(new DataGridViewTextBoxColumn 
            { 
                DataPropertyName = "PondName", 
                HeaderText = "الحوض", 
                Width = 80 
            });
            
            _waterQualityGrid.Columns.Add(new DataGridViewTextBoxColumn 
            { 
                DataPropertyName = "MeasurementDate", 
                HeaderText = "التاريخ", 
                Width = 100,
                DefaultCellStyle = new DataGridViewCellStyle { Format = "yyyy/MM/dd" }
            });
            
            _waterQualityGrid.Columns.Add(new DataGridViewTextBoxColumn 
            { 
                DataPropertyName = "Temperature", 
                HeaderText = "الحرارة °C", 
                Width = 80,
                DefaultCellStyle = new DataGridViewCellStyle { Format = "F1" }
            });
            
            _waterQualityGrid.Columns.Add(new DataGridViewTextBoxColumn 
            { 
                DataPropertyName = "DissolvedOxygen", 
                HeaderText = "الأوكسجين mg/L", 
                Width = 90,
                DefaultCellStyle = new DataGridViewCellStyle { Format = "F1" }
            });
            
            _waterQualityGrid.Columns.Add(new DataGridViewTextBoxColumn 
            { 
                DataPropertyName = "pH", 
                HeaderText = "pH", 
                Width = 60,
                DefaultCellStyle = new DataGridViewCellStyle { Format = "F1" }
            });
            
            _waterQualityGrid.Columns.Add(new DataGridViewTextBoxColumn 
            { 
                DataPropertyName = "Ammonia", 
                HeaderText = "الأمونيا mg/L", 
                Width = 90,
                DefaultCellStyle = new DataGridViewCellStyle { Format = "F2" }
            });
            
            _waterQualityGrid.Columns.Add(new DataGridViewTextBoxColumn 
            { 
                DataPropertyName = "Status", 
                HeaderText = "الحالة", 
                Width = 80 
            });
            
            _waterQualityGrid.Columns.Add(new DataGridViewTextBoxColumn 
            { 
                DataPropertyName = "RecordedBy", 
                HeaderText = "المسجل", 
                Width = 100 
            });

            _waterQualityGrid.SelectionChanged += WaterQualityGrid_SelectionChanged;
        }

        private Panel CreateInputPanel()
        {
            var panel = new Panel();
            panel.Dock = DockStyle.Fill;
            panel.Padding = new Padding(10);
            panel.AutoScroll = true;

            var y = 10;
            var labelWidth = 120;
            var controlWidth = 120;
            var spacing = 30;

            // تنبيه
            _alertLabel = new Label 
            { 
                Text = "", 
                Location = new Point(10, y), 
                Size = new Size(300, 40),
                BackColor = Color.Yellow,
                ForeColor = Color.Red,
                Font = new Font("Arial", 8, FontStyle.Bold),
                Visible = false,
                BorderStyle = BorderStyle.FixedSingle
            };
            panel.Controls.Add(_alertLabel);
            y += 50;

            // الدورة الإنتاجية
            var cycleLabel = new Label { Text = "الدورة الإنتاجية:", Location = new Point(10, y), Size = new Size(labelWidth, 20) };
            _cycleComboBox = new ComboBox 
            { 
                Location = new Point(140, y), 
                Size = new Size(180, 20),
                DropDownStyle = ComboBoxStyle.DropDownList
            };
            panel.Controls.Add(cycleLabel);
            panel.Controls.Add(_cycleComboBox);
            y += spacing;

            // تاريخ القياس
            var dateLabel = new Label { Text = "تاريخ القياس:", Location = new Point(10, y), Size = new Size(labelWidth, 20) };
            _measurementDatePicker = new DateTimePicker { Location = new Point(140, y), Size = new Size(180, 20) };
            panel.Controls.Add(dateLabel);
            panel.Controls.Add(_measurementDatePicker);
            y += spacing;

            // درجة الحرارة
            var tempLabel = new Label { Text = "درجة الحرارة °C:", Location = new Point(10, y), Size = new Size(labelWidth, 20) };
            _temperatureNumeric = new NumericUpDown 
            { 
                Location = new Point(140, y), 
                Size = new Size(controlWidth, 20),
                DecimalPlaces = 1,
                Maximum = 50,
                Minimum = 0
            };
            _temperatureNumeric.ValueChanged += CheckWaterQualityAlerts;
            panel.Controls.Add(tempLabel);
            panel.Controls.Add(_temperatureNumeric);
            y += spacing;

            // الأوكسجين المذاب
            var oxygenLabel = new Label { Text = "الأوكسجين mg/L:", Location = new Point(10, y), Size = new Size(labelWidth, 20) };
            _dissolvedOxygenNumeric = new NumericUpDown 
            { 
                Location = new Point(140, y), 
                Size = new Size(controlWidth, 20),
                DecimalPlaces = 1,
                Maximum = 20,
                Minimum = 0
            };
            _dissolvedOxygenNumeric.ValueChanged += CheckWaterQualityAlerts;
            panel.Controls.Add(oxygenLabel);
            panel.Controls.Add(_dissolvedOxygenNumeric);
            y += spacing;

            // درجة الحموضة
            var phLabel = new Label { Text = "درجة الحموضة pH:", Location = new Point(10, y), Size = new Size(labelWidth, 20) };
            _phNumeric = new NumericUpDown 
            { 
                Location = new Point(140, y), 
                Size = new Size(controlWidth, 20),
                DecimalPlaces = 1,
                Maximum = 14,
                Minimum = 0
            };
            _phNumeric.ValueChanged += CheckWaterQualityAlerts;
            panel.Controls.Add(phLabel);
            panel.Controls.Add(_phNumeric);
            y += spacing;

            // الأمونيا
            var ammoniaLabel = new Label { Text = "الأمونيا mg/L:", Location = new Point(10, y), Size = new Size(labelWidth, 20) };
            _ammoniaNumeric = new NumericUpDown 
            { 
                Location = new Point(140, y), 
                Size = new Size(controlWidth, 20),
                DecimalPlaces = 2,
                Maximum = 10,
                Minimum = 0
            };
            _ammoniaNumeric.ValueChanged += CheckWaterQualityAlerts;
            panel.Controls.Add(ammoniaLabel);
            panel.Controls.Add(_ammoniaNumeric);
            y += spacing;

            // النتريت
            var nitriteLabel = new Label { Text = "النتريت mg/L:", Location = new Point(10, y), Size = new Size(labelWidth, 20) };
            _nitriteNumeric = new NumericUpDown 
            { 
                Location = new Point(140, y), 
                Size = new Size(controlWidth, 20),
                DecimalPlaces = 2,
                Maximum = 10,
                Minimum = 0
            };
            panel.Controls.Add(nitriteLabel);
            panel.Controls.Add(_nitriteNumeric);
            y += spacing;

            // النترات
            var nitrateLabel = new Label { Text = "النترات mg/L:", Location = new Point(10, y), Size = new Size(labelWidth, 20) };
            _nitrateNumeric = new NumericUpDown 
            { 
                Location = new Point(140, y), 
                Size = new Size(controlWidth, 20),
                DecimalPlaces = 2,
                Maximum = 100,
                Minimum = 0
            };
            panel.Controls.Add(nitrateLabel);
            panel.Controls.Add(_nitrateNumeric);
            y += spacing;

            // الملوحة
            var salinityLabel = new Label { Text = "الملوحة ppt:", Location = new Point(10, y), Size = new Size(labelWidth, 20) };
            _salinityNumeric = new NumericUpDown 
            { 
                Location = new Point(140, y), 
                Size = new Size(controlWidth, 20),
                DecimalPlaces = 1,
                Maximum = 50,
                Minimum = 0
            };
            panel.Controls.Add(salinityLabel);
            panel.Controls.Add(_salinityNumeric);
            y += spacing;

            // العكارة
            var turbidityLabel = new Label { Text = "العكارة NTU:", Location = new Point(10, y), Size = new Size(labelWidth, 20) };
            _turbidityNumeric = new NumericUpDown 
            { 
                Location = new Point(140, y), 
                Size = new Size(controlWidth, 20),
                DecimalPlaces = 1,
                Maximum = 100,
                Minimum = 0
            };
            panel.Controls.Add(turbidityLabel);
            panel.Controls.Add(_turbidityNumeric);
            y += spacing;

            // القلوية
            var alkalinityLabel = new Label { Text = "القلوية mg/L:", Location = new Point(10, y), Size = new Size(labelWidth, 20) };
            _alkalinityNumeric = new NumericUpDown 
            { 
                Location = new Point(140, y), 
                Size = new Size(controlWidth, 20),
                DecimalPlaces = 1,
                Maximum = 500,
                Minimum = 0
            };
            panel.Controls.Add(alkalinityLabel);
            panel.Controls.Add(_alkalinityNumeric);
            y += spacing;

            // الحالة
            var statusLabel = new Label { Text = "الحالة:", Location = new Point(10, y), Size = new Size(labelWidth, 20) };
            _statusComboBox = new ComboBox 
            { 
                Location = new Point(140, y), 
                Size = new Size(180, 20),
                DropDownStyle = ComboBoxStyle.DropDownList
            };
            _statusComboBox.Items.AddRange(new string[] { "ممتاز", "جيد", "تحذير", "حرج" });
            panel.Controls.Add(statusLabel);
            panel.Controls.Add(_statusComboBox);
            y += spacing;

            // الملاحظات
            var notesLabel = new Label { Text = "ملاحظات:", Location = new Point(10, y), Size = new Size(labelWidth, 20) };
            _notesTextBox = new TextBox 
            { 
                Location = new Point(140, y), 
                Size = new Size(180, 60),
                Multiline = true
            };
            panel.Controls.Add(notesLabel);
            panel.Controls.Add(_notesTextBox);
            y += 70;

            // المسجل
            var recordedByLabel = new Label { Text = "المسجل:", Location = new Point(10, y), Size = new Size(labelWidth, 20) };
            _recordedByTextBox = new TextBox 
            { 
                Location = new Point(140, y), 
                Size = new Size(180, 20),
                Text = Environment.UserName
            };
            panel.Controls.Add(recordedByLabel);
            panel.Controls.Add(_recordedByTextBox);
            y += spacing;

            // الأزرار
            var buttonWidth = 80;
            var buttonSpacing = 90;
            
            _addButton = new Button 
            { 
                Text = "إضافة", 
                Location = new Point(10, y), 
                Size = new Size(buttonWidth, 30),
                BackColor = Color.LightGreen
            };
            _addButton.Click += AddButton_Click;
            
            _updateButton = new Button 
            { 
                Text = "تحديث", 
                Location = new Point(10 + buttonSpacing, y), 
                Size = new Size(buttonWidth, 30),
                BackColor = Color.LightBlue,
                Enabled = false
            };
            _updateButton.Click += UpdateButton_Click;
            
            _deleteButton = new Button 
            { 
                Text = "حذف", 
                Location = new Point(10 + buttonSpacing * 2, y), 
                Size = new Size(buttonWidth, 30),
                BackColor = Color.LightCoral,
                Enabled = false
            };
            _deleteButton.Click += DeleteButton_Click;

            panel.Controls.Add(_addButton);
            panel.Controls.Add(_updateButton);
            panel.Controls.Add(_deleteButton);

            // زر مسح الحقول
            var clearButton = new Button 
            { 
                Text = "مسح", 
                Location = new Point(10, y + 40), 
                Size = new Size(buttonWidth, 30)
            };
            clearButton.Click += ClearButton_Click;
            panel.Controls.Add(clearButton);

            return panel;
        }

        private void ClearButton_Click(object? sender, EventArgs e)
        {
            ClearInputs();
        }

        private void LoadWaterQualityRecords()
        {
#pragma warning disable CS8602
            var records = _context.WaterQualityRecords
                .Include(w => w.Cycle)
                    .ThenInclude(c => c.ProductionCyclePonds)
                        .ThenInclude(pcp => pcp.Pond)
                .Select(w => new
                {
                    w.Id,
                    CycleName = w.Cycle != null ? w.Cycle.Name : string.Empty,
                    PondName = w.Cycle != null && w.Cycle.ProductionCyclePonds != null 
                        ? string.Join(", ", w.Cycle.ProductionCyclePonds.Select(pcp => pcp.Pond.Name)) 
                        : string.Empty,
                    w.MeasurementDate,
                    w.Temperature,
                    w.DissolvedOxygen,
                    w.pH,
                    w.Ammonia,
                    w.Status,
                    w.RecordedBy
                })
                .OrderByDescending(w => w.MeasurementDate)
                .ToList();
#pragma warning restore CS8602
            
            _waterQualityGrid.DataSource = records;
        }

        private void LoadActiveCycles()
        {
            var cycles = _context.ProductionCycles
                .Include(c => c.ProductionCyclePonds)
                .ThenInclude(pcp => pcp.Pond)
                .Where(c => c.Status == CycleStatus.Active || c.Status == CycleStatus.Planning)
                .Select(c => new
                {
                    c.Id,
                    DisplayName = $"{c.Name} - {string.Join(", ", c.ProductionCyclePonds.Select(pcp => pcp.Pond.Name))}"
                })
                .ToList();
            
            _cycleComboBox.DataSource = cycles;
            _cycleComboBox.DisplayMember = "DisplayName";
            _cycleComboBox.ValueMember = "Id";
            _cycleComboBox.SelectedIndex = -1;
        }

        private void WaterQualityGrid_SelectionChanged(object? sender, EventArgs e)
        {
            if (_waterQualityGrid.SelectedRows.Count > 0)
            {
                var selectedRow = _waterQualityGrid.SelectedRows[0];
                var recordId = (int)selectedRow.Cells["Id"].Value;
                
                LoadWaterQualityData(recordId);
                _selectedRecordId = recordId;
                _updateButton.Enabled = true;
                _deleteButton.Enabled = true;
            }
            else
            {
                _selectedRecordId = -1;
                _updateButton.Enabled = false;
                _deleteButton.Enabled = false;
            }
        }

        private void LoadWaterQualityData(int recordId)
        {
            var record = _context.WaterQualityRecords
                .Include(w => w.Cycle)
                .FirstOrDefault(w => w.Id == recordId);
            
            if (record != null)
            {
                _cycleComboBox.SelectedValue = record.CycleId;
                _measurementDatePicker.Value = record.MeasurementDate ?? DateTime.Now;
                _temperatureNumeric.Value = record.Temperature;
                _dissolvedOxygenNumeric.Value = record.DissolvedOxygen;
                _phNumeric.Value = record.pH;
                _ammoniaNumeric.Value = record.Ammonia;
                _nitriteNumeric.Value = record.Nitrite;
                _nitrateNumeric.Value = record.Nitrate;
                _salinityNumeric.Value = record.Salinity ?? 0m;
                _turbidityNumeric.Value = record.Turbidity ?? 0m;
                _alkalinityNumeric.Value = record.Alkalinity ?? 0m;
                _statusComboBox.Text = GetStatusText(record.Status);
                _notesTextBox.Text = record.Notes ?? "";
                _recordedByTextBox.Text = record.RecordedBy?.ToString() ?? "";
                
                CheckWaterQualityAlerts(this, EventArgs.Empty);
            }
        }

        private void CheckWaterQualityAlerts(object? sender, EventArgs e)
        {
            var alerts = new List<string>();

            // فحص الأوكسجين المذاب
            if (_dissolvedOxygenNumeric.Value < 4.0m)
            {
                alerts.Add("تحذير: مستوى الأوكسجين المذاب منخفض");
            }

            // فحص الأمونيا
            if (_ammoniaNumeric.Value > 0.5m)
            {
                alerts.Add("تحذير: مستوى الأمونيا مرتفع");
            }

            // فحص درجة الحموضة
            if (_phNumeric.Value < 6.5m || _phNumeric.Value > 8.5m)
            {
                alerts.Add("تحذير: درجة الحموضة خارج النطاق المثالي");
            }

            // فحص درجة الحرارة
            if (_temperatureNumeric.Value < 20m || _temperatureNumeric.Value > 30m)
            {
                alerts.Add("تحذير: درجة الحرارة خارج النطاق المثالي");
            }

            if (alerts.Count > 0)
            {
                _alertLabel.Text = string.Join("\n", alerts);
                _alertLabel.Visible = true;
                
                // تحديد الحالة تلقائياً
                if (alerts.Count >= 3)
                    _statusComboBox.Text = "حرج";
                else if (alerts.Count >= 2)
                    _statusComboBox.Text = "تحذير";
                else
                    _statusComboBox.Text = "جيد";
            }
            else
            {
                _alertLabel.Visible = false;
                if (_statusComboBox.SelectedIndex == -1)
                    _statusComboBox.Text = "ممتاز";
            }
        }

        private string GetStatusText(WaterQualityStatus status)
        {
            return status switch
            {
                WaterQualityStatus.Excellent => "ممتاز",
                WaterQualityStatus.Good => "جيد",
                WaterQualityStatus.Warning => "تحذير",
                WaterQualityStatus.Critical => "حرج",
                _ => "جيد"
            };
        }

        private WaterQualityStatus GetStatusFromText(string text)
        {
            return text switch
            {
                "ممتاز" => WaterQualityStatus.Excellent,
                "جيد" => WaterQualityStatus.Good,
                "تحذير" => WaterQualityStatus.Warning,
                "حرج" => WaterQualityStatus.Critical,
                _ => WaterQualityStatus.Good
            };
        }

        private void AddButton_Click(object? sender, EventArgs e)
        {
            if (ValidateInput())
            {
                var record = new WaterQualityRecord
                {
                    CycleId = _cycleComboBox.SelectedValue is int cycleId ? cycleId : 0,
                    MeasurementDate = _measurementDatePicker.Value.Date,
                    Temperature = _temperatureNumeric.Value > 0 ? _temperatureNumeric.Value : 0m,
                    DissolvedOxygen = _dissolvedOxygenNumeric.Value > 0 ? _dissolvedOxygenNumeric.Value : 0m,
                    pH = _phNumeric.Value > 0 ? _phNumeric.Value : 0m,
                    Ammonia = _ammoniaNumeric.Value > 0 ? _ammoniaNumeric.Value : 0m,
                    Nitrite = _nitriteNumeric.Value > 0 ? _nitriteNumeric.Value : 0m,
                    Nitrate = _nitrateNumeric.Value > 0 ? _nitrateNumeric.Value : 0m,
                    Salinity = _salinityNumeric.Value > 0 ? _salinityNumeric.Value : (decimal?)null,
                    Turbidity = _turbidityNumeric.Value > 0 ? _turbidityNumeric.Value : (decimal?)null,
                    Alkalinity = _alkalinityNumeric.Value > 0 ? _alkalinityNumeric.Value : (decimal?)null,
                    Status = GetStatusFromText(_statusComboBox.Text),
                    Notes = _notesTextBox.Text.Trim(),
                    RecordedBy = int.TryParse(_recordedByTextBox.Text.Trim(), out int recordedBy) ? recordedBy : (int?)null,
                    CreatedAt = DateTime.Now
                };

                _context.WaterQualityRecords.Add(record);
                _context.SaveChanges();
                
                LoadWaterQualityRecords();
                ClearInputs();
                MessageBox.Show("تم تسجيل قياس جودة المياه بنجاح", "نجح", MessageBoxButtons.OK, MessageBoxIcon.Information);
            }
        }

        private void UpdateButton_Click(object? sender, EventArgs e)
        {
            if (_selectedRecordId > 0 && ValidateInput())
            {
                var record = _context.WaterQualityRecords.Find(_selectedRecordId);
                if (record != null)
                {
                    record.CycleId = _cycleComboBox.SelectedValue is int cycleId ? cycleId : 0;
                    record.MeasurementDate = _measurementDatePicker.Value.Date;
                    record.Temperature = _temperatureNumeric.Value > 0 ? _temperatureNumeric.Value : 0m;
                    record.DissolvedOxygen = _dissolvedOxygenNumeric.Value > 0 ? _dissolvedOxygenNumeric.Value : 0m;
                    record.pH = _phNumeric.Value > 0 ? _phNumeric.Value : 0m;
                    record.Ammonia = _ammoniaNumeric.Value > 0 ? _ammoniaNumeric.Value : 0m;
                    record.Nitrite = _nitriteNumeric.Value > 0 ? _nitriteNumeric.Value : 0m;
                    record.Nitrate = _nitrateNumeric.Value > 0 ? _nitrateNumeric.Value : 0m;
                    record.Salinity = _salinityNumeric.Value > 0 ? _salinityNumeric.Value : (decimal?)null;
                    record.Turbidity = _turbidityNumeric.Value > 0 ? _turbidityNumeric.Value : (decimal?)null;
                    record.Alkalinity = _alkalinityNumeric.Value > 0 ? _alkalinityNumeric.Value : (decimal?)null;
                    record.Status = GetStatusFromText(_statusComboBox.Text);
                    record.Notes = _notesTextBox.Text.Trim();
                    record.RecordedBy = int.TryParse(_recordedByTextBox.Text.Trim(), out int recordedBy) ? recordedBy : (int?)null;

                    _context.SaveChanges();
                    
                    LoadWaterQualityRecords();
                    ClearInputs();
                    MessageBox.Show("تم تحديث قياس جودة المياه بنجاح", "نجح", MessageBoxButtons.OK, MessageBoxIcon.Information);
                }
            }
        }

        private void DeleteButton_Click(object? sender, EventArgs e)
        {
            if (_selectedRecordId > 0)
            {
                var result = MessageBox.Show("هل أنت متأكد من حذف هذا القياس؟", "تأكيد الحذف", 
                                           MessageBoxButtons.YesNo, MessageBoxIcon.Question);
                
                if (result == DialogResult.Yes)
                {
                    var record = _context.WaterQualityRecords.Find(_selectedRecordId);
                    if (record != null)
                    {
                        _context.WaterQualityRecords.Remove(record);
                        _context.SaveChanges();
                        
                        LoadWaterQualityRecords();
                        ClearInputs();
                        MessageBox.Show("تم حذف القياس بنجاح", "نجح", MessageBoxButtons.OK, MessageBoxIcon.Information);
                    }
                }
            }
        }

        private void ClearInputs()
        {
            _cycleComboBox.SelectedIndex = -1;
            _measurementDatePicker.Value = DateTime.Now;
            _temperatureNumeric.Value = 0;
            _dissolvedOxygenNumeric.Value = 0;
            _phNumeric.Value = 0;
            _ammoniaNumeric.Value = 0;
            _nitriteNumeric.Value = 0;
            _nitrateNumeric.Value = 0;
            _salinityNumeric.Value = 0;
            _turbidityNumeric.Value = 0;
            _alkalinityNumeric.Value = 0;
            _statusComboBox.SelectedIndex = -1;
            _notesTextBox.Clear();
            _recordedByTextBox.Text = Environment.UserName;
            _alertLabel.Visible = false;
            _selectedRecordId = -1;
            _updateButton.Enabled = false;
            _deleteButton.Enabled = false;
        }

        private bool ValidateInput()
        {
            if (_cycleComboBox.SelectedIndex == -1)
            {
                MessageBox.Show("يرجى اختيار الدورة الإنتاجية", "خطأ", MessageBoxButtons.OK, MessageBoxIcon.Error);
                _cycleComboBox.Focus();
                return false;
            }

            if (_statusComboBox.SelectedIndex == -1)
            {
                MessageBox.Show("يرجى اختيار حالة جودة المياه", "خطأ", MessageBoxButtons.OK, MessageBoxIcon.Error);
                _statusComboBox.Focus();
                return false;
            }

            if (string.IsNullOrWhiteSpace(_recordedByTextBox.Text))
            {
                MessageBox.Show("يرجى إدخال اسم المسجل", "خطأ", MessageBoxButtons.OK, MessageBoxIcon.Error);
                _recordedByTextBox.Focus();
                return false;
            }

            // التأكد من وجود قياس واحد على الأقل
            if (_temperatureNumeric.Value == 0 && _dissolvedOxygenNumeric.Value == 0 && 
                _phNumeric.Value == 0 && _ammoniaNumeric.Value == 0)
            {
                MessageBox.Show("يرجى إدخال قياس واحد على الأقل", "خطأ", MessageBoxButtons.OK, MessageBoxIcon.Error);
                return false;
            }

            return true;
        }
    }
}
