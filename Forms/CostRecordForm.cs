using System;
using FishFarmManager.Services;
using System.Linq;
using System.Windows.Forms;
using FishFarmManager.Data;
using FishFarmManager.Models;
using Microsoft.EntityFrameworkCore;

namespace FishFarmManager.Forms
{
    /// <summary>
    /// نموذج إدارة تسجيل التكاليف والمصروفات
    /// Cost record management form - 26 cost categories
    /// </summary>
    public partial class CostRecordForm : Form
    {
        private readonly FishFarmContext _context;
        private int _selectedCostId = 0;

        // Controls
        private ComboBox _categoryComboBox = null!;
        private ComboBox _cycleComboBox = null!;
        private ComboBox _pondComboBox = null!;
        private ComboBox _supplierComboBox = null!;
        private DateTimePicker _datePicker = null!;
        private TextBox _descriptionTextBox = null!;
        private NumericUpDown _quantityNumeric = null!;
        private ComboBox _unitComboBox = null!;
        private NumericUpDown _unitPriceNumeric = null!;
        private Label _totalLabel = null!;
        private ComboBox _paymentMethodComboBox = null!;
        private ComboBox _paymentStatusComboBox = null!;
        private TextBox _invoiceNumberTextBox = null!;
        private DateTimePicker _invoiceDatePicker = null!;
        private DateTimePicker _paymentDueDatePicker = null!;
        private TextBox _notesTextBox = null!;
        private DataGridView _costsGrid = null!;
        private Button _saveButton = null!;
        private Button _deleteButton = null!;
        private Button _newButton = null!;
        private TextBox _searchTextBox = null!;
        private DateTimePicker _filterFromDatePicker = null!;
        private DateTimePicker _filterToDatePicker = null!;
        private ComboBox _filterCategoryComboBox = null!;

        public CostRecordForm(FishFarmContext context)
        {
            _context = context;
            InitializeComponent();
            LoadCategories();
            LoadCycles();
            LoadPonds();
            LoadSuppliers();
            LoadCosts();
        }

