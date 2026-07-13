using System;
using System.Drawing;
using System.Linq;
using System.Windows.Forms;
using Microsoft.EntityFrameworkCore;
using FishFarmManager.Data;
using FishFarmManager.Models;

namespace FishFarmManager.Forms
{
    public class EnvironmentalRecordForm : Form
    {
        private readonly FishFarmContext _context;
        private DataGridView _envGrid = null!;
        private ComboBox _cycleComboBox = null!;
        private ComboBox _pondComboBox = null!;
        private DateTimePicker _datePicker = null!;
        private TextBox _parameterTextBox = null!;
        private TextBox _valueTextBox = null!;
        private TextBox _unitTextBox = null!;
        private TextBox _notesTextBox = null!;
        private TextBox _recordedByTextBox = null!;
        private Button _addButton = null!;
        private Button _deleteButton = null!;
        private int _selectedRecordId = -1;

        public EnvironmentalRecordForm(FishFarmContext context)
        {
            _context = context;
            InitializeComponent();
            LoadEnvironmentalRecords();
            LoadCycles();
        }

        private void InitializeComponent()
        {
            this.Text = "سجل التتبع البيئي";
            this.Size = new Size(900, 600);
            this.StartPosition = FormStartPosition.CenterScreen;
            this.RightToLeft = RightToLeft.Yes;
            this.RightToLeftLayout = true;
            try { this.Font = new Font("Cairo", 10F, FontStyle.Regular); } catch { }

            var mainPanel = new TableLayoutPanel();
            mainPanel.Dock = DockStyle.Fill;
            mainPanel.ColumnCount = 2;
            mainPanel.RowCount = 1;
            mainPanel.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 60F));
            mainPanel.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 40F));

            // جدول البيانات
            _envGrid = new DataGridView();
            _envGrid.Dock = DockStyle.Fill;
            _envGrid.AutoGenerateColumns = false;
            _envGrid.SelectionMode = DataGridViewSelectionMode.FullRowSelect;
            _envGrid.MultiSelect = false;
            _envGrid.ReadOnly = true;
            _envGrid.AllowUserToAddRows = false;
            _envGrid.Columns.Add(new DataGridViewTextBoxColumn { DataPropertyName = "Id", Name = "Id", HeaderText = "الرقم", Width = 50 });
            _envGrid.Columns.Add(new DataGridViewTextBoxColumn { DataPropertyName = "CycleName", HeaderText = "الدورة", Width = 100 });
            _envGrid.Columns.Add(new DataGridViewTextBoxColumn { DataPropertyName = "PondName", HeaderText = "الحوض", Width = 100 });
            _envGrid.Columns.Add(new DataGridViewTextBoxColumn { DataPropertyName = "Date", HeaderText = "التاريخ", Width = 100, DefaultCellStyle = new DataGridViewCellStyle { Format = "yyyy/MM/dd" } });
            _envGrid.Columns.Add(new DataGridViewTextBoxColumn { DataPropertyName = "Parameter", HeaderText = "نوع القياس", Width = 120 });
            _envGrid.Columns.Add(new DataGridViewTextBoxColumn { DataPropertyName = "Value", HeaderText = "القيمة", Width = 80 });
            _envGrid.Columns.Add(new DataGridViewTextBoxColumn { DataPropertyName = "Unit", HeaderText = "الوحدة", Width = 60 });
            _envGrid.Columns.Add(new DataGridViewTextBoxColumn { DataPropertyName = "RecordedBy", HeaderText = "المسجل", Width = 80 });
            _envGrid.SelectionChanged += EnvGrid_SelectionChanged;
            mainPanel.Controls.Add(_envGrid, 0, 0);

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

            // التاريخ
            var dateLabel = new Label { Text = "التاريخ:", Location = new Point(10, y), Size = new Size(labelWidth, 20) };
            _datePicker = new DateTimePicker { Location = new Point(110, y), Size = new Size(controlWidth, 20) };
            inputPanel.Controls.Add(dateLabel);
            inputPanel.Controls.Add(_datePicker);
            y += spacing;

            // نوع القياس
            var paramLabel = new Label { Text = "نوع القياس:", Location = new Point(10, y), Size = new Size(labelWidth, 20) };
            _parameterTextBox = new TextBox { Location = new Point(110, y), Size = new Size(controlWidth, 20) };
            inputPanel.Controls.Add(paramLabel);
            inputPanel.Controls.Add(_parameterTextBox);
            y += spacing;

            // القيمة
            var valueLabel = new Label { Text = "القيمة:", Location = new Point(10, y), Size = new Size(labelWidth, 20) };
            _valueTextBox = new TextBox { Location = new Point(110, y), Size = new Size(controlWidth, 20) };
            inputPanel.Controls.Add(valueLabel);
            inputPanel.Controls.Add(_valueTextBox);
            y += spacing;

            // الوحدة
            var unitLabel = new Label { Text = "الوحدة:", Location = new Point(10, y), Size = new Size(labelWidth, 20) };
            _unitTextBox = new TextBox { Location = new Point(110, y), Size = new Size(controlWidth, 20) };
            inputPanel.Controls.Add(unitLabel);
            inputPanel.Controls.Add(_unitTextBox);
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

        private void LoadEnvironmentalRecords()
        {
            var records = _context.EnvironmentalRecords
                .Include(e => e.Cycle)
                .Include(e => e.Pond)
                .OrderByDescending(e => e.Date)
                .Select(e => new
                {
                    e.Id,
                    CycleName = e.Cycle != null ? e.Cycle.Name : "-",
                    PondName = e.Pond != null ? e.Pond.Name : "-",
                    e.Date,
                    e.Parameter,
                    e.Value,
                    e.Unit,
                    e.RecordedBy
                })
                .ToList();
            _envGrid.DataSource = records;
        }

        private void LoadCycles()
        {
            var cycles = _context.ProductionCycles
                .Where(c => c.Status == CycleStatus.Active || c.Status == CycleStatus.Planning)
                .Select(c => new { c.Id, c.Name }).ToList();
            _cycleComboBox.DataSource = cycles;
            _cycleComboBox.DisplayMember = "Name";
            _cycleComboBox.ValueMember = "Id";
            _cycleComboBox.SelectedIndex = -1;
        }

        private void CycleComboBox_SelectedIndexChanged(object? sender, EventArgs e)
        {
            if (_cycleComboBox.SelectedIndex >= 0)
            {
                int cycleId = _cycleComboBox.SelectedValue is int id ? id : 0;
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
            if (_cycleComboBox.SelectedIndex == -1 || string.IsNullOrWhiteSpace(_parameterTextBox.Text) || string.IsNullOrWhiteSpace(_valueTextBox.Text))
            {
                MessageBox.Show("يرجى تعبئة جميع الحقول الأساسية", "خطأ", MessageBoxButtons.OK, MessageBoxIcon.Error);
                return;
            }
            var record = new EnvironmentalRecord
            {
                CycleId = _cycleComboBox.SelectedValue is int cycleId ? cycleId : 0,
                PondId = _pondComboBox.SelectedIndex >= 0 && _pondComboBox.SelectedValue is int pondId ? pondId : 0,
                Date = _datePicker.Value.Date,
                Parameter = _parameterTextBox.Text.Trim(),
                Value = decimal.TryParse(_valueTextBox.Text.Trim(), out var val) ? (decimal?)val : null,
                Unit = _unitTextBox.Text.Trim(),
                Notes = _notesTextBox.Text.Trim(),
                RecordedBy = _recordedByTextBox.Text.Trim(),
                CreatedAt = DateTime.Now
            };
            _context.EnvironmentalRecords.Add(record);
            _context.SaveChanges();
            LoadEnvironmentalRecords();
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
                    var record = _context.EnvironmentalRecords.Find(_selectedRecordId);
                    if (record != null)
                    {
                        _context.EnvironmentalRecords.Remove(record);
                        _context.SaveChanges();
                        LoadEnvironmentalRecords();
                        ClearInputs();
                        MessageBox.Show("تم حذف السجل بنجاح", "نجاح", MessageBoxButtons.OK, MessageBoxIcon.Information);
                    }
                }
            }
        }

        private void EnvGrid_SelectionChanged(object? sender, EventArgs e)
        {
            if (_envGrid.SelectedRows.Count > 0)
            {
                var selectedRow = _envGrid.SelectedRows[0];
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
            _datePicker.Value = DateTime.Now;
            _parameterTextBox.Clear();
            _valueTextBox.Clear();
            _unitTextBox.Clear();
            _notesTextBox.Clear();
            _recordedByTextBox.Text = Environment.UserName;
            _selectedRecordId = -1;
            _deleteButton.Enabled = false;
        }
    }
}
