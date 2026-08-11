using System;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Globalization;
using System.Windows.Forms;
using Microsoft.EntityFrameworkCore;
using FishFarmManager.Data;
using FishFarmManager.Models;
using FishFarmManager.Services;
using ClosedXML.Excel;
using PdfSharpCore.Drawing;
using PdfSharpCore.Pdf;

namespace FishFarmManager.Forms
{
    public class InventoryReportForm : Form
    {
        private readonly FishFarmContext _context;
        
        // Tabs
        private TabControl _tabs = null!;
        private TabPage _itemsTab = null!;
        private TabPage _movementsTab = null!;
        private TabPage _fishInventoryTab = null!;

        // Items tab controls
        private DataGridView _itemsGrid = null!;
        private ComboBox _categoryFilter = null!;
        private TextBox _itemSearchText = null!;
        private CheckBox _lowStockOnly = null!;
        private CheckBox _expiredOnly = null!;
        private CheckBox _nearExpiryOnly = null!;
        private Button _itemsRefreshButton = null!;
        private Button _itemsExportButton = null!;
    private Label _itemsKpiLabel = null!;

        // Note: Movements functionality is planned for future implementation
        // These fields are declared but not yet used
#pragma warning disable CS0414
        private DataGridView _movementsGrid = null!;
        private DateTimePicker _fromDate = null!;
        private DateTimePicker _toDate = null!;
        private ComboBox _movementTypeFilter = null!;
        private ComboBox _movementItemFilter = null!;
        private ComboBox _approvalStatusFilter = null!;
        private TextBox _movementSearchText = null!;
        private Button _movementsRefreshButton = null!;
        private Button _movementsExportButton = null!;
    private Label _movementsKpiLabel = null!;

        // Fish inventory tab controls (existing) - planned for future use
#pragma warning disable CS0414
        private DataGridView _inventoryGrid = null!;
        private TextBox _summaryTextBox = null!;
#pragma warning restore CS0414

        public InventoryReportForm(FishFarmContext context)
        {
            _context = context;
            InitializeComponent();
            // Initial loads
            LoadItemsData();
            LoadMovementsData();
            LoadFishInventoryData();
            // Apply AquaFarm Pro theme
            try { FishFarmManager.Services.ThemeManager.ApplyTheme(this); } catch { }
        }

        private void InitializeComponent()
        {
            this.Text = "تقارير المخزون";
            this.Size = new Size(1000, 700);
            this.StartPosition = FormStartPosition.CenterScreen;
            this.RightToLeft = RightToLeft.Yes;
            this.RightToLeftLayout = true;
            try { this.Font = new Font("Cairo", 10F, FontStyle.Regular); } catch { }
            // Tabs container
            _tabs = new TabControl { Dock = DockStyle.Fill };
            CreateItemsTab();
            CreateMovementsTab();
            CreateFishInventoryTab();
            this.Controls.Add(_tabs);
        }

