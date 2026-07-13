using System;
using System.Drawing;
using System.Linq;
using System.Threading.Tasks;
using System.Windows.Forms;
using Microsoft.EntityFrameworkCore;
using ClosedXML.Excel;
using FishFarmManager.Data;
using FishFarmManager.Models;
using FishFarmManager.Services;

namespace FishFarmManager.Forms
{
    /// <summary>
    /// نموذج تقارير ضريبة القيمة المضافة
    /// VAT Reports Form - 5 Comprehensive Reports
    /// </summary>
    public partial class VATReportsForm : Form
    {
        private readonly FishFarmContext _context;
        
        // Tab Control
        private TabControl _reportTabControl = null!;
        
        // Tab 1: Tax Invoices Report
        private DateTimePicker _invoicesFromDatePicker = null!;
        private DateTimePicker _invoicesToDatePicker = null!;
        private ComboBox _invoiceStatusComboBox = null!;
        private DataGridView _invoicesGrid = null!;
        private TextBox _invoicesSummaryTextBox = null!;
        private Button _generateInvoicesButton = null!;
        private Button _exportInvoicesButton = null!;
        
        // Tab 2: Output VAT Report (Sales Tax)
        private DateTimePicker _outputFromDatePicker = null!;
        private DateTimePicker _outputToDatePicker = null!;
        private DataGridView _outputVATGrid = null!;
        private Panel _outputChartPanel = null!;
        private TextBox _outputSummaryTextBox = null!;
        private Button _generateOutputButton = null!;
        
        // Tab 3: Input VAT Report (Purchase Tax)
        private DateTimePicker _inputFromDatePicker = null!;
        private DateTimePicker _inputToDatePicker = null!;
        private DataGridView _inputVATGrid = null!;
        private Panel _inputChartPanel = null!;
        private TextBox _inputSummaryTextBox = null!;
        private Button _generateInputButton = null!;
        
        // Tab 4: VAT Returns Report
        private ComboBox _returnPeriodComboBox = null!;
        private DataGridView _returnsGrid = null!;
        private TextBox _returnsSummaryTextBox = null!;
        private Button _generateReturnsButton = null!;
        
        // Tab 5: VAT Dashboard
        private Panel _kpiPanel = null!;
        private Panel _monthlyTrendPanel = null!;
        private Panel _vatBreakdownPanel = null!;
        private TextBox _dashboardSummaryTextBox = null!;
        private Button _refreshDashboardButton = null!;

        public VATReportsForm(FishFarmContext context)
        {
            _context = context ?? throw new ArgumentNullException(nameof(context));

            if (!AuthenticationService.HasPermission(UserRole.Admin, UserRole.Manager, UserRole.Accountant))
            {
                MessageBox.Show(
                    "ليس لديك صلاحية لعرض تقارير الضريبة.\nيرجى الاتصال بالمدير لمنحك الصلاحيات اللازمة.",
                    "خطأ في الصلاحيات",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Warning
                );
                
                LoggingService.LogWarning($"محاولة وصول غير مصرح بها من {AuthenticationService.CurrentUsername} إلى تقارير الضريبة");
                
                this.Load += (s, e) => this.Close();
                return;
            }

            InitializeComponent();
            LoadInitialDataAsync().ConfigureAwait(false);
            
            try
            {
                ThemeManager.ApplyTheme(this);
            }
            catch (Exception ex)
            {
                LoggingService.LogWarning($"فشل تطبيق الثيم: {ex.Message}");
            }
            
            LoggingService.LogInfo("VATReportsForm initialized successfully");
        }

        private void InitializeComponent()
        {
            this.Text = "تقارير ضريبة القيمة المضافة - VAT Reports";
            this.Size = new Size(1600, 900);
            this.StartPosition = FormStartPosition.CenterScreen;
            this.WindowState = FormWindowState.Maximized;
            this.RightToLeft = RightToLeft.Yes;
            this.RightToLeftLayout = true;
            this.Font = new Font("Cairo", 10F);

            var mainPanel = new Panel 
            { 
                Dock = DockStyle.Fill, 
                Padding = new Padding(10), 
                BackColor = Color.FromArgb(240, 240, 240) 
            };

            var titleLabel = new Label
            {
                Text = "تقارير ضريبة القيمة المضافة - VAT Reports",
                Dock = DockStyle.Top,
                Height = 50,
                Font = new Font("Cairo", 14F, FontStyle.Bold),
                ForeColor = Color.FromArgb(46, 92, 138),
                TextAlign = ContentAlignment.MiddleCenter,
                BackColor = Color.White
            };
            mainPanel.Controls.Add(titleLabel);

            _reportTabControl = new TabControl
            {
                Dock = DockStyle.Fill,
                Font = new Font("Cairo", 10F)
            };

            // Create 5 tabs
            var invoicesTab = new TabPage("سجل الفواتير الضريبية");
            var outputVATTab = new TabPage("ضريبة المبيعات (Output VAT)");
            var inputVATTab = new TabPage("ضريبة المشتريات (Input VAT)");
            var returnsTab = new TabPage("الإقرارات الضريبية");
            var dashboardTab = new TabPage("لوحة التحكم");

            _reportTabControl.TabPages.AddRange(new TabPage[] 
            { 
                invoicesTab, outputVATTab, inputVATTab, returnsTab, dashboardTab 
            });

            // Setup each tab
            SetupInvoicesTab(invoicesTab);
            SetupOutputVATTab(outputVATTab);
            SetupInputVATTab(inputVATTab);
            SetupReturnsTab(returnsTab);
            SetupDashboardTab(dashboardTab);

            mainPanel.Controls.Add(_reportTabControl);
            this.Controls.Add(mainPanel);
        }

        #region Tab 1: Tax Invoices Report

