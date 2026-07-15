using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Net.Http;
using System.Security.Cryptography.X509Certificates;
using System.Threading.Tasks;
using System.Windows.Forms;
using Microsoft.EntityFrameworkCore;
using FishFarmManager.Data;
using FishFarmManager.Models;
using FishFarmManager.Services;

namespace FishFarmManager.Forms
{
    /// <summary>
    /// نموذج التكامل مع منصة ZATCA (هيئة الزكاة والضريبة والجمارك)
    /// E-Invoicing Integration Form - ZATCA API
    /// </summary>
    public partial class EInvoicingIntegrationForm : Form
    {
        private readonly FishFarmContext _context;
        private static readonly HttpClient _httpClient = new HttpClient();
        
        // Configuration Controls
        private TextBox _apiUrlTextBox = null!;
        private TextBox _apiKeyTextBox = null!;
        private TextBox _deviceIdTextBox = null!;
        private TextBox _certificatePathTextBox = null!;
        private Button _browseCertButton = null!;
        private Button _testConnectionButton = null!;
        private Button _saveConfigButton = null!;
        
        // Invoice Submission Controls
        private DataGridView _pendingInvoicesGrid = null!;
        private Button _refreshPendingButton = null!;
        private Button _submitSelectedButton = null!;
        private Button _submitAllButton = null!;
        
        // Status Controls
        private DataGridView _submittedInvoicesGrid = null!;
        private Button _refreshSubmittedButton = null!;
        private Button _checkStatusButton = null!;
        
        // Logs
        private TextBox _logsTextBox = null!;
        private Button _clearLogsButton = null!;
        
        // Certificate
        private X509Certificate2? _certificate;
        private VATConfiguration? _vatConfig;

        public EInvoicingIntegrationForm(FishFarmContext context)
        {
            _context = context ?? throw new ArgumentNullException(nameof(context));

            if (!AuthenticationService.HasPermission(UserRole.Admin, UserRole.Manager, UserRole.Accountant))
            {
                MessageBox.Show(
                    "ليس لديك صلاحية للوصول إلى نظام التكامل مع ZATCA.\nيرجى الاتصال بالمدير.",
                    "خطأ في الصلاحيات",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Warning
                );
                
                LoggingService.LogWarning($"محاولة وصول غير مصرح بها من {AuthenticationService.CurrentUsername} إلى ZATCA Integration");
                
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
            
            LoggingService.LogInfo("EInvoicingIntegrationForm initialized successfully");
        }

        private void InitializeComponent()
        {
            this.Text = "التكامل مع ZATCA - E-Invoicing Integration";
            this.Size = new Size(1400, 900);
            this.StartPosition = FormStartPosition.CenterScreen;
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
                Text = "التكامل مع منصة ZATCA - E-Invoicing Integration",
                Dock = DockStyle.Top,
                Height = 50,
                Font = new Font("Cairo", 14F, FontStyle.Bold),
                ForeColor = Color.FromArgb(46, 92, 138),
                TextAlign = ContentAlignment.MiddleCenter,
                BackColor = Color.White
            };
            mainPanel.Controls.Add(titleLabel);

            var tabControl = new TabControl
            {
                Dock = DockStyle.Fill,
                Font = new Font("Cairo", 10F)
            };

            // Create tabs
            var configTab = new TabPage("الإعدادات - Configuration");
            var submitTab = new TabPage("إرسال الفواتير - Submit Invoices");
            var statusTab = new TabPage("حالة الفواتير - Status");
            var logsTab = new TabPage("السجلات - Logs");

            tabControl.TabPages.AddRange(new TabPage[] { configTab, submitTab, statusTab, logsTab });

            // Setup tabs
            SetupConfigurationTab(configTab);
            SetupSubmitTab(submitTab);
            SetupStatusTab(statusTab);
            SetupLogsTab(logsTab);

            mainPanel.Controls.Add(tabControl);
            this.Controls.Add(mainPanel);
        }

        #region Configuration Tab

