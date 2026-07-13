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
    /// نموذج حركة المخزون - Stock Movement Form
    /// </summary>
    public partial class StockMovementForm : AquaFarmBaseForm
    {
        private readonly FishFarmContext _context = null!;
        private DataGridView _movementsGrid = null!;
        private ComboBox _itemComboBox = null!;
        private ComboBox _movementTypeComboBox = null!;
        private NumericUpDown _quantityNumeric = null!;
        private DateTimePicker _datePicker = null!;
        private TextBox _notesTextBox = null!;
        private Button _addButton = null!;
        private Button _updateButton = null!;
        private Button _deleteButton = null!;
        private int _selectedMovementId = -1;

        public StockMovementForm(FishFarmContext context)
        {
            // ✅ فحص الصلاحيات أولاً
            if (!AuthenticationService.HasPermission(UserRole.Admin, UserRole.Manager, UserRole.Accountant, UserRole.InventoryStaff))
            {
                MessageBox.Show(
                    "ليس لديك صلاحية لإدارة حركة المخزون.\nيرجى الاتصال بالمدير لمنحك الصلاحيات اللازمة.",
                    "خطأ في الصلاحيات",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Warning
                );
                
                LoggingService.LogWarning(
                    "محاولة وصول غير مصرح بها من {Username} إلى حركة المخزون",
                    AuthenticationService.CurrentUsername
                );
                
                this.Load += (s, e) => this.Close();
                return;
            }

            _context = context;
            InitializeComponent();
            this.Load += async (s, e) => await LoadDataAsync();
            
            // ✅ تطبيق صلاحيات على الأزرار
            ApplyPermissions();
        }

        private void InitializeComponent()
        {
            this.Text = "حركة المخزون";
            this.Size = new Size(1000, 700);
            this.StartPosition = FormStartPosition.CenterScreen;

            // إنشاء شريط العنوان
            var titleBar = CreateTitleBar("إدارة حركة المخزون");
            this.Controls.Add(titleBar);

            // إنشاء لوحة التحكم
            var controlPanel = CreateControlPanel();
            this.Controls.Add(controlPanel);

            // إنشاء شبكة البيانات
            _movementsGrid = CreateDataGridView();
            _movementsGrid.SelectionChanged += MovementsGrid_SelectionChanged;
            this.Controls.Add(_movementsGrid);

            // إنشاء شريط الأزرار
            _addButton = ThemeManager.CreateSuccessButton("إضافة حركة");
            _addButton.Click += AddButton_Click;
            
            _updateButton = ThemeManager.CreatePrimaryButton("تحديث");
            _updateButton.Enabled = false;
            _updateButton.Click += UpdateButton_Click;
            
            _deleteButton = ThemeManager.CreateErrorButton("حذف");
            _deleteButton.Enabled = false;
            _deleteButton.Click += DeleteButton_Click;

            var refreshButton = ThemeManager.CreateSecondaryButton("تحديث البيانات");
            refreshButton.Click += async (s, e) => await LoadDataAsync();

            var buttonBar = CreateButtonBar(_addButton, _updateButton, _deleteButton, refreshButton);
            this.Controls.Add(buttonBar);
        }

        private Panel CreateControlPanel()
        {
            var panel = new Panel();
            panel.Height = 150;
            panel.Dock = DockStyle.Top;
            panel.Padding = new Padding(10);

            var y = 10;
            var spacing = 30;
            var controlWidth = 200;

            // العنصر
            var itemLabel = CreateLabel("العنصر:", 10, y);
            panel.Controls.Add(itemLabel);

            _itemComboBox = new ComboBox();
            _itemComboBox.Location = new Point(120, y - 5);
            _itemComboBox.Size = new Size(controlWidth, 25);
            _itemComboBox.DropDownStyle = ComboBoxStyle.DropDownList;
            panel.Controls.Add(_itemComboBox);

            // نوع الحركة
            var movementTypeLabel = CreateLabel("نوع الحركة:", 340, y);
            panel.Controls.Add(movementTypeLabel);

            _movementTypeComboBox = new ComboBox();
            _movementTypeComboBox.Location = new Point(450, y - 5);
            _movementTypeComboBox.Size = new Size(controlWidth, 25);
            _movementTypeComboBox.DropDownStyle = ComboBoxStyle.DropDownList;
            _movementTypeComboBox.Items.AddRange(new[] { "إدخال", "إخراج", "تعديل", "إرجاع", "هالك" });
            panel.Controls.Add(_movementTypeComboBox);

            y += spacing;

            // الكمية
            var quantityLabel = CreateLabel("الكمية:", 10, y);
            panel.Controls.Add(quantityLabel);

            _quantityNumeric = new NumericUpDown();
            _quantityNumeric.Location = new Point(120, y - 5);
            _quantityNumeric.Size = new Size(controlWidth, 25);
            _quantityNumeric.Minimum = 0.01m;
            _quantityNumeric.Maximum = 999999;
            _quantityNumeric.DecimalPlaces = 2;
            panel.Controls.Add(_quantityNumeric);

            // التاريخ
            var dateLabel = CreateLabel("التاريخ:", 340, y);
            panel.Controls.Add(dateLabel);

            _datePicker = new DateTimePicker();
            _datePicker.Location = new Point(450, y - 5);
            _datePicker.Size = new Size(controlWidth, 25);
            _datePicker.Value = DateTime.Now;
            panel.Controls.Add(_datePicker);

            y += spacing;

            // الملاحظات
            var notesLabel = CreateLabel("ملاحظات:", 10, y);
            panel.Controls.Add(notesLabel);

            _notesTextBox = new TextBox();
            _notesTextBox.Location = new Point(120, y - 5);
            _notesTextBox.Size = new Size(530, 25);
            panel.Controls.Add(_notesTextBox);

            return panel;
        }

        private DataGridView CreateDataGridView()
        {
            var grid = new DataGridView();
            grid.Dock = DockStyle.Fill;
            grid.AutoGenerateColumns = false;
            grid.SelectionMode = DataGridViewSelectionMode.FullRowSelect;
            grid.MultiSelect = false;
            grid.ReadOnly = true;
            grid.AllowUserToAddRows = false;
            grid.AllowUserToDeleteRows = false;

            // إضافة الأعمدة
            grid.Columns.Add(new DataGridViewTextBoxColumn 
            { 
                DataPropertyName = "Id", 
                HeaderText = "المعرف", 
                Width = 60, 
                Visible = false 
            });

            grid.Columns.Add(new DataGridViewTextBoxColumn 
            { 
                DataPropertyName = "ItemName", 
                HeaderText = "العنصر", 
                Width = 150 
            });

            grid.Columns.Add(new DataGridViewTextBoxColumn 
            { 
                DataPropertyName = "MovementType", 
                HeaderText = "نوع الحركة", 
                Width = 100 
            });

            grid.Columns.Add(new DataGridViewTextBoxColumn 
            { 
                DataPropertyName = "Quantity", 
                HeaderText = "الكمية", 
                Width = 100,
                DefaultCellStyle = new DataGridViewCellStyle { Format = "N2", Alignment = DataGridViewContentAlignment.MiddleRight }
            });

            grid.Columns.Add(new DataGridViewTextBoxColumn 
            { 
                DataPropertyName = "Unit", 
                HeaderText = "الوحدة", 
                Width = 80 
            });

            grid.Columns.Add(new DataGridViewTextBoxColumn 
            { 
                DataPropertyName = "MovementDate", 
                HeaderText = "التاريخ", 
                Width = 120,
                DefaultCellStyle = new DataGridViewCellStyle { Format = "yyyy/MM/dd" }
            });

            grid.Columns.Add(new DataGridViewTextBoxColumn 
            { 
                DataPropertyName = "Notes", 
                HeaderText = "ملاحظات", 
                Width = 200 
            });

            grid.Columns.Add(new DataGridViewTextBoxColumn 
            { 
                DataPropertyName = "CreatedBy", 
                HeaderText = "المنشئ", 
                Width = 100 
            });

            return grid;
        }

        private async Task LoadDataAsync()
        {
            try
            {
                // تحميل عناصر المخزون
                var items = await _context.InventoryItems
                    .Where(i => i.IsActive)
                    .Select(i => new { i.Id, i.Name })
                    .ToListAsync();

                _itemComboBox.DataSource = items;
                _itemComboBox.DisplayMember = "Name";
                _itemComboBox.ValueMember = "Id";
                _itemComboBox.SelectedIndex = -1;

                // تحميل حركات المخزون
                var movements = await _context.StockMovements
                    .Include(sm => sm.InventoryItem)
                    .OrderByDescending(sm => sm.MovementDate)
                    .Select(sm => new
                    {
                        sm.Id,
                        ItemName = sm.InventoryItem.Name,
                        MovementType = sm.MovementType.ToString(),
                        sm.Quantity,
                        Unit = sm.InventoryItem.Unit,
                        sm.MovementDate,
                        sm.Notes,
                        sm.CreatedBy
                    })
                    .ToListAsync();

                _movementsGrid.DataSource = movements;
            }
            catch (Exception ex)
            {
                ThemeManager.ShowError($"خطأ في تحميل البيانات: {ex.Message}", "خطأ");
            }
        }

        private void MovementsGrid_SelectionChanged(object? sender, EventArgs e)
        {
            if (_movementsGrid.SelectedRows.Count > 0)
            {
                var selectedRow = _movementsGrid.SelectedRows[0];
                _selectedMovementId = (int)selectedRow.Cells["Id"].Value;
                
                LoadMovementData(_selectedMovementId);
                _updateButton.Enabled = true;
                _deleteButton.Enabled = true;
            }
            else
            {
                _selectedMovementId = -1;
                _updateButton.Enabled = false;
                _deleteButton.Enabled = false;
                ClearForm();
            }
        }

        private void LoadMovementData(int movementId)
        {
            try
            {
                var movement = _context.StockMovements
                    .Include(sm => sm.InventoryItem)
                    .FirstOrDefault(sm => sm.Id == movementId);

                if (movement != null)
                {
                    _itemComboBox.SelectedValue = movement.InventoryItemId;
                    _movementTypeComboBox.Text = movement.MovementType.ToString();
                    _quantityNumeric.Value = movement.Quantity;
                    _datePicker.Value = movement.MovementDate;
                    _notesTextBox.Text = movement.Notes ?? "";
                }
            }
            catch (Exception ex)
            {
                ThemeManager.ShowError($"خطأ في تحميل بيانات الحركة: {ex.Message}", "خطأ");
            }
        }

        private void ClearForm()
        {
            _itemComboBox.SelectedIndex = -1;
            _movementTypeComboBox.SelectedIndex = -1;
            _quantityNumeric.Value = 0.01m;
            _datePicker.Value = DateTime.Now;
            _notesTextBox.Clear();
        }

        private async void AddButton_Click(object? sender, EventArgs e)
        {
            if (!ValidateInput())
                return;

            try
            {
                if (_itemComboBox.SelectedValue == null)
                {
                    ThemeManager.ShowWarning("الرجاء اختيار عنصر", "تحذير");
                    return;
                }

                var movement = new StockMovement
                {
                    InventoryItemId = (int)_itemComboBox.SelectedValue!,
                    MovementType = Enum.Parse<StockMovementType>(_movementTypeComboBox.Text),
                    Quantity = _quantityNumeric.Value,
                    MovementDate = _datePicker.Value,
                    Notes = _notesTextBox.Text,
                    CreatedBy = AuthenticationService.CurrentUsername,
                    CreatedAt = DateTime.Now
                };

                _context.StockMovements.Add(movement);
                await _context.SaveChangesAsync();

                // تحديث كمية المخزون
                await UpdateInventoryQuantityAsync(movement);

                ThemeManager.ShowSuccess("تم إضافة حركة المخزون بنجاح", "نجح");
                await LoadDataAsync();
                ClearForm();
            }
            catch (Exception ex)
            {
                ThemeManager.ShowError($"خطأ في إضافة حركة المخزون: {ex.Message}", "خطأ");
            }
        }

        private async void UpdateButton_Click(object? sender, EventArgs e)
        {
            if (_selectedMovementId == -1 || !ValidateInput())
                return;

            if (!ThemeManager.Confirm("هل أنت متأكد من تحديث هذه الحركة؟", "تأكيد"))
                return;

            try
            {
                var movement = await _context.StockMovements.FindAsync(_selectedMovementId);
                if (movement != null)
                {
                    if (_itemComboBox.SelectedValue == null)
                    {
                        ThemeManager.ShowWarning("الرجاء اختيار عنصر", "تحذير");
                        return;
                    }

                    // إرجاع الكمية القديمة
                    await RevertInventoryQuantityAsync(movement);

                    // تحديث البيانات
                    movement.InventoryItemId = (int)_itemComboBox.SelectedValue!;
                    movement.MovementType = Enum.Parse<StockMovementType>(_movementTypeComboBox.Text);
                    movement.Quantity = _quantityNumeric.Value;
                    movement.MovementDate = _datePicker.Value;
                    movement.Notes = _notesTextBox.Text;
                    movement.UpdatedBy = AuthenticationService.CurrentUsername;
                    movement.UpdatedAt = DateTime.Now;

                    await _context.SaveChangesAsync();

                    // تطبيق الكمية الجديدة
                    await UpdateInventoryQuantityAsync(movement);

                    ThemeManager.ShowSuccess("تم تحديث حركة المخزون بنجاح", "نجح");
                    await LoadDataAsync();
                }
            }
            catch (Exception ex)
            {
                ThemeManager.ShowError($"خطأ في تحديث حركة المخزون: {ex.Message}", "خطأ");
            }
        }

        private async void DeleteButton_Click(object? sender, EventArgs e)
        {
            if (_selectedMovementId == -1)
                return;

            if (!ThemeManager.Confirm("هل أنت متأكد من حذف هذه الحركة؟", "تأكيد"))
                return;

            try
            {
                var movement = await _context.StockMovements.FindAsync(_selectedMovementId);
                if (movement != null)
                {
                    // إرجاع الكمية
                    await RevertInventoryQuantityAsync(movement);

                    _context.StockMovements.Remove(movement);
                    await _context.SaveChangesAsync();

                    ThemeManager.ShowSuccess("تم حذف حركة المخزون بنجاح", "نجح");
                    await LoadDataAsync();
                }
            }
            catch (Exception ex)
            {
                ThemeManager.ShowError($"خطأ في حذف حركة المخزون: {ex.Message}", "خطأ");
            }
        }

        private bool ValidateInput()
        {
            if (_itemComboBox.SelectedIndex == -1)
            {
                ThemeManager.ShowError("يرجى اختيار عنصر من المخزون", "خطأ في التحقق");
                return false;
            }

            if (string.IsNullOrEmpty(_movementTypeComboBox.Text))
            {
                ThemeManager.ShowError("يرجى اختيار نوع الحركة", "خطأ في التحقق");
                return false;
            }

            if (_quantityNumeric.Value <= 0)
            {
                ThemeManager.ShowError("يرجى إدخال كمية صحيحة", "خطأ في التحقق");
                return false;
            }

            return true;
        }

        private async Task UpdateInventoryQuantityAsync(StockMovement movement)
        {
            var item = await _context.InventoryItems.FindAsync(movement.InventoryItemId);
            if (item != null)
            {
                switch (movement.MovementType)
                {
                    case StockMovementType.Purchase:
                    case StockMovementType.Production:
                    case StockMovementType.Return:
                    case StockMovementType.Found:
                    case StockMovementType.AdjustmentIncrease:
                        item.CurrentStock += movement.Quantity;
                        break;
                    case StockMovementType.Sale:
                    case StockMovementType.Consumption:
                    case StockMovementType.Loss:
                    case StockMovementType.Damage:
                    case StockMovementType.Waste:
                    case StockMovementType.Expiry:
                    case StockMovementType.Expired:
                    case StockMovementType.AdjustmentDecrease:
                        item.CurrentStock -= movement.Quantity;
                        break;
                    case StockMovementType.Adjustment:
                        item.CurrentStock = movement.Quantity;
                        break;
                }

                await _context.SaveChangesAsync();
            }
        }

        private async Task RevertInventoryQuantityAsync(StockMovement movement)
        {
            var item = await _context.InventoryItems.FindAsync(movement.InventoryItemId);
            if (item != null)
            {
                switch (movement.MovementType)
                {
                    case StockMovementType.Purchase:
                    case StockMovementType.Production:
                    case StockMovementType.Return:
                    case StockMovementType.Found:
                    case StockMovementType.AdjustmentIncrease:
                        item.CurrentStock -= movement.Quantity;
                        break;
                    case StockMovementType.Sale:
                    case StockMovementType.Consumption:
                    case StockMovementType.Loss:
                    case StockMovementType.Damage:
                    case StockMovementType.Waste:
                    case StockMovementType.Expiry:
                    case StockMovementType.Expired:
                    case StockMovementType.AdjustmentDecrease:
                        item.CurrentStock += movement.Quantity;
                        break;
                    case StockMovementType.Adjustment:
                        // للضبط، نحتاج لحفظ القيمة القديمة في مكان آخر
                        break;
                }

                await _context.SaveChangesAsync();
            }
        }

        #region Permission Management - إدارة الصلاحيات

        /// <summary>
        /// تطبيق الصلاحيات على الأزرار والعناصر
        /// </summary>
        private void ApplyPermissions()
        {
            // المشاهد (Viewer) يمكنه فقط عرض البيانات
            bool canModify = AuthenticationService.HasPermission(
                UserRole.Admin, 
                UserRole.Manager, 
                UserRole.Accountant, 
                UserRole.InventoryStaff
            );

            bool canDelete = AuthenticationService.HasPermission(
                UserRole.Admin, 
                UserRole.Manager
            );

            _addButton.Enabled = canModify;
            _updateButton.Enabled = canModify;
            _deleteButton.Enabled = canDelete;
            
            // تعطيل الحقول للمشاهدين فقط
            if (!canModify)
            {
                _itemComboBox.Enabled = false;
                _movementTypeComboBox.Enabled = false;
                _quantityNumeric.Enabled = false;
                _datePicker.Enabled = false;
                _notesTextBox.ReadOnly = true;
            }
        }

        #endregion

        #region Dispose Pattern - تحرير الموارد

        /// <summary>
        /// تحرير الموارد المُدارة
        /// </summary>
        protected override void DisposeResources()
        {
            // لا نحتاج Dispose للـ Context هنا لأنه يتم تمريره من الخارج
            // ولكن نضيف هذا للتوافق مع النمط
            base.DisposeResources();
        }

        #endregion
    }
}

