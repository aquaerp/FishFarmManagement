using FishFarmManager.Data;
using FishFarmManager.Models;
using FishFarmManager.Services;
using Microsoft.EntityFrameworkCore;

namespace FishFarmManager.Forms;

public sealed class InventoryCountForm : Form
{
    private readonly FishFarmContext _context;
    private readonly InventoryCountService _service;
    private readonly ComboBox _type = new() { DropDownStyle = ComboBoxStyle.DropDownList };
    private readonly ComboBox _item = new() { DropDownStyle = ComboBoxStyle.DropDownList };
    private readonly ComboBox _period = new() { DropDownStyle = ComboBoxStyle.DropDownList };
    private readonly DateTimePicker _date = new() { Format = DateTimePickerFormat.Short };
    private readonly TextBox _reference = new();
    private readonly TextBox _actionReason = new();
    private readonly NumericUpDown _actual = new() { DecimalPlaces = 3, Maximum = 999999999m, ThousandsSeparator = true };
    private readonly TextBox _varianceReason = new() { Width = 300 };
    private readonly DataGridView _counts = CreateGrid();
    private readonly DataGridView _lines = CreateGrid();
    private readonly Button _create = ThemeManager.CreateSuccessButton("إنشاء مسودة جرد");
    private readonly Button _saveLine = ThemeManager.CreatePrimaryButton("حفظ نتيجة الصنف");
    private readonly Button _submit = ThemeManager.CreatePrimaryButton("إرسال للاعتماد");
    private readonly Button _approve = ThemeManager.CreateSuccessButton("اعتماد وتطبيق وقيد");
    private readonly Button _reject = ThemeManager.CreateSecondaryButton("رفض الجرد");
    private long _countId;
    private int _itemId;

    public InventoryCountForm(FishFarmContext context)
    {
        _context = context;
        _service = new InventoryCountService(context);
        Text = "الجرد الدوري والشامل وفروق المخزون";
        Width = 1250;
        Height = 780;
        StartPosition = FormStartPosition.CenterScreen;
        RightToLeft = RightToLeft.Yes;
        RightToLeftLayout = true;
        BuildLayout();
        LoadLookups();
        LoadCounts();
        ApplyPermissions();
    }

