using System;
using FishFarmManager.Services;
using System.Linq;
using System.Windows.Forms;
using System.Drawing;
using FishFarmManager.Data;
using FishFarmManager.Models;
using Microsoft.EntityFrameworkCore;

namespace FishFarmManager.Forms
{
    public class MaintenanceRecordForm : Form
    {
        private readonly FishFarmContext _context;
        private int _selectedRecordId = 0;

        // Controls
        private TextBox recordNumberTextBox = null!;
        private ComboBox equipmentComboBox = null!;
        private DateTimePicker maintenanceDatePicker = null!;
        private ComboBox maintenanceTypeComboBox = null!;
        private TextBox problemDescriptionTextBox = null!;
        private TextBox workPerformedTextBox = null!;
        private TextBox partsReplacedTextBox = null!;
        private NumericUpDown partsCostNumeric = null!;
        private NumericUpDown laborCostNumeric = null!;
        private Label totalCostLabelValue = null!;
        private TextBox performedByTextBox = null!;
        private ComboBox statusComboBox = null!;
        private CheckBox followUpRequiredCheckBox = null!;
        private DateTimePicker followUpDatePicker = null!;
        private TextBox notesTextBox = null!;
        private DataGridView recordsGrid = null!;
        private Button saveButton = null!;
        private Button deleteButton = null!;
        private Button newButton = null!;
        private ComboBox filterEquipmentComboBox = null!;
        private ComboBox filterStatusComboBox = null!;
        private DateTimePicker filterFromDatePicker = null!;
        private DateTimePicker filterToDatePicker = null!;
        private Button applyFilterButton = null!;
        private Label summaryLabel = null!;

        public MaintenanceRecordForm(FishFarmContext context)
        {
            _context = context;
            InitializeComponent();
            LoadEquipment();
            LoadMaintenanceTypes();
            LoadStatuses();
            LoadRecords();
            UpdateSummary();
        }

