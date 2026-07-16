using System.ComponentModel.DataAnnotations;

namespace FishFarmManager.Models;

public sealed class RecallExercise
{
    public int Id { get; set; }
    [Required, StringLength(50)] public string ExerciseNumber { get; set; } = string.Empty;
    [Required, StringLength(100)] public string TriggerLotCode { get; set; } = string.Empty;
    public DateTime StartedAtUtc { get; set; }
    public DateTime CompletedAtUtc { get; set; }
    public int AffectedOrderCount { get; set; }
    public int AffectedCustomerCount { get; set; }
    public int AffectedHarvestLotCount { get; set; }
    public bool PassedTwoHourTarget { get; set; }
    [Required, StringLength(100)] public string ConductedBy { get; set; } = string.Empty;
    [StringLength(2000)] public string? Findings { get; set; }
    public DateTime CreatedAtUtc { get; set; }
}
