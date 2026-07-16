using System.Text.Json;
using FishFarmManager.Data;
using FishFarmManager.Models;
using Microsoft.EntityFrameworkCore;

namespace FishFarmManager.Services;

public sealed record RecallExerciseResult(RecallExercise Exercise, IReadOnlyList<string> CustomerNames, IReadOnlyList<string> OrderNumbers, IReadOnlyList<string> HarvestLots);

public sealed class RecallExerciseService
{
    private readonly FishFarmContext _context;
    private readonly OperationalTraceabilityService _traceability;
    public RecallExerciseService(FishFarmContext context) { _context = context; _traceability = new OperationalTraceabilityService(context); }

    public RecallExerciseResult Run(string exerciseNumber, string inputLotCode, DateTime startedAtUtc, DateTime completedAtUtc, string actor, string reason, string? findings = null)
    {
        Require(exerciseNumber, 50); Require(inputLotCode, 100); Require(actor, 100); Require(reason, 1000);
        if (completedAtUtc < startedAtUtc) throw new InvalidOperationException("Recall completion cannot precede its start.");
        if (_context.RecallExercises.Any(value => value.ExerciseNumber == exerciseNumber.Trim())) throw new InvalidOperationException("Recall exercise number already exists.");
        var routes = _traceability.TraceForward(inputLotCode);
        var saleItemIds = routes.Where(value => value.SalesOrderItemId.HasValue).Select(value => value.SalesOrderItemId!.Value).Distinct().ToArray();
        var sales = _context.SalesOrderItems.AsNoTracking().Include(value => value.SalesOrder).ThenInclude(value => value.Customer).Where(value => saleItemIds.Contains(value.Id)).ToArray();
        var customers = sales.Select(value => value.SalesOrder.Customer.Name).Distinct(StringComparer.OrdinalIgnoreCase).OrderBy(value => value).ToArray();
        var orders = sales.Select(value => value.SalesOrder.OrderNumber).Distinct(StringComparer.OrdinalIgnoreCase).OrderBy(value => value).ToArray();
        var harvests = routes.Where(value => !string.IsNullOrWhiteSpace(value.HarvestLotCode)).Select(value => value.HarvestLotCode!).Distinct(StringComparer.OrdinalIgnoreCase).OrderBy(value => value).ToArray();
        var exercise = new RecallExercise { ExerciseNumber = exerciseNumber.Trim(), TriggerLotCode = inputLotCode.Trim().ToUpperInvariant(), StartedAtUtc = startedAtUtc, CompletedAtUtc = completedAtUtc, AffectedOrderCount = orders.Length, AffectedCustomerCount = customers.Length, AffectedHarvestLotCount = harvests.Length, PassedTwoHourTarget = completedAtUtc - startedAtUtc <= TimeSpan.FromHours(2), ConductedBy = actor.Trim(), Findings = findings?.Trim(), CreatedAtUtc = DateTime.UtcNow };
        _context.RecallExercises.Add(exercise); _context.SaveChanges();
        _context.AccountingAuditEvents.Add(new AccountingAuditEvent { OccurredAtUtc = DateTime.UtcNow, EntityType = nameof(RecallExercise), EntityId = exercise.Id.ToString(), Action = "RunRecallExercise", ActorUsername = actor.Trim(), Reason = reason.Trim(), AfterJson = JsonSerializer.Serialize(new { exercise.TriggerLotCode, exercise.PassedTwoHourTarget, customers, orders, harvests }), CorrelationId = Guid.NewGuid().ToString("N") });
        _context.SaveChanges(); return new RecallExerciseResult(exercise, customers, orders, harvests);
    }
    private static void Require(string? value, int max) { if (string.IsNullOrWhiteSpace(value) || value.Trim().Length > max) throw new InvalidOperationException("Recall exercise field is invalid."); }
}
