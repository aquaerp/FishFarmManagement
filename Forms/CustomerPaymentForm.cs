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
    /// نموذج إدارة مدفوعات العملاء
    /// Form for managing customer payments
    /// </summary>
    public partial class CustomerPaymentForm : Form
    {
        private readonly FishFarmContext _context;
        private int _selectedPaymentId = 0;

        // Controls
        private TextBox _paymentNumberTextBox = null!;
        private ComboBox _customerComboBox = null!;
        private ComboBox _salesOrderComboBox = null!;
        private DateTimePicker _paymentDatePicker = null!;
        private NumericUpDown _amountNumeric = null!;
        private ComboBox _paymentMethodComboBox = null!;
        private TextBox _referenceNumberTextBox = null!;
        private TextBox _bankNameTextBox = null!;
        private TextBox _notesTextBox = null!;
        private TextBox _receivedByTextBox = null!;
        
        private DataGridView _paymentsGrid = null!;
        private Button _saveButton = null!;
        private Button _deleteButton = null!;
        private Button _newButton = null!;
        private TextBox _searchTextBox = null!;

        // Info labels
        private Label _customerBalanceLabel = null!;
        private Label _orderTotalLabel = null!;
        private Label _orderPaidLabel = null!;
        private Label _orderRemainingLabel = null!;

        public CustomerPaymentForm(FishFarmContext context)
        {
            _context = context;
            InitializeComponent();
            
            // Initialize sales order dropdown with empty message (before attaching events)
            _salesOrderComboBox.DataSource = new[] { new { Id = 0, Display = "-- اختر العميل أولاً --" } };
            _salesOrderComboBox.DisplayMember = "Display";
            _salesOrderComboBox.ValueMember = "Id";
            
            LoadCustomers();
            LoadPayments();
        }

        private void InitializeComponent()
        {
            this.Text = "إدارة مدفوعات العملاء - Customer Payments";
            this.Size = new System.Drawing.Size(1200, 750);
            this.RightToLeft = RightToLeft.Yes;
            this.RightToLeftLayout = true;
            this.StartPosition = FormStartPosition.CenterScreen;

            var topPanel = new Panel
            {
                Dock = DockStyle.Top,
                Height = 500,
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
                Text = "تسجيل مدفوعات العملاء",
                Font = new System.Drawing.Font("Segoe UI", 12, System.Drawing.FontStyle.Bold),
                Location = new System.Drawing.Point(10, 10),
                AutoSize = true
            };
            topPanel.Controls.Add(titleLabel);

            // Payment entry form
            int yPos = 50;

            // Row 1
            topPanel.Controls.Add(new Label { Text = "رقم الدفع:", Location = new System.Drawing.Point(1000, yPos), AutoSize = true });
            _paymentNumberTextBox = new TextBox
            {
                Location = new System.Drawing.Point(800, yPos),
                Width = 180,
                ReadOnly = true,
                BackColor = System.Drawing.Color.LightGray
            };
            topPanel.Controls.Add(_paymentNumberTextBox);

            topPanel.Controls.Add(new Label { Text = "التاريخ:", Location = new System.Drawing.Point(700, yPos), AutoSize = true });
            _paymentDatePicker = new DateTimePicker
            {
                Location = new System.Drawing.Point(500, yPos),
                Width = 180,
                Format = DateTimePickerFormat.Short
            };
            topPanel.Controls.Add(_paymentDatePicker);

            // Row 2
            yPos += 40;
            topPanel.Controls.Add(new Label { Text = "العميل:", Location = new System.Drawing.Point(1000, yPos), AutoSize = true });
            _customerComboBox = new ComboBox
            {
                Location = new System.Drawing.Point(700, yPos),
                Width = 280,
                DropDownStyle = ComboBoxStyle.DropDownList
            };
            _customerComboBox.SelectedIndexChanged += CustomerComboBox_SelectedIndexChanged;
            topPanel.Controls.Add(_customerComboBox);

            topPanel.Controls.Add(new Label { Text = "الرصيد:", Location = new System.Drawing.Point(600, yPos), AutoSize = true });
            _customerBalanceLabel = new Label
            {
                Text = "0.00 ريال",
                Location = new System.Drawing.Point(400, yPos),
                Width = 180,
                Font = new System.Drawing.Font("Segoe UI", 9, System.Drawing.FontStyle.Bold),
                ForeColor = System.Drawing.Color.Red
            };
            topPanel.Controls.Add(_customerBalanceLabel);

            // Row 3
            yPos += 40;
            topPanel.Controls.Add(new Label { Text = "أمر المبيعات (اختياري):", Location = new System.Drawing.Point(1000, yPos), AutoSize = true });
            _salesOrderComboBox = new ComboBox
            {
                Location = new System.Drawing.Point(600, yPos),
                Width = 380,
                DropDownStyle = ComboBoxStyle.DropDownList
            };
            _salesOrderComboBox.SelectedIndexChanged += SalesOrderComboBox_SelectedIndexChanged;
            topPanel.Controls.Add(_salesOrderComboBox);

            // Order info section
            yPos += 40;
            var orderInfoGroup = new GroupBox
            {
                Text = "معلومات أمر المبيعات",
                Location = new System.Drawing.Point(600, yPos),
                Width = 580,
                Height = 100
            };

            orderInfoGroup.Controls.Add(new Label { Text = "إجمالي الطلب:", Location = new System.Drawing.Point(450, 25), AutoSize = true });
            _orderTotalLabel = new Label
            {
                Text = "0.00 ريال",
                Location = new System.Drawing.Point(300, 25),
                Width = 130,
                TextAlign = System.Drawing.ContentAlignment.MiddleRight
            };
            orderInfoGroup.Controls.Add(_orderTotalLabel);

            orderInfoGroup.Controls.Add(new Label { Text = "المدفوع:", Location = new System.Drawing.Point(450, 50), AutoSize = true });
            _orderPaidLabel = new Label
            {
                Text = "0.00 ريال",
                Location = new System.Drawing.Point(300, 50),
                Width = 130,
                TextAlign = System.Drawing.ContentAlignment.MiddleRight,
                ForeColor = System.Drawing.Color.Green
            };
            orderInfoGroup.Controls.Add(_orderPaidLabel);

            orderInfoGroup.Controls.Add(new Label { Text = "المتبقي:", Location = new System.Drawing.Point(450, 75), AutoSize = true });
            _orderRemainingLabel = new Label
            {
                Text = "0.00 ریال",
                Location = new System.Drawing.Point(300, 75),
                Width = 130,
                Font = new System.Drawing.Font("Segoe UI", 9, System.Drawing.FontStyle.Bold),
                TextAlign = System.Drawing.ContentAlignment.MiddleRight,
                ForeColor = System.Drawing.Color.Red
            };
            orderInfoGroup.Controls.Add(_orderRemainingLabel);

            topPanel.Controls.Add(orderInfoGroup);

            // Row 4
            yPos += 110;
            topPanel.Controls.Add(new Label { Text = "المبلغ المدفوع:", Location = new System.Drawing.Point(1000, yPos), AutoSize = true });
            _amountNumeric = new NumericUpDown
            {
                Location = new System.Drawing.Point(800, yPos),
                Width = 180,
                Maximum = 9999999,
                DecimalPlaces = 2,
                ThousandsSeparator = true,
                Minimum = 0.01m
            };
            topPanel.Controls.Add(_amountNumeric);

            topPanel.Controls.Add(new Label { Text = "طريقة الدفع:", Location = new System.Drawing.Point(680, yPos), AutoSize = true });
            _paymentMethodComboBox = new ComboBox
            {
                Location = new System.Drawing.Point(480, yPos),
                Width = 180,
                DropDownStyle = ComboBoxStyle.DropDownList
            };
            _paymentMethodComboBox.Items.AddRange(new object[]
            {
                "نقدي", "شيك", "حوالة بنكية", "بطاقة ائتمان", "بطاقة مدى", "أخرى"
            });
            _paymentMethodComboBox.SelectedIndex = 0;
            topPanel.Controls.Add(_paymentMethodComboBox);

            // Row 5
            yPos += 40;
            topPanel.Controls.Add(new Label { Text = "رقم المرجع:", Location = new System.Drawing.Point(1000, yPos), AutoSize = true });
            _referenceNumberTextBox = new TextBox
            {
                Location = new System.Drawing.Point(800, yPos),
                Width = 180
            };
            topPanel.Controls.Add(_referenceNumberTextBox);

            topPanel.Controls.Add(new Label { Text = "البنك:", Location = new System.Drawing.Point(680, yPos), AutoSize = true });
            _bankNameTextBox = new TextBox
            {
                Location = new System.Drawing.Point(480, yPos),
                Width = 180
            };
            topPanel.Controls.Add(_bankNameTextBox);

            // Row 6
            yPos += 40;
            topPanel.Controls.Add(new Label { Text = "استلمه:", Location = new System.Drawing.Point(1000, yPos), AutoSize = true });
            _receivedByTextBox = new TextBox
            {
                Location = new System.Drawing.Point(800, yPos),
                Width = 180,
                Text = Environment.UserName
            };
            topPanel.Controls.Add(_receivedByTextBox);

            // Row 7
            yPos += 40;
            topPanel.Controls.Add(new Label { Text = "ملاحظات:", Location = new System.Drawing.Point(1000, yPos), AutoSize = true });
            _notesTextBox = new TextBox
            {
                Location = new System.Drawing.Point(480, yPos),
                Width = 500,
                Multiline = true,
                Height = 60
            };
            topPanel.Controls.Add(_notesTextBox);

            // Buttons
            yPos += 70;
            _saveButton = new Button
            {
                Text = "حفظ الدفع",
                Location = new System.Drawing.Point(1000, yPos),
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
                Location = new System.Drawing.Point(870, yPos),
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
                Location = new System.Drawing.Point(740, yPos),
                Width = 120,
                Height = 40,
                BackColor = ThemeManager.ErrorRed,
                ForeColor = System.Drawing.Color.White,
                FlatStyle = FlatStyle.Flat
            };
            _deleteButton.Click += DeleteButton_Click;
            topPanel.Controls.Add(_deleteButton);

            // Bottom panel - Payments list
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
            _searchTextBox.TextChanged += (s, e) => LoadPayments();
            bottomPanel.Controls.Add(_searchTextBox);

            _paymentsGrid = new DataGridView
            {
                Location = new System.Drawing.Point(10, 45),
                Width = 1150,
                Height = 200,
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

        private void LoadCustomers()
        {
            try
            {
                var customers = _context.Customers
                    .Where(c => c.Status == CustomerStatus.Active)
                    .OrderBy(c => c.Name)
                    .Select(c => new { c.Id, c.Name, c.CurrentBalance })
                    .ToList();

                _customerComboBox.DataSource = customers;
                _customerComboBox.DisplayMember = "Name";
                _customerComboBox.ValueMember = "Id";
            }
            catch (Exception ex)
            {
                MessageBox.Show($"خطأ في تحميل العملاء: {ex.Message}", "خطأ",
                    MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void CustomerComboBox_SelectedIndexChanged(object? sender, EventArgs e)
        {
            if (_customerComboBox.SelectedValue != null && _customerComboBox.SelectedValue is int customerId && customerId > 0)
            {
                var customer = _context.Customers.Find(customerId);
                if (customer != null)
                {
                    _customerBalanceLabel.Text = $"{customer.CurrentBalance:N2} ريال";
                    LoadCustomerOrders(customerId);
                }
            }
            else
            {
                // Clear sales orders when no valid customer selected
                _salesOrderComboBox.DataSource = new[] { new { Id = 0, Display = "-- اختر العميل أولاً --" } };
                _salesOrderComboBox.DisplayMember = "Display";
                _salesOrderComboBox.ValueMember = "Id";
                _customerBalanceLabel.Text = "0.00 ريال";
            }
        }

        private void LoadCustomerOrders(int customerId)
        {
            try
            {
                var orders = _context.SalesOrders
                    .Where(so => so.CustomerId == customerId && so.RemainingAmount > 0)
                    .OrderByDescending(so => so.OrderDate)
                    .Select(so => new
                    {
                        so.Id,
                        Display = $"{so.OrderNumber} - {so.OrderDate:dd/MM/yyyy} - متبقي: {so.RemainingAmount:N2} ريال",
                        so.TotalAmount,
                        so.PaidAmount,
                        so.RemainingAmount
                    })
                    .ToList();

                orders.Insert(0, new
                {
                    Id = 0,
                    Display = "-- دفعة عامة (بدون ربط بطلب) --",
                    TotalAmount = 0m,
                    PaidAmount = 0m,
                    RemainingAmount = 0m
                });

                _salesOrderComboBox.DataSource = orders;
                _salesOrderComboBox.DisplayMember = "Display";
                _salesOrderComboBox.ValueMember = "Id";
            }
            catch (Exception ex)
            {
                MessageBox.Show($"خطأ في تحميل الطلبات: {ex.Message}", "خطأ",
                    MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void SalesOrderComboBox_SelectedIndexChanged(object? sender, EventArgs e)
        {
            if (_salesOrderComboBox.SelectedValue != null && _salesOrderComboBox.SelectedValue is int orderId && orderId > 0)
            {
                var order = _context.SalesOrders.Find(orderId);
                if (order != null)
                {
                    _orderTotalLabel.Text = $"{order.TotalAmount:N2} ريال";
                    _orderPaidLabel.Text = $"{order.PaidAmount:N2} ريال";
                    _orderRemainingLabel.Text = $"{order.RemainingAmount:N2} ريال";
                    _amountNumeric.Value = order.RemainingAmount;
                }
            }
            else
            {
                _orderTotalLabel.Text = "0.00 ريال";
                _orderPaidLabel.Text = "0.00 ريال";
                _orderRemainingLabel.Text = "0.00 ريال";
            }
        }

        private void LoadPayments()
        {
            try
            {
                var query = _context.CustomerPayments
                    .Include(cp => cp.Customer)
                    .Include(cp => cp.SalesOrder)
                    .AsQueryable();

                if (!string.IsNullOrWhiteSpace(_searchTextBox?.Text))
                {
                    var searchTerm = _searchTextBox.Text.ToLower();
                    query = query.Where(cp =>
                        (cp.PaymentNumber != null && cp.PaymentNumber.ToLower().Contains(searchTerm)) ||
                        (cp.Customer != null && cp.Customer.Name != null && cp.Customer.Name.ToLower().Contains(searchTerm)) ||
                        (cp.ReferenceNumber != null && cp.ReferenceNumber.ToLower().Contains(searchTerm)));
                }

                var payments = query
                    .OrderByDescending(cp => cp.PaymentDate)
                    .Select(cp => new
                    {
                        cp.Id,
                        رقم_الدفع = cp.PaymentNumber,
                        العميل = cp.Customer.Name,
                        التاريخ = cp.PaymentDate,
                        المبلغ = cp.Amount,
                        طريقة_الدفع = cp.PaymentMethod.ToString(),
                        رقم_الطلب = cp.SalesOrder != null ? cp.SalesOrder.OrderNumber : "دفعة عامة",
                        رقم_المرجع = cp.ReferenceNumber,
                        استلمه = cp.ReceivedBy
                    })
                    .ToList();

                _paymentsGrid.DataSource = payments;
                _paymentsGrid.Columns["Id"].Visible = false;
            }
            catch (Exception ex)
            {
                MessageBox.Show($"خطأ في تحميل المدفوعات: {ex.Message}", "خطأ",
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
                var payment = _context.CustomerPayments
                    .Include(cp => cp.Customer)
                    .Include(cp => cp.SalesOrder)
                    .FirstOrDefault(cp => cp.Id == paymentId);

                if (payment != null)
                {
                    _paymentNumberTextBox.Text = payment.PaymentNumber;
                    _customerComboBox.SelectedValue = (object?)payment.CustomerId;
                    _salesOrderComboBox.SelectedValue = (object?)(payment.SalesOrderId ?? 0);
                    _paymentDatePicker.Value = payment.PaymentDate;
                    _amountNumeric.Value = payment.Amount;
                    // PaymentMethod is string, map to combo box index
                    var methods = new[] { "نقدي", "بنكي", "شيك" };
                    _paymentMethodComboBox.SelectedIndex = Array.IndexOf(methods, payment.PaymentMethod);
                    _referenceNumberTextBox.Text = payment.ReferenceNumber;
                    _bankNameTextBox.Text = payment.BankName;
                    _notesTextBox.Text = payment.Notes;
                    _receivedByTextBox.Text = payment.ReceivedBy;
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"خطأ في تحميل تفاصيل الدفع: {ex.Message}", "خطأ",
                    MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void SaveButton_Click(object? sender, EventArgs e)
        {
            try
            {
                if (_customerComboBox.SelectedValue == null)
                {
                    MessageBox.Show("يرجى اختيار العميل", "تنبيه",
                        MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    return;
                }

                if (_amountNumeric.Value <= 0)
                {
                    MessageBox.Show("يرجى إدخال مبلغ صحيح", "تنبيه",
                        MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    return;
                }

                CustomerPayment? payment;
                if (_selectedPaymentId > 0)
                {
                    payment = _context.CustomerPayments.Find(_selectedPaymentId);
                    if (payment == null) return;

                    // Reverse old payment effects
                    UpdateCustomerBalance(payment.CustomerId, payment.Amount, isReversal: true);
                    if (payment.SalesOrderId.HasValue)
                    {
                        UpdateOrderPayment(payment.SalesOrderId.Value, payment.Amount, isReversal: true);
                    }
                }
                else
                {
                    payment = new CustomerPayment
                    {
                        PaymentNumber = GeneratePaymentNumber(),
                        CreatedAt = DateTime.Now
                    };
                    _context.CustomerPayments.Add(payment);
                }

                payment!.CustomerId = _customerComboBox.SelectedValue is int customerId ? customerId : 0;
                payment!.SalesOrderId = _salesOrderComboBox.SelectedValue is int orderId && orderId > 0
                    ? (int?)orderId : null;
                payment!.PaymentDate = _paymentDatePicker.Value;
                payment!.Amount = _amountNumeric.Value;
                payment!.PaymentMethod = ((PaymentMethod)(_paymentMethodComboBox.SelectedIndex + 1)).ToString();
                payment!.ReferenceNumber = _referenceNumberTextBox.Text;
                payment!.BankName = _bankNameTextBox.Text;
                payment!.Notes = _notesTextBox.Text;
                payment!.ReceivedBy = _receivedByTextBox.Text;

                // Update customer balance
                UpdateCustomerBalance(payment!.CustomerId, payment.Amount, isReversal: false);

                // Update sales order if linked
                if (payment!.SalesOrderId.HasValue)
                {
                    UpdateOrderPayment(payment!.SalesOrderId.Value, payment.Amount, isReversal: false);
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

            var result = MessageBox.Show("هل أنت متأكد من حذف هذه الدفعة؟",
                "تأكيد الحذف", MessageBoxButtons.YesNo, MessageBoxIcon.Question);

            if (result == DialogResult.Yes)
            {
                try
                {
                    var payment = _context.CustomerPayments.Find(_selectedPaymentId);
                    if (payment != null)
                    {
                        // Reverse payment effects
                        UpdateCustomerBalance(payment.CustomerId, payment.Amount, isReversal: true);
                        if (payment.SalesOrderId.HasValue)
                        {
                            UpdateOrderPayment(payment.SalesOrderId.Value, payment.Amount, isReversal: true);
                        }

                        _context.CustomerPayments.Remove(payment);
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

        private void UpdateCustomerBalance(int customerId, decimal amount, bool isReversal)
        {
            var customer = _context.Customers.Find(customerId);
            if (customer != null)
            {
                if (isReversal)
                {
                    customer.CurrentBalance += amount; // Add back the payment
                }
                else
                {
                    customer.CurrentBalance -= amount; // Reduce balance by payment
                }
            }
        }

        private void UpdateOrderPayment(int orderId, decimal amount, bool isReversal)
        {
            var order = _context.SalesOrders.Find(orderId);
            if (order != null)
            {
                if (isReversal)
                {
                    order.PaidAmount -= amount;
                    order.RemainingAmount += amount;
                }
                else
                {
                    order.PaidAmount += amount;
                    order.RemainingAmount -= amount;
                }

                // Update order status based on payment
                if (order.RemainingAmount <= 0)
                {
                    // Fully paid - could update status if needed
                }
            }
        }

        private void ClearForm()
        {
            _selectedPaymentId = 0;
            _paymentNumberTextBox.Clear();
            _customerComboBox.SelectedIndex = -1;
            _salesOrderComboBox.SelectedIndex = -1;
            _paymentDatePicker.Value = DateTime.Now;
            _amountNumeric.Value = 0.01m; // Changed from 0 to 0.01m to respect Minimum value
            _paymentMethodComboBox.SelectedIndex = 0;
            _referenceNumberTextBox.Clear();
            _bankNameTextBox.Clear();
            _notesTextBox.Clear();
            _receivedByTextBox.Text = Environment.UserName;
            
            _customerBalanceLabel.Text = "0.00 ريال";
            _orderTotalLabel.Text = "0.00 ريال";
            _orderPaidLabel.Text = "0.00 ريال";
            _orderRemainingLabel.Text = "0.00 ريال";
        }

        private string GeneratePaymentNumber()
        {
            var date = DateTime.Now;
            var count = _context.CustomerPayments.Count(cp => cp.PaymentDate.Date == date.Date) + 1;
            return $"PAY-{date:yyyyMMdd}-{count:D4}";
}
}
}