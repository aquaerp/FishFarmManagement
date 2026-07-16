using System.Data;
using System.Text.Json;
using FishFarmManager.Data;
using FishFarmManager.Models;
using Microsoft.EntityFrameworkCore;

namespace FishFarmManager.Services;

public sealed record HaccpMeasurementRequest(
    int PondId,
    string RecordNumber,
    DateTime RecordDate,
    string HazardType,
    string HazardDescription,
    HazardSeverity Severity,
    HazardLikelihood Likelihood,
    string ControlPoint,
    FoodSafetyControlMeasureType ControlMeasureType,
    string MonitoringMethod,
    MonitoringFrequency Frequency,
    string MeasurementUnit,
    decimal MinimumLimit,
    decimal MaximumLimit,
    decimal TargetValue,
    decimal ActualValue,
    DateTime MeasurementTime,
    string AcceptanceCriteria,
    string? DeviationDescription,
    string? ReferenceDocument,
    string? EquipmentUsed = null,
    string? Location = null,
    string? Notes = null,
    string? ControlPointDescription = null);

public sealed class HaccpMonitoringService
{
    private readonly FishFarmContext _context;

    public HaccpMonitoringService(FishFarmContext context) => _context = context;

    public HACCPRecord RecordMeasurement(HaccpMeasurementRequest request, string actor, string reason)
    {
        ArgumentNullException.ThrowIfNull(request);
        RequireActorAndReason(actor, reason);
        RequireText(request.RecordNumber, 50, "Record number");
        RequireText(request.HazardType, 200, "Hazard type");
        RequireText(request.HazardDescription, 200, "Hazard description");
        RequireText(request.ControlPoint, 200, "Control point");
        RequireText(request.MonitoringMethod, 200, "Monitoring method");
        RequireText(request.MeasurementUnit, 50, "Measurement unit");
        RequireText(request.AcceptanceCriteria, 200, "Acceptance criteria");
        if (!_context.Ponds.AsNoTracking().Any(value => value.Id == request.PondId))
            throw new InvalidOperationException("The monitored pond does not exist.");
        if (_context.HACCPRecords.Any(value => value.RecordNumber == request.RecordNumber.Trim()))
            throw new InvalidOperationException("The HACCP record number already exists.");
        if (request.MinimumLimit > request.TargetValue || request.TargetValue > request.MaximumLimit)
            throw new InvalidOperationException("The target must be within the documented minimum and maximum limits.");
        if (request.MeasurementTime < request.RecordDate.Date)
            throw new InvalidOperationException("The measurement time cannot precede the HACCP record date.");
        if (request.ControlMeasureType is FoodSafetyControlMeasureType.Oprp or FoodSafetyControlMeasureType.Ccp
            && string.IsNullOrWhiteSpace(request.ReferenceDocument))
            throw new InvalidOperationException("OPRP and CCP monitoring require an approved control-plan reference.");

        var within = request.ActualValue >= request.MinimumLimit && request.ActualValue <= request.MaximumLimit;
        if (!within && string.IsNullOrWhiteSpace(request.DeviationDescription))
            throw new InvalidOperationException("An out-of-limit measurement requires a deviation description.");
        var record = new HACCPRecord
        {
            PondId = request.PondId,
            RecordNumber = request.RecordNumber.Trim(),
            RecordDate = request.RecordDate.Date,
            HazardType = request.HazardType.Trim(),
            HazardDescription = request.HazardDescription.Trim(),
            Severity = request.Severity,
            Likelihood = request.Likelihood,
            RiskLevel = RiskLevel(request.Severity, request.Likelihood),
            ControlPoint = request.ControlPoint.Trim(),
            ControlPointDescription = request.ControlPointDescription?.Trim(),
            ControlMeasureType = request.ControlMeasureType,
            MonitoringMethod = request.MonitoringMethod.Trim(),
            Frequency = request.Frequency,
            MeasurementUnit = request.MeasurementUnit.Trim(),
            MinimumLimit = request.MinimumLimit,
            MaximumLimit = request.MaximumLimit,
            TargetValue = request.TargetValue,
            ActualValue = request.ActualValue,
            MeasurementTime = request.MeasurementTime,
            AcceptanceCriteria = request.AcceptanceCriteria.Trim(),
            IsWithinLimits = within,
            DeviationOccurred = !within,
            DeviationDescription = within ? null : request.DeviationDescription!.Trim(),
            Status = within ? ComplianceStatus.Compliant : ComplianceStatus.NonCompliant,
            LifecycleStatus = within
                ? HaccpRecordLifecycleStatus.AwaitingVerification
                : HaccpRecordLifecycleStatus.Open,
            ReferenceDocument = request.ReferenceDocument?.Trim(),
            EquipmentUsed = request.EquipmentUsed?.Trim(),
            Location = request.Location?.Trim(),
            Notes = request.Notes?.Trim(),
            RecordedBy = actor.Trim(),
            CreatedBy = actor.Trim(),
            CreatedAt = DateTime.UtcNow
        };
        _context.HACCPRecords.Add(record);
        _context.SaveChanges();
        AddAudit(record, "RecordMeasurement", actor, reason, new
        {
            record.ActualValue,
            record.MinimumLimit,
            record.MaximumLimit,
            record.IsWithinLimits,
            record.DeviationOccurred,
            record.ControlMeasureType,
            record.RiskLevel
        });
        _context.SaveChanges();
        return record;
    }

