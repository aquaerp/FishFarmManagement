using FishFarmManager.Data;
using FishFarmManager.Models;
using FishFarmManager.Services;
using Microsoft.EntityFrameworkCore;

namespace FishFarmManager.Forms;

public sealed class OperationalTraceabilityForm : Form
{
    private readonly FishFarmContext _context;
    private readonly OperationalTraceabilityService _service;
    private readonly TextBox _actionReason = new() { Width = 500 };
    private readonly ComboBox _receivedItem = Combo();
    private readonly ComboBox _consumptionLot = Combo(), _consumptionMovement = Combo(), _consumptionCycle = Combo(), _consumptionPond = Combo();
    private readonly NumericUpDown _consumptionQuantity = Quantity();
    private readonly DateTimePicker _consumptionDate = Date();
    private readonly TextBox _consumptionReference = TextInput();
    private readonly ComboBox _harvestCycle = Combo(), _harvestPond = Combo();
    private readonly NumericUpDown _harvestQuantity = Quantity();
    private readonly DateTimePicker _harvestDate = Date();
    private readonly TextBox _harvestCode = TextInput(), _harvestReference = TextInput();
    private readonly ComboBox _saleLot = Combo(), _saleItem = Combo();
    private readonly NumericUpDown _saleQuantity = Quantity();
    private readonly DateTimePicker _saleDate = Date();
    private readonly TextBox _saleReference = TextInput();
    private readonly ComboBox _queryInputLot = Combo(), _querySalesItem = Combo();
    private readonly DataGridView _results = new()
    {
        Dock = DockStyle.Fill, ReadOnly = true, AutoGenerateColumns = true,
        SelectionMode = DataGridViewSelectionMode.FullRowSelect, AllowUserToAddRows = false, RowHeadersVisible = false
    };
    private readonly TabControl _tabs = new() { Dock = DockStyle.Fill };

    public OperationalTraceabilityForm(FishFarmContext context)
    {
        _context = context;
        _service = new OperationalTraceabilityService(context);
        Text = "التتبع التشغيلي للدفعات من المدخلات حتى البيع";
        Width = 1250;
        Height = 760;
        StartPosition = FormStartPosition.CenterScreen;
        RightToLeft = RightToLeft.Yes;
        RightToLeftLayout = true;
        BuildLayout();
        LoadLookups();
        ApplyPermissions();
    }

    private void BuildLayout()
    {
        var reasonBar = new FlowLayoutPanel { Dock = DockStyle.Top, Height = 48, Padding = new Padding(8), FlowDirection = FlowDirection.RightToLeft };
        reasonBar.Controls.Add(_actionReason);
        reasonBar.Controls.Add(new Label { Text = "سبب الإجراء:", AutoSize = true, Padding = new Padding(8) });
        _tabs.TabPages.Add(CreateInputLotTab());
        _tabs.TabPages.Add(CreateConsumptionTab());
        _tabs.TabPages.Add(CreateHarvestTab());
        _tabs.TabPages.Add(CreateSaleTab());
        _tabs.TabPages.Add(CreateSearchTab());
        Controls.Add(_tabs);
        Controls.Add(reasonBar);
    }

    private TabPage CreateInputLotTab()
    {
        var page = Page("دفعات الاستلام");
        var panel = Bar();
        AddField(panel, "بند استلام معتمد غير مسجل", _receivedItem);
        var register = ThemeManager.CreateSuccessButton("تسجيل دفعة المدخل");
        register.Click += (_, _) => Execute(() =>
        {
            if (_receivedItem.SelectedValue is not int id) throw new InvalidOperationException("حدد بند الاستلام.");
            _service.RegisterReceivedInputLot(id, Actor(), Reason());
        });
        panel.Controls.Add(register);
        page.Controls.Add(panel);
        return page;
    }

