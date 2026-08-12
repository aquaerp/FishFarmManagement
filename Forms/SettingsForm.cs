using System;
using System.Drawing;
using System.Windows.Forms;
using FishFarmManager.Services;
using FishFarmManager.Data;
using FishFarmManager.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;

namespace FishFarmManager.Forms
{
    /// <summary>
    /// نموذج إعدادات النظام - System Settings Form
    /// </summary>
    public partial class SettingsForm : AquaFarmBaseForm
    {
        private readonly FishFarmContext _context = null!;
        private readonly UserSettingsService _settingsService = null!;
        private readonly IConfiguration _configuration = null!;
        private UserSettingsDocument _settings = new();
        private TextBox _dbPathTextBox = null!;
        private NumericUpDown _timeoutNumeric = null!;
        private TextBox _backupPathTextBox = null!;
        private CheckBox _autoBackupCheckBox = null!;
        private ComboBox _frequencyComboBox = null!;
        private NumericUpDown _backupCountNumeric = null!;
        private ComboBox _languageComboBox = null!;
        private ComboBox _themeComboBox = null!;
        private NumericUpDown _fontSizeNumeric = null!;
        private CheckBox _showTooltipsCheckBox = null!;
        private CheckBox _showStatusBarCheckBox = null!;
        private ComboBox _dateFormatComboBox = null!;
        private ComboBox _currencyComboBox = null!;
        private ComboBox _numberFormatComboBox = null!;
        private CheckBox _autoSaveCheckBox = null!;
        private CheckBox _showChartsCheckBox = null!;
        private TabControl _settingsTabControl = null!;
        private Button _saveButton = null!;
        private Button _cancelButton = null!;

        public SettingsForm(FishFarmContext context, UserSettingsService settingsService, IConfiguration configuration)
        {
            // ✅ فحص الصلاحيات - الإعدادات للمديرين فقط
            if (!AuthenticationService.HasPermission(UserRole.Admin))
            {
                MessageBox.Show(
                    "ليس لديك صلاحية لتعديل إعدادات النظام.\nالإعدادات متاحة للمديرين فقط.",
                    "خطأ في الصلاحيات",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Warning
                );
                
                LoggingService.LogWarning(
                    "محاولة وصول غير مصرح بها من {Username} إلى إعدادات النظام",
                    AuthenticationService.CurrentUsername
                );
                
                this.Load += (s, e) => this.Close();
                return;
            }

            _context = context;
            _settingsService = settingsService;
            _configuration = configuration;
            InitializeComponent();
            LoadSettings();
        }

        private void InitializeComponent()
        {
            LocalizationManager.Bind(this, "SystemSettings");
            this.Size = new Size(800, 600);
            this.MinimumSize = new Size(760, 560);
            this.StartPosition = FormStartPosition.CenterScreen;

            // إنشاء شريط العنوان
            var titleBar = CreateTitleBar("إعدادات النظام");
            LocalizationManager.Bind(titleBar.Controls.OfType<Label>().Single(), "SystemSettings");
            this.Controls.Add(titleBar);

            // إنشاء TabControl
            _settingsTabControl = new TabControl();
            _settingsTabControl.Dock = DockStyle.Fill;
            _settingsTabControl.Padding = new Point(20, 10);
            _settingsTabControl.AccessibleName = "أقسام إعدادات النظام";

            // إعدادات قاعدة البيانات
            var dbTab = new TabPage("إعدادات قاعدة البيانات");
            LocalizationManager.Bind(dbTab, "DatabaseSettings");
            CreateDatabaseSettingsTab(dbTab);
            _settingsTabControl.TabPages.Add(dbTab);

            // إعدادات النسخ الاحتياطي
            var backupTab = new TabPage("النسخ الاحتياطي");
            LocalizationManager.Bind(backupTab, "BackupSettings");
            CreateBackupSettingsTab(backupTab);
            _settingsTabControl.TabPages.Add(backupTab);

            // إعدادات الواجهة
            var uiTab = new TabPage("إعدادات الواجهة");
            LocalizationManager.Bind(uiTab, "UiSettings");
            CreateUISettingsTab(uiTab);
            _settingsTabControl.TabPages.Add(uiTab);

            // إعدادات التقارير
            var reportsTab = new TabPage("إعدادات التقارير");
            LocalizationManager.Bind(reportsTab, "ReportSettings");
            CreateReportsSettingsTab(reportsTab);
            _settingsTabControl.TabPages.Add(reportsTab);

            this.Controls.Add(_settingsTabControl);

            // إنشاء شريط الأزرار
            _saveButton = ThemeManager.CreateSuccessButton("حفظ الإعدادات");
            LocalizationManager.Bind(_saveButton, "SaveSettings");
            _saveButton.Click += SaveButton_Click;
            _cancelButton = ThemeManager.CreateSecondaryButton("إلغاء");
            LocalizationManager.Bind(_cancelButton, "Cancel");
            _cancelButton.Click += CancelButton_Click;
            
            var buttonBar = CreateButtonBar(_saveButton, _cancelButton);
            this.Controls.Add(buttonBar);
        }