        private void SetupInvoicesTab(TabPage tab)
        {
            var mainPanel = new Panel { Dock = DockStyle.Fill, Padding = new Padding(10), BackColor = Color.White };
            
            // Filter panel
            var filterPanel = new Panel { Dock = DockStyle.Top, Height = 80, BackColor = Color.FromArgb(250, 250, 250) };
            filterPanel.BorderStyle = BorderStyle.FixedSingle;
            
            int x = tab.Width - 200;
            int y = 15;
            
            var fromLabel = new Label { Text = "من تاريخ:", Location = new Point(x, y), AutoSize = true };
            _invoicesFromDatePicker = new DateTimePicker { Location = new Point(x - 180, y - 3), Width = 160 };
            _invoicesFromDatePicker.Value = DateTime.Today.AddMonths(-1);
            
            x -= 260;
            var toLabel = new Label { Text = "إلى تاريخ:", Location = new Point(x, y), AutoSize = true };
            _invoicesToDatePicker = new DateTimePicker { Location = new Point(x - 180, y - 3), Width = 160 };
            
            x -= 260;
            var statusLabel = new Label { Text = "الحالة:", Location = new Point(x, y), AutoSize = true };
            _invoiceStatusComboBox = new ComboBox 
            { 
                Location = new Point(x - 180, y - 3), 
                Width = 160,
                DropDownStyle = ComboBoxStyle.DropDownList
            };
            _invoiceStatusComboBox.Items.AddRange(new object[] { "الكل", "مسودة", "مقدمة", "معتمدة", "مرفوضة" });
            _invoiceStatusComboBox.SelectedIndex = 0;
            
            x -= 260;
            _generateInvoicesButton = new Button
            {
                Text = "إنشاء التقرير",
                Location = new Point(x - 120, y - 5),
                Width = 120,
                Height = 35,
                BackColor = Color.FromArgb(46, 92, 138),
                ForeColor = Color.White,
                Font = new Font("Cairo", 10F, FontStyle.Bold),
                FlatStyle = FlatStyle.Flat
            };
            _generateInvoicesButton.Click += async (s, e) => await GenerateInvoicesReport();
            
            x -= 140;
            _exportInvoicesButton = new Button
            {
                Text = "تصدير Excel",
                Location = new Point(x - 120, y - 5),
                Width = 120,
                Height = 35,
                BackColor = Color.FromArgb(76, 175, 80),
                ForeColor = Color.White,
                Font = new Font("Cairo", 9F, FontStyle.Bold),
                FlatStyle = FlatStyle.Flat
            };
            _exportInvoicesButton.Click += ExportInvoicesToExcel;
            
            filterPanel.Controls.AddRange(new Control[] 
            { 
                fromLabel, _invoicesFromDatePicker, toLabel, _invoicesToDatePicker,
                statusLabel, _invoiceStatusComboBox, _generateInvoicesButton, _exportInvoicesButton
            });
            
            // Grid
            _invoicesGrid = new DataGridView
            {
                Dock = DockStyle.Fill,
                Font = new Font("Cairo", 9F),
                BackgroundColor = Color.White,
                ReadOnly = true,
                AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill,
                AllowUserToAddRows = false,
                SelectionMode = DataGridViewSelectionMode.FullRowSelect,
                RowHeadersVisible = false
            };
            
            // Summary panel
            var summaryPanel = new Panel { Dock = DockStyle.Bottom, Height = 120, BackColor = Color.FromArgb(250, 250, 250) };
            summaryPanel.BorderStyle = BorderStyle.FixedSingle;
            
            _invoicesSummaryTextBox = new TextBox
            {
                Dock = DockStyle.Fill,
                Multiline = true,
                ReadOnly = true,
                Font = new Font("Cairo", 10F, FontStyle.Bold),
                BackColor = Color.FromArgb(250, 250, 250),
                BorderStyle = BorderStyle.None,
                TextAlign = HorizontalAlignment.Right,
                Padding = new Padding(10)
            };
            summaryPanel.Controls.Add(_invoicesSummaryTextBox);
            
            mainPanel.Controls.Add(_invoicesGrid);
            mainPanel.Controls.Add(summaryPanel);
            mainPanel.Controls.Add(filterPanel);
            tab.Controls.Add(mainPanel);
        }

        private async Task GenerateInvoicesReport()
        {
            try
            {
                _generateInvoicesButton.Enabled = false;
                Cursor.Current = Cursors.WaitCursor;
                
                var fromDate = _invoicesFromDatePicker.Value.Date;
                var toDate = _invoicesToDatePicker.Value.Date.AddDays(1).AddSeconds(-1);
                
                var query = _context.TaxInvoices
                    .Include(i => i.Customer)
                    .Include(i => i.Items)
                    .Where(i => i.IssueDate >= fromDate && i.IssueDate <= toDate);
                
                // Filter by status if not "الكل"
                var selectedStatus = _invoiceStatusComboBox.SelectedItem?.ToString();
                if (selectedStatus != "الكل")
                {
                    bool isSubmitted = selectedStatus == "مقدمة" || selectedStatus == "معتمدة";
                    query = query.Where(i => i.IsSubmittedToZATCA == isSubmitted);
                }
                
                var invoices = await query.OrderByDescending(i => i.IssueDate).ToListAsync();
                
                // Prepare data for grid
                var gridData = invoices.Select(i => new
                {
                    رقم_الفاتورة = i.InvoiceNumber,
                    التاريخ = i.IssueDate.ToString("yyyy-MM-dd"),
                    العميل = i.BuyerName,
                    المجموع_الفرعي = i.SubTotal.ToString("N2"),
                    الضريبة = i.VATAmount.ToString("N2"),
                    الإجمالي = i.TotalWithVAT.ToString("N2"),
                    مقدمة_لـZATCA = i.IsSubmittedToZATCA ? "نعم" : "لا",
                    الحالة = i.ZATCAResponseCode ?? "مسودة"
                }).ToList();
                
                _invoicesGrid.DataSource = gridData;
                
                // Calculate summary
                int totalCount = invoices.Count;
                decimal totalSubTotal = invoices.Sum(i => i.SubTotal);
                decimal totalVAT = invoices.Sum(i => i.VATAmount);
                decimal grandTotal = invoices.Sum(i => i.TotalWithVAT);
                int submittedCount = invoices.Count(i => i.IsSubmittedToZATCA);
                
                _invoicesSummaryTextBox.Text = $@"📊 ملخص الفواتير الضريبية:

🔢 إجمالي الفواتير: {totalCount} فاتورة
💰 إجمالي المبالغ قبل الضريبة: {totalSubTotal:N2} ريال
📈 إجمالي ضريبة القيمة المضافة: {totalVAT:N2} ريال
💵 الإجمالي شامل الضريبة: {grandTotal:N2} ريال
✅ الفواتير المقدمة لـ ZATCA: {submittedCount} فاتورة ({(totalCount > 0 ? (submittedCount * 100.0 / totalCount).ToString("N1") : "0")}%)";
                
                LoggingService.LogInfo($"تم إنشاء تقرير الفواتير الضريبية: {totalCount} فاتورة");
            }
            catch (Exception ex)
            {
                MessageBox.Show($"حدث خطأ أثناء إنشاء التقرير:\n{ex.Message}", "خطأ", 
                    MessageBoxButtons.OK, MessageBoxIcon.Error);
                LoggingService.LogError($"خطأ في تقرير الفواتير: {ex.Message}");
            }
            finally
            {
                _generateInvoicesButton.Enabled = true;
                Cursor.Current = Cursors.Default;
            }
        }

