using System;
using System.Drawing;
using System.Linq;
using System.Threading.Tasks;
using System.Windows.Forms;
using Microsoft.EntityFrameworkCore;
using FishFarmManager.Data;
using FishFarmManager.Models;
using FishFarmManager.Services;

namespace FishFarmManager.Forms
{
    public partial class SalesReportForm : Form
    {
        private readonly FishFarmContext _context = null!;
        
        // Controls
        private TabControl _reportTabControl = null!;
        
        // Tab 1: Daily/Monthly Sales
        private DateTimePicker _dailyFromDatePicker = null!;
        private DateTimePicker _dailyToDatePicker = null!;
        private DataGridView _dailySalesGrid = null!;
        private TextBox _dailySummaryTextBox = null!;
        private Button _generateDailySalesButton = null!;
        
        // Tab 2: Sales by Customer
        private ComboBox _customerFilterComboBox = null!;
        private DateTimePicker _customerFromDatePicker = null!;
        private DateTimePicker _customerToDatePicker = null!;
        private DataGridView _customerSalesGrid = null!;
        private TextBox _customerSummaryTextBox = null!;
        private Button _generateCustomerSalesButton = null!;
        
        // Tab 3: Pending Invoices
        private ComboBox _pendingStatusComboBox = null!;
        private DataGridView _pendingInvoicesGrid = null!;
        private TextBox _pendingSummaryTextBox = null!;
        private Button _generatePendingButton = null!;
        
        // Tab 4: Top Customers
        private NumericUpDown _topCountNumeric = null!;
        private DateTimePicker _topFromDatePicker = null!;
        private DateTimePicker _topToDatePicker = null!;
        private DataGridView _topCustomersGrid = null!;
        private Panel _topChartPanel = null!;
        private Button _generateTopCustomersButton = null!;
        
        // Tab 5: Sales Summary
        private DateTimePicker _summaryFromDatePicker = null!;
        private DateTimePicker _summaryToDatePicker = null!;
        private TextBox _summaryDetailsTextBox = null!;
        private Panel _summaryChartPanel = null!;
        private Button _generateSummaryButton = null!;

        public SalesReportForm(FishFarmContext context)
        {
            // ✅ فحص الصلاحيات أولاً
            if (!AuthenticationService.HasPermission(UserRole.Admin, UserRole.Manager, UserRole.Accountant, UserRole.SalesStaff, UserRole.Viewer))
            {
                MessageBox.Show(
                    "ليس لديك صلاحية لعرض تقارير المبيعات.\nيرجى الاتصال بالمدير لمنحك الصلاحيات اللازمة.",
                    "خطأ في الصلاحيات",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Warning
                );
                
                LoggingService.LogWarning(
                    "محاولة وصول غير مصرح بها من {Username} إلى تقرير المبيعات",
                    AuthenticationService.CurrentUsername
                );
                
                this.Close();
                return;
            }
            
            _context = context;
            InitializeComponent();
            LoadInitialDataAsync().ConfigureAwait(false);
            
            try 
            { 
                ThemeManager.ApplyTheme(this); 
            } 
            catch (Exception ex) 
            { 
                LoggingService.LogWarning("فشل تطبيق الثيم على نموذج تقارير المبيعات: {Error}", ex.Message);
            }
        }

        private void InitializeComponent()
        {
            this.Text = "تقارير المبيعات";
            this.Size = new Size(1400, 900);
            this.StartPosition = FormStartPosition.CenterScreen;
            this.RightToLeft = RightToLeft.Yes;
            this.RightToLeftLayout = true;
            this.Font = ThemeManager.MainFont;

            // Create main tab control
            _reportTabControl = new TabControl
            {
                Dock = DockStyle.Fill,
                Font = ThemeManager.SubtitleFont
            };

            // Create tabs
            var dailySalesTab = new TabPage("المبيعات اليومية/الشهرية");
            var customerSalesTab = new TabPage("المبيعات حسب العميل");
            var pendingInvoicesTab = new TabPage("الفواتير المعلقة");
            var topCustomersTab = new TabPage("أعلى العملاء");
            var summaryTab = new TabPage("ملخص المبيعات");

            _reportTabControl.TabPages.AddRange(new TabPage[] { 
                dailySalesTab, customerSalesTab, pendingInvoicesTab, topCustomersTab, summaryTab 
            });

            // Setup each tab
            SetupDailySalesTab(dailySalesTab);
            SetupCustomerSalesTab(customerSalesTab);
            SetupPendingInvoicesTab(pendingInvoicesTab);
            SetupTopCustomersTab(topCustomersTab);
            SetupSummaryTab(summaryTab);

            this.Controls.Add(_reportTabControl);
        }

        #region Tab 1: Daily/Monthly Sales

        private void SetupDailySalesTab(TabPage tab)
        {
            var mainPanel = new Panel { Dock = DockStyle.Fill, Padding = new Padding(10) };
            
            // Filter panel
            var filterPanel = new Panel { Dock = DockStyle.Top, Height = 80 };
            
            var fromLabel = new Label { Text = "من تاريخ:", Location = new Point(1200, 15), AutoSize = true };
            _dailyFromDatePicker = new DateTimePicker { Location = new Point(1000, 12), Width = 180 };
            _dailyFromDatePicker.Value = DateTime.Today.AddMonths(-1);
            
            var toLabel = new Label { Text = "إلى تاريخ:", Location = new Point(920, 15), AutoSize = true };
            _dailyToDatePicker = new DateTimePicker { Location = new Point(720, 12), Width = 180 };
            
            _generateDailySalesButton = new Button
            {
                Text = "إنشاء التقرير",
                Location = new Point(550, 10),
                Width = 150,
                Height = 35,
                BackColor = ThemeManager.SecondarySkyBlue,
                ForeColor = Color.White,
                FlatStyle = FlatStyle.Flat
            };
            _generateDailySalesButton.Click += GenerateDailySalesButton_Click;
            
            filterPanel.Controls.AddRange(new Control[] { 
                fromLabel, _dailyFromDatePicker, toLabel, _dailyToDatePicker, _generateDailySalesButton 
            });
            
            // Grid
            _dailySalesGrid = new DataGridView
            {
                Dock = DockStyle.Fill,
                AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill,
                SelectionMode = DataGridViewSelectionMode.FullRowSelect,
                MultiSelect = false,
                ReadOnly = true,
                AllowUserToAddRows = false,
                BackgroundColor = Color.White,
                BorderStyle = BorderStyle.Fixed3D
            };
            
            // Summary panel
            var summaryPanel = new Panel { Dock = DockStyle.Bottom, Height = 150 };
            _dailySummaryTextBox = new TextBox
            {
                Dock = DockStyle.Fill,
                Multiline = true,
                ReadOnly = true,
                Font = new Font("Consolas", 10F),
                BackColor = ThemeManager.GridAlternateRow
            };
            summaryPanel.Controls.Add(_dailySummaryTextBox);
            
            mainPanel.Controls.Add(_dailySalesGrid);
            mainPanel.Controls.Add(summaryPanel);
            mainPanel.Controls.Add(filterPanel);
            
            tab.Controls.Add(mainPanel);
        }

