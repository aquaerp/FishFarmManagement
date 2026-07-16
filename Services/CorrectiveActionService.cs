using System.Text.Json;
using FishFarmManager.Data;
using FishFarmManager.Models;

namespace FishFarmManager.Services;

public sealed record CorrectiveActionRequest(string ReferenceNumber, string Nonconformity, string RootCause,
    string ActionPlan, string Owner, DateTime DueDate);

public sealed record CapaPerformance(int TotalClosed, int ClosedOnTime, decimal OnTimeClosurePercent);

public sealed class CorrectiveActionService
{
    private readonly FishFarmContext _context;
    public CorrectiveActionService(FishFarmContext context) => _context = context;

    public CorrectiveAction Open(CorrectiveActionRequest request, string actor, string reason)
    {
        ArgumentNullException.ThrowIfNull(request);
        Require(actor, 100); Require(reason, 1000); Require(request.ReferenceNumber, 50); Require(request.Nonconformity, 1000);
        Require(request.RootCause, 1000); Require(request.ActionPlan, 1000); Require(request.Owner, 100);
        if (_context.CorrectiveActions.Any(value => value.ReferenceNumber == request.ReferenceNumber.Trim())) throw new InvalidOperationException("CAPA reference already exists.");
        var item = new CorrectiveAction { ReferenceNumber = request.ReferenceNumber.Trim(), Nonconformity = request.Nonconformity.Trim(), RootCause = request.RootCause.Trim(), ActionPlan = request.ActionPlan.Trim(), Owner = request.Owner.Trim(), DueDate = request.DueDate.Date, CreatedBy = actor.Trim(), CreatedAtUtc = DateTime.UtcNow };
        _context.CorrectiveActions.Add(item); _context.SaveChanges(); Audit(item, "OpenCAPA", actor, reason); _context.SaveChanges(); return item;
    }

    public CorrectiveAction Complete(int id, DateTime completedAt, string actor, string reason)
    {
        Require(actor, 100); Require(reason, 1000);
        var item = Find(id);
        if (item.Status is CapaStatus.Closed or CapaStatus.AwaitingVerification) throw new InvalidOperationException("CAPA cannot be completed in its current status.");
        item.Status = CapaStatus.AwaitingVerification; item.CompletedAtUtc = completedAt; item.CompletedBy = actor.Trim();
        Audit(item, "CompleteCAPA", actor, reason); _context.SaveChanges(); return item;
    }

    public CorrectiveAction Verify(int id, bool effective, string verifier, string notes, string reason)
    {
        Require(verifier, 100); Require(notes, 1000); Require(reason, 1000);
        var item = Find(id);
        if (item.Status != CapaStatus.AwaitingVerification) throw new InvalidOperationException("CAPA is not awaiting verification.");
        if (string.Equals(item.CreatedBy, verifier, StringComparison.OrdinalIgnoreCase) || string.Equals(item.CompletedBy, verifier, StringComparison.OrdinalIgnoreCase) || string.Equals(item.Owner, verifier, StringComparison.OrdinalIgnoreCase)) throw new InvalidOperationException("CAPA effectiveness must be verified independently.");
        item.EffectivenessVerified = effective; item.VerifiedBy = verifier.Trim(); item.VerifiedAtUtc = DateTime.UtcNow; item.VerificationNotes = notes.Trim(); item.Status = effective ? CapaStatus.Closed : CapaStatus.InProgress;
        Audit(item, effective ? "CloseCAPA" : "CAPAEffectivenessFailed", verifier, reason); _context.SaveChanges(); return item;
    }

    public CapaPerformance GetPerformance(DateTime from, DateTime to)
    {
        var closed = _context.CorrectiveActions.Where(value => value.Status == CapaStatus.Closed && value.CompletedAtUtc >= from && value.CompletedAtUtc <= to).ToList();
        var onTime = closed.Count(value => value.CompletedAtUtc!.Value.Date <= value.DueDate.Date);
        return new CapaPerformance(closed.Count, onTime, closed.Count == 0 ? 0m : Math.Round(onTime * 100m / closed.Count, 2));
    }

    private CorrectiveAction Find(int id) => _context.CorrectiveActions.SingleOrDefault(value => value.Id == id) ?? throw new InvalidOperationException("CAPA does not exist.");
    private void Audit(CorrectiveAction item, string action, string actor, string reason) => _context.AccountingAuditEvents.Add(new AccountingAuditEvent { OccurredAtUtc = DateTime.UtcNow, EntityType = nameof(CorrectiveAction), EntityId = item.Id.ToString(), Action = action, ActorUsername = actor.Trim(), Reason = reason.Trim(), AfterJson = JsonSerializer.Serialize(new { item.ReferenceNumber, item.Status, item.DueDate }), CorrelationId = Guid.NewGuid().ToString("N") });
    private static void Require(string? value, int limit) { if (string.IsNullOrWhiteSpace(value) || value.Trim().Length > limit) throw new InvalidOperationException("Required CAPA field is invalid."); }
}
