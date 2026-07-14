using FishFarmManager.Data;
using FishFarmManager.Models;
using FishFarmManager.Services;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Windows.Forms;

namespace FishFarmManager.Forms
{
    /// <summary>
    /// نموذج إدارة المستخدمين - لإضافة وتعديل وحذف المستخدمين
    /// </summary>
    public partial class UserManagementForm : Form
    {
        private readonly FishFarmContext _context;
        private readonly AuthenticationService _authService;
        private DataGridView _usersGridView = null!;
        private ComboBox _roleFilterComboBox = null!;
        private ComboBox _statusFilterComboBox = null!;
        private TextBox _searchTextBox = null!;
        private Button _addButton = null!;
        private Button _editButton = null!;
        private Button _deleteButton = null!;
        private Button _resetPasswordButton = null!;
        private Button _toggleStatusButton = null!;
        private Button _refreshButton = null!;
        private Label _statusLabel = null!;

        public UserManagementForm(FishFarmContext context, AuthenticationService authService)
        {
            if (context == null)
                throw new ArgumentNullException(nameof(context));
            if (authService == null)
                throw new ArgumentNullException(nameof(authService));

            _context = context;
            _authService = authService;
            InitializeComponent();
            InitializeCustomComponents();
            LoadUsers();

            LoggingService.LogInfo("فتح نموذج إدارة المستخدمين - المدير: {User}",
                AuthenticationService.CurrentUsername);
        }

        private void InitializeComponent()
        {
            this.Text = "إدارة المستخدمين";
            this.Size = new Size(1200, 700);
            this.StartPosition = FormStartPosition.CenterScreen;
            this.RightToLeft = RightToLeft.Yes;
            this.RightToLeftLayout = true;
            this.Font = new Font("Segoe UI", 10F);
            this.BackColor = Color.FromArgb(240, 240, 240);
        }

