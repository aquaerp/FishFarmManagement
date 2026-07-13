using System;
using System.Drawing;
using System.Linq;
using System.Windows.Forms;
using Microsoft.EntityFrameworkCore;
using FishFarmManager.Data;
using FishFarmManager.Models;
using FishFarmManager.Services;

namespace FishFarmManager.Forms
{
    public partial class ProductionCycleForm : Form
    {
        private readonly FishFarmContext _context;
        private readonly PerformanceCalculator _performanceCalculator;
        private DataGridView _cyclesGrid = null!;
        private TextBox _nameTextBox = null!;
        private CheckedListBox _pondsCheckedListBox = null!;
        private DataGridView _associatedPondsGrid = null!;
        private DateTimePicker _startDatePicker = null!;
        private DateTimePicker _endDatePicker = null!;
        private ComboBox _statusComboBox = null!;
        private NumericUpDown _initialFishCountNumeric = null!;
        private NumericUpDown _initialWeightNumeric = null!;
        private TextBox _frySourceTextBox = null!;
        private ComboBox _cycleTypeComboBox = null!;
        private TextBox _hatcherySourceTextBox = null!;
        private DateTimePicker _expectedHatchDatePicker = null!;
        private NumericUpDown _actualLarvalCountNumeric = null!;
        private Label _kpiLabel = null!;
        private NumericUpDown _finalFishCountNumeric = null!;
        private NumericUpDown _finalWeightNumeric = null!;
        private NumericUpDown _harvestWeightNumeric = null!;
        private TextBox _notesTextBox = null!;
        private Button _addButton = null!;
        private Button _updateButton = null!;
        private Button _deleteButton = null!;
        private Button _calculateButton = null!;
        private int _selectedCycleId = -1;

        public ProductionCycleForm(FishFarmContext context, PerformanceCalculator performanceCalculator)
        {
            _context = context;
            _performanceCalculator = performanceCalculator;
            InitializeComponent();
            LoadCycles();
            LoadPonds();
        }

        private void InitializeComponent()
        {
            this.SuspendLayout();
            
            this.Text = "إدارة الدورات الإنتاجية";
            this.Size = new Size(1200, 800);
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
            mainPanel.Controls.Add(_cyclesGrid, 0, 0);

            // إنشاء لوحة الإدخال
            var inputPanel = CreateInputPanel();
            mainPanel.Controls.Add(inputPanel, 1, 0);

            this.Controls.Add(mainPanel);
            this.ResumeLayout(false);
        }

