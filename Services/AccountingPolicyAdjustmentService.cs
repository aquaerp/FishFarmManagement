using FishFarmManager.Data;
using FishFarmManager.Models;
using Microsoft.EntityFrameworkCore;

namespace FishFarmManager.Services;

/// <summary>
/// Governed period-end measurements required by the approved accounting policies.
/// The measurement schedule remains supporting evidence; this service validates and
/// translates its result into an independently reviewable accounting adjustment.
/// </summary>
public sealed class AccountingPolicyAdjustmentService
{
    private readonly FishFarmContext _context;
    private readonly AccountingAdjustmentService _adjustments;

    public AccountingPolicyAdjustmentService(FishFarmContext context)
    {
        _context = context;
        _adjustments = new AccountingAdjustmentService(context);
    }

    public AccountingAdjustmentResult CreateBiologicalAssetFairValueDraft(
        DateTime measurementDate,
        int fiscalPeriodId,
        decimal carryingAmountBeforeMeasurement,
        decimal fairValueLessCostsToSell,
        string evidenceReference,
        string actor,
        string reason)
    {
        ValidateMoney(carryingAmountBeforeMeasurement, nameof(carryingAmountBeforeMeasurement));
        ValidateMoney(fairValueLessCostsToSell, nameof(fairValueLessCostsToSell));
        var change = decimal.Round(fairValueLessCostsToSell - carryingAmountBeforeMeasurement, 2,
            MidpointRounding.AwayFromZero);
        if (change == 0m)
            throw new InvalidOperationException("A zero fair-value change requires evidence retention but no journal entry.");

        var biologicalAsset = Account("1200");
        var lines = change > 0m
            ? new[]
            {
                new JournalLineRequest(biologicalAsset, change, 0m, Description: "Biological asset at FVLCTS"),
                new JournalLineRequest(Account("4210"), 0m, change, Description: "IAS 41 fair-value gain")
            }
            : new[]
            {
                new JournalLineRequest(Account("5420"), -change, 0m, Description: "IAS 41 fair-value loss"),
                new JournalLineRequest(biologicalAsset, 0m, -change, Description: "Biological asset at FVLCTS")
            };
        return _adjustments.CreateDraft(new AccountingAdjustmentRequest(
            measurementDate.Date, fiscalPeriodId,
            $"IAS 41 measurement: carrying SAR {carryingAmountBeforeMeasurement:0.00}, FVLCTS SAR {fairValueLessCostsToSell:0.00}",
            AccountingAdjustmentType.Estimate, evidenceReference, null, lines), actor, reason);
    }

    public AccountingAdjustmentResult CreateExpectedCreditLossDraft(
        DateTime measurementDate,
        int fiscalPeriodId,
        decimal grossTradeReceivables,
        decimal lifetimeLossRate,
        decimal existingAllowance,
        string evidenceReference,
        string actor,
        string reason)
    {
        ValidateMoney(grossTradeReceivables, nameof(grossTradeReceivables));
        ValidateMoney(existingAllowance, nameof(existingAllowance));
        if (lifetimeLossRate is < 0m or > 1m)
            throw new ArgumentOutOfRangeException(nameof(lifetimeLossRate), "The lifetime loss rate must be between zero and one.");
        if (existingAllowance > grossTradeReceivables)
            throw new InvalidOperationException("The existing allowance cannot exceed gross trade receivables.");
        var requiredAllowance = decimal.Round(grossTradeReceivables * lifetimeLossRate, 2,
            MidpointRounding.AwayFromZero);
        var change = requiredAllowance - existingAllowance;
        if (change == 0m)
            throw new InvalidOperationException("An unchanged expected-credit-loss allowance requires evidence retention but no journal entry.");

        var allowance = Account("1125");
        var loss = Account("5430");
        var lines = change > 0m
            ? new[]
            {
                new JournalLineRequest(loss, change, 0m, Description: "IFRS 9 lifetime ECL expense"),
                new JournalLineRequest(allowance, 0m, change, Description: "Loss allowance on trade receivables")
            }
            : new[]
            {
                new JournalLineRequest(allowance, -change, 0m, Description: "Reduction of loss allowance"),
                new JournalLineRequest(loss, 0m, -change, Description: "IFRS 9 ECL reversal")
            };
        return _adjustments.CreateDraft(new AccountingAdjustmentRequest(
            measurementDate.Date, fiscalPeriodId,
            $"IFRS 9 ECL: gross SAR {grossTradeReceivables:0.00}, lifetime rate {lifetimeLossRate:P2}",
            AccountingAdjustmentType.Estimate, evidenceReference, null, lines), actor, reason);
    }

    public AccountingAdjustmentResult CreateHarvestFairValueTransferDraft(
        DateTime harvestDate,
        int fiscalPeriodId,
        decimal harvestCostTransferredFromWorkInProgress,
        decimal fairValueLessCostsToSellAtHarvest,
        string evidenceReference,
        string actor,
        string reason)
    {
        ValidateMoney(harvestCostTransferredFromWorkInProgress, nameof(harvestCostTransferredFromWorkInProgress));
        ValidateMoney(fairValueLessCostsToSellAtHarvest, nameof(fairValueLessCostsToSellAtHarvest));
        var adjustment = decimal.Round(fairValueLessCostsToSellAtHarvest - harvestCostTransferredFromWorkInProgress,
            2, MidpointRounding.AwayFromZero);
        if (adjustment == 0m)
            throw new InvalidOperationException("The harvested product equals its cost basis and needs no fair-value reclassification.");
        var biologicalAsset = Account("1200");
        var inventory = Account("1130");
        var lines = adjustment > 0m
            ? new[]
            {
                new JournalLineRequest(inventory, adjustment, 0m, Description: "Harvest produce at IAS 41 deemed cost"),
                new JournalLineRequest(biologicalAsset, 0m, adjustment, Description: "Release biological fair-value adjustment")
            }
            : new[]
            {
                new JournalLineRequest(biologicalAsset, -adjustment, 0m, Description: "Release negative biological adjustment"),
                new JournalLineRequest(inventory, 0m, -adjustment, Description: "Harvest produce at IAS 41 deemed cost")
            };
        return _adjustments.CreateDraft(new AccountingAdjustmentRequest(
            harvestDate.Date, fiscalPeriodId,
            $"IAS 41 harvest transfer: cost SAR {harvestCostTransferredFromWorkInProgress:0.00}, FVLCTS SAR {fairValueLessCostsToSellAtHarvest:0.00}",
            AccountingAdjustmentType.Reclassification, evidenceReference, null, lines), actor, reason);
    }

    private int Account(string code) => _context.LedgerAccounts.AsNoTracking()
        .Where(value => value.Code == code && value.IsActive && value.AllowsPosting)
        .Select(value => value.Id)
        .SingleOrDefault() is var id && id > 0
            ? id
            : throw new InvalidOperationException($"The approved chart is missing active posting account {code}.");

    private static void ValidateMoney(decimal amount, string parameter)
    {
        if (amount < 0m || decimal.Round(amount, 2) != amount)
            throw new ArgumentOutOfRangeException(parameter, "The amount must be a non-negative SAR value with two-decimal precision.");
    }
}
