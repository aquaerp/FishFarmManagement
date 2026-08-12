using FishFarmManager.Data;
using FishFarmManager.Models;
using System.Text.RegularExpressions;

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
            Details = Truncate(RedactSecrets(details), 1000)
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

    public static string? RedactSecrets(string? value)
    {
        if (string.IsNullOrWhiteSpace(value)) return null;
        var redacted = Regex.Replace(value,
            @"(?i)authorization\s*[:=]\s*(?:basic|bearer)\s+[^;,&\s]+",
            "Authorization=[REDACTED]", RegexOptions.CultureInvariant, TimeSpan.FromMilliseconds(100));
        redacted = Regex.Replace(redacted,
            @"(?i)(password|passwd|pwd|secret|token|api[-_]?key|authorization)\s*[:=]\s*([^;,&\s]+)",
            "$1=[REDACTED]", RegexOptions.CultureInvariant, TimeSpan.FromMilliseconds(100));
        redacted = Regex.Replace(redacted, @"(?i)basic\s+[a-z0-9+/=]+", "Basic [REDACTED]",
            RegexOptions.CultureInvariant, TimeSpan.FromMilliseconds(100));
        return redacted;
    }
}
