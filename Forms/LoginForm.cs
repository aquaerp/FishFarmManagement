using System;
using System.Drawing;
using System.Windows.Forms;
using FishFarmManager.Services;

namespace FishFarmManager.Forms
{
    /// <summary>
    /// نموذج تسجيل الدخول - Login Form
    /// شاشة المصادقة الأولية للمستخدمين
    /// </summary>
    public partial class LoginForm : Form
    {
        private readonly AuthenticationService _authService;
        
        private TextBox _usernameTextBox = null!;
        private TextBox _passwordTextBox = null!;
        private Button _loginButton = null!;
        private Button _exitButton = null!;
        private Label _titleLabel = null!;
        private Label _usernameLabel = null!;
        private Label _passwordLabel = null!;
        private Label _errorLabel = null!;
        private CheckBox _showPasswordCheckBox = null!;
        private PictureBox _logoPictureBox = null!;

        public bool LoginSuccessful { get; private set; } = false;

        public LoginForm(AuthenticationService authService)
        {
            _authService = authService;
            InitializeComponent();
            SetupForm();
            ApplyTheme();
            BindLocalizedResources();
            LocalizationManager.CultureChanged += LocalizationManager_CultureChanged;
            ApplyLocalization();
        }

        private void InitializeComponent()
        {
            this.SuspendLayout();
            
            // Form settings
            this.ClientSize = new Size(450, 550);
            this.Text = "AquaFarm Pro - تسجيل الدخول";
            this.FormBorderStyle = FormBorderStyle.FixedDialog;
            this.AutoScaleMode = AutoScaleMode.Dpi;
            this.StartPosition = FormStartPosition.CenterScreen;
            this.MaximizeBox = false;
            this.MinimizeBox = false;
            this.KeyPreview = true;

            this.ResumeLayout(false);
        }