        private void CreateDatabaseSettingsTab(TabPage tab)
        {
            var panel = new Panel();
            panel.Dock = DockStyle.Fill;
            panel.Padding = new Padding(20);
            panel.AutoScroll = true;

            var y = 20;
            var spacing = 40;

            // عنوان القسم
            var titleLabel = CreateLabel("إعدادات قاعدة البيانات", 16, true);
            LocalizationManager.Bind(titleLabel, "DatabaseSettings");
            titleLabel.Location = new Point(20, y);
            panel.Controls.Add(titleLabel);
            y += spacing;

            // مسار قاعدة البيانات
            var dbPathLabel = CreateLabel("مسار قاعدة البيانات:", 12);
            LocalizationManager.Bind(dbPathLabel, "DatabasePath");
            dbPathLabel.Location = new Point(20, y);
            panel.Controls.Add(dbPathLabel);

            _dbPathTextBox = new TextBox();
            _dbPathTextBox.Location = new Point(200, y - 5);
            _dbPathTextBox.Size = new Size(400, 30);
            _dbPathTextBox.Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right;
            _dbPathTextBox.AccessibleName = "مسار قاعدة البيانات";
            panel.Controls.Add(_dbPathTextBox);

            var browseButton = ThemeManager.CreateSecondaryButton("تصفح");
            LocalizationManager.Bind(browseButton, "Browse");
            browseButton.Location = new Point(610, y - 5);
            browseButton.Size = new Size(80, 25);
            browseButton.Click += (s, e) => BrowseDatabasePath();
            panel.Controls.Add(browseButton);
            y += spacing;

            // إعدادات الاتصال
            var connectionLabel = CreateLabel("إعدادات الاتصال:", 12);
            LocalizationManager.Bind(connectionLabel, "ConnectionSettings");
            connectionLabel.Location = new Point(20, y);
            panel.Controls.Add(connectionLabel);
            y += 30;

            var timeoutLabel = CreateLabel("مهلة الاتصال (ثانية):", 10);
            LocalizationManager.Bind(timeoutLabel, "ConnectionTimeoutSeconds");
            timeoutLabel.Location = new Point(40, y);
            panel.Controls.Add(timeoutLabel);

            _timeoutNumeric = new NumericUpDown();
            _timeoutNumeric.Location = new Point(250, y - 5);
            _timeoutNumeric.Size = new Size(120, 30);
            _timeoutNumeric.Minimum = 5;
            _timeoutNumeric.Maximum = 300;
            _timeoutNumeric.Value = 30;
            _timeoutNumeric.AccessibleName = "مهلة الاتصال بالثواني";
            panel.Controls.Add(_timeoutNumeric);
            y += spacing;

            // زر اختبار الاتصال
            var testConnectionButton = ThemeManager.CreatePrimaryButton("اختبار الاتصال");
            LocalizationManager.Bind(testConnectionButton, "TestConnection");
            testConnectionButton.Location = new Point(40, y);
            testConnectionButton.Click += (s, e) => TestConnection();
            panel.Controls.Add(testConnectionButton);
            y += spacing;

            // إنشاء قاعدة جديدة في مسار منفصل دون حذف القاعدة الحالية
            var recreateDbButton = ThemeManager.CreateWarningButton("إنشاء قاعدة بيانات جديدة");
            LocalizationManager.Bind(recreateDbButton, "CreateNewDatabase");
            recreateDbButton.Location = new Point(40, y);
            recreateDbButton.Click += (s, e) => RecreateDatabase();
            panel.Controls.Add(recreateDbButton);

            tab.Controls.Add(panel);
        }

