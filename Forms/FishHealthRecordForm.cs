using System;
using System.Drawing;
using System.Linq;
using System.Windows.Forms;
using Microsoft.EntityFrameworkCore;
using FishFarmManager.Data;
using FishFarmManager.Models;

namespace FishFarmManager.Forms
{
    public class FishHealthRecordForm : Form
    {
        private readonly FishFarmContext _context;
        private DataGridView _healthGrid = null!;
        private ComboBox _cycleComboBox = null!;
        private ComboBox _pondComboBox = null!;
        private DateTimePicker _datePicker = null!;
        private ComboBox _statusComboBox = null!;
        private TextBox _diseaseTextBox = null!;
        private TextBox _actionTextBox = null!;
        private TextBox _notesTextBox = null!;
        private TextBox _recordedByTextBox = null!;
        private Button _addButton = null!;
        private Button _deleteButton = null!;
        private int _selectedRecordId = -1;

        public FishHealthRecordForm(FishFarmContext context)
        {
            _context = context;
            InitializeComponent();
            LoadHealthRecords();
            LoadCycles();
        }

        private void InitializeComponent()
        {
            this.Text = "سجل الصحة الحيوانية";
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
            _healthGrid = new DataGridView();
            _healthGrid.Dock = DockStyle.Fill;
            _healthGrid.AutoGenerateColumns = false;
            _healthGrid.SelectionMode = DataGridViewSelectionMode.FullRowSelect;
            _healthGrid.MultiSelect = false;
            _healthGrid.ReadOnly = true;
            _healthGrid.AllowUserToAddRows = false;
            _healthGrid.Columns.Add(new DataGridViewTextBoxColumn { DataPropertyName = "Id", Name = "Id", HeaderText = "الرقم", Width = 50 });
            _healthGrid.Columns.Add(new DataGridViewTextBoxColumn { DataPropertyName = "CycleName", HeaderText = "الدورة", Width = 100 });
            _healthGrid.Columns.Add(new DataGridViewTextBoxColumn { DataPropertyName = "PondName", HeaderText = "الحوض", Width = 100 });
            _healthGrid.Columns.Add(new DataGridViewTextBoxColumn { DataPropertyName = "Date", HeaderText = "التاريخ", Width = 100, DefaultCellStyle = new DataGridViewCellStyle { Format = "yyyy/MM/dd" } });
            _healthGrid.Columns.Add(new DataGridViewTextBoxColumn { DataPropertyName = "HealthStatus", HeaderText = "الحالة الصحية", Width = 100 });
            _healthGrid.Columns.Add(new DataGridViewTextBoxColumn { DataPropertyName = "Disease", HeaderText = "المرض", Width = 100 });
            _healthGrid.Columns.Add(new DataGridViewTextBoxColumn { DataPropertyName = "ActionTaken", HeaderText = "الإجراء", Width = 120 });
            _healthGrid.Columns.Add(new DataGridViewTextBoxColumn { DataPropertyName = "RecordedBy", HeaderText = "المسجل", Width = 80 });
            _healthGrid.SelectionChanged += HealthGrid_SelectionChanged;
            mainPanel.Controls.Add(_healthGrid, 0, 0);

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

            // الحالة الصحية
            var statusLabel = new Label { Text = "الحالة الصحية:", Location = new Point(10, y), Size = new Size(labelWidth, 20) };
            _statusComboBox = new ComboBox { Location = new Point(110, y), Size = new Size(controlWidth, 20), DropDownStyle = ComboBoxStyle.DropDownList };
            _statusComboBox.Items.AddRange(new string[] { "سليم", "مريض", "يحتاج متابعة", "حرج" });
            inputPanel.Controls.Add(statusLabel);
            inputPanel.Controls.Add(_statusComboBox);
            y += spacing;

            // المرض
            var diseaseLabel = new Label { Text = "المرض:", Location = new Point(10, y), Size = new Size(labelWidth, 20) };
            _diseaseTextBox = new TextBox { Location = new Point(110, y), Size = new Size(controlWidth, 20) };
            inputPanel.Controls.Add(diseaseLabel);
            inputPanel.Controls.Add(_diseaseTextBox);
            y += spacing;

            // الإجراء
            var actionLabel = new Label { Text = "الإجراء:", Location = new Point(10, y), Size = new Size(labelWidth, 20) };
            _actionTextBox = new TextBox { Location = new Point(110, y), Size = new Size(controlWidth, 40), Multiline = true };
            inputPanel.Controls.Add(actionLabel);
            inputPanel.Controls.Add(_actionTextBox);
            y += 50;

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

        private void LoadHealthRecords()
        {
            var records = _context.FishHealthRecords
                .Include(h => h.Cycle)
                .Include(h => h.Pond)
                .OrderByDescending(h => h.Date)
                .Select(h => new
                {
                    h.Id,
                    CycleName = h.Cycle != null ? h.Cycle.Name : "-",
                    PondName = h.Pond != null ? h.Pond.Name : "-",
                    h.Date,
                    h.HealthStatus,
                    h.Disease,
                    h.ActionTaken,
                    h.RecordedBy
                })
                .ToList();
            _healthGrid.DataSource = records;
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
                    var value = prop.GetValue(item);
                    cycleId = value is int id ? id : 0;
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
            if (_cycleComboBox.SelectedIndex == -1 || _statusComboBox.SelectedIndex == -1)
            {
                MessageBox.Show("يرجى تعبئة جميع الحقول الأساسية", "خطأ", MessageBoxButtons.OK, MessageBoxIcon.Error);
                return;
            }
            var record = new FishHealthRecord
            {
                CycleId = _cycleComboBox.SelectedValue is int cycleId ? cycleId : 0,
                PondId = _pondComboBox.SelectedIndex >= 0 && _pondComboBox.SelectedValue is int pondId ? pondId : 0,
                Date = _datePicker.Value.Date,
                HealthStatus = _statusComboBox.Text,
                Disease = _diseaseTextBox.Text.Trim(),
                ActionTaken = _actionTextBox.Text.Trim(),
                Notes = _notesTextBox.Text.Trim(),
                RecordedBy = _recordedByTextBox.Text.Trim(),
                CreatedAt = DateTime.Now
            };
            _context.FishHealthRecords.Add(record);
            _context.SaveChanges();
            LoadHealthRecords();
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
                    var record = _context.FishHealthRecords.Find(_selectedRecordId);
                    if (record != null)
                    {
                        _context.FishHealthRecords.Remove(record);
                        _context.SaveChanges();
                        LoadHealthRecords();
                        ClearInputs();
                        MessageBox.Show("تم حذف السجل بنجاح", "نجاح", MessageBoxButtons.OK, MessageBoxIcon.Information);
                    }
                }
            }
        }

        private void HealthGrid_SelectionChanged(object? sender, EventArgs e)
        {
            if (_healthGrid.SelectedRows.Count > 0)
            {
                var selectedRow = _healthGrid.SelectedRows[0];
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
            _statusComboBox.SelectedIndex = -1;
            _diseaseTextBox.Clear();
            _actionTextBox.Clear();
            _notesTextBox.Clear();
            _recordedByTextBox.Text = Environment.UserName;
            _selectedRecordId = -1;
            _deleteButton.Enabled = false;
        }
    }
}
