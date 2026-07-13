using System;
using System.Drawing;
using System.Linq;
using System.Windows.Forms;
using Microsoft.EntityFrameworkCore;
using FishFarmManager.Data;
using FishFarmManager.Models;

namespace FishFarmManager.Forms
{
    public partial class FeedingRecordForm : Form
    {
        private readonly FishFarmContext _context;
        private DataGridView _feedingGrid = null!;
        private ComboBox _cycleComboBox = null!;
        private DateTimePicker _feedingDatePicker = null!;
        private ComboBox _feedTypeComboBox = null!;
        private NumericUpDown _quantityNumeric = null!;
        private NumericUpDown _feedPriceNumeric = null!;
        private NumericUpDown _feedingTimesNumeric = null!;
        private NumericUpDown _estimatedWeightNumeric = null!;
        private NumericUpDown _estimatedCountNumeric = null!;
        private TextBox _notesTextBox = null!;
        private TextBox _recordedByTextBox = null!;
        private Label _totalCostLabel = null!;
        private Button _addButton = null!;
        private Button _updateButton = null!;
        private Button _deleteButton = null!;
        private int _selectedRecordId = -1;

        public FeedingRecordForm(FishFarmContext context)
        {
            _context = context;
            InitializeComponent();
            LoadFeedingRecords();
            LoadActiveCycles();
            this.RightToLeft = RightToLeft.Yes;
            this.RightToLeftLayout = true;
            try { this.Font = new Font("Cairo", 10F, FontStyle.Regular); } catch { }
        }

