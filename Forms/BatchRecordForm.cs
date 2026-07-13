using System;
using System.Drawing;
using System.Linq;
using System.Windows.Forms;
using Microsoft.EntityFrameworkCore;
using FishFarmManager.Data;
using FishFarmManager.Models;

namespace FishFarmManager.Forms
{
    public class BatchRecordForm : Form
    {
        private readonly FishFarmContext _context;
        private DataGridView _batchGrid = null!;
        private TextBox _typeTextBox = null!;
        private TextBox _sourceTextBox = null!;
        private TextBox _quantityTextBox = null!;
        private TextBox _unitTextBox = null!;
        private DateTimePicker _arrivalDatePicker = null!;
        private ComboBox _cycleComboBox = null!;
        private TextBox _notesTextBox = null!;
        private TextBox _recordedByTextBox = null!;
        private Button _addButton = null!;
        private Button _deleteButton = null!;
        private int _selectedRecordId = -1;

        public BatchRecordForm(FishFarmContext context)
        {
            _context = context;
            InitializeComponent();
            LoadBatchRecords();
            LoadCycles();
        }

        private void InitializeComponent()
        {
            this.Text = "سجل الدفعات";
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
            _batchGrid = new DataGridView();
            _batchGrid.Dock = DockStyle.Fill;
            _batchGrid.AutoGenerateColumns = false;
            _batchGrid.SelectionMode = DataGridViewSelectionMode.FullRowSelect;
            _batchGrid.MultiSelect = false;
            _batchGrid.ReadOnly = true;
            _batchGrid.AllowUserToAddRows = false;
            _batchGrid.Columns.Add(new DataGridViewTextBoxColumn { DataPropertyName = "Id", Name = "Id", HeaderText = "الرقم", Width = 50 });
            _batchGrid.Columns.Add(new DataGridViewTextBoxColumn { DataPropertyName = "BatchType", HeaderText = "نوع الدفعة", Width = 100 });
            _batchGrid.Columns.Add(new DataGridViewTextBoxColumn { DataPropertyName = "Source", HeaderText = "المصدر", Width = 100 });
            _batchGrid.Columns.Add(new DataGridViewTextBoxColumn { DataPropertyName = "Quantity", HeaderText = "الكمية", Width = 80 });
            _batchGrid.Columns.Add(new DataGridViewTextBoxColumn { DataPropertyName = "Unit", HeaderText = "الوحدة", Width = 60 });
            _batchGrid.Columns.Add(new DataGridViewTextBoxColumn { DataPropertyName = "ArrivalDate", HeaderText = "تاريخ الوصول", Width = 100, DefaultCellStyle = new DataGridViewCellStyle { Format = "yyyy/MM/dd" } });
            _batchGrid.Columns.Add(new DataGridViewTextBoxColumn { DataPropertyName = "CycleName", HeaderText = "الدورة المرتبطة", Width = 100 });
            _batchGrid.Columns.Add(new DataGridViewTextBoxColumn { DataPropertyName = "RecordedBy", HeaderText = "المسجل", Width = 80 });
            _batchGrid.SelectionChanged += BatchGrid_SelectionChanged;
            mainPanel.Controls.Add(_batchGrid, 0, 0);

            // لوحة الإدخال
            var inputPanel = new Panel();
            inputPanel.Dock = DockStyle.Fill;
            inputPanel.Padding = new Padding(10);
            int y = 10, spacing = 35, labelWidth = 90, controlWidth = 150;

            // نوع الدفعة
            var typeLabel = new Label { Text = "نوع الدفعة:", Location = new Point(10, y), Size = new Size(labelWidth, 20) };
            _typeTextBox = new TextBox { Location = new Point(110, y), Size = new Size(controlWidth, 20) };
            inputPanel.Controls.Add(typeLabel);
            inputPanel.Controls.Add(_typeTextBox);
            y += spacing;

            // المصدر
            var sourceLabel = new Label { Text = "المصدر:", Location = new Point(10, y), Size = new Size(labelWidth, 20) };
            _sourceTextBox = new TextBox { Location = new Point(110, y), Size = new Size(controlWidth, 20) };
            inputPanel.Controls.Add(sourceLabel);
            inputPanel.Controls.Add(_sourceTextBox);
            y += spacing;

            // الكمية
            var quantityLabel = new Label { Text = "الكمية:", Location = new Point(10, y), Size = new Size(labelWidth, 20) };
            _quantityTextBox = new TextBox { Location = new Point(110, y), Size = new Size(controlWidth, 20) };
            inputPanel.Controls.Add(quantityLabel);
            inputPanel.Controls.Add(_quantityTextBox);
            y += spacing;

            // الوحدة
            var unitLabel = new Label { Text = "الوحدة:", Location = new Point(10, y), Size = new Size(labelWidth, 20) };
            _unitTextBox = new TextBox { Location = new Point(110, y), Size = new Size(controlWidth, 20) };
            inputPanel.Controls.Add(unitLabel);
            inputPanel.Controls.Add(_unitTextBox);
            y += spacing;

            // تاريخ الوصول
            var arrivalDateLabel = new Label { Text = "تاريخ الوصول:", Location = new Point(10, y), Size = new Size(labelWidth, 20) };
            _arrivalDatePicker = new DateTimePicker { Location = new Point(110, y), Size = new Size(controlWidth, 20) };
            inputPanel.Controls.Add(arrivalDateLabel);
            inputPanel.Controls.Add(_arrivalDatePicker);
            y += spacing;

            // الدورة المرتبطة
            var cycleLabel = new Label { Text = "الدورة المرتبطة:", Location = new Point(10, y), Size = new Size(labelWidth, 20) };
            _cycleComboBox = new ComboBox { Location = new Point(110, y), Size = new Size(controlWidth, 20), DropDownStyle = ComboBoxStyle.DropDownList };
            inputPanel.Controls.Add(cycleLabel);
            inputPanel.Controls.Add(_cycleComboBox);
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

        private void LoadBatchRecords()
        {
            var records = _context.BatchRecords
                .Include(b => b.RelatedCycle)
                .OrderByDescending(b => b.ArrivalDate)
                .Select(b => new
                {
                    b.Id,
                    b.BatchType,
                    b.Source,
                    b.Quantity,
                    b.Unit,
                    b.ArrivalDate,
                    CycleName = b.RelatedCycle != null ? b.RelatedCycle.Name : "-",
                    b.RecordedBy
                })
                .ToList();
            _batchGrid.DataSource = records;
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

        private void AddButton_Click(object? sender, EventArgs e)
        {
            if (string.IsNullOrWhiteSpace(_typeTextBox.Text))
            {
                MessageBox.Show("يرجى تعبئة جميع الحقول الأساسية", "خطأ", MessageBoxButtons.OK, MessageBoxIcon.Error);
                return;
            }
            var record = new BatchRecord
            {
                BatchType = _typeTextBox.Text.Trim(),
                Source = _sourceTextBox.Text.Trim(),
                Quantity = double.TryParse(_quantityTextBox.Text, out var q) ? q : (double?)null,
                Unit = _unitTextBox.Text.Trim(),
                ArrivalDate = _arrivalDatePicker.Value.Date,
                RelatedCycleId = _cycleComboBox.SelectedIndex >= 0 ? (int?)_cycleComboBox.SelectedValue : null,
                Notes = _notesTextBox.Text.Trim(),
                RecordedBy = _recordedByTextBox.Text.Trim(),
                CreatedAt = DateTime.Now
            };
            _context.BatchRecords.Add(record);
            _context.SaveChanges();
            LoadBatchRecords();
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
                    var record = _context.BatchRecords.Find(_selectedRecordId);
                    if (record != null)
                    {
                        _context.BatchRecords.Remove(record);
                        _context.SaveChanges();
                        LoadBatchRecords();
                        ClearInputs();
                        MessageBox.Show("تم حذف السجل بنجاح", "نجاح", MessageBoxButtons.OK, MessageBoxIcon.Information);
                    }
                }
            }
        }

        private void BatchGrid_SelectionChanged(object? sender, EventArgs e)
        {
            if (_batchGrid != null && _batchGrid.SelectedRows.Count > 0)
            {
                var selectedRow = _batchGrid.SelectedRows[0];
                if (selectedRow.Cells["Id"].Value != null)
                    _selectedRecordId = (int)selectedRow.Cells["Id"].Value;
                _deleteButton!.Enabled = true;
            }
            else
            {
                _selectedRecordId = -1;
                if (_deleteButton != null) _deleteButton.Enabled = false;
            }
        }

        private void ClearInputs()
        {
            _typeTextBox.Clear();
            _sourceTextBox.Clear();
            _quantityTextBox.Clear();
            _unitTextBox.Clear();
            _arrivalDatePicker.Value = DateTime.Now;
            _cycleComboBox.SelectedIndex = -1;
            _notesTextBox.Clear();
            _recordedByTextBox.Text = Environment.UserName;
            _selectedRecordId = -1;
            _deleteButton.Enabled = false;
        }
    }
}