        private async void GenerateDailySalesButton_Click(object? sender, EventArgs e)
        {
            try
            {
                // ✅ تعطيل الزر أثناء التحميل
                _generateDailySalesButton.Enabled = false;
                Cursor = Cursors.WaitCursor;
                
                var fromDate = _dailyFromDatePicker.Value.Date;
                var toDate = _dailyToDatePicker.Value.Date.AddDays(1).AddSeconds(-1);

                var orders = await _context.SalesOrders
                    .AsNoTracking()
                    .Include(o => o.Customer)
                    .Include(o => o.Items)
                    .Where(o => o.OrderDate >= fromDate && o.OrderDate <= toDate)
                    .OrderBy(o => o.OrderDate)
                    .ToListAsync();
                
                // ✅ فحص Empty Collection
                if (!orders.Any())
                {
                    MessageBox.Show(
                        "لا توجد بيانات مبيعات في الفترة المحددة",
                        "تنبيه",
                        MessageBoxButtons.OK,
                        MessageBoxIcon.Information
                    );
                    return;
                }

                // Group by date
                var dailyData = orders.GroupBy(o => o.OrderDate.Date)
                    .Select(g => new
                    {
                        التاريخ = g.Key.ToString("yyyy/MM/dd"),
                        عدد_الأوامر = g.Count(),
                        إجمالي_المبيعات = g.Sum(o => o.SubTotal),
                        الضريبة = g.Sum(o => o.VATAmount),
                        الخصومات = g.Sum(o => o.DiscountAmount),
                        الإجمالي_الكلي = g.Sum(o => o.TotalAmount),
                        المدفوع = g.Sum(o => o.PaidAmount),
                        المتبقي = g.Sum(o => o.RemainingAmount)
                    })
                    .ToList();

                _dailySalesGrid.DataSource = dailyData;

                // Format currency columns
                foreach (DataGridViewColumn col in _dailySalesGrid.Columns)
                {
                    if (col.Name != "التاريخ" && col.Name != "عدد_الأوامر")
                    {
                        col.DefaultCellStyle.Format = "N2";
                        col.DefaultCellStyle.Alignment = DataGridViewContentAlignment.MiddleLeft;
                    }
                }

                // Calculate summary
                var totalOrders = orders.Count;
                var totalSales = orders.Sum(o => o.SubTotal);
                var totalVAT = orders.Sum(o => o.VATAmount);
                var totalDiscount = orders.Sum(o => o.DiscountAmount);
                var grandTotal = orders.Sum(o => o.TotalAmount);
                var totalPaid = orders.Sum(o => o.PaidAmount);
                var totalRemaining = orders.Sum(o => o.RemainingAmount);
                var avgOrderValue = totalOrders > 0 ? grandTotal / totalOrders : 0;

                _dailySummaryTextBox.Text = $@"
═══════════════════════════════════════════════════════════════
                    ملخص المبيعات - من {fromDate:yyyy/MM/dd} إلى {toDate:yyyy/MM/dd}
═══════════════════════════════════════════════════════════════

إجمالي عدد الأوامر:        {totalOrders:N0}
إجمالي المبيعات:            {totalSales:N2} ريال
إجمالي الضريبة (15%):       {totalVAT:N2} ريال
إجمالي الخصومات:            {totalDiscount:N2} ريال
الإجمالي الكلي:             {grandTotal:N2} ريال
المبلغ المدفوع:             {totalPaid:N2} ريال
المبلغ المتبقي:             {totalRemaining:N2} ريال
متوسط قيمة الأمر:           {avgOrderValue:N2} ريال

═══════════════════════════════════════════════════════════════";

                // ✅ Audit Log
                LoggingService.LogUserActivity(
                    AuthenticationService.CurrentUsername,
                    "GENERATE_DAILY_SALES_REPORT",
                    $"Period: {fromDate:yyyy-MM-dd} to {toDate:yyyy-MM-dd}, Orders: {totalOrders}"
                );
            }
            catch (DbUpdateException dbEx)
            {
                LoggingService.LogError(dbEx, "خطأ في قاعدة البيانات - تقرير المبيعات اليومية");
                MessageBox.Show(
                    "حدث خطأ في الاتصال بقاعدة البيانات. يرجى التحقق من الاتصال والمحاولة مرة أخرى.",
                    "خطأ في قاعدة البيانات",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Error
                );
            }
            catch (InvalidOperationException invEx)
            {
                LoggingService.LogError(invEx, "عملية غير صالحة - تقرير المبيعات اليومية");
                MessageBox.Show(
                    "حدث خطأ في معالجة البيانات. يرجى التحقق من المدخلات.",
                    "خطأ",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Error
                );
            }
            catch (Exception ex)
            {
                LoggingService.LogFatal(ex, "خطأ غير متوقع - تقرير المبيعات اليومية");
                MessageBox.Show(
                    $"حدث خطأ غير متوقع: {ex.Message}",
                    "خطأ",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Error
                );
            }
            finally
            {
                // ✅ إعادة تفعيل الزر
                _generateDailySalesButton.Enabled = true;
                Cursor = Cursors.Default;
            }
        }

        #endregion

        #region Tab 2: Sales by Customer

