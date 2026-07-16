using FishFarmManager.Data;
using FishFarmManager.Models;
using FishFarmManager.Services;
using Microsoft.EntityFrameworkCore;

namespace FishFarmManager.Tests;

public sealed class Phase5QualityPlanningTests
{
    [Fact]
    public void RiskRegister_ScoresTreatsReviewsAndClosesOnlyAfterEffectiveReview()
    {
        using var database = TestDatabase.Create();
        var service = new QualityPlanningService(database.Context);
        var risk = service.RegisterRisk(new QualityRiskRequest(
            "RISK-Q-001", "Cold-chain interruption", "Loss of cooling could affect harvested fish.",
            QualityRiskType.Risk, "Food safety", "quality-owner", 4, 5,
            "Validate backup cooling and perform monthly alarm tests.", DateTime.UtcNow.AddDays(30)),
            "quality-manager", "Annual quality risk assessment");

        Assert.Equal(20, risk.Score);
        Assert.Throws<InvalidOperationException>(() => service.ReviewRisk(risk.Id,
            QualityRiskStatus.Closed, false, "quality-reviewer", DateTime.UtcNow,
            "Controls were not yet tested.", "Premature closure"));

        var closed = service.ReviewRisk(risk.Id, QualityRiskStatus.Closed, true,
            "quality-reviewer", DateTime.UtcNow, "Alarm and backup cooling test passed.", "Effectiveness review");
        Assert.Equal(QualityRiskStatus.Closed, closed.Status);
        Assert.True(closed.EffectivenessReviewed);
        Assert.Equal(2, database.Context.AccountingAuditEvents.Count(value =>
            value.EntityType == nameof(QualityRiskRegister) && value.EntityId == risk.Id.ToString()));
    }

    [Fact]
    public void QualityObjective_TracksMeasurementAndDerivesAchievement()
    {
        using var database = TestDatabase.Create();
        var service = new QualityPlanningService(database.Context);
        var start = DateTime.UtcNow.Date;
        var objective = service.CreateObjective(new QualityObjectiveRequest(
            "QO-001", "On-time CAPA closure", "Percentage of CAPAs closed by due date",
            80m, 90m, "%", "quality-owner", start, start.AddDays(90)),
            "quality-manager", "Approved annual quality objectives");

        var measured = service.RecordObjectiveMeasurement(objective.Id, 91m, start.AddDays(30),
            "quality-analyst", "Monthly KPI measurement");

        Assert.Equal(QualityObjectiveStatus.Achieved, measured.Status);
        Assert.Equal(91m, measured.CurrentValue);
        Assert.Equal(2, database.Context.AccountingAuditEvents.Count(value =>
            value.EntityType == nameof(QualityObjective) && value.EntityId == objective.Id.ToString()));
    }

    private sealed class TestDatabase : IDisposable
    {
        private readonly string _path;
        public FishFarmContext Context { get; }
        private TestDatabase(string path, FishFarmContext context) { _path = path; Context = context; }

        public static TestDatabase Create()
        {
            var path = Path.Combine(Path.GetTempPath(), $"aquafarm-quality-{Guid.NewGuid():N}.db");
            var context = new FishFarmContext(new DbContextOptionsBuilder<FishFarmContext>()
                .UseSqlite($"Data Source={path};Pooling=False").Options);
            context.Database.Migrate();
            return new TestDatabase(path, context);
        }

        public void Dispose()
        {
            Context.Dispose();
            if (File.Exists(_path)) File.Delete(_path);
        }
    }
}
