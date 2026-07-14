using FishFarmManager.Data;
using FishFarmManager.Models;

namespace FishFarmManager.Services;

public sealed class SecurityAuditService
{
    private readonly FishFarmContext _context;

    public SecurityAuditService(FishFarmContext context) => _context = context;

    public SecurityAuditEvent Record(
        string category,
        string action,
        string outcome,
        string actorUsername,
        int? actorUserId = null,
        string? subjectType = null,
        string? subjectId = null,
        string? details = null,
        string? correlationId = null)
    {
        var auditEvent = new SecurityAuditEvent
        {
            OccurredAtUtc = DateTime.UtcNow,
            Category = RequireValue(category, nameof(category), 50),
            Action = RequireValue(action, nameof(action), 100),
            Outcome = RequireValue(outcome, nameof(outcome), 30),
            ActorUsername = Truncate(string.IsNullOrWhiteSpace(actorUsername) ? "anonymous" : actorUsername, 100)!,
            ActorUserId = actorUserId,
            SubjectType = Truncate(subjectType, 100),
            SubjectId = Truncate(subjectId, 100),
            CorrelationId = Truncate(correlationId ?? Guid.NewGuid().ToString("N"), 64)!,
            Details = Truncate(details, 1000)
        };

        _context.SecurityAuditEvents.Add(auditEvent);
        _context.SaveChanges();
        return auditEvent;
    }

    private static string RequireValue(string value, string parameterName, int maxLength)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            throw new ArgumentException("Audit values cannot be empty.", parameterName);
        }

        return Truncate(value, maxLength)!;
    }

    private static string? Truncate(string? value, int maxLength) =>
        string.IsNullOrWhiteSpace(value) ? null : value.Length <= maxLength ? value : value[..maxLength];
}
