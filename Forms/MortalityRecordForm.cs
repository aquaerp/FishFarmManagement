using System;
using System.Drawing;
using System.Linq;
using System.Windows.Forms;
using Microsoft.EntityFrameworkCore;
using FishFarmManager.Data;
using FishFarmManager.Models;

namespace FishFarmManager.Forms
{
    public partial class MortalityRecordForm : Form
    {
        private readonly FishFarmContext _context;
        private DataGridView _mortalityGrid = null!;
        private ComboBox _cycleComboBox = null!;
        // private ComboBox _pondComboBox = null!; // Unused - removed
        private DateTimePicker _datePicker = null!;
        private NumericUpDown _deadFishCountNumeric = null!;
        private NumericUpDown _averageWeightNumeric = null!;
        private ComboBox _causeComboBox = null!;
        private TextBox _causeDescriptionTextBox = null!;
        private TextBox _actionTakenTextBox = null!;
        private TextBox _treatmentTextBox = null!;
        private TextBox _notesTextBox = null!;
        private TextBox _recordedByTextBox = null!;
        private Label _mortalityRateLabel = null!;
        private Button _addButton = null!;
        private Button _updateButton = null!;
        private Button _deleteButton = null!;
        private int _selectedRecordId = -1;

        public MortalityRecordForm(FishFarmContext context)
        {
            _context = context;
            InitializeComponent();
            LoadMortalityRecords();
            LoadActiveCycles();
        }

        private void InitializeComponent()
        {
            this.SuspendLayout();
            
            this.Text = "تسجيل النفوق";
            this.Size = new Size(1200, 700);
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
            mainPanel.Controls.Add(_mortalityGrid, 0, 0);

            // إنشاء لوحة الإدخال
            var inputPanel = CreateInputPanel();
            mainPanel.Controls.Add(inputPanel, 1, 0);

            this.Controls.Add(mainPanel);
            this.ResumeLayout(false);
        }

        private void CreateDataGrid()
        {
            _mortalityGrid = new DataGridView();
            _mortalityGrid.Dock = DockStyle.Fill;
            _mortalityGrid.AutoGenerateColumns = false;
            _mortalityGrid.SelectionMode = DataGridViewSelectionMode.FullRowSelect;
            _mortalityGrid.MultiSelect = false;
            _mortalityGrid.ReadOnly = true;
            _mortalityGrid.AllowUserToAddRows = false;

            // إضافة الأعمدة
            _mortalityGrid.Columns.Add(new DataGridViewTextBoxColumn 
            { 
                DataPropertyName = "Id", 
                Name = "Id",
                HeaderText = "الرقم", 
                Width = 50 
            });
            
            _mortalityGrid.Columns.Add(new DataGridViewTextBoxColumn 
            { 
                DataPropertyName = "CycleName", 
                HeaderText = "الدورة", 
                Width = 120 
            });
            
            _mortalityGrid.Columns.Add(new DataGridViewTextBoxColumn 
            { 
                DataPropertyName = "PondName", 
                HeaderText = "الحوض", 
                Width = 100 
            });
            
            _mortalityGrid.Columns.Add(new DataGridViewTextBoxColumn 
            { 
                DataPropertyName = "Date", 
                HeaderText = "التاريخ", 
                Width = 100,
                DefaultCellStyle = new DataGridViewCellStyle { Format = "yyyy/MM/dd" }
            });
            
            _mortalityGrid.Columns.Add(new DataGridViewTextBoxColumn 
            { 
                DataPropertyName = "DeadFishCount", 
                HeaderText = "عدد النافق", 
                Width = 80 
            });
            
            _mortalityGrid.Columns.Add(new DataGridViewTextBoxColumn 
            { 
                DataPropertyName = "AverageWeight", 
                HeaderText = "متوسط الوزن", 
                Width = 90,
                DefaultCellStyle = new DataGridViewCellStyle { Format = "F2" }
            });
            
            _mortalityGrid.Columns.Add(new DataGridViewTextBoxColumn 
            { 
                DataPropertyName = "Cause", 
                HeaderText = "السبب", 
                Width = 100 
            });
            
            _mortalityGrid.Columns.Add(new DataGridViewTextBoxColumn 
            { 
                DataPropertyName = "MortalityRate", 
                HeaderText = "معدل النفوق %", 
                Width = 100,
                DefaultCellStyle = new DataGridViewCellStyle { Format = "F2" }
            });
            
            _mortalityGrid.Columns.Add(new DataGridViewTextBoxColumn 
            { 
                DataPropertyName = "RecordedBy", 
                HeaderText = "المسجل", 
                Width = 100 
            });

            _mortalityGrid.SelectionChanged += MortalityGrid_SelectionChanged;
        }