        private void CreateBackupSettingsTab(TabPage tab)
        {
            var panel = new Panel();
            panel.Dock = DockStyle.Fill;
            panel.Padding = new Padding(20);
            panel.AutoScroll = true;

            var y = 20;
            var spacing = 40;

            // عنوان القسم
            var titleLabel = CreateLabel("إعدادات النسخ الاحتياطي", 16, true);
            LocalizationManager.Bind(titleLabel, "BackupSettings");
            titleLabel.Location = new Point(20, y);
            panel.Controls.Add(titleLabel);
            y += spacing;

            // النسخ الاحتياطي التلقائي
            _autoBackupCheckBox = new CheckBox();
            _autoBackupCheckBox.Text = "تفعيل النسخ الاحتياطي التلقائي";
            LocalizationManager.Bind(_autoBackupCheckBox, "EnableAutomaticBackup");
            _autoBackupCheckBox.Location = new Point(20, y);
            _autoBackupCheckBox.AutoSize = true;
            panel.Controls.Add(_autoBackupCheckBox);
            y += 30;

            // تكرار النسخ الاحتياطي
            var frequencyLabel = CreateLabel("تكرار النسخ الاحتياطي:", 10);
            LocalizationManager.Bind(frequencyLabel, "BackupFrequency");
            frequencyLabel.Location = new Point(40, y);
            panel.Controls.Add(frequencyLabel);

            _frequencyComboBox = new ComboBox();
            _frequencyComboBox.Location = new Point(250, y - 5);
            _frequencyComboBox.Size = new Size(150, 30);
            _frequencyComboBox.DropDownStyle = ComboBoxStyle.DropDownList;
            _frequencyComboBox.Items.AddRange(new[] { "يومي", "أسبوعي", "شهري" });
            panel.Controls.Add(_frequencyComboBox);
            y += spacing;

            // عدد النسخ المحفوظة
            var backupCountLabel = CreateLabel("عدد النسخ المحفوظة:", 10);
            LocalizationManager.Bind(backupCountLabel, "SavedBackupsCount");
            backupCountLabel.Location = new Point(40, y);
            panel.Controls.Add(backupCountLabel);

            _backupCountNumeric = new NumericUpDown();
            _backupCountNumeric.Location = new Point(250, y - 5);
            _backupCountNumeric.Size = new Size(100, 30);
            _backupCountNumeric.Minimum = 1;
            _backupCountNumeric.Maximum = 50;
            panel.Controls.Add(_backupCountNumeric);
            y += spacing;

            // مسار النسخ الاحتياطي
            var backupPathLabel = CreateLabel("مسار النسخ الاحتياطي:", 10);
            LocalizationManager.Bind(backupPathLabel, "BackupPath");
            backupPathLabel.Location = new Point(40, y);
            panel.Controls.Add(backupPathLabel);

            _backupPathTextBox = new TextBox();
            _backupPathTextBox.Location = new Point(250, y - 5);
            _backupPathTextBox.Size = new Size(300, 30);
            _backupPathTextBox.AccessibleName = "مسار النسخ الاحتياطي";
            panel.Controls.Add(_backupPathTextBox);

            var browseBackupButton = ThemeManager.CreateSecondaryButton("تصفح");
            LocalizationManager.Bind(browseBackupButton, "Browse");
            browseBackupButton.Location = new Point(560, y - 5);
            browseBackupButton.Size = new Size(80, 25);
            browseBackupButton.Click += (_, _) =>
            {
                using var dialog = new FolderBrowserDialog { SelectedPath = _backupPathTextBox.Text };
                if (dialog.ShowDialog(this) == DialogResult.OK) _backupPathTextBox.Text = dialog.SelectedPath;
            };
            panel.Controls.Add(browseBackupButton);
            y += spacing;

            // زر إنشاء نسخة احتياطية الآن
            var backupNowButton = ThemeManager.CreateSuccessButton("إنشاء نسخة احتياطية الآن");
            LocalizationManager.Bind(backupNowButton, "CreateBackupNow");
            backupNowButton.Location = new Point(40, y);
            backupNowButton.Click += (s, e) => CreateBackupNow();
            panel.Controls.Add(backupNowButton);

            tab.Controls.Add(panel);
        }