        private void InitializeComponent()
        {
            this.Text = "إدارة التكاليف والمصروفات - Cost Management";
            this.Size = new System.Drawing.Size(1400, 850);
            this.RightToLeft = RightToLeft.Yes;
            this.RightToLeftLayout = true;
            this.StartPosition = FormStartPosition.CenterScreen;

            var topPanel = new Panel
            {
                Dock = DockStyle.Top,
                Height = 550,
                Padding = new Padding(10),
                AutoScroll = true
            };

            var bottomPanel = new Panel
            {
                Dock = DockStyle.Fill,
                Padding = new Padding(10)
            };

            // Title
            var titleLabel = new Label
            {
                Text = "تسجيل التكاليف والمصروفات",
                Font = new System.Drawing.Font("Segoe UI", 12, System.Drawing.FontStyle.Bold),
                Location = new System.Drawing.Point(10, 10),
                AutoSize = true
            };
            topPanel.Controls.Add(titleLabel);

            int yPos = 50;

            // Row 1 - Category and Date
            topPanel.Controls.Add(new Label { Text = "فئة التكلفة:", Location = new System.Drawing.Point(1250, yPos), AutoSize = true });
            _categoryComboBox = new ComboBox
            {
                Location = new System.Drawing.Point(1000, yPos),
                Width = 230,
                DropDownStyle = ComboBoxStyle.DropDownList
            };
            _categoryComboBox.SelectedIndexChanged += (s, e) => UpdateCategoryHelp();
            topPanel.Controls.Add(_categoryComboBox);

            topPanel.Controls.Add(new Label { Text = "التاريخ:", Location = new System.Drawing.Point(900, yPos), AutoSize = true });
            _datePicker = new DateTimePicker
            {
                Location = new System.Drawing.Point(720, yPos),
                Width = 160,
                Format = DateTimePickerFormat.Short
            };
            topPanel.Controls.Add(_datePicker);

            // Row 2 - Cycle and Pond
            yPos += 40;
            topPanel.Controls.Add(new Label { Text = "الدورة الإنتاجية:", Location = new System.Drawing.Point(1250, yPos), AutoSize = true });
            _cycleComboBox = new ComboBox
            {
                Location = new System.Drawing.Point(1000, yPos),
                Width = 230,
                DropDownStyle = ComboBoxStyle.DropDownList
            };
            topPanel.Controls.Add(_cycleComboBox);

            topPanel.Controls.Add(new Label { Text = "الحوض:", Location = new System.Drawing.Point(900, yPos), AutoSize = true });
            _pondComboBox = new ComboBox
            {
                Location = new System.Drawing.Point(720, yPos),
                Width = 160,
                DropDownStyle = ComboBoxStyle.DropDownList
            };
            topPanel.Controls.Add(_pondComboBox);

            // Row 3 - Supplier
            yPos += 40;
            topPanel.Controls.Add(new Label { Text = "المورد:", Location = new System.Drawing.Point(1250, yPos), AutoSize = true });
            _supplierComboBox = new ComboBox
            {
                Location = new System.Drawing.Point(900, yPos),
                Width = 330,
                DropDownStyle = ComboBoxStyle.DropDownList
            };
            topPanel.Controls.Add(_supplierComboBox);

            // Row 4 - Description
            yPos += 40;
            topPanel.Controls.Add(new Label { Text = "الوصف:", Location = new System.Drawing.Point(1250, yPos), AutoSize = true });
            _descriptionTextBox = new TextBox
            {
                Location = new System.Drawing.Point(720, yPos),
                Width = 510,
                Multiline = true,
                Height = 50
            };
            topPanel.Controls.Add(_descriptionTextBox);

            // Row 5 - Quantity, Unit, Unit Price
            yPos += 60;
            topPanel.Controls.Add(new Label { Text = "الكمية:", Location = new System.Drawing.Point(1250, yPos), AutoSize = true });
            _quantityNumeric = new NumericUpDown
            {
                Location = new System.Drawing.Point(1100, yPos),
                Width = 130,
                DecimalPlaces = 2,
                Maximum = 999999,
                Minimum = 0
            };
            _quantityNumeric.ValueChanged += (s, e) => CalculateTotal();
            topPanel.Controls.Add(_quantityNumeric);

            topPanel.Controls.Add(new Label { Text = "الوحدة:", Location = new System.Drawing.Point(1000, yPos), AutoSize = true });
            _unitComboBox = new ComboBox
            {
                Location = new System.Drawing.Point(850, yPos),
                Width = 130,
                DropDownStyle = ComboBoxStyle.DropDownList
            };
            _unitComboBox.Items.AddRange(new object[]
            {
                "كجم", "طن", "كيس", "صندوق", "لتر", "وحدة", "شهر", "ساعة", "يوم", "أخرى"
            });
            _unitComboBox.SelectedIndex = 0;
            topPanel.Controls.Add(_unitComboBox);

            topPanel.Controls.Add(new Label { Text = "سعر الوحدة:", Location = new System.Drawing.Point(750, yPos), AutoSize = true });
            _unitPriceNumeric = new NumericUpDown
            {
                Location = new System.Drawing.Point(580, yPos),
                Width = 150,
                DecimalPlaces = 2,
                Maximum = 999999,
                Minimum = 0,
                ThousandsSeparator = true
            };
            _unitPriceNumeric.ValueChanged += (s, e) => CalculateTotal();
            topPanel.Controls.Add(_unitPriceNumeric);

            // Total Label
            yPos += 40;
            topPanel.Controls.Add(new Label { Text = "المبلغ الإجمالي:", Location = new System.Drawing.Point(1250, yPos), AutoSize = true });
            _totalLabel = new Label
            {
                Text = "0.00 ريال",
                Location = new System.Drawing.Point(1000, yPos),
                Width = 230,
                Font = new System.Drawing.Font("Segoe UI", 11, System.Drawing.FontStyle.Bold),
                ForeColor = System.Drawing.Color.DarkGreen,
                TextAlign = System.Drawing.ContentAlignment.MiddleRight
            };
            topPanel.Controls.Add(_totalLabel);

            // Row 6 - Payment Method and Status
            yPos += 40;
            topPanel.Controls.Add(new Label { Text = "طريقة الدفع:", Location = new System.Drawing.Point(1250, yPos), AutoSize = true });
            _paymentMethodComboBox = new ComboBox
            {
                Location = new System.Drawing.Point(1100, yPos),
                Width = 130,
                DropDownStyle = ComboBoxStyle.DropDownList
            };
            _paymentMethodComboBox.Items.AddRange(new object[]
            {
                "نقدي", "شيك", "حوالة بنكية", "بطاقة ائتمان", "بطاقة مدى", "آجل"
            });
            _paymentMethodComboBox.SelectedIndex = 0;
            topPanel.Controls.Add(_paymentMethodComboBox);

            topPanel.Controls.Add(new Label { Text = "حالة الدفع:", Location = new System.Drawing.Point(1000, yPos), AutoSize = true });
            _paymentStatusComboBox = new ComboBox
            {
                Location = new System.Drawing.Point(850, yPos),
                Width = 130,
                DropDownStyle = ComboBoxStyle.DropDownList
            };
            _paymentStatusComboBox.Items.AddRange(new object[]
            {
                "غير مدفوع", "مدفوع جزئياً", "مدفوع بالكامل", "متأخر"
            });
            _paymentStatusComboBox.SelectedIndex = 0;
            topPanel.Controls.Add(_paymentStatusComboBox);

            // Row 7 - Invoice Number and Date
            yPos += 40;
            topPanel.Controls.Add(new Label { Text = "رقم الفاتورة:", Location = new System.Drawing.Point(1250, yPos), AutoSize = true });
            _invoiceNumberTextBox = new TextBox
            {
                Location = new System.Drawing.Point(1100, yPos),
                Width = 130
            };
            topPanel.Controls.Add(_invoiceNumberTextBox);

            topPanel.Controls.Add(new Label { Text = "تاريخ الفاتورة:", Location = new System.Drawing.Point(1000, yPos), AutoSize = true });
            _invoiceDatePicker = new DateTimePicker
            {
                Location = new System.Drawing.Point(820, yPos),
                Width = 160,
                Format = DateTimePickerFormat.Short,
                Checked = false,
                ShowCheckBox = true
            };
            topPanel.Controls.Add(_invoiceDatePicker);

            topPanel.Controls.Add(new Label { Text = "موعد الاستحقاق:", Location = new System.Drawing.Point(700, yPos), AutoSize = true });
            _paymentDueDatePicker = new DateTimePicker
            {
                Location = new System.Drawing.Point(520, yPos),
                Width = 160,
                Format = DateTimePickerFormat.Short,
                Checked = false,
                ShowCheckBox = true
            };
            topPanel.Controls.Add(_paymentDueDatePicker);

            // Row 8 - Notes
            yPos += 40;
            topPanel.Controls.Add(new Label { Text = "ملاحظات:", Location = new System.Drawing.Point(1250, yPos), AutoSize = true });
            _notesTextBox = new TextBox
            {
                Location = new System.Drawing.Point(720, yPos),
                Width = 510,
                Multiline = true,
                Height = 60
            };
            topPanel.Controls.Add(_notesTextBox);

            // Buttons
            yPos += 70;
            _saveButton = new Button
            {
                Text = "حفظ التكلفة",
                Location = new System.Drawing.Point(1250, yPos),
                Width = 120,
                Height = 40,
                BackColor = ThemeManager.SecondarySkyBlue,
                ForeColor = System.Drawing.Color.White,
                FlatStyle = FlatStyle.Flat
            };
            _saveButton.Click += SaveButton_Click;
            topPanel.Controls.Add(_saveButton);

            _newButton = new Button
            {
                Text = "جديد",
                Location = new System.Drawing.Point(1120, yPos),
                Width = 120,
                Height = 40,
                BackColor = ThemeManager.SuccessGreen,
                ForeColor = System.Drawing.Color.White,
                FlatStyle = FlatStyle.Flat
            };
            _newButton.Click += (s, e) => ClearForm();
            topPanel.Controls.Add(_newButton);

            _deleteButton = new Button
            {
                Text = "حذف",
                Location = new System.Drawing.Point(990, yPos),
                Width = 120,
                Height = 40,
                BackColor = ThemeManager.ErrorRed,
                ForeColor = System.Drawing.Color.White,
                FlatStyle = FlatStyle.Flat
            };
            _deleteButton.Click += DeleteButton_Click;
            topPanel.Controls.Add(_deleteButton);

            // Bottom panel - Filter and Grid
            var filterLabel = new Label
            {
                Text = "تصفية النتائج:",
                Font = new System.Drawing.Font("Segoe UI", 10, System.Drawing.FontStyle.Bold),
                Location = new System.Drawing.Point(10, 10),
                AutoSize = true
            };
            bottomPanel.Controls.Add(filterLabel);

            bottomPanel.Controls.Add(new Label { Text = "بحث:", Location = new System.Drawing.Point(1300, 45), AutoSize = true });
            _searchTextBox = new TextBox
            {
                Location = new System.Drawing.Point(1150, 45),
                Width = 130
            };
            _searchTextBox.TextChanged += (s, e) => LoadCosts();
            bottomPanel.Controls.Add(_searchTextBox);

            bottomPanel.Controls.Add(new Label { Text = "من:", Location = new System.Drawing.Point(1100, 45), AutoSize = true });
            _filterFromDatePicker = new DateTimePicker
            {
                Location = new System.Drawing.Point(940, 45),
                Width = 150,
                Format = DateTimePickerFormat.Short
            };
            _filterFromDatePicker.Value = DateTime.Now.AddMonths(-1);
            _filterFromDatePicker.ValueChanged += (s, e) => LoadCosts();
            bottomPanel.Controls.Add(_filterFromDatePicker);

            bottomPanel.Controls.Add(new Label { Text = "إلى:", Location = new System.Drawing.Point(900, 45), AutoSize = true });
            _filterToDatePicker = new DateTimePicker
            {
                Location = new System.Drawing.Point(740, 45),
                Width = 150,
                Format = DateTimePickerFormat.Short
            };
            _filterToDatePicker.ValueChanged += (s, e) => LoadCosts();
            bottomPanel.Controls.Add(_filterToDatePicker);

            bottomPanel.Controls.Add(new Label { Text = "الفئة:", Location = new System.Drawing.Point(690, 45), AutoSize = true });
            _filterCategoryComboBox = new ComboBox
            {
                Location = new System.Drawing.Point(500, 45),
                Width = 180,
                DropDownStyle = ComboBoxStyle.DropDownList
            };
            _filterCategoryComboBox.SelectedIndexChanged += (s, e) => LoadCosts();
            bottomPanel.Controls.Add(_filterCategoryComboBox);

            _costsGrid = new DataGridView
            {
                Location = new System.Drawing.Point(10, 80),
                Width = 1360,
                Height = 180,
                AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill,
                SelectionMode = DataGridViewSelectionMode.FullRowSelect,
                MultiSelect = false,
                ReadOnly = true,
                AllowUserToAddRows = false
            };
            _costsGrid.SelectionChanged += CostsGrid_SelectionChanged;
            bottomPanel.Controls.Add(_costsGrid);

            this.Controls.Add(bottomPanel);
            this.Controls.Add(topPanel);
        }

