using System;
using System.Linq;
using System.Windows.Forms;
using FishFarmManager.Data;
using FishFarmManager.Models;
using FishFarmManager.Controls;
using FishFarmManager.Services;
using Microsoft.EntityFrameworkCore;

namespace FishFarmManager.Forms
{
    /// <summary>
    /// نموذج إدارة الموردين
    /// Supplier management form
    /// </summary>
    public partial class SupplierForm : Form
    {
        private readonly FishFarmContext _context;
        private readonly AutocompleteService _autocompleteService;
        private int _selectedSupplierId = 0;

        // Controls
        private SmartAutoCompleteTextBox _nameTextBox = null!;
        private ComboBox _typeComboBox = null!;
        private TextBox _phoneTextBox = null!;
        private TextBox _emailTextBox = null!;
        private TextBox _addressTextBox = null!;
        private TextBox _taxNumberTextBox = null!;
        private TextBox _contactPersonTextBox = null!;
        private NumericUpDown _paymentTermDaysNumeric = null!;
        private NumericUpDown _creditLimitNumeric = null!;
        private Label _currentBalanceLabel = null!;
        private ComboBox _statusComboBox = null!;
        private TextBox _notesTextBox = null!;
        private DataGridView _suppliersGrid = null!;
        private Button _saveButton = null!;
        private Button _deleteButton = null!;
        private Button _newButton = null!;
        private TextBox _searchTextBox = null!;
        private ComboBox _filterTypeComboBox = null!;

        public SupplierForm(FishFarmContext context)
        {
            _context = context;
            _autocompleteService = new AutocompleteService(_context);
            InitializeComponent();
            LoadSuppliers();
        }