        private void SetupConfigurationTab(TabPage tab)
        {
            var mainPanel = new Panel { Dock = DockStyle.Fill, Padding = new Padding(20), BackColor = Color.White };
            
            var infoLabel = new Label
            {
                Text = "⚙️ إعدادات الاتصال بمنصة ZATCA",
                Location = new Point(20, 20),
                Size = new Size(1300, 35),
                Font = new Font("Cairo", 12F, FontStyle.Bold),
                ForeColor = Color.FromArgb(46, 92, 138)
            };
            mainPanel.Controls.Add(infoLabel);
            
            int y = 70;
            int labelWidth = 150;
            int textBoxX = 1100;
            int textBoxWidth = 400;
            
            // API URL
            var apiUrlLabel = new Label 
            { 
                Text = "API Endpoint:", 
                Location = new Point(textBoxX + textBoxWidth + 20, y), 
                AutoSize = true,
                Font = new Font("Cairo", 10F, FontStyle.Bold)
            };
            _apiUrlTextBox = new TextBox 
            { 
                Location = new Point(textBoxX, y - 3), 
                Width = textBoxWidth,
                Text = "https://sandbox.zatca.gov.sa/api/v1/invoices"
            };
            mainPanel.Controls.AddRange(new Control[] { apiUrlLabel, _apiUrlTextBox });
            
            y += 50;
            
            // API Key
            var apiKeyLabel = new Label 
            { 
                Text = "API Key:", 
                Location = new Point(textBoxX + textBoxWidth + 20, y), 
                AutoSize = true,
                Font = new Font("Cairo", 10F, FontStyle.Bold)
            };
            _apiKeyTextBox = new TextBox 
            { 
                Location = new Point(textBoxX, y - 3), 
                Width = textBoxWidth,
                UseSystemPasswordChar = true
            };
            mainPanel.Controls.AddRange(new Control[] { apiKeyLabel, _apiKeyTextBox });
            
            y += 50;
            
            // Device ID
            var deviceIdLabel = new Label 
            { 
                Text = "Device ID:", 
                Location = new Point(textBoxX + textBoxWidth + 20, y), 
                AutoSize = true,
                Font = new Font("Cairo", 10F, FontStyle.Bold)
            };
            _deviceIdTextBox = new TextBox 
            { 
                Location = new Point(textBoxX, y - 3), 
                Width = textBoxWidth
            };
            mainPanel.Controls.AddRange(new Control[] { deviceIdLabel, _deviceIdTextBox });
            
            y += 50;
            
            // Certificate Path
            var certLabel = new Label 
            { 
                Text = "Certificate Path:", 
                Location = new Point(textBoxX + textBoxWidth + 20, y), 
                AutoSize = true,
                Font = new Font("Cairo", 10F, FontStyle.Bold)
            };
            _certificatePathTextBox = new TextBox 
            { 
                Location = new Point(textBoxX, y - 3), 
                Width = 330,
                ReadOnly = true
            };
            _browseCertButton = new Button
            {
                Text = "استعراض...",
                Location = new Point(textBoxX - 80, y - 5),
                Width = 70,
                Height = 30,
                BackColor = Color.FromArgb(76, 175, 80),
                ForeColor = Color.White,
                FlatStyle = FlatStyle.Flat
            };
            _browseCertButton.Click += BrowseCertificate;
            mainPanel.Controls.AddRange(new Control[] { certLabel, _certificatePathTextBox, _browseCertButton });
            
            y += 70;
            
            // Buttons
            _testConnectionButton = new Button
            {
                Text = "اختبار الاتصال",
                Location = new Point(textBoxX + 240, y),
                Width = 160,
                Height = 40,
                BackColor = Color.FromArgb(33, 150, 243),
                ForeColor = Color.White,
                Font = new Font("Cairo", 10F, FontStyle.Bold),
                FlatStyle = FlatStyle.Flat
            };
            _testConnectionButton.Click += async (s, e) => await TestConnectionAsync();
            
            _saveConfigButton = new Button
            {
                Text = "حفظ الإعدادات",
                Location = new Point(textBoxX, y),
                Width = 160,
                Height = 40,
                BackColor = Color.FromArgb(46, 92, 138),
                ForeColor = Color.White,
                Font = new Font("Cairo", 10F, FontStyle.Bold),
                FlatStyle = FlatStyle.Flat
            };
            _saveConfigButton.Click += async (s, e) => await SaveConfigurationAsync();
            
            mainPanel.Controls.AddRange(new Control[] { _testConnectionButton, _saveConfigButton });
            
            // Info Box
            var infoBox = new GroupBox
            {
                Text = "📋 معلومات مهمة",
                Location = new Point(20, y + 60),
                Size = new Size(900, 250),
                Font = new Font("Cairo", 10F, FontStyle.Bold),
                ForeColor = Color.FromArgb(46, 92, 138)
            };
            
            var infoText = new TextBox
            {
                Multiline = true,
                ReadOnly = true,
                Dock = DockStyle.Fill,
                BorderStyle = BorderStyle.None,
                BackColor = Color.FromArgb(250, 250, 250),
                Font = new Font("Cairo", 9F),
                Text = @"🔐 متطلبات التكامل مع ZATCA:

1. شهادة رقمية (Digital Certificate) صالحة من ZATCA
2. API Key و Device ID من حساب ZATCA الخاص بك
3. تفعيل الفوترة الإلكترونية لدى الهيئة
4. التأكد من صحة الرقم الضريبي (TRN)

⚠️ ملاحظات:
• استخدم Sandbox API للاختبار قبل الإنتاج
• احفظ الشهادة الرقمية في مكان آمن
• لا تشارك API Key مع أي شخص
• راجع وثائق ZATCA للمزيد من التفاصيل:
  https://zatca.gov.sa/ar/E-Invoicing/

📞 الدعم الفني:
• هاتف: 19993
• البريد: support@zatca.gov.sa"
            };
            infoBox.Controls.Add(infoText);
            mainPanel.Controls.Add(infoBox);
            
            tab.Controls.Add(mainPanel);
        }

