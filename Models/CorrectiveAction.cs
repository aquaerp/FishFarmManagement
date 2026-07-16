using System.ComponentModel.DataAnnotations;

namespace FishFarmManager.Models;

public sealed class CorrectiveAction
{
    public int Id { get; set; }
    [Required, StringLength(50)] public string ReferenceNumber { get; set; } = string.Empty;
    [Required, StringLength(1000)] public string Nonconformity { get; set; } = string.Empty;
    [Required, StringLength(1000)] public string RootCause { get; set; } = string.Empty;
    [Required, StringLength(1000)] public string ActionPlan { get; set; } = string.Empty;
    [Required, StringLength(100)] public string Owner { get; set; } = string.Empty;
    public DateTime DueDate { get; set; }
    public CapaStatus Status { get; set; } = CapaStatus.Open;
    public DateTime? CompletedAtUtc { get; set; }
    [StringLength(100)] public string? CompletedBy { get; set; }
    public bool EffectivenessVerified { get; set; }
    [StringLength(100)] public string? VerifiedBy { get; set; }
    public DateTime? VerifiedAtUtc { get; set; }
    [StringLength(1000)] public string? VerificationNotes { get; set; }
    [Required, StringLength(100)] public string CreatedBy { get; set; } = string.Empty;
    public DateTime CreatedAtUtc { get; set; }
}
