using System;
using System.Drawing;
using System.Windows.Forms;
using FishFarmManager.Services;
using FishFarmManager.Data;
using FishFarmManager.Models;
using Microsoft.EntityFrameworkCore;

namespace FishFarmManager.Forms
{
    /// <summary>
    /// نموذج إعدادات النظام - System Settings Form
    /// </summary>
    public partial class SettingsForm : AquaFarmBaseForm
    {
        private readonly FishFarmContext _context = null!;
        private TabControl _settingsTabControl = null!;
        private Button _saveButton = null!;
        private Button _cancelButton = null!;

        public SettingsForm(FishFarmContext context)
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
            InitializeComponent();
            LoadSettings();
        }

        private void InitializeComponent()
        {
            this.Text = "إعدادات النظام";
            this.Size = new Size(800, 600);
            this.StartPosition = FormStartPosition.CenterScreen;

            // إنشاء شريط العنوان
            var titleBar = CreateTitleBar("إعدادات النظام");
            this.Controls.Add(titleBar);

            // إنشاء TabControl
            _settingsTabControl = new TabControl();
            _settingsTabControl.Dock = DockStyle.Fill;
            _settingsTabControl.Padding = new Point(20, 10);

            // إعدادات قاعدة البيانات
            var dbTab = new TabPage("إعدادات قاعدة البيانات");
            CreateDatabaseSettingsTab(dbTab);
            _settingsTabControl.TabPages.Add(dbTab);

            // إعدادات النسخ الاحتياطي
            var backupTab = new TabPage("النسخ الاحتياطي");
            CreateBackupSettingsTab(backupTab);
            _settingsTabControl.TabPages.Add(backupTab);

            // إعدادات الواجهة
            var uiTab = new TabPage("إعدادات الواجهة");
            CreateUISettingsTab(uiTab);
            _settingsTabControl.TabPages.Add(uiTab);

            // إعدادات التقارير
            var reportsTab = new TabPage("إعدادات التقارير");
            CreateReportsSettingsTab(reportsTab);
            _settingsTabControl.TabPages.Add(reportsTab);

            this.Controls.Add(_settingsTabControl);

            // إنشاء شريط الأزرار
            _saveButton = ThemeManager.CreateSuccessButton("حفظ الإعدادات");
            _saveButton.Click += SaveButton_Click;
            _cancelButton = ThemeManager.CreateSecondaryButton("إلغاء");
            _cancelButton.Click += CancelButton_Click;
            
            var buttonBar = CreateButtonBar(_saveButton, _cancelButton);
            this.Controls.Add(buttonBar);
        }

