using System;
using System.Drawing;
using System.Linq;
using System.Windows.Forms;
using Microsoft.EntityFrameworkCore;
using FishFarmManager.Data;
using FishFarmManager.Models;

namespace FishFarmManager.Forms
{
    public partial class PondManagementForm : Form
    {
        private readonly FishFarmContext _context;
    private DataGridView _pondsGrid = null!;
    private TextBox _nameTextBox = null!;
    private ComboBox _typeComboBox = null!;
    private NumericUpDown _capacityNumeric = null!;
    private NumericUpDown _areaNumeric = null!;
    private NumericUpDown _depthNumeric = null!;
    private ComboBox _statusComboBox = null!;
    private TextBox _notesTextBox = null!;
    private Button _addButton = null!;
    private Button _updateButton = null!;
    private Button _deleteButton = null!;
        private int _selectedPondId = -1;

        public PondManagementForm(FishFarmContext context)
        {
            _context = context;
            InitializeComponent();
            LoadPonds();
        }

        private void InitializeComponent()
        {
            this.SuspendLayout();
            
            this.Text = "إدارة الأحواض";
            this.Size = new Size(1000, 700);
            this.StartPosition = FormStartPosition.CenterScreen;
            this.RightToLeft = RightToLeft.Yes;
            this.RightToLeftLayout = true;
            try { this.Font = new Font("Cairo", 10F, FontStyle.Regular); } catch { }

            // إنشاء اللوحة الرئيسية
            var mainPanel = new TableLayoutPanel();
            mainPanel.Dock = DockStyle.Fill;
            mainPanel.ColumnCount = 2;
            mainPanel.RowCount = 1;
            mainPanel.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 60F));
            mainPanel.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 40F));

            // إنشاء جدول البيانات
            CreateDataGrid();
            mainPanel.Controls.Add(_pondsGrid, 0, 0);

            // إنشاء لوحة الإدخال
            var inputPanel = CreateInputPanel();
            mainPanel.Controls.Add(inputPanel, 1, 0);

            this.Controls.Add(mainPanel);
            this.ResumeLayout(false);
        }

        private void CreateDataGrid()
        {
            _pondsGrid = new DataGridView();
            _pondsGrid.Dock = DockStyle.Fill;
            _pondsGrid.AutoGenerateColumns = false;
            _pondsGrid.SelectionMode = DataGridViewSelectionMode.FullRowSelect;
            _pondsGrid.MultiSelect = false;
            _pondsGrid.ReadOnly = true;
            _pondsGrid.AllowUserToAddRows = false;

            // إضافة الأعمدة
            _pondsGrid.Columns.Add(new DataGridViewTextBoxColumn 
            { 
                DataPropertyName = "Id", 
                HeaderText = "الرقم", 
                Width = 60 
            });
            
            _pondsGrid.Columns.Add(new DataGridViewTextBoxColumn 
            { 
                DataPropertyName = "Name", 
                HeaderText = "اسم الحوض", 
                Width = 120 
            });
            
            _pondsGrid.Columns.Add(new DataGridViewTextBoxColumn
            {
                DataPropertyName = "PondType",
                HeaderText = "النوع",
                Width = 100
            });
            
            _pondsGrid.Columns.Add(new DataGridViewTextBoxColumn 
            { 
                DataPropertyName = "Capacity", 
                HeaderText = "السعة (م³)", 
                Width = 80 
            });
            
            _pondsGrid.Columns.Add(new DataGridViewTextBoxColumn 
            { 
                DataPropertyName = "Area", 
                HeaderText = "المساحة (م²)", 
                Width = 80 
            });
            
            _pondsGrid.Columns.Add(new DataGridViewTextBoxColumn 
            { 
                DataPropertyName = "Depth", 
                HeaderText = "العمق (م)", 
                Width = 80 
            });
            
            _pondsGrid.Columns.Add(new DataGridViewTextBoxColumn 
            { 
                DataPropertyName = "Status", 
                HeaderText = "الحالة", 
                Width = 80 
            });

            _pondsGrid.SelectionChanged += PondsGrid_SelectionChanged;
        }

        private Panel CreateInputPanel()
        {
            var panel = new Panel();
            panel.Dock = DockStyle.Fill;
            panel.Padding = new Padding(10);

            var y = 10;
            var labelWidth = 100;
            var controlWidth = 200;
            var spacing = 35;

            // اسم الحوض
            var nameLabel = new Label { Text = "اسم الحوض:", Location = new Point(10, y), Size = new Size(labelWidth, 20) };
            _nameTextBox = new TextBox { Location = new Point(120, y), Size = new Size(controlWidth, 20) };
            panel.Controls.Add(nameLabel);
            panel.Controls.Add(_nameTextBox);
            y += spacing;

            // نوع الحوض
            var typeLabel = new Label { Text = "نوع الحوض:", Location = new Point(10, y), Size = new Size(labelWidth, 20) };
            _typeComboBox = new ComboBox 
            { 
                Location = new Point(120, y), 
                Size = new Size(controlWidth, 20),
                DropDownStyle = ComboBoxStyle.DropDownList
            };
            _typeComboBox.Items.AddRange(new string[] { "Hatchery", "Nursery", "GrowOut" });
            panel.Controls.Add(typeLabel);
            panel.Controls.Add(_typeComboBox);
            y += spacing;

            // السعة
            var capacityLabel = new Label { Text = "السعة (م³):", Location = new Point(10, y), Size = new Size(labelWidth, 20) };
            _capacityNumeric = new NumericUpDown 
            { 
                Location = new Point(120, y), 
                Size = new Size(controlWidth, 20),
                DecimalPlaces = 2,
                Maximum = 10000,
                Minimum = 0.1m
            };
            panel.Controls.Add(capacityLabel);
            panel.Controls.Add(_capacityNumeric);
            y += spacing;

            // المساحة
            var areaLabel = new Label { Text = "المساحة (م²):", Location = new Point(10, y), Size = new Size(labelWidth, 20) };
            _areaNumeric = new NumericUpDown 
            { 
                Location = new Point(120, y), 
                Size = new Size(controlWidth, 20),
                DecimalPlaces = 2,
                Maximum = 5000,
                Minimum = 0.1m
            };
            panel.Controls.Add(areaLabel);
            panel.Controls.Add(_areaNumeric);
            y += spacing;

            // العمق
            var depthLabel = new Label { Text = "العمق (م):", Location = new Point(10, y), Size = new Size(labelWidth, 20) };
            _depthNumeric = new NumericUpDown 
            { 
                Location = new Point(120, y), 
                Size = new Size(controlWidth, 20),
                DecimalPlaces = 2,
                Maximum = 10,
                Minimum = 0.1m
            };
            panel.Controls.Add(depthLabel);
            panel.Controls.Add(_depthNumeric);
            y += spacing;

            // الحالة
            var statusLabel = new Label { Text = "الحالة:", Location = new Point(10, y), Size = new Size(labelWidth, 20) };
            _statusComboBox = new ComboBox 
            { 
                Location = new Point(120, y), 
                Size = new Size(controlWidth, 20),
                DropDownStyle = ComboBoxStyle.DropDownList
            };
            _statusComboBox.Items.AddRange(new string[] { "نشط", "صيانة", "فارغ", "تحضير" });
            panel.Controls.Add(statusLabel);
            panel.Controls.Add(_statusComboBox);
            y += spacing;

            // الملاحظات
            var notesLabel = new Label { Text = "ملاحظات:", Location = new Point(10, y), Size = new Size(labelWidth, 20) };
            _notesTextBox = new TextBox 
            { 
                Location = new Point(120, y), 
                Size = new Size(controlWidth, 60),
                Multiline = true
            };
            panel.Controls.Add(notesLabel);
            panel.Controls.Add(_notesTextBox);
            y += 70;

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

        private void LoadPonds()
        {
            var ponds = _context.Ponds.OrderBy(p => p.Name).ToList();
            _pondsGrid.DataSource = ponds;
        }

        private void PondsGrid_SelectionChanged(object? sender, EventArgs e)
        {
            if (_pondsGrid.SelectedRows.Count > 0)
            {
                var selectedRow = _pondsGrid.SelectedRows[0];
                var pond = selectedRow.DataBoundItem as Pond;
                
                if (pond != null)
                {
                    _selectedPondId = pond.Id;
                    LoadPondData(pond);
                    _updateButton.Enabled = true;
                    _deleteButton.Enabled = true;
                }
            }
            else
            {
                _selectedPondId = -1;
                _updateButton.Enabled = false;
                _deleteButton.Enabled = false;
            }
        }

        private void LoadPondData(Pond pond)
        {
            _nameTextBox.Text = pond.Name;
            _typeComboBox.Text = GetPondTypeText(pond.PondType);
            _capacityNumeric.Value = (decimal)pond.Capacity;
            _areaNumeric.Value = (decimal)pond.Area;
            _depthNumeric.Value = (decimal)pond.Depth;
            _statusComboBox.Text = GetStatusText(pond.Status);
            _notesTextBox.Text = pond.Notes ?? "";
        }

        private string GetStatusText(PondStatus status)
        {
            return status switch
            {
                PondStatus.Active => "نشط",
                PondStatus.Maintenance => "صيانة",
                PondStatus.Empty => "فارغ",
                PondStatus.Preparation => "تحضير",
                _ => "نشط"
            };
        }

        private PondStatus GetStatusFromText(string text)
        {
            return text switch
            {
                "نشط" => PondStatus.Active,
                "صيانة" => PondStatus.Maintenance,
                "فارغ" => PondStatus.Empty,
                "تحضير" => PondStatus.Preparation,
                _ => PondStatus.Active
            };
        }

        private void AddButton_Click(object? sender, EventArgs e)
        {
            if (ValidateInput())
            {
                var pond = new Pond
                {
                    Name = _nameTextBox.Text.Trim(),
                    PondType = GetPondTypeFromText(_typeComboBox.Text),
                    Capacity = (decimal)_capacityNumeric.Value,
                    Area = (decimal)_areaNumeric.Value,
                    Depth = (decimal)_depthNumeric.Value,
                    Status = GetStatusFromText(_statusComboBox.Text),
                    Notes = _notesTextBox.Text.Trim(),
                    CreatedDate = DateTime.Now
                };

                _context.Ponds.Add(pond);
                _context.SaveChanges();
                
                LoadPonds();
                ClearInputs();
                MessageBox.Show("تم إضافة الحوض بنجاح", "نجح", MessageBoxButtons.OK, MessageBoxIcon.Information);
            }
        }

        private void UpdateButton_Click(object? sender, EventArgs e)
        {
            if (_selectedPondId > 0 && ValidateInput())
            {
                var pond = _context.Ponds.Find(_selectedPondId);
                if (pond != null)
                {
                    pond.Name = _nameTextBox.Text.Trim();
                    pond.PondType = GetPondTypeFromText(_typeComboBox.Text);
                    pond.Capacity = (decimal)_capacityNumeric.Value;
                    pond.Area = (decimal)_areaNumeric.Value;
                    pond.Depth = (decimal)_depthNumeric.Value;
                    pond.Status = GetStatusFromText(_statusComboBox.Text);
                    pond.Notes = _notesTextBox.Text.Trim();

                    _context.SaveChanges();
                    
                    LoadPonds();
                    ClearInputs();
                    MessageBox.Show("تم تحديث الحوض بنجاح", "نجح", MessageBoxButtons.OK, MessageBoxIcon.Information);
                }
            }
        }

        private void DeleteButton_Click(object? sender, EventArgs e)
        {
            if (_selectedPondId > 0)
            {
                var result = MessageBox.Show("هل أنت متأكد من حذف هذا الحوض؟", "تأكيد الحذف", 
                                           MessageBoxButtons.YesNo, MessageBoxIcon.Question);
                
                if (result == DialogResult.Yes)
                {
                    var pond = _context.Ponds.Find(_selectedPondId);
                    if (pond != null)
                    {
                        // فحص وجود دورات مرتبطة
                        var hasCycles = _context.ProductionCycles.Any(c => c.ProductionCyclePonds.Any(pcp => pcp.PondId == pond.Id));
                        if (hasCycles)
                        {
                            MessageBox.Show("لا يمكن حذف الحوض لوجود دورات إنتاجية مرتبطة به", "خطأ", 
                                          MessageBoxButtons.OK, MessageBoxIcon.Error);
                            return;
                        }

                        _context.Ponds.Remove(pond);
                        _context.SaveChanges();
                        
                        LoadPonds();
                        ClearInputs();
                        MessageBox.Show("تم حذف الحوض بنجاح", "نجح", MessageBoxButtons.OK, MessageBoxIcon.Information);
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
            _nameTextBox.Clear();
            _typeComboBox.SelectedIndex = -1;
            _capacityNumeric.Value = _capacityNumeric.Minimum;
            _areaNumeric.Value = _areaNumeric.Minimum;
            _depthNumeric.Value = _depthNumeric.Minimum;
            _statusComboBox.SelectedIndex = -1;
            _notesTextBox.Clear();
            _selectedPondId = -1;
            _updateButton.Enabled = false;
            _deleteButton.Enabled = false;
        }

        private bool ValidateInput()
        {
            if (string.IsNullOrWhiteSpace(_nameTextBox.Text))
            {
                MessageBox.Show("يرجى إدخال اسم الحوض", "خطأ", MessageBoxButtons.OK, MessageBoxIcon.Error);
                _nameTextBox.Focus();
                return false;
            }

            if (_typeComboBox.SelectedIndex == -1)
            {
                MessageBox.Show("يرجى اختيار نوع الحوض", "خطأ", MessageBoxButtons.OK, MessageBoxIcon.Error);
                _typeComboBox.Focus();
                return false;
            }

            if (_capacityNumeric.Value <= 0)
            {
                MessageBox.Show("يرجى إدخال سعة صحيحة للحوض", "خطأ", MessageBoxButtons.OK, MessageBoxIcon.Error);
                _capacityNumeric.Focus();
                return false;
            }

            if (_areaNumeric.Value <= 0)
            {
                MessageBox.Show("يرجى إدخال مساحة صحيحة للحوض", "خطأ", MessageBoxButtons.OK, MessageBoxIcon.Error);
                _areaNumeric.Focus();
                return false;
            }

            if (_depthNumeric.Value <= 0)
            {
                MessageBox.Show("يرجى إدخال عمق صحيح للحوض", "خطأ", MessageBoxButtons.OK, MessageBoxIcon.Error);
                _depthNumeric.Focus();
                return false;
            }

            if (_statusComboBox.SelectedIndex == -1)
            {
                MessageBox.Show("يرجى اختيار حالة الحوض", "خطأ", MessageBoxButtons.OK, MessageBoxIcon.Error);
                _statusComboBox.Focus();
                return false;
            }

            // فحص تكرار الاسم
            var existingPond = _context.Ponds.FirstOrDefault(p => p.Name == _nameTextBox.Text.Trim() && p.Id != _selectedPondId);
            if (existingPond != null)
            {
                MessageBox.Show("يوجد حوض بنفس الاسم مسبقاً", "خطأ", MessageBoxButtons.OK, MessageBoxIcon.Error);
                _nameTextBox.Focus();
                return false;
            }

            return true;
        }

        private string GetPondTypeText(PondType pondType)
        {
            return pondType switch
            {
                PondType.Hatchery => "Hatchery",
                PondType.Nursery => "Nursery",
                PondType.GrowOut => "GrowOut",
                _ => ""
            };
        }

        private PondType GetPondTypeFromText(string text)
        {
            return text switch
            {
                "Hatchery" => PondType.Hatchery,
                "Nursery" => PondType.Nursery,
                "GrowOut" => PondType.GrowOut,
                _ => throw new ArgumentOutOfRangeException(nameof(text), "Invalid PondType")
            };
        }
    }
}