        private void SetupForm()
        {
            // Logo area
            _logoPictureBox = new PictureBox
            {
                Location = new Point(150, 30),
                Size = new Size(150, 150),
                BorderStyle = BorderStyle.None,
                BackColor = Color.Transparent,
                SizeMode = PictureBoxSizeMode.Zoom
            };
            
            // محاولة تحميل الشعار
            try
            {
                if (System.IO.File.Exists("Assets/Images/Logo_Full.png"))
                {
                    _logoPictureBox.Image = Image.FromFile("Assets/Images/Logo_Full.png");
                }
                else
                {
                    // رسم شعار نصي مؤقت
                    _logoPictureBox.Paint += DrawTemporaryLogo;
                }
            }
            catch
            {
                // رسم شعار نصي مؤقت في حالة الخطأ
                _logoPictureBox.Paint += DrawTemporaryLogo;
            }
            
            this.Controls.Add(_logoPictureBox);

            // Title
            _titleLabel = new Label
            {
                Text = "AquaFarm Pro\nنظام إدارة المزارع السمكية",
                Location = new Point(50, 190),
                Size = new Size(350, 60),
                TextAlign = ContentAlignment.MiddleCenter,
                Font = new Font("Cairo", 14, FontStyle.Bold),
                ForeColor = ThemeManager.PrimaryDeepBlue // Deep Blue
            };
            this.Controls.Add(_titleLabel);

            // Username Label
            _usernameLabel = new Label
            {
                Text = "اسم المستخدم:",
                Location = new Point(50, 270),
                Size = new Size(350, 25),
                Font = new Font("Cairo", 10),
                TextAlign = ContentAlignment.MiddleRight
            };
            this.Controls.Add(_usernameLabel);

            // Username TextBox
            _usernameTextBox = new TextBox
            {
                Location = new Point(50, 300),
                Size = new Size(350, 30),
                Font = new Font("Cairo", 10),
                TextAlign = HorizontalAlignment.Right
            };
            this.Controls.Add(_usernameTextBox);

            // Password Label
            _passwordLabel = new Label
            {
                Text = "كلمة المرور:",
                Location = new Point(50, 340),
                Size = new Size(350, 25),
                Font = new Font("Cairo", 10),
                TextAlign = ContentAlignment.MiddleRight
            };
            this.Controls.Add(_passwordLabel);

            // Password TextBox
            _passwordTextBox = new TextBox
            {
                Location = new Point(50, 370),
                Size = new Size(350, 30),
                Font = new Font("Cairo", 10),
                UseSystemPasswordChar = true,
                TextAlign = HorizontalAlignment.Right
            };
            this.Controls.Add(_passwordTextBox);

            // Show Password CheckBox
            _showPasswordCheckBox = new CheckBox
            {
                Text = "إظهار كلمة المرور",
                Location = new Point(250, 405),
                Size = new Size(150, 25),
                Font = new Font("Cairo", 9),
                TextAlign = ContentAlignment.MiddleRight
            };
            _showPasswordCheckBox.AutoSize = true;
            _showPasswordCheckBox.CheckedChanged += ShowPasswordCheckBox_CheckedChanged;
            this.Controls.Add(_showPasswordCheckBox);

            // Error Label
            _errorLabel = new Label
            {
                Location = new Point(50, 435),
                Size = new Size(350, 25),
                Font = new Font("Cairo", 9),
                ForeColor = Color.Red,
                TextAlign = ContentAlignment.MiddleCenter,
                Visible = false
            };
            this.Controls.Add(_errorLabel);

            // Login Button
            _loginButton = new Button
            {
                Text = "تسجيل الدخول",
                Location = new Point(230, 470),
                Size = new Size(170, 40),
                Font = new Font("Cairo", 10, FontStyle.Bold),
                Cursor = Cursors.Hand
            };
            _loginButton.AutoSize = true;
            _loginButton.AutoSizeMode = AutoSizeMode.GrowAndShrink;
            _loginButton.MinimumSize = new Size(170, 40);
            _loginButton.Click += LoginButton_Click;
            this.Controls.Add(_loginButton);

            // Exit Button
            _exitButton = new Button
            {
                Text = "خروج",
                Location = new Point(50, 470),
                Size = new Size(170, 40),
                Font = new Font("Cairo", 10),
                Cursor = Cursors.Hand
            };
            _exitButton.AutoSize = true;
            _exitButton.AutoSizeMode = AutoSizeMode.GrowAndShrink;
            _exitButton.MinimumSize = new Size(170, 40);
            _exitButton.Click += ExitButton_Click;
            this.Controls.Add(_exitButton);

            // Event handlers
            this.KeyDown += LoginForm_KeyDown;
            _usernameTextBox.KeyDown += TextBox_KeyDown;
            _passwordTextBox.KeyDown += TextBox_KeyDown;
            _usernameTextBox.TabIndex = 0;
            _passwordTextBox.TabIndex = 1;
            _showPasswordCheckBox.TabIndex = 2;
            _loginButton.TabIndex = 3;
            _exitButton.TabIndex = 4;
            _logoPictureBox.TabStop = false;
            AcceptButton = _loginButton;
        }

        private void ApplyTheme()
        {
            // Form background
            this.BackColor = Color.FromArgb(240, 248, 255); // Light blue background

            // Login button style (Primary)
            _loginButton.BackColor = ThemeManager.PrimaryDeepBlue; // Deep Blue
            _loginButton.ForeColor = Color.White;
            _loginButton.FlatStyle = FlatStyle.Flat;
            _loginButton.FlatAppearance.BorderSize = 0;

            // Exit button style (Secondary)
            _exitButton.BackColor = Color.FromArgb(108, 117, 125); // Gray
            _exitButton.ForeColor = Color.White;
            _exitButton.FlatStyle = FlatStyle.Flat;
            _exitButton.FlatAppearance.BorderSize = 0;

            // TextBox borders
            _usernameTextBox.BorderStyle = BorderStyle.FixedSingle;
            _passwordTextBox.BorderStyle = BorderStyle.FixedSingle;
        }

        private void BindLocalizedResources()
        {
            LocalizationManager.Bind(this, "LoginWindowTitle");
            LocalizationManager.Bind(_titleLabel, "LoginProductTitle");
            LocalizationManager.Bind(_usernameLabel, "Username");
            LocalizationManager.Bind(_passwordLabel, "Password");
            LocalizationManager.Bind(_showPasswordCheckBox, "ShowPassword");
            LocalizationManager.Bind(_loginButton, "Login");
            LocalizationManager.Bind(_exitButton, "Exit");
            _usernameTextBox.AccessibleName = LocalizationManager.Get("Username");
            _passwordTextBox.AccessibleName = LocalizationManager.Get("Password");
        }

