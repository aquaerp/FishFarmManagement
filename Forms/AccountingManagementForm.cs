using System.Data;
using FishFarmManager.Data;
using FishFarmManager.Models;
using FishFarmManager.Services;
using Microsoft.EntityFrameworkCore;

namespace FishFarmManager.Forms;

public sealed class AccountingManagementForm : AquaFarmBaseForm
{
    private const string SystemFxActor = "SYSTEM:FOREIGN-CURRENCY-ENGINE";
    private readonly FishFarmContext _context;
    private readonly TextBox _reasonTextBox = new() { Width = 520 };
    private readonly Label _summaryLabel = new() { Dock = DockStyle.Fill, AutoSize = false, Padding = new Padding(20), Font = ThemeManager.SubtitleFont };
    private readonly DataGridView _journalsGrid = CreateGrid();
    private readonly DataGridView _periodsGrid = CreateGrid();
    private readonly DataGridView _yearsGrid = CreateGrid();
    private readonly DataGridView _adjustmentsGrid = CreateGrid();
    private readonly DataGridView _configurationGrid = CreateGrid();
    private readonly DataGridView _ratesGrid = CreateGrid();
    private readonly DataGridView _itemsGrid = CreateGrid();
    private readonly DateTimePicker _reversalDate = new() { Width = 130, Format = DateTimePickerFormat.Short };
    private readonly ComboBox _retainedEarningsAccount = CreateCombo(260);
    private readonly TextBox _nextYearName = new() { Width = 130 };
    private readonly DateTimePicker _nextYearStart = new() { Width = 130, Format = DateTimePickerFormat.Short };
    private readonly DateTimePicker _nextYearEnd = new() { Width = 130, Format = DateTimePickerFormat.Short };
    private readonly ComboBox _adjustmentPeriod = CreateCombo(230);
    private readonly DateTimePicker _adjustmentDate = new() { Width = 130, Format = DateTimePickerFormat.Short };
    private readonly ComboBox _adjustmentType = CreateCombo(170);
    private readonly TextBox _adjustmentDescription = new() { Width = 220 };
    private readonly TextBox _adjustmentEvidence = new() { Width = 190 };
    private readonly CheckBox _scheduleReversal = new() { Text = "عكس مجدول", AutoSize = true, Padding = new Padding(0, 8, 0, 0) };
    private readonly DateTimePicker _scheduledReversalDate = new() { Width = 130, Format = DateTimePickerFormat.Short };
    private readonly DateTimePicker _adjustmentReverseAsOf = new() { Width = 130, Format = DateTimePickerFormat.Short };
    private readonly TextBox _currencyText = new() { Width = 70, CharacterCasing = CharacterCasing.Upper, MaxLength = 3 };
    private readonly DateTimePicker _rateDate = new() { Width = 130, Format = DateTimePickerFormat.Short };
    private readonly ComboBox _ratePurpose = CreateCombo();
    private readonly NumericUpDown _rateValue = CreateAmount(8, 100_000_000m);
    private readonly TextBox _sourceText = new() { Width = 260 };
    private readonly TextBox _evidenceText = new() { Width = 260 };
    private readonly NumericUpDown _recognitionLineId = CreateAmount(0, 1_000_000_000_000m);
    private readonly ComboBox _itemKind = CreateCombo();
    private readonly TextBox _itemReference = new() { Width = 180 };
    private readonly DateTimePicker _settlementDate = new() { Width = 130, Format = DateTimePickerFormat.Short };
    private readonly NumericUpDown _settlementAmount = CreateAmount(8, 1_000_000_000m);
    private readonly ComboBox _settlementPeriod = CreateCombo(230);
    private readonly ComboBox _settlementRate = CreateCombo(270);
    private readonly ComboBox _cashAccount = CreateCombo(250);
    private readonly ComboBox _gainAccount = CreateCombo(250);
    private readonly ComboBox _lossAccount = CreateCombo(250);
    private readonly ComboBox _closingPeriod = CreateCombo(230);
    private readonly ComboBox _closingGainAccount = CreateCombo(250);
    private readonly ComboBox _closingLossAccount = CreateCombo(250);