        private void InitializeComponent()
        {
            this.Text = "إدارة الموردين - Supplier Management";
            this.Size = new System.Drawing.Size(1400, 750);
            this.RightToLeft = RightToLeft.Yes;
            this.RightToLeftLayout = true;
            this.StartPosition = FormStartPosition.CenterScreen;

            var topPanel = new Panel
            {
                Dock = DockStyle.Top,
                Height = 450,
                Padding = new Padding(10)
            };

            var bottomPanel = new Panel
            {
                Dock = DockStyle.Fill,
                Padding = new Padding(10)
            };

            // Title
            var titleLabel = new Label
            {
                Text = "معلومات المورد",
                Font = new System.Drawing.Font("Segoe UI", 12, System.Drawing.FontStyle.Bold),
                Location = new System.Drawing.Point(10, 10),
                AutoSize = true
            };
            topPanel.Controls.Add(titleLabel);

            int yPos = 50;

            // Row 1 - Name
            topPanel.Controls.Add(new Label { Text = "اسم المورد:", Location = new System.Drawing.Point(1250, yPos), AutoSize = true });
            _nameTextBox = new SmartAutoCompleteTextBox(_autocompleteService)
            {
                Location = new System.Drawing.Point(900, yPos),
                Width = 330,
                EntityType = "Supplier",
                SearchField = "All",
                MinCharacters = 2,
                AutoFillDetails = true,
                RecordUsage = true
            };
            _nameTextBox.DataFilled += NameTextBox_DataFilled;
            topPanel.Controls.Add(_nameTextBox);

            // Row 2 - Type and Phone
            yPos += 40;
            topPanel.Controls.Add(new Label { Text = "نوع المورد:", Location = new System.Drawing.Point(1250, yPos), AutoSize = true });
            _typeComboBox = new ComboBox
            {
                Location = new System.Drawing.Point(1050, yPos),
                Width = 180,
                DropDownStyle = ComboBoxStyle.DropDownList
            };
            _typeComboBox.Items.AddRange(new object[]
            {
                "موردو زريعة الأسماك",
                "موردو الأعلاف",
                "موردو الأدوية والعلاجات",
                "موردو المعدات",
                "الصيانة والإصلاح",
                "النقل والشحن",
                "التعبئة والتغليف",
                "الاستشارات الفنية",
                "موردون متنوعون"
            });
            _typeComboBox.SelectedIndex = 0;
            topPanel.Controls.Add(_typeComboBox);

            topPanel.Controls.Add(new Label { Text = "الهاتف:", Location = new System.Drawing.Point(950, yPos), AutoSize = true });
            _phoneTextBox = new TextBox
            {
                Location = new System.Drawing.Point(750, yPos),
                Width = 180
            };
            topPanel.Controls.Add(_phoneTextBox);

            // Row 3 - Email and Tax Number
            yPos += 40;
            topPanel.Controls.Add(new Label { Text = "البريد الإلكتروني:", Location = new System.Drawing.Point(1250, yPos), AutoSize = true });
            _emailTextBox = new TextBox
            {
                Location = new System.Drawing.Point(1050, yPos),
                Width = 180
            };
            topPanel.Controls.Add(_emailTextBox);

            topPanel.Controls.Add(new Label { Text = "الرقم الضريبي:", Location = new System.Drawing.Point(950, yPos), AutoSize = true });
            _taxNumberTextBox = new TextBox
            {
                Location = new System.Drawing.Point(750, yPos),
                Width = 180
            };
            topPanel.Controls.Add(_taxNumberTextBox);

            // Row 4 - Contact Person
            yPos += 40;
            topPanel.Controls.Add(new Label { Text = "جهة الاتصال:", Location = new System.Drawing.Point(1250, yPos), AutoSize = true });
            _contactPersonTextBox = new TextBox
            {
                Location = new System.Drawing.Point(900, yPos),
                Width = 330
            };
            topPanel.Controls.Add(_contactPersonTextBox);

            // Row 5 - Address
            yPos += 40;
            topPanel.Controls.Add(new Label { Text = "العنوان:", Location = new System.Drawing.Point(1250, yPos), AutoSize = true });
            _addressTextBox = new TextBox
            {
                Location = new System.Drawing.Point(750, yPos),
                Width = 480,
                Multiline = true,
                Height = 50
            };
            topPanel.Controls.Add(_addressTextBox);

            // Row 6 - Payment Terms, Credit Limit, Balance
            yPos += 60;
            topPanel.Controls.Add(new Label { Text = "مدة السداد (يوم):", Location = new System.Drawing.Point(1250, yPos), AutoSize = true });
            _paymentTermDaysNumeric = new NumericUpDown
            {
                Location = new System.Drawing.Point(1100, yPos),
                Width = 130,
                Minimum = 0,
                Maximum = 365,
                Value = 30
            };
            topPanel.Controls.Add(_paymentTermDaysNumeric);

            topPanel.Controls.Add(new Label { Text = "الحد الائتماني:", Location = new System.Drawing.Point(1000, yPos), AutoSize = true });
            _creditLimitNumeric = new NumericUpDown
            {
                Location = new System.Drawing.Point(820, yPos),
                Width = 160,
                DecimalPlaces = 2,
                Maximum = 9999999,
                ThousandsSeparator = true
            };
            topPanel.Controls.Add(_creditLimitNumeric);

            topPanel.Controls.Add(new Label { Text = "الرصيد الحالي:", Location = new System.Drawing.Point(720, yPos), AutoSize = true });
            _currentBalanceLabel = new Label
            {
                Text = "0.00 ريال",
                Location = new System.Drawing.Point(560, yPos),
                Width = 150,
                Font = new System.Drawing.Font("Segoe UI", 9, System.Drawing.FontStyle.Bold),
                ForeColor = System.Drawing.Color.DarkBlue,
                TextAlign = System.Drawing.ContentAlignment.MiddleRight
            };
            topPanel.Controls.Add(_currentBalanceLabel);

            // Row 7 - Status
            yPos += 40;
            topPanel.Controls.Add(new Label { Text = "الحالة:", Location = new System.Drawing.Point(1250, yPos), AutoSize = true });
            _statusComboBox = new ComboBox
            {
                Location = new System.Drawing.Point(1100, yPos),
                Width = 130,
                DropDownStyle = ComboBoxStyle.DropDownList
            };
            _statusComboBox.Items.AddRange(new object[] { "نشط", "معلق", "غير نشط" });
            _statusComboBox.SelectedIndex = 0;
            topPanel.Controls.Add(_statusComboBox);

            // Row 8 - Notes
            yPos += 40;
            topPanel.Controls.Add(new Label { Text = "ملاحظات:", Location = new System.Drawing.Point(1250, yPos), AutoSize = true });
            _notesTextBox = new TextBox
            {
                Location = new System.Drawing.Point(750, yPos),
                Width = 480,
                Multiline = true,
                Height = 60
            };
            topPanel.Controls.Add(_notesTextBox);

            // Buttons
            yPos += 70;
            _saveButton = new Button
            {
                Text = "حفظ",
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
                Text = "قائمة الموردين:",
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
            _searchTextBox.TextChanged += (s, e) => LoadSuppliers();
            bottomPanel.Controls.Add(_searchTextBox);

            bottomPanel.Controls.Add(new Label { Text = "النوع:", Location = new System.Drawing.Point(1100, 45), AutoSize = true });
            _filterTypeComboBox = new ComboBox
            {
                Location = new System.Drawing.Point(900, 45),
                Width = 180,
                DropDownStyle = ComboBoxStyle.DropDownList
            };
            _filterTypeComboBox.Items.Add("-- الكل --");
            _filterTypeComboBox.Items.AddRange(new object[]
            {
                "موردو زريعة الأسماك",
                "موردو الأعلاف",
                "موردو الأدوية والعلاجات",
                "موردو المعدات",
                "الصيانة والإصلاح",
                "النقل والشحن",
                "التعبئة والتغليف",
                "الاستشارات الفنية",
                "موردون متنوعون"
            });
            _filterTypeComboBox.SelectedIndex = 0;
            _filterTypeComboBox.SelectedIndexChanged += (s, e) => LoadSuppliers();
            bottomPanel.Controls.Add(_filterTypeComboBox);

            _suppliersGrid = new DataGridView
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
            _suppliersGrid.SelectionChanged += SuppliersGrid_SelectionChanged;
            bottomPanel.Controls.Add(_suppliersGrid);

            this.Controls.Add(bottomPanel);
            this.Controls.Add(topPanel);
        }

        private void LoadSuppliers()
        {
            try
            {
                var query = _context.Suppliers.AsQueryable();

                // Type filter
                if (_filterTypeComboBox.SelectedIndex > 0)
                {
                    var selectedType = (SupplierType)(_filterTypeComboBox.SelectedIndex - 1);
                    query = query.Where(s => s.Type == selectedType);
                }

                // Search filter
                if (!string.IsNullOrWhiteSpace(_searchTextBox?.Text))
                {
                    var searchTerm = _searchTextBox.Text.ToLower();
                    query = query.Where(s =>
                        (s.Name != null && s.Name.ToLower().Contains(searchTerm)) ||
                        (s.Phone != null && s.Phone.ToLower().Contains(searchTerm)) ||
                        (s.ContactPerson != null && s.ContactPerson.ToLower().Contains(searchTerm)));
                }

                var suppliers = query
                    .OrderBy(s => s.Name)
                    .Select(s => new
                    {
                        s.Id,
                        الاسم = s.Name,
                        النوع = s.Type.ToString(),
                        الهاتف = s.Phone,
                        البريد = s.Email,
                        جهة_الاتصال = s.ContactPerson,
                        مدة_السداد = s.PaymentTermDays + " يوم",
                        الحد_الائتماني = s.CreditLimit,
                        الرصيد_الحالي = s.CurrentBalance,
                        الحالة = s.Status.ToString(),
                        آخر_شراء = s.LastPurchaseDate
                    })
                    .ToList();

                _suppliersGrid.DataSource = suppliers;
                _suppliersGrid.Columns["Id"].Visible = false;

                // Format currency columns
                if (_suppliersGrid.Columns.Contains("الحد_الائتماني"))
                    _suppliersGrid.Columns["الحد_الائتماني"].DefaultCellStyle.Format = "N2";
                
                if (_suppliersGrid.Columns.Contains("الرصيد_الحالي"))
                {
                    _suppliersGrid.Columns["الرصيد_الحالي"].DefaultCellStyle.Format = "N2";
                    _suppliersGrid.Columns["الرصيد_الحالي"].DefaultCellStyle.ForeColor = System.Drawing.Color.DarkRed;
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"خطأ في تحميل الموردين: {ex.Message}", "خطأ",
                    MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void SuppliersGrid_SelectionChanged(object? sender, EventArgs e)
        {
            if (_suppliersGrid.SelectedRows.Count > 0)
            {
                var selectedRow = _suppliersGrid.SelectedRows[0];
                _selectedSupplierId = (int)selectedRow.Cells["Id"].Value;
                LoadSupplierDetails(_selectedSupplierId);
            }
        }

        private void LoadSupplierDetails(int supplierId)
        {
            try
            {
                var supplier = _context.Suppliers.Find(supplierId);
                if (supplier != null)
                {
                    _nameTextBox.Text = supplier.Name;
                    _typeComboBox.SelectedIndex = (int)supplier.Type;
                    _phoneTextBox.Text = supplier.Phone;
                    _emailTextBox.Text = supplier.Email;
                    _addressTextBox.Text = supplier.Address;
                    _taxNumberTextBox.Text = supplier.TaxNumber;
                    _contactPersonTextBox.Text = supplier.ContactPerson;
                    _paymentTermDaysNumeric.Value = supplier.PaymentTermDays;
                    _creditLimitNumeric.Value = supplier.CreditLimit;
                    _currentBalanceLabel.Text = $"{supplier.CurrentBalance:N2} ريال";
                    _currentBalanceLabel.ForeColor = supplier.CurrentBalance > 0 ? 
                        System.Drawing.Color.DarkRed : System.Drawing.Color.DarkGreen;
                    _statusComboBox.SelectedIndex = (int)supplier.Status;
                    _notesTextBox.Text = supplier.Notes;
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"خطأ في تحميل تفاصيل المورد: {ex.Message}", "خطأ",
                    MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void SaveButton_Click(object? sender, EventArgs e)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(_nameTextBox.Text))
                {
                    MessageBox.Show("يرجى إدخال اسم المورد", "تنبيه",
                        MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    return;
                }

                if (string.IsNullOrWhiteSpace(_phoneTextBox.Text))
                {
                    MessageBox.Show("يرجى إدخال رقم الهاتف", "تنبيه",
                        MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    return;
                }

                Supplier? supplier;
                if (_selectedSupplierId > 0)
                {
                    supplier = _context.Suppliers.Find(_selectedSupplierId);
                    if (supplier == null) return;
                }
                else
                {
                    supplier = new Supplier
                    {
                        CreatedAt = DateTime.Now,
                        CurrentBalance = 0
                    };
                    _context.Suppliers.Add(supplier);
                }

                supplier!.Name = _nameTextBox.Text;
                supplier!.Type = (SupplierType)_typeComboBox.SelectedIndex;
                supplier!.Phone = _phoneTextBox.Text;
                supplier!.Email = _emailTextBox.Text;
                supplier!.Address = _addressTextBox.Text;
                supplier!.TaxNumber = _taxNumberTextBox.Text;
                supplier!.ContactPerson = _contactPersonTextBox.Text;
                supplier!.PaymentTermDays = (int)_paymentTermDaysNumeric.Value;
                supplier!.CreditLimit = _creditLimitNumeric.Value;
                supplier!.Status = (SupplierStatus)_statusComboBox.SelectedIndex;
                supplier!.Notes = _notesTextBox.Text;

                _context.SaveChanges();

                MessageBox.Show("تم حفظ المورد بنجاح", "نجح",
                    MessageBoxButtons.OK, MessageBoxIcon.Information);

                LoadSuppliers();
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
            if (_selectedSupplierId == 0)
            {
                MessageBox.Show("يرجى اختيار مورد للحذف", "تنبيه",
                    MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            var result = MessageBox.Show("هل أنت متأكد من حذف هذا المورد؟\nملاحظة: لن يتم حذف المورد إذا كان لديه تكاليف مسجلة.",
                "تأكيد الحذف", MessageBoxButtons.YesNo, MessageBoxIcon.Question);

            if (result == DialogResult.Yes)
            {
                try
                {
                    var supplier = _context.Suppliers
                        .Include(s => s.CostRecords)
                        .FirstOrDefault(s => s.Id == _selectedSupplierId);

                    if (supplier != null)
                    {
                        if (supplier.CostRecords.Any())
                        {
                            MessageBox.Show("لا يمكن حذف المورد لأنه مرتبط بتكاليف مسجلة", "تنبيه",
                                MessageBoxButtons.OK, MessageBoxIcon.Warning);
                            return;
                        }

                        _context.Suppliers.Remove(supplier);
                        _context.SaveChanges();

                        MessageBox.Show("تم حذف المورد بنجاح", "نجح",
                            MessageBoxButtons.OK, MessageBoxIcon.Information);

                        LoadSuppliers();
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

        private void ClearForm()
        {
            _selectedSupplierId = 0;
            _nameTextBox.ClearSelection();
            _typeComboBox.SelectedIndex = 0;
            _phoneTextBox.Clear();
            _emailTextBox.Clear();
            _addressTextBox.Clear();
            _taxNumberTextBox.Clear();
            _contactPersonTextBox.Clear();
            _paymentTermDaysNumeric.Value = 30;
            _creditLimitNumeric.Value = 0;
            _currentBalanceLabel.Text = "0.00 ريال";
            _currentBalanceLabel.ForeColor = System.Drawing.Color.DarkBlue;
            _statusComboBox.SelectedIndex = 0;
            _notesTextBox.Clear();
        }

        /// <summary>
        /// معالج حدث تعبئة البيانات تلقائياً من الإكمال الذكي
        /// </summary>
        private void NameTextBox_DataFilled(object? sender, AutocompleteResult e)
        {
            try
            {
                if (e.AdditionalData.ContainsKey("Id"))
                {
                    int supplierId = Convert.ToInt32(e.AdditionalData["Id"]);
                    _selectedSupplierId = supplierId;

                    // تعبئة الحقول من البيانات الإضافية
                    if (e.AdditionalData.ContainsKey("ContactPerson"))
                        _contactPersonTextBox.Text = e.AdditionalData["ContactPerson"]?.ToString() ?? "";

                    if (e.AdditionalData.ContainsKey("Phone"))
                        _phoneTextBox.Text = e.AdditionalData["Phone"]?.ToString() ?? "";

                    if (e.AdditionalData.ContainsKey("Email"))
                        _emailTextBox.Text = e.AdditionalData["Email"]?.ToString() ?? "";

                    if (e.AdditionalData.ContainsKey("Address"))
                        _addressTextBox.Text = e.AdditionalData["Address"]?.ToString() ?? "";

                    if (e.AdditionalData.ContainsKey("TaxNumber"))
                        _taxNumberTextBox.Text = e.AdditionalData["TaxNumber"]?.ToString() ?? "";

                    // تعيين النوع
                    if (e.AdditionalData.ContainsKey("Type"))
                    {
                        string typeStr = e.AdditionalData["Type"]?.ToString() ?? "";
                        if (Enum.TryParse<SupplierType>(typeStr, out var supplierType))
                        {
                            int typeIndex = (int)supplierType;
                            if (typeIndex >= 0 && typeIndex < _typeComboBox.Items.Count)
                                _typeComboBox.SelectedIndex = typeIndex;
                        }
                    }

                    // تحميل باقي التفاصيل من قاعدة البيانات
                    var supplier = _context.Suppliers.Find(supplierId);
                    if (supplier != null)
                    {
                        _paymentTermDaysNumeric.Value = supplier.PaymentTermDays;
                        _creditLimitNumeric.Value = supplier.CreditLimit;
                        
                        int statusIndex = (int)supplier.Status;
                        if (statusIndex >= 0 && statusIndex < _statusComboBox.Items.Count)
                            _statusComboBox.SelectedIndex = statusIndex;
                        
                        _notesTextBox.Text = supplier.Notes ?? "";
                        
                        // عرض الرصيد الحالي
                        _currentBalanceLabel.Text = $"{supplier.CurrentBalance:N2} ريال";
                        _currentBalanceLabel.ForeColor = supplier.CurrentBalance > 0 
                            ? System.Drawing.Color.Red 
                            : System.Drawing.Color.DarkGreen;
                    }
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error in NameTextBox_DataFilled: {ex.Message}");
            }
        }
}
}