        // ============ Items Tab ============
        private void CreateItemsTab()
        {
            _itemsTab = new TabPage("بنود المخزون");

            var container = new TableLayoutPanel { Dock = DockStyle.Fill, RowCount = 3 };
            container.RowStyles.Add(new RowStyle(SizeType.Absolute, 70));
            container.RowStyles.Add(new RowStyle(SizeType.Absolute, 40));
            container.RowStyles.Add(new RowStyle(SizeType.Percent, 100));

            var filterPanel = new FlowLayoutPanel
            {
                Dock = DockStyle.Fill,
                FlowDirection = FlowDirection.RightToLeft,
                Padding = new Padding(10),
                Height = 70
            };

            _categoryFilter = new ComboBox { Width = 180, DropDownStyle = ComboBoxStyle.DropDownList };
            var categories = Enum.GetValues(typeof(InventoryCategory))
                .Cast<InventoryCategory>()
                .Select(c => new { Value = (int)c, Text = c.ToString() })
                .ToList();
            var allCat = new { Value = 0, Text = "كل الفئات" };
            _categoryFilter.DataSource = new[] { allCat }.Concat(categories).ToList();
            _categoryFilter.DisplayMember = "Text";
            _categoryFilter.ValueMember = "Value";

            _itemSearchText = new TextBox { Width = 220, PlaceholderText = "بحث بالكود/الاسم/الوصف" };
            _lowStockOnly = new CheckBox { Text = "منخفض فقط" };
            _expiredOnly = new CheckBox { Text = "منتهي الصلاحية" };
            _nearExpiryOnly = new CheckBox { Text = "قرب انتهاء" };
            _itemsRefreshButton = new Button { Text = "تحديث", Width = 100 };
            _itemsExportButton = new Button { Text = "تصدير Excel/PDF", Width = 140 };

            filterPanel.Controls.AddRange(new Control[]
            {
                _itemsExportButton, _itemsRefreshButton, _nearExpiryOnly, _expiredOnly, _lowStockOnly,
                new Label{ Text = "الفئة:", AutoSize = true, Padding = new Padding(0,7,0,0)}, _categoryFilter,
                new Label{ Text = "بحث:", AutoSize = true, Padding = new Padding(0,7,0,0)}, _itemSearchText
            });

            _itemsGrid = new DataGridView
            {
                Dock = DockStyle.Fill,
                AutoGenerateColumns = false,
                ReadOnly = true,
                AllowUserToAddRows = false
            };
            _itemsGrid.Columns.AddRange(new DataGridViewColumn[]
            {
                new DataGridViewTextBoxColumn{ DataPropertyName = "ItemCode", HeaderText = "الكود", Width = 90 },
                new DataGridViewTextBoxColumn{ DataPropertyName = "ItemName", HeaderText = "الاسم", Width = 180 },
                new DataGridViewTextBoxColumn{ DataPropertyName = "Category", HeaderText = "الفئة", Width = 140 },
                new DataGridViewTextBoxColumn{ DataPropertyName = "UnitOfMeasure", HeaderText = "الوحدة", Width = 70 },
                new DataGridViewTextBoxColumn{ DataPropertyName = "CurrentStock", HeaderText = "الرصيد", Width = 90, DefaultCellStyle = new DataGridViewCellStyle{ Format = "N3" } },
                new DataGridViewTextBoxColumn{ DataPropertyName = "UnitCost", HeaderText = "التكلفة", Width = 90, DefaultCellStyle = new DataGridViewCellStyle{ Format = "N2" } },
                new DataGridViewTextBoxColumn{ DataPropertyName = "StockValue", HeaderText = "قيمة المخزون", Width = 110, DefaultCellStyle = new DataGridViewCellStyle{ Format = "N2" } },
                new DataGridViewTextBoxColumn{ DataPropertyName = "Min", HeaderText = "الحد الأدنى", Width = 90, DefaultCellStyle = new DataGridViewCellStyle{ Format = "N3" } },
                new DataGridViewTextBoxColumn{ DataPropertyName = "Max", HeaderText = "الحد الأقصى", Width = 90, DefaultCellStyle = new DataGridViewCellStyle{ Format = "N3" } },
                new DataGridViewTextBoxColumn{ DataPropertyName = "ReorderPoint", HeaderText = "نقطة الطلب", Width = 90, DefaultCellStyle = new DataGridViewCellStyle{ Format = "N3" } },
                new DataGridViewTextBoxColumn{ DataPropertyName = "DaysToExpiry", HeaderText = "أيام للصلاحية", Width = 110 },
                new DataGridViewCheckBoxColumn{ DataPropertyName = "NeedsReorder", HeaderText = "يحتاج طلب", Width = 80 },
                new DataGridViewCheckBoxColumn{ DataPropertyName = "IsExpired", HeaderText = "منتهي", Width = 70 },
                new DataGridViewCheckBoxColumn{ DataPropertyName = "IsNearExpiry", HeaderText = "قرب انتهاء", Width = 90 },
                new DataGridViewCheckBoxColumn{ DataPropertyName = "IsActive", HeaderText = "نشط", Width = 60 }
            });

            _categoryFilter.SelectedIndexChanged += (s, e) => LoadItemsData();
            _itemSearchText.TextChanged += (s, e) => LoadItemsData();
            _lowStockOnly.CheckedChanged += (s, e) => LoadItemsData();
            _expiredOnly.CheckedChanged += (s, e) => LoadItemsData();
            _nearExpiryOnly.CheckedChanged += (s, e) => LoadItemsData();
            _itemsRefreshButton.Click += (s, e) => LoadItemsData();
            _itemsExportButton.Click += ItemsExportButton_Click;

            _itemsKpiLabel = new Label { Dock = DockStyle.Fill, AutoSize = false, TextAlign = ContentAlignment.MiddleRight };

            container.Controls.Add(filterPanel, 0, 0);
            container.Controls.Add(_itemsKpiLabel, 0, 1);
            container.Controls.Add(_itemsGrid, 0, 2);
            _itemsTab.Controls.Add(container);
            _tabs.TabPages.Add(_itemsTab);
        }