        private void InitializeCustomComponents()
        {
            // ============================================
            // Panel العلوي - البحث والفلاتر
            // ============================================
            var topPanel = new Panel
            {
                Dock = DockStyle.Top,
                Height = 80,
                Padding = new Padding(10),
                BackColor = Color.White
            };

            // Label العنوان
            var titleLabel = new Label
            {
                Text = "👥 إدارة المستخدمين",
                Font = new Font("Segoe UI", 14F, FontStyle.Bold),
                Location = new Point(10, 10),
                AutoSize = true,
                ForeColor = Color.FromArgb(41, 128, 185)
            };
            topPanel.Controls.Add(titleLabel);

            // البحث
            var searchLabel = new Label
            {
                Text = "🔍 بحث:",
                Location = new Point(10, 45),
                AutoSize = true
            };
            topPanel.Controls.Add(searchLabel);

            _searchTextBox = new TextBox
            {
                Location = new Point(80, 43),
                Width = 250,
                Font = new Font("Segoe UI", 10F)
            };
            _searchTextBox.TextChanged += SearchTextBox_TextChanged;
            topPanel.Controls.Add(_searchTextBox);

            // فلتر الدور
            var roleLabel = new Label
            {
                Text = "الدور:",
                Location = new Point(350, 45),
                AutoSize = true
            };
            topPanel.Controls.Add(roleLabel);

            _roleFilterComboBox = new ComboBox
            {
                Location = new Point(410, 43),
                Width = 150,
                DropDownStyle = ComboBoxStyle.DropDownList
            };
            _roleFilterComboBox.Items.AddRange(new object[]
            {
                "الكل",
                "مدير النظام",
                "مدير",
                "محاسب",
                "موظف إنتاج",
                "موظف مبيعات",
                "موظف مخزون",
                "مشرف جودة",
                "موظف صيانة",
                "موظف موارد بشرية",
                "مشاهد فقط"
            });
            _roleFilterComboBox.SelectedIndex = 0;
            _roleFilterComboBox.SelectedIndexChanged += FilterComboBox_SelectedIndexChanged;
            topPanel.Controls.Add(_roleFilterComboBox);

            // فلتر الحالة
            var statusLabel = new Label
            {
                Text = "الحالة:",
                Location = new Point(580, 45),
                AutoSize = true
            };
            topPanel.Controls.Add(statusLabel);

            _statusFilterComboBox = new ComboBox
            {
                Location = new Point(640, 43),
                Width = 120,
                DropDownStyle = ComboBoxStyle.DropDownList
            };
            _statusFilterComboBox.Items.AddRange(new object[] { "الكل", "نشط", "غير نشط" });
            _statusFilterComboBox.SelectedIndex = 0;
            _statusFilterComboBox.SelectedIndexChanged += FilterComboBox_SelectedIndexChanged;
            topPanel.Controls.Add(_statusFilterComboBox);

            // زر التحديث
            _refreshButton = new Button
            {
                Text = "🔄 تحديث",
                Location = new Point(780, 40),
                Width = 100,
                Height = 30,
                BackColor = ThemeManager.SecondarySkyBlue,
                ForeColor = Color.White,
                FlatStyle = FlatStyle.Flat,
                Cursor = Cursors.Hand
            };
            _refreshButton.FlatAppearance.BorderSize = 0;
            _refreshButton.Click += RefreshButton_Click;
            topPanel.Controls.Add(_refreshButton);

            this.Controls.Add(topPanel);

            // ============================================
            // DataGridView - عرض المستخدمين
            // ============================================
            _usersGridView = new DataGridView
            {
                Dock = DockStyle.Fill,
                AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill,
                SelectionMode = DataGridViewSelectionMode.FullRowSelect,
                MultiSelect = false,
                AllowUserToAddRows = false,
                AllowUserToDeleteRows = false,
                ReadOnly = true,
                RowHeadersVisible = false,
                BackgroundColor = Color.White,
                BorderStyle = BorderStyle.None,
                ColumnHeadersHeight = 40,
                RowTemplate = { Height = 35 },
                Font = new Font("Segoe UI", 9.5F)
            };

            // تنسيق رأس الأعمدة
            _usersGridView.ColumnHeadersDefaultCellStyle.BackColor = Color.FromArgb(52, 73, 94);
            _usersGridView.ColumnHeadersDefaultCellStyle.ForeColor = Color.White;
            _usersGridView.ColumnHeadersDefaultCellStyle.Font = new Font("Segoe UI", 10F, FontStyle.Bold);
            _usersGridView.ColumnHeadersDefaultCellStyle.Alignment = DataGridViewContentAlignment.MiddleCenter;

            // تنسيق الصفوف
            _usersGridView.AlternatingRowsDefaultCellStyle.BackColor = Color.FromArgb(236, 240, 241);
            _usersGridView.DefaultCellStyle.SelectionBackColor = Color.FromArgb(41, 128, 185);
            _usersGridView.DefaultCellStyle.SelectionForeColor = Color.White;

            _usersGridView.SelectionChanged += UsersGridView_SelectionChanged;

            this.Controls.Add(_usersGridView);

            // ============================================
            // Panel السفلي - الأزرار
            // ============================================
            var bottomPanel = new Panel
            {
                Dock = DockStyle.Bottom,
                Height = 70,
                Padding = new Padding(10),
                BackColor = Color.White
            };

            int buttonWidth = 140;
            int buttonHeight = 40;
            int buttonSpacing = 10;
            int startX = 10;

            _addButton = CreateButton("➕ إضافة مستخدم", startX, 15, buttonWidth, buttonHeight, ThemeManager.SuccessGreen);
            _addButton.Click += AddButton_Click;
            bottomPanel.Controls.Add(_addButton);

            _editButton = CreateButton("✏️ تعديل", startX + (buttonWidth + buttonSpacing), 15, buttonWidth, buttonHeight, ThemeManager.SecondarySkyBlue);
            _editButton.Click += EditButton_Click;
            _editButton.Enabled = false;
            bottomPanel.Controls.Add(_editButton);

            _resetPasswordButton = CreateButton("🔑 إعادة تعيين كلمة المرور", startX + 2 * (buttonWidth + buttonSpacing), 15, buttonWidth + 60, buttonHeight, Color.FromArgb(243, 156, 18));
            _resetPasswordButton.Click += ResetPasswordButton_Click;
            _resetPasswordButton.Enabled = false;
            bottomPanel.Controls.Add(_resetPasswordButton);

            _toggleStatusButton = CreateButton("🔄 تفعيل/إلغاء تفعيل", startX + 3 * (buttonWidth + buttonSpacing) + 60, 15, buttonWidth + 40, buttonHeight, ThemeManager.SecondarySkyBlue);
            _toggleStatusButton.Click += ToggleStatusButton_Click;
            _toggleStatusButton.Enabled = false;
            bottomPanel.Controls.Add(_toggleStatusButton);

            _deleteButton = CreateButton("🗑️ حذف", startX + 4 * (buttonWidth + buttonSpacing) + 100, 15, buttonWidth, buttonHeight, ThemeManager.ErrorRed);
            _deleteButton.Click += DeleteButton_Click;
            _deleteButton.Enabled = false;
            bottomPanel.Controls.Add(_deleteButton);

            // Status Label
            _statusLabel = new Label
            {
                Location = new Point(startX + 5 * (buttonWidth + buttonSpacing) + 240, 25),
                AutoSize = true,
                Font = new Font("Segoe UI", 9F, FontStyle.Italic),
                ForeColor = Color.Gray
            };
            bottomPanel.Controls.Add(_statusLabel);

            this.Controls.Add(bottomPanel);
        }