        private void LocalizationManager_CultureChanged(object? sender, EventArgs e) => ApplyLocalization();

        private void ApplyLocalization()
        {
            if (IsDisposed) return;
            if (InvokeRequired)
            {
                BeginInvoke(new Action(ApplyLocalization));
                return;
            }

            SuspendLayout();
            ThemeManager.ApplyCultureDirection(this);
            LocalizationManager.ApplyResources(this);
            var isRightToLeft = LocalizationManager.CurrentCulture.TextInfo.IsRightToLeft;
            _loginButton.Left = isRightToLeft ? 230 : 50;
            _exitButton.Left = isRightToLeft ? 50 : 230;
            _showPasswordCheckBox.Left = isRightToLeft ? 250 : 50;
            _usernameTextBox.AccessibleName = LocalizationManager.Get("Username");
            _passwordTextBox.AccessibleName = LocalizationManager.Get("Password");
            ResumeLayout(true);
        }

        private void ShowPasswordCheckBox_CheckedChanged(object? sender, EventArgs e)
        {
            _passwordTextBox.UseSystemPasswordChar = !_showPasswordCheckBox.Checked;
        }

        private void LoginButton_Click(object? sender, EventArgs e)
        {
            PerformLogin();
        }

        private void ExitButton_Click(object? sender, EventArgs e)
        {
            Application.Exit();
        }

        private void TextBox_KeyDown(object? sender, KeyEventArgs e)
        {
            if (e.KeyCode == Keys.Enter)
            {
                e.SuppressKeyPress = true;
                PerformLogin();
            }
        }

        private void LoginForm_KeyDown(object? sender, KeyEventArgs e)
        {
            if (e.KeyCode == Keys.Escape)
            {
                Application.Exit();
            }
        }

        private void PerformLogin()
        {
            string username = _usernameTextBox.Text.Trim();
            string password = _passwordTextBox.Text;

            if (string.IsNullOrWhiteSpace(username))
            {
                ShowError(LocalizationManager.Get("UsernameRequired"));
                _usernameTextBox.Focus();
                return;
            }

            if (string.IsNullOrWhiteSpace(password))
            {
                ShowError(LocalizationManager.Get("PasswordRequired"));
                _passwordTextBox.Focus();
                return;
            }

            // ✅ Check if account is locked (Rate Limiting)
            var unlockTime = AuthenticationService.GetUnlockTime(username);
            if (unlockTime.HasValue && unlockTime.Value > DateTime.Now)
            {
                var minutesRemaining = (int)(unlockTime.Value - DateTime.Now).TotalMinutes + 1;
                ShowError(LocalizationManager.Format("AccountLockedMinutes", minutesRemaining));
                LoggingService.LogWarning($"محاولة تسجيل دخول لحساب مقفل: {username}");
                return;
            }

            // ✅ Show remaining attempts
            var remainingAttempts = AuthenticationService.GetRemainingAttempts(username);
            if (remainingAttempts < 5 && remainingAttempts > 0)
            {
                LoggingService.LogInfo($"⚠️ محاولات متبقية: {remainingAttempts} لـ {username}");
            }

            // Disable login button to prevent multiple clicks
            _loginButton.Enabled = false;
            _loginButton.Text = LocalizationManager.Get("Validating");

            try
            {
                bool success = _authService.Login(username, password);

                if (success)
                {
                    // ✅ Check for backup warning
                    CheckBackupWarning();
                    
                    // ✅ Check for security issues
                    CheckSecurityWarnings();
                    
                    LoginSuccessful = true;
                    this.DialogResult = DialogResult.OK;
                    this.Close();
                }
                else
                {
                    // ✅ Show remaining attempts
                    var remaining = AuthenticationService.GetRemainingAttempts(username);
                    if (remaining > 0)
                    {
                        ShowError(LocalizationManager.Format("InvalidCredentialsRemaining", remaining));
                    }
                    else
                    {
                        ShowError(LocalizationManager.Get("InvalidCredentials"));
                    }
                    
                    _passwordTextBox.Clear();
                    _passwordTextBox.Focus();
                }
            }
            catch (Exception ex)
            {
                ShowError(LocalizationManager.Get("LoginConnectionFailed"));
                LoggingService.LogError($"خطأ في تسجيل الدخول: {ex.Message}");
            }
            finally
            {
                _loginButton.Enabled = true;
                LocalizationManager.ApplyResources(_loginButton);
            }
        }

