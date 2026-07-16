using System.ComponentModel.DataAnnotations;

namespace FishFarmManager.Models;

public sealed class QualityObjective
{
    public int Id { get; set; }
    [Required, StringLength(50)] public string ObjectiveCode { get; set; } = string.Empty;
    [Required, StringLength(200)] public string Title { get; set; } = string.Empty;
    [Required, StringLength(100)] public string Measure { get; set; } = string.Empty;
    public decimal BaselineValue { get; set; }
    public decimal TargetValue { get; set; }
    public decimal CurrentValue { get; set; }
    [Required, StringLength(50)] public string Unit { get; set; } = string.Empty;
    [Required, StringLength(100)] public string Owner { get; set; } = string.Empty;
    public DateTime PeriodStart { get; set; }
    public DateTime PeriodEnd { get; set; }
    public QualityObjectiveStatus Status { get; set; } = QualityObjectiveStatus.Active;
    public DateTime? LastMeasuredAtUtc { get; set; }
    [StringLength(1000)] public string? Notes { get; set; }
    [Required, StringLength(100)] public string CreatedBy { get; set; } = string.Empty;
    public DateTime CreatedAtUtc { get; set; }
    [StringLength(100)] public string? UpdatedBy { get; set; }
    public DateTime? UpdatedAtUtc { get; set; }
}