        private void LoadItemsData()
        {
            var query = _context.InventoryItems.AsQueryable();

            if (_categoryFilter?.SelectedValue is int cat && cat > 0)
            {
                var catEnum = (InventoryCategory)cat;
                query = query.Where(i => i.Category == catEnum);
            }

            if (!string.IsNullOrWhiteSpace(_itemSearchText?.Text))
            {
                var t = _itemSearchText.Text.Trim().ToLower();
                query = query.Where(i => (i.ItemCode != null && i.ItemCode.ToLower().Contains(t)) ||
                                         (i.ItemName != null && i.ItemName.ToLower().Contains(t)) ||
                                         (i.Description != null && i.Description.ToLower().Contains(t)));
            }

            if (_lowStockOnly?.Checked == true)
                query = query.Where(i => i.CurrentStock <= i.ReorderPoint);

            if (_expiredOnly?.Checked == true)
                query = query.Where(i => i.ExpiryDate.HasValue && i.ExpiryDate.Value < DateTime.Now);

            // Materialize then apply near-expiry filter in-memory (SQLite provider lacks DateDiffDay)
            var list = query
                .OrderBy(i => i.ItemName)
                .ToList();

            if (_nearExpiryOnly?.Checked == true)
            {
                list = list
                    .Where(i => i.DaysToExpiry.HasValue && i.DaysToExpiry.Value <= 30 && i.DaysToExpiry.Value > 0)
                    .ToList();
            }

            var data = list
                .Select(i => new
                {
                    i.ItemCode,
                    i.ItemName,
                    Category = i.Category.ToString(),
                    i.UnitOfMeasure,
                    i.CurrentStock,
                    i.UnitCost,
                    StockValue = i.StockValue,
                    Min = i.MinimumStock,
                    Max = i.MaximumStock,
                    i.ReorderPoint,
                    DaysToExpiry = i.DaysToExpiry.HasValue ? i.DaysToExpiry.Value.ToString() : "-",
                    i.NeedsReorder,
                    i.IsExpired,
                    i.IsNearExpiry,
                    i.IsActive
                })
                .ToList();

            _itemsGrid.DataSource = data;

            // KPI summary for items
            var totalStockValue = list.Sum(i => i.StockValue);
            var lowCount = list.Count(i => i.CurrentStock <= i.ReorderPoint);
            var expiredCount = list.Count(i => i.IsExpired);
            var nearExpiryCount = list.Count(i => i.IsNearExpiry);
            _itemsKpiLabel.Text = $"قيمة المخزون: {totalStockValue:N2} | منخفض: {lowCount} | منتهي: {expiredCount} | قرب انتهاء: {nearExpiryCount}";
        }