    private TabPage CreateConsumptionTab()
    {
        var page = Page("ربط الاستهلاك بالدورة والحوض");
        var panel = Bar();
        AddField(panel, "دفعة المدخل", _consumptionLot);
        AddField(panel, "حركة الصرف", _consumptionMovement);
        AddField(panel, "الدورة", _consumptionCycle);
        AddField(panel, "الحوض", _consumptionPond);
        AddField(panel, "الكمية", _consumptionQuantity);
        AddField(panel, "التاريخ", _consumptionDate);
        AddField(panel, "المرجع", _consumptionReference);
        var save = ThemeManager.CreateSuccessButton("ربط استهلاك الدفعة");
        save.Click += (_, _) => Execute(() =>
        {
            _service.AllocateInputConsumption(Value<long>(_consumptionLot), Value<int>(_consumptionMovement), Value<int>(_consumptionCycle),
                Value<int>(_consumptionPond), _consumptionQuantity.Value, _consumptionDate.Value, _consumptionReference.Text, Actor(), Reason());
        });
        panel.Controls.Add(save);
        page.Controls.Add(panel);
        return page;
    }

    private TabPage CreateHarvestTab()
    {
        var page = Page("تسجيل دفعة الحصاد");
        var panel = Bar();
        AddField(panel, "رمز دفعة الحصاد", _harvestCode);
        AddField(panel, "الدورة", _harvestCycle);
        AddField(panel, "الحوض", _harvestPond);
        AddField(panel, "الكمية كجم", _harvestQuantity);
        AddField(panel, "التاريخ", _harvestDate);
        AddField(panel, "مرجع الحصاد", _harvestReference);
        var save = ThemeManager.CreateSuccessButton("تسجيل دفعة الحصاد");
        save.Click += (_, _) => Execute(() =>
        {
            _service.RegisterHarvestLot(_harvestCode.Text, Value<int>(_harvestCycle), Value<int>(_harvestPond), _harvestQuantity.Value,
                _harvestDate.Value, _harvestReference.Text, Actor(), Reason());
        });
        panel.Controls.Add(save);
        page.Controls.Add(panel);
        return page;
    }

    private TabPage CreateSaleTab()
    {
        var page = Page("ربط الحصاد بالبيع");
        var panel = Bar();
        AddField(panel, "دفعة الحصاد", _saleLot);
        AddField(panel, "بند البيع المكتمل", _saleItem);
        AddField(panel, "الكمية", _saleQuantity);
        AddField(panel, "التاريخ", _saleDate);
        AddField(panel, "المرجع", _saleReference);
        var save = ThemeManager.CreateSuccessButton("ربط دفعة الحصاد بالبيع");
        save.Click += (_, _) => Execute(() =>
        {
            _service.AllocateHarvestSale(Value<long>(_saleLot), Value<int>(_saleItem), _saleQuantity.Value,
                _saleDate.Value, _saleReference.Text, Actor(), Reason());
        });
        panel.Controls.Add(save);
        page.Controls.Add(panel);
        return page;
    }

    private TabPage CreateSearchTab()
    {
        var page = Page("الاستعلام الأمامي والخلفي");
        var top = Bar();
        AddField(top, "دفعة المدخل", _queryInputLot);
        var forward = ThemeManager.CreatePrimaryButton("تتبع أمامي حتى البيع");
        forward.Click += (_, _) => Query(() => _service.TraceForward(_queryInputLot.Text));
        top.Controls.Add(forward);
        AddField(top, "بند البيع", _querySalesItem);
        var backward = ThemeManager.CreatePrimaryButton("تتبع خلفي حتى المدخلات");
        backward.Click += (_, _) => Query(() => _service.TraceBackward(Value<int>(_querySalesItem)));
        top.Controls.Add(backward);
        page.Controls.Add(_results);
        page.Controls.Add(top);
        return page;
    }

