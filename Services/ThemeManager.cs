using System;
using System.Drawing;
using System.Linq;
using System.Windows.Forms;

namespace FishFarmManager.Services
{
    public static class ThemeManager
    {
        // AquaFarm Pro palette
        public static readonly Color PrimaryDeepBlue = ColorTranslator.FromHtml("#003366");
        public static readonly Color SecondarySkyBlue = ColorTranslator.FromHtml("#3399FF");
        public static readonly Color SecondaryAquaGreen = ColorTranslator.FromHtml("#009977");
        public static readonly Color NeutralLightGray = ColorTranslator.FromHtml("#F2F2F2");
        public static readonly Color PureWhite = ColorTranslator.FromHtml("#FFFFFF");
        public static readonly Color White = ColorTranslator.FromHtml("#FFFFFF");
        
        // Functional colors
        public static readonly Color SuccessGreen = SecondaryAquaGreen;
        public static readonly Color ErrorRed = ColorTranslator.FromHtml("#D32F2F");
        public static readonly Color WarningAmber = ColorTranslator.FromHtml("#FFC107");
        public static readonly Color InfoBlue = SecondarySkyBlue;
        
        // Additional colors
        public static readonly Color TextDark = Color.FromArgb(51, 51, 51);
        public static readonly Color BorderColor = Color.FromArgb(220, 220, 220);
        public static readonly Color GridAlternateRow = Color.FromArgb(248, 249, 250);

        public static void ApplyTheme(Form form)
        {
            ApplyCultureDirection(form);
            form.Font = CreatePreferredFont(10f);
            form.BackColor = PureWhite;

            foreach (Control c in form.Controls)
            {
                ApplyThemeToControlTree(c);
            }
        }

        public static void ApplyCultureDirection(Form form)
        {
            var isRightToLeft = LocalizationManager.CurrentCulture.TextInfo.IsRightToLeft;
            form.RightToLeft = isRightToLeft ? RightToLeft.Yes : RightToLeft.No;
            form.RightToLeftLayout = isRightToLeft;
            ApplyDirectionToControlTree(form, isRightToLeft);
        }

        private static void ApplyDirectionToControlTree(Control control, bool isRightToLeft)
        {
            control.RightToLeft = isRightToLeft ? RightToLeft.Yes : RightToLeft.No;

            switch (control)
            {
                case TextBox textBox:
                    textBox.TextAlign = isRightToLeft
                        ? HorizontalAlignment.Right
                        : HorizontalAlignment.Left;
                    break;
                case Label label when label.TextAlign == ContentAlignment.MiddleRight
                                      || label.TextAlign == ContentAlignment.MiddleLeft:
                    label.TextAlign = isRightToLeft
                        ? ContentAlignment.MiddleRight
                        : ContentAlignment.MiddleLeft;
                    break;
                case ToolStrip toolStrip:
                    toolStrip.RightToLeft = isRightToLeft ? RightToLeft.Yes : RightToLeft.No;
                    break;
                case FlowLayoutPanel flow:
                    flow.FlowDirection = isRightToLeft
                        ? FlowDirection.RightToLeft
                        : FlowDirection.LeftToRight;
                    break;
            }

            foreach (Control child in control.Controls)
            {
                ApplyDirectionToControlTree(child, isRightToLeft);
            }
        }

        private static void ApplyThemeToControlTree(Control control)
        {
            switch (control)
            {
                case MenuStrip ms:
                    ms.RenderMode = ToolStripRenderMode.System;
                    ms.BackColor = PrimaryDeepBlue;
                    ms.ForeColor = PureWhite;
                    foreach (ToolStripItem item in ms.Items)
                    {
                        item.ForeColor = PureWhite;
                    }
                    break;
                case StatusStrip ss:
                    ss.BackColor = NeutralLightGray;
                    foreach (ToolStripItem item in ss.Items)
                    {
                        item.ForeColor = Color.Black;
                    }
                    break;
                case ToolStrip ts:
                    ts.BackColor = PrimaryDeepBlue;
                    foreach (ToolStripItem item in ts.Items)
                    {
                        item.ForeColor = PureWhite;
                    }
                    break;
                case Button btn:
                    EnsureButtonLayout(btn);
                    break;
                case Panel panel:
                    if (panel.Name?.Contains("DashboardPanel", StringComparison.OrdinalIgnoreCase) == true)
                    {
                        panel.BackColor = NeutralLightGray;
                    }
                    break;
                case DataGridView dgv:
                    StyleGrid(dgv);
                    break;
                case GroupBox gb:
                    gb.Font = CreatePreferredFont(10f, FontStyle.Bold);
                    break;
            }

            control.Font = control.Font.Name == "Microsoft Sans Serif" ? CreatePreferredFont(control.Font.Size) : control.Font;

            foreach (Control child in control.Controls)
            {
                ApplyThemeToControlTree(child);
            }
        }

