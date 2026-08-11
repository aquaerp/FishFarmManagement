using System;
using System.Drawing;
using System.Linq;
using System.Windows.Forms;
using Microsoft.EntityFrameworkCore;
using FishFarmManager.Data;
using FishFarmManager.Models;

namespace FishFarmManager.Forms
{
    public class StaffRecordForm : Form
    {
        private readonly FishFarmContext _context;
        private DataGridView _staffGrid = null!;
        private TextBox _nameTextBox = null!;
        private TextBox _roleTextBox = null!;
        private DateTimePicker _hireDatePicker = null!;
        private TextBox _trainingTextBox = null!;
        private TextBox _certificateTextBox = null!;
        private TextBox _notesTextBox = null!;
        private TextBox _recordedByTextBox = null!;
        private ComboBox _employeeComboBox = null!;
        private Button _addButton = null!;
        private Button _deleteButton = null!;
        private int _selectedRecordId = -1;

        public StaffRecordForm(FishFarmContext context)
        {
            _context = context;
            InitializeComponent();
            LoadEmployees();
            LoadStaffRecords();
        }

        private void LoadEmployees()
        {
            var employees = _context.Employees.AsNoTracking()
                .Where(value => value.Status == EmployeeStatus.Active)
                .OrderBy(value => value.Name)
                .Select(value => new { value.Id, value.Name })
                .ToList();
            _employeeComboBox.DataSource = employees;
            _employeeComboBox.DisplayMember = "Name";
            _employeeComboBox.ValueMember = "Id";
        }