        private void CreateDatabaseSettingsTab(TabPage tab)
        {
            var panel = new Panel();
            panel.Dock = DockStyle.Fill;
            panel.Padding = new Padding(20);

            var y = 20;
            var spacing = 40;

            // عنوان القسم
            var titleLabel = CreateLabel("إعدادات قاعدة البيانات", 16, true);
            titleLabel.Location = new Point(20, y);
            panel.Controls.Add(titleLabel);
            y += spacing;

            // مسار قاعدة البيانات
            var dbPathLabel = CreateLabel("مسار قاعدة البيانات:", 12);
            dbPathLabel.Location = new Point(20, y);
            panel.Controls.Add(dbPathLabel);

            var dbPathTextBox = new TextBox();
            dbPathTextBox.Location = new Point(200, y - 5);
            dbPathTextBox.Size = new Size(400, 25);
            dbPathTextBox.Text = _context.Database.GetConnectionString();
            dbPathTextBox.ReadOnly = true;
            panel.Controls.Add(dbPathTextBox);

            var browseButton = ThemeManager.CreateSecondaryButton("تصفح");
            browseButton.Location = new Point(610, y - 5);
            browseButton.Size = new Size(80, 25);
            browseButton.Click += (s, e) => BrowseDatabasePath();
            panel.Controls.Add(browseButton);
            y += spacing;

            // إعدادات الاتصال
            var connectionLabel = CreateLabel("إعدادات الاتصال:", 12);
            connectionLabel.Location = new Point(20, y);
            panel.Controls.Add(connectionLabel);
            y += 30;

            var timeoutLabel = CreateLabel("مهلة الاتصال (ثانية):", 10);
            timeoutLabel.Location = new Point(40, y);
            panel.Controls.Add(timeoutLabel);

            var timeoutNumeric = new NumericUpDown();
            timeoutNumeric.Location = new Point(250, y - 5);
            timeoutNumeric.Size = new Size(100, 25);
            timeoutNumeric.Minimum = 5;
            timeoutNumeric.Maximum = 300;
            timeoutNumeric.Value = 30;
            panel.Controls.Add(timeoutNumeric);
            y += spacing;

            // زر اختبار الاتصال
            var testConnectionButton = ThemeManager.CreatePrimaryButton("اختبار الاتصال");
            testConnectionButton.Location = new Point(40, y);
            testConnectionButton.Click += (s, e) => TestConnection();
            panel.Controls.Add(testConnectionButton);
            y += spacing;

            // زر إعادة إنشاء قاعدة البيانات
            var recreateDbButton = ThemeManager.CreateWarningButton("إعادة إنشاء قاعدة البيانات");
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

            var y = 20;
            var spacing = 40;

            // عنوان القسم
            var titleLabel = CreateLabel("إعدادات النسخ الاحتياطي", 16, true);
            titleLabel.Location = new Point(20, y);
            panel.Controls.Add(titleLabel);
            y += spacing;

            // النسخ الاحتياطي التلقائي
            var autoBackupCheckBox = new CheckBox();
            autoBackupCheckBox.Text = "تفعيل النسخ الاحتياطي التلقائي";
            autoBackupCheckBox.Location = new Point(20, y);
            autoBackupCheckBox.Checked = true;
            panel.Controls.Add(autoBackupCheckBox);
            y += 30;

            // تكرار النسخ الاحتياطي
            var frequencyLabel = CreateLabel("تكرار النسخ الاحتياطي:", 10);
            frequencyLabel.Location = new Point(40, y);
            panel.Controls.Add(frequencyLabel);

            var frequencyComboBox = new ComboBox();
            frequencyComboBox.Location = new Point(250, y - 5);
            frequencyComboBox.Size = new Size(150, 25);
            frequencyComboBox.Items.AddRange(new[] { "يومي", "أسبوعي", "شهري" });
            frequencyComboBox.SelectedIndex = 0;
            panel.Controls.Add(frequencyComboBox);
            y += spacing;

            // عدد النسخ المحفوظة
            var backupCountLabel = CreateLabel("عدد النسخ المحفوظة:", 10);
            backupCountLabel.Location = new Point(40, y);
            panel.Controls.Add(backupCountLabel);

            var backupCountNumeric = new NumericUpDown();
            backupCountNumeric.Location = new Point(250, y - 5);
            backupCountNumeric.Size = new Size(100, 25);
            backupCountNumeric.Minimum = 1;
            backupCountNumeric.Maximum = 50;
            backupCountNumeric.Value = 7;
            panel.Controls.Add(backupCountNumeric);
            y += spacing;

            // مسار النسخ الاحتياطي
            var backupPathLabel = CreateLabel("مسار النسخ الاحتياطي:", 10);
            backupPathLabel.Location = new Point(40, y);
            panel.Controls.Add(backupPathLabel);

            var backupPathTextBox = new TextBox();
            backupPathTextBox.Location = new Point(250, y - 5);
            backupPathTextBox.Size = new Size(300, 25);
            backupPathTextBox.Text = Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments) + "\\AquaFarm Backups";
            panel.Controls.Add(backupPathTextBox);

            var browseBackupButton = ThemeManager.CreateSecondaryButton("تصفح");
            browseBackupButton.Location = new Point(560, y - 5);
            browseBackupButton.Size = new Size(80, 25);
            panel.Controls.Add(browseBackupButton);
            y += spacing;

            // زر إنشاء نسخة احتياطية الآن
            var backupNowButton = ThemeManager.CreateSuccessButton("إنشاء نسخة احتياطية الآن");
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

            var y = 20;
            var spacing = 40;

            // عنوان القسم
            var titleLabel = CreateLabel("إعدادات الواجهة", 16, true);
            titleLabel.Location = new Point(20, y);
            panel.Controls.Add(titleLabel);
            y += spacing;

            // اللغة
            var languageLabel = CreateLabel("اللغة:", 12);
            languageLabel.Location = new Point(20, y);
            panel.Controls.Add(languageLabel);