        private void BrowseCertificate(object? sender, EventArgs e)
        {
            try
            {
                var openDialog = new OpenFileDialog
                {
                    Filter = "Certificate Files|*.pfx;*.p12;*.cer|All Files|*.*",
                    Title = "اختر ملف الشهادة الرقمية"
                };
                
                if (openDialog.ShowDialog() == DialogResult.OK)
                {
                    _certificatePathTextBox.Text = openDialog.FileName;
                    
                    // Try to load certificate
                    try
                    {
                        var password = PromptForPassword();
                        if (!string.IsNullOrEmpty(password))
                    {
                        _certificate = new X509Certificate2(openDialog.FileName, password);
                        
                        // ✅ Check certificate expiry
                        CheckCertificateExpiry();
                        
                        AddLog($"✅ تم تحميل الشهادة بنجاح: {_certificate.Subject}");
                        AddLog($"📅 صالحة حتى: {_certificate.NotAfter:yyyy-MM-dd}");
                        
                        MessageBox.Show("تم تحميل الشهادة بنجاح!", "نجح", MessageBoxButtons.OK, MessageBoxIcon.Information);
                    }
                    }
                    catch (Exception ex)
                    {
                        AddLog($"❌ خطأ في تحميل الشهادة: {ex.Message}");
                        MessageBox.Show($"فشل تحميل الشهادة:\n{ex.Message}", "خطأ", MessageBoxButtons.OK, MessageBoxIcon.Error);
                    }
                }
            }
            catch (Exception ex)
            {
                LoggingService.LogError($"خطأ في استعراض الشهادة: {ex.Message}");
            }
        }

        private string? PromptForPassword()
        {
            using var form = new Form();
            form.Text = "كلمة مرور الشهادة";
            form.Size = new Size(400, 150);
            form.StartPosition = FormStartPosition.CenterParent;
            form.RightToLeft = RightToLeft.Yes;
            form.RightToLeftLayout = true;
            
            var label = new Label { Text = "أدخل كلمة مرور الشهادة:", Location = new Point(20, 20), AutoSize = true };
            var textBox = new TextBox { Location = new Point(20, 50), Width = 340, UseSystemPasswordChar = true };
            var okButton = new Button { Text = "موافق", Location = new Point(260, 80), DialogResult = DialogResult.OK };
            var cancelButton = new Button { Text = "إلغاء", Location = new Point(180, 80), DialogResult = DialogResult.Cancel };
            
            form.Controls.AddRange(new Control[] { label, textBox, okButton, cancelButton });
            form.AcceptButton = okButton;
            form.CancelButton = cancelButton;
            
            return form.ShowDialog() == DialogResult.OK ? textBox.Text : null;
        }

