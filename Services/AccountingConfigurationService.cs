using System.Text.Json;
using FishFarmManager.Data;
using FishFarmManager.Models;
using Microsoft.EntityFrameworkCore;

namespace FishFarmManager.Services;

public sealed class AccountingConfigurationService
{
    public const string PilotConfigurationName = "Saudi Pilot Chart of Accounts";
    public const int PilotConfigurationVersion = 2;
    private readonly FishFarmContext _context;

    public AccountingConfigurationService(FishFarmContext context) => _context = context;

    public AccountingConfiguration CreatePilotDraft(string actorUsername)
    {
        RequireActor(actorUsername);
        var existing = _context.AccountingConfigurations.Include(item => item.PostingMappings)
            .SingleOrDefault(item => item.Name == PilotConfigurationName && item.Version == PilotConfigurationVersion);
        if (existing != null) return existing;

        var accounts = EnsurePilotAccounts();
        var configuration = new AccountingConfiguration
        {
            Name = PilotConfigurationName,
            Version = PilotConfigurationVersion,
            Status = AccountingConfigurationStatus.Draft,
            CreatedAtUtc = DateTime.UtcNow,
            CreatedBy = actorUsername.Trim(),
            PostingMappings = new List<PostingMapping>
            {
                Map(PostingEventType.SalesCompleted, PostingComponent.AccountsReceivable, accounts["1120"]),
                Map(PostingEventType.SalesCompleted, PostingComponent.SalesRevenue, accounts["4100"]),
                Map(PostingEventType.SalesCompleted, PostingComponent.OutputVat, accounts["2200"]),
                Map(PostingEventType.PurchaseReceived, PostingComponent.InventoryOrExpense, accounts["1130"]),
                Map(PostingEventType.PurchaseReceived, PostingComponent.InputVat, accounts["1140"]),
                Map(PostingEventType.PurchaseReceived, PostingComponent.AccountsPayable, accounts["2100"]),
                Map(PostingEventType.CustomerPaymentReceived, PostingComponent.Cash, accounts["1110"]),
                Map(PostingEventType.CustomerPaymentReceived, PostingComponent.AccountsReceivable, accounts["1120"]),
                Map(PostingEventType.SupplierPaymentCompleted, PostingComponent.AccountsPayable, accounts["2100"]),
                Map(PostingEventType.SupplierPaymentCompleted, PostingComponent.Cash, accounts["1110"]),
                Map(PostingEventType.PayrollApproved, PostingComponent.PayrollExpense, accounts["5200"]),
                Map(PostingEventType.PayrollApproved, PostingComponent.SalariesPayable, accounts["2110"]),
                Map(PostingEventType.PayrollApproved, PostingComponent.PayrollWithholdingsPayable, accounts["2120"]),
                Map(PostingEventType.DepreciationApproved, PostingComponent.DepreciationExpense, accounts["5300"]),
                Map(PostingEventType.DepreciationApproved, PostingComponent.AccumulatedDepreciation, accounts["1520"])
            }
        };
        _context.AccountingConfigurations.Add(configuration);
        _context.SaveChanges();
        AddAudit(configuration, "CreateDraft", actorUsername, "Initial pilot configuration for accountant review", null);
        _context.SaveChanges();
        return configuration;
    }

    public AccountingConfiguration Approve(int configurationId, string actorUsername, string reason)
    {
        RequireActor(actorUsername);
        if (string.IsNullOrWhiteSpace(reason)) throw new ArgumentException("Approval reason is required.", nameof(reason));
        var configuration = _context.AccountingConfigurations.Include(item => item.PostingMappings)
            .ThenInclude(mapping => mapping.LedgerAccount)
            .SingleOrDefault(item => item.Id == configurationId)
            ?? throw new InvalidOperationException("The accounting configuration does not exist.");
        if (configuration.Status != AccountingConfigurationStatus.Draft)
            throw new InvalidOperationException("Only a draft accounting configuration can be approved.");
        if (string.Equals(configuration.CreatedBy, actorUsername, StringComparison.OrdinalIgnoreCase))
            throw new InvalidOperationException("The configuration creator cannot approve it.");
        if (configuration.PostingMappings.Count != 15
            || configuration.PostingMappings.Any(mapping => !mapping.IsActive
                || !mapping.LedgerAccount.IsActive || !mapping.LedgerAccount.AllowsPosting))
            throw new InvalidOperationException("The posting map is incomplete or contains inactive accounts.");

        var before = Snapshot(configuration);
        foreach (var active in _context.AccountingConfigurations
            .Where(item => item.Status == AccountingConfigurationStatus.Approved && item.Id != configurationId))
        {
            active.Status = AccountingConfigurationStatus.Retired;
        }
        configuration.Status = AccountingConfigurationStatus.Approved;
        configuration.ApprovedAtUtc = DateTime.UtcNow;
        configuration.ApprovedBy = actorUsername.Trim();
        AddAudit(configuration, "Approve", actorUsername, reason, before);
        _context.SaveChanges();
        return configuration;
    }