        /// <summary>
        /// التحقق من تحذيرات النسخ الاحتياطي
        /// </summary>
        private void CheckBackupWarning()
        {
            try
            {
                if (BackupService.NeedsBackup())
                {
                    var days = BackupService.GetDaysSinceLastBackup();
                    MessageBox.Show(
                        LocalizationManager.Format("BackupOverdueWarning", days),
                        LocalizationManager.Get("BackupWarningTitle"),
                        MessageBoxButtons.OK,
                        MessageBoxIcon.Warning
                    );
                }
            }
            catch (Exception ex)
            {
                LoggingService.LogWarning($"⚠️ فشل فحص النسخ الاحتياطي: {ex.Message}");
            }
        }

        /// <summary>
        /// التحقق من التحذيرات الأمنية
        /// </summary>
        private void CheckSecurityWarnings()
        {
            try
            {
                // Check if using default password
                if (AuthenticationService.CurrentUsername == "admin" && 
                    AuthenticationService.CurrentUser?.UpdatedAt == null)
                {
                    var result = MessageBox.Show(
                        LocalizationManager.Get("DefaultPasswordWarning"),
                        LocalizationManager.Get("CriticalSecurityWarningTitle"),
                        MessageBoxButtons.YesNo,
                        MessageBoxIcon.Warning
                    );

                    if (result == DialogResult.Yes)
                    {
                        // سيتم فتح نموذج تغيير كلمة المرور في MainForm
                        LoggingService.LogInfo("المستخدم وافق على تغيير كلمة المرور الافتراضية");
                    }
                }
            }
            catch (Exception ex)
            {
                LoggingService.LogWarning($"⚠️ فشل فحص التحذيرات الأمنية: {ex.Message}");
            }
        }

        private void ShowError(string message)
        {
            _errorLabel.Text = message;
            _errorLabel.Visible = true;

            // Hide error after 5 seconds
            var timer = new System.Windows.Forms.Timer();
            timer.Interval = 5000;
            timer.Tick += (s, e) =>
            {
                _errorLabel.Visible = false;
                timer.Stop();
                timer.Dispose();
            };
            timer.Start();
        }

        /// <summary>
        /// رسم شعار نصي مؤقت إذا لم يكن الشعار موجوداً
        /// </summary>
        private void DrawTemporaryLogo(object? sender, PaintEventArgs e)
        {
            var graphics = e.Graphics;
            graphics.SmoothingMode = System.Drawing.Drawing2D.SmoothingMode.AntiAlias;
            
            // رسم دائرة زرقاء عميقة
            using (var brush = new SolidBrush(Color.FromArgb(0, 51, 102)))
            {
                graphics.FillEllipse(brush, 25, 25, 100, 100);
            }
            
            // رسم موجة بسيطة (تمثل الماء)
            using (var pen = new Pen(Color.White, 4))
            {
                // موجة علوية
                graphics.DrawArc(pen, 35, 50, 80, 25, 0, 180);
                // موجة سفلية
                graphics.DrawArc(pen, 35, 65, 80, 25, 0, 180);
            }
            
            // رسم شكل سمكة بسيط
            using (var fishBrush = new SolidBrush(Color.FromArgb(0, 153, 119)))
            {
                // جسم السمكة (بيضاوي صغير)
                graphics.FillEllipse(fishBrush, 60, 75, 30, 15);
                
                // ذيل السمكة (مثلث)
                Point[] tail = new Point[]
                {
                    new Point(58, 82),
                    new Point(45, 75),
                    new Point(45, 90)
                };
                graphics.FillPolygon(fishBrush, tail);
            }
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing)
                LocalizationManager.CultureChanged -= LocalizationManager_CultureChanged;
            base.Dispose(disposing);
        }
    }
}
