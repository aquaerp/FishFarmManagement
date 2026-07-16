using FishFarmManager.Data;
using FishFarmManager.Models;
using FishFarmManager.Services;
using Microsoft.EntityFrameworkCore;

namespace FishFarmManager.Tests;

public sealed class Phase5HaccpMonitoringTests
{
    [Fact]
    public void CcpDeviation_IsDerivedCorrectedIndependentlyClosedAndMadeImmutable()
    {
        using var database = TestDatabase.Create();
        var service = new HaccpMonitoringService(database.Context);
        var measuredAt = new DateTime(2026, 7, 16, 8, 0, 0, DateTimeKind.Utc);

        var record = service.RecordMeasurement(Request(database.PondId, 4.5m, measuredAt),
            "operator-1", "Routine CCP monitoring");

        Assert.False(record.IsWithinLimits);
        Assert.True(record.DeviationOccurred);
        Assert.Equal(ComplianceStatus.NonCompliant, record.Status);
        Assert.Equal(HaccpRecordLifecycleStatus.Open, record.LifecycleStatus);
        Assert.Equal("Critical", record.RiskLevel);
        service.ApplyCorrectiveAction(record.Id, "Cooling unit stopped",
            "Isolate affected product and restore cooling", "quality-owner",
            measuredAt.AddMinutes(10), measuredAt.AddHours(2), "operator-1", "Immediate correction documented");
        Assert.Throws<InvalidOperationException>(() => service.VerifyEffectiveness(record.Id, true,
            measuredAt.AddHours(1), "operator-1", "Self verification prohibited"));

        var closed = service.VerifyEffectiveness(record.Id, true, measuredAt.AddHours(1),
            "quality-verifier", "Temperature restored and isolated product disposition confirmed");

        Assert.True(closed.Verified);
        Assert.True(closed.EffectivenessVerified);
        Assert.Equal(ComplianceStatus.Corrected, closed.Status);
        Assert.Equal(HaccpRecordLifecycleStatus.Closed, closed.LifecycleStatus);
        Assert.Equal(3, database.Context.AccountingAuditEvents.Count(value =>
            value.EntityType == nameof(HACCPRecord) && value.EntityId == record.Id.ToString()));
        database.Context.ChangeTracker.Clear();
        var stored = database.Context.HACCPRecords.Single(value => value.Id == record.Id);
        stored.Notes = "tampered";
        Assert.Throws<DbUpdateException>(() => database.Context.SaveChanges());
        database.Context.ChangeTracker.Clear();
        stored = database.Context.HACCPRecords.Single(value => value.Id == record.Id);
        database.Context.Remove(stored);
        Assert.Throws<DbUpdateException>(() => database.Context.SaveChanges());
    }

    [Fact]
    public void Monitoring_GovernsReferencesLimitsDuplicateNumbersAndFailedEffectiveness()
    {
        using var database = TestDatabase.Create();
        var service = new HaccpMonitoringService(database.Context);
        var measuredAt = new DateTime(2026, 7, 16, 9, 0, 0, DateTimeKind.Utc);
        var noReference = Request(database.PondId, 2.5m, measuredAt) with { ReferenceDocument = null };
        Assert.Throws<InvalidOperationException>(() => service.RecordMeasurement(
            noReference, "operator-2", "Missing CCP plan"));
        var record = service.RecordMeasurement(Request(database.PondId, 2.5m, measuredAt),
            "operator-2", "CCP deviation recorded");
        Assert.Throws<InvalidOperationException>(() => service.RecordMeasurement(
            Request(database.PondId, 2.8m, measuredAt) with { RecordNumber = record.RecordNumber },
            "operator-3", "Duplicate record number"));
        service.ApplyCorrectiveAction(record.Id, "Ice replenishment delay", "Replenish ice and hold batch",
            "quality-owner", measuredAt.AddMinutes(5), measuredAt.AddHours(1),
            "operator-2", "First corrective action");

        var failed = service.VerifyEffectiveness(record.Id, false, measuredAt.AddMinutes(30),
            "quality-verifier", "Temperature remained above the critical limit");

        Assert.False(failed.Verified);
        Assert.Equal(HaccpRecordLifecycleStatus.CorrectiveActionInProgress, failed.LifecycleStatus);
        Assert.Equal(ComplianceStatus.NonCompliant, failed.Status);
        service.ApplyCorrectiveAction(record.Id, "Insufficient ice reserve",
            "Move batch to validated cold room and replenish safety stock", "quality-owner",
            measuredAt.AddMinutes(35), measuredAt.AddHours(2), "operator-3", "Escalated correction");
        Assert.Equal(HaccpRecordLifecycleStatus.AwaitingVerification, record.LifecycleStatus);
    }

    private static HaccpMeasurementRequest Request(int pondId, decimal actual, DateTime measuredAt) => new(
        pondId,
        $"HACCP-{Guid.NewGuid():N}",
        measuredAt.Date,
        "Biological",
        "Pathogen growth caused by temperature abuse",
        HazardSeverity.Critical,
        HazardLikelihood.Likely,
        "Cold storage",
        FoodSafetyControlMeasureType.Ccp,
        "Calibrated probe measurement",
        MonitoringFrequency.Daily,
        "°C",
        0m,
        2m,
        1m,
        actual,
        measuredAt,
        "Product temperature must not exceed 2°C",
        "Measured product temperature exceeded the critical limit",
        "HACCP-PLAN-CCP-01");

    private sealed class TestDatabase : IDisposable
    {
        private readonly string _path;
        public FishFarmContext Context { get; }
        public int PondId { get; }

        private TestDatabase(string path, FishFarmContext context, int pondId)
        {
            _path = path;
            Context = context;
            PondId = pondId;
        }

        public static TestDatabase Create()
        {
            var path = Path.Combine(Path.GetTempPath(), $"aquafarm-haccp-{Guid.NewGuid():N}.db");
            var context = new FishFarmContext(new DbContextOptionsBuilder<FishFarmContext>()
                .UseSqlite($"Data Source={path};Pooling=False").Options);
            context.Database.Migrate();
            var pond = new Pond
            {
                Name = "HACCP test pond", Capacity = 100m, Area = 50m, Depth = 2m,
                Status = PondStatus.Active, PondType = PondType.GrowOut, CreatedAt = DateTime.UtcNow
            };
            context.Ponds.Add(pond);
            context.SaveChanges();
            return new TestDatabase(path, context, pond.Id);
        }

        public void Dispose()
        {
            Context.Dispose();
            if (File.Exists(_path)) File.Delete(_path);
        }
    }
}
