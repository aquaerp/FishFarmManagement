using System.ComponentModel.DataAnnotations;

namespace FishFarmManager.Models;

public sealed class ControlledDocument
{
    public int Id { get; set; }
    [Required, StringLength(50)] public string DocumentCode { get; set; } = string.Empty;
    [Required, StringLength(30)] public string Version { get; set; } = string.Empty;
    [Required, StringLength(200)] public string Title { get; set; } = string.Empty;
    [Required, StringLength(100)] public string Category { get; set; } = string.Empty;
    [Required, StringLength(100)] public string Owner { get; set; } = string.Empty;
    [Required, StringLength(500)] public string StorageReference { get; set; } = string.Empty;
    [Required, StringLength(64)] public string ContentSha256 { get; set; } = string.Empty;
    [StringLength(1000)] public string? ChangeSummary { get; set; }
    public ControlledDocumentStatus Status { get; set; } = ControlledDocumentStatus.Draft;
    public DateTime? EffectiveDate { get; set; }
    [StringLength(100)] public string? ApprovedBy { get; set; }
    public DateTime? ApprovedAtUtc { get; set; }
    [StringLength(500)] public string? ApprovalReason { get; set; }
    public DateTime? ObsoletedAtUtc { get; set; }
    [StringLength(100)] public string? ObsoletedBy { get; set; }
    [Required, StringLength(100)] public string CreatedBy { get; set; } = string.Empty;
    public DateTime CreatedAtUtc { get; set; }
}
