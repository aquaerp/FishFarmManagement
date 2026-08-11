using System;
using System.Drawing;
using System.Linq;
using System.Windows.Forms;
using Microsoft.EntityFrameworkCore;
using FishFarmManager.Data;
using FishFarmManager.Models;

namespace FishFarmManager.Forms
{
    public class CertificationRecordForm : Form
    {
        private readonly FishFarmContext _context;
        private DataGridView _certGrid = null!;
        private TextBox _nameTextBox = null!;
        private TextBox _issuerTextBox = null!;
        private DateTimePicker _issueDatePicker = null!;
        private DateTimePicker _expiryDatePicker = null!;
        private TextBox _numberTextBox = null!;
        private TextBox _notesTextBox = null!;
        private TextBox _recordedByTextBox = null!;
        private ComboBox _certificationComboBox = null!;
        private Button _addButton = null!;
        private Button _deleteButton = null!;
        private int _selectedRecordId = -1;

        public CertificationRecordForm(FishFarmContext context)
        {
            _context = context;
            InitializeComponent();
            LoadCertifications();
            LoadCertificationRecords();
        }

        private void LoadCertifications()
        {
            var certifications = _context.Certifications.AsNoTracking()
                .OrderBy(value => value.Name)
                .Select(value => new { value.Id, Name = value.Name })
                .ToList();
            _certificationComboBox.DataSource = certifications;
            _certificationComboBox.DisplayMember = "Name";
            _certificationComboBox.ValueMember = "Id";
        }