        private void CreateDataGrid()
        {
            _cyclesGrid = new DataGridView();
            _cyclesGrid.Dock = DockStyle.Fill;
            _cyclesGrid.AutoGenerateColumns = false;
            _cyclesGrid.SelectionMode = DataGridViewSelectionMode.FullRowSelect;
            _cyclesGrid.MultiSelect = false;
            _cyclesGrid.ReadOnly = true;
            _cyclesGrid.AllowUserToAddRows = false;

            // إضافة الأعمدة
            _cyclesGrid.Columns.Add(new DataGridViewTextBoxColumn 
            { 
                Name = "Id",
                DataPropertyName = "Id", 
                HeaderText = "الرقم", 
                Width = 50 
            });
            
            _cyclesGrid.Columns.Add(new DataGridViewTextBoxColumn 
            { 
                DataPropertyName = "Name", 
                HeaderText = "اسم الدورة", 
                Width = 120 
            });
            
            _cyclesGrid.Columns.Add(new DataGridViewTextBoxColumn
            {
                DataPropertyName = "PondNames",
                HeaderText = "الأحواض",
                Width = 150
            });
            
            _cyclesGrid.Columns.Add(new DataGridViewTextBoxColumn 
            { 
                DataPropertyName = "StartDate", 
                HeaderText = "تاريخ البداية", 
                Width = 100,
                DefaultCellStyle = new DataGridViewCellStyle { Format = "yyyy/MM/dd" }
            });
            
            _cyclesGrid.Columns.Add(new DataGridViewTextBoxColumn 
            { 
                DataPropertyName = "EndDate", 
                HeaderText = "تاريخ النهاية", 
                Width = 100,
                DefaultCellStyle = new DataGridViewCellStyle { Format = "yyyy/MM/dd" }
            });
            
            _cyclesGrid.Columns.Add(new DataGridViewTextBoxColumn 
            { 
                DataPropertyName = "Status", 
                HeaderText = "الحالة", 
                Width = 80 
            });
            
            _cyclesGrid.Columns.Add(new DataGridViewTextBoxColumn 
            { 
                DataPropertyName = "InitialFishCount", 
                HeaderText = "عدد الأسماك", 
                Width = 80 
            });
            
            _cyclesGrid.Columns.Add(new DataGridViewTextBoxColumn 
            { 
                DataPropertyName = "SurvivalRate", 
                HeaderText = "معدل البقاء %", 
                Width = 80,
                DefaultCellStyle = new DataGridViewCellStyle { Format = "F1" }
            });

            _cyclesGrid.SelectionChanged += CyclesGrid_SelectionChanged;
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

            // اسم الدورة
            var nameLabel = new Label { Text = "اسم الدورة:", Location = new Point(10, y), Size = new Size(labelWidth, 20) };
            _nameTextBox = new TextBox { Location = new Point(140, y), Size = new Size(controlWidth, 20) };
            panel.Controls.Add(nameLabel);
            panel.Controls.Add(_nameTextBox);
            y += spacing;

            // الأحواض
            var pondLabel = new Label { Text = "الأحواض:", Location = new Point(10, y), Size = new Size(labelWidth, 20) };
            _pondsCheckedListBox = new CheckedListBox
            {
                Location = new Point(140, y),
                Size = new Size(controlWidth, 100),
                CheckOnClick = true
            };
            panel.Controls.Add(pondLabel);
            panel.Controls.Add(_pondsCheckedListBox);
            y += 110;

            // الأحواض المرتبطة
            var associatedPondsLabel = new Label { Text = "الأحواض المرتبطة:", Location = new Point(10, y), Size = new Size(labelWidth, 20) };
            _associatedPondsGrid = new DataGridView
            {
                Location = new Point(140, y),
                Size = new Size(controlWidth, 100),
                ReadOnly = true,
                AllowUserToAddRows = false,
                AutoGenerateColumns = false
            };
            _associatedPondsGrid.Columns.Add(new DataGridViewTextBoxColumn { DataPropertyName = "Name", HeaderText = "اسم الحوض", Width = 150 });
            panel.Controls.Add(associatedPondsLabel);
            panel.Controls.Add(_associatedPondsGrid);
            y += 110;

            // تاريخ البداية
            var startLabel = new Label { Text = "تاريخ البداية:", Location = new Point(10, y), Size = new Size(labelWidth, 20) };
            _startDatePicker = new DateTimePicker { Location = new Point(140, y), Size = new Size(controlWidth, 20) };
            panel.Controls.Add(startLabel);
            panel.Controls.Add(_startDatePicker);
            y += spacing;

            // تاريخ النهاية
            var endLabel = new Label { Text = "تاريخ النهاية:", Location = new Point(10, y), Size = new Size(labelWidth, 20) };
            _endDatePicker = new DateTimePicker 
            { 
                Location = new Point(140, y), 
                Size = new Size(controlWidth, 20),
                ShowCheckBox = true,
                Checked = false
            };
            panel.Controls.Add(endLabel);
            panel.Controls.Add(_endDatePicker);
            y += spacing;

            // الحالة
            var statusLabel = new Label { Text = "الحالة:", Location = new Point(10, y), Size = new Size(labelWidth, 20) };
            _statusComboBox = new ComboBox 
            { 
                Location = new Point(140, y), 
                Size = new Size(controlWidth, 20),
                DropDownStyle = ComboBoxStyle.DropDownList
            };
            _statusComboBox.Items.AddRange(new string[] { "تخطيط", "نشط", "مكتمل", "منتهي مبكراً" });
            panel.Controls.Add(statusLabel);
            panel.Controls.Add(_statusComboBox);
            y += spacing;

            // عدد الأسماك الأولي
            var initialCountLabel = new Label { Text = "عدد الأسماك الأولي:", Location = new Point(10, y), Size = new Size(labelWidth, 20) };
            _initialFishCountNumeric = new NumericUpDown 
            { 
                Location = new Point(140, y), 
                Size = new Size(controlWidth, 20),
                Maximum = 1000000,
                Minimum = 1
            };
            panel.Controls.Add(initialCountLabel);
            panel.Controls.Add(_initialFishCountNumeric);
            y += spacing;

            // الوزن الأولي
            var initialWeightLabel = new Label { Text = "الوزن الأولي (جم):", Location = new Point(10, y), Size = new Size(labelWidth, 20) };
            _initialWeightNumeric = new NumericUpDown 
            { 
                Location = new Point(140, y), 
                Size = new Size(controlWidth, 20),
                DecimalPlaces = 2,
                Maximum = 1000,
                Minimum = 0.01m  // تغيير من 0.1 إلى 0.01 لدعم الزريعة الصغيرة جداً
            };
            panel.Controls.Add(initialWeightLabel);
            panel.Controls.Add(_initialWeightNumeric);
            y += spacing;

            // مصدر الزريعة
            var frySourceLabel = new Label { Text = "مصدر الزريعة:", Location = new Point(10, y), Size = new Size(labelWidth, 20) };
            _frySourceTextBox = new TextBox { Location = new Point(140, y), Size = new Size(controlWidth, 20) };
            panel.Controls.Add(frySourceLabel);
            panel.Controls.Add(_frySourceTextBox);
            y += spacing;

            // نوع الدورة
            var cycleTypeLabel = new Label { Text = "نوع الدورة:", Location = new Point(10, y), Size = new Size(labelWidth, 20) };
            _cycleTypeComboBox = new ComboBox
            {
                Location = new Point(140, y),
                Size = new Size(controlWidth, 20),
                DropDownStyle = ComboBoxStyle.DropDownList
            };
            _cycleTypeComboBox.Items.AddRange(new string[] { "تفريخ", "حضانة", "تسمين" });
            panel.Controls.Add(cycleTypeLabel);
            panel.Controls.Add(_cycleTypeComboBox);
            y += spacing;

            // مصدر المفرخ
            var hatcherySourceLabel = new Label { Text = "مصدر المفرخ:", Location = new Point(10, y), Size = new Size(labelWidth, 20) };
            _hatcherySourceTextBox = new TextBox { Location = new Point(140, y), Size = new Size(controlWidth, 20) };
            panel.Controls.Add(hatcherySourceLabel);
            panel.Controls.Add(_hatcherySourceTextBox);
            y += spacing;

            // تاريخ الفقس المتوقع
            var expectedHatchDateLabel = new Label { Text = "تاريخ الف��س المتوقع:", Location = new Point(10, y), Size = new Size(labelWidth, 20) };
            _expectedHatchDatePicker = new DateTimePicker
            {
                Location = new Point(140, y),
                Size = new Size(controlWidth, 20),
                ShowCheckBox = true,
                Checked = false
            };
            panel.Controls.Add(expectedHatchDateLabel);
            panel.Controls.Add(_expectedHatchDatePicker);
            y += spacing;

            // العدد الفعلي لليرقات
            var actualLarvalCountLabel = new Label { Text = "العدد الفعلي لليرقات:", Location = new Point(10, y), Size = new Size(labelWidth, 20) };
            _actualLarvalCountNumeric = new NumericUpDown
            {
                Location = new Point(140, y),
                Size = new Size(controlWidth, 20),
                Maximum = 1000000,
                Minimum = 0
            };
            panel.Controls.Add(actualLarvalCountLabel);
            panel.Controls.Add(_actualLarvalCountNumeric);
            y += spacing;

            // عدد الأسماك النهائي
            var finalCountLabel = new Label { Text = "عدد الأسماك النهائي:", Location = new Point(10, y), Size = new Size(labelWidth, 20) };
            _finalFishCountNumeric = new NumericUpDown 
            { 
                Location = new Point(140, y), 
                Size = new Size(controlWidth, 20),
                Maximum = 1000000,
                Minimum = 0
            };
            panel.Controls.Add(finalCountLabel);
            panel.Controls.Add(_finalFishCountNumeric);
            y += spacing;

            // الوزن النهائي
            var finalWeightLabel = new Label { Text = "الوزن النهائي (جم):", Location = new Point(10, y), Size = new Size(labelWidth, 20) };
            _finalWeightNumeric = new NumericUpDown 
            { 
                Location = new Point(140, y), 
                Size = new Size(controlWidth, 20),
                DecimalPlaces = 2,
                Maximum = 5000,
                Minimum = 0
            };
            panel.Controls.Add(finalWeightLabel);
            panel.Controls.Add(_finalWeightNumeric);
            y += spacing;

            // وزن الحصاد
            var harvestLabel = new Label { Text = "وزن الحصاد (كجم):", Location = new Point(10, y), Size = new Size(labelWidth, 20) };
            _harvestWeightNumeric = new NumericUpDown 
            { 
                Location = new Point(140, y), 
                Size = new Size(controlWidth, 20),
                DecimalPlaces = 2,
                Maximum = 100000,
                Minimum = 0
            };
            panel.Controls.Add(harvestLabel);
            panel.Controls.Add(_harvestWeightNumeric);
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

            // الأزرار
            var buttonWidth = 70;
            var buttonSpacing = 80;
            
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
            
            _calculateButton = new Button 
            { 
                Text = "حساب الأداء", 
                Location = new Point(10 + buttonSpacing * 3, y), 
                Size = new Size(buttonWidth + 20, 30),
                BackColor = Color.LightYellow,
                Enabled = false
            };
            _calculateButton.Click += CalculateButton_Click;

            panel.Controls.Add(_addButton);
            panel.Controls.Add(_updateButton);
            panel.Controls.Add(_deleteButton);
            panel.Controls.Add(_calculateButton);

            // زر مسح الحقول
            var clearButton = new Button 
            { 
                Text = "مسح", 
                Location = new Point(10, y + 40), 
                Size = new Size(buttonWidth, 30)
            };
            clearButton.Click += ClearButton_Click;
            panel.Controls.Add(clearButton);

            // إضافة تسمية مؤشرات الأداء
            _kpiLabel = new Label 
            { 
                Text = "مؤشرات الأداء: غير محسوبة", 
                Location = new Point(10, y + 80), 
                Size = new Size(controlWidth + 120, 40),
                Font = new Font("Arial", 10, FontStyle.Bold),
                ForeColor = Color.DarkBlue,
                BorderStyle = BorderStyle.FixedSingle,
                TextAlign = ContentAlignment.MiddleCenter
            };
            panel.Controls.Add(_kpiLabel);

            return panel;
        }