        private void ExportInvoicesToExcel(object? sender, EventArgs e)
        {
            try
            {
                if (_invoicesGrid.Rows.Count == 0)
                {
                    MessageBox.Show("لا توجد بيانات للتصدير", "تنبيه", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    return;
                }
                
                using var workbook = new XLWorkbook();
                var worksheet = workbook.Worksheets.Add("الفواتير الضريبية");
                
                // Headers
                for (int i = 0; i < _invoicesGrid.Columns.Count; i++)
                {
                    worksheet.Cell(1, i + 1).Value = _invoicesGrid.Columns[i].HeaderText;
                    worksheet.Cell(1, i + 1).Style.Font.Bold = true;
                    worksheet.Cell(1, i + 1).Style.Fill.BackgroundColor = XLColor.LightBlue;
                }
                
                // Data
                for (int i = 0; i < _invoicesGrid.Rows.Count; i++)
                {
                    for (int j = 0; j < _invoicesGrid.Columns.Count; j++)
                    {
                        var value = _invoicesGrid.Rows[i].Cells[j].Value?.ToString() ?? "";
                        worksheet.Cell(i + 2, j + 1).Value = value;
                    }
                }
                
                worksheet.Columns().AdjustToContents();
                
                var saveDialog = new SaveFileDialog
                {
                    Filter = "Excel Files|*.xlsx",
                    FileName = $"VAT_Invoices_{DateTime.Now:yyyyMMdd}.xlsx"
                };
                
                if (saveDialog.ShowDialog() == DialogResult.OK)
                {
                    workbook.SaveAs(saveDialog.FileName);
                    MessageBox.Show("تم التصدير بنجاح!", "نجح", MessageBoxButtons.OK, MessageBoxIcon.Information);
                    LoggingService.LogInfo($"تم تصدير تقرير الفواتير إلى: {saveDialog.FileName}");
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"حدث خطأ أثناء التصدير:\n{ex.Message}", "خطأ",
                    MessageBoxButtons.OK, MessageBoxIcon.Error);
                LoggingService.LogError($"خطأ في تصدير الفواتير: {ex.Message}");
            }
        }

        #endregion

        #region Tab 2: Output VAT Report (Sales Tax)

        private void SetupOutputVATTab(TabPage tab)
        {
            var mainPanel = new Panel { Dock = DockStyle.Fill, Padding = new Padding(10), BackColor = Color.White };
            
            // Filter panel
            var filterPanel = new Panel { Dock = DockStyle.Top, Height = 60, BackColor = Color.FromArgb(250, 250, 250) };
            filterPanel.BorderStyle = BorderStyle.FixedSingle;
            
            int x = tab.Width - 200;
            int y = 15;
            
            var fromLabel = new Label { Text = "من تاريخ:", Location = new Point(x, y), AutoSize = true };
            _outputFromDatePicker = new DateTimePicker { Location = new Point(x - 180, y - 3), Width = 160 };
            _outputFromDatePicker.Value = DateTime.Today.AddMonths(-1);
            
            x -= 260;
            var toLabel = new Label { Text = "إلى تاريخ:", Location = new Point(x, y), AutoSize = true };
            _outputToDatePicker = new DateTimePicker { Location = new Point(x - 180, y - 3), Width = 160 };
            
            x -= 260;
            _generateOutputButton = new Button
            {
                Text = "إنشاء التقرير",
                Location = new Point(x - 120, y - 5),
                Width = 120,
                Height = 35,
                BackColor = Color.FromArgb(46, 92, 138),
                ForeColor = Color.White,
                Font = new Font("Cairo", 10F, FontStyle.Bold),
                FlatStyle = FlatStyle.Flat
            };
            _generateOutputButton.Click += async (s, e) => await GenerateOutputVATReport();
            
            filterPanel.Controls.AddRange(new Control[] 
            { 
                fromLabel, _outputFromDatePicker, toLabel, _outputToDatePicker, _generateOutputButton
            });
            
            // Split container
            var splitContainer = new SplitContainer
            {
                Dock = DockStyle.Fill,
                Orientation = Orientation.Vertical,
                SplitterDistance = 550
            };
            
            // Grid
            _outputVATGrid = new DataGridView
            {
                Dock = DockStyle.Fill,
                Font = new Font("Cairo", 9F),
                BackgroundColor = Color.White,
                ReadOnly = true,
                AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill,
                AllowUserToAddRows = false,
                SelectionMode = DataGridViewSelectionMode.FullRowSelect,
                RowHeadersVisible = false
            };
            
            // Chart panel
            _outputChartPanel = new Panel
            {
                Dock = DockStyle.Fill,
                BackColor = Color.White,
                Padding = new Padding(10)
            };
            
            splitContainer.Panel1.Controls.Add(_outputVATGrid);
            splitContainer.Panel2.Controls.Add(_outputChartPanel);
            
            // Summary panel
            var summaryPanel = new Panel { Dock = DockStyle.Bottom, Height = 100, BackColor = Color.FromArgb(250, 250, 250) };
            summaryPanel.BorderStyle = BorderStyle.FixedSingle;
            
            _outputSummaryTextBox = new TextBox
            {
                Dock = DockStyle.Fill,
                Multiline = true,
                ReadOnly = true,
                Font = new Font("Cairo", 10F, FontStyle.Bold),
                BackColor = Color.FromArgb(250, 250, 250),
                BorderStyle = BorderStyle.None,
                TextAlign = HorizontalAlignment.Right,
                Padding = new Padding(10)
            };
            summaryPanel.Controls.Add(_outputSummaryTextBox);
            
            mainPanel.Controls.Add(splitContainer);
            mainPanel.Controls.Add(summaryPanel);
            mainPanel.Controls.Add(filterPanel);
            tab.Controls.Add(mainPanel);
        }

        private async Task GenerateOutputVATReport()
        {
            try
            {
                _generateOutputButton.Enabled = false;
                Cursor.Current = Cursors.WaitCursor;
                
                var fromDate = _outputFromDatePicker.Value.Date;
                var toDate = _outputToDatePicker.Value.Date.AddDays(1).AddSeconds(-1);
                
                // Get sales data
                var salesOrders = await _context.SalesOrders
                    .Where(s => s.OrderDate >= fromDate && s.OrderDate <= toDate)
                    .ToListAsync();
                
                // Group by month
                var monthlyData = salesOrders
                    .GroupBy(s => new { s.OrderDate.Year, s.OrderDate.Month })
                    .Select(g => new
                    {
                        الشهر = $"{g.Key.Year}-{g.Key.Month:D2}",
                        عدد_الطلبات = g.Count(),
                        المبيعات_الخاضعة = g.Sum(s => s.SubTotal),
                        ضريبة_المخرجات = g.Sum(s => s.VATAmount),
                        الإجمالي = g.Sum(s => s.GrandTotal)
                    })
                    .OrderBy(x => x.الشهر)
                    .ToList();
                
                _outputVATGrid.DataSource = monthlyData;
                
                // Calculate totals
                decimal totalSales = salesOrders.Sum(s => s.SubTotal);
                decimal totalVAT = salesOrders.Sum(s => s.VATAmount);
                decimal grandTotal = salesOrders.Sum(s => s.GrandTotal);
                
                _outputSummaryTextBox.Text = $@"📊 ملخص ضريبة المبيعات (Output VAT):

💰 إجمالي المبيعات الخاضعة: {totalSales:N2} ريال
📈 إجمالي ضريبة المخرجات: {totalVAT:N2} ريال
💵 الإجمالي شامل الضريبة: {grandTotal:N2} ريال
📊 نسبة الضريبة الفعلية: {(totalSales > 0 ? (totalVAT / totalSales * 100).ToString("N2") : "0")}%";
                
                // Create chart
                CreateOutputVATChart(monthlyData);
                
                LoggingService.LogInfo($"تم إنشاء تقرير ضريبة المبيعات: {totalVAT:N2} ريال");
            }
            catch (Exception ex)
            {
                MessageBox.Show($"حدث خطأ أثناء إنشاء التقرير:\n{ex.Message}", "خطأ",
                    MessageBoxButtons.OK, MessageBoxIcon.Error);
                LoggingService.LogError($"خطأ في تقرير ضريبة المبيعات: {ex.Message}");
            }
            finally
            {
                _generateOutputButton.Enabled = true;
                Cursor.Current = Cursors.Default;
            }
        }

        private void CreateOutputVATChart(dynamic data)
        {
            _outputChartPanel.Controls.Clear();
            
            var summaryText = new TextBox
            {
                Dock = DockStyle.Fill,
                Multiline = true,
                ReadOnly = true,
                Font = new Font("Cairo", 10F),
                BackColor = Color.White,
                BorderStyle = BorderStyle.None,
                ScrollBars = ScrollBars.Vertical
            };
            
            var text = "📊 الاتجاه الشهري لضريبة المبيعات:\n\n";
            foreach (var item in data)
            {
                text += $"📅 {item.الشهر}:\n";
                text += $"  💰 المبيعات: {item.المبيعات_الخاضعة:N2} ريال\n";
                text += $"  📈 الضريبة: {item.ضريبة_المخرجات:N2} ريال\n";
                text += $"  📊 النسبة: {(item.المبيعات_الخاضعة > 0 ? (item.ضريبة_المخرجات / item.المبيعات_الخاضعة * 100).ToString("N2") : "0")}%\n\n";
            }
            
            summaryText.Text = text;
            _outputChartPanel.Controls.Add(summaryText);
        }

        #endregion

        #region Tab 3: Input VAT Report (Purchase Tax)

        private void SetupInputVATTab(TabPage tab)
        {
            var mainPanel = new Panel { Dock = DockStyle.Fill, Padding = new Padding(10), BackColor = Color.White };
            
            // Filter panel
            var filterPanel = new Panel { Dock = DockStyle.Top, Height = 60, BackColor = Color.FromArgb(250, 250, 250) };
            filterPanel.BorderStyle = BorderStyle.FixedSingle;
            
            int x = tab.Width - 200;
            int y = 15;
            
            var fromLabel = new Label { Text = "من تاريخ:", Location = new Point(x, y), AutoSize = true };
            _inputFromDatePicker = new DateTimePicker { Location = new Point(x - 180, y - 3), Width = 160 };
            _inputFromDatePicker.Value = DateTime.Today.AddMonths(-1);
            
            x -= 260;
            var toLabel = new Label { Text = "إلى تاريخ:", Location = new Point(x, y), AutoSize = true };
            _inputToDatePicker = new DateTimePicker { Location = new Point(x - 180, y - 3), Width = 160 };
            
            x -= 260;
            _generateInputButton = new Button
            {
                Text = "إنشاء التقرير",
                Location = new Point(x - 120, y - 5),
                Width = 120,
                Height = 35,
                BackColor = Color.FromArgb(46, 92, 138),
                ForeColor = Color.White,
                Font = new Font("Cairo", 10F, FontStyle.Bold),
                FlatStyle = FlatStyle.Flat
            };
            _generateInputButton.Click += async (s, e) => await GenerateInputVATReport();
            
            filterPanel.Controls.AddRange(new Control[] 
            { 
                fromLabel, _inputFromDatePicker, toLabel, _inputToDatePicker, _generateInputButton
            });
            
            // Split container
            var splitContainer = new SplitContainer
            {
                Dock = DockStyle.Fill,
                Orientation = Orientation.Vertical,
                SplitterDistance = 550
            };
            
            // Grid
            _inputVATGrid = new DataGridView
            {
                Dock = DockStyle.Fill,
                Font = new Font("Cairo", 9F),
                BackgroundColor = Color.White,
                ReadOnly = true,
                AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill,
                AllowUserToAddRows = false,
                SelectionMode = DataGridViewSelectionMode.FullRowSelect,
                RowHeadersVisible = false
            };
            
            // Chart panel
            _inputChartPanel = new Panel
            {
                Dock = DockStyle.Fill,
                BackColor = Color.White,
                Padding = new Padding(10)
            };
            
            splitContainer.Panel1.Controls.Add(_inputVATGrid);
            splitContainer.Panel2.Controls.Add(_inputChartPanel);
            
            // Summary panel
            var summaryPanel = new Panel { Dock = DockStyle.Bottom, Height = 100, BackColor = Color.FromArgb(250, 250, 250) };
            summaryPanel.BorderStyle = BorderStyle.FixedSingle;
            
            _inputSummaryTextBox = new TextBox
            {
                Dock = DockStyle.Fill,
                Multiline = true,
                ReadOnly = true,
                Font = new Font("Cairo", 10F, FontStyle.Bold),
                BackColor = Color.FromArgb(250, 250, 250),
                BorderStyle = BorderStyle.None,
                TextAlign = HorizontalAlignment.Right,
                Padding = new Padding(10)
            };
            summaryPanel.Controls.Add(_inputSummaryTextBox);
            
            mainPanel.Controls.Add(splitContainer);
            mainPanel.Controls.Add(summaryPanel);
            mainPanel.Controls.Add(filterPanel);
            tab.Controls.Add(mainPanel);
        }

        private async Task GenerateInputVATReport()
        {
            try
            {
                _generateInputButton.Enabled = false;
                Cursor.Current = Cursors.WaitCursor;
                
                var fromDate = _inputFromDatePicker.Value.Date;
                var toDate = _inputToDatePicker.Value.Date.AddDays(1).AddSeconds(-1);
                
                // Get purchase data
                var purchases = await _context.PurchaseOrders
                    .Where(p => p.OrderDate >= fromDate && p.OrderDate <= toDate)
                    .ToListAsync();
                
                // Group by month
                var monthlyData = purchases
                    .GroupBy(p => new { p.OrderDate.Year, p.OrderDate.Month })
                    .Select(g => new
                    {
                        الشهر = $"{g.Key.Year}-{g.Key.Month:D2}",
                        عدد_الطلبات = g.Count(),
                        المشتريات_الخاضعة = g.Sum(p => p.SubTotal),
                        ضريبة_المدخلات = g.Sum(p => p.VATAmount),
                        الإجمالي = g.Sum(p => p.Total)
                    })
                    .OrderBy(x => x.الشهر)
                    .ToList();
                
                _inputVATGrid.DataSource = monthlyData;
                
                // Calculate totals
                decimal totalPurchases = purchases.Sum(p => p.SubTotal);
                decimal totalVAT = purchases.Sum(p => p.VATAmount);
                decimal grandTotal = purchases.Sum(p => p.Total);
                
                _inputSummaryTextBox.Text = $@"📊 ملخص ضريبة المشتريات (Input VAT):

💰 إجمالي المشتريات الخاضعة: {totalPurchases:N2} ريال
📈 إجمالي ضريبة المدخلات (القابلة للخصم): {totalVAT:N2} ريال
💵 الإجمالي شامل الضريبة: {grandTotal:N2} ريال
📊 نسبة الضريبة الفعلية: {(totalPurchases > 0 ? (totalVAT / totalPurchases * 100).ToString("N2") : "0")}%";
                
                // Create chart
                CreateInputVATChart(monthlyData);
                
                LoggingService.LogInfo($"تم إنشاء تقرير ضريبة المشتريات: {totalVAT:N2} ريال");
            }
            catch (Exception ex)
            {
                MessageBox.Show($"حدث خطأ أثناء إنشاء التقرير:\n{ex.Message}", "خطأ",
                    MessageBoxButtons.OK, MessageBoxIcon.Error);
                LoggingService.LogError($"خطأ في تقرير ضريبة المشتريات: {ex.Message}");
            }
            finally
            {
                _generateInputButton.Enabled = true;
                Cursor.Current = Cursors.Default;
            }
        }

        private void CreateInputVATChart(dynamic data)
        {
            _inputChartPanel.Controls.Clear();
            
            var summaryText = new TextBox
            {
                Dock = DockStyle.Fill,
                Multiline = true,
                ReadOnly = true,
                Font = new Font("Cairo", 10F),
                BackColor = Color.White,
                BorderStyle = BorderStyle.None,
                ScrollBars = ScrollBars.Vertical
            };
            
            var text = "📊 الاتجاه الشهري لضريبة المشتريات:\n\n";
            foreach (var item in data)
            {
                text += $"📅 {item.الشهر}:\n";
                text += $"  💰 المشتريات: {item.المشتريات_الخاضعة:N2} ريال\n";
                text += $"  📈 الضريبة: {item.ضريبة_المدخلات:N2} ريال\n";
                text += $"  📊 النسبة: {(item.المشتريات_الخاضعة > 0 ? (item.ضريبة_المدخلات / item.المشتريات_الخاضعة * 100).ToString("N2") : "0")}%\n\n";
            }
            
            summaryText.Text = text;
            _inputChartPanel.Controls.Add(summaryText);
        }

        #endregion

        #region Tab 4: VAT Returns Report

        private void SetupReturnsTab(TabPage tab)
        {
            var mainPanel = new Panel { Dock = DockStyle.Fill, Padding = new Padding(10), BackColor = Color.White };
            
            // Filter panel
            var filterPanel = new Panel { Dock = DockStyle.Top, Height = 60, BackColor = Color.FromArgb(250, 250, 250) };
            filterPanel.BorderStyle = BorderStyle.FixedSingle;
            
            int x = tab.Width - 200;
            int y = 15;
            
            var periodLabel = new Label { Text = "الفترة:", Location = new Point(x, y), AutoSize = true };
            _returnPeriodComboBox = new ComboBox
            {
                Location = new Point(x - 180, y - 3),
                Width = 160,
                DropDownStyle = ComboBoxStyle.DropDownList
            };
            _returnPeriodComboBox.Items.AddRange(new object[] 
            { 
                "الكل", "الربع الأول", "الربع الثاني", "الربع الثالث", "الربع الرابع" 
            });
            _returnPeriodComboBox.SelectedIndex = 0;
            
            x -= 260;
            _generateReturnsButton = new Button
            {
                Text = "إنشاء التقرير",
                Location = new Point(x - 120, y - 5),
                Width = 120,
                Height = 35,
                BackColor = Color.FromArgb(46, 92, 138),
                ForeColor = Color.White,
                Font = new Font("Cairo", 10F, FontStyle.Bold),
                FlatStyle = FlatStyle.Flat
            };
            _generateReturnsButton.Click += async (s, e) => await GenerateReturnsReport();
            
            filterPanel.Controls.AddRange(new Control[] 
            { 
                periodLabel, _returnPeriodComboBox, _generateReturnsButton
            });
            
            // Grid
            _returnsGrid = new DataGridView
            {
                Dock = DockStyle.Fill,
                Font = new Font("Cairo", 9F),
                BackgroundColor = Color.White,
                ReadOnly = true,
                AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill,
                AllowUserToAddRows = false,
                SelectionMode = DataGridViewSelectionMode.FullRowSelect,
                RowHeadersVisible = false
            };
            
            // Summary panel
            var summaryPanel = new Panel { Dock = DockStyle.Bottom, Height = 150, BackColor = Color.FromArgb(250, 250, 250) };
            summaryPanel.BorderStyle = BorderStyle.FixedSingle;
            
            _returnsSummaryTextBox = new TextBox
            {
                Dock = DockStyle.Fill,
                Multiline = true,
                ReadOnly = true,
                Font = new Font("Cairo", 10F, FontStyle.Bold),
                BackColor = Color.FromArgb(250, 250, 250),
                BorderStyle = BorderStyle.None,
                TextAlign = HorizontalAlignment.Right,
                Padding = new Padding(10)
            };
            summaryPanel.Controls.Add(_returnsSummaryTextBox);
            
            mainPanel.Controls.Add(_returnsGrid);
            mainPanel.Controls.Add(summaryPanel);
            mainPanel.Controls.Add(filterPanel);
            tab.Controls.Add(mainPanel);
        }

        private async Task GenerateReturnsReport()
        {
            try
            {
                _generateReturnsButton.Enabled = false;
                Cursor.Current = Cursors.WaitCursor;
                
                var query = _context.VATReturns.AsQueryable();
                
                var returns = await query.OrderByDescending(r => r.PeriodStartDate).ToListAsync();
                
                // Prepare data for grid
                var gridData = returns.Select(r => new
                {
                    رقم_الفترة = r.PeriodNumber,
                    من = r.PeriodStartDate.ToString("yyyy-MM-dd"),
                    إلى = r.PeriodEndDate.ToString("yyyy-MM-dd"),
                    المبيعات_الخاضعة = r.Box1_TaxableSalesInKSA.ToString("N2"),
                    ضريبة_المخرجات = r.Box6_VATOnSales.ToString("N2"),
                    ضريبة_المدخلات = r.Box10_VATOnPurchases.ToString("N2"),
                    صافي_الضريبة = r.Box15_NetVATDueForPeriod.ToString("N2"),
                    الحالة = r.Status.ToString(),
                    تاريخ_التقديم = r.SubmissionDate?.ToString("yyyy-MM-dd") ?? "لم يقدم"
                }).ToList();
                
                _returnsGrid.DataSource = gridData;
                
                // Calculate summary
                int totalReturns = returns.Count;
                decimal totalSales = returns.Sum(r => r.Box1_TaxableSalesInKSA);
                decimal totalOutputVAT = returns.Sum(r => r.Box6_VATOnSales);
                decimal totalInputVAT = returns.Sum(r => r.Box10_VATOnPurchases);
                decimal totalNetVAT = returns.Sum(r => r.Box15_NetVATDueForPeriod);
                int submitted = returns.Count(r => r.Status == VATReturnStatus.Submitted || r.Status == VATReturnStatus.Paid);
                
                _returnsSummaryTextBox.Text = $@"📊 ملخص الإقرارات الضريبية:

🔢 إجمالي الإقرارات: {totalReturns} إقرار
💰 إجمالي المبيعات الخاضعة: {totalSales:N2} ريال
📈 إجمالي ضريبة المخرجات: {totalOutputVAT:N2} ريال
📉 إجمالي ضريبة المدخلات: {totalInputVAT:N2} ريال
💵 صافي الضريبة المستحقة: {totalNetVAT:N2} ريال
✅ الإقرارات المقدمة: {submitted} إقرار ({(totalReturns > 0 ? (submitted * 100.0 / totalReturns).ToString("N1") : "0")}%)

{(totalNetVAT > 0 ? "📌 مستحق للسداد" : totalNetVAT < 0 ? "📌 قابل للاسترداد" : "📌 متوازن")}";
                
                LoggingService.LogInfo($"تم إنشاء تقرير الإقرارات الضريبية: {totalReturns} إقرار");
            }
            catch (Exception ex)
            {
                MessageBox.Show($"حدث خطأ أثناء إنشاء التقرير:\n{ex.Message}", "خطأ",
                    MessageBoxButtons.OK, MessageBoxIcon.Error);
                LoggingService.LogError($"خطأ في تقرير الإقرارات: {ex.Message}");
            }
            finally
            {
                _generateReturnsButton.Enabled = true;
                Cursor.Current = Cursors.Default;
            }
        }

        #endregion

        #region Tab 5: VAT Dashboard

        private void SetupDashboardTab(TabPage tab)
        {
            var mainPanel = new Panel { Dock = DockStyle.Fill, Padding = new Padding(10), BackColor = Color.White };
            
            // Header panel
            var headerPanel = new Panel { Dock = DockStyle.Top, Height = 60, BackColor = Color.FromArgb(250, 250, 250) };
            headerPanel.BorderStyle = BorderStyle.FixedSingle;
            
            var titleLabel = new Label
            {
                Text = "لوحة تحكم ضريبة القيمة المضافة - VAT Dashboard",
                Location = new Point(tab.Width / 2 - 200, 15),
                Size = new Size(400, 30),
                Font = new Font("Cairo", 12F, FontStyle.Bold),
                ForeColor = Color.FromArgb(46, 92, 138),
                TextAlign = ContentAlignment.MiddleCenter
            };
            
            _refreshDashboardButton = new Button
            {
                Text = "تحديث",
                Location = new Point(tab.Width - 150, 12),
                Width = 120,
                Height = 35,
                BackColor = Color.FromArgb(46, 92, 138),
                ForeColor = Color.White,
                Font = new Font("Cairo", 10F, FontStyle.Bold),
                FlatStyle = FlatStyle.Flat
            };
            _refreshDashboardButton.Click += async (s, e) => await LoadDashboardData();
            
            headerPanel.Controls.AddRange(new Control[] { titleLabel, _refreshDashboardButton });
            
            // KPI Panel
            _kpiPanel = new Panel
            {
                Dock = DockStyle.Top,
                Height = 150,
                BackColor = Color.White,
                Padding = new Padding(10)
            };
            
            // Charts container
            var chartsContainer = new Panel
            {
                Dock = DockStyle.Fill,
                BackColor = Color.White,
                Padding = new Padding(10)
            };
            
            var chartsSplitContainer = new SplitContainer
            {
                Dock = DockStyle.Fill,
                Orientation = Orientation.Horizontal,
                SplitterDistance = 300
            };
            
            _monthlyTrendPanel = new Panel
            {
                Dock = DockStyle.Fill,
                BackColor = Color.White,
                Padding = new Padding(10)
            };
            
            _vatBreakdownPanel = new Panel
            {
                Dock = DockStyle.Fill,
                BackColor = Color.White,
                Padding = new Padding(10)
            };
            
            chartsSplitContainer.Panel1.Controls.Add(_monthlyTrendPanel);
            chartsSplitContainer.Panel2.Controls.Add(_vatBreakdownPanel);
            
            chartsContainer.Controls.Add(chartsSplitContainer);
            
            // Summary panel
            var summaryPanel = new Panel { Dock = DockStyle.Bottom, Height = 120, BackColor = Color.FromArgb(250, 250, 250) };
            summaryPanel.BorderStyle = BorderStyle.FixedSingle;
            
            _dashboardSummaryTextBox = new TextBox
            {
                Dock = DockStyle.Fill,
                Multiline = true,
                ReadOnly = true,
                Font = new Font("Cairo", 10F, FontStyle.Bold),
                BackColor = Color.FromArgb(250, 250, 250),
                BorderStyle = BorderStyle.None,
                TextAlign = HorizontalAlignment.Right,
                Padding = new Padding(10)
            };
            summaryPanel.Controls.Add(_dashboardSummaryTextBox);
            
            mainPanel.Controls.Add(chartsContainer);
            mainPanel.Controls.Add(_kpiPanel);
            mainPanel.Controls.Add(summaryPanel);
            mainPanel.Controls.Add(headerPanel);
            
            tab.Controls.Add(mainPanel);
        }

        private async Task LoadDashboardData()
        {
            try
            {
                _refreshDashboardButton.Enabled = false;
                Cursor.Current = Cursors.WaitCursor;
                
                var currentMonth = DateTime.Today.AddMonths(-6);
                
                // Get sales and purchases for last 6 months
                var sales = await _context.SalesOrders
                    .Where(s => s.OrderDate >= currentMonth)
                    .ToListAsync();
                
                var purchases = await _context.PurchaseOrders
                    .Where(p => p.OrderDate >= currentMonth)
                    .ToListAsync();
                
                var invoices = await _context.TaxInvoices
                    .Where(i => i.IssueDate >= currentMonth)
                    .ToListAsync();
                
                // Calculate KPIs
                decimal totalOutputVAT = sales.Sum(s => s.VATAmount);
                decimal totalInputVAT = purchases.Sum(p => p.VATAmount);
                decimal netVAT = totalOutputVAT - totalInputVAT;
                int totalInvoices = invoices.Count;
                int submittedInvoices = invoices.Count(i => i.IsSubmittedToZATCA);
                
                // Create KPI cards
                CreateKPICards(totalOutputVAT, totalInputVAT, netVAT, totalInvoices, submittedInvoices);
                
                // Create monthly trend chart
                CreateMonthlyTrendChart(sales, purchases);
                
                // Create VAT breakdown chart
                CreateVATBreakdownChart(totalOutputVAT, totalInputVAT);
                
                // Update summary
                _dashboardSummaryTextBox.Text = $@"📊 ملخص لوحة التحكم (آخر 6 أشهر):

💰 ضريبة المخرجات: {totalOutputVAT:N2} ريال
📉 ضريبة المدخلات: {totalInputVAT:N2} ريال
💵 صافي الضريبة: {netVAT:N2} ريال ({(netVAT > 0 ? "مستحق للسداد" : "قابل للاسترداد")})
📄 إجمالي الفواتير: {totalInvoices} | المقدمة: {submittedInvoices} ({(totalInvoices > 0 ? (submittedInvoices * 100.0 / totalInvoices).ToString("N1") : "0")}%)";
                
                LoggingService.LogInfo("تم تحديث لوحة تحكم الضريبة");
            }
            catch (Exception ex)
            {
                MessageBox.Show($"حدث خطأ أثناء تحديث اللوحة:\n{ex.Message}", "خطأ",
                    MessageBoxButtons.OK, MessageBoxIcon.Error);
                LoggingService.LogError($"خطأ في لوحة التحكم: {ex.Message}");
            }
            finally
            {
                _refreshDashboardButton.Enabled = true;
                Cursor.Current = Cursors.Default;
            }
        }

        private void CreateKPICards(decimal outputVAT, decimal inputVAT, decimal netVAT, int total, int submitted)
        {
            _kpiPanel.Controls.Clear();
            
            int cardWidth = (_kpiPanel.Width - 80) / 5;
            int x = 20;
            
            AddKPICard(_kpiPanel, "ضريبة المخرجات", $"{outputVAT:N2} ريال", Color.FromArgb(76, 175, 80), x, 10, cardWidth);
            x += cardWidth + 10;
            
            AddKPICard(_kpiPanel, "ضريبة المدخلات", $"{inputVAT:N2} ريال", Color.FromArgb(255, 152, 0), x, 10, cardWidth);
            x += cardWidth + 10;
            
            AddKPICard(_kpiPanel, "صافي الضريبة", $"{netVAT:N2} ريال", 
                netVAT > 0 ? Color.FromArgb(244, 67, 54) : Color.FromArgb(33, 150, 243), x, 10, cardWidth);
            x += cardWidth + 10;
            
            AddKPICard(_kpiPanel, "إجمالي الفواتير", total.ToString(), Color.FromArgb(46, 92, 138), x, 10, cardWidth);
            x += cardWidth + 10;
            
            AddKPICard(_kpiPanel, "الفواتير المقدمة", $"{submitted} ({(total > 0 ? (submitted * 100.0 / total).ToString("N0") : "0")}%)", 
                Color.FromArgb(156, 39, 176), x, 10, cardWidth);
        }

        private void AddKPICard(Panel parent, string title, string value, Color color, int x, int y, int width)
        {
            var card = new Panel
            {
                Location = new Point(x, y),
                Size = new Size(width, 120),
                BackColor = color,
                BorderStyle = BorderStyle.FixedSingle
            };
            
            var titleLabel = new Label
            {
                Text = title,
                Location = new Point(10, 15),
                Size = new Size(width - 20, 30),
                ForeColor = Color.White,
                Font = new Font("Cairo", 9F, FontStyle.Bold),
                TextAlign = ContentAlignment.MiddleCenter
            };
            
            var valueLabel = new Label
            {
                Text = value,
                Location = new Point(10, 50),
                Size = new Size(width - 20, 50),
                ForeColor = Color.White,
                Font = new Font("Cairo", 12F, FontStyle.Bold),
                TextAlign = ContentAlignment.MiddleCenter
            };
            
            card.Controls.AddRange(new Control[] { titleLabel, valueLabel });
            parent.Controls.Add(card);
        }

        private void CreateMonthlyTrendChart(List<SalesOrder> sales, List<PurchaseOrder> purchases)
        {
            _monthlyTrendPanel.Controls.Clear();
            
            var summaryText = new TextBox
            {
                Dock = DockStyle.Fill,
                Multiline = true,
                ReadOnly = true,
                Font = new Font("Cairo", 9F),
                BackColor = Color.White,
                BorderStyle = BorderStyle.None,
                ScrollBars = ScrollBars.Vertical
            };
            
            // Group by month
            var salesByMonth = sales.GroupBy(s => new { s.OrderDate.Year, s.OrderDate.Month })
                .Select(g => new 
                { 
                    Month = $"{g.Key.Year}-{g.Key.Month:D2}", 
                    VAT = g.Sum(s => s.VATAmount) 
                }).ToList();
            
            var purchasesByMonth = purchases.GroupBy(p => new { p.OrderDate.Year, p.OrderDate.Month })
                .Select(g => new 
                { 
                    Month = $"{g.Key.Year}-{g.Key.Month:D2}", 
                    VAT = g.Sum(p => p.VATAmount) 
                }).ToList();
            
            var text = "📊 الاتجاه الشهري لضريبة القيمة المضافة:\n\n";
            foreach (var item in salesByMonth)
            {
                var purchaseVAT = purchasesByMonth.FirstOrDefault(p => p.Month == item.Month)?.VAT ?? 0;
                var netVAT = item.VAT - purchaseVAT;
                
                text += $"📅 {item.Month}:\n";
                text += $"  ✅ ضريبة المخرجات: {item.VAT:N2} ريال\n";
                text += $"  ❌ ضريبة المدخلات: {purchaseVAT:N2} ريال\n";
                text += $"  💵 صافي الضريبة: {netVAT:N2} ريال {(netVAT > 0 ? "(مستحق)" : "(قابل للاسترداد)")}\n\n";
            }
            
            summaryText.Text = text;
            _monthlyTrendPanel.Controls.Add(summaryText);
        }

        private void CreateVATBreakdownChart(decimal outputVAT, decimal inputVAT)
        {
            _vatBreakdownPanel.Controls.Clear();
            
            var summaryText = new TextBox
            {
                Dock = DockStyle.Fill,
                Multiline = true,
                ReadOnly = true,
                Font = new Font("Cairo", 11F),
                BackColor = Color.White,
                BorderStyle = BorderStyle.None,
                TextAlign = HorizontalAlignment.Center
            };
            
            var total = outputVAT + inputVAT;
            var outputPercent = total > 0 ? (outputVAT / total * 100) : 0;
            var inputPercent = total > 0 ? (inputVAT / total * 100) : 0;
            
            var text = "📊 توزيع ضريبة القيمة المضافة\n\n\n";
            text += $"✅ ضريبة المخرجات\n";
            text += $"{outputVAT:N2} ريال\n";
            text += $"{outputPercent:N1}%\n\n\n";
            text += $"❌ ضريبة المدخلات\n";
            text += $"{inputVAT:N2} ريال\n";
            text += $"{inputPercent:N1}%\n\n\n";
            text += $"━━━━━━━━━━━━━\n\n";
            text += $"💵 صافي الضريبة\n";
            text += $"{(outputVAT - inputVAT):N2} ريال\n";
            text += (outputVAT - inputVAT) > 0 ? "مستحق للسداد" : "قابل للاسترداد";
            
            summaryText.Text = text;
            _vatBreakdownPanel.Controls.Add(summaryText);
        }

        #endregion

        #region Helper Methods

        private async Task LoadInitialDataAsync()
        {
            await Task.Run(() =>
            {
                // Load initial data if needed
                LoggingService.LogInfo("Loading VAT reports initial data");
            });
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                // Cleanup
            }
            base.Dispose(disposing);
        }

        #endregion
    }
}
