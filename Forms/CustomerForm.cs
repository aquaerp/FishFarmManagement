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
    /// نموذج إدارة العملاء
    /// Form for managing customers
    /// </summary>
    public partial class CustomerForm : Form
    {
        private readonly FishFarmContext _context;
        private readonly AutocompleteService _autocompleteService;
        private int _selectedCustomerId = 0;

        // Controls
        private SmartAutoCompleteTextBox _nameTextBox = null!;
        private ComboBox _typeComboBox = null!;
        private TextBox _phoneTextBox = null!;
        private TextBox _emailTextBox = null!;
        private TextBox _addressTextBox = null!;
        private TextBox _taxNumberTextBox = null!;
        private TextBox _commercialRegTextBox = null!;
        private NumericUpDown _creditLimitNumeric = null!;
        private NumericUpDown _paymentTermNumeric = null!;
        private ComboBox _statusComboBox = null!;
        private TextBox _notesTextBox = null!;
        private DataGridView _customersGrid = null!;
        private Button _saveButton = null!;
        private Button _deleteButton = null!;
        private Button _newButton = null!;
        private TextBox _searchTextBox = null!;

        public CustomerForm(FishFarmContext context)
        {
            _context = context;
            _autocompleteService = new AutocompleteService(_context);
            InitializeComponent();
            LoadCustomers();
        }

        private void InitializeComponent()
        {
            this.Text = "إدارة العملاء - Customer Management";
            this.Size = new System.Drawing.Size(1200, 700);
            this.RightToLeft = RightToLeft.Yes;
            this.RightToLeftLayout = true;
            this.StartPosition = FormStartPosition.CenterScreen;

            // Create panels
            var topPanel = new Panel
            {
                Dock = DockStyle.Top,
                Height = 350,
                Padding = new Padding(10)
            };

            var bottomPanel = new Panel
            {
                Dock = DockStyle.Fill,
                Padding = new Padding(10)
            };

            // Top panel - Customer details
            var titleLabel = new Label
            {
                Text = "بيانات العميل",
                Font = new System.Drawing.Font("Segoe UI", 12, System.Drawing.FontStyle.Bold),
                Location = new System.Drawing.Point(10, 10),
                AutoSize = true
            };
            topPanel.Controls.Add(titleLabel);

            // Row 1
            int yPos = 50;
            topPanel.Controls.Add(new Label { Text = "اسم العميل:", Location = new System.Drawing.Point(1000, yPos), AutoSize = true });
            _nameTextBox = new SmartAutoCompleteTextBox(_autocompleteService)
            {
                Location = new System.Drawing.Point(650, yPos),
                Width = 300,
                EntityType = "Customer",
                SearchField = "All",
                MinCharacters = 2,
                AutoFillDetails = true,
                RecordUsage = true
            };
            _nameTextBox.DataFilled += NameTextBox_DataFilled;
            topPanel.Controls.Add(_nameTextBox);

            topPanel.Controls.Add(new Label { Text = "النوع:", Location = new System.Drawing.Point(550, yPos), AutoSize = true });
            _typeComboBox = new ComboBox
            {
                Location = new System.Drawing.Point(350, yPos),
                Width = 180,
                DropDownStyle = ComboBoxStyle.DropDownList
            };
            _typeComboBox.Items.AddRange(new object[]
            {
                "قطاعي",
                "جملة",
                "مطعم",
                "فندق",
                "سوبر ماركت",
                "مصدر",
                "أخرى"
            });
            _typeComboBox.SelectedIndex = 0;
            topPanel.Controls.Add(_typeComboBox);

            topPanel.Controls.Add(new Label { Text = "الحالة:", Location = new System.Drawing.Point(250, yPos), AutoSize = true });
            _statusComboBox = new ComboBox
            {
                Location = new System.Drawing.Point(100, yPos),
                Width = 130,
                DropDownStyle = ComboBoxStyle.DropDownList
            };
            _statusComboBox.Items.AddRange(new object[] { "نشط", "معلق", "غير نشط" });
            _statusComboBox.SelectedIndex = 0;
            topPanel.Controls.Add(_statusComboBox);

            // Row 2
            yPos += 40;
            topPanel.Controls.Add(new Label { Text = "الهاتف:", Location = new System.Drawing.Point(1000, yPos), AutoSize = true });
            _phoneTextBox = new TextBox { Location = new System.Drawing.Point(800, yPos), Width = 180 };
            topPanel.Controls.Add(_phoneTextBox);

            topPanel.Controls.Add(new Label { Text = "البريد الإلكتروني:", Location = new System.Drawing.Point(680, yPos), AutoSize = true });
            _emailTextBox = new TextBox { Location = new System.Drawing.Point(400, yPos), Width = 260 };
            topPanel.Controls.Add(_emailTextBox);

            // Row 3
            yPos += 40;
            topPanel.Controls.Add(new Label { Text = "العنوان:", Location = new System.Drawing.Point(1000, yPos), AutoSize = true });
            _addressTextBox = new TextBox { Location = new System.Drawing.Point(400, yPos), Width = 580, Multiline = true, Height = 60 };
            topPanel.Controls.Add(_addressTextBox);

            // Row 4
            yPos += 70;
            topPanel.Controls.Add(new Label { Text = "الرقم الضريبي:", Location = new System.Drawing.Point(1000, yPos), AutoSize = true });
            _taxNumberTextBox = new TextBox { Location = new System.Drawing.Point(800, yPos), Width = 180 };
            topPanel.Controls.Add(_taxNumberTextBox);

            topPanel.Controls.Add(new Label { Text = "السجل التجاري:", Location = new System.Drawing.Point(680, yPos), AutoSize = true });
            _commercialRegTextBox = new TextBox { Location = new System.Drawing.Point(500, yPos), Width = 160 };
            topPanel.Controls.Add(_commercialRegTextBox);

            topPanel.Controls.Add(new Label { Text = "حد الائتمان:", Location = new System.Drawing.Point(380, yPos), AutoSize = true });
            _creditLimitNumeric = new NumericUpDown
            {
                Location = new System.Drawing.Point(200, yPos),
                Width = 160,
                Maximum = 9999999,
                DecimalPlaces = 2,
                ThousandsSeparator = true
            };
            topPanel.Controls.Add(_creditLimitNumeric);

            // Row 5
            yPos += 40;
            topPanel.Controls.Add(new Label { Text = "مدة السداد (أيام):", Location = new System.Drawing.Point(1000, yPos), AutoSize = true });
            _paymentTermNumeric = new NumericUpDown
            {
                Location = new System.Drawing.Point(850, yPos),
                Width = 130,
                Maximum = 365,
                Value = 30
            };
            topPanel.Controls.Add(_paymentTermNumeric);

            topPanel.Controls.Add(new Label { Text = "ملاحظات:", Location = new System.Drawing.Point(750, yPos), AutoSize = true });
            _notesTextBox = new TextBox { Location = new System.Drawing.Point(400, yPos), Width = 330, Multiline = true, Height = 40 };
            topPanel.Controls.Add(_notesTextBox);

            // Buttons
            yPos += 50;
            _saveButton = new Button
            {
                Text = "حفظ",
                Location = new System.Drawing.Point(950, yPos),
                Width = 100,
                Height = 35,
                BackColor = ThemeManager.SecondarySkyBlue,
                ForeColor = System.Drawing.Color.White,
                FlatStyle = FlatStyle.Flat
            };
            _saveButton.Click += SaveButton_Click;
            topPanel.Controls.Add(_saveButton);

            _newButton = new Button
            {
                Text = "جديد",
                Location = new System.Drawing.Point(840, yPos),
                Width = 100,
                Height = 35,
                BackColor = ThemeManager.SuccessGreen,
                ForeColor = System.Drawing.Color.White,
                FlatStyle = FlatStyle.Flat
            };
            _newButton.Click += (s, e) => ClearForm();
            topPanel.Controls.Add(_newButton);

            _deleteButton = new Button
            {
                Text = "حذف",
                Location = new System.Drawing.Point(730, yPos),
                Width = 100,
                Height = 35,
                BackColor = ThemeManager.ErrorRed,
                ForeColor = System.Drawing.Color.White,
                FlatStyle = FlatStyle.Flat
            };
            _deleteButton.Click += DeleteButton_Click;
            topPanel.Controls.Add(_deleteButton);

            // Bottom panel - Grid
            var searchLabel = new Label
            {
                Text = "بحث:",
                Location = new System.Drawing.Point(1000, 10),
                AutoSize = true
            };
            bottomPanel.Controls.Add(searchLabel);

            _searchTextBox = new TextBox
            {
                Location = new System.Drawing.Point(800, 10),
                Width = 180
            };
            _searchTextBox.TextChanged += (s, e) => LoadCustomers();
            bottomPanel.Controls.Add(_searchTextBox);

            _customersGrid = new DataGridView
            {
                Location = new System.Drawing.Point(10, 45),
                Width = 1150,
                Height = 250,
                AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill,
                SelectionMode = DataGridViewSelectionMode.FullRowSelect,
                MultiSelect = false,
                ReadOnly = true,
                AllowUserToAddRows = false,
                AllowUserToDeleteRows = false
            };
            _customersGrid.SelectionChanged += CustomersGrid_SelectionChanged;
            bottomPanel.Controls.Add(_customersGrid);

            this.Controls.Add(bottomPanel);
            this.Controls.Add(topPanel);
        }

        private void LoadCustomers()
        {
            try
            {
                var query = _context.Customers.AsQueryable();

                if (!string.IsNullOrWhiteSpace(_searchTextBox?.Text))
                {
                    var searchTerm = _searchTextBox.Text.ToLower();
                    query = query.Where(c =>
                        (c.Name != null && c.Name.ToLower().Contains(searchTerm)) ||
                        (c.Phone != null && c.Phone.Contains(searchTerm)) ||
                        (c.Email != null && c.Email.ToLower().Contains(searchTerm)));
                }

                var customers = query
                    .OrderByDescending(c => c.CreatedAt)
                    .Select(c => new
                    {
                        c.Id,
                        الاسم = c.Name,
                        النوع = c.Type.ToString(),
                        الهاتف = c.Phone,
                        البريد = c.Email,
                        الرصيد = c.CurrentBalance,
                        الحالة = c.Status.ToString(),
                        تاريخ_الإنشاء = c.CreatedAt
                    })
                    .ToList();

                _customersGrid.DataSource = customers;
                _customersGrid.Columns["Id"].Visible = false;
            }
            catch (Exception ex)
            {
                MessageBox.Show($"خطأ في تحميل البيانات: {ex.Message}", "خطأ",
                    MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void CustomersGrid_SelectionChanged(object? sender, EventArgs e)
        {
            if (_customersGrid.SelectedRows.Count > 0)
            {
                var selectedRow = _customersGrid.SelectedRows[0];
                _selectedCustomerId = (int)selectedRow.Cells["Id"].Value;
                LoadCustomerDetails(_selectedCustomerId);
            }
        }

        private void LoadCustomerDetails(int customerId)
        {
            var customer = _context.Customers.Find(customerId);
            if (customer != null)
            {
                _nameTextBox.Text = customer.Name;
                
                // Safely set ComboBox SelectedIndex - check if value is within range
                int typeIndex = (int)customer.Type - 1;
                if (typeIndex >= 0 && typeIndex < _typeComboBox.Items.Count)
                {
                    _typeComboBox.SelectedIndex = typeIndex;
                }
                
                _phoneTextBox.Text = customer.Phone;
                _emailTextBox.Text = customer.Email;
                _addressTextBox.Text = customer.Address;
                _taxNumberTextBox.Text = customer.TaxNumber;
                _commercialRegTextBox.Text = customer.CommercialRegistration;
                _creditLimitNumeric.Value = customer.CreditLimit;
                _paymentTermNumeric.Value = customer.PaymentTermDays;
                
                // Safely set Status ComboBox
                int statusIndex = (int)customer.Status - 1;
                if (statusIndex >= 0 && statusIndex < _statusComboBox.Items.Count)
                {
                    _statusComboBox.SelectedIndex = statusIndex;
                }
                
                _notesTextBox.Text = customer.Notes;
            }
        }

        private void SaveButton_Click(object? sender, EventArgs e)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(_nameTextBox.Text))
                {
                    MessageBox.Show("يرجى إدخال اسم العميل", "تنبيه",
                        MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    return;
                }

                Customer? customer;
                if (_selectedCustomerId > 0)
                {
                    customer = _context.Customers.Find(_selectedCustomerId);
                    if (customer == null) return;
                }
                else
                {
                    customer = new Customer
                    {
                        CreatedAt = DateTime.Now,
                        CreatedBy = Environment.UserName
                    };
                    _context.Customers.Add(customer);
                }

                customer.Name = _nameTextBox.Text;
                customer.Type = (CustomerType)(_typeComboBox.SelectedIndex + 1);
                customer.Phone = _phoneTextBox.Text;
                customer.Email = _emailTextBox.Text;
                customer.Address = _addressTextBox.Text;
                customer.TaxNumber = _taxNumberTextBox.Text;
                customer.CommercialRegistration = _commercialRegTextBox.Text;
                customer.CreditLimit = _creditLimitNumeric.Value;
                customer.PaymentTermDays = (int)_paymentTermNumeric.Value;
                customer.Status = (CustomerStatus)(_statusComboBox.SelectedIndex + 1);
                customer.Notes = _notesTextBox.Text;

                _context.SaveChanges();

                MessageBox.Show("تم حفظ بيانات العميل بنجاح", "نجح",
                    MessageBoxButtons.OK, MessageBoxIcon.Information);

                LoadCustomers();
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
            if (_selectedCustomerId == 0)
            {
                MessageBox.Show("يرجى اختيار عميل للحذف", "تنبيه",
                    MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            var result = MessageBox.Show("هل أنت متأكد من حذف هذا العميل؟",
                "تأكيد الحذف", MessageBoxButtons.YesNo, MessageBoxIcon.Question);

            if (result == DialogResult.Yes)
            {
                try
                {
                    var customer = _context.Customers.Find(_selectedCustomerId);
                    if (customer != null)
                    {
                        _context.Customers.Remove(customer);
                        _context.SaveChanges();

                        MessageBox.Show("تم حذف العميل بنجاح", "نجح",
                            MessageBoxButtons.OK, MessageBoxIcon.Information);

                        LoadCustomers();
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
            _selectedCustomerId = 0;
            _nameTextBox.ClearSelection();
            _typeComboBox.SelectedIndex = 0;
            _phoneTextBox.Clear();
            _emailTextBox.Clear();
            _addressTextBox.Clear();
            _taxNumberTextBox.Clear();
            _commercialRegTextBox.Clear();
            _creditLimitNumeric.Value = 0;
            _paymentTermNumeric.Value = 30;
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
                // الحصول على Id العميل
                if (e.AdditionalData.ContainsKey("Id"))
                {
                    int customerId = Convert.ToInt32(e.AdditionalData["Id"]);
                    _selectedCustomerId = customerId;

                    // تعبئة الحقول من البيانات الإضافية
                    if (e.AdditionalData.ContainsKey("Phone"))
                        _phoneTextBox.Text = e.AdditionalData["Phone"]?.ToString() ?? "";

                    if (e.AdditionalData.ContainsKey("Email"))
                        _emailTextBox.Text = e.AdditionalData["Email"]?.ToString() ?? "";

                    if (e.AdditionalData.ContainsKey("Address"))
                        _addressTextBox.Text = e.AdditionalData["Address"]?.ToString() ?? "";

                    if (e.AdditionalData.ContainsKey("TaxNumber"))
                        _taxNumberTextBox.Text = e.AdditionalData["TaxNumber"]?.ToString() ?? "";

                    if (e.AdditionalData.ContainsKey("CreditLimit"))
                        _creditLimitNumeric.Value = Convert.ToDecimal(e.AdditionalData["CreditLimit"]);

                    // تعيين النوع
                    if (e.AdditionalData.ContainsKey("Type"))
                    {
                        string typeStr = e.AdditionalData["Type"]?.ToString() ?? "";
                        if (Enum.TryParse<CustomerType>(typeStr, out var customerType))
                        {
                            int typeIndex = (int)customerType - 1;
                            if (typeIndex >= 0 && typeIndex < _typeComboBox.Items.Count)
                                _typeComboBox.SelectedIndex = typeIndex;
                        }
                    }

                    // تحميل باقي التفاصيل من قاعدة البيانات
                    var customer = _context.Customers.Find(customerId);
                    if (customer != null)
                    {
                        _commercialRegTextBox.Text = customer.CommercialRegistration ?? "";
                        _paymentTermNumeric.Value = customer.PaymentTermDays;
                        
                        int statusIndex = (int)customer.Status - 1;
                        if (statusIndex >= 0 && statusIndex < _statusComboBox.Items.Count)
                            _statusComboBox.SelectedIndex = statusIndex;
                        
                        _notesTextBox.Text = customer.Notes ?? "";
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