        private Panel CreateInputPanel()
        {
            var panel = new Panel();
            panel.Dock = DockStyle.Fill;
            panel.Padding = new Padding(10);
            panel.AutoScroll = true;

            var y = 10;
            var labelWidth = 120;
            var controlWidth = 180;
            var spacing = 35;

            // الدورة الإنتاجية
            var cycleLabel = new Label { Text = "الدورة الإنتاجية:", Location = new Point(10, y), Size = new Size(labelWidth, 20) };
            _cycleComboBox = new ComboBox 
            { 
                Location = new Point(140, y), 
                Size = new Size(controlWidth, 20),
                DropDownStyle = ComboBoxStyle.DropDownList
            };
            _cycleComboBox.SelectedIndexChanged += CalculateMortalityRate;
            panel.Controls.Add(cycleLabel);
            panel.Controls.Add(_cycleComboBox);
            y += spacing;

            // التاريخ
            var dateLabel = new Label { Text = "تاريخ النفوق:", Location = new Point(10, y), Size = new Size(labelWidth, 20) };
            _datePicker = new DateTimePicker { Location = new Point(140, y), Size = new Size(controlWidth, 20) };
            panel.Controls.Add(dateLabel);
            panel.Controls.Add(_datePicker);
            y += spacing;

            // عدد الأسماك النافقة
            var countLabel = new Label { Text = "عدد الأسماك النافقة:", Location = new Point(10, y), Size = new Size(labelWidth, 20) };
            _deadFishCountNumeric = new NumericUpDown 
            { 
                Location = new Point(140, y), 
                Size = new Size(controlWidth, 20),
                Maximum = 100000,
                Minimum = 1
            };
            _deadFishCountNumeric.ValueChanged += CalculateMortalityRate;
            panel.Controls.Add(countLabel);
            panel.Controls.Add(_deadFishCountNumeric);
            y += spacing;

            // متوسط الوزن
            var weightLabel = new Label { Text = "متوسط الوزن (جم):", Location = new Point(10, y), Size = new Size(labelWidth, 20) };
            _averageWeightNumeric = new NumericUpDown 
            { 
                Location = new Point(140, y), 
                Size = new Size(controlWidth, 20),
                DecimalPlaces = 2,
                Maximum = 5000,
                Minimum = 0
            };
            panel.Controls.Add(weightLabel);
            panel.Controls.Add(_averageWeightNumeric);
            y += spacing;

            // سبب النفوق
            var causeLabel = new Label { Text = "سبب النفوق:", Location = new Point(10, y), Size = new Size(labelWidth, 20) };
            _causeComboBox = new ComboBox 
            { 
                Location = new Point(140, y), 
                Size = new Size(controlWidth, 20),
                DropDownStyle = ComboBoxStyle.DropDownList
            };
            _causeComboBox.Items.AddRange(new string[] { "مرض", "جودة المياه", "إجهاد", "افتراس", "غير معروف", "سوء التعامل", "درجة الحرارة" });
            panel.Controls.Add(causeLabel);
            panel.Controls.Add(_causeComboBox);
            y += spacing;

            // وصف السبب
            var descLabel = new Label { Text = "وصف السبب:", Location = new Point(10, y), Size = new Size(labelWidth, 20) };
            _causeDescriptionTextBox = new TextBox 
            { 
                Location = new Point(140, y), 
                Size = new Size(controlWidth, 60),
                Multiline = true
            };
            panel.Controls.Add(descLabel);
            panel.Controls.Add(_causeDescriptionTextBox);
            y += 70;

            // الإجراءات المتخذة
            var actionLabel = new Label { Text = "الإجراءات المتخذة:", Location = new Point(10, y), Size = new Size(labelWidth, 20) };
            _actionTakenTextBox = new TextBox 
            { 
                Location = new Point(140, y), 
                Size = new Size(controlWidth, 60),
                Multiline = true
            };
            panel.Controls.Add(actionLabel);
            panel.Controls.Add(_actionTakenTextBox);
            y += 70;

            // العلاج
            var treatmentLabel = new Label { Text = "العلاج المطبق:", Location = new Point(10, y), Size = new Size(labelWidth, 20) };
            _treatmentTextBox = new TextBox 
            { 
                Location = new Point(140, y), 
                Size = new Size(controlWidth, 60),
                Multiline = true
            };
            panel.Controls.Add(treatmentLabel);
            panel.Controls.Add(_treatmentTextBox);
            y += 70;

            // معدل النفوق
            var rateLabel = new Label { Text = "معدل النفوق:", Location = new Point(10, y), Size = new Size(labelWidth, 20) };
            _mortalityRateLabel = new Label 
            { 
                Text = "0.00%", 
                Location = new Point(140, y), 
                Size = new Size(controlWidth, 20),
                BackColor = Color.LightYellow,
                BorderStyle = BorderStyle.FixedSingle,
                TextAlign = ContentAlignment.MiddleCenter,
                Font = new Font("Arial", 9, FontStyle.Bold)
            };
            panel.Controls.Add(rateLabel);
            panel.Controls.Add(_mortalityRateLabel);
            y += spacing;

            // الملاحظات
            var notesLabel = new Label { Text = "ملاحظات:", Location = new Point(10, y), Size = new Size(labelWidth, 20) };
            _notesTextBox = new TextBox 
            { 
                Location = new Point(140, y), 
                Size = new Size(controlWidth, 60),
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
                Size = new Size(controlWidth, 20),
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

        private void LoadMortalityRecords()
        {
            var records = _context.MortalityRecords
                .Include(m => m.Cycle)
                    .ThenInclude(c => c.ProductionCyclePonds)
                        .ThenInclude(pcp => pcp.Pond)
                .OrderByDescending(m => m.Date)
                .ToList()
                .Select(m => new
                {
                    m.Id,
                    CycleName = m.Cycle.Name,
                    PondName = string.Join(", ", m.Cycle.ProductionCyclePonds.Select(pcp => pcp.Pond.Name)),
                    m.Date,
                    m.DeadFishCount,
                    m.AverageWeight,
                    m.Cause,
                    MortalityRate = CalculateRecordMortalityRate(m.CycleId, m.DeadFishCount),
                    m.RecordedBy
                })
                .ToList();

            _mortalityGrid.DataSource = records;
        }

        private double CalculateRecordMortalityRate(int cycleId, int deadCount)
        {
            var cycle = _context.ProductionCycles.Find(cycleId);
            if (cycle != null && cycle.InitialFishCount > 0)
            {
                return (double)deadCount / cycle.InitialFishCount * 100;
            }
            return 0;
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
                    DisplayName = $"{c.Name} - {string.Join(", ", c.ProductionCyclePonds.Select(pcp => pcp.Pond.Name))}",
                    c.InitialFishCount
                })
                .ToList();
            
            _cycleComboBox.DataSource = cycles;
            _cycleComboBox.DisplayMember = "DisplayName";
            _cycleComboBox.ValueMember = "Id";
            _cycleComboBox.SelectedIndex = -1;
        }

