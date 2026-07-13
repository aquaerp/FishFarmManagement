using System;
using System.Drawing;
using System.Linq;
using System.Text.RegularExpressions;
using System.Windows.Forms;
using FishFarmManager.Data;
using FishFarmManager.Models;
using FishFarmManager.Services;

namespace FishFarmManager.Forms
{
    public class FirstRunAdminForm : Form
    {
        private readonly FishFarmContext _context;
        private TextBox _usernameTextBox = null!;
        private TextBox _fullNameTextBox = null!;
        private TextBox _emailTextBox = null!;
        private TextBox _passwordTextBox = null!;
        private TextBox _confirmPasswordTextBox = null!;
        private Label _errorLabel = null!;

        public FirstRunAdminForm(FishFarmContext context)
        {
            _context = context;
            InitializeComponent();
        }

        private void InitializeComponent()
        {
            Text = "AquaFarm Pro - First Run Setup";
            ClientSize = new Size(520, 430);
            FormBorderStyle = FormBorderStyle.FixedDialog;
            StartPosition = FormStartPosition.CenterScreen;
            MaximizeBox = false;
            MinimizeBox = false;

            Controls.Add(new Label
            {
                Text = "Create Administrator Account",
                Location = new Point(40, 25),
                Size = new Size(440, 35),
                TextAlign = ContentAlignment.MiddleCenter,
                Font = new Font("Segoe UI", 14, FontStyle.Bold)
            });

            Controls.Add(new Label
            {
                Text = "No users were found. Create the first administrator account to continue.",
                Location = new Point(40, 65),
                Size = new Size(440, 30),
                TextAlign = ContentAlignment.MiddleCenter
            });

            _usernameTextBox = AddField("Username:", 115, "admin");
            _fullNameTextBox = AddField("Full name:", 155, "");
            _emailTextBox = AddField("Email:", 195, "");
            _passwordTextBox = AddField("Password:", 235, "", true);
            _confirmPasswordTextBox = AddField("Confirm password:", 275, "", true);

            _errorLabel = new Label
            {
                Location = new Point(40, 315),
                Size = new Size(440, 35),
                ForeColor = Color.DarkRed,
                TextAlign = ContentAlignment.MiddleCenter
            };
            Controls.Add(_errorLabel);

            var createButton = new Button
            {
                Text = "Create Admin",
                Location = new Point(270, 360),
                Size = new Size(150, 38),
                BackColor = ThemeManager.PrimaryDeepBlue,
                ForeColor = Color.White,
                FlatStyle = FlatStyle.Flat
            };
            createButton.Click += CreateButton_Click;
            Controls.Add(createButton);

            var cancelButton = new Button
            {
                Text = "Cancel",
                Location = new Point(100, 360),
                Size = new Size(150, 38)
            };
            cancelButton.Click += (_, _) =>
            {
                DialogResult = DialogResult.Cancel;
                Close();
            };
            Controls.Add(cancelButton);
        }

        private TextBox AddField(string label, int y, string value, bool password = false)
        {
            Controls.Add(new Label
            {
                Text = label,
                Location = new Point(55, y + 4),
                Size = new Size(150, 24),
                TextAlign = ContentAlignment.MiddleLeft
            });

            var textBox = new TextBox
            {
                Location = new Point(210, y),
                Size = new Size(260, 28),
                Text = value,
                UseSystemPasswordChar = password
            };
            Controls.Add(textBox);
            return textBox;
        }

        private void CreateButton_Click(object? sender, EventArgs e)
        {
            _errorLabel.Text = string.Empty;

            var username = _usernameTextBox.Text.Trim();
            var fullName = _fullNameTextBox.Text.Trim();
            var email = _emailTextBox.Text.Trim();
            var password = _passwordTextBox.Text;
            var confirmPassword = _confirmPasswordTextBox.Text;

            if (_context.Users.Any())
            {
                _errorLabel.Text = "A user already exists. Restart the application.";
                return;
            }

            if (string.IsNullOrWhiteSpace(username) || string.IsNullOrWhiteSpace(fullName))
            {
                _errorLabel.Text = "Username and full name are required.";
                return;
            }

            if (!Regex.IsMatch(username, @"^[a-zA-Z0-9_]{3,50}$"))
            {
                _errorLabel.Text = "Username must be 3-50 letters, numbers, or underscores.";
                return;
            }

            if (password != confirmPassword)
            {
                _errorLabel.Text = "Passwords do not match.";
                return;
            }

            var validation = AuthenticationService.ValidatePasswordComplexity(password);
            if (!validation.IsValid)
            {
                _errorLabel.Text = "Password must be 8+ chars with upper, lower, digit, and symbol.";
                return;
            }

            var user = new User
            {
                Username = username,
                FullName = fullName,
                Email = email,
                Role = UserRole.Admin,
                IsActive = true,
                PasswordHash = AuthenticationService.HashPassword(password),
                CreatedAt = DateTime.Now,
                CreatedBy = "FirstRun"
            };

            _context.Users.Add(user);
            _context.SaveChanges();
            LoggingService.LogInfo("First administrator account created during first-run setup.");

            DialogResult = DialogResult.OK;
            Close();
        }
    }
}