    public AccountingManagementForm(FishFarmContext context)
    {
        _context = context;
        Text = "إدارة المحاسبة";
        WindowState = FormWindowState.Maximized;
        MinimumSize = new Size(1200, 760);
        if (!AuthenticationService.HasPermission(UserRole.Admin, UserRole.Manager, UserRole.Accountant))
        {
            Load += (_, _) =>
            {
                MessageBox.Show("ليس لديك صلاحية لإدارة المحاسبة.", "صلاحيات غير كافية",
                    MessageBoxButtons.OK, MessageBoxIcon.Warning);
                Close();
            };
            return;
        }
        BuildUi();
        Load += (_, _) => RefreshAll();
    }

    private void BuildUi()
    {
        Controls.Add(CreateTabs());
        Controls.Add(CreateCommandHeader());
        Controls.Add(CreateTitleBar("إدارة المحاسبة — الأستاذ العام والعملات الأجنبية"));
    }

    private Control CreateCommandHeader()
    {
        var panel = new FlowLayoutPanel
        {
            Dock = DockStyle.Top, Height = 55, Padding = new Padding(12, 10, 12, 5),
            FlowDirection = FlowDirection.RightToLeft, WrapContents = false, BackColor = ThemeManager.PureWhite
        };
        var refresh = ActionButton("تحديث", (_, _) => RefreshAll(), secondary: true);
        panel.Controls.Add(refresh);
        panel.Controls.Add(_reasonTextBox);
        panel.Controls.Add(new Label { Text = "سبب العملية / مرجع الموافقة:", AutoSize = true, Padding = new Padding(5, 7, 5, 0) });
        return panel;
    }

    private TabControl CreateTabs()
    {
        var tabs = new TabControl { Dock = DockStyle.Fill };
        tabs.TabPages.Add(CreateOverviewTab());
        tabs.TabPages.Add(CreateJournalsTab());
        tabs.TabPages.Add(CreatePeriodsTab());
        tabs.TabPages.Add(CreateFiscalYearsTab());
        tabs.TabPages.Add(CreateAdjustmentsTab());
        tabs.TabPages.Add(CreateConfigurationTab());
        tabs.TabPages.Add(CreateRatesTab());
        tabs.TabPages.Add(CreateForeignItemsTab());
        return tabs;
    }

    private TabPage CreateOverviewTab()
    {
        var page = new TabPage("نظرة عامة");
        page.Controls.Add(_summaryLabel);
        return page;
    }

    private TabPage CreateJournalsTab()
    {
        var page = new TabPage("القيود");
        var commands = CommandBar(
            ActionButton("اعتماد القيد", (_, _) => Run(() =>
                new GeneralLedgerService(_context).Approve(SelectedId(_journalsGrid), Actor(), Reason()))),
            ActionButton("ترحيل القيد", (_, _) => Run(() =>
                new GeneralLedgerService(_context).Post(SelectedId(_journalsGrid), Actor(), Reason()))),
            ActionButton("عكس القيد", (_, _) => Run(() =>
                new GeneralLedgerService(_context).Reverse(SelectedId(_journalsGrid), _reversalDate.Value.Date, Actor(), Reason()))),
            new Label { Text = "تاريخ العكس:", AutoSize = true, Padding = new Padding(5, 8, 5, 0) },
            _reversalDate);
        page.Controls.Add(_journalsGrid);
        page.Controls.Add(commands);
        return page;
    }

    private TabPage CreatePeriodsTab()
    {
        var page = new TabPage("الفترات المالية");
        page.Controls.Add(_periodsGrid);
        page.Controls.Add(CommandBar(ActionButton("إقفال الفترة المحددة", (_, _) => Run(() =>
            new GeneralLedgerService(_context).ClosePeriod((int)SelectedId(_periodsGrid), Actor(), Reason())))));
        return page;
    }