        private void CreateUISettingsTab(TabPage tab)
        {
            var panel = new Panel();
            panel.Dock = DockStyle.Fill;
            panel.Padding = new Padding(20);
            panel.AutoScroll = true;

            var y = 20;
            var spacing = 40;

            // عنوان القسم
            var titleLabel = CreateLabel("إعدادات الواجهة", 16, true);
            LocalizationManager.Bind(titleLabel, "UiSettings");
            titleLabel.Location = new Point(20, y);
            panel.Controls.Add(titleLabel);
            y += spacing;

            // اللغة
            var languageLabel = CreateLabel("اللغة:", 12);
            LocalizationManager.Bind(languageLabel, "Language");
            languageLabel.Location = new Point(20, y);
            panel.Controls.Add(languageLabel);

            _languageComboBox = new ComboBox();
            _languageComboBox.Location = new Point(150, y - 5);
            _languageComboBox.Size = new Size(150, 30);
            _languageComboBox.DropDownStyle = ComboBoxStyle.DropDownList;
            _languageComboBox.DisplayMember = nameof(LanguageOption.DisplayName);
            _languageComboBox.ValueMember = nameof(LanguageOption.CultureName);
            _languageComboBox.Items.AddRange(new object[]
            {
                new LanguageOption(SupportedCultures.Arabic, "العربية"),
                new LanguageOption(SupportedCultures.English, "English")
            });
            _languageComboBox.SelectedIndexChanged += LanguageComboBox_SelectedIndexChanged;
            panel.Controls.Add(_languageComboBox);
            y += spacing;

            // السمة
            var themeLabel = CreateLabel("السمة:", 12);
            LocalizationManager.Bind(themeLabel, "Theme");
            themeLabel.Location = new Point(20, y);
            panel.Controls.Add(themeLabel);

            _themeComboBox = new ComboBox();
            _themeComboBox.Location = new Point(150, y - 5);
            _themeComboBox.Size = new Size(150, 30);
            _themeComboBox.DropDownStyle = ComboBoxStyle.DropDownList;
            _themeComboBox.Items.AddRange(new[] { "الافتراضية", "فاتحة", "داكنة" });
            panel.Controls.Add(_themeComboBox);
            y += spacing;

            // حجم الخط
            var fontSizeLabel = CreateLabel("حجم الخط:", 12);
            LocalizationManager.Bind(fontSizeLabel, "FontSize");
            fontSizeLabel.Location = new Point(20, y);
            panel.Controls.Add(fontSizeLabel);

            _fontSizeNumeric = new NumericUpDown();
            _fontSizeNumeric.Location = new Point(150, y - 5);
            _fontSizeNumeric.Size = new Size(100, 30);
            _fontSizeNumeric.Minimum = 8;
            _fontSizeNumeric.Maximum = 24;
            panel.Controls.Add(_fontSizeNumeric);
            y += spacing;

            // إظهار التلميحات
            _showTooltipsCheckBox = new CheckBox();
            _showTooltipsCheckBox.Text = "إظهار التلميحات";
            LocalizationManager.Bind(_showTooltipsCheckBox, "ShowTooltips");
            _showTooltipsCheckBox.Location = new Point(20, y);
            _showTooltipsCheckBox.AutoSize = true;
            panel.Controls.Add(_showTooltipsCheckBox);
            y += 30;

            // إظهار شريط الحالة
            _showStatusBarCheckBox = new CheckBox();
            _showStatusBarCheckBox.Text = "إظهار شريط الحالة";
            LocalizationManager.Bind(_showStatusBarCheckBox, "ShowStatusBar");
            _showStatusBarCheckBox.Location = new Point(20, y);
            _showStatusBarCheckBox.AutoSize = true;
            panel.Controls.Add(_showStatusBarCheckBox);

            tab.Controls.Add(panel);
        }

