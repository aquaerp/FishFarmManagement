using FishFarmManager.Data;
using FishFarmManager.Models;
using FishFarmManager.Services;
using Microsoft.EntityFrameworkCore;

namespace FishFarmManager.Forms;

public sealed class ProductionCostingForm : AquaFarmBaseForm
{
    private readonly FishFarmContext _context;
    private readonly ProductionCostingService _service;
    private readonly TextBox _reason = new() { Width = 420 };
    private readonly TabControl _tabs = new() { Dock = DockStyle.Fill };
    private readonly DataGridView _grid = Grid();

    public ProductionCostingForm(FishFarmContext context, ProductionCostingService service)
    {
        _context = context;
        _service = service;
        Text = "تكلفة الإنتاج والنفوق والتحويل والحصاد";
        WindowState = FormWindowState.Maximized;
        MinimumSize = new Size(1100, 720);
        Controls.Add(_tabs);
        Controls.Add(new FlowLayoutPanel
        {
            Dock = DockStyle.Top, Height = 52, FlowDirection = FlowDirection.RightToLeft, Padding = new Padding(12),
            Controls = { new Label { Text = "سبب الإجراء:", AutoSize = true, Padding = new Padding(0, 7, 0, 0) }, _reason }
        });
        BuildConsumptionTab();
        BuildDirectCostTab();
        BuildMortalityTab();
        BuildTransferTab();
        BuildHarvestTab();
        BuildLedgerTab();
        Load += (_, _) => RefreshLedger();
    }

    private void BuildConsumptionTab()
    {
        ComboBox movement = Combo(), pond = Combo();
        var panel = Panel();
        Field(panel, "حركة استهلاك معتمدة", movement);
        Field(panel, "الحوض", pond);
        var button = ThemeManager.CreateSuccessButton("تحميل استهلاك المدخلات على الإنتاج");
        panel.Controls.Add(button);
        _tabs.TabPages.Add(Page("استهلاك المدخلات", panel));
        movement.DataSource = _context.StockMovements.AsNoTracking()
            .Where(value => value.IsApproved && value.MovementType == StockMovementType.Consumption)
            .Select(value => new Choice<int>(value.Id, (value.Reference ?? value.Id.ToString()) + $" — {value.TotalCost:N2} ر.س")).ToArray();
        BindPonds(pond);
        button.Click += (_, _) => Run(() => _service.CapitalizeInputConsumption(Id<int>(movement), Id<int>(pond), Actor(), Reason()));
    }

    private void BuildDirectCostTab()
    {
        var cost = Combo();
        var panel = Panel();
        Field(panel, "تكلفة مباشرة مرتبطة بدورة وحوض", cost);
        var button = ThemeManager.CreateSuccessButton("تحميل التكلفة المباشرة");
        panel.Controls.Add(button);
        _tabs.TabPages.Add(Page("التكاليف المباشرة", panel));
        cost.DataSource = _context.CostRecords.AsNoTracking()
            .Where(value => value.ProductionCycleId != null && value.PondId != null && value.Amount > 0)
            .Select(value => new Choice<int>(value.Id, value.Description + $" — {value.Amount:N2} ر.س")).ToArray();
        button.Click += (_, _) => Run(() => _service.CapitalizeDirectCost(Id<int>(cost), Actor(), Reason()));
    }

    private void BuildMortalityTab()
    {
        ComboBox mortality = Combo(), pond = Combo();
        var biomass = Number();
        var abnormal = new CheckBox { Text = "نفوق غير طبيعي — يفصل كخسارة", AutoSize = true };
        var reference = new TextBox { Width = 300 };
        var panel = Panel();
        Field(panel, "سجل النفوق المحلل/المغلق", mortality);
        Field(panel, "الحوض", pond);
        Field(panel, "الكتلة الحية قبل النفوق (كجم)", biomass);
        Field(panel, "مرجع القياس", reference);
        panel.Controls.Add(abnormal);
        var button = ThemeManager.CreateSuccessButton("احتساب تكلفة النفوق");
        panel.Controls.Add(button);
        _tabs.TabPages.Add(Page("تكلفة النفوق", panel));
        mortality.DataSource = _context.MortalityRecords.AsNoTracking()
            .Where(value => value.Status == MortalityStatus.Analyzed || value.Status == MortalityStatus.Closed)
            .Select(value => new Choice<int>(value.Id, $"#{value.Id} — {value.Date:yyyy-MM-dd} — {value.DeadFishCount} سمكة")).ToArray();
        BindPonds(pond);
        button.Click += (_, _) => Run(() => _service.CostMortality(Id<int>(mortality), Id<int>(pond), biomass.Value,
            abnormal.Checked, reference.Text, Actor(), Reason()));
    }

    private void BuildTransferTab()
    {
        ComboBox cycle = Combo(), source = Combo(), destination = Combo();
        NumericUpDown quantity = Number(), biomass = Number();
        var date = new DateTimePicker { Width = 180, Format = DateTimePickerFormat.Short };
        var reference = new TextBox { Width = 300 };
        var panel = Panel();
        Field(panel, "الدورة", cycle); Field(panel, "من حوض", source); Field(panel, "إلى حوض", destination);
        Field(panel, "الكتلة المحولة (كجم)", quantity); Field(panel, "كتلة المصدر قبل التحويل (كجم)", biomass);
        Field(panel, "التاريخ", date); Field(panel, "مرجع التحويل", reference);
        var button = ThemeManager.CreateSuccessButton("نقل تكلفة الإنتاج بين الأحواض");
        panel.Controls.Add(button);
        _tabs.TabPages.Add(Page("تحويل الأحواض", panel));
        BindCycles(cycle); BindPonds(source); BindPonds(destination);
        button.Click += (_, _) => Run(() => _service.TransferPondCost(Id<int>(cycle), Id<int>(source), Id<int>(destination),
            quantity.Value, biomass.Value, date.Value, reference.Text, Actor(), Reason()));
    }

