using FishFarmManager.Data;
using FishFarmManager.Models;
using FishFarmManager.Services;
using Microsoft.EntityFrameworkCore;
using System;
using System.Drawing;
using System.Linq;
using System.Windows.Forms;

namespace FishFarmManager.Forms
{
    /// <summary>
    /// نموذج إضافة أو تعديل مستخدم
    /// </summary>
    public partial class AddEditUserForm : Form
    {
        private readonly FishFarmContext _context;
        private readonly AuthenticationService _authenticationService;
        private readonly int? _userId;
        private User? _existingUser;

        private TextBox _usernameTextBox = null!;
        private TextBox _fullNameTextBox = null!;
        private TextBox _emailTextBox = null!;
        private TextBox _passwordTextBox = null!;
        private TextBox _confirmPasswordTextBox = null!;
        private ComboBox _roleComboBox = null!;
        private CheckBox _isActiveCheckBox = null!;
        private Button _saveButton = null!;
        private Button _cancelButton = null!;
        private Label _passwordHintLabel = null!;

        public AddEditUserForm(FishFarmContext context, int? userId = null)
        {
            _context = context;
            _authenticationService = new AuthenticationService(context);
            _userId = userId;
            
            InitializeComponent();
            InitializeCustomComponents();
            
            if (_userId.HasValue)
            {
                LoadUserData();
            }
        }

        private void InitializeComponent()
        {
            this.Text = _userId.HasValue ? "تعديل مستخدم" : "إضافة مستخدم جديد";
            this.Size = new Size(600, 550);
            this.StartPosition = FormStartPosition.CenterParent;
            this.FormBorderStyle = FormBorderStyle.FixedDialog;
            this.MaximizeBox = false;
            this.MinimizeBox = false;
            this.RightToLeft = RightToLeft.Yes;
            this.RightToLeftLayout = true;
            this.Font = new Font("Segoe UI", 10F);
            this.BackColor = Color.FromArgb(240, 240, 240);
        }