        private void LoadCycles()
        {
            var cycles = _context.ProductionCycles
                .Include(c => c.ProductionCyclePonds)
                .ThenInclude(pcp => pcp.Pond)
                .Select(c => new
                {
                    c.Id,
                    c.Name,
                    PondNames = string.Join(", ", c.ProductionCyclePonds.Select(pcp => pcp.Pond.Name)),
                    c.StartDate,
                    c.EndDate,
                    c.Status,
                    c.InitialFishCount,
                    c.SurvivalRate
                })
                .OrderByDescending(c => c.StartDate)
                .ToList();

            _cyclesGrid.DataSource = cycles;
        }

        private void LoadPonds()
        {
            var ponds = _context.Ponds
                .Where(p => p.Status == PondStatus.Active || p.Status == PondStatus.Empty)
                .ToList();

            ((ListBox)_pondsCheckedListBox).DataSource = ponds;
            ((ListBox)_pondsCheckedListBox).DisplayMember = "Name";
            ((ListBox)_pondsCheckedListBox).ValueMember = "Id";
        }

        private void CyclesGrid_SelectionChanged(object? sender, EventArgs e)
        {
            if (!_cyclesGrid.Columns.Contains("Id")) return;
            if (_cyclesGrid.SelectedRows.Count > 0)
            {
                var selectedRow = _cyclesGrid.SelectedRows[0];
                var cycleId = (int)selectedRow.Cells["Id"].Value;
                
                LoadCycleData(cycleId);
                _selectedCycleId = cycleId;
                _updateButton.Enabled = true;
                _deleteButton.Enabled = true;
                _calculateButton.Enabled = true;
            }
            else
            {
                _selectedCycleId = -1;
                _updateButton.Enabled = false;
                _deleteButton.Enabled = false;
                _calculateButton.Enabled = false;
            }
        }