    private void BuildHarvestTab()
    {
        ComboBox lot = Combo(), item = Combo();
        var biomass = Number();
        var final = new CheckBox { Text = "حصاد نهائي — رسملة كامل الرصيد المتبقي", AutoSize = true };
        var panel = Panel();
        Field(panel, "دفعة حصاد موثقة", lot); Field(panel, "صنف المنتج بالمخزون", item);
        Field(panel, "الكتلة المتاحة قبل الحصاد (كجم)", biomass); panel.Controls.Add(final);
        var button = ThemeManager.CreateSuccessButton("رسملة منتج الحصاد بالمخزون");
        panel.Controls.Add(button);
        _tabs.TabPages.Add(Page("منتج الحصاد", panel));
        lot.DataSource = _context.TraceabilityLots.AsNoTracking().Where(value => value.Kind == TraceabilityLotKind.Harvest)
            .Select(value => new Choice<long>(value.Id, value.LotCode + $" — {value.InitialQuantity:N3} كجم")).ToArray();
        item.DataSource = _context.InventoryItems.AsNoTracking().Where(value => value.IsActive
                && (value.Category == InventoryCategory.FreshFish || value.Category == InventoryCategory.FrozenFish))
            .Select(value => new Choice<int>(value.Id, value.Name)).ToArray();
        button.Click += (_, _) => Run(() => _service.CapitalizeHarvest(Id<long>(lot), Id<int>(item), biomass.Value,
            final.Checked, Actor(), Reason()));
    }

    private void BuildLedgerTab()
    {
        var refresh = ThemeManager.CreatePrimaryButton("تحديث دفتر تكلفة الإنتاج");
        refresh.Click += (_, _) => RefreshLedger();
        var page = new TabPage("دفتر التكلفة والأرصدة");
        page.Controls.Add(_grid); page.Controls.Add(new FlowLayoutPanel { Dock = DockStyle.Top, Height = 52, Controls = { refresh } });
        _tabs.TabPages.Add(page);
    }

    private void RefreshLedger()
    {
        _grid.DataSource = _context.ProductionCostEvents.AsNoTracking().Include(value => value.ProductionCycle)
            .Include(value => value.Pond).Include(value => value.DestinationPond)
            .OrderByDescending(value => value.EventDate).ThenByDescending(value => value.Id)
            .Select(value => new
            {
                value.Id, التاريخ = value.EventDate, النوع = value.EventType, الدورة = value.ProductionCycle.Name,
                الحوض = value.Pond.Name, الحوض_الوجهة = value.DestinationPond == null ? "" : value.DestinationPond.Name,
                الكمية_كجم = value.QuantityKg, المبلغ = value.Amount, المرجع = value.Reference,
                أساس_القياس = value.MeasurementBasis, المستخدم = value.CreatedBy
            }).ToArray();
    }

    private void Run(Action action)
    {
        try { action(); RefreshLedger(); ThemeManager.ShowSuccess("تم تسجيل حدث تكلفة الإنتاج وحفظ مسار التدقيق.", "نجح"); }
        catch (Exception ex) { MessageBox.Show(ex.Message, "تعذر التنفيذ", MessageBoxButtons.OK, MessageBoxIcon.Warning); }
    }

    private static TabPage Page(string title, Control content) { var page = new TabPage(title); page.Controls.Add(content); return page; }
    private static FlowLayoutPanel Panel() => new() { Dock = DockStyle.Fill, FlowDirection = FlowDirection.TopDown, Padding = new Padding(28), AutoScroll = true, WrapContents = false };
    private static ComboBox Combo() => new() { Width = 520, DropDownStyle = ComboBoxStyle.DropDownList, DisplayMember = nameof(Choice<int>.Text), ValueMember = nameof(Choice<int>.Id) };
    private static NumericUpDown Number() => new() { Width = 220, DecimalPlaces = 3, Maximum = 1_000_000_000m, Minimum = 0m };
    private static DataGridView Grid() => new() { Dock = DockStyle.Fill, ReadOnly = true, AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.DisplayedCells, AllowUserToAddRows = false };
    private static void Field(FlowLayoutPanel panel, string label, Control control) { panel.Controls.Add(new Label { Text = label, AutoSize = true, Margin = new Padding(3, 10, 3, 2) }); panel.Controls.Add(control); }
    private void BindPonds(ComboBox combo) => combo.DataSource = _context.Ponds.AsNoTracking().OrderBy(value => value.Name).Select(value => new Choice<int>(value.Id, value.Name)).ToArray();
    private void BindCycles(ComboBox combo) => combo.DataSource = _context.ProductionCycles.AsNoTracking().Where(value => value.Status == CycleStatus.Active || value.Status == CycleStatus.Completed).OrderBy(value => value.Name).Select(value => new Choice<int>(value.Id, value.Name)).ToArray();
    private static T Id<T>(ComboBox combo) => combo.SelectedValue is T value ? value : throw new InvalidOperationException("يرجى اختيار قيمة صحيحة.");
    private static string Actor() => string.IsNullOrWhiteSpace(AuthenticationService.CurrentUsername) ? "SYSTEM" : AuthenticationService.CurrentUsername;
    private string Reason() => string.IsNullOrWhiteSpace(_reason.Text) ? throw new InvalidOperationException("سبب الإجراء إلزامي.") : _reason.Text.Trim();
    private sealed record Choice<T>(T Id, string Text);
}
