using System.Text.Json;
using FishFarmManager.Data;
using FishFarmManager.Models;

namespace FishFarmManager.Services;

public sealed record QualityRiskRequest(
    string ReferenceNumber, string Title, string Description, QualityRiskType Type, string Category,
    string Owner, int Likelihood, int Impact, string TreatmentPlan, DateTime DueDate, string? Source = null);

public sealed record QualityObjectiveRequest(
    string ObjectiveCode, string Title, string Measure, decimal BaselineValue, decimal TargetValue,
    string Unit, string Owner, DateTime PeriodStart, DateTime PeriodEnd, string? Notes = null);

public sealed class QualityPlanningService
{
    private readonly FishFarmContext _context;
    public QualityPlanningService(FishFarmContext context) => _context = context;

    public QualityRiskRegister RegisterRisk(QualityRiskRequest request, string actor, string reason)
    {
        ArgumentNullException.ThrowIfNull(request);
        RequireActorAndReason(actor, reason);
        Require(request.ReferenceNumber, 50, "Reference number");
        Require(request.Title, 200, "Title");
        Require(request.Description, 2000, "Description");
        Require(request.Category, 100, "Category");
        Require(request.Owner, 100, "Owner");
        Require(request.TreatmentPlan, 2000, "Treatment plan");
        ValidateScore(request.Likelihood, request.Impact);
        if (request.DueDate.Date < DateTime.UtcNow.Date)
            throw new InvalidOperationException("The risk treatment due date cannot be in the past.");
        if (_context.QualityRiskRegisters.Any(value => value.ReferenceNumber == request.ReferenceNumber.Trim()))
            throw new InvalidOperationException("The quality risk reference already exists.");

        var risk = new QualityRiskRegister
        {
            ReferenceNumber = request.ReferenceNumber.Trim(), Title = request.Title.Trim(),
            Description = request.Description.Trim(), Type = request.Type, Category = request.Category.Trim(),
            Source = request.Source?.Trim(), Owner = request.Owner.Trim(), Likelihood = request.Likelihood,
            Impact = request.Impact, Score = request.Likelihood * request.Impact,
            TreatmentPlan = request.TreatmentPlan.Trim(), DueDate = request.DueDate.Date,
            Status = QualityRiskStatus.Open, CreatedBy = actor.Trim(), CreatedAtUtc = DateTime.UtcNow
        };
        _context.QualityRiskRegisters.Add(risk);
        _context.SaveChanges();
        Audit(risk.GetType().Name, risk.Id, "RegisterRisk", actor, reason, new { risk.ReferenceNumber, risk.Score, risk.Type });
        _context.SaveChanges();
        return risk;
    }

    public QualityRiskRegister ReviewRisk(int riskId, QualityRiskStatus status, bool effective,
        string reviewer, DateTime reviewDate, string reviewNotes, string reason)
    {
        RequireActorAndReason(reviewer, reason);
        Require(reviewNotes, 1000, "Review notes");
        var risk = _context.QualityRiskRegisters.SingleOrDefault(value => value.Id == riskId)
            ?? throw new InvalidOperationException("The quality risk does not exist.");
        if (risk.Status == QualityRiskStatus.Closed)
            throw new InvalidOperationException("A closed quality risk cannot be changed.");
        if (reviewDate.Date < risk.CreatedAtUtc.Date)
            throw new InvalidOperationException("The review date cannot precede registration.");
        if (status == QualityRiskStatus.Closed && !effective)
            throw new InvalidOperationException("A risk may be closed only after an effective review.");
        risk.Status = status;
        risk.EffectivenessReviewed = effective;
        risk.ReviewedBy = reviewer.Trim();
        risk.ReviewDate = reviewDate.Date;
        risk.ReviewNotes = reviewNotes.Trim();
        risk.UpdatedBy = reviewer.Trim();
        risk.UpdatedAtUtc = DateTime.UtcNow;
        Audit(risk.GetType().Name, risk.Id, "ReviewRisk", reviewer, reason, new { status, effective, risk.Score });
        _context.SaveChanges();
        return risk;
    }