        public static void StylePrimaryButton(Button btn)
        {
            btn.FlatStyle = FlatStyle.Flat;
            btn.FlatAppearance.BorderSize = 0;
            btn.BackColor = SecondaryAquaGreen;
            btn.ForeColor = PureWhite;
            btn.Padding = new Padding(8, 4, 8, 4);
            btn.Font = CreatePreferredFont(9.5f, FontStyle.Bold);
            EnsureButtonLayout(btn);
        }

        public static void StyleSecondaryButton(Button btn)
        {
            btn.FlatStyle = FlatStyle.Flat;
            btn.FlatAppearance.BorderSize = 0;
            btn.BackColor = SecondarySkyBlue;
            btn.ForeColor = PureWhite;
            btn.Padding = new Padding(8, 4, 8, 4);
            btn.Font = CreatePreferredFont(9.5f, FontStyle.Bold);
            EnsureButtonLayout(btn);
        }

        private static void EnsureButtonLayout(Button btn)
        {
            btn.AutoSize = true;
            btn.AutoSizeMode = AutoSizeMode.GrowAndShrink;
            btn.MinimumSize = new Size(120, 40);
            btn.Padding = new Padding(16, 6, 16, 6);
            btn.TextAlign = ContentAlignment.MiddleCenter;
            btn.UseCompatibleTextRendering = false;
            btn.AccessibleName ??= btn.Text;
        }

        public static void StyleGrid(DataGridView grid)
        {
            grid.EnableHeadersVisualStyles = false;
            grid.BackgroundColor = PureWhite;
            grid.BorderStyle = BorderStyle.None;
            grid.ColumnHeadersDefaultCellStyle.BackColor = PrimaryDeepBlue;
            grid.ColumnHeadersDefaultCellStyle.ForeColor = PureWhite;
            grid.ColumnHeadersDefaultCellStyle.Font = CreatePreferredFont(9.5f, FontStyle.Bold);
            grid.AlternatingRowsDefaultCellStyle.BackColor = Color.FromArgb(245, 250, 255);
            grid.DefaultCellStyle.SelectionBackColor = Color.FromArgb(230, 240, 255);
            grid.DefaultCellStyle.SelectionForeColor = Color.Black;
            grid.GridColor = NeutralLightGray;
        }

        private static Font CreatePreferredFont(float size, FontStyle style = FontStyle.Regular)
        {
            // Try preferred fonts; fall back to Cairo for Arabic, then Segoe UI
            string[] preferred = new[] { "Segoe UI", "Tahoma", "Cairo", "Arial" };
            foreach (var name in preferred)
            {
                try
                {
                    using var test = new Font(name, size, style);
                    if (string.Equals(test.Name, name, StringComparison.OrdinalIgnoreCase))
                        return new Font(name, size, style);
                }
                catch { }
            }
            return SystemFonts.DefaultFont;
        }
        
        // Font properties for compatibility
        private static Font? _mainFont = null;
        private static Font? _titleFont = null;
        private static Font? _subtitleFont = null;
        private static Font? _buttonFont = null;
        private static Font? _smallFont = null;
        
        public static Font MainFont
        {
            get
            {
                if (_mainFont == null)
                    _mainFont = CreatePreferredFont(10f);
                return _mainFont;
            }
        }
        
        public static Font TitleFont
        {
            get
            {
                if (_titleFont == null)
                    _titleFont = CreatePreferredFont(14f, FontStyle.Bold);
                return _titleFont;
            }
        }
        
        public static Font SubtitleFont
        {
            get
            {
                if (_subtitleFont == null)
                    _subtitleFont = CreatePreferredFont(12f, FontStyle.Bold);
                return _subtitleFont;
            }
        }
        
