using System.Text.Json;
using FishFarmManager.Data;
using FishFarmManager.Models;
using Microsoft.EntityFrameworkCore;

namespace FishFarmManager.Services;

public sealed record ForeignExchangeRateDraftRequest(
    string CurrencyCode,
    DateTime RateDate,
    ExchangeRatePurpose Purpose,
    decimal SarPerUnit,
    string SourceReference,
    string EvidenceReference);

public sealed class ForeignExchangeRateService
{
    public const string FunctionalCurrencyCode = "SAR";
    private readonly FishFarmContext _context;

    public ForeignExchangeRateService(FishFarmContext context) => _context = context;

    public ForeignExchangeRate CreateDraft(
        ForeignExchangeRateDraftRequest request,
        string actorUsername,
        string reason)
    {
        RequireActorAndReason(actorUsername, reason);
        var currency = (request.CurrencyCode ?? string.Empty).Trim().ToUpperInvariant();
        if (currency.Length != 3 || currency.Any(character => character is < 'A' or > 'Z'))
            throw new ArgumentException("Foreign currency code must contain exactly three Latin letters.", nameof(request));
        if (currency == FunctionalCurrencyCode)
            throw new InvalidOperationException("SAR is the functional currency and must not be registered as a foreign exchange rate.");
        if (request.SarPerUnit <= 0m || DecimalScale(request.SarPerUnit) > 8)
            throw new ArgumentOutOfRangeException(nameof(request), "The rate must be positive and use no more than eight decimal places.");
        if (string.IsNullOrWhiteSpace(request.SourceReference))
            throw new ArgumentException("An official source reference is required.", nameof(request));
        if (string.IsNullOrWhiteSpace(request.EvidenceReference))
            throw new ArgumentException("A retained evidence reference is required.", nameof(request));

        var rateDate = request.RateDate.Date;
        var lastVersion = _context.ForeignExchangeRates
            .Where(item => item.CurrencyCode == currency && item.RateDate == rateDate && item.Purpose == request.Purpose)
            .Select(item => (int?)item.Version).Max() ?? 0;
        var rate = new ForeignExchangeRate
        {
            CurrencyCode = currency,
            RateDate = rateDate,
            Purpose = request.Purpose,
            Version = lastVersion + 1,
            SarPerUnit = request.SarPerUnit,
            SourceReference = request.SourceReference.Trim(),
            EvidenceReference = request.EvidenceReference.Trim(),
            Status = ExchangeRateStatus.Draft,
            CreatedAtUtc = DateTime.UtcNow,
            CreatedBy = actorUsername.Trim()
        };
        _context.ForeignExchangeRates.Add(rate);
        _context.SaveChanges();
        AddAudit(rate, "CreateDraft", actorUsername, reason, null);
        _context.SaveChanges();
        return rate;
    }

    public ForeignExchangeRate Approve(long rateId, string actorUsername, string reason)
    {
        RequireActorAndReason(actorUsername, reason);
        using var transaction = _context.Database.BeginTransaction();
        var rate = _context.ForeignExchangeRates.SingleOrDefault(item => item.Id == rateId)
            ?? throw new InvalidOperationException("The foreign exchange rate does not exist.");
        if (rate.Status != ExchangeRateStatus.Draft)
            throw new InvalidOperationException("Only a draft foreign exchange rate can be approved.");
        if (string.Equals(rate.CreatedBy, actorUsername.Trim(), StringComparison.OrdinalIgnoreCase))
            throw new InvalidOperationException("The rate creator cannot approve it.");

        var before = Snapshot(rate);
        var activeRates = _context.ForeignExchangeRates.Where(item =>
            item.Id != rate.Id
            && item.CurrencyCode == rate.CurrencyCode
            && item.RateDate == rate.RateDate
            && item.Purpose == rate.Purpose
            && item.Status == ExchangeRateStatus.Approved).ToList();
        foreach (var active in activeRates)
        {
            active.Status = ExchangeRateStatus.Retired;
            AddAudit(active, "Retire", actorUsername,
                $"Superseded by approved exchange-rate version {rate.Version}: {reason}", null);
        }
        if (activeRates.Count > 0) _context.SaveChanges();
        rate.Status = ExchangeRateStatus.Approved;
        rate.ApprovedAtUtc = DateTime.UtcNow;
        rate.ApprovedBy = actorUsername.Trim();
        AddAudit(rate, "Approve", actorUsername, reason, before);
        _context.SaveChanges();
        transaction.Commit();
        return rate;
    }

    public ForeignExchangeRate GetApprovedRate(
        string currencyCode,
        DateTime rateDate,
        ExchangeRatePurpose purpose)
    {
        var currency = (currencyCode ?? string.Empty).Trim().ToUpperInvariant();
        return _context.ForeignExchangeRates.AsNoTracking().SingleOrDefault(item =>
                   item.CurrencyCode == currency
                   && item.RateDate == rateDate.Date
                   && item.Purpose == purpose
                   && item.Status == ExchangeRateStatus.Approved)
               ?? throw new InvalidOperationException("No approved exchange rate exists for the exact date and purpose.");
    }

    private void AddAudit(ForeignExchangeRate rate, string action, string actor, string reason, string? before) =>
        _context.AccountingAuditEvents.Add(new AccountingAuditEvent
        {
            OccurredAtUtc = DateTime.UtcNow,
            EntityType = nameof(ForeignExchangeRate),
            EntityId = rate.Id.ToString(),
            Action = action,
            ActorUsername = actor.Trim(),
            Reason = reason.Trim(),
            BeforeJson = before,
            AfterJson = Snapshot(rate),
            CorrelationId = Guid.NewGuid().ToString("N")
        });

    private static string Snapshot(ForeignExchangeRate rate) => JsonSerializer.Serialize(new
    {
        rate.CurrencyCode,
        rate.RateDate,
        rate.Purpose,
        rate.Version,
        rate.SarPerUnit,
        rate.SourceReference,
        rate.EvidenceReference,
        rate.Status,
        rate.CreatedBy,
        rate.ApprovedBy
    });

    private static int DecimalScale(decimal value) => (decimal.GetBits(value)[3] >> 16) & 0x7F;

    private static void RequireActorAndReason(string actor, string reason)
    {
        if (string.IsNullOrWhiteSpace(actor)) throw new ArgumentException("Actor is required.", nameof(actor));
        if (string.IsNullOrWhiteSpace(reason)) throw new ArgumentException("Reason is required.", nameof(reason));
    }
}