        private void InitializeComponent()
        {
            this.Text = "سجل الشهادات";
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
            _certGrid = new DataGridView();
            _certGrid.Dock = DockStyle.Fill;
            _certGrid.AutoGenerateColumns = false;
            _certGrid.SelectionMode = DataGridViewSelectionMode.FullRowSelect;
            _certGrid.MultiSelect = false;
            _certGrid.ReadOnly = true;
            _certGrid.AllowUserToAddRows = false;
            _certGrid.Columns.Add(new DataGridViewTextBoxColumn { DataPropertyName = "Id", Name = "Id", HeaderText = "الرقم", Width = 50 });
            _certGrid.Columns.Add(new DataGridViewTextBoxColumn { DataPropertyName = "CertificateName", HeaderText = "اسم الشهادة", Width = 120 });
            _certGrid.Columns.Add(new DataGridViewTextBoxColumn { DataPropertyName = "Issuer", HeaderText = "الجهة المانحة", Width = 100 });
            _certGrid.Columns.Add(new DataGridViewTextBoxColumn { DataPropertyName = "IssueDate", HeaderText = "تاريخ الإصدار", Width = 100, DefaultCellStyle = new DataGridViewCellStyle { Format = "yyyy/MM/dd" } });
            _certGrid.Columns.Add(new DataGridViewTextBoxColumn { DataPropertyName = "ExpiryDate", HeaderText = "تاريخ الانتهاء", Width = 100, DefaultCellStyle = new DataGridViewCellStyle { Format = "yyyy/MM/dd" } });
            _certGrid.Columns.Add(new DataGridViewTextBoxColumn { DataPropertyName = "CertificateNumber", HeaderText = "رقم الشهادة", Width = 100 });
            _certGrid.Columns.Add(new DataGridViewTextBoxColumn { DataPropertyName = "RecordedBy", HeaderText = "المسجل", Width = 80 });
            _certGrid.SelectionChanged += CertGrid_SelectionChanged;
            mainPanel.Controls.Add(_certGrid, 0, 0);

            // لوحة الإدخال
            var inputPanel = new Panel();
            inputPanel.Dock = DockStyle.Fill;
            inputPanel.Padding = new Padding(10);
            int y = 10, spacing = 35, labelWidth = 90, controlWidth = 150;

            // اسم الشهادة
            var nameLabel = new Label { Text = "اسم الشهادة:", Location = new Point(10, y), Size = new Size(labelWidth, 20) };
            _nameTextBox = new TextBox { Location = new Point(110, y), Size = new Size(controlWidth, 20) };
            inputPanel.Controls.Add(nameLabel);
            inputPanel.Controls.Add(_nameTextBox);
            var certificationLabel = new Label { Text = "نوع الشهادة:", Location = new Point(10, y + spacing), Size = new Size(labelWidth, 20) };
            _certificationComboBox = new ComboBox { Location = new Point(110, y + spacing), Size = new Size(controlWidth, 20), DropDownStyle = ComboBoxStyle.DropDownList };
            inputPanel.Controls.Add(certificationLabel);
            inputPanel.Controls.Add(_certificationComboBox);
            y += spacing;
            y += spacing;

            // الجهة المانحة
            var issuerLabel = new Label { Text = "الجهة المانحة:", Location = new Point(10, y), Size = new Size(labelWidth, 20) };
            _issuerTextBox = new TextBox { Location = new Point(110, y), Size = new Size(controlWidth, 20) };
            inputPanel.Controls.Add(issuerLabel);
            inputPanel.Controls.Add(_issuerTextBox);
            y += spacing;

            // تاريخ الإصدار
            var issueDateLabel = new Label { Text = "تاريخ الإصدار:", Location = new Point(10, y), Size = new Size(labelWidth, 20) };
            _issueDatePicker = new DateTimePicker { Location = new Point(110, y), Size = new Size(controlWidth, 20) };
            inputPanel.Controls.Add(issueDateLabel);
            inputPanel.Controls.Add(_issueDatePicker);
            y += spacing;

            // تاريخ الانتهاء
            var expiryDateLabel = new Label { Text = "تاريخ الانتهاء:", Location = new Point(10, y), Size = new Size(labelWidth, 20) };
            _expiryDatePicker = new DateTimePicker { Location = new Point(110, y), Size = new Size(controlWidth, 20), ShowCheckBox = true };
            inputPanel.Controls.Add(expiryDateLabel);
            inputPanel.Controls.Add(_expiryDatePicker);
            y += spacing;

            // رقم الشهادة
            var numberLabel = new Label { Text = "رقم الشهادة:", Location = new Point(10, y), Size = new Size(labelWidth, 20) };
            _numberTextBox = new TextBox { Location = new Point(110, y), Size = new Size(controlWidth, 20) };
            inputPanel.Controls.Add(numberLabel);
            inputPanel.Controls.Add(_numberTextBox);
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

        private void LoadCertificationRecords()
        {
            var records = _context.CertificationRecords
                .OrderByDescending(c => c.IssueDate)
                .Select(c => new
                {
                    c.Id,
                    c.CertificateName,
                    c.Issuer,
                    c.IssueDate,
                    c.ExpiryDate,
                    c.CertificateNumber,
                    c.RecordedBy
                })
                .ToList();
            _certGrid.DataSource = records;
        }

        private void AddButton_Click(object? sender, EventArgs e)
        {
            if (string.IsNullOrWhiteSpace(_nameTextBox.Text) || string.IsNullOrWhiteSpace(_issuerTextBox.Text))
            {
                MessageBox.Show("يرجى تعبئة جميع الحقول الأساسية", "خطأ", MessageBoxButtons.OK, MessageBoxIcon.Error);
                return;
            }

            if (_certificationComboBox.SelectedValue is not int certificationId || certificationId <= 0)
            {
                MessageBox.Show("الرجاء اختيار نوع الشهادة المرتبط بالسجل", "خطأ", MessageBoxButtons.OK, MessageBoxIcon.Error);
                return;
            }

            var record = new CertificationRecord
            {
                CertificationId = certificationId,
                RecordDate = DateTime.Now,
                RecordType = "CertificationRecord",
                CertificateName = _nameTextBox.Text.Trim(),
                Issuer = _issuerTextBox.Text.Trim(),
                IssueDate = _issueDatePicker.Value.Date,
                ExpiryDate = _expiryDatePicker.Checked ? _expiryDatePicker.Value.Date : null,
                CertificateNumber = _numberTextBox.Text.Trim(),
                Notes = _notesTextBox.Text.Trim(),
                RecordedBy = Services.AuthenticationService.CurrentUser?.EmployeeId,
                CreatedAt = DateTime.Now
            };
            _context.CertificationRecords.Add(record);
            _context.SaveChanges();
            LoadCertificationRecords();
            ClearInputs();
            MessageBox.Show("تمت إضافة سجل الشهادة بنجاح", "نجاح", MessageBoxButtons.OK, MessageBoxIcon.Information);
        }

        private void DeleteButton_Click(object? sender, EventArgs e)
        {
            if (_selectedRecordId > 0)
            {
                var result = MessageBox.Show("هل أنت متأكد من حذف السجل؟", "تأكيد الحذف", MessageBoxButtons.YesNo, MessageBoxIcon.Question);
                if (result == DialogResult.Yes)
                {
                    var record = _context.CertificationRecords.Find(_selectedRecordId);
                    if (record != null)
                    {
                        _context.CertificationRecords.Remove(record);
                        _context.SaveChanges();
                        LoadCertificationRecords();
                        ClearInputs();
                        MessageBox.Show("تم حذف السجل بنجاح", "نجاح", MessageBoxButtons.OK, MessageBoxIcon.Information);
                    }
                }
            }
        }

        private void CertGrid_SelectionChanged(object? sender, EventArgs e)
        {
            if (_certGrid.SelectedRows.Count > 0)
            {
                var selectedRow = _certGrid.SelectedRows[0];
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
            _nameTextBox.Clear();
            _issuerTextBox.Clear();
            _issueDatePicker.Value = DateTime.Now;
            _expiryDatePicker.Value = DateTime.Now;
            _expiryDatePicker.Checked = false;
            _numberTextBox.Clear();
            _notesTextBox.Clear();
            _recordedByTextBox.Text = Environment.UserName;
            _selectedRecordId = -1;
            _deleteButton.Enabled = false;
        }
    }
}