    private TabPage CreateFiscalYearsTab()
    {
        var page = new TabPage("السنوات المالية");
        _yearsGrid.SelectionChanged += (_, _) => SuggestNextFiscalYear();
        var commands = CommandBar(
            ActionButton("إنشاء أرصدة افتتاحية", (_, _) => CreateOpeningBalances()),
            Labeled("حساب الأرباح المبقاة", _retainedEarningsAccount),
            ActionButton("إنشاء قيد الإقفال", (_, _) => Run(() =>
                new FiscalYearClosingService(_context).CreateYearEndClosingDraft(
                    (int)SelectedId(_yearsGrid), SelectedLookup(_retainedEarningsAccount), Actor(), Reason()))),
            ActionButton("إقفال السنة", (_, _) => Run(() =>
                new FiscalYearClosingService(_context).CloseFiscalYear((int)SelectedId(_yearsGrid), Actor(), Reason()))),
            Labeled("اسم السنة التالية", _nextYearName), Labeled("البداية", _nextYearStart), Labeled("النهاية", _nextYearEnd),
            ActionButton("فتح السنة التالية", (_, _) => Run(() =>
                new FiscalYearClosingService(_context).CreateNextFiscalYearWithOpeningDraft(
                    (int)SelectedId(_yearsGrid), _nextYearName.Text, _nextYearStart.Value.Date,
                    _nextYearEnd.Value.Date, Actor(), Reason()))));
        commands.Height = 120;
        commands.WrapContents = true;
        page.Controls.Add(_yearsGrid);
        page.Controls.Add(commands);
        return page;
    }

    private TabPage CreateAdjustmentsTab()
    {
        _adjustmentType.DataSource = Enum.GetValues<AccountingAdjustmentType>();
        _scheduleReversal.CheckedChanged += (_, _) => _scheduledReversalDate.Enabled = _scheduleReversal.Checked;
        _scheduledReversalDate.Enabled = false;
        var page = new TabPage("التسويات");
        var commands = CommandBar(
            Labeled("الفترة", _adjustmentPeriod), Labeled("تاريخ القيد", _adjustmentDate),
            Labeled("نوع التسوية", _adjustmentType), Labeled("البيان", _adjustmentDescription),
            Labeled("المستند المؤيد", _adjustmentEvidence), _scheduleReversal,
            Labeled("تاريخ العكس", _scheduledReversalDate),
            ActionButton("إنشاء قيد تسوية", (_, _) => CreateAdjustment()),
            Labeled("العكس المستحق حتى", _adjustmentReverseAsOf),
            ActionButton("تنفيذ العكس المستحق", (_, _) => Run(() =>
                new AccountingAdjustmentService(_context).ReverseDue(
                    SelectedId(_adjustmentsGrid), _adjustmentReverseAsOf.Value.Date, Actor(), Reason()))));
        commands.Height = 120;
        commands.WrapContents = true;
        page.Controls.Add(_adjustmentsGrid);
        page.Controls.Add(commands);
        return page;
    }

    private TabPage CreateConfigurationTab()
    {
        var page = new TabPage("الإعداد المحاسبي");
        page.Controls.Add(_configurationGrid);
        page.Controls.Add(CommandBar(
            ActionButton("إنشاء/تحميل الخريطة الأولية", (_, _) => Run(() =>
                new AccountingConfigurationService(_context).CreatePilotDraft(Actor()))),
            ActionButton("اعتماد الخريطة المحددة", (_, _) => Run(() =>
                new AccountingConfigurationService(_context).Approve((int)SelectedId(_configurationGrid), Actor(), Reason())))));
        return page;
    }

    private TabPage CreateRatesTab()
    {
        _ratePurpose.DataSource = Enum.GetValues<ExchangeRatePurpose>();
        var page = new TabPage("أسعار الصرف");
        var entry = CommandBar(
            Labeled("العملة", _currencyText), Labeled("التاريخ", _rateDate), Labeled("الغرض", _ratePurpose),
            Labeled("SAR لكل وحدة", _rateValue), Labeled("المصدر الرسمي", _sourceText), Labeled("مرجع الدليل", _evidenceText),
            ActionButton("حفظ كمسودة", (_, _) => CreateRateDraft()),
            ActionButton("اعتماد السعر المحدد", (_, _) => Run(() =>
                new ForeignExchangeRateService(_context).Approve(SelectedId(_ratesGrid), Actor(), Reason()))));
        entry.Height = 105;
        entry.WrapContents = true;
        page.Controls.Add(_ratesGrid);
        page.Controls.Add(entry);
        return page;
    }