    private void LoadLookups()
    {
        Bind(_receivedItem, _context.PurchaseReceivingItems.AsNoTracking()
            .Where(value => value.AddedToStock && value.BatchNumber != null
                && !_context.TraceabilityLots.Any(lot => lot.PurchaseReceivingItemId == value.Id))
            .OrderBy(value => value.BatchNumber).Select(value => new Lookup<int>(value.Id, $"{value.BatchNumber} - {value.ItemName}")).ToArray());
        var inputLots = _context.TraceabilityLots.AsNoTracking().Where(value => value.Kind == TraceabilityLotKind.Input)
            .OrderBy(value => value.LotCode).Select(value => new Lookup<long>(value.Id, value.LotCode)).ToArray();
        Bind(_consumptionLot, inputLots);
        Bind(_queryInputLot, inputLots.ToArray());
        var harvestLots = _context.TraceabilityLots.AsNoTracking().Where(value => value.Kind == TraceabilityLotKind.Harvest)
            .OrderBy(value => value.LotCode).Select(value => new Lookup<long>(value.Id, value.LotCode)).ToArray();
        Bind(_saleLot, harvestLots);
        Bind(_consumptionMovement, _context.StockMovements.AsNoTracking().Where(value => value.IsApproved && !value.IsRejected && !value.IsCancelled
                && value.MovementType == StockMovementType.Consumption)
            .OrderByDescending(value => value.Id).Select(value => new Lookup<int>(value.Id, $"{value.Reference} - {value.Quantity:N3}")).ToArray());
        var cycles = _context.ProductionCycles.AsNoTracking().Where(value => value.Status == CycleStatus.Active || value.Status == CycleStatus.Completed)
            .OrderBy(value => value.Name).Select(value => new Lookup<int>(value.Id, value.Name)).ToArray();
        Bind(_consumptionCycle, cycles);
        Bind(_harvestCycle, cycles.ToArray());
        var ponds = _context.Ponds.AsNoTracking().OrderBy(value => value.Name)
            .Select(value => new Lookup<int>(value.Id, value.Name)).ToArray();
        Bind(_consumptionPond, ponds);
        Bind(_harvestPond, ponds.ToArray());
        var sales = _context.SalesOrderItems.AsNoTracking().Where(value => value.SalesOrder.Status == SalesOrderStatus.Completed)
            .OrderByDescending(value => value.Id)
            .Select(value => new Lookup<int>(value.Id, $"{value.SalesOrder.OrderNumber} - {value.ProductName}")).ToArray();
        Bind(_saleItem, sales);
        Bind(_querySalesItem, sales.ToArray());
    }

    private void Execute(Action action)
    {
        try { action(); LoadLookups(); ThemeManager.ShowSuccess("تم حفظ رابط التتبع بنجاح.", "نجح"); }
        catch (Exception ex) { ThemeManager.ShowError(ex.Message, "تعذر حفظ التتبع"); }
    }

    private void Query(Func<IReadOnlyList<TraceabilityRoute>> query)
    {
        try { _results.DataSource = query().ToArray(); }
        catch (Exception ex) { ThemeManager.ShowError(ex.Message, "تعذر تنفيذ التتبع"); }
    }

    private void ApplyPermissions()
    {
        var canOperate = AuthenticationService.HasPermission(UserRole.Admin, UserRole.Manager, UserRole.InventoryStaff, UserRole.ProductionStaff);
        for (var index = 0; index < 4; index++) _tabs.TabPages[index].Enabled = canOperate;
        _actionReason.ReadOnly = !canOperate;
    }

    private string Reason() => string.IsNullOrWhiteSpace(_actionReason.Text)
        ? throw new InvalidOperationException("أدخل سبب الإجراء.") : _actionReason.Text.Trim();
    private static string Actor() => string.IsNullOrWhiteSpace(AuthenticationService.CurrentUsername) ? "system-user" : AuthenticationService.CurrentUsername;
    private static T Value<T>(ComboBox combo) => combo.SelectedValue is T value ? value : throw new InvalidOperationException("أكمل الحقول المطلوبة.");

    private static void Bind<T>(ComboBox combo, Lookup<T>[] values)
    {
        combo.DataSource = values;
        combo.DisplayMember = nameof(Lookup<T>.Text);
        combo.ValueMember = nameof(Lookup<T>.Id);
    }

    private static void AddField(FlowLayoutPanel panel, string label, Control control)
    {
        panel.Controls.Add(new Label { Text = label, AutoSize = true, Padding = new Padding(8) });
        panel.Controls.Add(control);
    }

    private static ComboBox Combo() => new() { DropDownStyle = ComboBoxStyle.DropDownList, Width = 220 };
    private static NumericUpDown Quantity() => new() { DecimalPlaces = 3, Maximum = 999999999m, Minimum = 0.001m, Width = 140 };
    private static DateTimePicker Date() => new() { Format = DateTimePickerFormat.Short, Width = 140 };
    private static TextBox TextInput() => new() { Width = 180 };
    private static FlowLayoutPanel Bar() => new() { Dock = DockStyle.Top, Height = 150, AutoScroll = true, Padding = new Padding(10), FlowDirection = FlowDirection.RightToLeft, WrapContents = true };
    private static TabPage Page(string text) => new(text) { Padding = new Padding(8) };
    private sealed record Lookup<T>(T Id, string Text);
}
