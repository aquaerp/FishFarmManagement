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
    public class SparePartForm : Form
    {
        private readonly FishFarmContext _context;
        private int _selectedPartId = 0;

        // Controls
        private TextBox partNumberTextBox = null!;
        private TextBox nameTextBox = null!;
        private TextBox descriptionTextBox = null!;
        private TextBox categoryTextBox = null!;
        private ComboBox equipmentComboBox = null!;
        private NumericUpDown minimumQuantityNumeric = null!;
        private NumericUpDown quantityInStockNumeric = null!;
        private NumericUpDown unitPriceNumeric = null!;
        private TextBox supplierTextBox = null!;
        private TextBox locationTextBox = null!;
        private DateTimePicker lastPurchaseDatePicker = null!;
        private TextBox notesTextBox = null!;
        private ComboBox statusComboBox = null!;
        private CheckBox isCriticalCheckBox = null!;
        private DataGridView partsGrid = null!;
        private Button saveButton = null!;
        private Button deleteButton = null!;
        private Button newButton = null!;
        private TextBox searchTextBox = null!;
        private CheckBox filterLowStockCheckBox = null!;
        private Button applyFilterButton = null!;
        private Label summaryLabel = null!;
        private Label stockWarningLabel = null!;

        public SparePartForm(FishFarmContext context)
        {
            _context = context;
            InitializeComponent();
            LoadEquipment();
            LoadStatuses();
            LoadParts();
            UpdateSummary();
        }

        private void InitializeComponent()
        {
            // Form setup
            this.Text = "قطع الغيار - Spare Parts";
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

            // Left panel
            var leftPanel = CreateLeftPanel();
            // Right panel
            var rightPanel = CreateRightPanel();

            mainLayout.Controls.Add(leftPanel, 0, 0);
            mainLayout.Controls.Add(rightPanel, 1, 0);

            this.Controls.Add(mainLayout);
        }

        private Panel CreateLeftPanel()
        {
            var leftPanel = new Panel { Dock = DockStyle.Fill, AutoScroll = true };
            var layout = new FlowLayoutPanel
            {
                Dock = DockStyle.Fill,
                FlowDirection = FlowDirection.TopDown,
                WrapContents = false,
                Padding = new Padding(5)
            };

            // Basic Info Group
            var basicGroup = new GroupBox
            {
                Text = "معلومات أساسية - Basic Info",
                Width = 450,
                Height = 250,
                Padding = new Padding(10)
            };
            var basicLayout = new TableLayoutPanel
            {
                Dock = DockStyle.Fill,
                ColumnCount = 2,
                RowCount = 5
            };
            basicLayout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 40F));
            basicLayout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 60F));

            partNumberTextBox = new TextBox { Dock = DockStyle.Fill };
            nameTextBox = new TextBox { Dock = DockStyle.Fill };
            categoryTextBox = new TextBox { Dock = DockStyle.Fill };
            equipmentComboBox = new ComboBox
            {
                Dock = DockStyle.Fill,
                DropDownStyle = ComboBoxStyle.DropDownList,
                DisplayMember = "Name",
                ValueMember = "Id"
            };
            descriptionTextBox = new TextBox { Dock = DockStyle.Fill, Multiline = true, Height = 50 };

            basicLayout.Controls.Add(new Label { Text = "رقم القطعة - Part #:", TextAlign = ContentAlignment.MiddleRight, Dock = DockStyle.Fill }, 0, 0);
            basicLayout.Controls.Add(partNumberTextBox, 1, 0);
            basicLayout.Controls.Add(new Label { Text = "الاسم - Name:", TextAlign = ContentAlignment.MiddleRight, Dock = DockStyle.Fill }, 0, 1);
            basicLayout.Controls.Add(nameTextBox, 1, 1);
            basicLayout.Controls.Add(new Label { Text = "الفئة - Category:", TextAlign = ContentAlignment.MiddleRight, Dock = DockStyle.Fill }, 0, 2);
            basicLayout.Controls.Add(categoryTextBox, 1, 2);
            basicLayout.Controls.Add(new Label { Text = "المعدة - Equipment:", TextAlign = ContentAlignment.MiddleRight, Dock = DockStyle.Fill }, 0, 3);
            basicLayout.Controls.Add(equipmentComboBox, 1, 3);
            basicLayout.Controls.Add(new Label { Text = "الوصف - Description:", TextAlign = ContentAlignment.TopRight, Dock = DockStyle.Fill }, 0, 4);
            basicLayout.Controls.Add(descriptionTextBox, 1, 4);
            basicGroup.Controls.Add(basicLayout);

            // Inventory Group
            var inventoryGroup = new GroupBox
            {
                Text = "المخزون والأسعار - Inventory & Pricing",
                Width = 450,
                Height = 200,
                Padding = new Padding(10)
            };
            var inventoryLayout = new TableLayoutPanel
            {
                Dock = DockStyle.Fill,
                ColumnCount = 2,
                RowCount = 4
            };
            inventoryLayout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 40F));
            inventoryLayout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 60F));

            minimumQuantityNumeric = new NumericUpDown { Dock = DockStyle.Fill, Minimum = 0, Maximum = 10000, Value = 5 };
            quantityInStockNumeric = new NumericUpDown { Dock = DockStyle.Fill, Minimum = 0, Maximum = 10000, Value = 0 };
            quantityInStockNumeric.ValueChanged += QuantityChanged;
            unitPriceNumeric = new NumericUpDown { Dock = DockStyle.Fill, Minimum = 0, Maximum = 1000000, DecimalPlaces = 2 };
            stockWarningLabel = new Label
            {
                Text = "",
                Dock = DockStyle.Fill,
                ForeColor = Color.Red,
                Font = new Font(this.Font, FontStyle.Bold),
                Visible = false
            };

            inventoryLayout.Controls.Add(new Label { Text = "الحد الأدنى - Min Qty:", TextAlign = ContentAlignment.MiddleRight, Dock = DockStyle.Fill }, 0, 0);
            inventoryLayout.Controls.Add(minimumQuantityNumeric, 1, 0);
            inventoryLayout.Controls.Add(new Label { Text = "الكمية الحالية - Current:", TextAlign = ContentAlignment.MiddleRight, Dock = DockStyle.Fill }, 0, 1);
            inventoryLayout.Controls.Add(quantityInStockNumeric, 1, 1);
            inventoryLayout.Controls.Add(new Label(), 0, 2);
            inventoryLayout.Controls.Add(stockWarningLabel, 1, 2);
            inventoryLayout.Controls.Add(new Label { Text = "سعر الوحدة - Price:", TextAlign = ContentAlignment.MiddleRight, Dock = DockStyle.Fill }, 0, 3);
            inventoryLayout.Controls.Add(unitPriceNumeric, 1, 3);
            inventoryGroup.Controls.Add(inventoryLayout);

            // Additional Details Group
            var detailsGroup = new GroupBox
            {
                Text = "تفاصيل إضافية - Additional Details",
                Width = 450,
                Height = 220,
                Padding = new Padding(10)
            };
            var detailsLayout = new TableLayoutPanel
            {
                Dock = DockStyle.Fill,
                ColumnCount = 2,
                RowCount = 5
            };
            detailsLayout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 40F));
            detailsLayout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 60F));

            supplierTextBox = new TextBox { Dock = DockStyle.Fill };
            locationTextBox = new TextBox { Dock = DockStyle.Fill };
            lastPurchaseDatePicker = new DateTimePicker { Dock = DockStyle.Fill, Format = DateTimePickerFormat.Short, Checked = false, ShowCheckBox = true };
            statusComboBox = new ComboBox { Dock = DockStyle.Fill, DropDownStyle = ComboBoxStyle.DropDownList };
            isCriticalCheckBox = new CheckBox { Dock = DockStyle.Fill, Text = "قطعة حرجة - Critical Part" };
            notesTextBox = new TextBox { Dock = DockStyle.Fill, Multiline = true, Height = 40 };

            detailsLayout.Controls.Add(new Label { Text = "المورد - Supplier:", TextAlign = ContentAlignment.MiddleRight, Dock = DockStyle.Fill }, 0, 0);
            detailsLayout.Controls.Add(supplierTextBox, 1, 0);
            detailsLayout.Controls.Add(new Label { Text = "الموقع - Location:", TextAlign = ContentAlignment.MiddleRight, Dock = DockStyle.Fill }, 0, 1);
            detailsLayout.Controls.Add(locationTextBox, 1, 1);
            detailsLayout.Controls.Add(new Label { Text = "آخر شراء - Last Purchase:", TextAlign = ContentAlignment.MiddleRight, Dock = DockStyle.Fill }, 0, 2);
            detailsLayout.Controls.Add(lastPurchaseDatePicker, 1, 2);
            detailsLayout.Controls.Add(new Label { Text = "الحالة - Status:", TextAlign = ContentAlignment.MiddleRight, Dock = DockStyle.Fill }, 0, 3);
            detailsLayout.Controls.Add(statusComboBox, 1, 3);
            detailsLayout.Controls.Add(new Label(), 0, 4);
            detailsLayout.Controls.Add(isCriticalCheckBox, 1, 4);
            detailsGroup.Controls.Add(detailsLayout);

            // Actions Group
            var actionsGroup = new GroupBox
            {
                Text = "الإجراءات - Actions",
                Width = 450,
                Height = 70,
                Padding = new Padding(10)
            };
            var actionsLayout = new FlowLayoutPanel
            {
                Dock = DockStyle.Fill,
                FlowDirection = FlowDirection.RightToLeft
            };

            saveButton = new Button
            {
                Text = "حفظ - Save",
                Size = new Size(100, 30),
                BackColor = ThemeManager.SecondarySkyBlue,
                ForeColor = Color.White,
                FlatStyle = FlatStyle.Flat
            };
            saveButton.Click += SaveButton_Click;

            deleteButton = new Button
            {
                Text = "حذف - Delete",
                Size = new Size(100, 30),
                BackColor = Color.FromArgb(204, 0, 0),
                ForeColor = Color.White,
                FlatStyle = FlatStyle.Flat
            };
            deleteButton.Click += DeleteButton_Click;

            newButton = new Button
            {
                Text = "جديد - New",
                Size = new Size(100, 30),
                BackColor = Color.FromArgb(0, 153, 0),
                ForeColor = Color.White,
                FlatStyle = FlatStyle.Flat
            };
            newButton.Click += NewButton_Click;

            actionsLayout.Controls.Add(saveButton);
            actionsLayout.Controls.Add(deleteButton);
            actionsLayout.Controls.Add(newButton);
            actionsGroup.Controls.Add(actionsLayout);

            layout.Controls.Add(basicGroup);
            layout.Controls.Add(inventoryGroup);
            layout.Controls.Add(detailsGroup);
            layout.Controls.Add(actionsGroup);
            leftPanel.Controls.Add(layout);

            return leftPanel;
        }

        private Panel CreateRightPanel()
        {
            var rightPanel = new Panel { Dock = DockStyle.Fill };
            var layout = new TableLayoutPanel
            {
                Dock = DockStyle.Fill,
                ColumnCount = 1,
                RowCount = 3,
                Padding = new Padding(5)
            };
            layout.RowStyles.Add(new RowStyle(SizeType.AutoSize));
            layout.RowStyles.Add(new RowStyle(SizeType.Percent, 100F));
            layout.RowStyles.Add(new RowStyle(SizeType.AutoSize));

            // Filter Group
            var filterGroup = new GroupBox
            {
                Text = "البحث والتصفية - Search & Filter",
                Dock = DockStyle.Fill,
                Height = 80,
                Padding = new Padding(10)
            };
            var filterLayout = new FlowLayoutPanel
            {
                Dock = DockStyle.Fill,
                FlowDirection = FlowDirection.RightToLeft
            };

            searchTextBox = new TextBox { Width = 200 };
            filterLowStockCheckBox = new CheckBox { Text = "مخزون منخفض - Low Stock", Width = 150 };
            applyFilterButton = new Button
            {
                Text = "تطبيق - Apply",
                Width = 100,
                BackColor = ThemeManager.SecondarySkyBlue,
                ForeColor = Color.White,
                FlatStyle = FlatStyle.Flat
            };
            applyFilterButton.Click += ApplyFilterButton_Click;

            filterLayout.Controls.Add(applyFilterButton);
            filterLayout.Controls.Add(filterLowStockCheckBox);
            filterLayout.Controls.Add(new Label { Text = "بحث - Search:", TextAlign = ContentAlignment.MiddleLeft, Width = 80 });
            filterLayout.Controls.Add(searchTextBox);
            filterGroup.Controls.Add(filterLayout);

            // Parts Grid
            var partsGridGroup = new GroupBox
            {
                Text = "قطع الغيار - Spare Parts List",
                Dock = DockStyle.Fill,
                Padding = new Padding(10)
            };

            partsGrid = new DataGridView
            {
                Dock = DockStyle.Fill,
                AllowUserToAddRows = false,
                AllowUserToDeleteRows = false,
                ReadOnly = true,
                SelectionMode = DataGridViewSelectionMode.FullRowSelect,
                AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill,
                BackgroundColor = Color.White
            };
            partsGrid.SelectionChanged += PartsGrid_SelectionChanged;
            partsGridGroup.Controls.Add(partsGrid);

            // Summary Panel
            var summaryPanel = new Panel
            {
                Dock = DockStyle.Fill,
                Height = 40,
                BackColor = Color.FromArgb(240, 240, 240)
            };

            summaryLabel = new Label
            {
                Text = "إجمالي القيمة - Total Value: 0.00 ريال",
                Dock = DockStyle.Fill,
                TextAlign = ContentAlignment.MiddleCenter,
                Font = new Font(this.Font.FontFamily, 12, FontStyle.Bold),
                ForeColor = ThemeManager.SecondarySkyBlue
            };
            summaryPanel.Controls.Add(summaryLabel);

            layout.Controls.Add(filterGroup, 0, 0);
            layout.Controls.Add(partsGridGroup, 0, 1);
            layout.Controls.Add(summaryPanel, 0, 2);
            rightPanel.Controls.Add(layout);

            return rightPanel;
        }

        private void LoadEquipment()
        {
            try
            {
                var equipment = _context.Equipment.OrderBy(e => e.Name).ToList();
                var allEquipment = new List<Equipment> { new Equipment { Id = 0, Name = "-- عام - General --" } };
                allEquipment.AddRange(equipment);
                
                equipmentComboBox.DataSource = null;
                equipmentComboBox.DataSource = allEquipment;
                equipmentComboBox.DisplayMember = "Name";
                equipmentComboBox.ValueMember = "Id";
                equipmentComboBox.SelectedIndex = 0;
            }
            catch (Exception ex)
            {
                MessageBox.Show($"خطأ في تحميل المعدات\nError loading equipment: {ex.Message}",
                    "خطأ - Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void LoadStatuses()
        {
            try
            {
                statusComboBox.Items.Clear();
                statusComboBox.Items.Add("متوفر");
                statusComboBox.Items.Add("منخفض");
                statusComboBox.Items.Add("نفذ");
                statusComboBox.Items.Add("مطلوب طلب");
                statusComboBox.SelectedIndex = 0;
            }
            catch
            {
                // Ignore if status combo box not yet initialized
            }
        }

        private void LoadParts()
        {
            try
            {
                var query = _context.SpareParts.Include(sp => sp.Equipment).AsQueryable();

                if (!string.IsNullOrWhiteSpace(searchTextBox.Text))
                {
                    var search = searchTextBox.Text.Trim();
                    query = query.Where(sp => sp.PartNumber.Contains(search) || sp.Name.Contains(search));
                }

                if (filterLowStockCheckBox.Checked)
                {
                    query = query.Where(sp => sp.QuantityInStock <= sp.MinimumQuantity);
                }

                var parts = query.OrderBy(sp => sp.Name)
                    .Select(sp => new
                    {
                        sp.Id,
                        sp.PartNumber,
                        sp.Name,
                        sp.Category,
                        Equipment = sp.Equipment != null ? sp.Equipment.Name : "عام",
                        MinQty = sp.MinimumQuantity,
                        CurrentQty = sp.QuantityInStock,
                        UnitPrice = sp.UnitPrice,
                        TotalValue = sp.QuantityInStock * sp.UnitPrice,
                        sp.Status,
                        IsCritical = sp.IsCritical ? "⚠️ حرجة" : "-",
                        StockStatus = sp.QuantityInStock <= sp.MinimumQuantity ? "⚠️ منخفض" : "✓ جيد"
                    }).ToList();

                partsGrid.DataSource = parts;

                if (partsGrid.Columns.Count > 0)
                {
                    partsGrid.Columns["Id"].Visible = false;
                    partsGrid.Columns["PartNumber"].HeaderText = "الرقم - Part #";
                    partsGrid.Columns["PartNumber"].AutoSizeMode = DataGridViewAutoSizeColumnMode.AllCells;
                    partsGrid.Columns["Name"].HeaderText = "الاسم - Name";
                    partsGrid.Columns["Name"].AutoSizeMode = DataGridViewAutoSizeColumnMode.Fill;
                    partsGrid.Columns["Category"].HeaderText = "الفئة - Category";
                    partsGrid.Columns["Category"].AutoSizeMode = DataGridViewAutoSizeColumnMode.AllCells;
                    partsGrid.Columns["Equipment"].HeaderText = "المعدة - Equipment";
                    partsGrid.Columns["Equipment"].AutoSizeMode = DataGridViewAutoSizeColumnMode.AllCells;
                    partsGrid.Columns["MinQty"].HeaderText = "الحد الأدنى - Min";
                    partsGrid.Columns["MinQty"].AutoSizeMode = DataGridViewAutoSizeColumnMode.AllCells;
                    partsGrid.Columns["CurrentQty"].HeaderText = "الحالي - Current";
                    partsGrid.Columns["CurrentQty"].AutoSizeMode = DataGridViewAutoSizeColumnMode.AllCells;
                    partsGrid.Columns["UnitPrice"].HeaderText = "السعر - Price";
                    partsGrid.Columns["UnitPrice"].DefaultCellStyle.Format = "N2";
                    partsGrid.Columns["UnitPrice"].AutoSizeMode = DataGridViewAutoSizeColumnMode.AllCells;
                    partsGrid.Columns["TotalValue"].HeaderText = "القيمة - Value";
                    partsGrid.Columns["TotalValue"].DefaultCellStyle.Format = "N2";
                    partsGrid.Columns["TotalValue"].AutoSizeMode = DataGridViewAutoSizeColumnMode.AllCells;
                    partsGrid.Columns["Status"].HeaderText = "الحالة - Status";
                    partsGrid.Columns["Status"].AutoSizeMode = DataGridViewAutoSizeColumnMode.AllCells;
                    partsGrid.Columns["IsCritical"].HeaderText = "حرجة - Critical";
                    partsGrid.Columns["IsCritical"].AutoSizeMode = DataGridViewAutoSizeColumnMode.AllCells;
                    partsGrid.Columns["StockStatus"].HeaderText = "حالة المخزون - Stock";
                    partsGrid.Columns["StockStatus"].AutoSizeMode = DataGridViewAutoSizeColumnMode.AllCells;

                    foreach (DataGridViewRow row in partsGrid.Rows)
                    {
                        var stockStatusCell = row.Cells["StockStatus"];
                        if (stockStatusCell?.Value?.ToString()?.Contains("منخفض") == true)
                        {
                            row.DefaultCellStyle.BackColor = Color.FromArgb(255, 235, 235);
                            row.DefaultCellStyle.ForeColor = Color.DarkRed;
                        }
                    }
                }

                UpdateSummary();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"خطأ في تحميل القطع\nError: {ex.Message}", "خطأ - Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void UpdateSummary()
        {
            try
            {
                if (_context?.SpareParts == null)
                {
                    summaryLabel.Text = "إجمالي القيمة - Total Value: 0.00 ريال";
                    return;
                }

                var query = _context.SpareParts.AsQueryable();

                if (!string.IsNullOrWhiteSpace(searchTextBox?.Text))
                {
                    var search = searchTextBox.Text.Trim();
                    query = query.Where(sp => sp.PartNumber.Contains(search) || sp.Name.Contains(search));
                }

                if (filterLowStockCheckBox?.Checked == true)
                {
                    query = query.Where(sp => sp.QuantityInStock <= sp.MinimumQuantity);
                }

                var parts = query.ToList();
                var totalValue = parts.Sum(sp => sp.QuantityInStock * sp.UnitPrice);
                var partCount = query.Count();
                var lowStockCount = query.Count(sp => sp.QuantityInStock <= sp.MinimumQuantity);

                summaryLabel.Text = $"العدد: {partCount} | مخزون منخفض: {lowStockCount} | إجمالي القيمة: {totalValue:N2} ريال";
            }
            catch
            {
                if (summaryLabel != null)
                {
                    summaryLabel.Text = "إجمالي القيمة - Total Value: 0.00 ريال";
                }
            }
        }

        private void QuantityChanged(object? sender, EventArgs e)
        {
            if (quantityInStockNumeric.Value <= minimumQuantityNumeric.Value)
            {
                stockWarningLabel.Text = "⚠️ تنبيه: المخزون أقل من الحد الأدنى!";
                stockWarningLabel.Visible = true;
            }
            else
            {
                stockWarningLabel.Visible = false;
            }
        }

        private void SaveButton_Click(object? sender, EventArgs e)
        {
            if (string.IsNullOrWhiteSpace(partNumberTextBox.Text) || string.IsNullOrWhiteSpace(nameTextBox.Text))
            {
                MessageBox.Show("يرجى إدخال رقم القطعة والاسم\nPlease enter part number and name",
                    "خطأ - Error", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            try
            {
                SparePart part;

                if (_selectedPartId == 0)
                {
                    if (_context.SpareParts.Any(sp => sp.PartNumber == partNumberTextBox.Text.Trim()))
                    {
                        MessageBox.Show("رقم القطعة موجود مسبقاً\nPart number exists",
                            "خطأ - Error", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                        return;
                    }
                    part = new SparePart();
                    _context.SpareParts.Add(part);
                }
                else
                {
                    part = _context.SpareParts.Find(_selectedPartId)!;
                    if (part == null) return;
                }

#pragma warning disable CS8601
                part.PartNumber = partNumberTextBox.Text.Trim();
                part.Name = nameTextBox.Text.Trim();
                part.Description = string.IsNullOrWhiteSpace(descriptionTextBox.Text) ? null : descriptionTextBox.Text.Trim();
                part.Category = string.IsNullOrWhiteSpace(categoryTextBox.Text) ? null : categoryTextBox.Text.Trim();
                part.EquipmentId = equipmentComboBox.SelectedIndex > 0 && equipmentComboBox.SelectedValue is int eqId 
                    ? (int?)eqId 
                    : null;
#pragma warning restore CS8601
                part.MinimumQuantity = (int)minimumQuantityNumeric.Value;
                part.QuantityInStock = (int)quantityInStockNumeric.Value;
                part.UnitPrice = unitPriceNumeric.Value;
                part.Supplier = string.IsNullOrWhiteSpace(supplierTextBox.Text) ? null : supplierTextBox.Text.Trim();
                part.Location = string.IsNullOrWhiteSpace(locationTextBox.Text) ? null : locationTextBox.Text.Trim();
                part.LastPurchaseDate = lastPurchaseDatePicker.Checked ? lastPurchaseDatePicker.Value : (DateTime?)null;
                part.Status = statusComboBox.SelectedItem?.ToString() ?? "متوفر";
                part.IsCritical = isCriticalCheckBox.Checked;

                _context.SaveChanges();

                MessageBox.Show("تم حفظ القطعة بنجاح\nPart saved successfully",
                    "نجاح - Success", MessageBoxButtons.OK, MessageBoxIcon.Information);

                LoadParts();
                ClearForm();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"خطأ في حفظ القطعة\nError: {ex.Message}",
                    "خطأ - Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void DeleteButton_Click(object? sender, EventArgs e)
        {
            if (_selectedPartId == 0)
            {
                MessageBox.Show("يرجى اختيار قطعة للحذف\nPlease select a part",
                    "تنبيه - Warning", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            if (MessageBox.Show("هل أنت متأكد من حذف هذه القطعة؟\nAre you sure?",
                "تأكيد - Confirm", MessageBoxButtons.YesNo, MessageBoxIcon.Question) == DialogResult.Yes)
            {
                try
                {
                    var part = _context.SpareParts.Find(_selectedPartId);
                    if (part != null)
                    {
                        _context.SpareParts.Remove(part);
                        _context.SaveChanges();

                        MessageBox.Show("تم حذف القطعة\nPart deleted",
                            "نجاح - Success", MessageBoxButtons.OK, MessageBoxIcon.Information);

                        LoadParts();
                        ClearForm();
                    }
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"خطأ في الحذف\nError: {ex.Message}",
                        "خطأ - Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                }
            }
        }

        private void NewButton_Click(object? sender, EventArgs e)
        {
            ClearForm();
        }

        private void PartsGrid_SelectionChanged(object? sender, EventArgs e)
        {
            if (partsGrid.SelectedRows.Count > 0 && partsGrid.SelectedRows[0].Cells["Id"].Value != null)
            {
                var partId = (int)partsGrid.SelectedRows[0].Cells["Id"].Value;
                var part = _context.SpareParts.Include(sp => sp.Equipment).FirstOrDefault(sp => sp.Id == partId);

                if (part != null)
                {
                    _selectedPartId = part.Id;
                    partNumberTextBox.Text = part.PartNumber;
                    nameTextBox.Text = part.Name;
                    descriptionTextBox.Text = part.Description ?? "";
                    categoryTextBox.Text = part.Category ?? "";
                    if (part.EquipmentId.HasValue && part.EquipmentId.Value > 0)
                    {
                        equipmentComboBox.SelectedValue = part.EquipmentId.Value;
                    }
                    else
                    {
                        equipmentComboBox.SelectedIndex = 0;
                    }
                    minimumQuantityNumeric.Value = part.MinimumQuantity;
                    quantityInStockNumeric.Value = part.QuantityInStock;
                    unitPriceNumeric.Value = part.UnitPrice;
                    supplierTextBox.Text = part.Supplier ?? "";
                    locationTextBox.Text = part.Location ?? "";
                    
                    if (part.LastPurchaseDate.HasValue)
                    {
                        lastPurchaseDatePicker.Checked = true;
                        lastPurchaseDatePicker.Value = part.LastPurchaseDate.Value;
                    }
                    else
                    {
                        lastPurchaseDatePicker.Checked = false;
                    }

                    statusComboBox.SelectedItem = part.Status;
                    isCriticalCheckBox.Checked = part.IsCritical;
                }
            }
        }

        private void ApplyFilterButton_Click(object? sender, EventArgs e)
        {
            LoadParts();
        }

        private void ClearForm()
        {
            _selectedPartId = 0;
            partNumberTextBox.Clear();
            nameTextBox.Clear();
            descriptionTextBox.Clear();
            categoryTextBox.Clear();
            equipmentComboBox.SelectedIndex = 0;
            minimumQuantityNumeric.Value = 5;
            quantityInStockNumeric.Value = 0;
            unitPriceNumeric.Value = 0;
            supplierTextBox.Clear();
            locationTextBox.Clear();
            lastPurchaseDatePicker.Checked = false;
            statusComboBox.SelectedIndex = 0;
            isCriticalCheckBox.Checked = false;
            stockWarningLabel.Visible = false;
        }
    }
}