        private void SetupCustomerSalesTab(TabPage tab)
        {
            var mainPanel = new Panel { Dock = DockStyle.Fill, Padding = new Padding(10) };
            
            // Filter panel
            var filterPanel = new Panel { Dock = DockStyle.Top, Height = 80 };
            
            var customerLabel = new Label { Text = "العميل:", Location = new Point(1200, 15), AutoSize = true };
            _customerFilterComboBox = new ComboBox
            {
                Location = new Point(1000, 12),
                Width = 180,
                DropDownStyle = ComboBoxStyle.DropDownList
            };
            
            var fromLabel = new Label { Text = "من:", Location = new Point(920, 15), AutoSize = true };
            _customerFromDatePicker = new DateTimePicker { Location = new Point(720, 12), Width = 180 };
            _customerFromDatePicker.Value = DateTime.Today.AddMonths(-1);
            
            var toLabel = new Label { Text = "إلى:", Location = new Point(640, 15), AutoSize = true };
            _customerToDatePicker = new DateTimePicker { Location = new Point(440, 12), Width = 180 };
            
            _generateCustomerSalesButton = new Button
            {
                Text = "إنشاء التقرير",
                Location = new Point(270, 10),
                Width = 150,
                Height = 35,
                BackColor = ThemeManager.SecondarySkyBlue,
                ForeColor = Color.White,
                FlatStyle = FlatStyle.Flat
            };
            _generateCustomerSalesButton.Click += GenerateCustomerSalesButton_Click;
            
            filterPanel.Controls.AddRange(new Control[] { 
                customerLabel, _customerFilterComboBox, fromLabel, _customerFromDatePicker, 
                toLabel, _customerToDatePicker, _generateCustomerSalesButton 
            });
            
            // Grid
            _customerSalesGrid = new DataGridView
            {
                Dock = DockStyle.Fill,
                AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill,
                SelectionMode = DataGridViewSelectionMode.FullRowSelect,
                MultiSelect = false,
                ReadOnly = true,
                AllowUserToAddRows = false,
                BackgroundColor = Color.White
            };
            
            // Summary
            var summaryPanel = new Panel { Dock = DockStyle.Bottom, Height = 150 };
            _customerSummaryTextBox = new TextBox
            {
                Dock = DockStyle.Fill,
                Multiline = true,
                ReadOnly = true,
                Font = new Font("Consolas", 10F),
                BackColor = ThemeManager.GridAlternateRow
            };
            summaryPanel.Controls.Add(_customerSummaryTextBox);
            
            mainPanel.Controls.Add(_customerSalesGrid);
            mainPanel.Controls.Add(summaryPanel);
            mainPanel.Controls.Add(filterPanel);
            
            tab.Controls.Add(mainPanel);
        }