        // ============ Movements Tab ============
        private void CreateMovementsTab()
        {
            _movementsTab = new TabPage("حركات المخزون");

            var container = new TableLayoutPanel { Dock = DockStyle.Fill, RowCount = 3 };
            container.RowStyles.Add(new RowStyle(SizeType.Absolute, 90));
            container.RowStyles.Add(new RowStyle(SizeType.Absolute, 40));
            container.RowStyles.Add(new RowStyle(SizeType.Percent, 100));

            var filterPanel = new FlowLayoutPanel
            {
                Dock = DockStyle.Fill,
                FlowDirection = FlowDirection.RightToLeft,
                Padding = new Padding(10),
                Height = 90
            };

            _fromDate = new DateTimePicker { Width = 130, Format = DateTimePickerFormat.Short, Value = DateTime.Now.AddMonths(-1) };
            _toDate = new DateTimePicker { Width = 130, Format = DateTimePickerFormat.Short, Value = DateTime.Now };

            // Movement types filter
            _movementTypeFilter = new ComboBox { Width = 180, DropDownStyle = ComboBoxStyle.DropDownList };
            var types = Enum.GetValues(typeof(StockMovementType))
                .Cast<StockMovementType>()
                .Select(t => new { Value = (int)t, Text = t.ToString() })
                .ToList();
            
            _movementTypeFilter.DataSource = types;
            _movementTypeFilter.DisplayMember = "Text";
            _movementTypeFilter.ValueMember = "Value";

            _movementItemFilter = new ComboBox { Width = 180, DropDownStyle = ComboBoxStyle.DropDownList };
            _approvalStatusFilter = new ComboBox { Width = 130, DropDownStyle = ComboBoxStyle.DropDownList };
            _approvalStatusFilter.DataSource = new[]
            {
                new { Value = -1, Text = "كل الحالات" },
                new { Value = 1, Text = "معتمدة" },
                new { Value = 0, Text = "مسودة" }
            };
            _approvalStatusFilter.DisplayMember = "Text";
            _approvalStatusFilter.ValueMember = "Value";
            _movementSearchText = new TextBox { Width = 180, PlaceholderText = "بحث بالمرجع أو الملاحظات" };
            _movementsRefreshButton = new Button { Text = "تحديث", Width = 90 };
            _movementsExportButton = new Button { Text = "تصدير Excel", Width = 120 };
            filterPanel.Controls.AddRange(new Control[]
            {
                _movementsExportButton, _movementsRefreshButton,
                new Label { Text = "بحث:", AutoSize = true, Padding = new Padding(0, 7, 0, 0) }, _movementSearchText,
                new Label { Text = "الاعتماد:", AutoSize = true, Padding = new Padding(0, 7, 0, 0) }, _approvalStatusFilter,
                new Label { Text = "النوع:", AutoSize = true, Padding = new Padding(0, 7, 0, 0) }, _movementTypeFilter,
                new Label { Text = "الصنف:", AutoSize = true, Padding = new Padding(0, 7, 0, 0) }, _movementItemFilter,
                new Label { Text = "من:", AutoSize = true, Padding = new Padding(0, 7, 0, 0) }, _fromDate,
                new Label { Text = "إلى:", AutoSize = true, Padding = new Padding(0, 7, 0, 0) }, _toDate
            });

            _movementsGrid = new DataGridView
            {
                Dock = DockStyle.Fill,
                AutoGenerateColumns = false,
                ReadOnly = true,
                AllowUserToAddRows = false,
                AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill
            };
            _movementsGrid.Columns.AddRange(new DataGridViewColumn[]
            {
                new DataGridViewTextBoxColumn { DataPropertyName = "MovementDate", HeaderText = "التاريخ" },
                new DataGridViewTextBoxColumn { DataPropertyName = "ItemName", HeaderText = "الصنف" },
                new DataGridViewTextBoxColumn { DataPropertyName = "MovementType", HeaderText = "النوع" },
                new DataGridViewTextBoxColumn { DataPropertyName = "Quantity", HeaderText = "الكمية", DefaultCellStyle = new DataGridViewCellStyle { Format = "N3" } },
                new DataGridViewTextBoxColumn { DataPropertyName = "BalanceAfter", HeaderText = "الرصيد بعد الحركة", DefaultCellStyle = new DataGridViewCellStyle { Format = "N3" } },
                new DataGridViewTextBoxColumn { DataPropertyName = "Reference", HeaderText = "المرجع" },
                new DataGridViewCheckBoxColumn { DataPropertyName = "IsApproved", HeaderText = "معتمدة" }
            });
            _movementsKpiLabel = new Label { Dock = DockStyle.Fill, AutoSize = false, TextAlign = ContentAlignment.MiddleRight };
            container.Controls.Add(filterPanel, 0, 0);
            container.Controls.Add(_movementsKpiLabel, 0, 1);
            container.Controls.Add(_movementsGrid, 0, 2);
            _movementsTab.Controls.Add(container);
            _tabs.TabPages.Add(_movementsTab);
            LoadMovementItems();
            _fromDate.ValueChanged += (s, e) => LoadMovementsData();
            _toDate.ValueChanged += (s, e) => LoadMovementsData();
            _movementTypeFilter.SelectedIndexChanged += (s, e) => LoadMovementsData();
            _movementItemFilter.SelectedIndexChanged += (s, e) => LoadMovementsData();
            _approvalStatusFilter.SelectedIndexChanged += (s, e) => LoadMovementsData();
            _movementSearchText.TextChanged += (s, e) => LoadMovementsData();
            _movementsRefreshButton.Click += (s, e) => LoadMovementsData();
            _movementsExportButton.Click += MovementsExportButton_Click;
        }