        private void InitializeComponent()
        {
            this.Text = "سجل العمال والتدريب";
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
            _staffGrid = new DataGridView();
            _staffGrid.Dock = DockStyle.Fill;
            _staffGrid.AutoGenerateColumns = false;
            _staffGrid.SelectionMode = DataGridViewSelectionMode.FullRowSelect;
            _staffGrid.MultiSelect = false;
            _staffGrid.ReadOnly = true;
            _staffGrid.AllowUserToAddRows = false;
            _staffGrid.Columns.Add(new DataGridViewTextBoxColumn { DataPropertyName = "Id", Name = "Id", HeaderText = "الرقم", Width = 50 });
            _staffGrid.Columns.Add(new DataGridViewTextBoxColumn { DataPropertyName = "Name", HeaderText = "اسم العامل", Width = 120 });
            _staffGrid.Columns.Add(new DataGridViewTextBoxColumn { DataPropertyName = "Role", HeaderText = "الدور/الوظيفة", Width = 100 });
            _staffGrid.Columns.Add(new DataGridViewTextBoxColumn { DataPropertyName = "HireDate", HeaderText = "تاريخ التعيين", Width = 100, DefaultCellStyle = new DataGridViewCellStyle { Format = "yyyy/MM/dd" } });
            _staffGrid.Columns.Add(new DataGridViewTextBoxColumn { DataPropertyName = "Training", HeaderText = "الدورات التدريبية", Width = 120 });
            _staffGrid.Columns.Add(new DataGridViewTextBoxColumn { DataPropertyName = "Certificate", HeaderText = "الشهادات المهنية", Width = 120 });
            _staffGrid.Columns.Add(new DataGridViewTextBoxColumn { DataPropertyName = "RecordedBy", HeaderText = "المسجل", Width = 80 });
            _staffGrid.SelectionChanged += StaffGrid_SelectionChanged;
            mainPanel.Controls.Add(_staffGrid, 0, 0);

            // لوحة الإدخال
            var inputPanel = new Panel();
            inputPanel.Dock = DockStyle.Fill;
            inputPanel.Padding = new Padding(10);
            int y = 10, spacing = 35, labelWidth = 90, controlWidth = 150;

            // اسم العامل
            var nameLabel = new Label { Text = "اسم العامل:", Location = new Point(10, y), Size = new Size(labelWidth, 20) };
            _nameTextBox = new TextBox { Location = new Point(110, y), Size = new Size(controlWidth, 20) };
            inputPanel.Controls.Add(nameLabel);
            inputPanel.Controls.Add(_nameTextBox);
            var employeeLabel = new Label { Text = "الموظف المرتبط:", Location = new Point(10, y + spacing), Size = new Size(labelWidth, 20) };
            _employeeComboBox = new ComboBox { Location = new Point(110, y + spacing), Size = new Size(controlWidth, 20), DropDownStyle = ComboBoxStyle.DropDownList };
            inputPanel.Controls.Add(employeeLabel);
            inputPanel.Controls.Add(_employeeComboBox);
            y += spacing;
            y += spacing;

            // الدور/الوظيفة
            var roleLabel = new Label { Text = "الدور/الوظيفة:", Location = new Point(10, y), Size = new Size(labelWidth, 20) };
            _roleTextBox = new TextBox { Location = new Point(110, y), Size = new Size(controlWidth, 20) };
            inputPanel.Controls.Add(roleLabel);
            inputPanel.Controls.Add(_roleTextBox);
            y += spacing;

            // تاريخ التعيين
            var hireDateLabel = new Label { Text = "تاريخ التعيين:", Location = new Point(10, y), Size = new Size(labelWidth, 20) };
            _hireDatePicker = new DateTimePicker { Location = new Point(110, y), Size = new Size(controlWidth, 20) };
            inputPanel.Controls.Add(hireDateLabel);
            inputPanel.Controls.Add(_hireDatePicker);
            y += spacing;

            // الدورات التدريبية
            var trainingLabel = new Label { Text = "الدورات التدريبية:", Location = new Point(10, y), Size = new Size(labelWidth, 20) };
            _trainingTextBox = new TextBox { Location = new Point(110, y), Size = new Size(controlWidth, 20) };
            inputPanel.Controls.Add(trainingLabel);
            inputPanel.Controls.Add(_trainingTextBox);
            y += spacing;

            // الشهادات المهنية
            var certLabel = new Label { Text = "الشهادات المهنية:", Location = new Point(10, y), Size = new Size(labelWidth, 20) };
            _certificateTextBox = new TextBox { Location = new Point(110, y), Size = new Size(controlWidth, 20) };
            inputPanel.Controls.Add(certLabel);
            inputPanel.Controls.Add(_certificateTextBox);
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

        private void LoadStaffRecords()
        {
            var records = _context.StaffRecords
                .OrderByDescending(s => s.HireDate)
                .Select(s => new
                {
                    s.Id,
                    s.Name,
                    s.Role,
                    s.HireDate,
                    s.Training,
                    s.Certificate,
                    s.RecordedBy
                })
                .ToList();
            _staffGrid.DataSource = records;
        }

        private void AddButton_Click(object? sender, EventArgs e)
        {
            if (string.IsNullOrWhiteSpace(_nameTextBox.Text))
            {
                MessageBox.Show("يرجى تعبئة جميع الحقول الأساسية", "خطأ", MessageBoxButtons.OK, MessageBoxIcon.Error);
                return;
            }

            if (_employeeComboBox.SelectedValue is not int employeeId || employeeId <= 0)
            {
                MessageBox.Show("الرجاء اختيار الموظف المرتبط بالسجل", "خطأ", MessageBoxButtons.OK, MessageBoxIcon.Error);
                return;
            }

            var employee = _context.Employees.Find(employeeId);
            if (employee == null)
                return;
            var record = new StaffRecord
            {
                EmployeeId = employee.Id,
                RecordType = "StaffRecord",
                RecordDate = DateTime.Now,
                Name = _nameTextBox.Text.Trim(),
                Role = _roleTextBox.Text.Trim(),
                HireDate = _hireDatePicker.Value.Date,
                Training = _trainingTextBox.Text.Trim(),
                Certificate = _certificateTextBox.Text.Trim(),
                Notes = _notesTextBox.Text.Trim(),
                RecordedBy = Services.AuthenticationService.CurrentUsername,
                CreatedAt = DateTime.Now
            };
            _context.StaffRecords.Add(record);
            _context.SaveChanges();
            LoadStaffRecords();
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
                    var record = _context.StaffRecords.Find(_selectedRecordId);
                    if (record != null)
                    {
                        _context.StaffRecords.Remove(record);
                        _context.SaveChanges();
                        LoadStaffRecords();
                        ClearInputs();
                        MessageBox.Show("تم حذف السجل بنجاح", "نجاح", MessageBoxButtons.OK, MessageBoxIcon.Information);
                    }
                }
            }
        }

        private void StaffGrid_SelectionChanged(object? sender, EventArgs e)
        {
            if (_staffGrid.SelectedRows.Count > 0)
            {
                var selectedRow = _staffGrid.SelectedRows[0];
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
            _roleTextBox.Clear();
            _hireDatePicker.Value = DateTime.Now;
            _trainingTextBox.Clear();
            _certificateTextBox.Clear();
            _notesTextBox.Clear();
            _recordedByTextBox.Text = Environment.UserName;
            _selectedRecordId = -1;
            _deleteButton.Enabled = false;
        }
    }
}
