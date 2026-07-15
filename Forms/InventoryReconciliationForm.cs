using FishFarmManager.Data;
using FishFarmManager.Models;
using FishFarmManager.Services;
using Microsoft.EntityFrameworkCore;

namespace FishFarmManager.Forms;

public sealed class InventoryReconciliationForm : AquaFarmBaseForm
{
    private readonly FishFarmContext _context;
    private readonly OperationalPostingService _posting;
    private readonly InventoryGeneralLedgerReconciliationService _reconciliation;
    private readonly ComboBox _period = new() { Width = 240, DropDownStyle = ComboBoxStyle.DropDownList };
    private readonly DateTimePicker _asOf = new() { Width = 150, Format = DateTimePickerFormat.Short };
    private readonly TextBox _reason = new() { Width = 380 };
    private readonly DataGridView _events = Grid();
    private readonly DataGridView _runs = Grid();

    public InventoryReconciliationForm(FishFarmContext context, OperationalPostingService posting,
        InventoryGeneralLedgerReconciliationService reconciliation)
    {
        _context = context;
        _posting = posting;
        _reconciliation = reconciliation;
        Text = "مطابقة المخزون مع الأستاذ العام";
        WindowState = FormWindowState.Maximized;
        MinimumSize = new Size(1100, 720);
        var postButton = ThemeManager.CreatePrimaryButton("إنشاء قيد تكلفة الإنتاج");
        var reconcileButton = ThemeManager.CreateSuccessButton("تشغيل مطابقة المخزون مع الأستاذ");
        postButton.Click += (_, _) => PostSelected();
        reconcileButton.Click += (_, _) => Reconcile();
        var tools = new FlowLayoutPanel
        {
            Dock = DockStyle.Top, Height = 58, FlowDirection = FlowDirection.RightToLeft, Padding = new Padding(10),
            Controls =
            {
                reconcileButton, postButton,
                new Label { Text = "حتى تاريخ", AutoSize = true, Padding = new Padding(4, 8, 4, 0) }, _asOf,
                new Label { Text = "الفترة", AutoSize = true, Padding = new Padding(4, 8, 4, 0) }, _period,
                new Label { Text = "السبب", AutoSize = true, Padding = new Padding(4, 8, 4, 0) }, _reason
            }
        };
        var tabs = new TabControl { Dock = DockStyle.Fill };
        tabs.TabPages.Add(Page("أحداث تكلفة بانتظار القيد", _events));
        tabs.TabPages.Add(Page("نتائج المطابقة المحفوظة", _runs));
        Controls.Add(tabs);
        Controls.Add(tools);
        Load += (_, _) => LoadData();
    }

    private void LoadData()
    {
        var periods = _context.FiscalPeriods.AsNoTracking().Where(value => value.Status == FiscalPeriodStatus.Open)
            .OrderByDescending(value => value.StartDate)
            .Select(value => new Choice(value.Id, value.Name)).ToArray();
        _period.DataSource = periods;
        _period.DisplayMember = nameof(Choice.Text);
        _period.ValueMember = nameof(Choice.Id);
        var postedIds = _context.OperationalPostingRecords.AsNoTracking()
            .Where(value => value.EventType == PostingEventType.ProductionCostEventApproved)
            .Select(value => value.SourceEntityId).ToHashSet();
        _events.DataSource = _context.ProductionCostEvents.AsNoTracking()
            .Include(value => value.ProductionCycle).Include(value => value.Pond)
            .Where(value => value.Amount > 0m)
            .OrderBy(value => value.EventDate).ThenBy(value => value.Id).ToArray()
            .Where(value => !postedIds.Contains(value.Id.ToString()))
            .Select(value => new
            {
                value.Id, التاريخ = value.EventDate, النوع = value.EventType,
                الدورة = value.ProductionCycle.Name, الحوض = value.Pond.Name,
                القيمة = value.Amount, المرجع = value.Reference
            }).ToArray();
        _runs.DataSource = _context.InventoryLedgerReconciliations.AsNoTracking()
            .OrderByDescending(value => value.CreatedAtUtc).Select(value => new
            {
                value.Id, التاريخ = value.AsOfDate, النتيجة = value.IsPassed ? "مطابق" : "به فروق",
                استثناءات_الكمية = value.QuantityExceptionCount, استثناءات_التقييم = value.ValuationExceptionCount,
                مخزون_تشغيلي = value.InventorySubledgerValue, مخزون_الأستاذ = value.InventoryGeneralLedgerValue,
                فرق_المخزون = value.InventoryDifference, إنتاج_تحت_التشغيل = value.WorkInProgressSubledgerValue,
                إنتاج_الأستاذ = value.WorkInProgressGeneralLedgerValue, فرق_الإنتاج = value.WorkInProgressDifference,
                المستخدم = value.CreatedBy
            }).ToArray();
    }

    private void PostSelected()
    {
        try
        {
            if (_events.SelectedRows.Count == 0) throw new InvalidOperationException("اختر حدث تكلفة إنتاج.");
            var eventId = Convert.ToInt64(_events.SelectedRows[0].Cells["Id"].Value);
            var periodId = _period.SelectedValue is int value ? value : throw new InvalidOperationException("اختر فترة مفتوحة.");
            var journal = _posting.CreateProductionCostEventDraft(eventId, periodId, Actor(), Reason());
            LoadData();
            ThemeManager.ShowSuccess($"تم إنشاء القيد {journal.EntryNumber} كمسودة للمراجعة والاعتماد.", "نجح");
        }
        catch (Exception ex) { MessageBox.Show(ex.Message, "تعذر إنشاء القيد", MessageBoxButtons.OK, MessageBoxIcon.Warning); }
    }

    private void Reconcile()
    {
        try
        {
            var run = _reconciliation.Run(_asOf.Value, Actor(), Reason());
            LoadData();
            var message = run.IsPassed
                ? "المخزون والإنتاج تحت التشغيل مطابقان للأستاذ العام."
                : $"توجد فروق: كمية {run.QuantityExceptionCount}، تقييم {run.ValuationExceptionCount}، " +
                  $"مخزون {run.InventoryDifference:N2}، إنتاج تحت التشغيل {run.WorkInProgressDifference:N2}.";
            MessageBox.Show(message, run.IsPassed ? "مطابقة ناجحة" : "فروق تحتاج معالجة",
                MessageBoxButtons.OK, run.IsPassed ? MessageBoxIcon.Information : MessageBoxIcon.Warning);
        }
        catch (Exception ex) { MessageBox.Show(ex.Message, "تعذر تشغيل المطابقة", MessageBoxButtons.OK, MessageBoxIcon.Warning); }
    }

    private string Reason() => string.IsNullOrWhiteSpace(_reason.Text)
        ? throw new InvalidOperationException("سبب الإجراء إلزامي.") : _reason.Text.Trim();
    private static string Actor() => string.IsNullOrWhiteSpace(AuthenticationService.CurrentUsername)
        ? "SYSTEM" : AuthenticationService.CurrentUsername;
    private static TabPage Page(string title, Control content) { var page = new TabPage(title); page.Controls.Add(content); return page; }
    private static DataGridView Grid() => new() { Dock = DockStyle.Fill, ReadOnly = true,
        SelectionMode = DataGridViewSelectionMode.FullRowSelect, MultiSelect = false,
        AllowUserToAddRows = false, AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.DisplayedCells };
    private sealed record Choice(int Id, string Text);
}