        private void LoadCategories()
        {
            var categories = Enum.GetValues(typeof(CostCategory))
                .Cast<CostCategory>()
                .Select(c => new
                {
                    Value = c,
                    Display = GetCategoryDisplayName(c)
                })
                .ToList();

            _categoryComboBox.DataSource = categories;
            _categoryComboBox.DisplayMember = "Display";
            _categoryComboBox.ValueMember = "Value";

            // For filter
            var filterCategories = new[] { new { Value = (CostCategory?)null, Display = "-- الكل --" } }
                .Concat(categories.Select(c => new { Value = (CostCategory?)c.Value, Display = c.Display }))
                .ToList();

            _filterCategoryComboBox.DataSource = filterCategories;
            _filterCategoryComboBox.DisplayMember = "Display";
            _filterCategoryComboBox.ValueMember = "Value";
        }

        private string GetCategoryDisplayName(CostCategory category)
        {
            return category switch
            {
                CostCategory.Fingerlings => "زريعة الأسماك",
                CostCategory.Feed => "أعلاف",
                CostCategory.Medication => "أدوية بيطرية",
                CostCategory.Vitamins => "فيتامينات ومكملات",
                CostCategory.Labor_Production => "عمالة الإنتاج",
                CostCategory.Electricity => "كهرباء",
                CostCategory.Water => "مياه",
                CostCategory.Oxygen => "أكسجين",
                CostCategory.Fuel => "وقود",
                CostCategory.Pond_Maintenance => "صيانة أحواض",
                CostCategory.Equipment_Maintenance => "صيانة معدات",
                CostCategory.Equipment_Purchase => "شراء معدات",
                CostCategory.Pond_Construction => "إنشاء أحواض",
                CostCategory.Labor_Maintenance => "عمالة صيانة",
                CostCategory.Labor_Management => "رواتب إدارية",
                CostCategory.Rent => "إيجار",
                CostCategory.Insurance => "تأمين",
                CostCategory.Taxes_Fees => "ضرائب ورسوم",
                CostCategory.Certification => "تصاريح وشهادات",
                CostCategory.Transportation => "نقل وشحن",
                CostCategory.Packaging => "تعبئة وتغليف",
                CostCategory.Marketing => "تسويق وإعلان",
                CostCategory.Testing_Analysis => "فحوصات وتحاليل",
                CostCategory.Chemicals => "مواد كيميائية",
                CostCategory.Consulting => "استشارات",
                _ => "متنوعة"
            };
        }