        private Button CreateButton(string text, int x, int y, int width, int height, Color backColor)
        {
            var button = new Button
            {
                Text = text,
                Location = new Point(x, y),
                Width = width,
                Height = height,
                BackColor = backColor,
                ForeColor = Color.White,
                FlatStyle = FlatStyle.Flat,
                Font = new Font("Segoe UI", 9.5F, FontStyle.Bold),
                Cursor = Cursors.Hand
            };
            button.FlatAppearance.BorderSize = 0;
            return button;
        }

        private void LoadUsers(string? searchText = null, string? roleFilter = null, string? statusFilter = null)
        {
            try
            {
                if (_context == null)
                {
                    MessageBox.Show("خطأ في تهيئة قاعدة البيانات", "خطأ", MessageBoxButtons.OK, MessageBoxIcon.Error);
                    return;
                }

                var stopwatch = System.Diagnostics.Stopwatch.StartNew();

                // التحقق من وجود جدول Users
                if (!_context.Database.CanConnect())
                {
                    MessageBox.Show("لا يمكن الاتصال بقاعدة البيانات", "خطأ", MessageBoxButtons.OK, MessageBoxIcon.Error);
                    return;
                }

                var query = _context.Users.AsQueryable();

                // تطبيق البحث
                if (!string.IsNullOrWhiteSpace(searchText))
                {
                    query = query.Where(u =>
                        u.Username.Contains(searchText) ||
                        u.FullName.Contains(searchText) ||
                        u.Email.Contains(searchText));
                }

                // تطبيق فلتر الدور
                if (!string.IsNullOrEmpty(roleFilter) && roleFilter != "الكل")
                {
                    UserRole? targetRole = roleFilter switch
                    {
                        "مدير النظام" => UserRole.Admin,
                        "مدير" => UserRole.Manager,
                        "محاسب" => UserRole.Accountant,
                        "موظف إنتاج" => UserRole.ProductionStaff,
                        "موظف مبيعات" => UserRole.SalesStaff,
                        "موظف مخزون" => UserRole.InventoryStaff,
                        "مشرف جودة" => UserRole.QualityControl,
                        "موظف صيانة" => UserRole.MaintenanceStaff,
                        "موظف موارد بشرية" => UserRole.HRStaff,
                        "مشاهد فقط" => UserRole.Viewer,
                        _ => null
                    };
                    
                    if (targetRole.HasValue)
                        query = query.Where(u => u.Role == targetRole.Value);
                }

                // تطبيق فلتر الحالة
                if (!string.IsNullOrEmpty(statusFilter))
                {
                    if (statusFilter == "نشط")
                        query = query.Where(u => u.IsActive);
                    else if (statusFilter == "غير نشط")
                        query = query.Where(u => !u.IsActive);
                }

                var users = query.OrderBy(u => u.Username).ToList();

                if (!users.Any())
                {
                    LoggingService.LogWarning("No users matched the selected filters.");
                }

                _usersGridView.DataSource = users;
                ConfigureGridColumns();

                stopwatch.Stop();
                LoggingService.LogPerformance("تحميل المستخدمين", stopwatch.Elapsed, 
                    $"عدد المستخدمين: {users.Count}");

                _statusLabel.Text = $"📊 إجمالي المستخدمين: {users.Count}";
            }
            catch (Exception ex)
            {
                LoggingService.LogError(ex, "خطأ في تحميل المستخدمين");
                MessageBox.Show($"حدث خطأ أثناء تحميل المستخدمين:\n{ex.Message}",
                    "خطأ", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void ConfigureGridColumns()
        {
            if (_usersGridView?.Columns == null || _usersGridView.Columns.Count == 0) return;

            try
            {
                // إخفاء الأعمدة غير الضرورية
                if (_usersGridView.Columns.Contains("UserId"))
                    _usersGridView.Columns["UserId"]!.Visible = false;
                
                if (_usersGridView.Columns.Contains("PasswordHash"))
                    _usersGridView.Columns["PasswordHash"]!.Visible = false;

                // تخصيص رؤوس الأعمدة
                if (_usersGridView.Columns.Contains("Username"))
                {
                    var col = _usersGridView.Columns["Username"];
                    if (col != null)
                    {
                        col.HeaderText = "اسم المستخدم";
                        col.AutoSizeMode = DataGridViewAutoSizeColumnMode.None;
                        col.Width = 150;
                    }
                }

                if (_usersGridView.Columns.Contains("FullName"))
                {
                    var col = _usersGridView.Columns["FullName"];
                    if (col != null)
                    {
                        col.HeaderText = "الاسم الكامل";
                        col.AutoSizeMode = DataGridViewAutoSizeColumnMode.None;
                        col.Width = 200;
                    }
                }

                if (_usersGridView.Columns.Contains("Email"))
                {
                    var col = _usersGridView.Columns["Email"];
                    if (col != null)
                    {
                        col.HeaderText = "البريد الإلكتروني";
                        col.AutoSizeMode = DataGridViewAutoSizeColumnMode.None;
                        col.Width = 200;
                    }
                }

                if (_usersGridView.Columns.Contains("Role"))
                {
                    var col = _usersGridView.Columns["Role"];
                    if (col != null)
                    {
                        col.HeaderText = "الدور";
                        col.AutoSizeMode = DataGridViewAutoSizeColumnMode.None;
                        col.Width = 150;
                    }
                }

                if (_usersGridView.Columns.Contains("IsActive"))
                {
                    var col = _usersGridView.Columns["IsActive"];
                    if (col != null)
                    {
                        col.HeaderText = "نشط";
                        col.AutoSizeMode = DataGridViewAutoSizeColumnMode.None;
                        col.Width = 80;
                    }
                }

                if (_usersGridView.Columns.Contains("LastLogin"))
                {
                    var col = _usersGridView.Columns["LastLogin"];
                    if (col != null)
                    {
                        col.HeaderText = "آخر دخول";
                        col.AutoSizeMode = DataGridViewAutoSizeColumnMode.None;
                        col.Width = 150;
                        col.DefaultCellStyle.Format = "dd/MM/yyyy HH:mm";
                    }
                }
            }
            catch (Exception ex)
            {
                LoggingService.LogError(ex, "خطأ في تكوين أعمدة الجدول");
                // لا نريد إيقاف التطبيق بسبب مشكلة في عرض الأعمدة
            }

                if (_usersGridView.Columns.Contains("CreatedAt"))
                {
                    var col = _usersGridView.Columns["CreatedAt"];
                    if (col != null)
                    {
                        col.HeaderText = "تاريخ الإنشاء";
                        col.AutoSizeMode = DataGridViewAutoSizeColumnMode.None;
                        col.Width = 150;
                        col.DefaultCellStyle.Format = "dd/MM/yyyy HH:mm";
                    }
                }
        }

        // ============================================
        // Event Handlers
        // ============================================

        private void SearchTextBox_TextChanged(object? sender, EventArgs e)
        {
            LoadUsers(_searchTextBox.Text,
                _roleFilterComboBox.SelectedItem?.ToString(),
                _statusFilterComboBox.SelectedItem?.ToString());
        }

        private void FilterComboBox_SelectedIndexChanged(object? sender, EventArgs e)
        {
            LoadUsers(_searchTextBox.Text,
                _roleFilterComboBox.SelectedItem?.ToString(),
                _statusFilterComboBox.SelectedItem?.ToString());
        }

        private void RefreshButton_Click(object? sender, EventArgs e)
        {
            _searchTextBox.Clear();
            _roleFilterComboBox.SelectedIndex = 0;
            _statusFilterComboBox.SelectedIndex = 0;
            LoadUsers();

            LoggingService.LogUserActivity(AuthenticationService.CurrentUsername,
                "تحديث قائمة المستخدمين", "");
        }

        private void UsersGridView_SelectionChanged(object? sender, EventArgs e)
        {
            bool hasSelection = _usersGridView.SelectedRows.Count > 0;
            _editButton.Enabled = hasSelection;
            _resetPasswordButton.Enabled = hasSelection;
            _toggleStatusButton.Enabled = hasSelection;
            _deleteButton.Enabled = hasSelection;
        }

        private void AddButton_Click(object? sender, EventArgs e)
        {
            try
            {
                LoggingService.LogUserActivity(AuthenticationService.CurrentUsername,
                    "فتح نموذج إضافة مستخدم", "");

                var addEditForm = new AddEditUserForm(_context);
                if (addEditForm.ShowDialog() == DialogResult.OK)
                {
                    LoadUsers();
                    MessageBox.Show("تم إضافة المستخدم بنجاح!", "نجاح",
                        MessageBoxButtons.OK, MessageBoxIcon.Information);
                }
            }
            catch (Exception ex)
            {
                LoggingService.LogError(ex, "خطأ في فتح نموذج إضافة مستخدم");
                MessageBox.Show($"حدث خطأ: {ex.Message}", "خطأ",
                    MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void EditButton_Click(object? sender, EventArgs e)
        {
            if (_usersGridView.SelectedRows.Count == 0) return;

            try
            {
                var selectedUser = (User)_usersGridView.SelectedRows[0].DataBoundItem;

                LoggingService.LogUserActivity(AuthenticationService.CurrentUsername,
                    "فتح نموذج تعديل مستخدم", $"المستخدم: {selectedUser.Username}");

                var addEditForm = new AddEditUserForm(_context, selectedUser.UserId);
                if (addEditForm.ShowDialog() == DialogResult.OK)
                {
                    LoadUsers();
                    MessageBox.Show("تم تحديث المستخدم بنجاح!", "نجاح",
                        MessageBoxButtons.OK, MessageBoxIcon.Information);
                }
            }
            catch (Exception ex)
            {
                LoggingService.LogError(ex, "خطأ في فتح نموذج تعديل مستخدم");
                MessageBox.Show($"حدث خطأ: {ex.Message}", "خطأ",
                    MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void ResetPasswordButton_Click(object? sender, EventArgs e)
        {
            if (_usersGridView.SelectedRows.Count == 0) return;

            try
            {
                var selectedUser = (User)_usersGridView.SelectedRows[0].DataBoundItem;

                var result = MessageBox.Show(
                    $"هل تريد إعادة تعيين كلمة المرور للمستخدم '{selectedUser.Username}'?\n\n" +
                    "سيتم توليد كلمة مرور مؤقتة قوية. يجب تسليمها للمستخدم عبر قناة آمنة.",
                    "تأكيد إعادة التعيين",
                    MessageBoxButtons.YesNo,
                    MessageBoxIcon.Question);

                if (result == DialogResult.Yes)
                {
                    var temporaryPassword = AuthenticationService.GenerateSecurePassword(14);
                    bool success = _authService.ResetPassword(selectedUser.UserId, temporaryPassword);

                    if (success)
                    {
                        LoggingService.LogUserActivity(
                            AuthenticationService.CurrentUsername,
                            "إعادة تعيين كلمة المرور",
                            $"المستخدم المستهدف: {selectedUser.Username}"
                        );

                        MessageBox.Show(
                            $"تم إعادة تعيين كلمة المرور بنجاح!\n\n" +
                            $"كلمة المرور المؤقتة: {temporaryPassword}",
                            "نجاح",
                            MessageBoxButtons.OK,
                            MessageBoxIcon.Information);
                    }
                    else
                    {
                        MessageBox.Show("فشلت عملية إعادة التعيين", "خطأ",
                            MessageBoxButtons.OK, MessageBoxIcon.Error);
                    }
                }
            }
            catch (Exception ex)
            {
                LoggingService.LogError(ex, "خطأ في إعادة تعيين كلمة المرور");
                MessageBox.Show($"حدث خطأ: {ex.Message}", "خطأ",
                    MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void ToggleStatusButton_Click(object? sender, EventArgs e)
        {
            if (_usersGridView.SelectedRows.Count == 0) return;

            try
            {
                var selectedUser = (User)_usersGridView.SelectedRows[0].DataBoundItem;

                // منع المستخدم من تعطيل نفسه
                if (selectedUser.Username == AuthenticationService.CurrentUsername)
                {
                    MessageBox.Show("لا يمكنك تعطيل حسابك الخاص!", "تحذير",
                        MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    return;
                }

                string action = selectedUser.IsActive ? "إلغاء تفعيل" : "تفعيل";
                var result = MessageBox.Show(
                    $"هل تريد {action} المستخدم '{selectedUser.Username}'?",
                    $"تأكيد {action}",
                    MessageBoxButtons.YesNo,
                    MessageBoxIcon.Question);

                if (result == DialogResult.Yes)
                {
                    bool newStatus = !selectedUser.IsActive;
                    bool success = _authService.SetUserActiveStatus(selectedUser.UserId, newStatus);

                    if (success)
                    {
                        LoggingService.LogUserActivity(
                            AuthenticationService.CurrentUsername,
                            action + " مستخدم",
                            $"المستخدم المستهدف: {selectedUser.Username}"
                        );

                        LoadUsers();
                        MessageBox.Show($"تم {action} المستخدم بنجاح!", "نجاح",
                            MessageBoxButtons.OK, MessageBoxIcon.Information);
                    }
                    else
                    {
                        MessageBox.Show($"فشلت عملية {action}", "خطأ",
                            MessageBoxButtons.OK, MessageBoxIcon.Error);
                    }
                }
            }
            catch (Exception ex)
            {
                LoggingService.LogError(ex, "خطأ في تغيير حالة المستخدم");
                MessageBox.Show($"حدث خطأ: {ex.Message}", "خطأ",
                    MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void DeleteButton_Click(object? sender, EventArgs e)
        {
            if (_usersGridView.SelectedRows.Count == 0) return;

            try
            {
                var selectedUser = (User)_usersGridView.SelectedRows[0].DataBoundItem;

                // منع المستخدم من حذف نفسه
                if (selectedUser.Username == AuthenticationService.CurrentUsername)
                {
                    MessageBox.Show("لا يمكنك حذف حسابك الخاص!", "تحذير",
                        MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    return;
                }

                // منع حذف المستخدم admin
                if (selectedUser.Username == "admin")
                {
                    MessageBox.Show("لا يمكن حذف حساب المدير الرئيسي!", "تحذير",
                        MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    return;
                }

                var result = MessageBox.Show(
                    $"هل أنت متأكد من حذف المستخدم '{selectedUser.Username}'؟\n\n" +
                    "⚠️ تحذير: هذا الإجراء لا يمكن التراجع عنه!",
                    "تأكيد الحذف",
                    MessageBoxButtons.YesNo,
                    MessageBoxIcon.Warning);

                if (result == DialogResult.Yes)
                {
                    _context.Users.Remove(selectedUser);
                    _context.SaveChanges();

                    LoggingService.LogUserActivity(
                        AuthenticationService.CurrentUsername,
                        "حذف مستخدم",
                        $"المستخدم المحذوف: {selectedUser.Username}"
                    );

                    LoadUsers();
                    MessageBox.Show("تم حذف المستخدم بنجاح!", "نجاح",
                        MessageBoxButtons.OK, MessageBoxIcon.Information);
                }
            }
            catch (Exception ex)
            {
                LoggingService.LogError(ex, "خطأ في حذف مستخدم");
                MessageBox.Show($"حدث خطأ أثناء الحذف:\n{ex.Message}", "خطأ",
                    MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                _usersGridView?.Dispose();
                _roleFilterComboBox?.Dispose();
                _statusFilterComboBox?.Dispose();
                _searchTextBox?.Dispose();
                _addButton?.Dispose();
                _editButton?.Dispose();
                _deleteButton?.Dispose();
                _resetPasswordButton?.Dispose();
                _toggleStatusButton?.Dispose();
                _refreshButton?.Dispose();
                _statusLabel?.Dispose();
            }
            base.Dispose(disposing);
        }
    }
}