    private void BuildLayout()
    {
        var inputs = new TableLayoutPanel { Dock = DockStyle.Top, Height = 112, Padding = new Padding(10), ColumnCount = 8, RowCount = 2 };
        for (var index = 0; index < 8; index++) inputs.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 12.5f));
        AddField(inputs, "نوع الجرد", _type, 0, 0);
        AddField(inputs, "صنف الدوري", _item, 2, 0);
        AddField(inputs, "تاريخ الجرد", _date, 4, 0);
        AddField(inputs, "الفترة المحاسبية", _period, 6, 0);
        AddField(inputs, "المرجع", _reference, 0, 1);
        inputs.Controls.Add(new Label { Text = "سبب الإجراء", Dock = DockStyle.Fill, TextAlign = ContentAlignment.MiddleLeft }, 2, 1);
        inputs.Controls.Add(_actionReason, 3, 1);
        inputs.SetColumnSpan(_actionReason, 5);
        _actionReason.Dock = DockStyle.Fill;
        _type.DataSource = Enum.GetValues<InventoryCountType>();
        _type.SelectedIndexChanged += (_, _) => _item.Enabled = _type.SelectedItem is InventoryCountType.Periodic;

        AddCountColumns();
        AddLineColumns();
        _counts.SelectionChanged += (_, _) => SelectCount();
        _lines.SelectionChanged += (_, _) => SelectLine();
        var split = new SplitContainer { Dock = DockStyle.Fill, Orientation = Orientation.Horizontal, SplitterDistance = 230 };
        split.Panel1.Controls.Add(_counts);
        split.Panel2.Controls.Add(_lines);

        var entryBar = new FlowLayoutPanel { Dock = DockStyle.Bottom, Height = 52, Padding = new Padding(8), FlowDirection = FlowDirection.RightToLeft };
        entryBar.Controls.AddRange([
            _saveLine,
            new Label { Text = "سبب الفرق:", AutoSize = true, Padding = new Padding(8) }, _varianceReason,
            new Label { Text = "الكمية الفعلية:", AutoSize = true, Padding = new Padding(8) }, _actual
        ]);
        _actual.Width = 130;

        var buttons = new FlowLayoutPanel { Dock = DockStyle.Bottom, Height = 58, Padding = new Padding(8), FlowDirection = FlowDirection.RightToLeft };
        var refresh = ThemeManager.CreateSecondaryButton("تحديث البيانات");
        buttons.Controls.AddRange([_create, _submit, _approve, _reject, refresh]);
        _create.Click += (_, _) => Execute(CreateDraft);
        _saveLine.Click += (_, _) => Execute(SaveLine);
        _submit.Click += (_, _) => Execute(Submit);
        _approve.Click += (_, _) => Execute(Approve);
        _reject.Click += (_, _) => Execute(Reject);
        refresh.Click += (_, _) => { LoadLookups(); LoadCounts(); };

        Controls.Add(split);
        Controls.Add(entryBar);
        Controls.Add(buttons);
        Controls.Add(inputs);
    }

    private void AddCountColumns()
    {
        _counts.Columns.Add(new DataGridViewTextBoxColumn { Name = "Id", DataPropertyName = "Id", Visible = false });
        _counts.Columns.Add(new DataGridViewTextBoxColumn { DataPropertyName = "Number", HeaderText = "رقم الجرد", Width = 190 });
        _counts.Columns.Add(new DataGridViewTextBoxColumn { DataPropertyName = "Type", HeaderText = "النوع", Width = 80 });
        _counts.Columns.Add(new DataGridViewTextBoxColumn { DataPropertyName = "Status", HeaderText = "الحالة", Width = 120 });
        _counts.Columns.Add(new DataGridViewTextBoxColumn { DataPropertyName = "Date", HeaderText = "التاريخ", Width = 100, DefaultCellStyle = new DataGridViewCellStyle { Format = "yyyy-MM-dd" } });
        _counts.Columns.Add(new DataGridViewTextBoxColumn { DataPropertyName = "Reference", HeaderText = "المرجع", Width = 160 });
        _counts.Columns.Add(new DataGridViewTextBoxColumn { DataPropertyName = "CreatedBy", HeaderText = "المنشئ", Width = 110 });
        _counts.Columns.Add(new DataGridViewTextBoxColumn { DataPropertyName = "ApprovedBy", HeaderText = "المعتمد", Width = 110 });
    }

    private void AddLineColumns()
    {
        _lines.Columns.Add(new DataGridViewTextBoxColumn { Name = "ItemId", DataPropertyName = "ItemId", Visible = false });
        _lines.Columns.Add(new DataGridViewTextBoxColumn { DataPropertyName = "Item", HeaderText = "الصنف", Width = 190 });
        AddNumberColumn("Book", "الدفتري", "N3");
        AddNumberColumn("Actual", "الفعلي", "N3");
        AddNumberColumn("Variance", "الفرق", "N3");
        AddNumberColumn("Cost", "التكلفة", "N2");
        AddNumberColumn("Value", "قيمة الفرق", "N2");
        _lines.Columns.Add(new DataGridViewTextBoxColumn { DataPropertyName = "Reason", HeaderText = "سبب الفرق", Width = 220 });
        _lines.Columns.Add(new DataGridViewTextBoxColumn { DataPropertyName = "Journal", HeaderText = "القيد", Width = 90 });
    }

    private void AddNumberColumn(string property, string header, string format) =>
        _lines.Columns.Add(new DataGridViewTextBoxColumn
        {
            DataPropertyName = property, HeaderText = header, Width = 90,
            DefaultCellStyle = new DataGridViewCellStyle { Format = format, Alignment = DataGridViewContentAlignment.MiddleRight }
        });

    private void LoadLookups()
    {
        _item.DataSource = _context.InventoryItems.AsNoTracking().Where(value => value.IsActive)
            .OrderBy(value => value.Name).Select(value => new { value.Id, value.Name }).ToArray();
        _item.DisplayMember = "Name";
        _item.ValueMember = "Id";
        _period.DataSource = _context.FiscalPeriods.AsNoTracking().Where(value => value.Status == FiscalPeriodStatus.Open)
            .OrderByDescending(value => value.StartDate).Select(value => new { value.Id, value.Name }).ToArray();
        _period.DisplayMember = "Name";
        _period.ValueMember = "Id";
    }

    private void LoadCounts()
    {
        _counts.DataSource = _context.InventoryCounts.AsNoTracking().OrderByDescending(value => value.Id)
            .Select(value => new
            {
                value.Id, Number = value.CountNumber,
                Type = value.CountType == InventoryCountType.Full ? "شامل" : "دوري",
                Status = StatusText(value.Status), Date = value.CountDate,
                value.Reference, value.CreatedBy, value.ApprovedBy
            }).ToArray();
        if (_countId != 0) SelectCountById(_countId);
    }

    private void SelectCount()
    {
        if (_counts.SelectedRows.Count == 0 || _counts.SelectedRows[0].Cells["Id"].Value is not long id) return;
        _countId = id;
        _lines.DataSource = _context.InventoryCountLines.AsNoTracking().Where(value => value.InventoryCountId == id)
            .OrderBy(value => value.InventoryItem.Name).Select(value => new
            {
                ItemId = value.InventoryItemId, Item = value.InventoryItem.Name,
                Book = value.BookQuantitySnapshot, Actual = value.ActualQuantity, Variance = value.VarianceQuantity,
                Cost = value.UnitCostSnapshot, Value = value.VarianceValue, Reason = value.VarianceReason,
                Journal = value.JournalEntryId
            }).ToArray();
        UpdateActions();
    }

    private void SelectLine()
    {
        if (_lines.SelectedRows.Count == 0 || _lines.SelectedRows[0].Cells["ItemId"].Value is not int id) return;
        _itemId = id;
        var line = _context.InventoryCountLines.AsNoTracking().Single(value => value.InventoryCountId == _countId && value.InventoryItemId == id);
        _actual.Value = Math.Min(_actual.Maximum, line.ActualQuantity ?? line.BookQuantitySnapshot);
        _varianceReason.Text = line.VarianceReason ?? string.Empty;
    }

    private void CreateDraft()
    {
        if (_type.SelectedItem is not InventoryCountType type) throw new InvalidOperationException("حدد نوع الجرد.");
        IReadOnlyCollection<int>? ids = null;
        if (type == InventoryCountType.Periodic)
        {
            if (_item.SelectedValue is not int id) throw new InvalidOperationException("حدد صنف الجرد الدوري.");
            ids = [id];
        }
        var count = _service.CreateDraft(new InventoryCountDraftRequest(type, _date.Value, _reference.Text, ids), Actor(), Reason());
        _countId = count.Id;
        LoadCounts();
    }

    private void SaveLine()
    {
        EnsureLine();
        _service.RecordActualQuantity(_countId, _itemId, _actual.Value, _varianceReason.Text, Actor(), Reason());
        SelectCount();
    }

    private void Submit() { EnsureCount(); _service.Submit(_countId, Actor(), Reason()); LoadCounts(); }

    private void Approve()
    {
        EnsureCount();
        if (_period.SelectedValue is not int periodId) throw new InvalidOperationException("حدد فترة محاسبية مفتوحة.");
        if (!ThemeManager.Confirm("سيتم تطبيق فروق الجرد وترحيل قيودها تلقائيًا. هل تريد المتابعة؟", "اعتماد الجرد"))
            throw new OperationCanceledException();
        _service.Approve(_countId, periodId, Actor(), Reason());
        LoadCounts();
    }

    private void Reject() { EnsureCount(); _service.Reject(_countId, Actor(), Reason()); LoadCounts(); }

    private void Execute(Action action)
    {
        try { action(); ThemeManager.ShowSuccess("تم تنفيذ العملية بنجاح.", "نجح"); }
        catch (OperationCanceledException) { }
        catch (Exception ex) { ThemeManager.ShowError(ex.Message, "تعذر تنفيذ العملية"); }
    }

    private void ApplyPermissions()
    {
        _create.Enabled = AuthenticationService.HasPermission(UserRole.Admin, UserRole.Manager, UserRole.Accountant, UserRole.InventoryStaff);
        UpdateActions();
    }

    private void UpdateActions()
    {
        var count = _countId == 0 ? null : _context.InventoryCounts.AsNoTracking().SingleOrDefault(value => value.Id == _countId);
        var owner = count != null && string.Equals(count.CreatedBy, Actor(), StringComparison.OrdinalIgnoreCase);
        var allowed = AuthenticationService.HasPermission(UserRole.Admin, UserRole.Manager, UserRole.Accountant, UserRole.InventoryStaff);
        _saveLine.Enabled = allowed && owner && count?.Status == InventoryCountStatus.Draft;
        _submit.Enabled = allowed && owner && count?.Status == InventoryCountStatus.Draft;
        _approve.Enabled = allowed && !owner && count?.Status == InventoryCountStatus.Submitted;
        _reject.Enabled = allowed && !owner && count?.Status == InventoryCountStatus.Submitted;
    }

    private void SelectCountById(long id)
    {
        foreach (DataGridViewRow row in _counts.Rows)
            if (row.Cells["Id"].Value is long rowId && rowId == id) { row.Selected = true; break; }
    }

    private void EnsureCount() { if (_countId == 0) throw new InvalidOperationException("حدد جردًا أولًا."); }
    private void EnsureLine() { EnsureCount(); if (_itemId == 0) throw new InvalidOperationException("حدد صنفًا."); }
    private string Reason() => string.IsNullOrWhiteSpace(_actionReason.Text) ? throw new InvalidOperationException("أدخل سبب الإجراء.") : _actionReason.Text.Trim();
    private static string Actor() => string.IsNullOrWhiteSpace(AuthenticationService.CurrentUsername) ? "system-user" : AuthenticationService.CurrentUsername;

    private static void AddField(TableLayoutPanel panel, string label, Control control, int column, int row)
    {
        panel.Controls.Add(new Label { Text = label, Dock = DockStyle.Fill, TextAlign = ContentAlignment.MiddleLeft }, column, row);
        control.Dock = DockStyle.Fill;
        panel.Controls.Add(control, column + 1, row);
    }

    private static DataGridView CreateGrid() => new()
    {
        Dock = DockStyle.Fill, ReadOnly = true, AutoGenerateColumns = false,
        SelectionMode = DataGridViewSelectionMode.FullRowSelect, MultiSelect = false,
        AllowUserToAddRows = false, AllowUserToDeleteRows = false, RowHeadersVisible = false
    };

    private static string StatusText(InventoryCountStatus status) => status switch
    {
        InventoryCountStatus.Draft => "مسودة",
        InventoryCountStatus.Submitted => "بانتظار الاعتماد",
        InventoryCountStatus.Approved => "معتمد ومرحّل",
        InventoryCountStatus.Rejected => "مرفوض",
        _ => status.ToString()
    };
}