        private void MortalityGrid_SelectionChanged(object? sender, EventArgs e)
        {
            if (_mortalityGrid.SelectedRows.Count > 0)
            {
                var selectedRow = _mortalityGrid.SelectedRows[0];
                var recordId = (int)selectedRow.Cells["Id"].Value;
                
                LoadMortalityData(recordId);
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

        private void LoadMortalityData(int recordId)
        {
            var record = _context.MortalityRecords
                .Include(m => m.Cycle)
                .FirstOrDefault(m => m.Id == recordId);
            
            if (record != null)
            {
                _cycleComboBox.SelectedValue = record.CycleId;
                _datePicker.Value = record.Date;
                _deadFishCountNumeric.Value = record.DeadFishCount;
                _averageWeightNumeric.Value = (decimal)(record.AverageWeight ?? 0);
                _causeComboBox.Text = GetCauseText(record.Cause);
                _causeDescriptionTextBox.Text = record.CauseDescription ?? "";
                _actionTakenTextBox.Text = record.ActionTaken ?? "";
                _treatmentTextBox.Text = record.Treatment ?? "";
                _notesTextBox.Text = record.Notes ?? "";
                _recordedByTextBox.Text = record.RecordedBy ?? "";
                
                CalculateMortalityRate(null, EventArgs.Empty);
            }
        }

        private void CalculateMortalityRate(object? sender, EventArgs e)
        {
            if (_cycleComboBox.SelectedIndex >= 0 && _deadFishCountNumeric.Value > 0)
            {
                int cycleId;
                if (_cycleComboBox.SelectedValue is int)
                {
                    cycleId = (int)_cycleComboBox.SelectedValue;
                }
                else if (_cycleComboBox.SelectedValue != null)
                {
                    // Try to get Id property via reflection (for anonymous types)
                    var idProp = _cycleComboBox.SelectedValue.GetType().GetProperty("Id");
                    if (idProp != null)
                        cycleId = idProp.GetValue(_cycleComboBox.SelectedValue) is int id ? id : -1;
                    else
                        cycleId = -1;
                }
                else
                {
                    cycleId = -1;
                }

                if (cycleId > 0)
                {
                    var cycle = _context.ProductionCycles.Find(cycleId);
                    if (cycle != null && cycle.InitialFishCount > 0)
                    {
                        var mortalityRate = (double)_deadFishCountNumeric.Value / cycle.InitialFishCount * 100;
                        _mortalityRateLabel.Text = $"{mortalityRate:F2}%";
                        // تغيير لون التحذير
                        if (mortalityRate > 10)
                            _mortalityRateLabel.BackColor = Color.Red;
                        else if (mortalityRate > 5)
                            _mortalityRateLabel.BackColor = Color.Orange;
                        else
                            _mortalityRateLabel.BackColor = Color.LightYellow;
                    }
                    else
                    {
                        _mortalityRateLabel.Text = "0.00%";
                        _mortalityRateLabel.BackColor = Color.LightYellow;
                    }
                }
                else
                {
                    _mortalityRateLabel.Text = "0.00%";
                    _mortalityRateLabel.BackColor = Color.LightYellow;
                }
            }
            else
            {
                _mortalityRateLabel.Text = "0.00%";
                _mortalityRateLabel.BackColor = Color.LightYellow;
            }
        }

        private string GetCauseText(MortalityCause cause)
        {
            return cause switch
            {
                MortalityCause.Disease => "مرض",
                MortalityCause.WaterQuality => "جودة المياه",
                MortalityCause.Stress => "إجهاد",
                MortalityCause.Predation => "افتراس",
                MortalityCause.Unknown => "غير معروف",
                MortalityCause.Handling => "سوء التعامل",
                MortalityCause.Temperature => "درجة الحرارة",
                _ => "غير معروف"
            };
        }

        private MortalityCause GetCauseFromText(string text)
        {
            return text switch
            {
                "مرض" => MortalityCause.Disease,
                "جودة المياه" => MortalityCause.WaterQuality,
                "إجهاد" => MortalityCause.Stress,
                "افتراس" => MortalityCause.Predation,
                "غير معروف" => MortalityCause.Unknown,
                "سوء التعامل" => MortalityCause.Handling,
                "درجة الحرارة" => MortalityCause.Temperature,
                _ => MortalityCause.Unknown
            };
        }

        private void AddButton_Click(object? sender, EventArgs e)
        {
            if (ValidateInput())
            {
                var record = new MortalityRecord
                {
                    CycleId = _cycleComboBox.SelectedValue is int cycleId ? cycleId : 0,
                    Date = _datePicker.Value.Date,
                    DeadFishCount = (int)_deadFishCountNumeric.Value,
                    AverageWeight = _averageWeightNumeric.Value > 0 ? (double)_averageWeightNumeric.Value : null,
                    Cause = GetCauseFromText(_causeComboBox.Text),
                    CauseDescription = _causeDescriptionTextBox.Text.Trim(),
                    ActionTaken = _actionTakenTextBox.Text.Trim(),
                    Treatment = _treatmentTextBox.Text.Trim(),
                    Notes = _notesTextBox.Text.Trim(),
                    PhotoPath = string.Empty,
                    RecordedBy = _recordedByTextBox.Text.Trim(),
                    CreatedAt = DateTime.Now
                };

                _context.MortalityRecords.Add(record);
                _context.SaveChanges();
                
                LoadMortalityRecords();
                ClearInputs();
                MessageBox.Show("تم تسجيل النفوق بنجاح", "نجح", MessageBoxButtons.OK, MessageBoxIcon.Information);
            }
        }

        private void UpdateButton_Click(object? sender, EventArgs e)
        {
            if (_selectedRecordId > 0 && ValidateInput())
            {
                var record = _context.MortalityRecords.Find(_selectedRecordId);
                if (record != null)
                {
                    record.CycleId = _cycleComboBox.SelectedValue is int cycleId ? cycleId : 0;
                    record.Date = _datePicker.Value.Date;
                    record.DeadFishCount = (int)_deadFishCountNumeric.Value;
                    record.AverageWeight = _averageWeightNumeric.Value > 0 ? (double)_averageWeightNumeric.Value : null;
                    record.Cause = GetCauseFromText(_causeComboBox.Text);
                    record.CauseDescription = _causeDescriptionTextBox.Text.Trim();
                    record.ActionTaken = _actionTakenTextBox.Text.Trim();
                    record.Treatment = _treatmentTextBox.Text.Trim();
                    record.Notes = _notesTextBox.Text.Trim();
                    record.RecordedBy = _recordedByTextBox.Text.Trim();

                    _context.SaveChanges();
                    
                    LoadMortalityRecords();
                    ClearInputs();
                    MessageBox.Show("تم تحديث تسجيل النفوق بنجاح", "نجح", MessageBoxButtons.OK, MessageBoxIcon.Information);
                }
            }
        }

        private void DeleteButton_Click(object? sender, EventArgs e)
        {
            if (_selectedRecordId > 0)
            {
                var result = MessageBox.Show("هل أنت متأكد من حذف هذا التسجيل؟", "تأكيد الحذف", 
                                           MessageBoxButtons.YesNo, MessageBoxIcon.Question);
                
                if (result == DialogResult.Yes)
                {
                    var record = _context.MortalityRecords.Find(_selectedRecordId);
                    if (record != null)
                    {
                        _context.MortalityRecords.Remove(record);
                        _context.SaveChanges();
                        
                        LoadMortalityRecords();
                        ClearInputs();
                        MessageBox.Show("تم حذف التسجيل بنجاح", "نجح", MessageBoxButtons.OK, MessageBoxIcon.Information);
                    }
                }
            }
        }

        private void ClearButton_Click(object? sender, EventArgs e)
        {
            ClearInputs();
        }

        private void ClearInputs()
        {
            _cycleComboBox.SelectedIndex = -1;
            _datePicker.Value = DateTime.Now;
            _deadFishCountNumeric.Value = 1;
            _averageWeightNumeric.Value = 0;
            _causeComboBox.SelectedIndex = -1;
            _causeDescriptionTextBox.Clear();
            _actionTakenTextBox.Clear();
            _treatmentTextBox.Clear();
            _notesTextBox.Clear();
            _recordedByTextBox.Text = Environment.UserName;
            _mortalityRateLabel.Text = "0.00%";
            _mortalityRateLabel.BackColor = Color.LightYellow;
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

            if (_deadFishCountNumeric.Value <= 0)
            {
                MessageBox.Show("يرجى إدخال عدد صحيح للأسماك النافقة", "خطأ", MessageBoxButtons.OK, MessageBoxIcon.Error);
                _deadFishCountNumeric.Focus();
                return false;
            }

            if (_causeComboBox.SelectedIndex == -1)
            {
                MessageBox.Show("يرجى اختيار سبب النفوق", "خطأ", MessageBoxButtons.OK, MessageBoxIcon.Error);
                _causeComboBox.Focus();
                return false;
            }

            if (string.IsNullOrWhiteSpace(_recordedByTextBox.Text))
            {
                MessageBox.Show("يرجى إدخال اسم المسجل", "خطأ", MessageBoxButtons.OK, MessageBoxIcon.Error);
                _recordedByTextBox.Focus();
                return false;
            }

            return true;
        }
    }
}