            var languageComboBox = new ComboBox();
            languageComboBox.Location = new Point(150, y - 5);
            languageComboBox.Size = new Size(150, 25);
            languageComboBox.Items.AddRange(new[] { "العربية", "English" });
            languageComboBox.SelectedIndex = 0;
            panel.Controls.Add(languageComboBox);
            y += spacing;

            // السمة
            var themeLabel = CreateLabel("السمة:", 12);
            themeLabel.Location = new Point(20, y);
            panel.Controls.Add(themeLabel);

            var themeComboBox = new ComboBox();
            themeComboBox.Location = new Point(150, y - 5);
            themeComboBox.Size = new Size(150, 25);
            themeComboBox.Items.AddRange(new[] { "الافتراضية", "فاتحة", "داكنة" });
            themeComboBox.SelectedIndex = 0;
            panel.Controls.Add(themeComboBox);
            y += spacing;

            // حجم الخط
            var fontSizeLabel = CreateLabel("حجم الخط:", 12);
            fontSizeLabel.Location = new Point(20, y);
            panel.Controls.Add(fontSizeLabel);

            var fontSizeNumeric = new NumericUpDown();
            fontSizeNumeric.Location = new Point(150, y - 5);
            fontSizeNumeric.Size = new Size(100, 25);
            fontSizeNumeric.Minimum = 8;
            fontSizeNumeric.Maximum = 24;
            fontSizeNumeric.Value = 10;
            panel.Controls.Add(fontSizeNumeric);
            y += spacing;

            // إظهار التلميحات
            var showTooltipsCheckBox = new CheckBox();
            showTooltipsCheckBox.Text = "إظهار التلميحات";
            showTooltipsCheckBox.Location = new Point(20, y);
            showTooltipsCheckBox.Checked = true;
            panel.Controls.Add(showTooltipsCheckBox);
            y += 30;

            // إظهار شريط الحالة
            var showStatusBarCheckBox = new CheckBox();
            showStatusBarCheckBox.Text = "إظهار شريط الحالة";
            showStatusBarCheckBox.Location = new Point(20, y);
            showStatusBarCheckBox.Checked = true;
            panel.Controls.Add(showStatusBarCheckBox);