        private void LoadCycleData(int cycleId)
        {
            var cycle = _context.ProductionCycles
                .Include(c => c.ProductionCyclePonds)
                .ThenInclude(pcp => pcp.Pond)
                .FirstOrDefault(c => c.Id == cycleId);

            if (cycle != null)
            {
                _nameTextBox.Text = cycle.Name;
                _startDatePicker.Value = cycle.StartDate;

                if (cycle.EndDate.HasValue)
                {
                    _endDatePicker.Checked = true;
                    _endDatePicker.Value = cycle.EndDate.Value;
                }
                else
                {
                    _endDatePicker.Checked = false;
                }

                _statusComboBox.Text = GetStatusText(cycle.Status);
                _cycleTypeComboBox.Text = GetCycleTypeText(cycle.CycleType);
                _initialFishCountNumeric.Value = cycle.InitialFishCount;
                _initialWeightNumeric.Value = (decimal)cycle.InitialAverageWeight;
                _frySourceTextBox.Text = cycle.FrySource ?? "";
                _hatcherySourceTextBox.Text = cycle.HatcherySource ?? "";

                if (cycle.ExpectedHatchDate.HasValue)
                {
                    _expectedHatchDatePicker.Checked = true;
                    _expectedHatchDatePicker.Value = cycle.ExpectedHatchDate.Value;
                }
                else
                {
                    _expectedHatchDatePicker.Checked = false;
                }

                _actualLarvalCountNumeric.Value = cycle.ActualLarvalCount ?? 0;
                _finalFishCountNumeric.Value = cycle.FinalFishCount ?? 0;
                _finalWeightNumeric.Value = (decimal)(cycle.FinalAverageWeight ?? 0);
                _harvestWeightNumeric.Value = (decimal)(cycle.TotalHarvestWeight ?? 0);
                _notesTextBox.Text = cycle.Notes ?? "";

                // Update checkboxes
                for (int i = 0; i < _pondsCheckedListBox.Items.Count; i++)
                {
                    var pond = (Pond)_pondsCheckedListBox.Items[i];
                    _pondsCheckedListBox.SetItemChecked(i, cycle.ProductionCyclePonds.Any(pcp => pcp.PondId == pond.Id));
                }

                _associatedPondsGrid.DataSource = cycle.ProductionCyclePonds.Select(pcp => pcp.Pond).ToList();

                UpdateKpiLabel(cycle);
            }
        }