    private TabPage CreateForeignItemsTab()
    {
        _itemKind.DataSource = Enum.GetValues<ForeignMonetaryItemKind>();
        _settlementDate.ValueChanged += (_, _) => LoadSettlementRates();
        _itemsGrid.SelectionChanged += (_, _) => LoadSettlementRates();
        var page = new TabPage("البنود والعملات الأجنبية");
        var commands = new TableLayoutPanel { Dock = DockStyle.Bottom, Height = 235, ColumnCount = 1, RowCount = 3 };
        commands.RowStyles.Add(new RowStyle(SizeType.Absolute, 65));
        commands.RowStyles.Add(new RowStyle(SizeType.Absolute, 100));
        commands.RowStyles.Add(new RowStyle(SizeType.Absolute, 65));
        commands.Controls.Add(CommandBar(
            Labeled("معرف سطر الاعتراف المرحل", _recognitionLineId), Labeled("النوع", _itemKind),
            Labeled("المرجع", _itemReference), ActionButton("تسجيل بند نقدي", (_, _) => RegisterItem())), 0, 0);
        var settleBar = CommandBar(
            Labeled("تاريخ التسوية", _settlementDate), Labeled("المبلغ الأجنبي", _settlementAmount),
            Labeled("الفترة", _settlementPeriod), Labeled("سعر المعاملة", _settlementRate),
            Labeled("النقدية", _cashAccount), Labeled("ربح محقق", _gainAccount), Labeled("خسارة محققة", _lossAccount),
            ActionButton("تسوية البند المحدد", (_, _) => SettleItem()));
        settleBar.WrapContents = true;
        commands.Controls.Add(settleBar, 0, 1);
        commands.Controls.Add(CommandBar(
            Labeled("فترة الإقفال", _closingPeriod), Labeled("ربح تقييم", _closingGainAccount),
            Labeled("خسارة تقييم", _closingLossAccount),
            ActionButton("إعادة تقييم جميع البنود المفتوحة", (_, _) => RevalueAll())), 0, 2);
        page.Controls.Add(_itemsGrid);
        page.Controls.Add(commands);
        return page;
    }

    private void RefreshAll()
    {
        try
        {
            _context.ChangeTracker.Clear();
            LoadOverview();
            LoadJournals();
            LoadPeriods();
            LoadFiscalYears();
            LoadAdjustments();
            LoadConfigurations();
            LoadRates();
            LoadItems();
            LoadLookups();
        }
        catch (Exception ex) { ShowError(ex); }
    }

    private void LoadOverview()
    {
        var draft = _context.JournalEntries.Count(value => value.Status == JournalEntryStatus.Draft);
        var approved = _context.JournalEntries.Count(value => value.Status == JournalEntryStatus.Approved);
        var posted = _context.JournalEntries.Count(value => value.Status == JournalEntryStatus.Posted);
        var openPeriods = _context.FiscalPeriods.Count(value => value.Status == FiscalPeriodStatus.Open);
        var approvedRates = _context.ForeignExchangeRates.Count(value => value.Status == ExchangeRateStatus.Approved);
        var openItems = _context.ForeignMonetaryItems.Count(value => value.Status == ForeignMonetaryItemStatus.Open);
        var missingClose = _context.ForeignMonetaryItems.Count(value => value.Status == ForeignMonetaryItemStatus.Open
            && _context.FiscalPeriods.Any(period => period.Status == FiscalPeriodStatus.Open
                && period.EndDate >= value.LastMeasurementDate && period.EndDate != value.LastMeasurementDate));
        _summaryLabel.Text = $"""
            حالة النواة المحاسبية

            القيود المسودة: {draft:N0}        القيود المعتمدة غير المرحلة: {approved:N0}        القيود المرحلة: {posted:N0}

            الفترات المفتوحة: {openPeriods:N0}        أسعار الصرف المعتمدة: {approvedRates:N0}

            البنود النقدية الأجنبية المفتوحة: {openItems:N0}        بنود تحتاج متابعة إقفال: {missingClose:N0}

            العملة الوظيفية وعملة العرض: SAR
            """;
    }

    private void LoadJournals() => _journalsGrid.DataSource = _context.JournalEntries.AsNoTracking()
        .OrderByDescending(value => value.SequenceNumber).Take(500)
        .Select(value => new
        {
            value.Id, value.EntryNumber, value.EntryDate, value.Description, value.Source,
            Status = value.Status.ToString(), value.CreatedBy, value.ApprovedBy, value.PostedBy
        }).ToList();

    private void LoadPeriods() => _periodsGrid.DataSource = _context.FiscalPeriods.AsNoTracking()
        .OrderByDescending(value => value.StartDate).Select(value => new
        {
            value.Id, value.Name, value.StartDate, value.EndDate, Status = value.Status.ToString(), value.ClosedBy
        }).ToList();

