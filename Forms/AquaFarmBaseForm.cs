using System;
using System.Drawing;
using System.Windows.Forms;
using FishFarmManager.Services;

namespace FishFarmManager.Forms
{
    /// <summary>
    /// نموذج أساسي لجميع نماذج AquaFarm Pro
    /// يطبق الهوية البصرية تلقائياً ويوفر طرق مساعدة موحدة
    /// </summary>
    public class AquaFarmBaseForm : Form
    {
        /// <summary>
        /// Constructor - تطبيق الإعدادات الأساسية
        /// </summary>
        protected AquaFarmBaseForm()
        {
            InitializeBaseForm();
        }

        /// <summary>
        /// تهيئة النموذج الأساسي بالإعدادات الموحدة
        /// </summary>
        private void InitializeBaseForm()
        {
            // تطبيق الإعدادات الأساسية للهوية البصرية
            this.RightToLeft = RightToLeft.Yes;
            this.RightToLeftLayout = true;
            this.BackColor = ThemeManager.NeutralLightGray;
            this.Font = ThemeManager.MainFont;
            this.StartPosition = FormStartPosition.CenterScreen;
            this.MinimumSize = new Size(600, 400);

            // تطبيق الثيم عند تحميل النموذج
            this.Load += AquaFarmBaseForm_Load;
        }

        /// <summary>
        /// معالج حدث التحميل - تطبيق الثيم
        /// </summary>
        private void AquaFarmBaseForm_Load(object? sender, EventArgs e)
        {
            try
            {
                ThemeManager.ApplyTheme(this);
                LoggingService.LogInfo($"تم تحميل النموذج: {this.Text}");
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"خطأ في تطبيق الثيم: {ex.Message}");
            }
        }

        #region طرق مساعدة للواجهة - UI Helper Methods

        /// <summary>
        /// إنشاء شريط عنوان موحد
        /// </summary>
        /// <param name="title">نص العنوان</param>
        /// <returns>Panel يحتوي على العنوان</returns>
        protected Panel CreateTitleBar(string title)
        {
            var panel = new Panel
            {
                Dock = DockStyle.Top,
                Height = 60,
                BackColor = ThemeManager.PrimaryDeepBlue,
                Padding = new Padding(20, 0, 20, 0)
            };

            var label = new Label
            {
                Text = title,
                Font = ThemeManager.TitleFont,
                ForeColor = ThemeManager.White,
                AutoSize = false,
                Dock = DockStyle.Fill,
                TextAlign = ContentAlignment.MiddleRight
            };

            panel.Controls.Add(label);
            return panel;
        }

        /// <summary>
        /// إنشاء شريط أزرار موحد في أسفل النموذج
        /// </summary>
        /// <param name="buttons">الأزرار المراد إضافتها</param>
        /// <returns>Panel يحتوي على الأزرار</returns>
        protected Panel CreateButtonBar(params Button[] buttons)
        {
            var panel = new Panel
            {
                Dock = DockStyle.Bottom,
                Height = 70,
                BackColor = ThemeManager.White,
                Padding = new Padding(10)
            };

            int x = panel.Width - 20;
            foreach (var button in buttons)
            {
                button.Anchor = AnchorStyles.Top | AnchorStyles.Right;
                button.Location = new Point(x - button.Width, 15);
                panel.Controls.Add(button);
                x -= button.Width + 10;
            }

            // تحديث المواقع عند تغيير حجم Panel
            panel.SizeChanged += (s, e) =>
            {
                int xPos = panel.Width - 20;
                foreach (Button btn in panel.Controls)
                {
                    btn.Location = new Point(xPos - btn.Width, 15);
                    xPos -= btn.Width + 10;
                }
            };

            return panel;
        }

        /// <summary>
        /// إنشاء منطقة محتوى رئيسية
        /// </summary>
        /// <returns>Panel للمحتوى</returns>
        protected Panel CreateContentPanel()
        {
            return new Panel
            {
                Dock = DockStyle.Fill,
                Padding = new Padding(20),
                BackColor = ThemeManager.NeutralLightGray,
                AutoScroll = true
            };
        }