        private string GetStatusText(CycleStatus status)
        {
            return status switch
            {
                CycleStatus.Planning => "تخطيط",
                CycleStatus.Active => "نشط",
                CycleStatus.Completed => "مكتمل",
                CycleStatus.Terminated => "منتهي مبكراً",
                _ => "تخطيط"
            };
        }

        private CycleStatus GetStatusFromText(string text)
        {
            return text switch
            {
                "تخطيط" => CycleStatus.Planning,
                "نشط" => CycleStatus.Active,
                "مكتمل" => CycleStatus.Completed,
                "منتهي مبكراً" => CycleStatus.Terminated,
                _ => CycleStatus.Planning
            };
        }

        private string GetCycleTypeText(CycleType cycleType)
        {
            return cycleType switch
            {
                CycleType.Hatchery => "تفريخ",
                CycleType.Nursery => "حضانة",
                CycleType.GrowOut => "تسمين",
                _ => "تسمين"
            };
        }

        private CycleType GetCycleTypeFromText(string text)
        {
            return text switch
            {
                "تفريخ" => CycleType.Hatchery,
                "حضانة" => CycleType.Nursery,
                "تسمين" => CycleType.GrowOut,
                _ => CycleType.GrowOut
            };
        }

        private void UpdateKpiLabel(ProductionCycle cycle)
        {
            if (cycle != null)
            {
                var fcr = _performanceCalculator.CalculateFCR(cycle.Id);
                var adg = _performanceCalculator.CalculateADG(cycle.Id);
                var survivalRate = _performanceCalculator.CalculateSurvivalRate(cycle.Id);

                _kpiLabel.Text = $"FCR: {fcr:F2} | ADG: {adg:F2} | Survival Rate: {survivalRate:F1}%";
            }
        }

