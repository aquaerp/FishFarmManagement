using System.ComponentModel.DataAnnotations;

namespace FishFarmManager.Models;

public sealed class QualityRiskRegister
{
    public int Id { get; set; }
    [Required, StringLength(50)] public string ReferenceNumber { get; set; } = string.Empty;
    [Required, StringLength(200)] public string Title { get; set; } = string.Empty;
    [Required, StringLength(2000)] public string Description { get; set; } = string.Empty;
    public QualityRiskType Type { get; set; } = QualityRiskType.Risk;
    [Required, StringLength(100)] public string Category { get; set; } = string.Empty;
    [StringLength(200)] public string? Source { get; set; }
    [Required, StringLength(100)] public string Owner { get; set; } = string.Empty;
    public int Likelihood { get; set; }
    public int Impact { get; set; }
    public int Score { get; set; }
    [Required, StringLength(2000)] public string TreatmentPlan { get; set; } = string.Empty;
    public DateTime DueDate { get; set; }
    public QualityRiskStatus Status { get; set; } = QualityRiskStatus.Open;
    public bool EffectivenessReviewed { get; set; }
    [StringLength(100)] public string? ReviewedBy { get; set; }
    public DateTime? ReviewDate { get; set; }
    [StringLength(1000)] public string? ReviewNotes { get; set; }
    [Required, StringLength(100)] public string CreatedBy { get; set; } = string.Empty;
    public DateTime CreatedAtUtc { get; set; }
    [StringLength(100)] public string? UpdatedBy { get; set; }
    public DateTime? UpdatedAtUtc { get; set; }
}