        /// <summary>
        /// إنشاء مجموعة عناصر (GroupBox) موحدة
        /// </summary>
        protected GroupBox CreateGroupBox(string title, int x, int y, int width, int height)
        {
            return new GroupBox
            {
                Text = title,
                Location = new Point(x, y),
                Size = new Size(width, height),
                Font = ThemeManager.SubtitleFont,
                ForeColor = ThemeManager.PrimaryDeepBlue,
                Padding = new Padding(10)
            };
        }

        /// <summary>
        /// إنشاء تسمية (Label) موحدة
        /// </summary>
        protected Label CreateLabel(string text, int x, int y, int width = 150)
        {
            return new Label
            {
                Text = text,
                Location = new Point(x, y),
                Size = new Size(width, 25),
                Font = ThemeManager.MainFont,
                ForeColor = ThemeManager.TextDark,
                TextAlign = ContentAlignment.MiddleRight,
                AutoSize = false
            };
        }

        /// <summary>
        /// إنشاء تسمية (Label) موحدة مع خيارات تنسيق
        /// </summary>
        protected Label CreateLabel(string text, int fontSize, bool isBold = false)
        {
            var font = isBold 
                ? new Font(ThemeManager.MainFont.FontFamily, fontSize, FontStyle.Bold)
                : new Font(ThemeManager.MainFont.FontFamily, fontSize, FontStyle.Regular);
            
            return new Label
            {
                Text = text,
                Font = font,
                ForeColor = ThemeManager.TextDark,
                AutoSize = true,
                TextAlign = ContentAlignment.MiddleRight
            };
        }

        /// <summary>
        /// إنشاء حقل نص (TextBox) موحد
        /// </summary>
        protected TextBox CreateTextBox(int x, int y, int width = 250)
        {
            return new TextBox
            {
                Location = new Point(x, y),
                Size = new Size(width, 30),
                Font = ThemeManager.MainFont,
                TextAlign = HorizontalAlignment.Right,
                BorderStyle = BorderStyle.FixedSingle
            };
        }

        /// <summary>
        /// إنشاء DataGridView موحد
        /// </summary>
        protected DataGridView CreateDataGridView(int x, int y, int width, int height)
        {
            var grid = new DataGridView
            {
                Location = new Point(x, y),
                Size = new Size(width, height),
                AutoGenerateColumns = false,
                AllowUserToAddRows = false,
                AllowUserToDeleteRows = false,
                ReadOnly = true,
                SelectionMode = DataGridViewSelectionMode.FullRowSelect,
                MultiSelect = false,
                RowHeadersVisible = false,
                AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill,
                BackgroundColor = ThemeManager.White,
                BorderStyle = BorderStyle.None,
                CellBorderStyle = DataGridViewCellBorderStyle.SingleHorizontal,
                GridColor = ThemeManager.BorderColor,
                Font = ThemeManager.MainFont
            };

            // تطبيق الثيم
            grid.EnableHeadersVisualStyles = false;
            grid.ColumnHeadersDefaultCellStyle.BackColor = ThemeManager.PrimaryDeepBlue;
            grid.ColumnHeadersDefaultCellStyle.ForeColor = ThemeManager.White;
            grid.ColumnHeadersDefaultCellStyle.Font = ThemeManager.ButtonFont;
            grid.ColumnHeadersDefaultCellStyle.Padding = new Padding(5);
            grid.ColumnHeadersHeight = 40;

            grid.AlternatingRowsDefaultCellStyle.BackColor = ThemeManager.GridAlternateRow;
            grid.DefaultCellStyle.SelectionBackColor = ThemeManager.SecondarySkyBlue;
            grid.DefaultCellStyle.SelectionForeColor = ThemeManager.White;
            grid.DefaultCellStyle.Padding = new Padding(5);
            grid.RowTemplate.Height = 35;

            return grid;
        }