        private void AddButton_Click(object? sender, EventArgs e)
        {
            if (ValidateInput())
            {
                var cycle = new ProductionCycle
                {
                    Name = _nameTextBox.Text.Trim(),
                    StartDate = _startDatePicker.Value.Date,
                    EndDate = _endDatePicker.Checked ? _endDatePicker.Value.Date : null,
                    Status = GetStatusFromText(_statusComboBox.Text),
                    CycleType = GetCycleTypeFromText(_cycleTypeComboBox.Text),
                    InitialFishCount = (int)_initialFishCountNumeric.Value,
                    InitialAverageWeight = (decimal)_initialWeightNumeric.Value,
                    FrySource = _frySourceTextBox.Text.Trim(),
                    HatcherySource = _hatcherySourceTextBox.Text.Trim(),
                    ExpectedHatchDate = _expectedHatchDatePicker.Checked ? _expectedHatchDatePicker.Value.Date : null,
                    ActualLarvalCount = (int)_actualLarvalCountNumeric.Value > 0 ? (int)_actualLarvalCountNumeric.Value : null,
                    FinalFishCount = _finalFishCountNumeric.Value > 0 ? (int)_finalFishCountNumeric.Value : null,
                    FinalAverageWeight = _finalWeightNumeric.Value > 0 ? (decimal?)_finalWeightNumeric.Value : null,
                    TotalHarvestWeight = _harvestWeightNumeric.Value > 0 ? (decimal?)_harvestWeightNumeric.Value : null,
                    Notes = _notesTextBox.Text.Trim(),
                    ProductionCyclePonds = new List<ProductionCyclePond>()
                };

                foreach (var item in _pondsCheckedListBox.CheckedItems)
                {
                    var pond = (Pond)item;
                    cycle.ProductionCyclePonds.Add(new ProductionCyclePond { PondId = pond.Id });
                }

                _context.ProductionCycles.Add(cycle);
                _context.SaveChanges();

                LoadCycles();
                ClearInputs();
                MessageBox.Show("تم إضافة الدورة الإنتاجية بنجاح", "نجح", MessageBoxButtons.OK, MessageBoxIcon.Information);
            }
        }

        private void UpdateButton_Click(object? sender, EventArgs e)
        {
            if (_selectedCycleId > 0 && ValidateInput())
            {
                var cycle = _context.ProductionCycles
                    .Include(c => c.ProductionCyclePonds)
                    .FirstOrDefault(c => c.Id == _selectedCycleId);

                if (cycle != null)
                {
                    cycle.Name = _nameTextBox.Text.Trim();
                    cycle.StartDate = _startDatePicker.Value.Date;
                    cycle.EndDate = _endDatePicker.Checked ? _endDatePicker.Value.Date : null;
                    cycle.Status = GetStatusFromText(_statusComboBox.Text);
                    cycle.CycleType = GetCycleTypeFromText(_cycleTypeComboBox.Text);
                    cycle.InitialFishCount = (int)_initialFishCountNumeric.Value;
                    cycle.InitialAverageWeight = (decimal)_initialWeightNumeric.Value;
                    cycle.FrySource = _frySourceTextBox.Text.Trim();
                    cycle.HatcherySource = _hatcherySourceTextBox.Text.Trim();
                    cycle.ExpectedHatchDate = _expectedHatchDatePicker.Checked ? _expectedHatchDatePicker.Value.Date : null;
                    cycle.ActualLarvalCount = (int)_actualLarvalCountNumeric.Value > 0 ? (int)_actualLarvalCountNumeric.Value : null;
                    cycle.FinalFishCount = _finalFishCountNumeric.Value > 0 ? (int)_finalFishCountNumeric.Value : null;
                    cycle.FinalAverageWeight = _finalWeightNumeric.Value > 0 ? (decimal?)_finalWeightNumeric.Value : null;
                    cycle.TotalHarvestWeight = _harvestWeightNumeric.Value > 0 ? (decimal?)_harvestWeightNumeric.Value : null;
                    cycle.Notes = _notesTextBox.Text.Trim();

                    // Update ponds
                    cycle.ProductionCyclePonds.Clear();
                    foreach (var item in _pondsCheckedListBox.CheckedItems)
                    {
                        var pond = (Pond)item;
                        cycle.ProductionCyclePonds.Add(new ProductionCyclePond { PondId = pond.Id });
                    }

                    _context.SaveChanges();

                    LoadCycles();
                    ClearInputs();
                    MessageBox.Show("تم تحديث الدورة الإنتاجية بنجاح", "نجح", MessageBoxButtons.OK, MessageBoxIcon.Information);
                }
            }
        }