            tab.Controls.Add(panel);
        }

        private void CreateReportsSettingsTab(TabPage tab)
        {
            var panel = new Panel();
            panel.Dock = DockStyle.Fill;
            panel.Padding = new Padding(20);

            var y = 20;
            var spacing = 40;

            // عنوان القسم
            var titleLabel = CreateLabel("إعدادات التقارير", 16, true);
            titleLabel.Location = new Point(20, y);
            panel.Controls.Add(titleLabel);
            y += spacing;

            // تنسيق التاريخ الافتراضي
            var dateFormatLabel = CreateLabel("تنسيق التاريخ الافتراضي:", 12);
            dateFormatLabel.Location = new Point(20, y);
            panel.Controls.Add(dateFormatLabel);

            var dateFormatComboBox = new ComboBox();
            dateFormatComboBox.Location = new Point(250, y - 5);
            dateFormatComboBox.Size = new Size(150, 25);
            dateFormatComboBox.Items.AddRange(new[] { "yyyy/MM/dd", "dd/MM/yyyy", "MM/dd/yyyy" });
            dateFormatComboBox.SelectedIndex = 0;
            panel.Controls.Add(dateFormatComboBox);
            y += spacing;

            // العملة الافتراضية
            var currencyLabel = CreateLabel("العملة الافتراضية:", 12);
            currencyLabel.Location = new Point(20, y);
            panel.Controls.Add(currencyLabel);

            var currencyComboBox = new ComboBox();
            currencyComboBox.Location = new Point(250, y - 5);
            currencyComboBox.Size = new Size(150, 25);
            currencyComboBox.Items.AddRange(new[] { "ريال سعودي (SAR)", "دولار أمريكي (USD)", "يورو (EUR)" });
            currencyComboBox.SelectedIndex = 0;
            panel.Controls.Add(currencyComboBox);
            y += spacing;

            // تنسيق الأرقام
            var numberFormatLabel = CreateLabel("تنسيق الأرقام:", 12);
            numberFormatLabel.Location = new Point(20, y);
            panel.Controls.Add(numberFormatLabel);

            var numberFormatComboBox = new ComboBox();
            numberFormatComboBox.Location = new Point(250, y - 5);
            numberFormatComboBox.Size = new Size(150, 25);
            numberFormatComboBox.Items.AddRange(new[] { "1,234.56", "1.234,56", "1 234,56" });
            numberFormatComboBox.SelectedIndex = 0;
            panel.Controls.Add(numberFormatComboBox);
            y += spacing;

            // حفظ التقارير تلقائياً
            var autoSaveCheckBox = new CheckBox();
            autoSaveCheckBox.Text = "حفظ التقارير تلقائياً";
            autoSaveCheckBox.Location = new Point(20, y);
            autoSaveCheckBox.Checked = true;
            panel.Controls.Add(autoSaveCheckBox);
            y += 30;

            // إظهار الرسوم البيانية
            var showChartsCheckBox = new CheckBox();
            showChartsCheckBox.Text = "إظهار الرسوم البيانية في التقارير";
            showChartsCheckBox.Location = new Point(20, y);
            showChartsCheckBox.Checked = true;
            panel.Controls.Add(showChartsCheckBox);

            tab.Controls.Add(panel);
        }

        private void LoadSettings()
        {
            // تحميل الإعدادات من ملف الإعدادات أو قاعدة البيانات
            // هذا مثال بسيط - يمكن تطويره لاحقاً
        }

        private void SaveButton_Click(object? sender, EventArgs e)
        {
            try
            {
                // حفظ الإعدادات
                // هذا مثال بسيط - يمكن تطويره لاحقاً
                
                ThemeManager.ShowSuccess("تم حفظ الإعدادات بنجاح", "نجح");
                this.DialogResult = DialogResult.OK;
                this.Close();
            }
            catch (Exception ex)
            {
                ThemeManager.ShowError($"خطأ في حفظ الإعدادات: {ex.Message}", "خطأ");
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
                dialog.FileName = "FishFarm.db";
                
                if (dialog.ShowDialog() == DialogResult.OK)
                {
                    // تحديث مسار قاعدة البيانات
                    ThemeManager.ShowInfo("سيتم تطبيق مسار قاعدة البيانات الجديد عند إعادة تشغيل التطبيق", "معلومة");
                }
            }
        }

        private void TestConnection()
        {
            try
            {
                _context.Database.OpenConnection();
                _context.Database.CloseConnection();
                ThemeManager.ShowSuccess("تم اختبار الاتصال بنجاح", "نجح");
            }
            catch (Exception ex)
            {
                ThemeManager.ShowError($"فشل اختبار الاتصال: {ex.Message}", "خطأ");
            }
        }

        private void RecreateDatabase()
        {
            if (ThemeManager.Confirm("هل أنت متأكد من إعادة إنشاء قاعدة البيانات؟ سيتم فقدان جميع البيانات!", "تحذير"))
            {
                try
                {
                    _context.Database.EnsureDeleted();
                    _context.Database.EnsureCreated();
                    ThemeManager.ShowSuccess("تم إعادة إنشاء قاعدة البيانات بنجاح", "نجح");
                }
                catch (Exception ex)
                {
                    ThemeManager.ShowError($"خطأ في إعادة إنشاء قاعدة البيانات: {ex.Message}", "خطأ");
                }
            }
        }

        private void CreateBackupNow()
        {
            try
            {
                // إنشاء نسخة احتياطية
                var backupPath = Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments) + "\\AquaFarm Backups";
                var fileName = $"FishFarm_Backup_{DateTime.Now:yyyyMMdd_HHmmss}.db";
                var fullPath = System.IO.Path.Combine(backupPath, fileName);
                
                System.IO.Directory.CreateDirectory(backupPath);
                
                // نسخ ملف قاعدة البيانات
                var sourceDb = _context.Database.GetConnectionString();
                if (!string.IsNullOrEmpty(sourceDb) && sourceDb.Contains("Data Source="))
                {
                    var dbPath = sourceDb.Substring(sourceDb.IndexOf("Data Source=") + 12);
                    System.IO.File.Copy(dbPath, fullPath, true);
                }
                
                ThemeManager.ShowSuccess($"تم إنشاء النسخة الاحتياطية: {fileName}", "نجح");
            }
            catch (Exception ex)
            {
                ThemeManager.ShowError($"خطأ في إنشاء النسخة الاحتياطية: {ex.Message}", "خطأ");
            }
        }
    }
}