        private async void GenerateCustomerSalesButton_Click(object? sender, EventArgs e)
        {
            try
            {
                // ✅ تعطيل UI
                _generateCustomerSalesButton.Enabled = false;
                Cursor = Cursors.WaitCursor;
                
                var fromDate = _customerFromDatePicker.Value.Date;
                var toDate = _customerToDatePicker.Value.Date.AddDays(1).AddSeconds(-1);

                var query = _context.SalesOrders
                    .AsNoTracking()
                    .Include(o => o.Customer)
                    .Include(o => o.Items)
                    .Where(o => o.OrderDate >= fromDate && o.OrderDate <= toDate);

                // Filter by customer if selected
                if (_customerFilterComboBox.SelectedItem != null)
                {
                    var selectedCustomer = _customerFilterComboBox.SelectedItem as Customer;
                    if (selectedCustomer != null && selectedCustomer.Id > 0)
                    {
                        query = query.Where(o => o.CustomerId == selectedCustomer.Id);
                    }
                }

                var customerData = await query.GroupBy(o => new { o.CustomerId, o.Customer.Name, o.Customer.Type })
                    .Select(g => new
                    {
                        رقم_العميل = g.Key.CustomerId,
                        اسم_العميل = g.Key.Name,
                        نوع_العميل = g.Key.Type.ToString(),
                        عدد_الأوامر = g.Count(),
                        إجمالي_المبيعات = g.Sum(o => o.TotalAmount),
                        المدفوع = g.Sum(o => o.PaidAmount),
                        المتبقي = g.Sum(o => o.RemainingAmount),
                        متوسط_الأمر = g.Average(o => o.TotalAmount)
                    })
                    .OrderByDescending(c => c.إجمالي_المبيعات)
                    .ToListAsync();
                
                // ✅ فحص Empty
                if (!customerData.Any())
                {
                    MessageBox.Show(
                        "لا توجد بيانات مبيعات للعملاء في الفترة المحددة",
                        "تنبيه",
                        MessageBoxButtons.OK,
                        MessageBoxIcon.Information
                    );
                    return;
                }

                _customerSalesGrid.DataSource = customerData;

                // Format columns
                foreach (DataGridViewColumn col in _customerSalesGrid.Columns)
                {
                    if (col.Name.Contains("إجمالي") || col.Name.Contains("المدفوع") || col.Name.Contains("المتبقي") || col.Name.Contains("متوسط"))
                    {
                        col.DefaultCellStyle.Format = "N2";
                    }
                }

                // Summary
                var totalCustomers = customerData.Count;
                var totalOrders = customerData.Sum(c => c.عدد_الأوامر);
                var totalSales = customerData.Sum(c => c.إجمالي_المبيعات);
                var totalPaid = customerData.Sum(c => c.المدفوع);
                var totalRemaining = customerData.Sum(c => c.المتبقي);

                _customerSummaryTextBox.Text = $@"
═══════════════════════════════════════════════════════════════
                    تقرير المبيعات حسب العميل
═══════════════════════════════════════════════════════════════

عدد العملاء:                {totalCustomers:N0}
إجمالي الأوامر:             {totalOrders:N0}
إجمالي المبيعات:            {totalSales:N2} ريال
المبلغ المدفوع:             {totalPaid:N2} ريال
المبلغ المتبقي:             {totalRemaining:N2} ريال

═══════════════════════════════════════════════════════════════";

                // ✅ Audit Log
                LoggingService.LogUserActivity(
                    AuthenticationService.CurrentUsername,
                    "GENERATE_CUSTOMER_SALES_REPORT",
                    $"Period: {fromDate:yyyy-MM-dd} to {toDate:yyyy-MM-dd}, Customers: {totalCustomers}"
                );
            }
            catch (DbUpdateException dbEx)
            {
                LoggingService.LogError(dbEx, "خطأ في قاعدة البيانات - تقرير مبيعات العملاء");
                MessageBox.Show(
                    "حدث خطأ في الاتصال بقاعدة البيانات",
                    "خطأ",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Error
                );
            }
            catch (Exception ex)
            {
                LoggingService.LogFatal(ex, "خطأ غير متوقع - تقرير مبيعات العملاء");
                MessageBox.Show($"حدث خطأ: {ex.Message}", "خطأ", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
            finally
            {
                // ✅ إعادة تفعيل UI
                _generateCustomerSalesButton.Enabled = true;
                Cursor = Cursors.Default;
            }
        }

        #endregion

        #region Tab 3: Pending Invoices

        private void SetupPendingInvoicesTab(TabPage tab)
        {
            var mainPanel = new Panel { Dock = DockStyle.Fill, Padding = new Padding(10) };
            
            // Filter panel
            var filterPanel = new Panel { Dock = DockStyle.Top, Height = 80 };
            
            var statusLabel = new Label { Text = "الحالة:", Location = new Point(1200, 15), AutoSize = true };
            _pendingStatusComboBox = new ComboBox
            {
                Location = new Point(1000, 12),
                Width = 180,
                DropDownStyle = ComboBoxStyle.DropDownList
            };
            _pendingStatusComboBox.Items.AddRange(new object[] { "الكل", "جزئياً", "غير مدفوع" });
            _pendingStatusComboBox.SelectedIndex = 0;
            
            _generatePendingButton = new Button
            {
                Text = "إنشاء التقرير",
                Location = new Point(800, 10),
                Width = 150,
                Height = 35,
                BackColor = ThemeManager.SecondarySkyBlue,
                ForeColor = Color.White,
                FlatStyle = FlatStyle.Flat
            };
            _generatePendingButton.Click += GeneratePendingButton_Click;
            
            filterPanel.Controls.AddRange(new Control[] { statusLabel, _pendingStatusComboBox, _generatePendingButton });
            
            // Grid
            _pendingInvoicesGrid = new DataGridView
            {
                Dock = DockStyle.Fill,
                AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill,
                SelectionMode = DataGridViewSelectionMode.FullRowSelect,
                MultiSelect = false,
                ReadOnly = true,
                AllowUserToAddRows = false,
                BackgroundColor = Color.White
            };
            
            // Summary
            var summaryPanel = new Panel { Dock = DockStyle.Bottom, Height = 150 };
            _pendingSummaryTextBox = new TextBox
            {
                Dock = DockStyle.Fill,
                Multiline = true,
                ReadOnly = true,
                Font = new Font("Consolas", 10F),
                BackColor = ThemeManager.GridAlternateRow
            };
            summaryPanel.Controls.Add(_pendingSummaryTextBox);
            
            mainPanel.Controls.Add(_pendingInvoicesGrid);
            mainPanel.Controls.Add(summaryPanel);
            mainPanel.Controls.Add(filterPanel);
            
            tab.Controls.Add(mainPanel);
        }

        private async void GeneratePendingButton_Click(object? sender, EventArgs e)
        {
            try
            {
                // ✅ تعطيل UI
                _generatePendingButton.Enabled = false;
                Cursor = Cursors.WaitCursor;
                
                var query = _context.SalesOrders
                    .AsNoTracking()
                    .Include(o => o.Customer)
                    .Where(o => o.RemainingAmount > 0);

                // Filter by payment status
                var filterStatus = _pendingStatusComboBox.SelectedItem?.ToString();
                if (filterStatus == "غير مدفوع")
                {
                    query = query.Where(o => o.PaidAmount == 0);
                }
                else if (filterStatus == "جزئياً")
                {
                    query = query.Where(o => o.PaidAmount > 0 && o.RemainingAmount > 0);
                }

                var pendingData = await query.Select(o => new
                {
                    رقم_الطلب = o.OrderNumber,
                    العميل = o.Customer.Name,
                    تاريخ_الطلب = o.OrderDate,
                    تاريخ_التسليم_المتوقع = o.ExpectedDeliveryDate,
                    الإجمالي = o.TotalAmount,
                    المدفوع = o.PaidAmount,
                    المتبقي = o.RemainingAmount,
                    أيام_التأخير = (DateTime.Today - (o.ExpectedDeliveryDate ?? o.OrderDate)).Days,
                    الحالة = o.Status.ToString()
                })
                .OrderByDescending(o => o.المتبقي)
                .ToListAsync();
                
                // ✅ فحص Empty
                if (!pendingData.Any())
                {
                    MessageBox.Show(
                        "لا توجد فواتير معلقة",
                        "تنبيه",
                        MessageBoxButtons.OK,
                        MessageBoxIcon.Information
                    );
                    return;
                }

                _pendingInvoicesGrid.DataSource = pendingData;

                // Format columns
                foreach (DataGridViewColumn col in _pendingInvoicesGrid.Columns)
                {
                    if (col.Name.Contains("الإجمالي") || col.Name.Contains("المدفوع") || col.Name.Contains("المتبقي"))
                    {
                        col.DefaultCellStyle.Format = "N2";
                    }
                    else if (col.Name.Contains("تاريخ"))
                    {
                        col.DefaultCellStyle.Format = "yyyy/MM/dd";
                    }
                }

                // Highlight overdue
                foreach (DataGridViewRow row in _pendingInvoicesGrid.Rows)
                {
                    if (row.Cells["أيام_التأخير"].Value != null)
                    {
                        var daysOverdue = Convert.ToInt32(row.Cells["أيام_التأخير"].Value);
                        if (daysOverdue > 30)
                        {
                            row.DefaultCellStyle.BackColor = Color.FromArgb(255, 235, 235); // لون أحمر فاتح للتأخير
                        }
                        else if (daysOverdue > 0)
                        {
                            row.DefaultCellStyle.BackColor = Color.FromArgb(255, 252, 220); // لون أصفر فاتح للجزئي
                        }
                    }
                }

                // Summary
                var totalInvoices = pendingData.Count;
                var totalAmount = pendingData.Sum(p => p.الإجمالي);
                var totalPaid = pendingData.Sum(p => p.المدفوع);
                var totalRemaining = pendingData.Sum(p => p.المتبقي);
                var overdueCount = pendingData.Count(p => p.أيام_التأخير > 0);
                var unpaidCount = pendingData.Count(p => p.المدفوع == 0);

                _pendingSummaryTextBox.Text = $@"
═══════════════════════════════════════════════════════════════
                    تقرير الفواتير المعلقة
═══════════════════════════════════════════════════════════════

عدد الفواتير المعلقة:      {totalInvoices:N0}
  - غير مدفوعة:             {unpaidCount:N0}
  - مدفوعة جزئياً:          {totalInvoices - unpaidCount:N0}
  - متأخرة:                 {overdueCount:N0}

إجمالي المبالغ:             {totalAmount:N2} ريال
المبلغ المدفوع:             {totalPaid:N2} ريال
المبلغ المتبقي:             {totalRemaining:N2} ريال

═══════════════════════════════════════════════════════════════
ملاحظة: الصفوف الحمراء = متأخرة أكثر من 30 يوم
        الصفوف الصفراء = متأخرة أقل من 30 يوم
═══════════════════════════════════════════════════════════════";

                // ✅ Audit Log
                LoggingService.LogUserActivity(
                    AuthenticationService.CurrentUsername,
                    "GENERATE_PENDING_INVOICES_REPORT",
                    $"Total: {totalInvoices}, Overdue: {overdueCount}, Unpaid: {unpaidCount}"
                );
            }
            catch (DbUpdateException dbEx)
            {
                LoggingService.LogError(dbEx, "خطأ في قاعدة البيانات - تقرير الفواتير المعلقة");
                MessageBox.Show("حدث خطأ في الاتصال بقاعدة البيانات", "خطأ", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
            catch (Exception ex)
            {
                LoggingService.LogFatal(ex, "خطأ غير متوقع - تقرير الفواتير المعلقة");
                MessageBox.Show($"حدث خطأ: {ex.Message}", "خطأ", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
            finally
            {
                // ✅ إعادة تفعيل UI
                _generatePendingButton.Enabled = true;
                Cursor = Cursors.Default;
            }
        }

        #endregion

        #region Tab 4: Top Customers

        private void SetupTopCustomersTab(TabPage tab)
        {
            var mainPanel = new Panel { Dock = DockStyle.Fill, Padding = new Padding(10) };
            
            // Filter panel
            var filterPanel = new Panel { Dock = DockStyle.Top, Height = 80 };
            
            var topLabel = new Label { Text = "أعلى:", Location = new Point(1200, 15), AutoSize = true };
            _topCountNumeric = new NumericUpDown
            {
                Location = new Point(1120, 12),
                Width = 60,
                Minimum = 5,
                Maximum = 50,
                Value = 10
            };
            
            var fromLabel = new Label { Text = "من:", Location = new Point(1040, 15), AutoSize = true };
            _topFromDatePicker = new DateTimePicker { Location = new Point(840, 12), Width = 180 };
            _topFromDatePicker.Value = DateTime.Today.AddMonths(-6);
            
            var toLabel = new Label { Text = "إلى:", Location = new Point(760, 15), AutoSize = true };
            _topToDatePicker = new DateTimePicker { Location = new Point(560, 12), Width = 180 };
            
            _generateTopCustomersButton = new Button
            {
                Text = "إنشاء التقرير",
                Location = new Point(390, 10),
                Width = 150,
                Height = 35,
                BackColor = ThemeManager.SecondarySkyBlue,
                ForeColor = Color.White,
                FlatStyle = FlatStyle.Flat
            };
            _generateTopCustomersButton.Click += GenerateTopCustomersButton_Click;
            
            filterPanel.Controls.AddRange(new Control[] { 
                topLabel, _topCountNumeric, fromLabel, _topFromDatePicker, 
                toLabel, _topToDatePicker, _generateTopCustomersButton 
            });
            
            // Split container
            var splitContainer = new SplitContainer
            {
                Dock = DockStyle.Fill,
                Orientation = Orientation.Horizontal,
                SplitterDistance = 350
            };
            
            // Grid
            _topCustomersGrid = new DataGridView
            {
                Dock = DockStyle.Fill,
                AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill,
                SelectionMode = DataGridViewSelectionMode.FullRowSelect,
                MultiSelect = false,
                ReadOnly = true,
                AllowUserToAddRows = false,
                BackgroundColor = Color.White
            };
            
            // Chart panel
            _topChartPanel = new Panel
            {
                Dock = DockStyle.Fill,
                BackColor = Color.White,
                BorderStyle = BorderStyle.Fixed3D
            };
            _topChartPanel.Paint += TopChartPanel_Paint;
            
            splitContainer.Panel1.Controls.Add(_topCustomersGrid);
            splitContainer.Panel2.Controls.Add(_topChartPanel);
            
            mainPanel.Controls.Add(splitContainer);
            mainPanel.Controls.Add(filterPanel);
            
            tab.Controls.Add(mainPanel);
        }

        private async void GenerateTopCustomersButton_Click(object? sender, EventArgs e)
        {
            try
            {
                _generateTopCustomersButton.Enabled = false;
                Cursor = Cursors.WaitCursor;
                
                var topCount = (int)_topCountNumeric.Value;
                var fromDate = _topFromDatePicker.Value.Date;
                var toDate = _topToDatePicker.Value.Date.AddDays(1).AddSeconds(-1);

                var topCustomers = await _context.SalesOrders
                    .AsNoTracking()
                    .Include(o => o.Customer)
                    .Where(o => o.OrderDate >= fromDate && o.OrderDate <= toDate)
                    .GroupBy(o => new { o.CustomerId, o.Customer.Name, o.Customer.Type })
                    .Select(g => new
                    {
                        الترتيب = 0,
                        رقم_العميل = g.Key.CustomerId,
                        اسم_العميل = g.Key.Name,
                        نوع_العميل = g.Key.Type.ToString(),
                        عدد_الأوامر = g.Count(),
                        إجمالي_المبيعات = g.Sum(o => o.TotalAmount),
                        المدفوع = g.Sum(o => o.PaidAmount),
                        المتبقي = g.Sum(o => o.RemainingAmount),
                        متوسط_الأمر = g.Average(o => o.TotalAmount)
                    })
                    .OrderByDescending(c => c.إجمالي_المبيعات)
                    .Take(topCount)
                    .ToListAsync();

                // Add ranking
                var rankedCustomers = topCustomers.Select((c, index) => new
                {
                    الترتيب = index + 1,
                    c.رقم_العميل,
                    c.اسم_العميل,
                    c.نوع_العميل,
                    c.عدد_الأوامر,
                    c.إجمالي_المبيعات,
                    c.المدفوع,
                    c.المتبقي,
                    c.متوسط_الأمر
                }).ToList();

                _topCustomersGrid.DataSource = rankedCustomers;

                // Format columns
                foreach (DataGridViewColumn col in _topCustomersGrid.Columns)
                {
                    if (col.Name.Contains("إجمالي") || col.Name.Contains("المدفوع") || col.Name.Contains("المتبقي") || col.Name.Contains("متوسط"))
                    {
                        col.DefaultCellStyle.Format = "N2";
                    }
                }

                // Refresh chart
                _topChartPanel.Invalidate();
                
                // ✅ Audit Log
                LoggingService.LogUserActivity(
                    AuthenticationService.CurrentUsername,
                    "GENERATE_TOP_CUSTOMERS_REPORT",
                    $"Period: {fromDate:yyyy-MM-dd} to {toDate:yyyy-MM-dd}, Top: {topCount}"
                );
            }
            catch (DbUpdateException dbEx)
            {
                LoggingService.LogError(dbEx, "خطأ في قاعدة البيانات - تقرير أعلى العملاء");
                MessageBox.Show("حدث خطأ في الاتصال بقاعدة البيانات", "خطأ", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
            catch (Exception ex)
            {
                LoggingService.LogFatal(ex, "خطأ غير متوقع - تقرير أعلى العملاء");
                MessageBox.Show($"حدث خطأ: {ex.Message}", "خطأ", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
            finally
            {
                // ✅ إعادة تفعيل UI
                _generateTopCustomersButton.Enabled = true;
                Cursor = Cursors.Default;
            }
        }

        private void TopChartPanel_Paint(object? sender, PaintEventArgs e)
        {
            if (_topCustomersGrid.DataSource == null) return;

            try
            {
                var graphics = e?.Graphics;
                if (graphics == null) return;
                graphics.Clear(Color.White);

                var data = _topCustomersGrid.DataSource as System.Collections.IList;
                if (data == null || data.Count == 0) return;

                // Chart settings
                var chartArea = new Rectangle(60, 40, _topChartPanel.Width - 100, _topChartPanel.Height - 100);
                var maxValue = 0m;

                // Find max value
                foreach (var item in data)
                {
                    var salesProp = item.GetType().GetProperty("إجمالي_المبيعات");
                    var salesObj = salesProp?.GetValue(item);
                    var sales = salesObj is decimal d ? d : 0m;
                    if (sales > maxValue) maxValue = sales;
                }

                if (maxValue == 0) return;

                // Draw title
                var titleFont = ThemeManager.SubtitleFont;
                var titleText = "أعلى العملاء حسب إجمالي المبيعات";
                var titleSize = graphics.MeasureString(titleText, titleFont);
                graphics.DrawString(titleText, titleFont, Brushes.Black, 
                    (_topChartPanel.Width - titleSize.Width) / 2, 10);

                // Draw bars
                var barWidth = chartArea.Width / data.Count;
                var colors = new Color[] { 
                    ThemeManager.SecondarySkyBlue, ThemeManager.SecondarySkyBlue, 
                    ThemeManager.SecondarySkyBlue, ThemeManager.SecondaryAquaGreen 
                };

                for (int i = 0; i < data.Count; i++)
                {
                    var item = data[i];
                    if (item == null) continue;
                    var salesProp = item.GetType().GetProperty("إجمالي_المبيعات");
                    var nameProp = item.GetType().GetProperty("اسم_العميل");
                    var salesObj = salesProp?.GetValue(item);
                    var sales = salesObj is decimal d ? d : 0m;
                    var nameObj = nameProp?.GetValue(item);
                    var customerName = nameObj?.ToString() ?? string.Empty;

                    var barHeight = (int)((sales / maxValue) * chartArea.Height);
                    var x = chartArea.X + (i * barWidth) + 5;
                    var y = chartArea.Bottom - barHeight;

                    // Draw bar
                    var barColor = colors[i % colors.Length];
                    graphics.FillRectangle(new SolidBrush(barColor), x, y, barWidth - 10, barHeight);
                    graphics.DrawRectangle(Pens.Black, x, y, barWidth - 10, barHeight);

                    // Draw value on top
                    var valueText = $"{sales:N0}";
                    var valueFont = ThemeManager.SmallFont;
                    var valueSize = graphics.MeasureString(valueText, valueFont);
                    graphics.DrawString(valueText, valueFont, Brushes.Black, 
                        x + (barWidth - 10 - valueSize.Width) / 2, y - 20);

                    // Draw customer name at bottom (rotated)
                    var nameFont = ThemeManager.SmallFont;
                    var state = graphics.Save();
                    graphics.TranslateTransform(x + barWidth / 2, chartArea.Bottom + 10);
                    graphics.RotateTransform(-45);
                    graphics.DrawString(customerName, nameFont, Brushes.Black, 0, 0);
                    graphics.Restore(state);
                }

                // Draw Y-axis labels
                var labelFont = ThemeManager.SmallFont;
                for (int i = 0; i <= 5; i++)
                {
                    var value = maxValue * i / 5;
                    var y = chartArea.Bottom - (chartArea.Height * i / 5);
                    graphics.DrawString($"{value:N0}", labelFont, Brushes.Black, 5, y - 8);
                    graphics.DrawLine(Pens.LightGray, chartArea.X, y, chartArea.Right, y);
                }
            }
            catch { }
        }

        #endregion

        #region Tab 5: Sales Summary

        private void SetupSummaryTab(TabPage tab)
        {
            var mainPanel = new Panel { Dock = DockStyle.Fill, Padding = new Padding(10) };
            
            // Filter panel
            var filterPanel = new Panel { Dock = DockStyle.Top, Height = 80 };
            
            var fromLabel = new Label { Text = "من تاريخ:", Location = new Point(1200, 15), AutoSize = true };
            _summaryFromDatePicker = new DateTimePicker { Location = new Point(1000, 12), Width = 180 };
            _summaryFromDatePicker.Value = DateTime.Today.AddMonths(-1);
            
            var toLabel = new Label { Text = "إلى تاريخ:", Location = new Point(920, 15), AutoSize = true };
            _summaryToDatePicker = new DateTimePicker { Location = new Point(720, 12), Width = 180 };
            
            _generateSummaryButton = new Button
            {
                Text = "إنشاء التقرير",
                Location = new Point(550, 10),
                Width = 150,
                Height = 35,
                BackColor = ThemeManager.SecondarySkyBlue,
                ForeColor = Color.White,
                FlatStyle = FlatStyle.Flat
            };
            _generateSummaryButton.Click += GenerateSummaryButton_Click;
            
            filterPanel.Controls.AddRange(new Control[] { 
                fromLabel, _summaryFromDatePicker, toLabel, _summaryToDatePicker, _generateSummaryButton 
            });
            
            // Split container
            var splitContainer = new SplitContainer
            {
                Dock = DockStyle.Fill,
                Orientation = Orientation.Horizontal,
                SplitterDistance = 300
            };
            
            // Summary text
            _summaryDetailsTextBox = new TextBox
            {
                Dock = DockStyle.Fill,
                Multiline = true,
                ReadOnly = true,
                Font = new Font("Consolas", 11F),
                BackColor = Color.FromArgb(240, 240, 240),
                ScrollBars = ScrollBars.Vertical
            };
            
            // Chart panel
            _summaryChartPanel = new Panel
            {
                Dock = DockStyle.Fill,
                BackColor = Color.White,
                BorderStyle = BorderStyle.Fixed3D
            };
            _summaryChartPanel.Paint += SummaryChartPanel_Paint;
            
            splitContainer.Panel1.Controls.Add(_summaryDetailsTextBox);
            splitContainer.Panel2.Controls.Add(_summaryChartPanel);
            
            mainPanel.Controls.Add(splitContainer);
            mainPanel.Controls.Add(filterPanel);
            
            tab.Controls.Add(mainPanel);
        }

        private async void GenerateSummaryButton_Click(object? sender, EventArgs e)
        {
            try
            {
                _generateSummaryButton.Enabled = false;
                Cursor = Cursors.WaitCursor;
                
                var fromDate = _summaryFromDatePicker.Value.Date;
                var toDate = _summaryToDatePicker.Value.Date.AddDays(1).AddSeconds(-1);

                var orders = await _context.SalesOrders
                    .AsNoTracking()
                    .Include(o => o.Customer)
                    .Include(o => o.Items)
                    .Where(o => o.OrderDate >= fromDate && o.OrderDate <= toDate)
                    .ToListAsync();

                // Calculate statistics
                var totalOrders = orders.Count;
                var totalCustomers = orders.Select(o => o.CustomerId).Distinct().Count();
                var totalItems = orders.SelectMany(o => o.Items).Sum(i => i.Quantity);
                
                var totalSubTotal = orders.Sum(o => o.SubTotal);
                var totalVAT = orders.Sum(o => o.VATAmount);
                var totalDiscount = orders.Sum(o => o.DiscountAmount);
                var grandTotal = orders.Sum(o => o.TotalAmount);
                var totalPaid = orders.Sum(o => o.PaidAmount);
                var totalRemaining = orders.Sum(o => o.RemainingAmount);
                
                var avgOrderValue = totalOrders > 0 ? grandTotal / totalOrders : 0;
                var collectionRate = grandTotal > 0 ? (totalPaid / grandTotal) * 100 : 0;

                // By status
                var byStatus = orders.GroupBy(o => o.Status)
                    .Select(g => new { Status = g.Key, Count = g.Count(), Total = g.Sum(o => o.TotalAmount) })
                    .OrderByDescending(s => s.Total)
                    .ToList();

                // By customer type
                var byType = orders.GroupBy(o => o.Customer.Type)
                    .Select(g => new { Type = g.Key, Count = g.Count(), Total = g.Sum(o => o.TotalAmount) })
                    .OrderByDescending(t => t.Total)
                    .ToList();

                _summaryDetailsTextBox.Text = $@"
╔═══════════════════════════════════════════════════════════════════════╗
║                         ملخص المبيعات الشامل                          ║
║                  من {fromDate:yyyy/MM/dd} إلى {toDate:yyyy/MM/dd}                    ║
╚═══════════════════════════════════════════════════════════════════════╝

┌─────────────────────────────────────────────────────────────────────┐
│ الإحصائيات العامة:                                                  │
├─────────────────────────────────────────────────────────────────────┤
│ إجمالي عدد الأوامر:              {totalOrders,-30:N0} │
│ عدد العملاء:                     {totalCustomers,-30:N0} │
│ إجمالي الكمية المباعة:           {totalItems,-30:N2} كجم │
└─────────────────────────────────────────────────────────────────────┘

┌─────────────────────────────────────────────────────────────────────┐
│ المبالغ المالية:                                                    │
├─────────────────────────────────────────────────────────────────────┤
│ إجمالي المبيعات (قبل الضريبة):   {totalSubTotal,-30:N2} ريال │
│ ضريبة القيمة المضافة (15%):       {totalVAT,-30:N2} ريال │
│ الخصومات الممنوحة:                {totalDiscount,-30:N2} ريال │
│ ─────────────────────────────────────────────────────────────────── │
│ الإجمالي الكلي:                   {grandTotal,-30:N2} ريال │
│ المبلغ المدفوع:                   {totalPaid,-30:N2} ريال │
│ المبلغ المتبقي:                   {totalRemaining,-30:N2} ريال │
└─────────────────────────────────────────────────────────────────────┘

┌─────────────────────────────────────────────────────────────────────┐
│ المؤشرات:                                                           │
├─────────────────────────────────────────────────────────────────────┤
│ متوسط قيمة الأمر:                 {avgOrderValue,-30:N2} ريال │
│ نسبة التحصيل:                     {collectionRate,-30:N2} % │
│ نسبة الخصم:                       {(totalDiscount / totalSubTotal * 100),-30:N2} % │
└─────────────────────────────────────────────────────────────────────┘

┌─────────────────────────────────────────────────────────────────────┐
│ المبيعات حسب حالة الطلب:                                            │
├─────────────────────────────────────────────────────────────────────┤
{string.Join("\n", byStatus.Select(s => $"│ {s.Status.ToString(),-30} {s.Count,-10:N0} {s.Total,-20:N2} ريال │"))}
└─────────────────────────────────────────────────────────────────────┘

┌─────────────────────────────────────────────────────────────────────┐
│ المبيعات حسب نوع العميل:                                            │
├─────────────────────────────────────────────────────────────────────┤
{string.Join("\n", byType.Select(t => $"│ {t.Type.ToString(),-30} {t.Count,-10:N0} {t.Total,-20:N2} ريال │"))}
└─────────────────────────────────────────────────────────────────────┘

═══════════════════════════════════════════════════════════════════════";

                _summaryChartPanel.Invalidate();
                
                // ✅ Audit Log
                LoggingService.LogUserActivity(
                    AuthenticationService.CurrentUsername,
                    "GENERATE_SALES_SUMMARY_REPORT",
                    $"Period: {fromDate:yyyy-MM-dd} to {toDate:yyyy-MM-dd}, Orders: {totalOrders}"
                );
            }
            catch (DbUpdateException dbEx)
            {
                LoggingService.LogError(dbEx, "خطأ في قاعدة البيانات - ملخص المبيعات");
                MessageBox.Show("حدث خطأ في الاتصال بقاعدة البيانات", "خطأ", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
            catch (Exception ex)
            {
                LoggingService.LogFatal(ex, "خطأ غير متوقع - ملخص المبيعات");
                MessageBox.Show($"حدث خطأ: {ex.Message}", "خطأ", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
            finally
            {
                // ✅ إعادة تفعيل UI
                _generateSummaryButton.Enabled = true;
                Cursor = Cursors.Default;
            }
        }

        private void SummaryChartPanel_Paint(object? sender, PaintEventArgs e)
        {
            try
            {
                var graphics = e.Graphics;
                graphics.Clear(Color.White);

                var fromDate = _summaryFromDatePicker.Value.Date;
                var toDate = _summaryToDatePicker.Value.Date.AddDays(1).AddSeconds(-1);

                var orders = _context.SalesOrders
                    .Where(o => o.OrderDate >= fromDate && o.OrderDate <= toDate)
                    .ToList();

                if (orders.Count == 0) return;

                // Pie chart data
                var byType = orders.GroupBy(o => o.Customer.Type)
                    .Select(g => new { Type = g.Key.ToString(), Total = g.Sum(o => o.TotalAmount) })
                    .OrderByDescending(t => t.Total)
                    .ToList();

                var grandTotal = byType.Sum(t => t.Total);
                if (grandTotal == 0) return;

                // Draw title
                var titleFont = ThemeManager.SubtitleFont;
                var titleText = "توزيع المبيعات حسب نوع العميل";
                var titleSize = graphics.MeasureString(titleText, titleFont);
                graphics.DrawString(titleText, titleFont, Brushes.Black, 
                    (_summaryChartPanel.Width - titleSize.Width) / 2, 10);

                // Pie chart settings
                var centerX = _summaryChartPanel.Width / 3;
                var centerY = _summaryChartPanel.Height / 2 + 20;
                var radius = Math.Min(centerX, centerY) - 60;

                var colors = new Color[] {
                    ThemeManager.SecondarySkyBlue, ThemeManager.WarningAmber,
                    ThemeManager.SuccessGreen, ThemeManager.ErrorRed,
                    ThemeManager.WarningAmber, ThemeManager.InfoBlue,
                    ThemeManager.PrimaryDeepBlue
                };

                float startAngle = 0;
                var legendY = 50;
                var legendX = centerX + radius + 50;

                for (int i = 0; i < byType.Count; i++)
                {
                    var item = byType[i];
                    var percentage = (float)(item.Total / grandTotal * 100);
                    var sweepAngle = (float)(item.Total / grandTotal * 360);

                    // Draw pie slice
                    var color = colors[i % colors.Length];
                    graphics.FillPie(new SolidBrush(color), 
                        centerX - radius, centerY - radius, radius * 2, radius * 2, 
                        startAngle, sweepAngle);
                    graphics.DrawPie(Pens.White, 
                        centerX - radius, centerY - radius, radius * 2, radius * 2, 
                        startAngle, sweepAngle);

                    // Draw legend
                    graphics.FillRectangle(new SolidBrush(color), legendX, legendY, 20, 20);
                    graphics.DrawRectangle(Pens.Black, legendX, legendY, 20, 20);
                    
                    var legendFont = ThemeManager.SmallFont;
                    var legendText = $"{item.Type}: {percentage:F1}% ({item.Total:N0} ريال)";
                    graphics.DrawString(legendText, legendFont, Brushes.Black, legendX + 30, legendY);

                    startAngle += sweepAngle;
                    legendY += 35;
                }
            }
            catch { }
        }

        #endregion

        private async Task LoadInitialDataAsync()
        {
            try
            {
                // Load customers for filter
                var customers = await _context.Customers
                    .AsNoTracking()
                    .Where(c => c.Status == CustomerStatus.Active)
                    .OrderBy(c => c.Name)
                    .ToListAsync();
                _customerFilterComboBox.DataSource = customers;
                _customerFilterComboBox.DisplayMember = "Name";
                _customerFilterComboBox.ValueMember = "Id";
            }
            catch (Exception ex)
            {
                LoggingService.LogError(ex, "خطأ في تحميل البيانات الأولية - تقرير المبيعات");
                MessageBox.Show($"خطأ في تحميل العملاء: {ex.Message}", "خطأ", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        /// <summary>
        /// ✅ Dispose override لتحرير الموارد
        /// </summary>
        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                try
                {
                    _context?.Dispose();
                }
                catch (Exception ex)
                {
                    LoggingService.LogWarning("خطأ أثناء Dispose للـ Context - تقرير المبيعات: {Error}", ex.Message);
                }
            }
            base.Dispose(disposing);
        }
    }
}