        private void DeleteButton_Click(object? sender, EventArgs e)
        {
            if (_selectedCycleId > 0)
            {
                var result = MessageBox.Show("هل أنت متأكد من حذف هذه الدورة الإنتاجية؟\nسيتم حذف جميع البيانات المرتبطة بها.",
                                           "تأكيد الحذف", MessageBoxButtons.YesNo, MessageBoxIcon.Question);

                if (result == DialogResult.Yes)
                {
                    var cycle = _context.ProductionCycles
                        .Include(c => c.ProductionCyclePonds)
                        .FirstOrDefault(c => c.Id == _selectedCycleId);

                    if (cycle != null)
                    {
                        // حذف البيانات المرتبطة
                        var feedingRecords = _context.FeedingRecords.Where(f => f.CycleId == cycle.Id);
                        var waterRecords = _context.WaterQualityRecords.Where(w => w.CycleId == cycle.Id);
                        var mortalityRecords = _context.MortalityRecords.Where(m => m.CycleId == cycle.Id);

                        _context.FeedingRecords.RemoveRange(feedingRecords);
                        _context.WaterQualityRecords.RemoveRange(waterRecords);
                        _context.MortalityRecords.RemoveRange(mortalityRecords);
                        _context.ProductionCyclePonds.RemoveRange(cycle.ProductionCyclePonds);
                        _context.ProductionCycles.Remove(cycle);

                        _context.SaveChanges();

                        LoadCycles();
                        ClearInputs();
                        MessageBox.Show("تم حذف الدورة الإنتاجية بنجاح", "نجح", MessageBoxButtons.OK, MessageBoxIcon.Information);
                    }
                }
            }
        }

        private void CalculateButton_Click(object? sender, EventArgs e)
        {
            if (_selectedCycleId > 0)
            {
                var cycle = _context.ProductionCycles.Find(_selectedCycleId);
                if (cycle != null)
                {
                    // حساب مؤشرات الأداء
                    var fcr = _performanceCalculator.CalculateFCR(_selectedCycleId);
                    var adg = _performanceCalculator.CalculateADG(_selectedCycleId);
                    var survivalRate = _performanceCalculator.CalculateSurvivalRate(_selectedCycleId);
                    
                    // تحديث البيانات
                    cycle.FCR = fcr > 0 ? (decimal?)fcr : null;
                    cycle.ADG = adg > 0 ? (decimal?)adg : null;
                    cycle.SurvivalRate = survivalRate > 0 ? (decimal?)survivalRate : null;
                    
                    _context.SaveChanges();
                    
                    // عرض النتائج
                    var message = $"مؤشرات الأداء:\n\n";
                    message += $"معدل التحويل الغذائي (FCR): {fcr:F2}\n";
                    message += $"معدل النمو اليومي (ADG): {adg:F2} جم/يوم\n";
                    message += $"معدل البقاء: {survivalRate:F1}%";
                    
                    MessageBox.Show(message, "مؤشرات الأداء", MessageBoxButtons.OK, MessageBoxIcon.Information);
                    
                    LoadCycles();
                }
            }
        }