        private void InitializeComponent()
        {
            this.SuspendLayout();
            
            this.Text = "تسجيل التغذية";
            this.Size = new Size(1200, 700);
            this.StartPosition = FormStartPosition.CenterScreen;

            // إنشاء اللوحة الرئيسية
            var mainPanel = new TableLayoutPanel();
            mainPanel.Dock = DockStyle.Fill;
            mainPanel.ColumnCount = 2;
            mainPanel.RowCount = 1;
            mainPanel.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 65F));
            mainPanel.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 35F));

            // إنشاء جدول البيانات
            CreateDataGrid();
            mainPanel.Controls.Add(_feedingGrid, 0, 0);

            // إنشاء لوحة الإدخال
            var inputPanel = CreateInputPanel();
            mainPanel.Controls.Add(inputPanel, 1, 0);

            this.Controls.Add(mainPanel);
            this.ResumeLayout(false);
        }

        private void CreateDataGrid()
        {
            _feedingGrid = new DataGridView();
            _feedingGrid.Dock = DockStyle.Fill;
            _feedingGrid.AutoGenerateColumns = false;
            _feedingGrid.SelectionMode = DataGridViewSelectionMode.FullRowSelect;
            _feedingGrid.MultiSelect = false;
            _feedingGrid.ReadOnly = true;
            _feedingGrid.AllowUserToAddRows = false;

            // إضافة الأعمدة
            _feedingGrid.Columns.Add(new DataGridViewTextBoxColumn 
            { 
                Name = "Id",
                DataPropertyName = "Id", 
                HeaderText = "الرقم", 
                Width = 50 
            });
            
            _feedingGrid.Columns.Add(new DataGridViewTextBoxColumn 
            { 
                DataPropertyName = "CycleName", 
                HeaderText = "الدورة", 
                Width = 120 
            });
            
            _feedingGrid.Columns.Add(new DataGridViewTextBoxColumn 
            { 
                DataPropertyName = "PondName", 
                HeaderText = "الحوض", 
                Width = 100 
            });
            
            _feedingGrid.Columns.Add(new DataGridViewTextBoxColumn 
            { 
                DataPropertyName = "FeedingDate", 
                HeaderText = "التاريخ", 
                Width = 100,
                DefaultCellStyle = new DataGridViewCellStyle { Format = "yyyy/MM/dd" }
            });
            
            _feedingGrid.Columns.Add(new DataGridViewTextBoxColumn 
            { 
                DataPropertyName = "FeedType", 
                HeaderText = "نوع العلف", 
                Width = 100 
            });
            
            _feedingGrid.Columns.Add(new DataGridViewTextBoxColumn 
            { 
                DataPropertyName = "Quantity", 
                HeaderText = "الكمية (كجم)", 
                Width = 80,
                DefaultCellStyle = new DataGridViewCellStyle { Format = "F2" }
            });
            
            _feedingGrid.Columns.Add(new DataGridViewTextBoxColumn 
            { 
                DataPropertyName = "FeedPrice", 
                HeaderText = "سعر الكجم", 
                Width = 80,
                DefaultCellStyle = new DataGridViewCellStyle { Format = "F2" }
            });
            
            _feedingGrid.Columns.Add(new DataGridViewTextBoxColumn 
            { 
                DataPropertyName = "TotalCost", 
                HeaderText = "التكلفة الإجمالية", 
                Width = 100,
                DefaultCellStyle = new DataGridViewCellStyle { Format = "F2" }
            });
            
            _feedingGrid.Columns.Add(new DataGridViewTextBoxColumn 
            { 
                DataPropertyName = "FeedingTimes", 
                HeaderText = "عدد المرات", 
                Width = 80 
            });
            
            _feedingGrid.Columns.Add(new DataGridViewTextBoxColumn 
            { 
                DataPropertyName = "RecordedBy", 
                HeaderText = "المسجل", 
                Width = 100 
            });

            _feedingGrid.SelectionChanged += FeedingGrid_SelectionChanged;
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
            panel.Controls.Add(cycleLabel);
            panel.Controls.Add(_cycleComboBox);
            y += spacing;

            // تاريخ التغذية
            var dateLabel = new Label { Text = "تاريخ التغذية:", Location = new Point(10, y), Size = new Size(labelWidth, 20) };
            _feedingDatePicker = new DateTimePicker { Location = new Point(140, y), Size = new Size(controlWidth, 20) };
            panel.Controls.Add(dateLabel);
            panel.Controls.Add(_feedingDatePicker);
            y += spacing;

            // نوع العلف
            var feedTypeLabel = new Label { Text = "نوع العلف:", Location = new Point(10, y), Size = new Size(labelWidth, 20) };
            _feedTypeComboBox = new ComboBox
            {
                Location = new Point(140, y),
                Size = new Size(controlWidth, 20),
                DropDownStyle = ComboBoxStyle.DropDownList
            };
            _feedTypeComboBox.Items.AddRange(new string[] { "Starter", "Grower", "Finisher" });
            panel.Controls.Add(feedTypeLabel);
            panel.Controls.Add(_feedTypeComboBox);
            y += spacing;

            // الكمية
            var quantityLabel = new Label { Text = "الكمية (كجم):", Location = new Point(10, y), Size = new Size(labelWidth, 20) };
            _quantityNumeric = new NumericUpDown 
            { 
                Location = new Point(140, y), 
                Size = new Size(controlWidth, 20),
                DecimalPlaces = 2,
                Maximum = 1000,
                Minimum = 0.1m
            };
            _quantityNumeric.ValueChanged += CalculateTotalCost;
            panel.Controls.Add(quantityLabel);
            panel.Controls.Add(_quantityNumeric);
            y += spacing;

            // سعر الكجم
            var priceLabel = new Label { Text = "سعر الكجم:", Location = new Point(10, y), Size = new Size(labelWidth, 20) };
            _feedPriceNumeric = new NumericUpDown 
            { 
                Location = new Point(140, y), 
                Size = new Size(controlWidth, 20),
                DecimalPlaces = 2,
                Maximum = 1000,
                Minimum = 0.1m
            };
            _feedPriceNumeric.ValueChanged += CalculateTotalCost;
            panel.Controls.Add(priceLabel);
            panel.Controls.Add(_feedPriceNumeric);
            y += spacing;

            // التكلفة الإجمالية
            var totalCostTitleLabel = new Label { Text = "التكلفة الإجمالية:", Location = new Point(10, y), Size = new Size(labelWidth, 20) };
            _totalCostLabel = new Label 
            { 
                Text = "0.00", 
                Location = new Point(140, y), 
                Size = new Size(controlWidth, 20),
                BackColor = Color.LightYellow,
                BorderStyle = BorderStyle.FixedSingle,
                TextAlign = ContentAlignment.MiddleCenter,
                Font = new Font("Arial", 9, FontStyle.Bold)
            };
            panel.Controls.Add(totalCostTitleLabel);
            panel.Controls.Add(_totalCostLabel);
            y += spacing;

            // عدد مرات التغذية
            var timesLabel = new Label { Text = "عدد مرات التغذية:", Location = new Point(10, y), Size = new Size(labelWidth, 20) };
            _feedingTimesNumeric = new NumericUpDown 
            { 
                Location = new Point(140, y), 
                Size = new Size(controlWidth, 20),
                Maximum = 10,
                Minimum = 1,
                Value = 1
            };
            panel.Controls.Add(timesLabel);
            panel.Controls.Add(_feedingTimesNumeric);
            y += spacing;

            // الوزن المقدر للأسماك
            var weightLabel = new Label { Text = "الوزن المقدر (جم):", Location = new Point(10, y), Size = new Size(labelWidth, 20) };
            _estimatedWeightNumeric = new NumericUpDown 
            { 
                Location = new Point(140, y), 
                Size = new Size(controlWidth, 20),
                DecimalPlaces = 2,
                Maximum = 5000,
                Minimum = 0
            };
            panel.Controls.Add(weightLabel);
            panel.Controls.Add(_estimatedWeightNumeric);
            y += spacing;

            // العدد المقدر للأسماك
            var countLabel = new Label { Text = "العدد المقدر:", Location = new Point(10, y), Size = new Size(labelWidth, 20) };
            _estimatedCountNumeric = new NumericUpDown 
            { 
                Location = new Point(140, y), 
                Size = new Size(controlWidth, 20),
                Maximum = 1000000,
                Minimum = 0
            };
            panel.Controls.Add(countLabel);
            panel.Controls.Add(_estimatedCountNumeric);
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

        private void LoadFeedingRecords()
        {
            var records = _context.FeedingRecords
                .Include(f => f.Cycle)
                    .ThenInclude(c => c.ProductionCyclePonds)
                        .ThenInclude(pcp => pcp.Pond)
                .Select(f => new
                {
                    f.Id,
                    CycleName = f.Cycle.Name,
                    PondName = string.Join(", ", f.Cycle.ProductionCyclePonds.Select(pcp => pcp.Pond.Name)),
                    f.FeedingDate,
                    f.FeedType,
                    f.Quantity,
                    f.FeedPrice,
                    TotalCost = f.Quantity * f.FeedPrice,
                    f.FeedingTimes,
                    f.RecordedBy
                })
                .OrderByDescending(f => f.FeedingDate)
                .ToList();
            
            _feedingGrid.DataSource = records;
        }

        private void LoadActiveCycles()
        {
            _cycleComboBox.SelectedIndexChanged -= (s, e) => { };
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
            // no handler to attach here, only selection used on save
        }

        private void FeedingGrid_SelectionChanged(object? sender, EventArgs e)
        {
            if (!_feedingGrid.Columns.Contains("Id")) return;
            if (_feedingGrid.SelectedRows.Count > 0)
            {
                var selectedRow = _feedingGrid.SelectedRows[0];
                var recordId = (int)selectedRow.Cells["Id"].Value;
                
                LoadFeedingData(recordId);
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

        private void LoadFeedingData(int recordId)
        {
            var record = _context.FeedingRecords
                .Include(f => f.Cycle)
                .FirstOrDefault(f => f.Id == recordId);

            if (record != null)
            {
                _cycleComboBox.SelectedValue = record.CycleId;
                _feedingDatePicker.Value = record.FeedingDate;
                _feedTypeComboBox.Text = GetFeedTypeText(record.FeedType);
                _quantityNumeric.Value = (decimal)record.Quantity;
                _feedPriceNumeric.Value = (decimal)record.FeedPrice;
                _feedingTimesNumeric.Value = record.FeedingTimes;
                _estimatedWeightNumeric.Value = (decimal)(record.EstimatedFishWeight ?? 0);
                _estimatedCountNumeric.Value = record.EstimatedFishCount ?? 0;
                _notesTextBox.Text = record.Notes ?? "";
                _recordedByTextBox.Text = record.RecordedBy ?? "";
                
                CalculateTotalCost(null, EventArgs.Empty);
            }
        }

        private void CalculateTotalCost(object? sender, EventArgs e)
        {
            var totalCost = _quantityNumeric.Value * _feedPriceNumeric.Value;
            _totalCostLabel.Text = totalCost.ToString("F2");
        }

        private void AddButton_Click(object? sender, EventArgs e)
        {
            if (ValidateInput())
            {
                var record = new FeedingRecord
                {
                    CycleId = _cycleComboBox.SelectedValue is int cycleId ? cycleId : 0,
                    FeedingDate = _feedingDatePicker.Value.Date,
                    FeedType = GetFeedTypeFromText(_feedTypeComboBox.Text),
                    Quantity = (double)_quantityNumeric.Value,
                    FeedPrice = (double)_feedPriceNumeric.Value,
                    FeedingTimes = (int)_feedingTimesNumeric.Value,
                    EstimatedFishWeight = _estimatedWeightNumeric.Value > 0 ? (double)_estimatedWeightNumeric.Value : null,
                    EstimatedFishCount = _estimatedCountNumeric.Value > 0 ? (int)_estimatedCountNumeric.Value : null,
                    Notes = _notesTextBox.Text.Trim(),
                    RecordedBy = _recordedByTextBox.Text.Trim(),
                    CreatedAt = DateTime.Now
                };

                _context.FeedingRecords.Add(record);
                _context.SaveChanges();
                
                LoadFeedingRecords();
                ClearInputs();
                MessageBox.Show("تم تسجيل التغذية بنجاح", "نجح", MessageBoxButtons.OK, MessageBoxIcon.Information);
            }
        }

        private void UpdateButton_Click(object? sender, EventArgs e)
        {
            if (_selectedRecordId > 0 && ValidateInput())
            {
                var record = _context.FeedingRecords.Find(_selectedRecordId);
                if (record != null)
                {
                    record.CycleId = _cycleComboBox.SelectedValue is int cycleId ? cycleId : 0;
                    record.FeedingDate = _feedingDatePicker.Value.Date;
                    record.FeedType = GetFeedTypeFromText(_feedTypeComboBox.Text);
                    record.Quantity = (double)_quantityNumeric.Value;
                    record.FeedPrice = (double)_feedPriceNumeric.Value;
                    record.FeedingTimes = (int)_feedingTimesNumeric.Value;
                    record.EstimatedFishWeight = _estimatedWeightNumeric.Value > 0 ? (double)_estimatedWeightNumeric.Value : null;
                    record.EstimatedFishCount = _estimatedCountNumeric.Value > 0 ? (int)_estimatedCountNumeric.Value : null;
                    record.Notes = _notesTextBox.Text.Trim();
                    record.RecordedBy = _recordedByTextBox.Text.Trim();

                    _context.SaveChanges();
                    
                    LoadFeedingRecords();
                    ClearInputs();
                    MessageBox.Show("تم تحديث تسجيل التغذية بنجاح", "نجح", MessageBoxButtons.OK, MessageBoxIcon.Information);
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
                    var record = _context.FeedingRecords.Find(_selectedRecordId);
                    if (record != null)
                    {
                        _context.FeedingRecords.Remove(record);
                        _context.SaveChanges();
                        
                        LoadFeedingRecords();
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
            _feedingDatePicker.Value = DateTime.Now;
            _feedTypeComboBox.SelectedIndex = -1;
            _quantityNumeric.Value = _quantityNumeric.Minimum;
            _feedPriceNumeric.Value = _feedPriceNumeric.Minimum;
            _feedingTimesNumeric.Value = 1;
            _estimatedWeightNumeric.Value = 0;
            _estimatedCountNumeric.Value = 0;
            _notesTextBox.Clear();
            _recordedByTextBox.Text = Environment.UserName;
            _totalCostLabel.Text = "0.00";
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

            if (_feedTypeComboBox.SelectedIndex == -1)
            {
                MessageBox.Show("يرجى اختيار نوع العلف", "خطأ", MessageBoxButtons.OK, MessageBoxIcon.Error);
                _feedTypeComboBox.Focus();
                return false;
            }

            if (_quantityNumeric.Value <= 0)
            {
                MessageBox.Show("يرجى إدخال كمية صحيحة", "خطأ", MessageBoxButtons.OK, MessageBoxIcon.Error);
                _quantityNumeric.Focus();
                return false;
            }

            if (_feedPriceNumeric.Value <= 0)
            {
                MessageBox.Show("يرجى إدخال سعر صحيح", "خطأ", MessageBoxButtons.OK, MessageBoxIcon.Error);
                _feedPriceNumeric.Focus();
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

        private string GetFeedTypeText(FeedType feedType)
        {
            return feedType switch
            {
                FeedType.Starter => "Starter",
                FeedType.Grower => "Grower",
                FeedType.Finisher => "Finisher",
                _ => ""
            };
        }

        private FeedType GetFeedTypeFromText(string text)
        {
            return text switch
            {
                "Starter" => FeedType.Starter,
                "Grower" => FeedType.Grower,
                "Finisher" => FeedType.Finisher,
                _ => throw new ArgumentOutOfRangeException(nameof(text), "Invalid FeedType")
            };
        }
    }
}