    public QualityObjective CreateObjective(QualityObjectiveRequest request, string actor, string reason)
    {
        ArgumentNullException.ThrowIfNull(request);
        RequireActorAndReason(actor, reason);
        Require(request.ObjectiveCode, 50, "Objective code"); Require(request.Title, 200, "Title");
        Require(request.Measure, 100, "Measure"); Require(request.Unit, 50, "Unit"); Require(request.Owner, 100, "Owner");
        if (request.PeriodStart.Date > request.PeriodEnd.Date)
            throw new InvalidOperationException("The objective period is invalid.");
        if (_context.QualityObjectives.Any(value => value.ObjectiveCode == request.ObjectiveCode.Trim()))
            throw new InvalidOperationException("The quality objective code already exists.");
        var objective = new QualityObjective
        {
            ObjectiveCode = request.ObjectiveCode.Trim(), Title = request.Title.Trim(), Measure = request.Measure.Trim(),
            BaselineValue = request.BaselineValue, TargetValue = request.TargetValue, CurrentValue = request.BaselineValue,
            Unit = request.Unit.Trim(), Owner = request.Owner.Trim(), PeriodStart = request.PeriodStart.Date,
            PeriodEnd = request.PeriodEnd.Date, Notes = request.Notes?.Trim(), CreatedBy = actor.Trim(), CreatedAtUtc = DateTime.UtcNow
        };
        _context.QualityObjectives.Add(objective);
        _context.SaveChanges();
        Audit(objective.GetType().Name, objective.Id, "CreateQualityObjective", actor, reason,
            new { objective.ObjectiveCode, objective.TargetValue, objective.Unit });
        _context.SaveChanges();
        return objective;
    }

    public QualityObjective RecordObjectiveMeasurement(int objectiveId, decimal currentValue, DateTime measuredAt,
        string actor, string reason)
    {
        RequireActorAndReason(actor, reason);
        var objective = _context.QualityObjectives.SingleOrDefault(value => value.Id == objectiveId)
            ?? throw new InvalidOperationException("The quality objective does not exist.");
        if (objective.Status is QualityObjectiveStatus.Achieved or QualityObjectiveStatus.Cancelled)
            throw new InvalidOperationException("Measurements cannot be added to a completed or cancelled objective.");
        if (measuredAt.Date < objective.PeriodStart || measuredAt.Date > objective.PeriodEnd)
            throw new InvalidOperationException("The measurement date is outside the objective period.");
        objective.CurrentValue = currentValue;
        objective.LastMeasuredAtUtc = measuredAt;
        objective.Status = ReachedTarget(objective.BaselineValue, objective.TargetValue, currentValue)
            ? QualityObjectiveStatus.Achieved
            : measuredAt.Date > objective.PeriodEnd ? QualityObjectiveStatus.Overdue : QualityObjectiveStatus.Active;
        objective.UpdatedBy = actor.Trim(); objective.UpdatedAtUtc = DateTime.UtcNow;
        Audit(objective.GetType().Name, objective.Id, "RecordObjectiveMeasurement", actor, reason,
            new { currentValue, measuredAt, objective.Status });
        _context.SaveChanges();
        return objective;
    }

    private void Audit(string entityType, int entityId, string action, string actor, string reason, object details) =>
        _context.AccountingAuditEvents.Add(new AccountingAuditEvent
        {
            OccurredAtUtc = DateTime.UtcNow, EntityType = entityType, EntityId = entityId.ToString(), Action = action,
            ActorUsername = actor.Trim(), Reason = reason.Trim(), AfterJson = JsonSerializer.Serialize(details),
            CorrelationId = Guid.NewGuid().ToString("N")
        });

    private static bool ReachedTarget(decimal baseline, decimal target, decimal current) =>
        target >= baseline ? current >= target : current <= target;
    private static void ValidateScore(int likelihood, int impact)
    {
        if (likelihood is < 1 or > 5 || impact is < 1 or > 5)
            throw new InvalidOperationException("Likelihood and impact must each be between 1 and 5.");
    }
    private static void RequireActorAndReason(string actor, string reason)
    {
        Require(actor, 100, "Actor"); Require(reason, 1000, "Reason");
    }
    private static void Require(string? value, int maxLength, string field)
    {
        if (string.IsNullOrWhiteSpace(value) || value.Trim().Length > maxLength)
            throw new InvalidOperationException($"{field} is required and cannot exceed {maxLength} characters.");
    }
}