        private void ClearButton_Click(object? sender, EventArgs e)
        {
            ClearInputs();
        }

        private void ClearInputs()
        {
            _nameTextBox.Clear();
            for (int i = 0; i < _pondsCheckedListBox.Items.Count; i++)
            {
                _pondsCheckedListBox.SetItemChecked(i, false);
            }
            _associatedPondsGrid.DataSource = null;
            _startDatePicker.Value = DateTime.Now;
            _endDatePicker.Checked = false;
            _statusComboBox.SelectedIndex = -1;
            _initialFishCountNumeric.Value = _initialFishCountNumeric.Minimum;
            _initialWeightNumeric.Value = _initialWeightNumeric.Minimum;
            _frySourceTextBox.Clear();
            _finalFishCountNumeric.Value = 0;
            _finalWeightNumeric.Value = 0;
            _harvestWeightNumeric.Value = 0;
            _notesTextBox.Clear();
            _selectedCycleId = -1;
            _updateButton.Enabled = false;
            _deleteButton.Enabled = false;
            _calculateButton.Enabled = false;
        }

        private bool ValidateInput()
        {
            if (string.IsNullOrWhiteSpace(_nameTextBox.Text))
            {
                MessageBox.Show("يرجى إدخال اسم الدورة", "خطأ", MessageBoxButtons.OK, MessageBoxIcon.Error);
                _nameTextBox.Focus();
                return false;
            }

            if (_pondsCheckedListBox.CheckedItems.Count == 0)
            {
                MessageBox.Show("يرجى اختيار حوض واحد على الأقل", "خطأ", MessageBoxButtons.OK, MessageBoxIcon.Error);
                _pondsCheckedListBox.Focus();
                return false;
            }

            if (_statusComboBox.SelectedIndex == -1)
            {
                MessageBox.Show("يرجى اختيار حالة الدورة", "خطأ", MessageBoxButtons.OK, MessageBoxIcon.Error);
                _statusComboBox.Focus();
                return false;
            }

            if (_initialFishCountNumeric.Value <= 0)
            {
                MessageBox.Show("يرجى إدخال عدد صحيح للأسماك", "خطأ", MessageBoxButtons.OK, MessageBoxIcon.Error);
                _initialFishCountNumeric.Focus();
                return false;
            }

            if (_initialWeightNumeric.Value <= 0)
            {
                MessageBox.Show("يرجى إدخال وزن صحيح للأسماك", "خطأ", MessageBoxButtons.OK, MessageBoxIcon.Error);
                _initialWeightNumeric.Focus();
                return false;
            }

            // فحص تكرار الاسم
            var existingCycle = _context.ProductionCycles.FirstOrDefault(c => c.Name == _nameTextBox.Text.Trim() && c.Id != _selectedCycleId);
            if (existingCycle != null)
            {
                MessageBox.Show("يوجد دورة إنتاجية بنفس الاسم مسبقاً", "خطأ", MessageBoxButtons.OK, MessageBoxIcon.Error);
                _nameTextBox.Focus();
                return false;
            }

            // فحص وجود دورة نشطة في نفس الحوض
            var selectedPondIds = _pondsCheckedListBox.CheckedItems.OfType<Pond>().Select(p => p.Id).ToList();
            var activeCycleInSelectedPonds = _context.ProductionCycles
                .Include(c => c.ProductionCyclePonds)
                .FirstOrDefault(c => c.ProductionCyclePonds.Any(pcp => selectedPondIds.Contains(pcp.PondId)) &&
                                   c.Status == CycleStatus.Active &&
                                   c.Id != _selectedCycleId);

            if (activeCycleInSelectedPonds != null && GetStatusFromText(_statusComboBox.Text) == CycleStatus.Active)
            {
                MessageBox.Show("يوجد دورة إنتاجية نشطة في أحد الأحواض المختارة مسبقاً", "خطأ", MessageBoxButtons.OK, MessageBoxIcon.Error);
                _pondsCheckedListBox.Focus();
                return false;
            }

            return true;
        }
    }
}