    public HACCPRecord ApplyCorrectiveAction(int recordId, string rootCause, string correctiveAction,
        string owner, DateTime actionDate, DateTime dueDate, string actor, string reason)
    {
        RequireActorAndReason(actor, reason);
        RequireText(rootCause, 1000, "Root cause");
        RequireText(correctiveAction, 1000, "Corrective action");
        RequireText(owner, 100, "Corrective-action owner");
        using var transaction = _context.Database.BeginTransaction(IsolationLevel.Serializable);
        var record = _context.HACCPRecords.SingleOrDefault(value => value.Id == recordId)
            ?? throw new InvalidOperationException("The HACCP record does not exist.");
        if (!record.DeviationOccurred || record.LifecycleStatus == HaccpRecordLifecycleStatus.Closed)
            throw new InvalidOperationException("Corrective action is allowed only for an open HACCP deviation.");
        if (actionDate < record.MeasurementTime || dueDate.Date < actionDate.Date)
            throw new InvalidOperationException("Corrective-action dates are inconsistent with the measurement timeline.");
        record.RootCause = rootCause.Trim();
        record.CorrectiveActions = correctiveAction.Trim();
        record.CorrectiveActionOwner = owner.Trim();
        record.CorrectiveActionDueDate = dueDate.Date;
        record.ActionTakenDate = actionDate;
        record.ActionTakenBy = actor.Trim();
        record.LifecycleStatus = HaccpRecordLifecycleStatus.AwaitingVerification;
        record.UpdatedAt = DateTime.UtcNow;
        record.UpdatedBy = actor.Trim();
        AddAudit(record, "ApplyCorrectiveAction", actor, reason, new
        {
            record.RootCause,
            record.CorrectiveActions,
            record.CorrectiveActionOwner,
            record.CorrectiveActionDueDate,
            record.ActionTakenDate
        });
        _context.SaveChanges();
        transaction.Commit();
        return record;
    }

    public HACCPRecord VerifyEffectiveness(int recordId, bool effective, DateTime verificationDate,
        string verifier, string reason)
    {
        RequireActorAndReason(verifier, reason);
        using var transaction = _context.Database.BeginTransaction(IsolationLevel.Serializable);
        var record = _context.HACCPRecords.SingleOrDefault(value => value.Id == recordId)
            ?? throw new InvalidOperationException("The HACCP record does not exist.");
        if (record.LifecycleStatus != HaccpRecordLifecycleStatus.AwaitingVerification)
            throw new InvalidOperationException("The HACCP record is not awaiting verification.");
        if (string.Equals(record.RecordedBy, verifier, StringComparison.OrdinalIgnoreCase)
            || string.Equals(record.ActionTakenBy, verifier, StringComparison.OrdinalIgnoreCase)
            || string.Equals(record.CorrectiveActionOwner, verifier, StringComparison.OrdinalIgnoreCase))
            throw new InvalidOperationException("HACCP effectiveness must be verified independently.");
        var earliest = record.ActionTakenDate ?? record.MeasurementTime;
        if (verificationDate < earliest)
            throw new InvalidOperationException("Verification cannot precede the monitored event or corrective action.");
        if (record.DeviationOccurred && (string.IsNullOrWhiteSpace(record.RootCause)
            || string.IsNullOrWhiteSpace(record.CorrectiveActions)))
            throw new InvalidOperationException("A deviation requires root-cause analysis and corrective action before verification.");

        record.VerifiedBy = verifier.Trim();
        record.VerificationDate = verificationDate;
        record.EffectivenessVerifiedBy = verifier.Trim();
        record.EffectivenessVerificationDate = verificationDate;
        record.EffectivenessVerified = effective;
        record.Verified = effective;
        record.ReviewedBy = verifier.Trim();
        record.ReviewDate = verificationDate;
        record.Status = effective
            ? record.DeviationOccurred ? ComplianceStatus.Corrected : ComplianceStatus.Compliant
            : ComplianceStatus.NonCompliant;
        record.LifecycleStatus = effective
            ? HaccpRecordLifecycleStatus.Closed
            : HaccpRecordLifecycleStatus.CorrectiveActionInProgress;
        record.UpdatedAt = DateTime.UtcNow;
        record.UpdatedBy = verifier.Trim();
        AddAudit(record, effective ? "VerifyEffectiveAndClose" : "VerificationFailed",
            verifier, reason, new { effective, verificationDate, record.Status, record.LifecycleStatus });
        _context.SaveChanges();
        transaction.Commit();
        return record;
    }

    private void AddAudit(HACCPRecord record, string action, string actor, string reason, object details) =>
        _context.AccountingAuditEvents.Add(new AccountingAuditEvent
        {
            OccurredAtUtc = DateTime.UtcNow,
            EntityType = nameof(HACCPRecord),
            EntityId = record.Id.ToString(),
            Action = action,
            ActorUsername = actor.Trim(),
            Reason = reason.Trim(),
            AfterJson = JsonSerializer.Serialize(details),
            CorrelationId = Guid.NewGuid().ToString("N")
        });

    private static string RiskLevel(HazardSeverity severity, HazardLikelihood likelihood)
    {
        var score = (int)severity * (int)likelihood;
        return score >= 15 ? "Critical" : score >= 9 ? "High" : score >= 4 ? "Medium" : "Low";
    }

    private static void RequireActorAndReason(string actor, string reason)
    {
        if (string.IsNullOrWhiteSpace(actor)) throw new ArgumentException("Actor is required.", nameof(actor));
        if (string.IsNullOrWhiteSpace(reason)) throw new ArgumentException("Reason is required.", nameof(reason));
    }

    private static void RequireText(string value, int maxLength, string name)
    {
        if (string.IsNullOrWhiteSpace(value) || value.Trim().Length > maxLength)
            throw new InvalidOperationException($"{name} is required and cannot exceed {maxLength} characters.");
    }
}