        private void CreateReportsSettingsTab(TabPage tab)
        {
            var panel = new Panel();
            panel.Dock = DockStyle.Fill;
            panel.Padding = new Padding(20);
            panel.AutoScroll = true;

            var y = 20;
            var spacing = 40;

            // عنوان القسم
            var titleLabel = CreateLabel("إعدادات التقارير", 16, true);
            LocalizationManager.Bind(titleLabel, "ReportSettings");
            titleLabel.Location = new Point(20, y);
            panel.Controls.Add(titleLabel);
            y += spacing;

            // تنسيق التاريخ الافتراضي
            var dateFormatLabel = CreateLabel("تنسيق التاريخ الافتراضي:", 12);
            LocalizationManager.Bind(dateFormatLabel, "DefaultDateFormat");
            dateFormatLabel.Location = new Point(20, y);
            panel.Controls.Add(dateFormatLabel);

            _dateFormatComboBox = new ComboBox();
            _dateFormatComboBox.Location = new Point(250, y - 5);
            _dateFormatComboBox.Size = new Size(150, 30);
            _dateFormatComboBox.DropDownStyle = ComboBoxStyle.DropDownList;
            _dateFormatComboBox.Items.AddRange(new[] { "yyyy/MM/dd", "dd/MM/yyyy", "MM/dd/yyyy" });
            panel.Controls.Add(_dateFormatComboBox);
            y += spacing;

            // العملة الافتراضية
            var currencyLabel = CreateLabel("العملة الافتراضية:", 12);
            LocalizationManager.Bind(currencyLabel, "DefaultCurrency");
            currencyLabel.Location = new Point(20, y);
            panel.Controls.Add(currencyLabel);

            _currencyComboBox = new ComboBox();
            _currencyComboBox.Location = new Point(250, y - 5);
            _currencyComboBox.Size = new Size(200, 30);
            _currencyComboBox.DropDownStyle = ComboBoxStyle.DropDownList;
            _currencyComboBox.Items.AddRange(new[] { "ريال سعودي (SAR)", "دولار أمريكي (USD)", "يورو (EUR)" });
            panel.Controls.Add(_currencyComboBox);
            y += spacing;

            // تنسيق الأرقام
            var numberFormatLabel = CreateLabel("تنسيق الأرقام:", 12);
            LocalizationManager.Bind(numberFormatLabel, "NumberFormat");
            numberFormatLabel.Location = new Point(20, y);
            panel.Controls.Add(numberFormatLabel);

            _numberFormatComboBox = new ComboBox();
            _numberFormatComboBox.Location = new Point(250, y - 5);
            _numberFormatComboBox.Size = new Size(150, 30);
            _numberFormatComboBox.DropDownStyle = ComboBoxStyle.DropDownList;
            _numberFormatComboBox.Items.AddRange(new[] { "1,234.56", "1.234,56", "1 234,56" });
            panel.Controls.Add(_numberFormatComboBox);
            y += spacing;

            // حفظ التقارير تلقائياً
            _autoSaveCheckBox = new CheckBox();
            _autoSaveCheckBox.Text = "حفظ التقارير تلقائياً";
            LocalizationManager.Bind(_autoSaveCheckBox, "AutoSaveReports");
            _autoSaveCheckBox.Location = new Point(20, y);
            _autoSaveCheckBox.AutoSize = true;
            panel.Controls.Add(_autoSaveCheckBox);
            y += 30;

            // إظهار الرسوم البيانية
            _showChartsCheckBox = new CheckBox();
            _showChartsCheckBox.Text = "إظهار الرسوم البيانية في التقارير";
            LocalizationManager.Bind(_showChartsCheckBox, "ShowChartsInReports");
            _showChartsCheckBox.Location = new Point(20, y);
            _showChartsCheckBox.AutoSize = true;
            panel.Controls.Add(_showChartsCheckBox);

            tab.Controls.Add(panel);
        }