        public static Font ButtonFont
        {
            get
            {
                if (_buttonFont == null)
                    _buttonFont = CreatePreferredFont(10f, FontStyle.Bold);
                return _buttonFont;
            }
        }
        
        public static Font SmallFont
        {
            get
            {
                if (_smallFont == null)
                    _smallFont = CreatePreferredFont(9f);
                return _smallFont;
            }
        }
        
        #region طرق الرسائل - Message Methods
        
        /// <summary>
        /// عرض رسالة نجاح
        /// </summary>
        public static void ShowSuccess(string message, string? title = null)
        {
            MessageBox.Show(message, title ?? LocalizationManager.Get("Success"), MessageBoxButtons.OK, MessageBoxIcon.Information);
        }

        /// <summary>
        /// عرض رسالة خطأ
        /// </summary>
        public static void ShowError(string message, string? title = null)
        {
            MessageBox.Show(message, title ?? LocalizationManager.Get("Error"), MessageBoxButtons.OK, MessageBoxIcon.Error);
        }

        /// <summary>
        /// عرض رسالة تحذير
        /// </summary>
        public static void ShowWarning(string message, string? title = null)
        {
            MessageBox.Show(message, title ?? LocalizationManager.Get("Warning"), MessageBoxButtons.OK, MessageBoxIcon.Warning);
        }

        /// <summary>
        /// عرض رسالة معلومات
        /// </summary>
        public static void ShowInfo(string message, string? title = null)
        {
            MessageBox.Show(message, title ?? LocalizationManager.Get("Information"), MessageBoxButtons.OK, MessageBoxIcon.Information);
        }

        /// <summary>
        /// طلب تأكيد
        /// </summary>
        public static bool Confirm(string message, string? title = null)
        {
            return MessageBox.Show(
                message,
                title ?? LocalizationManager.Get("Confirmation"),
                MessageBoxButtons.YesNo,
                MessageBoxIcon.Question) == DialogResult.Yes;
        }
        
        #endregion

        #region طرق إنشاء الأزرار - Button Creation Methods
        
        public static Button CreatePrimaryButton(string text)
        {
            var button = new Button
            {
                Text = text,
                BackColor = PrimaryDeepBlue,
                ForeColor = Color.White,
                FlatStyle = FlatStyle.Flat,
                Font = ButtonFont,
                FlatAppearance = { BorderSize = 0 },
                UseVisualStyleBackColor = false
            };
            EnsureButtonLayout(button);
            return button;
        }
        
        public static Button CreateSuccessButton(string text)
        {
            var button = new Button
            {
                Text = text,
                BackColor = SuccessGreen,
                ForeColor = Color.White,
                FlatStyle = FlatStyle.Flat,
                Font = ButtonFont,
                FlatAppearance = { BorderSize = 0 },
                UseVisualStyleBackColor = false
            };
            EnsureButtonLayout(button);
            return button;
        }
        
        public static Button CreateSecondaryButton(string text)
        {
            var button = new Button
            {
                Text = text,
                BackColor = SecondarySkyBlue,
                ForeColor = Color.White,
                FlatStyle = FlatStyle.Flat,
                Font = ButtonFont,
                FlatAppearance = { BorderSize = 0 },
                UseVisualStyleBackColor = false
            };
            EnsureButtonLayout(button);
            return button;
        }
        
        public static Button CreateWarningButton(string text)
        {
            var button = new Button
            {
                Text = text,
                BackColor = WarningAmber,
                ForeColor = Color.White,
                FlatStyle = FlatStyle.Flat,
                Font = ButtonFont,
                FlatAppearance = { BorderSize = 0 },
                UseVisualStyleBackColor = false
            };
            EnsureButtonLayout(button);
            return button;
        }
        
        public static Button CreateErrorButton(string text)
        {
            var button = new Button
            {
                Text = text,
                BackColor = ErrorRed,
                ForeColor = Color.White,
                FlatStyle = FlatStyle.Flat,
                Font = ButtonFont,
                FlatAppearance = { BorderSize = 0 },
                UseVisualStyleBackColor = false
            };
            EnsureButtonLayout(button);
            return button;
        }
        
        #endregion
    }
}