    private void LoadFiscalYears() => _yearsGrid.DataSource = _context.FiscalYears.AsNoTracking()
        .OrderByDescending(value => value.StartDate).Select(value => new
        {
            value.Id, value.Name, value.StartDate, value.EndDate, value.IsClosed,
            value.OpeningBalanceJournalEntryId, value.ClosingJournalEntryId, value.ClosedBy
        }).ToList();

    private void LoadAdjustments() => _adjustmentsGrid.DataSource = _context.AccountingAdjustments.AsNoTracking()
        .OrderByDescending(value => value.Id).Select(value => new
        {
            value.Id, value.JournalEntry.EntryNumber, Type = value.Type.ToString(),
            value.SupportingDocumentReference, value.ScheduledReversalDate,
            JournalStatus = value.JournalEntry.Status.ToString(), value.ReversalJournalEntryId,
            value.CreatedBy, value.CreatedAtUtc
        }).ToList();

    private void LoadConfigurations() => _configurationGrid.DataSource = _context.AccountingConfigurations.AsNoTracking()
        .OrderByDescending(value => value.Version).Select(value => new
        {
            value.Id, value.Name, value.Version, Status = value.Status.ToString(), value.CreatedBy, value.ApprovedBy
        }).ToList();

    private void LoadRates() => _ratesGrid.DataSource = _context.ForeignExchangeRates.AsNoTracking()
        .OrderByDescending(value => value.RateDate).ThenBy(value => value.CurrencyCode)
        .Select(value => new
        {
            value.Id, value.CurrencyCode, value.RateDate, Purpose = value.Purpose.ToString(), value.Version,
            value.SarPerUnit, Status = value.Status.ToString(), value.SourceReference, value.EvidenceReference,
            value.CreatedBy, value.ApprovedBy
        }).ToList();

    private void LoadItems() => _itemsGrid.DataSource = _context.ForeignMonetaryItems.AsNoTracking()
        .OrderBy(value => value.Status).ThenBy(value => value.CurrencyCode).ThenBy(value => value.Reference)
        .Select(value => new
        {
            value.Id, value.Reference, Kind = value.Kind.ToString(), value.CurrencyCode,
            value.OriginalForeignAmount, value.OutstandingForeignAmount, value.CarryingAmountSar,
            value.LastMeasurementDate, Status = value.Status.ToString(), value.LedgerAccountId
        }).ToList();

    private void LoadLookups()
    {
        var periods = _context.FiscalPeriods.AsNoTracking().Where(value => value.Status == FiscalPeriodStatus.Open)
            .OrderBy(value => value.StartDate).Select(value => new Lookup<int>(value.Id, $"{value.Name} ({value.EndDate:yyyy-MM-dd})")).ToList();
        Bind(_settlementPeriod, periods);
        Bind(_closingPeriod, periods);
        Bind(_adjustmentPeriod, periods);
        var accounts = _context.LedgerAccounts.AsNoTracking().Where(value => value.IsActive && value.AllowsPosting
                && value.CurrencyCode == GeneralLedgerService.FunctionalCurrencyCode)
            .OrderBy(value => value.Code).Select(value => new { value.Id, value.Code, value.NameAr, value.Type }).ToList();
        Bind(_cashAccount, accounts.Where(value => value.Type == LedgerAccountType.Asset)
            .Select(value => new Lookup<int>(value.Id, $"{value.Code} — {value.NameAr}")).ToList());
        var gains = accounts.Where(value => value.Type == LedgerAccountType.Revenue)
            .Select(value => new Lookup<int>(value.Id, $"{value.Code} — {value.NameAr}")).ToList();
        var losses = accounts.Where(value => value.Type == LedgerAccountType.Expense)
            .Select(value => new Lookup<int>(value.Id, $"{value.Code} — {value.NameAr}")).ToList();
        Bind(_gainAccount, gains); Bind(_closingGainAccount, gains);
        Bind(_lossAccount, losses); Bind(_closingLossAccount, losses);
        Bind(_retainedEarningsAccount, accounts.Where(value => value.Type == LedgerAccountType.Equity)
            .Select(value => new Lookup<int>(value.Id, $"{value.Code} — {value.NameAr}")).ToList());
        LoadSettlementRates();
    }