        private void LoadMovementsData()
        {
            if (_movementsGrid == null) return;
            var from = _fromDate.Value.Date;
            var to = _toDate.Value.Date.AddDays(1);
            var query = _context.StockMovements
                .AsNoTracking()
                .Include(value => value.InventoryItem)
                .Where(value => value.MovementDate >= from && value.MovementDate < to);
            if (_movementTypeFilter.SelectedValue is int movementType && movementType > 0)
                query = query.Where(value => (int)value.MovementType == movementType);
            if (_movementItemFilter.SelectedValue is int itemId && itemId > 0)
                query = query.Where(value => value.InventoryItemId == itemId);
            if (_approvalStatusFilter.SelectedValue is int approval && approval >= 0)
                query = query.Where(value => value.IsApproved == (approval == 1));
            var search = _movementSearchText.Text.Trim();
            if (search.Length > 0)
                query = query.Where(value => (value.Reference ?? "").Contains(search) || (value.Notes ?? "").Contains(search));

            var movements = query.OrderByDescending(value => value.MovementDate).ThenByDescending(value => value.Id).ToList();
            _movementsGrid.DataSource = movements.Select(value => new
            {
                MovementDate = value.MovementDate.ToString("yyyy-MM-dd HH:mm"),
                ItemName = value.InventoryItem.ItemName ?? value.InventoryItem.Name,
                MovementType = value.GetMovementTypeDisplay(),
                value.Quantity,
                value.BalanceAfter,
                Reference = value.Reference ?? value.ReferenceNumber ?? "-",
                value.IsApproved
            }).ToList();
            _movementsKpiLabel.Text = $"عدد الحركات: {movements.Count} | المعتمدة: {movements.Count(value => value.IsApproved)} | الكمية: {movements.Sum(value => value.Quantity):N3}";
        }

        private void LoadMovementItems()
        {
            var items = _context.InventoryItems.AsNoTracking()
                .Where(value => value.IsActive)
                .OrderBy(value => value.Name)
                .Select(value => new { value.Id, Name = value.ItemName ?? value.Name })
                .ToList();
            _movementItemFilter.DataSource = new[] { new { Id = 0, Name = "كل الأصناف" } }.Concat(items).ToList();
            _movementItemFilter.DisplayMember = "Name";
            _movementItemFilter.ValueMember = "Id";
        }

        private void LoadFishInventoryData()
        {
            if (_inventoryGrid == null) return;
            var fishItems = _context.InventoryItems.AsNoTracking()
                .Where(value => value.IsActive && value.Category == InventoryCategory.Fingerlings)
                .OrderBy(value => value.Name)
                .Select(value => new
                {
                    Code = value.ItemCode ?? value.Code,
                    Name = value.ItemName ?? value.Name,
                    value.Unit,
                    value.CurrentStock,
                    value.UnitCost,
                    StockValue = value.CurrentStock * value.UnitCost,
                    value.StorageLocation,
                    value.BatchNumber,
                    value.ExpiryDate
                }).ToList();
            _inventoryGrid.DataSource = fishItems;
            _summaryTextBox.Text = $"عدد أصناف الزريعة: {fishItems.Count}\r\nإجمالي الكمية: {fishItems.Sum(value => value.CurrentStock):N3}\r\nإجمالي القيمة: {fishItems.Sum(value => value.StockValue):N2}";
        }