        private void LoadSettings()
        {
            _settings = _settingsService.Load();
            _dbPathTextBox.Text = UserSettingsService.ExtractDatabasePath(
                _settings.ConnectionStrings.DefaultConnection);
            _timeoutNumeric.Value = _settings.Database.ConnectionTimeoutSeconds;
            _backupPathTextBox.Text = _settings.Backup.Directory;
            _autoBackupCheckBox.Checked = _settings.Backup.AutomaticEnabled;
            SelectValue(_frequencyComboBox, _settings.Backup.Frequency);
            _backupCountNumeric.Value = _settings.Backup.MaxBackups;
            SelectLanguage(_settings.UserInterface.Language);
            SelectValue(_themeComboBox, _settings.UserInterface.Theme);
            _fontSizeNumeric.Value = _settings.UserInterface.FontSize;
            _showTooltipsCheckBox.Checked = _settings.UserInterface.ShowTooltips;
            _showStatusBarCheckBox.Checked = _settings.UserInterface.ShowStatusBar;
            SelectValue(_dateFormatComboBox, _settings.Reports.DateFormat);
            SelectValue(_currencyComboBox, _settings.Reports.Currency);
            SelectValue(_numberFormatComboBox, _settings.Reports.NumberFormat);
            _autoSaveCheckBox.Checked = _settings.Reports.AutoSave;
            _showChartsCheckBox.Checked = _settings.Reports.ShowCharts;
        }

        private void SaveButton_Click(object? sender, EventArgs e)
        {
            try
            {
                _settings.ConnectionStrings.DefaultConnection = UserSettingsService.CreateConnectionString(
                    _dbPathTextBox.Text.Trim(), (int)_timeoutNumeric.Value);
                _settings.Database.ConnectionTimeoutSeconds = (int)_timeoutNumeric.Value;
                _settings.Backup.Directory = Path.GetFullPath(_backupPathTextBox.Text.Trim());
                _settings.Backup.AutomaticEnabled = _autoBackupCheckBox.Checked;
                _settings.Backup.Frequency = SelectedValue(_frequencyComboBox);
                _settings.Backup.MaxBackups = (int)_backupCountNumeric.Value;
                _settings.UserInterface.Language = SelectedLanguage();
                _settings.UserInterface.Theme = SelectedValue(_themeComboBox);
                _settings.UserInterface.FontSize = (int)_fontSizeNumeric.Value;
                _settings.UserInterface.ShowTooltips = _showTooltipsCheckBox.Checked;
                _settings.UserInterface.ShowStatusBar = _showStatusBarCheckBox.Checked;
                _settings.Reports.DateFormat = SelectedValue(_dateFormatComboBox);
                _settings.Reports.Currency = SelectedValue(_currencyComboBox);
                _settings.Reports.NumberFormat = SelectedValue(_numberFormatComboBox);
                _settings.Reports.AutoSave = _autoSaveCheckBox.Checked;
                _settings.Reports.ShowCharts = _showChartsCheckBox.Checked;
                _settingsService.Save(_settings);
                ThemeManager.ShowSuccess(LocalizationManager.Get("SettingsSavedRestart"));
                this.DialogResult = DialogResult.OK;
                this.Close();
            }
            catch (Exception ex)
            {
                LoggingService.LogError(ex, "Saving user settings failed");
                ThemeManager.ShowError(LocalizationManager.Get("SettingsSaveFailed"));
            }
        }

        private void CancelButton_Click(object? sender, EventArgs e)
        {
            this.DialogResult = DialogResult.Cancel;
            this.Close();
        }

        private void BrowseDatabasePath()
        {
            using (var dialog = new SaveFileDialog())
            {
                dialog.Filter = "SQLite Database (*.db)|*.db|All Files (*.*)|*.*";
                dialog.FileName = Path.GetFileName(_dbPathTextBox.Text);
                
                if (dialog.ShowDialog() == DialogResult.OK)
                {
                    _dbPathTextBox.Text = dialog.FileName;
                    ThemeManager.ShowInfo(LocalizationManager.Get("DatabasePathRestart"));
                }
            }
        }