    private void CreateOpeningBalances()
    {
        try
        {
            var accounts = PostingAccounts(balanceSheetOnly: true);
            using var dialog = new AccountingJournalLinesDialog(accounts, "إدخال الأرصدة الافتتاحية", balanceSheetOnly: true);
            if (dialog.ShowDialog(this) != DialogResult.OK) return;
            var balances = dialog.Lines.Select(line => new OpeningBalanceRequest(
                line.LedgerAccountId, line.Debit, line.Credit, line.Description)).ToArray();
            new FiscalYearClosingService(_context).CreateOpeningBalanceDraft(
                (int)SelectedId(_yearsGrid), balances, Actor(), Reason());
            ShowSuccessAndRefresh();
        }
        catch (Exception ex) { ShowError(ex); }
    }

    private void CreateAdjustment()
    {
        try
        {
            using var dialog = new AccountingJournalLinesDialog(
                PostingAccounts(balanceSheetOnly: false), "إدخال أسطر قيد التسوية", balanceSheetOnly: false);
            if (dialog.ShowDialog(this) != DialogResult.OK) return;
            new AccountingAdjustmentService(_context).CreateDraft(new AccountingAdjustmentRequest(
                _adjustmentDate.Value.Date, SelectedLookup(_adjustmentPeriod), _adjustmentDescription.Text,
                (AccountingAdjustmentType)_adjustmentType.SelectedItem!, _adjustmentEvidence.Text,
                _scheduleReversal.Checked ? _scheduledReversalDate.Value.Date : null, dialog.Lines),
                Actor(), Reason());
            ShowSuccessAndRefresh();
        }
        catch (Exception ex) { ShowError(ex); }
    }

    private List<(int Id, string Text)> PostingAccounts(bool balanceSheetOnly) => _context.LedgerAccounts.AsNoTracking()
        .Where(value => value.IsActive && value.AllowsPosting
            && value.CurrencyCode == GeneralLedgerService.FunctionalCurrencyCode
            && (!balanceSheetOnly || (value.Type != LedgerAccountType.Revenue && value.Type != LedgerAccountType.Expense)))
        .OrderBy(value => value.Code)
        .Select(value => new { value.Id, value.Code, value.NameAr }).AsEnumerable()
        .Select(value => (value.Id, $"{value.Code} — {value.NameAr}"))
        .ToList();

    private void SuggestNextFiscalYear()
    {
        if (_yearsGrid.CurrentRow == null) return;
        if (_yearsGrid.CurrentRow.Cells["EndDate"].Value is not DateTime end) return;
        var start = end.Date.AddDays(1);
        _nextYearStart.Value = start;
        _nextYearEnd.Value = start.AddYears(1).AddDays(-1);
        _nextYearName.Text = start.Year.ToString();
    }

    private void LoadSettlementRates()
    {
        if (SelectedNullableId(_itemsGrid) is not long itemId) return;
        var currency = _context.ForeignMonetaryItems.AsNoTracking().Where(value => value.Id == itemId)
            .Select(value => value.CurrencyCode).SingleOrDefault();
        if (currency == null) return;
        var rates = _context.ForeignExchangeRates.AsNoTracking().Where(value =>
                value.CurrencyCode == currency && value.RateDate == _settlementDate.Value.Date
                && value.Purpose == ExchangeRatePurpose.Transaction && value.Status == ExchangeRateStatus.Approved)
            .OrderByDescending(value => value.Version)
            .Select(value => new Lookup<long>(value.Id, $"{value.CurrencyCode} {value.SarPerUnit:N8} — {value.RateDate:yyyy-MM-dd}"))
            .ToList();
        Bind(_settlementRate, rates);
    }

    private void CreateRateDraft() => Run(() => new ForeignExchangeRateService(_context).CreateDraft(new(
        _currencyText.Text, _rateDate.Value.Date, (ExchangeRatePurpose)_ratePurpose.SelectedItem!,
        _rateValue.Value, _sourceText.Text, _evidenceText.Text), Actor(), Reason()));

    private void RegisterItem() => Run(() => new ForeignCurrencyMonetaryItemService(_context).Register(new(
        decimal.ToInt64(_recognitionLineId.Value), (ForeignMonetaryItemKind)_itemKind.SelectedItem!,
        _itemReference.Text), Actor(), Reason()));

    private void SettleItem() => Run(() => new ForeignCurrencyMonetaryItemService(_context).Settle(new(
            SelectedId(_itemsGrid), _settlementDate.Value.Date, SelectedLookup(_settlementPeriod),
            _settlementAmount.Value, SelectedLongLookup(_settlementRate), SelectedLookup(_cashAccount),
            SelectedLookup(_gainAccount), SelectedLookup(_lossAccount)),
        SystemFxActor, Actor(), Actor(), Reason()));