        private void CreateFishInventoryTab()
        {
            _fishInventoryTab = new TabPage("مخزون الأسماك");
            var container = new TableLayoutPanel { Dock = DockStyle.Fill, RowCount = 2 };
            container.RowStyles.Add(new RowStyle(SizeType.Absolute, 90));
            container.RowStyles.Add(new RowStyle(SizeType.Percent, 100));
            _summaryTextBox = new TextBox { Dock = DockStyle.Fill, Multiline = true, ReadOnly = true, BackColor = Color.White };
            _inventoryGrid = new DataGridView { Dock = DockStyle.Fill, ReadOnly = true, AutoGenerateColumns = true, AllowUserToAddRows = false };
            container.Controls.Add(_summaryTextBox, 0, 0);
            container.Controls.Add(_inventoryGrid, 0, 1);
            _fishInventoryTab.Controls.Add(container);
            _tabs.TabPages.Add(_fishInventoryTab);
        }

        private void ItemsExportButton_Click(object? sender, EventArgs e)
        {
            try
            {
                var items = _context.InventoryItems.AsNoTracking()
                    .OrderBy(value => value.Name)
                    .ToList()
                    .Select(value => new
                    {
                        Code = value.ItemCode ?? value.Code,
                        Name = value.ItemName ?? value.Name,
                        Category = value.Category.ToString(),
                        value.Unit,
                        value.CurrentStock,
                        value.UnitCost,
                        StockValue = value.CurrentStock * value.UnitCost,
                        value.MinimumStock,
                        value.MaximumStock,
                        value.ReorderPoint,
                        value.ExpiryDate,
                        value.IsActive
                    }).ToList();
                var path = new ExcelExportService().ExportListToExcel(items, $"Inventory_{DateTime.Now:yyyyMMdd_HHmmss}.xlsx", "Inventory");
                MessageBox.Show($"تم تصدير التقرير إلى:\n{path}", "نجح", MessageBoxButtons.OK, MessageBoxIcon.Information);
            }
            catch (Exception ex)
            {
                LoggingService.LogError(ex, "Error exporting inventory report");
                MessageBox.Show("تعذر تصدير تقرير المخزون. راجع السجل الفني.", "خطأ", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void MovementsExportButton_Click(object? sender, EventArgs e)
        {
            try
            {
                var from = _fromDate.Value.Date;
                var to = _toDate.Value.Date.AddDays(1);
                var movements = _context.StockMovements.AsNoTracking()
                    .Include(value => value.InventoryItem)
                    .Where(value => value.MovementDate >= from && value.MovementDate < to)
                    .OrderByDescending(value => value.MovementDate)
                    .ToList()
                    .Select(value => new
                    {
                        value.MovementDate,
                        Item = value.InventoryItem.ItemName ?? value.InventoryItem.Name,
                        MovementType = value.MovementType.ToString(),
                        value.Quantity,
                        value.UnitCost,
                        value.TotalCost,
                        Reference = value.Reference ?? value.ReferenceNumber,
                        value.IsApproved
                    }).ToList();
                var path = new ExcelExportService().ExportListToExcel(movements, $"InventoryMovements_{DateTime.Now:yyyyMMdd_HHmmss}.xlsx", "Movements");
                MessageBox.Show($"تم تصدير التقرير إلى:\n{path}", "نجح", MessageBoxButtons.OK, MessageBoxIcon.Information);
            }
            catch (Exception ex)
            {
                LoggingService.LogError(ex, "Error exporting inventory movements");
                MessageBox.Show("تعذر تصدير حركات المخزون. راجع السجل الفني.", "خطأ", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }
    }
}