        private async Task TestConnectionAsync()
        {
            try
            {
                _testConnectionButton.Enabled = false;
                Cursor.Current = Cursors.WaitCursor;
                
                AddLog("🔄 جاري اختبار الاتصال بـ ZATCA...");
                
                var apiUrl = _apiUrlTextBox.Text.Trim();
                var apiKey = _apiKeyTextBox.Text.Trim();
                
                if (string.IsNullOrEmpty(apiUrl) || string.IsNullOrEmpty(apiKey))
                {
                    AddLog("❌ الرجاء إدخال API URL و API Key");
                    MessageBox.Show("الرجاء إدخال جميع البيانات المطلوبة", "تحذير", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    return;
                }
                
                // Ping test
                var request = new HttpRequestMessage(HttpMethod.Get, $"{apiUrl}/ping");
                request.Headers.Add("Authorization", $"Bearer {apiKey}");
                
                var response = await _httpClient.SendAsync(request);
                
                if (response.IsSuccessStatusCode)
                {
                    AddLog($"✅ الاتصال ناجح! Status: {response.StatusCode}");
                    MessageBox.Show("✅ الاتصال بـ ZATCA ناجح!", "نجح", MessageBoxButtons.OK, MessageBoxIcon.Information);
                }
                else
                {
                    AddLog($"⚠️ الاتصال فشل. Status: {response.StatusCode}");
                    MessageBox.Show($"فشل الاتصال:\nStatus Code: {response.StatusCode}", "خطأ", MessageBoxButtons.OK, MessageBoxIcon.Error);
                }
                
                LoggingService.LogInfo($"ZATCA Connection Test: {response.StatusCode}");
            }
            catch (Exception ex)
            {
                AddLog($"❌ خطأ في الاتصال: {ex.Message}");
                MessageBox.Show($"حدث خطأ:\n{ex.Message}", "خطأ", MessageBoxButtons.OK, MessageBoxIcon.Error);
                LoggingService.LogError($"ZATCA connection test failed: {ex.Message}");
            }
            finally
            {
                _testConnectionButton.Enabled = true;
                Cursor.Current = Cursors.Default;
            }
        }

        private async Task SaveConfigurationAsync()
        {
            try
            {
                _saveConfigButton.Enabled = false;
                
                var config = await _context.VATConfigurations.FirstOrDefaultAsync();
                if (config == null)
                {
                    config = new VATConfiguration();
                    _context.VATConfigurations.Add(config);
                }
                
                config.ZATCAApiEndpoint = _apiUrlTextBox.Text.Trim();
                config.ZATCAApiKey = SensitiveDataProtection.EncryptApiKey(_apiKeyTextBox.Text.Trim());
                config.DeviceId = _deviceIdTextBox.Text.Trim();
                config.EnableZATCAIntegration = true;
                config.UpdatedAt = DateTime.Now;
                
                await _context.SaveChangesAsync();
                
                AddLog("✅ تم حفظ الإعدادات بنجاح");
                MessageBox.Show("تم حفظ الإعدادات بنجاح!", "نجح", MessageBoxButtons.OK, MessageBoxIcon.Information);
                
                LoggingService.LogInfo("ZATCA configuration saved successfully");
            }
            catch (Exception ex)
            {
                AddLog($"❌ خطأ في حفظ الإعدادات: {ex.Message}");
                MessageBox.Show($"حدث خطأ أثناء الحفظ:\n{ex.Message}", "خطأ", MessageBoxButtons.OK, MessageBoxIcon.Error);
                LoggingService.LogError($"Failed to save ZATCA config: {ex.Message}");
            }
            finally
            {
                _saveConfigButton.Enabled = true;
            }
        }

        #endregion

        #region Submit Invoices Tab

        private void SetupSubmitTab(TabPage tab)
        {
            var mainPanel = new Panel { Dock = DockStyle.Fill, Padding = new Padding(10), BackColor = Color.White };
            
            // Header panel
            var headerPanel = new Panel { Dock = DockStyle.Top, Height = 60, BackColor = Color.FromArgb(250, 250, 250) };
            headerPanel.BorderStyle = BorderStyle.FixedSingle;
            
            var titleLabel = new Label
            {
                Text = "📤 إرسال الفواتير إلى ZATCA",
                Location = new Point(20, 15),
                Size = new Size(400, 30),
                Font = new Font("Cairo", 12F, FontStyle.Bold),
                ForeColor = Color.FromArgb(46, 92, 138)
            };
            
            _refreshPendingButton = new Button
            {
                Text = "تحديث",
                Location = new Point(tab.Width - 300, 12),
                Width = 100,
                Height = 35,
                BackColor = Color.FromArgb(33, 150, 243),
                ForeColor = Color.White,
                Font = new Font("Cairo", 10F, FontStyle.Bold),
                FlatStyle = FlatStyle.Flat
            };
            _refreshPendingButton.Click += async (s, e) => await LoadPendingInvoicesAsync();
            
            _submitSelectedButton = new Button
            {
                Text = "غير مفعل — G4",
                Location = new Point(tab.Width - 420, 12),
                Width = 110,
                Height = 35,
                BackColor = Color.FromArgb(76, 175, 80),
                ForeColor = Color.White,
                Font = new Font("Cairo", 10F, FontStyle.Bold),
                FlatStyle = FlatStyle.Flat,
                Enabled = false
            };
            _submitSelectedButton.Click += async (s, e) => await SubmitSelectedInvoicesAsync();
            
            _submitAllButton = new Button
            {
                Text = "غير مفعل — G4",
                Location = new Point(tab.Width - 540, 12),
                Width = 110,
                Height = 35,
                BackColor = Color.FromArgb(46, 92, 138),
                ForeColor = Color.White,
                Font = new Font("Cairo", 10F, FontStyle.Bold),
                FlatStyle = FlatStyle.Flat,
                Enabled = false
            };
            _submitAllButton.Click += async (s, e) => await SubmitAllInvoicesAsync();
            
            headerPanel.Controls.AddRange(new Control[] { titleLabel, _refreshPendingButton, _submitSelectedButton, _submitAllButton });
            
            // Grid
            _pendingInvoicesGrid = new DataGridView
            {
                Dock = DockStyle.Fill,
                Font = new Font("Cairo", 9F),
                BackgroundColor = Color.White,
                ReadOnly = false,
                AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill,
                AllowUserToAddRows = false,
                SelectionMode = DataGridViewSelectionMode.FullRowSelect,
                MultiSelect = true,
                RowHeadersVisible = false
            };
            
            mainPanel.Controls.Add(_pendingInvoicesGrid);
            mainPanel.Controls.Add(headerPanel);
            tab.Controls.Add(mainPanel);
        }

        private async Task LoadPendingInvoicesAsync()
        {
            try
            {
                _refreshPendingButton.Enabled = false;
                
                var pendingInvoices = await _context.TaxInvoices
                    .Include(i => i.Customer)
                    .Where(i => !i.IsSubmittedToZATCA)
                    .OrderByDescending(i => i.IssueDate)
                    .ToListAsync();
                
                var gridData = pendingInvoices.Select(i => new
                {
                    Id = i.Id,
                    رقم_الفاتورة = i.InvoiceNumber,
                    التاريخ = i.IssueDate.ToString("yyyy-MM-dd"),
                    العميل = i.BuyerName,
                    المجموع = i.TotalWithVAT.ToString("N2"),
                    الضريبة = i.VATAmount.ToString("N2"),
                    UUID = i.UUID ?? "غير محدد"
                }).ToList();
                
                _pendingInvoicesGrid.DataSource = gridData;
                
                AddLog($"📋 تم تحميل {pendingInvoices.Count} فاتورة معلقة");
            }
            catch (Exception ex)
            {
                AddLog($"❌ خطأ في تحميل الفواتير: {ex.Message}");
                LoggingService.LogError($"Failed to load pending invoices: {ex.Message}");
            }
            finally
            {
                _refreshPendingButton.Enabled = true;
            }
        }

        private async Task SubmitSelectedInvoicesAsync()
        {
            try
            {
                if (_pendingInvoicesGrid.SelectedRows.Count == 0)
                {
                    MessageBox.Show("الرجاء تحديد فاتورة واحدة على الأقل", "تنبيه", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    return;
                }
                
                var selectedIds = new List<int>();
                foreach (DataGridViewRow row in _pendingInvoicesGrid.SelectedRows)
                {
                    selectedIds.Add((int)row.Cells["Id"].Value);
                }
                
                await SubmitInvoicesAsync(selectedIds);
            }
            catch (Exception ex)
            {
                AddLog($"❌ خطأ: {ex.Message}");
                LoggingService.LogError($"Failed to submit selected invoices: {ex.Message}");
            }
        }

        private async Task SubmitAllInvoicesAsync()
        {
            try
            {
                var result = MessageBox.Show(
                    "هل أنت متأكد من إرسال جميع الفواتير المعلقة إلى ZATCA؟",
                    "تأكيد",
                    MessageBoxButtons.YesNo,
                    MessageBoxIcon.Question
                );
                
                if (result != DialogResult.Yes) return;
                
                var allIds = new List<int>();
                foreach (DataGridViewRow row in _pendingInvoicesGrid.Rows)
                {
                    allIds.Add((int)row.Cells["Id"].Value);
                }
                
                await SubmitInvoicesAsync(allIds);
            }
            catch (Exception ex)
            {
                AddLog($"❌ خطأ: {ex.Message}");
                LoggingService.LogError($"Failed to submit all invoices: {ex.Message}");
            }
        }

        private async Task SubmitInvoicesAsync(List<int> invoiceIds)
        {
            await Task.CompletedTask;
            const string message = "تم تعطيل الإرسال القديم لأنه كان محاكاة ولا ينفذ UBL أو CSID أو واجهات Clearance/Reporting. " +
                                   "لن تتغير حالة أي فاتورة إلى مقدمة أو مقبولة حتى اكتمال مسار G4 والتحقق الرسمي.";
            AddLog($"⛔ {message}");
            LoggingService.LogWarning($"Blocked legacy simulated ZATCA submission for {invoiceIds.Count} invoice(s).");
            MessageBox.Show(message, "G4 — الإرسال غير مفعّل", MessageBoxButtons.OK, MessageBoxIcon.Warning);
        }

        #endregion

        #region Status Tab

        private void SetupStatusTab(TabPage tab)
        {
            var mainPanel = new Panel { Dock = DockStyle.Fill, Padding = new Padding(10), BackColor = Color.White };
            
            // Header panel
            var headerPanel = new Panel { Dock = DockStyle.Top, Height = 60, BackColor = Color.FromArgb(250, 250, 250) };
            headerPanel.BorderStyle = BorderStyle.FixedSingle;
            
            var titleLabel = new Label
            {
                Text = "📊 حالة الفواتير المقدمة",
                Location = new Point(20, 15),
                Size = new Size(400, 30),
                Font = new Font("Cairo", 12F, FontStyle.Bold),
                ForeColor = Color.FromArgb(46, 92, 138)
            };
            
            _refreshSubmittedButton = new Button
            {
                Text = "تحديث",
                Location = new Point(tab.Width - 300, 12),
                Width = 100,
                Height = 35,
                BackColor = Color.FromArgb(33, 150, 243),
                ForeColor = Color.White,
                Font = new Font("Cairo", 10F, FontStyle.Bold),
                FlatStyle = FlatStyle.Flat
            };
            _refreshSubmittedButton.Click += async (s, e) => await LoadSubmittedInvoicesAsync();
            
            _checkStatusButton = new Button
            {
                Text = "غير مفعل — G4",
                Location = new Point(tab.Width - 430, 12),
                Width = 120,
                Height = 35,
                BackColor = Color.FromArgb(76, 175, 80),
                ForeColor = Color.White,
                Font = new Font("Cairo", 10F, FontStyle.Bold),
                FlatStyle = FlatStyle.Flat,
                Enabled = false
            };
            _checkStatusButton.Click += async (s, e) => await CheckInvoiceStatusAsync();
            
            headerPanel.Controls.AddRange(new Control[] { titleLabel, _refreshSubmittedButton, _checkStatusButton });
            
            // Grid
            _submittedInvoicesGrid = new DataGridView
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
            
            mainPanel.Controls.Add(_submittedInvoicesGrid);
            mainPanel.Controls.Add(headerPanel);
            tab.Controls.Add(mainPanel);
        }

        private async Task LoadSubmittedInvoicesAsync()
        {
            try
            {
                _refreshSubmittedButton.Enabled = false;
                
                var submittedInvoices = await _context.TaxInvoices
                    .Include(i => i.Customer)
                    .Where(i => i.IsSubmittedToZATCA)
                    .OrderByDescending(i => i.SubmittedToZATCADate)
                    .ToListAsync();
                
                var gridData = submittedInvoices.Select(i => new
                {
                    رقم_الفاتورة = i.InvoiceNumber,
                    التاريخ = i.IssueDate.ToString("yyyy-MM-dd"),
                    العميل = i.BuyerName,
                    المجموع = i.TotalWithVAT.ToString("N2"),
                    تاريخ_التقديم = i.SubmittedToZATCADate?.ToString("yyyy-MM-dd HH:mm") ?? "",
                    الحالة = i.ZATCAResponseCode ?? "غير معروف",
                    الرسالة = i.ZATCAResponseMessage ?? "",
                    UUID = i.UUID ?? ""
                }).ToList();
                
                _submittedInvoicesGrid.DataSource = gridData;
                
                AddLog($"📋 تم تحميل {submittedInvoices.Count} فاتورة مقدمة");
            }
            catch (Exception ex)
            {
                AddLog($"❌ خطأ في تحميل الفواتير: {ex.Message}");
                LoggingService.LogError($"Failed to load submitted invoices: {ex.Message}");
            }
            finally
            {
                _refreshSubmittedButton.Enabled = true;
            }
        }

        private async Task CheckInvoiceStatusAsync()
        {
            await Task.CompletedTask;
            const string message = "التحقق القديم كان محاكاة، ولذلك عُطّل حتى اكتمال عميل ZATCA الحقيقي وحفظ الاستجابة الموقعة.";
            AddLog($"⛔ {message}");
            LoggingService.LogWarning("Blocked legacy simulated ZATCA status check.");
            MessageBox.Show(message, "G4 — التحقق غير مفعّل", MessageBoxButtons.OK, MessageBoxIcon.Warning);
        }

        #endregion

        #region Logs Tab

        private void SetupLogsTab(TabPage tab)
        {
            var mainPanel = new Panel { Dock = DockStyle.Fill, Padding = new Padding(10), BackColor = Color.White };
            
            // Header panel
            var headerPanel = new Panel { Dock = DockStyle.Top, Height = 60, BackColor = Color.FromArgb(250, 250, 250) };
            headerPanel.BorderStyle = BorderStyle.FixedSingle;
            
            var titleLabel = new Label
            {
                Text = "📝 سجل العمليات - Operations Log",
                Location = new Point(20, 15),
                Size = new Size(400, 30),
                Font = new Font("Cairo", 12F, FontStyle.Bold),
                ForeColor = Color.FromArgb(46, 92, 138)
            };
            
            _clearLogsButton = new Button
            {
                Text = "مسح السجل",
                Location = new Point(tab.Width - 300, 12),
                Width = 120,
                Height = 35,
                BackColor = Color.FromArgb(244, 67, 54),
                ForeColor = Color.White,
                Font = new Font("Cairo", 10F, FontStyle.Bold),
                FlatStyle = FlatStyle.Flat
            };
            _clearLogsButton.Click += (s, e) => 
            {
                _logsTextBox.Clear();
                AddLog("🗑️ تم مسح السجل");
            };
            
            headerPanel.Controls.AddRange(new Control[] { titleLabel, _clearLogsButton });
            
            // Logs TextBox
            _logsTextBox = new TextBox
            {
                Dock = DockStyle.Fill,
                Multiline = true,
                ReadOnly = true,
                Font = new Font("Consolas", 9F),
                BackColor = Color.FromArgb(30, 30, 30),
                ForeColor = Color.FromArgb(0, 255, 0),
                BorderStyle = BorderStyle.None,
                ScrollBars = ScrollBars.Vertical
            };
            
            mainPanel.Controls.Add(_logsTextBox);
            mainPanel.Controls.Add(headerPanel);
            tab.Controls.Add(mainPanel);
        }

        private void AddLog(string message)
        {
            if (_logsTextBox.InvokeRequired)
            {
                _logsTextBox.Invoke(new Action(() => AddLog(message)));
                return;
            }
            
            var timestamp = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss");
            _logsTextBox.AppendText($"[{timestamp}] {message}\r\n");
            _logsTextBox.SelectionStart = _logsTextBox.Text.Length;
            _logsTextBox.ScrollToCaret();
        }

        #endregion

        #region Certificate Management

        /// <summary>
        /// التحقق من صلاحية الشهادة
        /// </summary>
        private void CheckCertificateExpiry()
        {
            if (_certificate == null)
                return;

            try
            {
                // ✅ Check if expired
                if (_certificate.NotAfter < DateTime.Now)
                {
                    AddLog("❌ الشهادة منتهية الصلاحية!");
                    MessageBox.Show(
                        "⚠️ تحذير: الشهادة منتهية الصلاحية!\n\n" +
                        $"انتهت في: {_certificate.NotAfter:yyyy-MM-dd}\n\n" +
                        "يجب تجديد الشهادة قبل الإرسال إلى ZATCA",
                        "شهادة منتهية",
                        MessageBoxButtons.OK,
                        MessageBoxIcon.Error
                    );
                    LoggingService.LogError($"❌ الشهادة منتهية: {_certificate.Subject}");
                    return;
                }

                // ✅ Check if expiring soon (30 days)
                var daysUntilExpiry = (_certificate.NotAfter - DateTime.Now).Days;
                
                if (daysUntilExpiry <= 30)
                {
                    AddLog($"⚠️ الشهادة ستنتهي خلال {daysUntilExpiry} يوم");
                    
                    MessageBox.Show(
                        $"⚠️ تنبيه: الشهادة قريبة من الانتهاء!\n\n" +
                        $"ستنتهي في: {_certificate.NotAfter:yyyy-MM-dd}\n" +
                        $"المتبقي: {daysUntilExpiry} يوم\n\n" +
                        "يُنصح بتجديد الشهادة في أقرب وقت",
                        "تحذير انتهاء الشهادة",
                        MessageBoxButtons.OK,
                        MessageBoxIcon.Warning
                    );
                    
                    LoggingService.LogWarning($"⚠️ الشهادة ستنتهي خلال {daysUntilExpiry} يوم");
                }
                else if (daysUntilExpiry <= 60)
                {
                    AddLog($"ℹ️ الشهادة ستنتهي خلال {daysUntilExpiry} يوم");
                    LoggingService.LogInfo($"ℹ️ الشهادة صالحة لـ {daysUntilExpiry} يوم آخر");
                }
                else
                {
                    AddLog($"✅ الشهادة صالحة ({daysUntilExpiry} يوم متبقية)");
                }
            }
            catch (Exception ex)
            {
                LoggingService.LogError($"خطأ في فحص الشهادة: {ex.Message}");
            }
        }

        #endregion

        #region Helper Methods

        private async Task LoadInitialDataAsync()
        {
            try
            {
                AddLog("🔄 جاري تحميل البيانات الأولية...");
                
                _vatConfig = await _context.VATConfigurations.FirstOrDefaultAsync();
                
                if (_vatConfig != null)
                {
                    _apiUrlTextBox.Text = _vatConfig.ZATCAApiEndpoint ?? "";
                    _apiKeyTextBox.Text = SensitiveDataProtection.DecryptDatabaseField(_vatConfig.ZATCAApiKey ?? "");
                    _deviceIdTextBox.Text = _vatConfig.DeviceId ?? "";
                    
                    AddLog("✅ تم تحميل الإعدادات المحفوظة");
                }
                
                await LoadPendingInvoicesAsync();
                await LoadSubmittedInvoicesAsync();
                
                AddLog("✅ جاهز للعمل");
            }
            catch (Exception ex)
            {
                AddLog($"❌ خطأ في التحميل: {ex.Message}");
                LoggingService.LogError($"Failed to load initial data: {ex.Message}");
            }
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                _certificate?.Dispose();
            }
            base.Dispose(disposing);
        }

        #endregion
    }
}

