using System;
using System.Drawing;
using System.Linq;
using System.Windows.Forms;
using Microsoft.EntityFrameworkCore;
using FishFarmManager.Data;
using FishFarmManager.Models;

namespace FishFarmManager.Forms
{
    public partial class TreatmentRecordForm : Form
    {
        private readonly FishFarmContext _context;
        private DataGridView _treatmentGrid = null!;
        private ComboBox _cycleComboBox = null!;
        private ComboBox _pondComboBox = null!;
        private TextBox _treatmentNameTextBox = null!;
        private TextBox _dosageTextBox = null!;
        private DateTimePicker _datePicker = null!;
        private TextBox _withdrawalTextBox = null!;
        private TextBox _notesTextBox = null!;
        private TextBox _recordedByTextBox = null!;
        private Button _addButton = null!;
        private Button _deleteButton = null!;
        private int _selectedRecordId = -1;

        public TreatmentRecordForm(FishFarmContext context)
        {
            _context = context;
            InitializeComponent();
            LoadTreatmentRecords();
            LoadCycles();
        }

        private void InitializeComponent()
        {
            this.Text = "سجل الأدوية والمعالجات";
            this.Size = new Size(900, 600);
            this.StartPosition = FormStartPosition.CenterScreen;
            this.RightToLeft = RightToLeft.Yes;
            this.RightToLeftLayout = true;
            try { this.Font = new Font("Cairo", 10F, FontStyle.Regular); } catch { /* fallback to default font */ }

            var mainPanel = new TableLayoutPanel();
            mainPanel.Dock = DockStyle.Fill;
            mainPanel.ColumnCount = 2;
            mainPanel.RowCount = 1;
            mainPanel.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 60F));
            mainPanel.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 40F));

            // جدول البيانات
            _treatmentGrid = new DataGridView();
            _treatmentGrid.Dock = DockStyle.Fill;
            _treatmentGrid.AutoGenerateColumns = false;
            _treatmentGrid.SelectionMode = DataGridViewSelectionMode.FullRowSelect;
            _treatmentGrid.MultiSelect = false;
            _treatmentGrid.ReadOnly = true;
            _treatmentGrid.AllowUserToAddRows = false;
            _treatmentGrid.Columns.Add(new DataGridViewTextBoxColumn { DataPropertyName = "Id", Name = "Id", HeaderText = "الرقم", Width = 50 });
            _treatmentGrid.Columns.Add(new DataGridViewTextBoxColumn { DataPropertyName = "CycleName", HeaderText = "الدورة", Width = 100 });
            _treatmentGrid.Columns.Add(new DataGridViewTextBoxColumn { DataPropertyName = "PondName", HeaderText = "الحوض", Width = 100 });
            _treatmentGrid.Columns.Add(new DataGridViewTextBoxColumn { DataPropertyName = "TreatmentName", HeaderText = "اسم العلاج/الدواء", Width = 120 });
            _treatmentGrid.Columns.Add(new DataGridViewTextBoxColumn { DataPropertyName = "Dosage", HeaderText = "الجرعة", Width = 80 });
            _treatmentGrid.Columns.Add(new DataGridViewTextBoxColumn { DataPropertyName = "Date", HeaderText = "تاريخ التطبيق", Width = 100, DefaultCellStyle = new DataGridViewCellStyle { Format = "yyyy/MM/dd" } });
            _treatmentGrid.Columns.Add(new DataGridViewTextBoxColumn { DataPropertyName = "WithdrawalPeriod", HeaderText = "فترة السحب", Width = 80 });
            _treatmentGrid.Columns.Add(new DataGridViewTextBoxColumn { DataPropertyName = "RecordedBy", HeaderText = "المسجل", Width = 80 });
            _treatmentGrid.SelectionChanged += TreatmentGrid_SelectionChanged;
            mainPanel.Controls.Add(_treatmentGrid, 0, 0);

            // لوحة الإدخال
            var inputPanel = new Panel();
            inputPanel.Dock = DockStyle.Fill;
            inputPanel.Padding = new Padding(10);
            int y = 10, spacing = 35, labelWidth = 90, controlWidth = 150;

            // الدورة
            var cycleLabel = new Label { Text = "الدورة:", Location = new Point(10, y), Size = new Size(labelWidth, 20) };
            _cycleComboBox = new ComboBox { Location = new Point(110, y), Size = new Size(controlWidth, 20), DropDownStyle = ComboBoxStyle.DropDownList };
            _cycleComboBox.SelectedIndexChanged += CycleComboBox_SelectedIndexChanged;
            inputPanel.Controls.Add(cycleLabel);
            inputPanel.Controls.Add(_cycleComboBox);
            y += spacing;

            // الحوض
            var pondLabel = new Label { Text = "الحوض:", Location = new Point(10, y), Size = new Size(labelWidth, 20) };
            _pondComboBox = new ComboBox { Location = new Point(110, y), Size = new Size(controlWidth, 20), DropDownStyle = ComboBoxStyle.DropDownList };
            inputPanel.Controls.Add(pondLabel);
            inputPanel.Controls.Add(_pondComboBox);
            y += spacing;

            // اسم العلاج
            var treatmentLabel = new Label { Text = "اسم العلاج:", Location = new Point(10, y), Size = new Size(labelWidth, 20) };
            _treatmentNameTextBox = new TextBox { Location = new Point(110, y), Size = new Size(controlWidth, 20) };
            inputPanel.Controls.Add(treatmentLabel);
            inputPanel.Controls.Add(_treatmentNameTextBox);
            y += spacing;

            // الجرعة
            var dosageLabel = new Label { Text = "الجرعة:", Location = new Point(10, y), Size = new Size(labelWidth, 20) };
            _dosageTextBox = new TextBox { Location = new Point(110, y), Size = new Size(controlWidth, 20) };
            inputPanel.Controls.Add(dosageLabel);
            inputPanel.Controls.Add(_dosageTextBox);
            y += spacing;

            // التاريخ
            var dateLabel = new Label { Text = "تاريخ التطبيق:", Location = new Point(10, y), Size = new Size(labelWidth, 20) };
            _datePicker = new DateTimePicker { Location = new Point(110, y), Size = new Size(controlWidth, 20) };
            inputPanel.Controls.Add(dateLabel);
            inputPanel.Controls.Add(_datePicker);
            y += spacing;

            // فترة السحب
            var withdrawalLabel = new Label { Text = "فترة السحب:", Location = new Point(10, y), Size = new Size(labelWidth, 20) };
            _withdrawalTextBox = new TextBox { Location = new Point(110, y), Size = new Size(controlWidth, 20) };
            inputPanel.Controls.Add(withdrawalLabel);
            inputPanel.Controls.Add(_withdrawalTextBox);
            y += spacing;

            // ملاحظات
            var notesLabel = new Label { Text = "ملاحظات:", Location = new Point(10, y), Size = new Size(labelWidth, 20) };
            _notesTextBox = new TextBox { Location = new Point(110, y), Size = new Size(controlWidth, 40), Multiline = true };
            inputPanel.Controls.Add(notesLabel);
            inputPanel.Controls.Add(_notesTextBox);
            y += 50;

            // المسجل
            var recordedByLabel = new Label { Text = "المسجل:", Location = new Point(10, y), Size = new Size(labelWidth, 20) };
            _recordedByTextBox = new TextBox { Location = new Point(110, y), Size = new Size(controlWidth, 20), Text = Environment.UserName };
            inputPanel.Controls.Add(recordedByLabel);
            inputPanel.Controls.Add(_recordedByTextBox);
            y += spacing;

            // الأزرار
            _addButton = new Button { Text = "إضافة", Location = new Point(10, y), Size = new Size(80, 30), BackColor = Color.LightGreen };
            _addButton.Click += AddButton_Click;
            inputPanel.Controls.Add(_addButton);
            _deleteButton = new Button { Text = "حذف", Location = new Point(100, y), Size = new Size(80, 30), BackColor = Color.LightCoral, Enabled = false };
            _deleteButton.Click += DeleteButton_Click;
            inputPanel.Controls.Add(_deleteButton);

            mainPanel.Controls.Add(inputPanel, 1, 0);
            this.Controls.Add(mainPanel);
        }

        private void LoadTreatmentRecords()
        {
            var records = _context.TreatmentRecords
                .Include(t => t.Cycle)
                .Include(t => t.Pond)
                .OrderByDescending(t => t.Date)
                .Select(t => new
                {
                    t.Id,
                    CycleName = t.Cycle != null ? t.Cycle.Name : string.Empty,
                    PondName = t.Pond != null ? t.Pond.Name : string.Empty,
                    t.TreatmentName,
                    t.Dosage,
                    t.Date,
                    WithdrawalPeriod = t.WithdrawalPeriod,
                    t.RecordedBy
                })
                .ToList();
            _treatmentGrid.DataSource = records;
        }

        private void LoadCycles()
        {
            _cycleComboBox.SelectedIndexChanged -= CycleComboBox_SelectedIndexChanged;

            var cycles = _context.ProductionCycles
                .Where(c => c.Status == CycleStatus.Active || c.Status == CycleStatus.Planning)
                .Select(c => new { c.Id, c.Name })
                .OrderBy(c => c.Name)
                .ToList();

            _cycleComboBox.DisplayMember = "Name";
            _cycleComboBox.ValueMember = "Id";
            _cycleComboBox.DataSource = cycles;
            _cycleComboBox.SelectedIndex = -1;

            _cycleComboBox.SelectedIndexChanged += CycleComboBox_SelectedIndexChanged;
        }

        private void CycleComboBox_SelectedIndexChanged(object? sender, EventArgs e)
        {
            if (_cycleComboBox.SelectedIndex >= 0 && _cycleComboBox.SelectedValue != null)
            {
                int cycleId;
                if (_cycleComboBox.SelectedValue is int v)
                {
                    cycleId = v;
                }
                else
                {
                    var item = _cycleComboBox.SelectedItem;
                    var prop = item?.GetType().GetProperty("Id");
                    if (prop == null) return;
                    cycleId = Convert.ToInt32(prop.GetValue(item) ?? 0);
                }
                var ponds = _context.ProductionCyclePonds
                    .Include(pcp => pcp.Pond)
                    .Where(pcp => pcp.ProductionCycleId == cycleId)
                    .Select(pcp => new { pcp.Pond.Id, pcp.Pond.Name })
                    .ToList();
                _pondComboBox.DataSource = ponds;
                _pondComboBox.DisplayMember = "Name";
                _pondComboBox.ValueMember = "Id";
                _pondComboBox.SelectedIndex = -1;
            }
        }

        private void AddButton_Click(object? sender, EventArgs e)
        {
            if (_cycleComboBox.SelectedIndex == -1 || _pondComboBox.SelectedIndex == -1 || string.IsNullOrWhiteSpace(_treatmentNameTextBox.Text))
            {
                MessageBox.Show("يرجى تعبئة جميع الحقول الأساسية", "خطأ", MessageBoxButtons.OK, MessageBoxIcon.Error);
                return;
            }
            var record = new TreatmentRecord
            {
                CycleId = _cycleComboBox.SelectedValue is int cycleId ? cycleId : 0,
                PondId = _pondComboBox.SelectedValue is int pondId ? pondId : 0,
                TreatmentName = _treatmentNameTextBox.Text.Trim(),
                Dosage = decimal.TryParse(_dosageTextBox.Text.Trim(), out var dosage) ? dosage : 0m,
                Date = _datePicker.Value.Date,
                WithdrawalPeriod = int.TryParse(_withdrawalTextBox.Text.Trim(), out var withdrawal) ? (int?)withdrawal : null,
                Notes = _notesTextBox.Text.Trim(),
                RecordedBy = _recordedByTextBox.Text.Trim(),
                CreatedAt = DateTime.Now
            };
            _context.TreatmentRecords.Add(record);
            _context.SaveChanges();
            LoadTreatmentRecords();
            ClearInputs();
            MessageBox.Show("تمت إضافة السجل بنجاح", "نجاح", MessageBoxButtons.OK, MessageBoxIcon.Information);
        }

        private void DeleteButton_Click(object? sender, EventArgs e)
        {
            if (_selectedRecordId > 0)
            {
                var result = MessageBox.Show("هل أنت متأكد من حذف السجل؟", "تأكيد الحذف", MessageBoxButtons.YesNo, MessageBoxIcon.Question);
                if (result == DialogResult.Yes)
                {
                    var record = _context.TreatmentRecords.Find(_selectedRecordId);
                    if (record != null)
                    {
                        _context.TreatmentRecords.Remove(record);
                        _context.SaveChanges();
                        LoadTreatmentRecords();
                        ClearInputs();
                        MessageBox.Show("تم حذف السجل بنجاح", "نجاح", MessageBoxButtons.OK, MessageBoxIcon.Information);
                    }
                }
            }
        }

        private void TreatmentGrid_SelectionChanged(object? sender, EventArgs e)
        {
            if (_treatmentGrid.SelectedRows.Count > 0)
            {
                var selectedRow = _treatmentGrid.SelectedRows[0];
                _selectedRecordId = (int)selectedRow.Cells["Id"].Value;
                _deleteButton.Enabled = true;
            }
            else
            {
                _selectedRecordId = -1;
                _deleteButton.Enabled = false;
            }
        }

        private void ClearInputs()
        {
            _cycleComboBox.SelectedIndex = -1;
            _pondComboBox.DataSource = null;
            _treatmentNameTextBox.Clear();
            _dosageTextBox.Clear();
            _datePicker.Value = DateTime.Now;
            _withdrawalTextBox.Clear();
            _notesTextBox.Clear();
            _recordedByTextBox.Text = Environment.UserName;
            _selectedRecordId = -1;
            _deleteButton.Enabled = false;
        }
    }
}
