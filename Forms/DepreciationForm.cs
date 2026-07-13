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
    /// <summary>
    /// نموذج حساب ومعالجة الإهلاك
    /// Depreciation Processing Form
    /// </summary>
    public partial class DepreciationForm : Form
    {
        private readonly FishFarmContext _context;

        private ComboBox _monthComboBox = null!;
        private ComboBox _yearComboBox = null!;
        private DataGridView _depreciationGrid = null!;
        private Label _totalDepreciationLabel = null!;
        private Button _calculateButton = null!;
        private Button _saveButton = null!;

        public DepreciationForm(FishFarmContext context)
        {
            _context = context ?? throw new ArgumentNullException(nameof(context));

            if (!AuthenticationService.HasPermission(UserRole.Admin, UserRole.Manager, UserRole.Accountant))
            {
                MessageBox.Show("ليس لديك صلاحية لمعالجة الإهلاك", "تحذير", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                this.Load += (s, e) => this.Close();
                return;
            }

            InitializeComponent();
            InitializeForm();
        }

        private void InitializeComponent()
        {
            this.Text = "معالجة الإهلاك الشهري";
            this.Size = new Size(1400, 800);
            this.StartPosition = FormStartPosition.CenterScreen;
            this.RightToLeft = RightToLeft.Yes;
            this.RightToLeftLayout = true;
            this.Font = new Font("Cairo", 10F);

            var mainPanel = new Panel { Dock = DockStyle.Fill, Padding = new Padding(20), BackColor = Color.FromArgb(240, 240, 240) };

            var titleLabel = new Label
            {
                Text = "معالجة الإهلاك الشهري للأصول الثابتة",
                Location = new Point(20, 10),
                Size = new Size(1340, 40),
                Font = new Font("Cairo", 14F, FontStyle.Bold),
                ForeColor = Color.FromArgb(46, 92, 138),
                TextAlign = ContentAlignment.MiddleRight
            };
            mainPanel.Controls.Add(titleLabel);

            CreateFilterSection(mainPanel, 60);
            CreateGridSection(mainPanel, 140);
            CreateSummarySection(mainPanel, 620);
            CreateButtonsSection(mainPanel);

            this.Controls.Add(mainPanel);
        }

        private void CreateFilterSection(Panel parent, int startY)
        {
            var filterPanel = new GroupBox { Text = "الفترة", Location = new Point(20, startY), Size = new Size(1340, 70), Font = new Font("Cairo", 10F, FontStyle.Bold) };

            _monthComboBox = new ComboBox { Location = new Point(1000, 30), Size = new Size(150, 30), DropDownStyle = ComboBoxStyle.DropDownList };
            _yearComboBox = new ComboBox { Location = new Point(820, 30), Size = new Size(120, 30), DropDownStyle = ComboBoxStyle.DropDownList };

            LoadMonthsAndYears();

            filterPanel.Controls.Add(_monthComboBox);
            filterPanel.Controls.Add(_yearComboBox);
            parent.Controls.Add(filterPanel);
        }

        private void CreateGridSection(Panel parent, int startY)
        {
            _depreciationGrid = new DataGridView
            {
                Location = new Point(20, startY),
                Size = new Size(1340, 470),
                Font = new Font("Cairo", 9F),
                AllowUserToAddRows = false,
                ReadOnly = true,
                BackgroundColor = Color.White,
                AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill
            };

            parent.Controls.Add(_depreciationGrid);
        }

        private void CreateSummarySection(Panel parent, int startY)
        {
            var summaryPanel = new GroupBox { Text = "الملخص", Location = new Point(20, startY), Size = new Size(1340, 70), Font = new Font("Cairo", 10F, FontStyle.Bold) };

            var label = new Label { Text = "إجمالي الإهلاك:", Location = new Point(950, 30), Size = new Size(150, 25), TextAlign = ContentAlignment.MiddleRight };
            summaryPanel.Controls.Add(label);

            _totalDepreciationLabel = new Label
            {
                Text = "0.00 ريال",
                Location = new Point(700, 30),
                Size = new Size(240, 30),
                TextAlign = ContentAlignment.MiddleCenter,
                Font = new Font("Cairo", 12F, FontStyle.Bold),
                ForeColor = Color.FromArgb(229, 57, 53),
                BorderStyle = BorderStyle.FixedSingle,
                BackColor = Color.White
            };
            summaryPanel.Controls.Add(_totalDepreciationLabel);

            parent.Controls.Add(summaryPanel);
        }

        private void CreateButtonsSection(Panel parent)
        {
            var buttonPanel = new Panel { Location = new Point(20, 700), Size = new Size(1340, 50), BackColor = Color.Transparent };

            _calculateButton = new Button
            {
                Text = "حساب الإهلاك",
                Location = new Point(1050, 10),
                Size = new Size(150, 35),
                Font = new Font("Cairo", 10F, FontStyle.Bold),
                BackColor = Color.FromArgb(46, 92, 138),
                ForeColor = Color.White
            };
            _calculateButton.Click += async (s, e) => await CalculateButton_ClickAsync();
            buttonPanel.Controls.Add(_calculateButton);

            _saveButton = new Button
            {
                Text = "حفظ",
                Location = new Point(880, 10),
                Size = new Size(150, 35),
                Font = new Font("Cairo", 10F, FontStyle.Bold),
                BackColor = Color.FromArgb(38, 166, 154),
                ForeColor = Color.White,
                Enabled = false
            };
            _saveButton.Click += async (s, e) => await SaveButton_ClickAsync();
            buttonPanel.Controls.Add(_saveButton);

            parent.Controls.Add(buttonPanel);
        }

        private void InitializeForm()
        {
            try
            {
                if (_monthComboBox.Items.Count > 0)
                {
                    var currentMonth = DateTime.Now.Month - 1;
                    if (currentMonth >= 0 && currentMonth < _monthComboBox.Items.Count)
                    {
                        _monthComboBox.SelectedIndex = currentMonth;
                    }
                    else
                    {
                        _monthComboBox.SelectedIndex = 0;
                    }
                }
                
                if (_yearComboBox.Items.Count > 0)
                {
                    _yearComboBox.SelectedItem = DateTime.Now.Year;
                }
                
                LoggingService.LogInfo("DepreciationForm initialized");
            }
            catch (Exception ex)
            {
                LoggingService.LogError(ex, "خطأ في تهيئة DepreciationForm");
                MessageBox.Show($"خطأ في تهيئة النموذج: {ex.Message}", "خطأ", 
                    MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void LoadMonthsAndYears()
        {
            var months = Enumerable.Range(1, 12).Select(m => new { Value = m, Display = $"{m} - {new DateTime(2025, m, 1):MMMM}" }).ToList();
            _monthComboBox.DataSource = months;
            _monthComboBox.DisplayMember = "Display";
            _monthComboBox.ValueMember = "Value";

            var years = Enumerable.Range(DateTime.Now.Year - 2, 5).ToList();
            _yearComboBox.DataSource = years;
        }

        private async Task CalculateButton_ClickAsync()
        {
            try
            {
                var month = _monthComboBox.SelectedIndex + 1;
                var year = int.Parse(_yearComboBox.Text);

                // الحصول على جميع الأصول النشطة
                var assets = await _context.FixedAssets
                    .Where(a => a.Status == AssetStatus.Active)
                    .ToListAsync();

                if (assets.Count == 0)
                {
                    MessageBox.Show("لا توجد أصول نشطة لحساب الإهلاك", "تنبيه", 
                        MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    return;
                }

                var result = MessageBox.Show(
                    $"سيتم حساب الإهلاك لـ {assets.Count} أصل\nللشهر {month}/{year}\n\nهل تريد المتابعة؟",
                    "تأكيد",
                    MessageBoxButtons.YesNo,
                    MessageBoxIcon.Question
                );

                if (result != DialogResult.Yes)
                    return;

                // حساب الإهلاك لكل أصل
                var depreciations = new List<AssetDepreciation>();
                foreach (var asset in assets)
                {
                    var monthlyDepreciation = asset.CalculateAnnualDepreciation() / 12;
                    
                    var depreciation = new AssetDepreciation
                    {
                        FixedAssetId = asset.Id,
                        Year = year,
                        Month = month,
                        DepreciationDate = new DateTime(year, month, DateTime.DaysInMonth(year, month)),
                        OpeningBookValue = asset.CurrentBookValue,
                        DepreciationAmount = monthlyDepreciation,
                        AccumulatedDepreciation = asset.AccumulatedDepreciation + monthlyDepreciation,
                        ClosingBookValue = asset.CurrentBookValue - monthlyDepreciation,
                        AppliedRate = asset.AnnualDepreciationRate,
                        DaysInUse = DateTime.DaysInMonth(year, month),
                        DailyDepreciation = monthlyDepreciation / DateTime.DaysInMonth(year, month),
                        IsAutoCalculated = true,
                        CalculatedBy = AuthenticationService.CurrentUsername,
                        CreatedBy = AuthenticationService.CurrentUsername,
                        CreatedAt = DateTime.Now
                    };

                    depreciations.Add(depreciation);
                }

                // عرض النتائج (يمكن إضافة Grid هنا)
                MessageBox.Show(
                    $"تم حساب الإهلاك لـ {depreciations.Count} أصل\nإجمالي الإهلاك: {depreciations.Sum(d => d.DepreciationAmount):N2} ريال",
                    "نجح",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Information
                );

                _saveButton.Enabled = true;
                LoggingService.LogInfo($"Depreciation calculated for {depreciations.Count} assets");
            }
            catch (Exception ex)
            {
                LoggingService.LogError(ex, "Error calculating depreciation");
                MessageBox.Show($"حدث خطأ: {ex.Message}", "خطأ", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private async Task SaveButton_ClickAsync()
        {
            try
            {
                var month = _monthComboBox.SelectedIndex + 1;
                var year = int.Parse(_yearComboBox.Text);

                // التحقق من عدم وجود إهلاك محفوظ لنفس الفترة
                var existingCount = await _context.AssetDepreciations
                    .CountAsync(d => d.Year == year && d.Month == month);

                if (existingCount > 0)
                {
                    var overwriteResult = MessageBox.Show(
                        $"يوجد {existingCount} سجل إهلاك محفوظ مسبقاً لـ {month}/{year}\n\nهل تريد الحذف والاستبدال؟",
                        "تحذير",
                        MessageBoxButtons.YesNo,
                        MessageBoxIcon.Warning
                    );

                    if (overwriteResult != DialogResult.Yes)
                        return;

                    // حذف السجلات القديمة
                    var existingDepreciations = await _context.AssetDepreciations
                        .Where(d => d.Year == year && d.Month == month)
                        .ToListAsync();
                    
                    _context.AssetDepreciations.RemoveRange(existingDepreciations);
                }

                // الحصول على الأصول وحساب الإهلاك
                var assets = await _context.FixedAssets
                    .Where(a => a.Status == AssetStatus.Active)
                    .ToListAsync();

                foreach (var asset in assets)
                {
                    var monthlyDepreciation = asset.CalculateAnnualDepreciation() / 12;
                    
                    var depreciation = new AssetDepreciation
                    {
                        FixedAssetId = asset.Id,
                        Year = year,
                        Month = month,
                        DepreciationDate = new DateTime(year, month, DateTime.DaysInMonth(year, month)),
                        OpeningBookValue = asset.CurrentBookValue,
                        DepreciationAmount = monthlyDepreciation,
                        AccumulatedDepreciation = asset.AccumulatedDepreciation + monthlyDepreciation,
                        ClosingBookValue = asset.CurrentBookValue - monthlyDepreciation,
                        AppliedRate = asset.AnnualDepreciationRate,
                        DaysInUse = DateTime.DaysInMonth(year, month),
                        DailyDepreciation = monthlyDepreciation / DateTime.DaysInMonth(year, month),
                        IsAutoCalculated = true,
                        CalculatedBy = AuthenticationService.CurrentUsername,
                        CreatedBy = AuthenticationService.CurrentUsername,
                        CreatedAt = DateTime.Now
                    };

                    _context.AssetDepreciations.Add(depreciation);
                    
                    // تحديث الأصل
                    asset.AccumulatedDepreciation += monthlyDepreciation;
                    asset.BookValue = asset.CurrentBookValue;
                }

                await _context.SaveChangesAsync();

                LoggingService.LogInfo($"Depreciation saved for {assets.Count} assets - {month}/{year}");
                MessageBox.Show(
                    $"تم حفظ الإهلاك بنجاح\nعدد الأصول: {assets.Count}\nالفترة: {month}/{year}",
                    "نجح",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Information
                );

                _saveButton.Enabled = false;
            }
            catch (Exception ex)
            {
                LoggingService.LogError(ex, "Error saving depreciation");
                MessageBox.Show($"حدث خطأ: {ex.Message}", "خطأ", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing) { }
            base.Dispose(disposing);
        }
    }
}