        private void InitializeCustomComponents()
        {
            // Panel الرئيسي
            var mainPanel = new Panel
            {
                Dock = DockStyle.Fill,
                Padding = new Padding(20),
                BackColor = Color.White
            };

            int labelX = 400;
            int inputX = 50;
            int inputWidth = 330;
            int currentY = 20;
            int spacing = 60;

            // العنوان
            var titleLabel = new Label
            {
                Text = _userId.HasValue ? "✏️ تعديل بيانات المستخدم" : "➕ إضافة مستخدم جديد",
                Font = new Font("Segoe UI", 14F, FontStyle.Bold),
                Location = new Point(labelX - 100, currentY),
                AutoSize = true,
                ForeColor = Color.FromArgb(41, 128, 185)
            };
            mainPanel.Controls.Add(titleLabel);
            currentY += 50;

            // اسم المستخدم
            var usernameLabel = new Label
            {
                Text = "اسم المستخدم: *",
                Location = new Point(labelX, currentY + 5),
                AutoSize = true,
                Font = new Font("Segoe UI", 10F, FontStyle.Bold)
            };
            mainPanel.Controls.Add(usernameLabel);

            _usernameTextBox = new TextBox
            {
                Location = new Point(inputX, currentY),
                Width = inputWidth,
                Font = new Font("Segoe UI", 10F)
            };
            _usernameTextBox.ReadOnly = _userId.HasValue; // لا يمكن تعديل اسم المستخدم
            mainPanel.Controls.Add(_usernameTextBox);
            currentY += spacing;

            // الاسم الكامل
            var fullNameLabel = new Label
            {
                Text = "الاسم الكامل: *",
                Location = new Point(labelX, currentY + 5),
                AutoSize = true,
                Font = new Font("Segoe UI", 10F, FontStyle.Bold)
            };
            mainPanel.Controls.Add(fullNameLabel);

            _fullNameTextBox = new TextBox
            {
                Location = new Point(inputX, currentY),
                Width = inputWidth,
                Font = new Font("Segoe UI", 10F)
            };
            mainPanel.Controls.Add(_fullNameTextBox);
            currentY += spacing;

            // البريد الإلكتروني
            var emailLabel = new Label
            {
                Text = "البريد الإلكتروني:",
                Location = new Point(labelX, currentY + 5),
                AutoSize = true,
                Font = new Font("Segoe UI", 10F, FontStyle.Bold)
            };
            mainPanel.Controls.Add(emailLabel);

            _emailTextBox = new TextBox
            {
                Location = new Point(inputX, currentY),
                Width = inputWidth,
                Font = new Font("Segoe UI", 10F),
                PlaceholderText = "example@domain.com"
            };
            mainPanel.Controls.Add(_emailTextBox);
            currentY += spacing;

            // الدور
            var roleLabel = new Label
            {
                Text = "الدور: *",
                Location = new Point(labelX, currentY + 5),
                AutoSize = true,
                Font = new Font("Segoe UI", 10F, FontStyle.Bold)
            };
            mainPanel.Controls.Add(roleLabel);

            _roleComboBox = new ComboBox
            {
                Location = new Point(inputX, currentY),
                Width = inputWidth,
                DropDownStyle = ComboBoxStyle.DropDownList,
                Font = new Font("Segoe UI", 10F)
            };
            _roleComboBox.Items.AddRange(new object[]
            {
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
            _roleComboBox.SelectedIndex = 4; // Default: موظف مبيعات
            mainPanel.Controls.Add(_roleComboBox);
            currentY += spacing;

            // كلمة المرور (للإضافة فقط)
            if (!_userId.HasValue)
            {
                var passwordLabel = new Label
                {
                    Text = "كلمة المرور: *",
                    Location = new Point(labelX, currentY + 5),
                    AutoSize = true,
                    Font = new Font("Segoe UI", 10F, FontStyle.Bold)
                };
                mainPanel.Controls.Add(passwordLabel);

                _passwordTextBox = new TextBox
                {
                    Location = new Point(inputX, currentY),
                    Width = inputWidth,
                    Font = new Font("Segoe UI", 10F),
                    UseSystemPasswordChar = true
                };
                mainPanel.Controls.Add(_passwordTextBox);
                currentY += spacing;

                // تأكيد كلمة المرور
                var confirmPasswordLabel = new Label
                {
                    Text = "تأكيد كلمة المرور: *",
                    Location = new Point(labelX, currentY + 5),
                    AutoSize = true,
                    Font = new Font("Segoe UI", 10F, FontStyle.Bold)
                };
                mainPanel.Controls.Add(confirmPasswordLabel);

                _confirmPasswordTextBox = new TextBox
                {
                    Location = new Point(inputX, currentY),
                    Width = inputWidth,
                    Font = new Font("Segoe UI", 10F),
                    UseSystemPasswordChar = true
                };
                mainPanel.Controls.Add(_confirmPasswordTextBox);
                currentY += spacing;
            }
            else
            {
                _passwordHintLabel = new Label
                {
                    Text = "💡 لتغيير كلمة المرور، استخدم زر 'إعادة تعيين كلمة المرور'",
                    Location = new Point(inputX, currentY),
                    Width = inputWidth + 100,
                    Font = new Font("Segoe UI", 9F, FontStyle.Italic),
                    ForeColor = Color.Gray
                };
                mainPanel.Controls.Add(_passwordHintLabel);
                currentY += 40;
            }

            // الحالة (نشط)
            _isActiveCheckBox = new CheckBox
            {
                Text = "الحساب نشط",
                Location = new Point(labelX - 50, currentY),
                AutoSize = true,
                Checked = true,
                Font = new Font("Segoe UI", 10F, FontStyle.Bold)
            };
            mainPanel.Controls.Add(_isActiveCheckBox);
            currentY += 50;

            // الأزرار
            var buttonPanel = new Panel
            {
                Location = new Point(inputX, currentY),
                Width = inputWidth + 100,
                Height = 50
            };

            _saveButton = new Button
            {
                Text = _userId.HasValue ? "💾 حفظ التغييرات" : "➕ إضافة المستخدم",
                Location = new Point(0, 0),
                Width = 200,
                Height = 40,
                BackColor = ThemeManager.SuccessGreen,
                ForeColor = Color.White,
                FlatStyle = FlatStyle.Flat,
                Font = new Font("Segoe UI", 10F, FontStyle.Bold),
                Cursor = Cursors.Hand
            };
            _saveButton.FlatAppearance.BorderSize = 0;
            _saveButton.Click += SaveButton_Click;
            buttonPanel.Controls.Add(_saveButton);

            _cancelButton = new Button
            {
                Text = "❌ إلغاء",
                Location = new Point(220, 0),
                Width = 150,
                Height = 40,
                BackColor = Color.FromArgb(149, 165, 166),
                ForeColor = Color.White,
                FlatStyle = FlatStyle.Flat,
                Font = new Font("Segoe UI", 10F, FontStyle.Bold),
                Cursor = Cursors.Hand
            };
            _cancelButton.FlatAppearance.BorderSize = 0;
            _cancelButton.Click += (s, e) => { this.DialogResult = DialogResult.Cancel; this.Close(); };
            buttonPanel.Controls.Add(_cancelButton);

            mainPanel.Controls.Add(buttonPanel);

            this.Controls.Add(mainPanel);
        }

        private void LoadUserData()
        {
            try
            {
                _existingUser = _context.Users.Find(_userId);
                
                if (_existingUser == null)
                {
                    MessageBox.Show("لم يتم العثور على المستخدم!", "خطأ",
                        MessageBoxButtons.OK, MessageBoxIcon.Error);
                    this.Close();
                    return;
                }

                _usernameTextBox.Text = _existingUser.Username;
                _fullNameTextBox.Text = _existingUser.FullName;
                _emailTextBox.Text = _existingUser.Email ?? "";
                _roleComboBox.SelectedItem = _existingUser.Role;
                _isActiveCheckBox.Checked = _existingUser.IsActive;
            }
            catch (Exception ex)
            {
                LoggingService.LogError(ex, "خطأ في تحميل بيانات المستخدم");
                MessageBox.Show($"حدث خطأ: {ex.Message}", "خطأ",
                    MessageBoxButtons.OK, MessageBoxIcon.Error);
                this.Close();
            }
        }

        private void SaveButton_Click(object? sender, EventArgs e)
        {
            try
            {
                // التحقق من صحة البيانات
                if (!ValidateInput())
                    return;

                if (_userId.HasValue)
                {
                    // تعديل مستخدم موجود
                    UpdateExistingUser();
                }
                else
                {
                    // إضافة مستخدم جديد
                    CreateNewUser();
                }

                this.DialogResult = DialogResult.OK;
                this.Close();
            }
            catch (Exception ex)
            {
                LoggingService.LogError(ex, "خطأ في حفظ المستخدم");
                MessageBox.Show($"حدث خطأ أثناء الحفظ:\n{ex.Message}", "خطأ",
                    MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private bool ValidateInput()
        {
            // التحقق من اسم المستخدم
            if (string.IsNullOrWhiteSpace(_usernameTextBox.Text))
            {
                MessageBox.Show("يرجى إدخال اسم المستخدم", "تحقق من البيانات",
                    MessageBoxButtons.OK, MessageBoxIcon.Warning);
                _usernameTextBox.Focus();
                return false;
            }

            // التحقق من عدم تكرار اسم المستخدم (عند الإضافة فقط)
            if (!_userId.HasValue)
            {
                var existingUser = _context.Users
                    .FirstOrDefault(u => u.Username == _usernameTextBox.Text.Trim());
                
                if (existingUser != null)
                {
                    MessageBox.Show("اسم المستخدم موجود بالفعل! اختر اسماً آخر", "تحذير",
                        MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    _usernameTextBox.Focus();
                    return false;
                }
            }

            // التحقق من الاسم الكامل
            if (string.IsNullOrWhiteSpace(_fullNameTextBox.Text))
            {
                MessageBox.Show("يرجى إدخال الاسم الكامل", "تحقق من البيانات",
                    MessageBoxButtons.OK, MessageBoxIcon.Warning);
                _fullNameTextBox.Focus();
                return false;
            }

            // التحقق من الدور
            if (_roleComboBox.SelectedItem == null)
            {
                MessageBox.Show("يرجى اختيار الدور", "تحقق من البيانات",
                    MessageBoxButtons.OK, MessageBoxIcon.Warning);
                _roleComboBox.Focus();
                return false;
            }

            // التحقق من كلمة المرور (عند الإضافة فقط)
            if (!_userId.HasValue)
            {
                if (string.IsNullOrWhiteSpace(_passwordTextBox.Text))
                {
                    MessageBox.Show("يرجى إدخال كلمة المرور", "تحقق من البيانات",
                        MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    _passwordTextBox.Focus();
                    return false;
                }

                var passwordValidation = AuthenticationService.ValidatePasswordComplexity(_passwordTextBox.Text);
                if (!passwordValidation.IsValid)
                {
                    MessageBox.Show(string.Join("\n", passwordValidation.Errors), "تحذير",
                        MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    _passwordTextBox.Focus();
                    return false;
                }

                if (_passwordTextBox.Text != _confirmPasswordTextBox.Text)
                {
                    MessageBox.Show("كلمة المرور وتأكيد كلمة المرور غير متطابقين", "تحذير",
                        MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    _confirmPasswordTextBox.Focus();
                    return false;
                }
            }

            // التحقق من البريد الإلكتروني (إذا تم إدخاله)
            if (!string.IsNullOrWhiteSpace(_emailTextBox.Text))
            {
                if (!_emailTextBox.Text.Contains("@") || !_emailTextBox.Text.Contains("."))
                {
                    MessageBox.Show("البريد الإلكتروني غير صحيح", "تحذير",
                        MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    _emailTextBox.Focus();
                    return false;
                }
            }

            return true;
        }

        private void CreateNewUser()
        {
            var selectedRole = _roleComboBox.SelectedItem?.ToString() ?? "موظف مبيعات";
            UserRole userRole = selectedRole switch
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
                _ => UserRole.SalesStaff
            };

            if (!_authenticationService.CreateUser(_usernameTextBox.Text.Trim(), _passwordTextBox.Text,
                    _fullNameTextBox.Text.Trim(), _emailTextBox.Text.Trim(), userRole))
                throw new InvalidOperationException("تعذر إنشاء المستخدم وفق سياسة الصلاحيات وكلمة المرور.");
        }

        private void UpdateExistingUser()
        {
            if (_existingUser == null) return;

            var selectedRole = _roleComboBox.SelectedItem?.ToString();
            if (!string.IsNullOrEmpty(selectedRole))
            {
                _existingUser.Role = selectedRole switch
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
                    _ => _existingUser.Role
                };
            }

            if (!_authenticationService.UpdateUserProfile(_existingUser.UserId, _fullNameTextBox.Text.Trim(),
                    _emailTextBox.Text.Trim(), _existingUser.Role, _isActiveCheckBox.Checked))
                throw new InvalidOperationException("تعذر تعديل المستخدم وفق سياسة الصلاحيات.");
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                _usernameTextBox?.Dispose();
                _fullNameTextBox?.Dispose();
                _emailTextBox?.Dispose();
                _passwordTextBox?.Dispose();
                _confirmPasswordTextBox?.Dispose();
                _roleComboBox?.Dispose();
                _isActiveCheckBox?.Dispose();
                _saveButton?.Dispose();
                _cancelButton?.Dispose();
                _passwordHintLabel?.Dispose();
            }
            base.Dispose(disposing);
        }
    }
}