        #endregion

        #region طرق الرسائل - Message Methods

        /// <summary>
        /// عرض رسالة نجاح
        /// </summary>
        protected void ShowSuccess(string message, string title = "نجح")
        {
            ThemeManager.ShowSuccess(message, title);
        }

        /// <summary>
        /// عرض رسالة خطأ
        /// </summary>
        protected void ShowError(string message, string title = "خطأ")
        {
            ThemeManager.ShowError(message, title);
        }

        /// <summary>
        /// عرض رسالة تحذير
        /// </summary>
        protected void ShowWarning(string message, string title = "تحذير")
        {
            ThemeManager.ShowWarning(message, title);
        }

        /// <summary>
        /// عرض رسالة معلومات
        /// </summary>
        protected void ShowInfo(string message, string title = "معلومات")
        {
            ThemeManager.ShowInfo(message, title);
        }

        /// <summary>
        /// طلب تأكيد من المستخدم
        /// </summary>
        protected bool Confirm(string message, string title = "تأكيد")
        {
            return ThemeManager.Confirm(message, title);
        }

        #endregion

        #region طرق التحقق من البيانات - Validation Methods

        /// <summary>
        /// التحقق من أن الحقل النصي ليس فارغاً
        /// </summary>
        protected bool ValidateRequired(TextBox textBox, string fieldName)
        {
            if (string.IsNullOrWhiteSpace(textBox.Text))
            {
                ShowWarning($"الرجاء إدخال {fieldName}");
                textBox.Focus();
                return false;
            }
            return true;
        }

        /// <summary>
        /// التحقق من أن الرقم صحيح
        /// </summary>
        protected bool ValidateNumeric(TextBox textBox, string fieldName, out decimal value)
        {
            if (!decimal.TryParse(textBox.Text, out value))
            {
                ShowWarning($"{fieldName} يجب أن يكون رقماً صحيحاً");
                textBox.Focus();
                return false;
            }
            return true;
        }

        /// <summary>
        /// التحقق من أن الرقم موجب
        /// </summary>
        protected bool ValidatePositive(TextBox textBox, string fieldName, out decimal value)
        {
            if (!ValidateNumeric(textBox, fieldName, out value))
                return false;

            if (value <= 0)
            {
                ShowWarning($"{fieldName} يجب أن يكون أكبر من صفر");
                textBox.Focus();
                return false;
            }
            return true;
        }

        #endregion

        #region طرق مساعدة عامة - General Helper Methods

        /// <summary>
        /// تنسيق رقم كمبلغ مالي
        /// </summary>
        protected string FormatCurrency(decimal amount)
        {
            return $"{amount:N2} ريال";
        }

        /// <summary>
        /// تنسيق تاريخ
        /// </summary>
        protected string FormatDate(DateTime date)
        {
            return date.ToString("dd/MM/yyyy");
        }

        /// <summary>
        /// تنسيق تاريخ ووقت
        /// </summary>
        protected string FormatDateTime(DateTime dateTime)
        {
            return dateTime.ToString("dd/MM/yyyy HH:mm");
        }

        #endregion

        #region Dispose Pattern - تحرير الموارد

        /// <summary>
        /// تحرير الموارد المُدارة - يمكن للـ Forms الوراثية تجاوز هذا
        /// </summary>
        protected virtual void DisposeResources()
        {
            // يمكن للفورمات الموروثة تجاوز هذه الطريقة لتحرير مواردها
        }

        /// <summary>
        /// تجاوز Dispose للتأكد من تحرير الموارد
        /// </summary>
        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                try
                {
                    DisposeResources();
                }
                catch (Exception ex)
                {
                    System.Diagnostics.Debug.WriteLine($"خطأ في تحرير الموارد: {ex.Message}");
                }
            }
            base.Dispose(disposing);
        }

        #endregion
    }
}