    private void RevalueAll() => Run(() => new ForeignCurrencyMonetaryItemService(_context).RevalueAllOpenItems(
        SelectedLookup(_closingPeriod), SelectedLookup(_closingGainAccount), SelectedLookup(_closingLossAccount),
        SystemFxActor, Actor(), Actor(), Reason()));

    private void Run(Action action)
    {
        try
        {
            action();
            ShowSuccessAndRefresh();
        }
        catch (Exception ex) { ShowError(ex); }
    }

    private void ShowSuccessAndRefresh()
    {
        MessageBox.Show("تمت العملية بنجاح.", "إدارة المحاسبة", MessageBoxButtons.OK, MessageBoxIcon.Information);
        RefreshAll();
    }

    private string Reason() => string.IsNullOrWhiteSpace(_reasonTextBox.Text)
        ? throw new InvalidOperationException("أدخل سبب العملية أو مرجع الموافقة أولًا.")
        : _reasonTextBox.Text.Trim();

    private static string Actor() => AuthenticationService.CurrentUsername;
    private static long SelectedId(DataGridView grid) => SelectedNullableId(grid)
        ?? throw new InvalidOperationException("حدد سجلًا أولًا.");
    private static long? SelectedNullableId(DataGridView grid) => grid.CurrentRow?.Cells["Id"].Value is object value
        ? Convert.ToInt64(value) : null;
    private static int SelectedLookup(ComboBox combo) => combo.SelectedItem is Lookup<int> item
        ? item.Id : throw new InvalidOperationException("حدد قيمة من القائمة.");
    private static long SelectedLongLookup(ComboBox combo) => combo.SelectedItem is Lookup<long> item
        ? item.Id : throw new InvalidOperationException("حدد سعر صرف معتمدًا.");

    private static void Bind<T>(ComboBox combo, IReadOnlyList<Lookup<T>> values)
    {
        combo.DataSource = null;
        combo.DisplayMember = nameof(Lookup<T>.Text);
        combo.ValueMember = nameof(Lookup<T>.Id);
        combo.DataSource = values.ToList();
    }

    private static FlowLayoutPanel CommandBar(params Control[] controls)
    {
        var panel = new FlowLayoutPanel
        {
            Dock = DockStyle.Top, Height = 60, Padding = new Padding(8),
            FlowDirection = FlowDirection.RightToLeft, WrapContents = false, AutoScroll = true
        };
        panel.Controls.AddRange(controls);
        return panel;
    }

    private static Control Labeled(string text, Control control)
    {
        var panel = new FlowLayoutPanel { Width = control.Width + 10, Height = 55, FlowDirection = FlowDirection.TopDown, WrapContents = false };
        panel.Controls.Add(new Label { Text = text, AutoSize = true });
        panel.Controls.Add(control);
        return panel;
    }

    private static Button ActionButton(string text, EventHandler handler, bool secondary = false)
    {
        var button = new Button { Text = text, Width = 165, Height = 34 };
        button.Click += handler;
        if (secondary) ThemeManager.StyleSecondaryButton(button); else ThemeManager.StylePrimaryButton(button);
        return button;
    }

    private static DataGridView CreateGrid() => new()
    {
        Dock = DockStyle.Fill, ReadOnly = true, AllowUserToAddRows = false, AllowUserToDeleteRows = false,
        MultiSelect = false, SelectionMode = DataGridViewSelectionMode.FullRowSelect,
        AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill, RowHeadersVisible = false
    };
    private static ComboBox CreateCombo(int width = 150) => new()
        { Width = width, DropDownStyle = ComboBoxStyle.DropDownList };
    private static NumericUpDown CreateAmount(int decimals, decimal maximum) => new()
        { Width = 140, DecimalPlaces = decimals, Maximum = maximum, Minimum = 0m, ThousandsSeparator = true };

    private static void ShowError(Exception ex)
    {
        LoggingService.LogError(ex, "Accounting management operation failed");
        MessageBox.Show(ex.Message, "تعذر تنفيذ العملية", MessageBoxButtons.OK, MessageBoxIcon.Error);
    }

    private sealed record Lookup<T>(T Id, string Text);
}
