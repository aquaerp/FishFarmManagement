namespace FishFarmManager.Models;

/// <summary>
/// Append-only security and administration audit event. Secrets must never be stored here.
/// </summary>
public sealed class SecurityAuditEvent
{
    public long Id { get; set; }
    public DateTime OccurredAtUtc { get; set; }
    public string Category { get; set; } = string.Empty;
    public string Action { get; set; } = string.Empty;
    public string Outcome { get; set; } = string.Empty;
    public int? ActorUserId { get; set; }
    public string ActorUsername { get; set; } = string.Empty;
    public string? SubjectType { get; set; }
    public string? SubjectId { get; set; }
    public string CorrelationId { get; set; } = string.Empty;
    public string? Details { get; set; }
}