        private void TestConnection()
        {
            try
            {
                using var connection = new Microsoft.Data.Sqlite.SqliteConnection(
                    UserSettingsService.CreateConnectionString(_dbPathTextBox.Text.Trim(), (int)_timeoutNumeric.Value));
                connection.Open();
                using var command = connection.CreateCommand();
                command.CommandText = "PRAGMA integrity_check;";
                var result = Convert.ToString(command.ExecuteScalar());
                if (!string.Equals(result, "ok", StringComparison.OrdinalIgnoreCase))
                    throw new InvalidOperationException($"فشل فحص السلامة: {result}");
                ThemeManager.ShowSuccess(LocalizationManager.Get("ConnectionTestSucceeded"));
            }
            catch (Exception ex)
            {
                LoggingService.LogError(ex, "Database connection test failed");
                ThemeManager.ShowError(LocalizationManager.Get("ConnectionTestFailed"));
            }
        }

        private void RecreateDatabase()
        {
            using var dialog = new SaveFileDialog
            {
                Filter = "SQLite Database (*.db)|*.db", FileName = "FishFarm-New.db"
            };
            if (dialog.ShowDialog(this) != DialogResult.OK) return;
            try
            {
                var current = UserSettingsService.ExtractDatabasePath(_context.Database.GetConnectionString()!);
                var target = Path.GetFullPath(dialog.FileName);
                if (string.Equals(Path.GetFullPath(current), target, StringComparison.OrdinalIgnoreCase))
                    throw new InvalidOperationException("اختر مساراً جديداً؛ لن يتم حذف قاعدة البيانات الحالية.");
                if (File.Exists(target)) throw new InvalidOperationException("الملف موجود بالفعل.");
                var options = new DbContextOptionsBuilder<FishFarmContext>()
                    .UseSqlite(UserSettingsService.CreateConnectionString(target, (int)_timeoutNumeric.Value)).Options;
                using var context = new FishFarmContext(options);
                context.Database.Migrate();
                _dbPathTextBox.Text = target;
                ThemeManager.ShowSuccess(LocalizationManager.Get("DatabaseCreated"));
            }
            catch (Exception ex)
            {
                LoggingService.LogError(ex, "Creating a new database failed");
                ThemeManager.ShowError(LocalizationManager.Get("DatabaseCreateFailed"));
            }
        }

        private async void CreateBackupNow()
        {
            try
            {
                var overrides = new Dictionary<string, string?>
                {
                    ["ConnectionStrings:DefaultConnection"] = _context.Database.GetConnectionString(),
                    ["Backup:Directory"] = Path.GetFullPath(_backupPathTextBox.Text.Trim()),
                    ["Backup:MaxBackups"] = ((int)_backupCountNumeric.Value).ToString(),
                    ["Backup:Encrypt"] = "true"
                };
                var configuration = new ConfigurationBuilder()
                    .AddConfiguration(_configuration).AddInMemoryCollection(overrides).Build();
                var service = new BackupService(_context, configuration);
                if (!await service.CreateBackup(BackupType.Manual))
                    throw new InvalidOperationException("The verified backup operation failed.");
                var backup = service.GetAvailableBackups().First();
                ThemeManager.ShowSuccess(LocalizationManager.Format("BackupCreated", backup.FileName));
            }
            catch (Exception ex)
            {
                LoggingService.LogError(ex, "Creating a database backup failed");
                ThemeManager.ShowError(LocalizationManager.Get("BackupCreateFailed"));
            }
        }

        private static void SelectValue(ComboBox comboBox, string value)
        {
            comboBox.SelectedItem = comboBox.Items.Contains(value) ? value : comboBox.Items[0];
        }

        private static string SelectedValue(ComboBox comboBox)
        {
            return Convert.ToString(comboBox.SelectedItem) ?? string.Empty;
        }

        private void SelectLanguage(string cultureName)
        {
            var normalized = SupportedCultures.Normalize(cultureName);
            _languageComboBox.SelectedItem = _languageComboBox.Items
                .OfType<LanguageOption>()
                .First(option => option.CultureName == normalized);
        }

        private string SelectedLanguage() =>
            (_languageComboBox.SelectedItem as LanguageOption)?.CultureName
            ?? SupportedCultures.Default;

        private void LanguageComboBox_SelectedIndexChanged(object? sender, EventArgs e)
        {
            LocalizationManager.SetCulture(SelectedLanguage());
        }

        private sealed record LanguageOption(string CultureName, string DisplayName);
    }
}