    private Dictionary<string, LedgerAccount> EnsurePilotAccounts()
    {
        var existing = _context.LedgerAccounts.ToDictionary(account => account.Code);
        LedgerAccount Ensure(string code, string nameAr, LedgerAccountType type, AccountNormalBalance normal,
            bool allowsPosting, string? parentCode = null)
        {
            if (existing.TryGetValue(code, out var found)) return found;
            var account = new LedgerAccount
            {
                Code = code,
                NameAr = nameAr,
                Type = type,
                NormalBalance = normal,
                CurrencyCode = "SAR",
                IsActive = true,
                AllowsPosting = allowsPosting,
                ParentAccount = parentCode == null ? null : existing[parentCode]
            };
            _context.LedgerAccounts.Add(account);
            existing[code] = account;
            return account;
        }

        Ensure("1000", "الأصول", LedgerAccountType.Asset, AccountNormalBalance.Debit, false);
        Ensure("1100", "الأصول المتداولة", LedgerAccountType.Asset, AccountNormalBalance.Debit, false, "1000");
        Ensure("1110", "النقدية والبنوك", LedgerAccountType.Asset, AccountNormalBalance.Debit, true, "1100");
        Ensure("1120", "الذمم المدينة", LedgerAccountType.Asset, AccountNormalBalance.Debit, true, "1100");
        Ensure("1130", "المخزون", LedgerAccountType.Asset, AccountNormalBalance.Debit, true, "1100");
        Ensure("1140", "ضريبة القيمة المضافة القابلة للاسترداد", LedgerAccountType.Asset, AccountNormalBalance.Debit, true, "1100");
        Ensure("1200", "الأصول البيولوجية", LedgerAccountType.Asset, AccountNormalBalance.Debit, true, "1000");
        Ensure("1500", "الأصول الثابتة", LedgerAccountType.Asset, AccountNormalBalance.Debit, false, "1000");
        Ensure("1510", "تكلفة الأصول الثابتة", LedgerAccountType.Asset, AccountNormalBalance.Debit, true, "1500");
        Ensure("1520", "مجمع إهلاك الأصول الثابتة", LedgerAccountType.Asset, AccountNormalBalance.Credit, true, "1500");
        Ensure("2000", "الالتزامات", LedgerAccountType.Liability, AccountNormalBalance.Credit, false);
        Ensure("2100", "الذمم الدائنة", LedgerAccountType.Liability, AccountNormalBalance.Credit, true, "2000");
        Ensure("2110", "رواتب مستحقة", LedgerAccountType.Liability, AccountNormalBalance.Credit, true, "2000");
        Ensure("2120", "استقطاعات رواتب مستحقة", LedgerAccountType.Liability, AccountNormalBalance.Credit, true, "2000");
        Ensure("2200", "ضريبة القيمة المضافة المستحقة", LedgerAccountType.Liability, AccountNormalBalance.Credit, true, "2000");
        Ensure("3000", "حقوق الملكية", LedgerAccountType.Equity, AccountNormalBalance.Credit, false);
        Ensure("3100", "رأس المال", LedgerAccountType.Equity, AccountNormalBalance.Credit, true, "3000");
        Ensure("4000", "الإيرادات", LedgerAccountType.Revenue, AccountNormalBalance.Credit, false);
        Ensure("4100", "إيرادات المبيعات", LedgerAccountType.Revenue, AccountNormalBalance.Credit, true, "4000");
        Ensure("5000", "المصروفات", LedgerAccountType.Expense, AccountNormalBalance.Debit, false);
        Ensure("5100", "تكلفة المبيعات والمشتريات", LedgerAccountType.Expense, AccountNormalBalance.Debit, true, "5000");
        Ensure("5200", "مصروف الرواتب", LedgerAccountType.Expense, AccountNormalBalance.Debit, true, "5000");
        Ensure("5300", "مصروف الإهلاك", LedgerAccountType.Expense, AccountNormalBalance.Debit, true, "5000");
        _context.SaveChanges();
        return existing;
    }

    private static PostingMapping Map(PostingEventType eventType, PostingComponent component, LedgerAccount account) =>
        new() { EventType = eventType, Component = component, LedgerAccount = account, IsActive = true };

    private void AddAudit(AccountingConfiguration configuration, string action, string actor, string reason, string? before) =>
        _context.AccountingAuditEvents.Add(new AccountingAuditEvent
        {
            OccurredAtUtc = DateTime.UtcNow,
            EntityType = nameof(AccountingConfiguration),
            EntityId = configuration.Id.ToString(),
            Action = action,
            ActorUsername = actor.Trim(),
            Reason = reason,
            BeforeJson = before,
            AfterJson = Snapshot(configuration),
            CorrelationId = Guid.NewGuid().ToString("N")
        });

    private static string Snapshot(AccountingConfiguration configuration) => JsonSerializer.Serialize(new
    {
        configuration.Name,
        configuration.Version,
        configuration.Status,
        configuration.CreatedBy,
        configuration.ApprovedBy,
        Mappings = configuration.PostingMappings.OrderBy(item => item.EventType).ThenBy(item => item.Component)
            .Select(item => new { item.EventType, item.Component, AccountCode = item.LedgerAccount?.Code })
    });

    private static void RequireActor(string actor)
    {
        if (string.IsNullOrWhiteSpace(actor)) throw new ArgumentException("Actor is required.", nameof(actor));
    }
}
