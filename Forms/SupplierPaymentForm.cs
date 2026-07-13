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
    /// نموذج دفع مستحقات الموردين
    /// Supplier payment form
    /// </summary>
    public partial class SupplierPaymentForm : Form
    {
        private readonly FishFarmContext _context;
        private int _selectedPaymentId = 0;

        // Controls
        private ComboBox _supplierComboBox = null!;
        private Label _currentBalanceLabel = null!;
        private Label _creditLimitLabel = null!;
        private DateTimePicker _paymentDatePicker = null!;
        private NumericUpDown _amountNumeric = null!;
        private ComboBox _paymentMethodComboBox = null!;
        private TextBox _referenceNumberTextBox = null!;
        private TextBox _bankNameTextBox = null!;
        private TextBox _notesTextBox = null!;
        private DataGridView _paymentsGrid = null!;
        private Button _saveButton = null!;
        private Button _deleteButton = null!;
        private Button _newButton = null!;
        private DateTimePicker _filterFromDatePicker = null!;
        private DateTimePicker _filterToDatePicker = null!;
        private ComboBox _filterSupplierComboBox = null!;

        public SupplierPaymentForm(FishFarmContext context)
        {
            _context = context;
            InitializeComponent();
            LoadSuppliers();
            LoadPayments();
        }

        private void InitializeComponent()
        {
            this.Text = "دفع مستحقات الموردين - Supplier Payment";
            this.Size = new System.Drawing.Size(1200, 750);
            this.RightToLeft = RightToLeft.Yes;
            this.RightToLeftLayout = true;
            this.StartPosition = FormStartPosition.CenterScreen;

            var topPanel = new Panel
            {
                Dock = DockStyle.Top,
                Height = 400,
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
                Text = "دفعة جديدة للمورد",
                Font = new System.Drawing.Font("Segoe UI", 12, System.Drawing.FontStyle.Bold),
                Location = new System.Drawing.Point(10, 10),
                AutoSize = true
            };
            topPanel.Controls.Add(titleLabel);

            int yPos = 50;

            // Row 1 - Supplier
            topPanel.Controls.Add(new Label { Text = "المورد:", Location = new System.Drawing.Point(1050, yPos), AutoSize = true });
            _supplierComboBox = new ComboBox
            {
                Location = new System.Drawing.Point(700, yPos),
                Width = 330,
                DropDownStyle = ComboBoxStyle.DropDownList
            };
            _supplierComboBox.SelectedIndexChanged += SupplierComboBox_SelectedIndexChanged;
            topPanel.Controls.Add(_supplierComboBox);

            // Supplier Info Panel
            yPos += 40;
            var infoPanel = new Panel
            {
                Location = new System.Drawing.Point(700, yPos),
                Width = 450,
                Height = 80,
                BorderStyle = BorderStyle.FixedSingle,
                BackColor = System.Drawing.Color.FromArgb(240, 248, 255)
            };

            var balanceInfoLabel = new Label
            {
                Text = "الرصيد الحالي:",
                Location = new System.Drawing.Point(330, 10),
                AutoSize = true,
                Font = new System.Drawing.Font("Segoe UI", 9, System.Drawing.FontStyle.Bold)
            };
            infoPanel.Controls.Add(balanceInfoLabel);

            _currentBalanceLabel = new Label
            {
                Text = "0.00 ريال",
                Location = new System.Drawing.Point(180, 10),
                Width = 140,
                Font = new System.Drawing.Font("Segoe UI", 11, System.Drawing.FontStyle.Bold),
                ForeColor = System.Drawing.Color.DarkRed,
                TextAlign = System.Drawing.ContentAlignment.MiddleRight
            };
            infoPanel.Controls.Add(_currentBalanceLabel);

            var creditLimitInfoLabel = new Label
            {
                Text = "الحد الائتماني:",
                Location = new System.Drawing.Point(330, 40),
                AutoSize = true,
                Font = new System.Drawing.Font("Segoe UI", 9, System.Drawing.FontStyle.Bold)
            };
            infoPanel.Controls.Add(creditLimitInfoLabel);

            _creditLimitLabel = new Label
            {
                Text = "0.00 ريال",
                Location = new System.Drawing.Point(180, 40),
                Width = 140,
                Font = new System.Drawing.Font("Segoe UI", 10),
                ForeColor = System.Drawing.Color.DarkBlue,
                TextAlign = System.Drawing.ContentAlignment.MiddleRight
            };
            infoPanel.Controls.Add(_creditLimitLabel);

            topPanel.Controls.Add(infoPanel);

            // Row 2 - Payment Date and Amount
            yPos += 90;
            topPanel.Controls.Add(new Label { Text = "تاريخ الدفع:", Location = new System.Drawing.Point(1050, yPos), AutoSize = true });
            _paymentDatePicker = new DateTimePicker
            {
                Location = new System.Drawing.Point(870, yPos),
                Width = 160,
                Format = DateTimePickerFormat.Short
            };
            topPanel.Controls.Add(_paymentDatePicker);

            topPanel.Controls.Add(new Label { Text = "المبلغ:", Location = new System.Drawing.Point(770, yPos), AutoSize = true });
            _amountNumeric = new NumericUpDown
            {
                Location = new System.Drawing.Point(580, yPos),
                Width = 180,
                DecimalPlaces = 2,
                Maximum = 9999999,
                ThousandsSeparator = true
            };
            topPanel.Controls.Add(_amountNumeric);

            // Row 3 - Payment Method
            yPos += 40;
            topPanel.Controls.Add(new Label { Text = "طريقة الدفع:", Location = new System.Drawing.Point(1050, yPos), AutoSize = true });
            _paymentMethodComboBox = new ComboBox
            {
                Location = new System.Drawing.Point(870, yPos),
                Width = 160,
                DropDownStyle = ComboBoxStyle.DropDownList
            };
            _paymentMethodComboBox.Items.AddRange(new object[]
            {
                "نقدي", "شيك", "حوالة بنكية", "بطاقة ائتمان", "بطاقة مدى", "أخرى"
            });
            _paymentMethodComboBox.SelectedIndex = 0;
            _paymentMethodComboBox.SelectedIndexChanged += PaymentMethodComboBox_SelectedIndexChanged;
            topPanel.Controls.Add(_paymentMethodComboBox);

            // Row 4 - Reference Number
            yPos += 40;
            topPanel.Controls.Add(new Label { Text = "رقم المرجع:", Location = new System.Drawing.Point(1050, yPos), AutoSize = true });
            _referenceNumberTextBox = new TextBox
            {
                Location = new System.Drawing.Point(870, yPos),
                Width = 160
            };
            topPanel.Controls.Add(_referenceNumberTextBox);

            // Row 5 - Bank Name
            yPos += 40;
            topPanel.Controls.Add(new Label { Text = "اسم البنك:", Location = new System.Drawing.Point(1050, yPos), AutoSize = true });
            _bankNameTextBox = new TextBox
            {
                Location = new System.Drawing.Point(700, yPos),
                Width = 330,
                Enabled = false
            };
            topPanel.Controls.Add(_bankNameTextBox);

            // Row 6 - Notes
            yPos += 40;
            topPanel.Controls.Add(new Label { Text = "ملاحظات:", Location = new System.Drawing.Point(1050, yPos), AutoSize = true });
            _notesTextBox = new TextBox
            {
                Location = new System.Drawing.Point(700, yPos),
                Width = 330,
                Multiline = true,
                Height = 50
            };
            topPanel.Controls.Add(_notesTextBox);

            // Buttons
            yPos += 60;
            _saveButton = new Button
            {
                Text = "حفظ الدفعة",
                Location = new System.Drawing.Point(1050, yPos),
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
                Location = new System.Drawing.Point(920, yPos),
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
                Location = new System.Drawing.Point(790, yPos),
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
                Text = "سجل الدفعات:",
                Font = new System.Drawing.Font("Segoe UI", 10, System.Drawing.FontStyle.Bold),
                Location = new System.Drawing.Point(10, 10),
                AutoSize = true
            };
            bottomPanel.Controls.Add(filterLabel);

            bottomPanel.Controls.Add(new Label { Text = "من:", Location = new System.Drawing.Point(1100, 45), AutoSize = true });
            _filterFromDatePicker = new DateTimePicker
            {
                Location = new System.Drawing.Point(940, 45),
                Width = 150,
                Format = DateTimePickerFormat.Short
            };
            _filterFromDatePicker.Value = DateTime.Now.AddMonths(-3);
            _filterFromDatePicker.ValueChanged += (s, e) => LoadPayments();
            bottomPanel.Controls.Add(_filterFromDatePicker);

            bottomPanel.Controls.Add(new Label { Text = "إلى:", Location = new System.Drawing.Point(900, 45), AutoSize = true });
            _filterToDatePicker = new DateTimePicker
            {
                Location = new System.Drawing.Point(740, 45),
                Width = 150,
                Format = DateTimePickerFormat.Short
            };
            _filterToDatePicker.ValueChanged += (s, e) => LoadPayments();
            bottomPanel.Controls.Add(_filterToDatePicker);

            bottomPanel.Controls.Add(new Label { Text = "المورد:", Location = new System.Drawing.Point(670, 45), AutoSize = true });
            _filterSupplierComboBox = new ComboBox
            {
                Location = new System.Drawing.Point(470, 45),
                Width = 190,
                DropDownStyle = ComboBoxStyle.DropDownList
            };
            _filterSupplierComboBox.SelectedIndexChanged += (s, e) => LoadPayments();
            bottomPanel.Controls.Add(_filterSupplierComboBox);

            _paymentsGrid = new DataGridView
            {
                Location = new System.Drawing.Point(10, 80),
                Width = 1160,
                Height = 220,
                AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill,
                SelectionMode = DataGridViewSelectionMode.FullRowSelect,
                MultiSelect = false,
                ReadOnly = true,
                AllowUserToAddRows = false
            };
            _paymentsGrid.SelectionChanged += PaymentsGrid_SelectionChanged;
            bottomPanel.Controls.Add(_paymentsGrid);

            this.Controls.Add(bottomPanel);
            this.Controls.Add(topPanel);
        }

        private void LoadSuppliers()
        {
            var suppliers = _context.Suppliers
                .Where(s => s.Status == SupplierStatus.Active)
                .OrderBy(s => s.Name)
                .Select(s => new SupplierItem { Id = s.Id, Name = s.Name })
                .ToList();

            suppliers.Insert(0, new SupplierItem { Id = 0, Name = "-- اختر المورد --" });

            _supplierComboBox.DataSource = suppliers;
            _supplierComboBox.DisplayMember = "Name";
            _supplierComboBox.ValueMember = "Id";

            // For filter
            var filterSuppliers = new[] { new SupplierItem { Id = 0, Name = "-- الكل --" } }
                .Concat(_context.Suppliers.OrderBy(s => s.Name)
                    .Select(s => new SupplierItem { Id = s.Id, Name = s.Name }).ToList())
                .ToList();

            _filterSupplierComboBox.DataSource = filterSuppliers;
            _filterSupplierComboBox.DisplayMember = "Name";
            _filterSupplierComboBox.ValueMember = "Id";
        }

        private void SupplierComboBox_SelectedIndexChanged(object? sender, EventArgs e)
        {
            if (_supplierComboBox.SelectedValue is int supplierId && supplierId > 0)
            {
                var supplier = _context.Suppliers.Find(supplierId);
                if (supplier != null)
                {
                    _currentBalanceLabel.Text = $"{supplier.CurrentBalance:N2} ريال";
                    _currentBalanceLabel.ForeColor = supplier.CurrentBalance > 0 ?
                        System.Drawing.Color.DarkRed : System.Drawing.Color.DarkGreen;
                    
                    _creditLimitLabel.Text = $"{supplier.CreditLimit:N2} ريال";
                    
                    // Suggest payment amount (current balance)
                    if (supplier.CurrentBalance > 0)
                    {
                        _amountNumeric.Value = supplier.CurrentBalance;
                    }
                }
            }
            else
            {
                _currentBalanceLabel.Text = "0.00 ريال";
                _creditLimitLabel.Text = "0.00 ريال";
                _amountNumeric.Value = 0;
            }
        }

        private void PaymentMethodComboBox_SelectedIndexChanged(object? sender, EventArgs e)
        {
            var method = _paymentMethodComboBox.SelectedIndex;
            // Enable bank name for bank transfer or check
            _bankNameTextBox.Enabled = (method == 1 || method == 2); // Check or Bank Transfer
        }

        private void LoadPayments()
        {
            try
            {
                var query = _context.SupplierPayments
                    .Include(sp => sp.Supplier)
                    .AsQueryable();

                // Date filter
                query = query.Where(sp => sp.PaymentDate >= _filterFromDatePicker.Value.Date &&
                                         sp.PaymentDate <= _filterToDatePicker.Value.Date);

                // Supplier filter
                var filterSupplierId = _filterSupplierComboBox.SelectedValue is int id ? id : 0;
                if (filterSupplierId > 0)
                {
                    query = query.Where(sp => sp.SupplierId == filterSupplierId);
                }

                var payments = query
                    .OrderByDescending(sp => sp.PaymentDate)
                    .Select(sp => new
                    {
                        sp.Id,
                        رقم_الدفعة = sp.PaymentNumber,
                        التاريخ = sp.PaymentDate,
                        المورد = sp.Supplier.Name,
                        المبلغ = sp.Amount,
                        طريقة_الدفع = sp.PaymentMethod.ToString(),
                        رقم_المرجع = sp.ReferenceNumber,
                        البنك = sp.BankName,
                        المسجل = sp.PaidBy
                    })
                    .ToList();

                _paymentsGrid.DataSource = payments;
                _paymentsGrid.Columns["Id"].Visible = false;

                // Format currency
                if (_paymentsGrid.Columns.Contains("المبلغ"))
                {
                    _paymentsGrid.Columns["المبلغ"].DefaultCellStyle.Format = "N2";
                    _paymentsGrid.Columns["المبلغ"].DefaultCellStyle.ForeColor = System.Drawing.Color.DarkGreen;
                }

                // Calculate totals
                var totalAmount = payments.Sum(p => p.المبلغ);
                this.Text = $"دفع مستحقات الموردين - إجمالي الدفعات: {totalAmount:N2} ريال";
            }
            catch (Exception ex)
            {
                MessageBox.Show($"خطأ في تحميل الدفعات: {ex.Message}", "خطأ",
                    MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void PaymentsGrid_SelectionChanged(object? sender, EventArgs e)
        {
            if (_paymentsGrid.SelectedRows.Count > 0)
            {
                var selectedRow = _paymentsGrid.SelectedRows[0];
                _selectedPaymentId = (int)selectedRow.Cells["Id"].Value;
                LoadPaymentDetails(_selectedPaymentId);
            }
        }

        private void LoadPaymentDetails(int paymentId)
        {
            try
            {
                var payment = _context.SupplierPayments.Find(paymentId);
                if (payment != null)
                {
                    _supplierComboBox.SelectedValue = (object?)payment.SupplierId;
                    _paymentDatePicker.Value = payment.PaymentDate;
                    _amountNumeric.Value = payment.Amount;
                    // PaymentMethod is string, map to combo box index
                    var methods = new[] { "نقدي", "بنكي", "شيك" };
                    _paymentMethodComboBox.SelectedIndex = Array.IndexOf(methods, payment.PaymentMethod);
                    _referenceNumberTextBox.Text = payment.ReferenceNumber;
                    _bankNameTextBox.Text = payment.BankName;
                    _notesTextBox.Text = payment.Notes;
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"خطأ في تحميل تفاصيل الدفعة: {ex.Message}", "خطأ",
                    MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void SaveButton_Click(object? sender, EventArgs e)
        {
            try
            {
                var supplierId = _supplierComboBox.SelectedValue is int id ? id : 0;
                if (supplierId == 0)
                {
                    MessageBox.Show("يرجى اختيار المورد", "تنبيه",
                        MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    return;
                }

                if (_amountNumeric.Value <= 0)
                {
                    MessageBox.Show("يرجى إدخال مبلغ صحيح", "تنبيه",
                        MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    return;
                }

                SupplierPayment? payment;
                var isNewPayment = _selectedPaymentId == 0;

                if (_selectedPaymentId > 0)
                {
                    payment = _context.SupplierPayments.Find(_selectedPaymentId);
                    if (payment == null) return;

                    // Reverse previous payment effect on supplier balance
                    var supplier = _context.Suppliers.Find(payment.SupplierId);
                    if (supplier != null)
                    {
                        supplier.CurrentBalance += payment.Amount;
                    }
                }
                else
                {
                    payment = new SupplierPayment
                    {
                        PaymentNumber = GeneratePaymentNumber(),
                        CreatedAt = DateTime.Now,
                        PaidBy = Environment.UserName
                    };
                    _context.SupplierPayments.Add(payment);
                }

                payment!.SupplierId = supplierId;
                payment!.PaymentDate = _paymentDatePicker.Value;
                payment!.Amount = _amountNumeric.Value;
                payment!.PaymentMethod = ((PaymentMethod)(_paymentMethodComboBox.SelectedIndex + 1)).ToString();
                payment!.ReferenceNumber = _referenceNumberTextBox.Text;
                payment!.BankName = _bankNameTextBox.Text;
                payment!.Notes = _notesTextBox.Text;

                // Update supplier balance (deduct payment)
                var currentSupplier = _context.Suppliers.Find(supplierId);
                if (currentSupplier != null)
                {
                    currentSupplier.CurrentBalance -= payment.Amount;
                }

                _context.SaveChanges();

                MessageBox.Show("تم حفظ الدفعة بنجاح", "نجح",
                    MessageBoxButtons.OK, MessageBoxIcon.Information);

                LoadPayments();
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
            if (_selectedPaymentId == 0)
            {
                MessageBox.Show("يرجى اختيار دفعة للحذف", "تنبيه",
                    MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            var result = MessageBox.Show("هل أنت متأكد من حذف هذه الدفعة؟\nسيتم إضافة المبلغ إلى رصيد المورد.",
                "تأكيد الحذف", MessageBoxButtons.YesNo, MessageBoxIcon.Question);

            if (result == DialogResult.Yes)
            {
                try
                {
                    var payment = _context.SupplierPayments.Find(_selectedPaymentId);
                    if (payment != null)
                    {
                        // Reverse payment effect on supplier balance
                        var supplier = _context.Suppliers.Find(payment.SupplierId);
                        if (supplier != null)
                        {
                            supplier.CurrentBalance += payment.Amount;
                        }

                        _context.SupplierPayments.Remove(payment);
                        _context.SaveChanges();

                        MessageBox.Show("تم حذف الدفعة بنجاح", "نجح",
                            MessageBoxButtons.OK, MessageBoxIcon.Information);

                        LoadPayments();
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

        private string GeneratePaymentNumber()
        {
            var today = DateTime.Now;
            var count = _context.SupplierPayments
                .Count(p => p.PaymentDate.Date == today.Date) + 1;
            
            return $"SP-{today:yyyyMMdd}-{count:000}";
        }

        private void ClearForm()
        {
            _selectedPaymentId = 0;
            _supplierComboBox.SelectedIndex = 0;
            _paymentDatePicker.Value = DateTime.Now;
            _amountNumeric.Value = 0;
            _paymentMethodComboBox.SelectedIndex = 0;
            _referenceNumberTextBox.Clear();
            _bankNameTextBox.Clear();
            _notesTextBox.Clear();
            _currentBalanceLabel.Text = "0.00 ريال";
            _creditLimitLabel.Text = "0.00 ريال";
        }

        // Helper class for ComboBox items
        private class SupplierItem
        {
            public int Id { get; set; }
            public string Name { get; set; } = string.Empty;
            public override string ToString() => Name;
        }
}
}