        private void InitializeComponent()
        {
            // Form setup
            this.Text = "سجلات الصيانة - Maintenance Records";
            this.Size = new Size(1400, 800);
            this.StartPosition = FormStartPosition.CenterScreen;
            this.RightToLeft = RightToLeft.Yes;
            this.RightToLeftLayout = true;

            // Main layout
            var mainLayout = new TableLayoutPanel
            {
                Dock = DockStyle.Fill,
                ColumnCount = 2,
                RowCount = 1,
                Padding = new Padding(10)
            };
            mainLayout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 35F));
            mainLayout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 65F));

            // Left panel - Record Details
            var leftPanel = new Panel
            {
                Dock = DockStyle.Fill,
                AutoScroll = true
            };

            var detailsLayout = new TableLayoutPanel
            {
                Dock = DockStyle.Fill,
                ColumnCount = 1,
                RowCount = 4,
                Padding = new Padding(5)
            };
            detailsLayout.RowStyles.Add(new RowStyle(SizeType.AutoSize));
            detailsLayout.RowStyles.Add(new RowStyle(SizeType.AutoSize));
            detailsLayout.RowStyles.Add(new RowStyle(SizeType.AutoSize));
            detailsLayout.RowStyles.Add(new RowStyle(SizeType.AutoSize));

            // GroupBox 1: Basic Information
            var basicInfoGroup = new GroupBox
            {
                Text = "معلومات أساسية - Basic Information",
                Dock = DockStyle.Fill,
                Padding = new Padding(10),
                AutoSize = true
            };

            var basicInfoLayout = new TableLayoutPanel
            {
                Dock = DockStyle.Fill,
                ColumnCount = 2,
                RowCount = 5,
                AutoSize = true,
                Padding = new Padding(5)
            };
            basicInfoLayout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 40F));
            basicInfoLayout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 60F));

            // Record Number
            var recordNumberLabel = new Label
            {
                Text = "رقم السجل - Record #:",
                Dock = DockStyle.Fill,
                TextAlign = ContentAlignment.MiddleRight,
                Font = new Font(this.Font, FontStyle.Bold)
            };
            recordNumberTextBox = new TextBox
            {
                Dock = DockStyle.Fill
            };

            // Equipment
            var equipmentLabel = new Label
            {
                Text = "المعدة - Equipment:",
                Dock = DockStyle.Fill,
                TextAlign = ContentAlignment.MiddleRight,
                Font = new Font(this.Font, FontStyle.Bold)
            };
            equipmentComboBox = new ComboBox
            {
                Dock = DockStyle.Fill,
                DropDownStyle = ComboBoxStyle.DropDownList,
                DisplayMember = "Name",
                ValueMember = "Id"
            };

            // Maintenance Date
            var maintenanceDateLabel = new Label
            {
                Text = "تاريخ الصيانة - Date:",
                Dock = DockStyle.Fill,
                TextAlign = ContentAlignment.MiddleRight,
                Font = new Font(this.Font, FontStyle.Bold)
            };
            maintenanceDatePicker = new DateTimePicker
            {
                Dock = DockStyle.Fill,
                Format = DateTimePickerFormat.Short
            };

            // Maintenance Type
            var maintenanceTypeLabel = new Label
            {
                Text = "نوع الصيانة - Type:",
                Dock = DockStyle.Fill,
                TextAlign = ContentAlignment.MiddleRight,
                Font = new Font(this.Font, FontStyle.Bold)
            };
            maintenanceTypeComboBox = new ComboBox
            {
                Dock = DockStyle.Fill,
                DropDownStyle = ComboBoxStyle.DropDownList
            };

            // Status
            var statusLabel = new Label
            {
                Text = "الحالة - Status:",
                Dock = DockStyle.Fill,
                TextAlign = ContentAlignment.MiddleRight,
                Font = new Font(this.Font, FontStyle.Bold)
            };
            statusComboBox = new ComboBox
            {
                Dock = DockStyle.Fill,
                DropDownStyle = ComboBoxStyle.DropDownList
            };

            basicInfoLayout.Controls.Add(recordNumberLabel, 0, 0);
            basicInfoLayout.Controls.Add(recordNumberTextBox, 1, 0);
            basicInfoLayout.Controls.Add(equipmentLabel, 0, 1);
            basicInfoLayout.Controls.Add(equipmentComboBox, 1, 1);
            basicInfoLayout.Controls.Add(maintenanceDateLabel, 0, 2);
            basicInfoLayout.Controls.Add(maintenanceDatePicker, 1, 2);
            basicInfoLayout.Controls.Add(maintenanceTypeLabel, 0, 3);
            basicInfoLayout.Controls.Add(maintenanceTypeComboBox, 1, 3);
            basicInfoLayout.Controls.Add(statusLabel, 0, 4);
            basicInfoLayout.Controls.Add(statusComboBox, 1, 4);

            basicInfoGroup.Controls.Add(basicInfoLayout);

            // GroupBox 2: Work Details
            var workDetailsGroup = new GroupBox
            {
                Text = "تفاصيل العمل - Work Details",
                Dock = DockStyle.Fill,
                Padding = new Padding(10),
                AutoSize = true
            };

            var workDetailsLayout = new TableLayoutPanel
            {
                Dock = DockStyle.Fill,
                ColumnCount = 2,
                RowCount = 6,
                AutoSize = true,
                Padding = new Padding(5)
            };
            workDetailsLayout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 40F));
            workDetailsLayout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 60F));

            // Problem Description
            var problemDescriptionLabel = new Label
            {
                Text = "وصف المشكلة - Problem:",
                Dock = DockStyle.Fill,
                TextAlign = ContentAlignment.TopRight
            };
            problemDescriptionTextBox = new TextBox
            {
                Dock = DockStyle.Fill,
                Multiline = true,
                Height = 50,
                ScrollBars = ScrollBars.Vertical
            };

            // Work Performed
            var workPerformedLabel = new Label
            {
                Text = "العمل المنفذ - Work Done:",
                Dock = DockStyle.Fill,
                TextAlign = ContentAlignment.TopRight
            };
            workPerformedTextBox = new TextBox
            {
                Dock = DockStyle.Fill,
                Multiline = true,
                Height = 50,
                ScrollBars = ScrollBars.Vertical
            };

            // Parts Replaced
            var partsReplacedLabel = new Label
            {
                Text = "القطع المستبدلة - Parts:",
                Dock = DockStyle.Fill,
                TextAlign = ContentAlignment.TopRight
            };
            partsReplacedTextBox = new TextBox
            {
                Dock = DockStyle.Fill,
                Multiline = true,
                Height = 40,
                ScrollBars = ScrollBars.Vertical
            };

            // Parts Cost
            var partsCostLabel = new Label
            {
                Text = "تكلفة القطع (ريال) - Parts Cost:",
                Dock = DockStyle.Fill,
                TextAlign = ContentAlignment.MiddleRight
            };
            partsCostNumeric = new NumericUpDown
            {
                Dock = DockStyle.Fill,
                Minimum = 0,
                Maximum = 1000000,
                DecimalPlaces = 2,
                Value = 0
            };
            partsCostNumeric.ValueChanged += CostChanged;

            // Labor Cost
            var laborCostLabel = new Label
            {
                Text = "تكلفة العمالة (ريال) - Labor Cost:",
                Dock = DockStyle.Fill,
                TextAlign = ContentAlignment.MiddleRight
            };
            laborCostNumeric = new NumericUpDown
            {
                Dock = DockStyle.Fill,
                Minimum = 0,
                Maximum = 1000000,
                DecimalPlaces = 2,
                Value = 0
            };
            laborCostNumeric.ValueChanged += CostChanged;

            // Total Cost
            var totalCostLabelText = new Label
            {
                Text = "الإجمالي - Total Cost:",
                Dock = DockStyle.Fill,
                TextAlign = ContentAlignment.MiddleRight,
                Font = new Font(this.Font, FontStyle.Bold)
            };
            totalCostLabelValue = new Label
            {
                Text = "0.00 ريال",
                Dock = DockStyle.Fill,
                TextAlign = ContentAlignment.MiddleLeft,
                Font = new Font(this.Font.FontFamily, 11, FontStyle.Bold),
                ForeColor = ThemeManager.SecondarySkyBlue
            };

            workDetailsLayout.Controls.Add(problemDescriptionLabel, 0, 0);
            workDetailsLayout.Controls.Add(problemDescriptionTextBox, 1, 0);
            workDetailsLayout.Controls.Add(workPerformedLabel, 0, 1);
            workDetailsLayout.Controls.Add(workPerformedTextBox, 1, 1);
            workDetailsLayout.Controls.Add(partsReplacedLabel, 0, 2);
            workDetailsLayout.Controls.Add(partsReplacedTextBox, 1, 2);
            workDetailsLayout.Controls.Add(partsCostLabel, 0, 3);
            workDetailsLayout.Controls.Add(partsCostNumeric, 1, 3);
            workDetailsLayout.Controls.Add(laborCostLabel, 0, 4);
            workDetailsLayout.Controls.Add(laborCostNumeric, 1, 4);
            workDetailsLayout.Controls.Add(totalCostLabelText, 0, 5);
            workDetailsLayout.Controls.Add(totalCostLabelValue, 1, 5);

            workDetailsGroup.Controls.Add(workDetailsLayout);

            // GroupBox 3: Personnel & Follow-up
            var followUpGroup = new GroupBox
            {
                Text = "الفنيون والمتابعة - Personnel & Follow-up",
                Dock = DockStyle.Fill,
                Padding = new Padding(10),
                AutoSize = true
            };

            var followUpLayout = new TableLayoutPanel
            {
                Dock = DockStyle.Fill,
                ColumnCount = 2,
                RowCount = 4,
                AutoSize = true,
                Padding = new Padding(5)
            };
            followUpLayout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 40F));
            followUpLayout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 60F));

            // Performed By
            var performedByLabel = new Label
            {
                Text = "المنفذ - Performed By:",
                Dock = DockStyle.Fill,
                TextAlign = ContentAlignment.MiddleRight
            };
            performedByTextBox = new TextBox
            {
                Dock = DockStyle.Fill
            };

            // Follow-up Required
            var followUpRequiredLabel = new Label
            {
                Text = "يتطلب متابعة - Requires Follow-up:",
                Dock = DockStyle.Fill,
                TextAlign = ContentAlignment.MiddleRight
            };
            followUpRequiredCheckBox = new CheckBox
            {
                Dock = DockStyle.Fill
            };
            followUpRequiredCheckBox.CheckedChanged += FollowUpRequiredCheckBox_CheckedChanged;

            // Follow-up Date
            var followUpDateLabel = new Label
            {
                Text = "تاريخ المتابعة - Follow-up Date:",
                Dock = DockStyle.Fill,
                TextAlign = ContentAlignment.MiddleRight
            };
            followUpDatePicker = new DateTimePicker
            {
                Dock = DockStyle.Fill,
                Format = DateTimePickerFormat.Short,
                Enabled = false
            };

            // Notes
            var notesLabel = new Label
            {
                Text = "ملاحظات - Notes:",
                Dock = DockStyle.Fill,
                TextAlign = ContentAlignment.TopRight
            };
            notesTextBox = new TextBox
            {
                Dock = DockStyle.Fill,
                Multiline = true,
                Height = 60,
                ScrollBars = ScrollBars.Vertical
            };

            followUpLayout.Controls.Add(performedByLabel, 0, 0);
            followUpLayout.Controls.Add(performedByTextBox, 1, 0);
            followUpLayout.Controls.Add(followUpRequiredLabel, 0, 1);
            followUpLayout.Controls.Add(followUpRequiredCheckBox, 1, 1);
            followUpLayout.Controls.Add(followUpDateLabel, 0, 2);
            followUpLayout.Controls.Add(followUpDatePicker, 1, 2);
            followUpLayout.Controls.Add(notesLabel, 0, 3);
            followUpLayout.Controls.Add(notesTextBox, 1, 3);

            followUpGroup.Controls.Add(followUpLayout);

            // GroupBox 4: Actions
            var actionsGroup = new GroupBox
            {
                Text = "الإجراءات - Actions",
                Dock = DockStyle.Fill,
                Padding = new Padding(10),
                AutoSize = true
            };

            var actionsLayout = new FlowLayoutPanel
            {
                Dock = DockStyle.Fill,
                FlowDirection = FlowDirection.RightToLeft,
                AutoSize = true,
                Padding = new Padding(5)
            };

            saveButton = new Button
            {
                Text = "حفظ - Save",
                Size = new Size(120, 35),
                BackColor = ThemeManager.SecondarySkyBlue,
                ForeColor = Color.White,
                FlatStyle = FlatStyle.Flat,
                Cursor = Cursors.Hand
            };
            saveButton.Click += SaveButton_Click;

            deleteButton = new Button
            {
                Text = "حذف - Delete",
                Size = new Size(120, 35),
                BackColor = Color.FromArgb(204, 0, 0),
                ForeColor = Color.White,
                FlatStyle = FlatStyle.Flat,
                Cursor = Cursors.Hand
            };
            deleteButton.Click += DeleteButton_Click;

            newButton = new Button
            {
                Text = "جديد - New",
                Size = new Size(120, 35),
                BackColor = Color.FromArgb(0, 153, 0),
                ForeColor = Color.White,
                FlatStyle = FlatStyle.Flat,
                Cursor = Cursors.Hand
            };
            newButton.Click += NewButton_Click;

            actionsLayout.Controls.Add(saveButton);
            actionsLayout.Controls.Add(deleteButton);
            actionsLayout.Controls.Add(newButton);

            actionsGroup.Controls.Add(actionsLayout);

            // Add all groups to details layout
            detailsLayout.Controls.Add(basicInfoGroup, 0, 0);
            detailsLayout.Controls.Add(workDetailsGroup, 0, 1);
            detailsLayout.Controls.Add(followUpGroup, 0, 2);
            detailsLayout.Controls.Add(actionsGroup, 0, 3);

            leftPanel.Controls.Add(detailsLayout);

            // Right panel - Records List
            var rightPanel = new Panel
            {
                Dock = DockStyle.Fill
            };

            var recordsLayout = new TableLayoutPanel
            {
                Dock = DockStyle.Fill,
                ColumnCount = 1,
                RowCount = 3,
                Padding = new Padding(5)
            };
            recordsLayout.RowStyles.Add(new RowStyle(SizeType.AutoSize));
            recordsLayout.RowStyles.Add(new RowStyle(SizeType.Percent, 100F));
            recordsLayout.RowStyles.Add(new RowStyle(SizeType.AutoSize));

            // Filter GroupBox
            var filterGroup = new GroupBox
            {
                Text = "التصفية - Filter",
                Dock = DockStyle.Fill,
                Padding = new Padding(10),
                AutoSize = true
            };

            var filterLayout = new TableLayoutPanel
            {
                Dock = DockStyle.Fill,
                ColumnCount = 5,
                RowCount = 2,
                AutoSize = true,
                Padding = new Padding(5)
            };
            filterLayout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 25F));
            filterLayout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 20F));
            filterLayout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 20F));
            filterLayout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 20F));
            filterLayout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 15F));

            // Filter Equipment
            var filterEquipmentLabel = new Label
            {
                Text = "المعدة - Equipment:",
                Dock = DockStyle.Fill,
                TextAlign = ContentAlignment.MiddleRight
            };
            filterEquipmentComboBox = new ComboBox
            {
                Dock = DockStyle.Fill,
                DropDownStyle = ComboBoxStyle.DropDownList,
                DisplayMember = "Name",
                ValueMember = "Id"
            };

            // Filter Status
            var filterStatusLabel = new Label
            {
                Text = "الحالة - Status:",
                Dock = DockStyle.Fill,
                TextAlign = ContentAlignment.MiddleRight
            };
            filterStatusComboBox = new ComboBox
            {
                Dock = DockStyle.Fill,
                DropDownStyle = ComboBoxStyle.DropDownList
            };

            // Filter From Date
            var filterFromDateLabel = new Label
            {
                Text = "من تاريخ - From:",
                Dock = DockStyle.Fill,
                TextAlign = ContentAlignment.MiddleRight
            };
            filterFromDatePicker = new DateTimePicker
            {
                Dock = DockStyle.Fill,
                Format = DateTimePickerFormat.Short
            };
            filterFromDatePicker.Value = DateTime.Now.AddMonths(-6); // عرض آخر 6 أشهر افتراضياً

            // Filter To Date
            var filterToDateLabel = new Label
            {
                Text = "إلى تاريخ - To:",
                Dock = DockStyle.Fill,
                TextAlign = ContentAlignment.MiddleRight
            };
            filterToDatePicker = new DateTimePicker
            {
                Dock = DockStyle.Fill,
                Format = DateTimePickerFormat.Short
            };

            // Apply Filter Button
            applyFilterButton = new Button
            {
                Text = "تطبيق - Apply",
                Dock = DockStyle.Fill,
                BackColor = ThemeManager.SecondarySkyBlue,
                ForeColor = Color.White,
                FlatStyle = FlatStyle.Flat,
                Cursor = Cursors.Hand
            };
            applyFilterButton.Click += ApplyFilterButton_Click;

            filterLayout.Controls.Add(filterEquipmentLabel, 0, 0);
            filterLayout.Controls.Add(filterEquipmentComboBox, 0, 1);
            filterLayout.Controls.Add(filterStatusLabel, 1, 0);
            filterLayout.Controls.Add(filterStatusComboBox, 1, 1);
            filterLayout.Controls.Add(filterFromDateLabel, 2, 0);
            filterLayout.Controls.Add(filterFromDatePicker, 2, 1);
            filterLayout.Controls.Add(filterToDateLabel, 3, 0);
            filterLayout.Controls.Add(filterToDatePicker, 3, 1);
            filterLayout.Controls.Add(applyFilterButton, 4, 1);

            filterGroup.Controls.Add(filterLayout);

            // Records Grid
            var recordsGroup = new GroupBox
            {
                Text = "سجلات الصيانة - Maintenance Records",
                Dock = DockStyle.Fill,
                Padding = new Padding(10)
            };

            recordsGrid = new DataGridView
            {
                Dock = DockStyle.Fill,
                AllowUserToAddRows = false,
                AllowUserToDeleteRows = false,
                ReadOnly = true,
                SelectionMode = DataGridViewSelectionMode.FullRowSelect,
                AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill,
                BackgroundColor = Color.White,
                BorderStyle = BorderStyle.Fixed3D
            };
            recordsGrid.SelectionChanged += RecordsGrid_SelectionChanged;

            recordsGroup.Controls.Add(recordsGrid);

            // Summary Panel
            var summaryPanel = new Panel
            {
                Dock = DockStyle.Fill,
                Height = 40,
                BackColor = Color.FromArgb(240, 240, 240)
            };

            summaryLabel = new Label
            {
                Text = "إجمالي التكلفة - Total Cost: 0.00 ريال",
                Dock = DockStyle.Fill,
                TextAlign = ContentAlignment.MiddleCenter,
                Font = new Font(this.Font.FontFamily, 12, FontStyle.Bold),
                ForeColor = ThemeManager.SecondarySkyBlue
            };

            summaryPanel.Controls.Add(summaryLabel);

            // Add to records layout
            recordsLayout.Controls.Add(filterGroup, 0, 0);
            recordsLayout.Controls.Add(recordsGroup, 0, 1);
            recordsLayout.Controls.Add(summaryPanel, 0, 2);

            rightPanel.Controls.Add(recordsLayout);

            // Add panels to main layout
            mainLayout.Controls.Add(leftPanel, 0, 0);
            mainLayout.Controls.Add(rightPanel, 1, 0);

            this.Controls.Add(mainLayout);
        }

        private void LoadEquipment()
        {
            var equipment = _context.Equipment
                .OrderBy(e => e.Name)
                .ToList();

            // Main ComboBox
            equipmentComboBox.DataSource = equipment.ToList();

            // Filter ComboBox
            var allEquipment = new List<Equipment> { new Equipment { Id = 0, Name = "الكل - All" } };
            allEquipment.AddRange(equipment);
            filterEquipmentComboBox.DataSource = allEquipment;
            filterEquipmentComboBox.SelectedIndex = 0;
        }

        private void LoadMaintenanceTypes()
        {
            maintenanceTypeComboBox.Items.Clear();
            maintenanceTypeComboBox.Items.Add("وقائية - Preventive");
            maintenanceTypeComboBox.Items.Add("إصلاحية - Corrective");
            maintenanceTypeComboBox.Items.Add("طارئة - Emergency");
            maintenanceTypeComboBox.Items.Add("تحسينية - Improvement");
            maintenanceTypeComboBox.SelectedIndex = 0;
        }

        private void LoadStatuses()
        {
            // Main ComboBox
            statusComboBox.Items.Clear();
            statusComboBox.Items.Add("مجدولة");
            statusComboBox.Items.Add("قيد التنفيذ");
            statusComboBox.Items.Add("مكتملة");
            statusComboBox.Items.Add("معلقة");
            statusComboBox.SelectedIndex = 2; // Default: مكتملة

            // Filter ComboBox
            filterStatusComboBox.Items.Clear();
            filterStatusComboBox.Items.Add("الكل - All");
            filterStatusComboBox.Items.Add("مجدولة");
            filterStatusComboBox.Items.Add("قيد التنفيذ");
            filterStatusComboBox.Items.Add("مكتملة");
            filterStatusComboBox.Items.Add("معلقة");
            filterStatusComboBox.SelectedIndex = 0;
        }

        private void LoadRecords()
        {
            // Temporarily disable SelectionChanged event to prevent errors during loading
            recordsGrid.SelectionChanged -= RecordsGrid_SelectionChanged;
            
            try
            {
                var query = _context.MaintenanceRecords
                    .Include(r => r.Equipment)
                    .AsQueryable();

                // Apply filters
                if (filterEquipmentComboBox.SelectedIndex > 0 && filterEquipmentComboBox.SelectedValue != null)
                {
                    var equipmentId = filterEquipmentComboBox.SelectedValue is int id ? id : 0;
                    query = query.Where(r => r.EquipmentId == equipmentId);
                }

                if (filterStatusComboBox.SelectedIndex > 0)
                {
                    var status = filterStatusComboBox.SelectedItem?.ToString();
                    if (!string.IsNullOrEmpty(status))
                    {
                        query = query.Where(r => r.Status == status);
                    }
                }

                query = query.Where(r => r.MaintenanceDate >= filterFromDatePicker.Value.Date &&
                                        r.MaintenanceDate <= filterToDatePicker.Value.Date);

                var records = query
                    .OrderByDescending(r => r.MaintenanceDate)
                    .Select(r => new
                    {
                        r.Id,
                        r.RecordNumber,
                        Equipment = r.Equipment.Name,
                        MaintenanceDate = r.MaintenanceDate,
                        Type = r.MaintenanceType,
                        Problem = r.ProblemDescription != null && r.ProblemDescription.Length > 30 
                            ? r.ProblemDescription.Substring(0, 30) + "..." 
                            : r.ProblemDescription ?? "-",
                        PerformedBy = r.PerformedBy ?? "-",
                        TotalCost = r.TotalCost,
                        Status = r.Status,
                        FollowUp = r.RequiresFollowUp ? "نعم - Yes" : "لا - No"
                    })
                    .ToList();

                recordsGrid.DataSource = records;

                // Configure columns (whether data exists or not)
                if (recordsGrid.Columns.Count > 0)
                {
                    if (recordsGrid.Columns["Id"] != null)
                        recordsGrid.Columns["Id"].Visible = false;
                    
                    if (recordsGrid.Columns["RecordNumber"] != null)
                    {
                        recordsGrid.Columns["RecordNumber"].HeaderText = "الرقم - #";
                        recordsGrid.Columns["RecordNumber"].AutoSizeMode = DataGridViewAutoSizeColumnMode.AllCells;
                    }
                    
                    if (recordsGrid.Columns["Equipment"] != null)
                    {
                        recordsGrid.Columns["Equipment"].HeaderText = "المعدة - Equipment";
                        recordsGrid.Columns["Equipment"].AutoSizeMode = DataGridViewAutoSizeColumnMode.AllCells;
                    }
                    
                    if (recordsGrid.Columns["MaintenanceDate"] != null)
                    {
                        recordsGrid.Columns["MaintenanceDate"].HeaderText = "التاريخ - Date";
                        recordsGrid.Columns["MaintenanceDate"].DefaultCellStyle.Format = "yyyy-MM-dd";
                        recordsGrid.Columns["MaintenanceDate"].AutoSizeMode = DataGridViewAutoSizeColumnMode.AllCells;
                    }
                    
                    if (recordsGrid.Columns["Type"] != null)
                    {
                        recordsGrid.Columns["Type"].HeaderText = "النوع - Type";
                        recordsGrid.Columns["Type"].AutoSizeMode = DataGridViewAutoSizeColumnMode.AllCells;
                    }
                    
                    if (recordsGrid.Columns["Problem"] != null)
                    {
                        recordsGrid.Columns["Problem"].HeaderText = "المشكلة - Problem";
                        recordsGrid.Columns["Problem"].AutoSizeMode = DataGridViewAutoSizeColumnMode.Fill;
                    }
                    
                    if (recordsGrid.Columns["PerformedBy"] != null)
                    {
                        recordsGrid.Columns["PerformedBy"].HeaderText = "المنفذ - By";
                        recordsGrid.Columns["PerformedBy"].AutoSizeMode = DataGridViewAutoSizeColumnMode.AllCells;
                    }
                    
                    if (recordsGrid.Columns["TotalCost"] != null)
                    {
                        recordsGrid.Columns["TotalCost"].HeaderText = "التكلفة - Cost";
                        recordsGrid.Columns["TotalCost"].DefaultCellStyle.Format = "N2";
                        recordsGrid.Columns["TotalCost"].AutoSizeMode = DataGridViewAutoSizeColumnMode.AllCells;
                    }
                    
                    if (recordsGrid.Columns["Status"] != null)
                    {
                        recordsGrid.Columns["Status"].HeaderText = "الحالة - Status";
                        recordsGrid.Columns["Status"].AutoSizeMode = DataGridViewAutoSizeColumnMode.AllCells;
                    }
                    
                    if (recordsGrid.Columns["FollowUp"] != null)
                    {
                        recordsGrid.Columns["FollowUp"].HeaderText = "متابعة - Follow-up";
                        recordsGrid.Columns["FollowUp"].AutoSizeMode = DataGridViewAutoSizeColumnMode.AllCells;
                    }
                }

                // Show message if no records found
                if (records.Count == 0)
                {
                    summaryLabel.Text = "لا توجد سجلات في هذه الفترة | No records found in this period";
                }
                else
                {
                    UpdateSummary();
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show(
                    $"خطأ في تحميل السجلات\nError loading records:\n{ex.Message}\n\nStack Trace:\n{ex.StackTrace}",
                    "خطأ - Error", 
                    MessageBoxButtons.OK, 
                    MessageBoxIcon.Error);
                
                summaryLabel.Text = "خطأ في تحميل البيانات - Error loading data";
            }
            finally
            {
                // Re-enable SelectionChanged event
                recordsGrid.SelectionChanged += RecordsGrid_SelectionChanged;
            }
        }

        private void UpdateSummary()
        {
            try
            {
                var query = _context.MaintenanceRecords.AsQueryable();

                // Apply same filters as LoadRecords
                if (filterEquipmentComboBox.SelectedIndex > 0 && filterEquipmentComboBox.SelectedValue != null)
                {
                    var equipmentId = filterEquipmentComboBox.SelectedValue is int id ? id : 0;
                    query = query.Where(r => r.EquipmentId == equipmentId);
                }

                if (filterStatusComboBox.SelectedIndex > 0)
                {
                    var status = filterStatusComboBox.SelectedItem?.ToString() ?? "";
                    query = query.Where(r => r.Status == status);
                }

                query = query.Where(r => r.MaintenanceDate >= filterFromDatePicker.Value.Date &&
                                        r.MaintenanceDate <= filterToDatePicker.Value.Date);

                var totalCost = query.Sum(r => r.TotalCost);
                var recordCount = query.Count();

                summaryLabel.Text = $"عدد السجلات: {recordCount} | إجمالي التكلفة - Total Cost: {totalCost:N2} ريال";
            }
            catch
            {
                summaryLabel.Text = "إجمالي التكلفة - Total Cost: 0.00 ريال";
            }
        }

        private void CostChanged(object? sender, EventArgs e)
        {
            var totalCost = partsCostNumeric.Value + laborCostNumeric.Value;
            totalCostLabelValue.Text = $"{totalCost:N2} ريال";
        }

        private void SaveButton_Click(object? sender, EventArgs e)
        {
            // Validation
            if (string.IsNullOrWhiteSpace(recordNumberTextBox.Text))
            {
                MessageBox.Show("يرجى إدخال رقم السجل\nPlease enter record number",
                    "خطأ - Error", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            if (equipmentComboBox.SelectedItem == null)
            {
                MessageBox.Show("يرجى اختيار معدة\nPlease select equipment",
                    "خطأ - Error", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            if (followUpRequiredCheckBox.Checked && followUpDatePicker.Value <= maintenanceDatePicker.Value)
            {
                MessageBox.Show("تاريخ المتابعة يجب أن يكون بعد تاريخ الصيانة\nFollow-up date must be after maintenance date",
                    "خطأ - Error", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            try
            {
                MaintenanceRecord record;

                if (_selectedRecordId == 0)
                {
                    // Check for duplicate record number
                    var exists = _context.MaintenanceRecords
                        .Any(r => r.RecordNumber == recordNumberTextBox.Text.Trim());
                    if (exists)
                    {
                        MessageBox.Show("رقم السجل موجود مسبقاً\nRecord number already exists",
                            "خطأ - Error", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                        return;
                    }

                    // New record
                    record = new MaintenanceRecord();
                    _context.MaintenanceRecords.Add(record);
                }
                else
                {
                    // Update existing
                    record = _context.MaintenanceRecords
                        .FirstOrDefault(r => r.Id == _selectedRecordId)!;
                    if (record == null)
                    {
                        MessageBox.Show("لم يتم العثور على السجل\nRecord not found",
                            "خطأ - Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                        return;
                    }
                    record.ModifiedDate = DateTime.Now;
                }

                // Set properties
#pragma warning disable CS8601
                record.RecordNumber = recordNumberTextBox.Text.Trim();
                record.EquipmentId = equipmentComboBox.SelectedValue is int eqId ? eqId : 0;
                record.MaintenanceDate = maintenanceDatePicker.Value;
                record.MaintenanceType = maintenanceTypeComboBox.SelectedItem?.ToString() ?? "";
                record.ProblemDescription = string.IsNullOrWhiteSpace(problemDescriptionTextBox.Text) 
                    ? null : problemDescriptionTextBox.Text.Trim();
                record.WorkPerformed = string.IsNullOrWhiteSpace(workPerformedTextBox.Text) 
                    ? null : workPerformedTextBox.Text.Trim();
                record.PartsReplaced = string.IsNullOrWhiteSpace(partsReplacedTextBox.Text) 
                    ? null : partsReplacedTextBox.Text.Trim();
                record.PartsCost = partsCostNumeric.Value;
                record.LaborCost = laborCostNumeric.Value;
                record.TotalCost = partsCostNumeric.Value + laborCostNumeric.Value;
#pragma warning disable CS8601
                record.PerformedBy = string.IsNullOrWhiteSpace(performedByTextBox.Text) 
                    ? null : performedByTextBox.Text.Trim();
#pragma warning restore CS8601
                record.Status = statusComboBox.SelectedItem?.ToString() ?? "مكتملة";
                record.RequiresFollowUp = followUpRequiredCheckBox.Checked;
                record.FollowUpDate = followUpRequiredCheckBox.Checked ? followUpDatePicker.Value : (DateTime?)null;
                record.Notes = string.IsNullOrWhiteSpace(notesTextBox.Text) 
                    ? null : notesTextBox.Text.Trim();
#pragma warning restore CS8601

                _context.SaveChanges();

                MessageBox.Show("تم حفظ السجل بنجاح\nRecord saved successfully",
                    "نجاح - Success", MessageBoxButtons.OK, MessageBoxIcon.Information);

                LoadRecords();
                ClearForm();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"خطأ في حفظ السجل\nError saving record: {ex.Message}",
                    "خطأ - Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void DeleteButton_Click(object? sender, EventArgs e)
        {
            if (_selectedRecordId == 0)
            {
                MessageBox.Show("يرجى اختيار سجل للحذف\nPlease select a record to delete",
                    "تنبيه - Warning", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            var result = MessageBox.Show("هل أنت متأكد من حذف هذا السجل؟\nAre you sure you want to delete this record?",
                "تأكيد الحذف - Confirm Delete", MessageBoxButtons.YesNo, MessageBoxIcon.Question);

            if (result == DialogResult.Yes)
            {
                try
                {
                    var record = _context.MaintenanceRecords.Find(_selectedRecordId);
                    if (record != null)
                    {
                        _context.MaintenanceRecords.Remove(record);
                        _context.SaveChanges();

                        MessageBox.Show("تم حذف السجل بنجاح\nRecord deleted successfully",
                            "نجاح - Success", MessageBoxButtons.OK, MessageBoxIcon.Information);

                        LoadRecords();
                        ClearForm();
                    }
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"خطأ في حذف السجل\nError deleting record: {ex.Message}",
                        "خطأ - Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                }
            }
        }

        private void NewButton_Click(object? sender, EventArgs e)
        {
            ClearForm();
        }

        private void RecordsGrid_SelectionChanged(object? sender, EventArgs e)
        {
            if (recordsGrid.SelectedRows.Count > 0)
            {
                var selectedRow = recordsGrid.SelectedRows[0];
                
                // Check if Id cell exists and has value
                if (selectedRow.Cells["Id"] == null || selectedRow.Cells["Id"].Value == null)
                    return;
                
                var recordId = (int)selectedRow.Cells["Id"].Value;

                var record = _context.MaintenanceRecords
                    .Include(r => r.Equipment)
                    .FirstOrDefault(r => r.Id == recordId);

                if (record != null)
                {
                    _selectedRecordId = record.Id;
                    recordNumberTextBox.Text = record.RecordNumber;
                    
                    // Set equipment using SelectedValue
                    if (equipmentComboBox.DataSource != null)
                    {
                        equipmentComboBox.SelectedValue = record.EquipmentId;
                    }
                    
                    maintenanceDatePicker.Value = record.MaintenanceDate;
                    maintenanceTypeComboBox.SelectedItem = record.MaintenanceType;
                    problemDescriptionTextBox.Text = record.ProblemDescription ?? string.Empty;
                    workPerformedTextBox.Text = record.WorkPerformed ?? string.Empty;
                    partsReplacedTextBox.Text = record.PartsReplaced ?? string.Empty;
                    partsCostNumeric.Value = record.PartsCost;
                    laborCostNumeric.Value = record.LaborCost;
                    performedByTextBox.Text = record.PerformedBy ?? string.Empty;
                    statusComboBox.SelectedItem = record.Status;
                    followUpRequiredCheckBox.Checked = record.RequiresFollowUp;
                    followUpDatePicker.Value = record.FollowUpDate ?? DateTime.Now.AddDays(7);
                    notesTextBox.Text = record.Notes ?? string.Empty;
                }
            }
        }

        private void FollowUpRequiredCheckBox_CheckedChanged(object? sender, EventArgs e)
        {
            followUpDatePicker.Enabled = followUpRequiredCheckBox.Checked;
        }

        private void ApplyFilterButton_Click(object? sender, EventArgs e)
        {
            LoadRecords();
        }

        private void ClearForm()
        {
            _selectedRecordId = 0;
            recordNumberTextBox.Clear();
            equipmentComboBox.SelectedIndex = -1;
            maintenanceDatePicker.Value = DateTime.Now;
            maintenanceTypeComboBox.SelectedIndex = 0;
            problemDescriptionTextBox.Clear();
            workPerformedTextBox.Clear();
            partsReplacedTextBox.Clear();
            partsCostNumeric.Value = 0;
            laborCostNumeric.Value = 0;
            totalCostLabelValue.Text = "0.00 ريال";
            performedByTextBox.Clear();
            statusComboBox.SelectedIndex = 2;
            followUpRequiredCheckBox.Checked = false;
            followUpDatePicker.Value = DateTime.Now.AddDays(7);
            notesTextBox.Clear();

            // Generate new record number
            recordNumberTextBox.Text = GenerateRecordNumber();
        }

        private string GenerateRecordNumber()
        {
            var year = DateTime.Now.Year;
            var month = DateTime.Now.Month;
            var prefix = $"MR-{year}{month:00}";

            var lastRecord = _context.MaintenanceRecords
                .Where(r => r.RecordNumber.StartsWith(prefix))
                .OrderByDescending(r => r.RecordNumber)
                .FirstOrDefault();

            if (lastRecord != null)
            {
                var lastNumber = lastRecord.RecordNumber.Substring(prefix.Length);
                if (int.TryParse(lastNumber, out int number))
                {
                    return $"{prefix}{(number + 1):000}";
                }
            }

            return $"{prefix}001";
        }
    }
}