        private void LoadCycles()
        {
            var cycles = _context.ProductionCycles
                .OrderByDescending(c => c.StartDate)
                .Select(c => new
                {
                    c.Id,
                    Display = c.Name + " (" + c.StartDate.ToString("dd/MM/yyyy") + ")"
                })
                .ToList();

            cycles.Insert(0, new { Id = 0, Display = "-- اختياري --" });

            _cycleComboBox.DataSource = cycles;
            _cycleComboBox.DisplayMember = "Display";
            _cycleComboBox.ValueMember = "Id";
        }

        private void LoadPonds()
        {
            var ponds = _context.Ponds
                .OrderBy(p => p.Name)
                .Select(p => new { p.Id, p.Name })
                .ToList();

            ponds.Insert(0, new { Id = 0, Name = "-- اختياري --" });

            _pondComboBox.DataSource = ponds;
            _pondComboBox.DisplayMember = "Name";
            _pondComboBox.ValueMember = "Id";
        }

        private void LoadSuppliers()
        {
            var suppliers = _context.Suppliers
                .Where(s => s.Status == SupplierStatus.Active)
                .OrderBy(s => s.Name)
                .Select(s => new { s.Id, s.Name })
                .ToList();

            suppliers.Insert(0, new { Id = 0, Name = "-- اختياري --" });

            _supplierComboBox.DataSource = suppliers;
            _supplierComboBox.DisplayMember = "Name";
            _supplierComboBox.ValueMember = "Id";
        }

        private void LoadCosts()
        {
            try
            {
                var query = _context.CostRecords
                    .Include(cr => cr.ProductionCycle)
                    .Include(cr => cr.Pond)
                    .Include(cr => cr.Supplier)
                    .AsQueryable();

                // Date filter
                query = query.Where(cr => cr.Date >= _filterFromDatePicker.Value.Date &&
                                         cr.Date <= _filterToDatePicker.Value.Date);

                // Category filter
                if (_filterCategoryComboBox.SelectedValue != null && _filterCategoryComboBox.SelectedValue is CostCategory category)
                {
                    query = query.Where(cr => cr.Category == category);
                }

                // Search filter
                if (!string.IsNullOrWhiteSpace(_searchTextBox?.Text))
                {
                    var searchTerm = _searchTextBox.Text.ToLower();
                    query = query.Where(cr =>
                        (cr.Description != null && cr.Description.ToLower().Contains(searchTerm)) ||
                        (cr.InvoiceNumber != null && cr.InvoiceNumber.ToLower().Contains(searchTerm)));
                }

                var costs = query
                    .OrderByDescending(cr => cr.Date)
                    .Select(cr => new
                    {
                        cr.Id,
                        التاريخ = cr.Date,
                        الفئة = cr.Category.ToString(),
                        الوصف = cr.Description,
                        الدورة = cr.ProductionCycle != null ? cr.ProductionCycle.Name : "",
                        المورد = cr.Supplier != null ? cr.Supplier.Name : "",
                        الكمية = cr.Quantity,
                        الوحدة = cr.Unit,
                        سعر_الوحدة = cr.UnitPrice,
                        المبلغ = cr.Amount,
                        حالة_الدفع = cr.PaymentStatus.ToString(),
                        رقم_الفاتورة = cr.InvoiceNumber
                    })
                    .ToList();

                _costsGrid.DataSource = costs;
                _costsGrid.Columns["Id"].Visible = false;

                // Calculate totals
                var totalAmount = costs.Sum(c => c.المبلغ);
                this.Text = $"إدارة التكاليف - إجمالي المبلغ: {totalAmount:N2} ريال";
            }
            catch (Exception ex)
            {
                MessageBox.Show($"خطأ في تحميل التكاليف: {ex.Message}", "خطأ",
                    MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void CostsGrid_SelectionChanged(object? sender, EventArgs e)
        {
            if (_costsGrid.SelectedRows.Count > 0)
            {
                var selectedRow = _costsGrid.SelectedRows[0];
                _selectedCostId = (int)selectedRow.Cells["Id"].Value;
                LoadCostDetails(_selectedCostId);
            }
        }

        private void LoadCostDetails(int costId)
        {
            try
            {
                var cost = _context.CostRecords.Find(costId);
                if (cost != null)
                {
                    _categoryComboBox.SelectedValue = cost.Category;
                    _datePicker.Value = cost.Date;
                    _cycleComboBox.SelectedValue = cost.ProductionCycleId ?? 0;
                    _pondComboBox.SelectedValue = cost.PondId ?? 0;
                    _supplierComboBox.SelectedValue = cost.SupplierId ?? 0;
                    _descriptionTextBox.Text = cost.Description;
                    _quantityNumeric.Value = cost.Quantity ?? 0m;
                    _unitComboBox.Text = cost.Unit;
                    _unitPriceNumeric.Value = cost.UnitPrice ?? 0m;
                    _paymentMethodComboBox.SelectedIndex = (int)cost.PaymentMethod - 1;
                    _paymentStatusComboBox.SelectedIndex = (int)cost.PaymentStatus - 1;
                    _invoiceNumberTextBox.Text = cost.InvoiceNumber;
                    
                    if (cost.InvoiceDate.HasValue)
                    {
                        _invoiceDatePicker.Checked = true;
                        _invoiceDatePicker.Value = cost.InvoiceDate.Value;
                    }
                    else
                    {
                        _invoiceDatePicker.Checked = false;
                    }

                    if (cost.PaymentDueDate.HasValue)
                    {
                        _paymentDueDatePicker.Checked = true;
                        _paymentDueDatePicker.Value = cost.PaymentDueDate.Value;
                    }
                    else
                    {
                        _paymentDueDatePicker.Checked = false;
                    }

                    _notesTextBox.Text = cost.Notes;
                    
                    CalculateTotal();
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"خطأ في تحميل تفاصيل التكلفة: {ex.Message}", "خطأ",
                    MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void CalculateTotal()
        {
            var quantity = (double)_quantityNumeric.Value;
            var unitPrice = _unitPriceNumeric.Value;
            var total = (decimal)quantity * unitPrice;

            _totalLabel.Text = $"{total:N2} ريال";
        }

        private void UpdateCategoryHelp()
        {
            // Could add help text or auto-fill based on category
        }

        private void SaveButton_Click(object? sender, EventArgs e)
        {
            try
            {
                if (_categoryComboBox.SelectedValue == null)
                {
                    MessageBox.Show("يرجى اختيار فئة التكلفة", "تنبيه",
                        MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    return;
                }

                if (string.IsNullOrWhiteSpace(_descriptionTextBox.Text))
                {
                    MessageBox.Show("يرجى إدخال وصف التكلفة", "تنبيه",
                        MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    return;
                }

                CostRecord? cost;
                if (_selectedCostId > 0)
                {
                    cost = _context.CostRecords.Find(_selectedCostId);
                    if (cost == null) return;

                    // Update supplier balance (reverse old amount)
                    if (cost.SupplierId.HasValue && cost.PaymentStatus != PaymentStatus.Paid)
                    {
                        UpdateSupplierBalance(cost.SupplierId.Value, -cost.Amount);
                    }
                }
                else
                {
                    cost = new CostRecord
                    {
                        CreatedAt = DateTime.Now,
                        RecordedBy = Environment.UserName
                    };
                    _context.CostRecords.Add(cost);
                }

                if (_categoryComboBox.SelectedValue is CostCategory cat)
                    cost!.Category = cat;
                cost!.Date = _datePicker.Value;
                
                var cycleId = _cycleComboBox.SelectedValue is int cId ? cId : 0;
                cost!.ProductionCycleId = cycleId > 0 ? cycleId : null;
                
                var pondId = _pondComboBox.SelectedValue is int pId ? pId : 0;
                cost!.PondId = pondId > 0 ? pondId : null;
                
                var supplierId = _supplierComboBox.SelectedValue is int sId ? sId : 0;
                cost!.SupplierId = supplierId > 0 ? supplierId : null;
                
                cost!.Description = _descriptionTextBox.Text;
                cost!.Quantity = _quantityNumeric.Value > 0 ? (decimal?)_quantityNumeric.Value : null;
                cost!.Unit = _unitComboBox.Text;
                cost!.UnitPrice = _unitPriceNumeric.Value;
                cost!.Amount = (cost.Quantity ?? 0m) * (cost.UnitPrice ?? 0m);
                cost!.PaymentMethod = (PaymentMethod)(_paymentMethodComboBox.SelectedIndex + 1);
                cost!.PaymentStatus = (PaymentStatus)(_paymentStatusComboBox.SelectedIndex + 1);
                cost!.InvoiceNumber = _invoiceNumberTextBox.Text;
                cost!.InvoiceDate = _invoiceDatePicker.Checked ? _invoiceDatePicker.Value : null;
                cost!.PaymentDueDate = _paymentDueDatePicker.Checked ? _paymentDueDatePicker.Value : null;
                cost!.Notes = _notesTextBox.Text;

                // Update supplier balance (add new amount if unpaid)
                if (cost.SupplierId.HasValue && cost.PaymentStatus != PaymentStatus.Paid)
                {
                    UpdateSupplierBalance(cost.SupplierId.Value, cost.Amount);
                }

                _context.SaveChanges();

                MessageBox.Show("تم حفظ التكلفة بنجاح", "نجح",
                    MessageBoxButtons.OK, MessageBoxIcon.Information);

                LoadCosts();
                ClearForm();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"خطأ في حفظ البيانات: {ex.Message}", "خطأ",
                    MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void DeleteButton_Click(object? sender, EventArgs e)
        {
            if (_selectedCostId == 0)
            {
                MessageBox.Show("يرجى اختيار تكلفة للحذف", "تنبيه",
                    MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            var result = MessageBox.Show("هل أنت متأكد من حذف هذه التكلفة؟",
                "تأكيد الحذف", MessageBoxButtons.YesNo, MessageBoxIcon.Question);

            if (result == DialogResult.Yes)
            {
                try
                {
                    var cost = _context.CostRecords.Find(_selectedCostId);
                    if (cost != null)
                    {
                        // Update supplier balance
                        if (cost.SupplierId.HasValue && cost.PaymentStatus != PaymentStatus.Paid)
                        {
                            UpdateSupplierBalance(cost.SupplierId.Value, -cost.Amount);
                        }

                        _context.CostRecords.Remove(cost);
                        _context.SaveChanges();

                        MessageBox.Show("تم حذف التكلفة بنجاح", "نجح",
                            MessageBoxButtons.OK, MessageBoxIcon.Information);

                        LoadCosts();
                        ClearForm();
                    }
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"خطأ في حذف البيانات: {ex.Message}", "خطأ",
                        MessageBoxButtons.OK, MessageBoxIcon.Error);
                }
            }
        }

        private void UpdateSupplierBalance(int supplierId, decimal amount)
        {
            var supplier = _context.Suppliers.Find(supplierId);
            if (supplier != null)
            {
                supplier.CurrentBalance += amount;
            }
        }

        private void ClearForm()
        {
            _selectedCostId = 0;
            _categoryComboBox.SelectedIndex = 0;
            _datePicker.Value = DateTime.Now;
            _cycleComboBox.SelectedIndex = 0;
            _pondComboBox.SelectedIndex = 0;
            _supplierComboBox.SelectedIndex = 0;
            _descriptionTextBox.Clear();
            _quantityNumeric.Value = 0;
            _unitComboBox.SelectedIndex = 0;
            _unitPriceNumeric.Value = 0;
            _paymentMethodComboBox.SelectedIndex = 0;
            _paymentStatusComboBox.SelectedIndex = 0;
            _invoiceNumberTextBox.Clear();
            _invoiceDatePicker.Checked = false;
            _paymentDueDatePicker.Checked = false;
            _notesTextBox.Clear();
            _totalLabel.Text = "0.00 ريال";
        }
    }
}