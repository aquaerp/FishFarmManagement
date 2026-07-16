using FishFarmManager.Data;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace FishFarmManager.Migrations;

[DbContext(typeof(FishFarmContext))]
[Migration("20260716050000_AddGovernedHaccpMonitoring")]
public sealed class AddGovernedHaccpMonitoring : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.AddColumn<int>("ControlMeasureType", "HACCPRecords", "INTEGER", nullable: false, defaultValue: 1);
        migrationBuilder.AddColumn<string>("RootCause", "HACCPRecords", "TEXT", maxLength: 1000, nullable: true);
        migrationBuilder.AddColumn<string>("CorrectiveActionOwner", "HACCPRecords", "TEXT", maxLength: 100, nullable: true);
        migrationBuilder.AddColumn<DateTime>("CorrectiveActionDueDate", "HACCPRecords", "TEXT", nullable: true);
        migrationBuilder.AddColumn<bool>("EffectivenessVerified", "HACCPRecords", "INTEGER", nullable: false, defaultValue: false);
        migrationBuilder.AddColumn<string>("EffectivenessVerifiedBy", "HACCPRecords", "TEXT", maxLength: 100, nullable: true);
        migrationBuilder.AddColumn<DateTime>("EffectivenessVerificationDate", "HACCPRecords", "TEXT", nullable: true);
        migrationBuilder.AddColumn<int>("LifecycleStatus", "HACCPRecords", "INTEGER", nullable: false, defaultValue: 1);
        migrationBuilder.CreateIndex("IX_HACCPRecords_RecordNumber", "HACCPRecords", "RecordNumber");

        migrationBuilder.Sql("""
            CREATE TRIGGER TRG_HACCPRecords_UniqueNumber_Insert
            BEFORE INSERT ON HACCPRecords
            WHEN EXISTS (SELECT 1 FROM HACCPRecords h WHERE h.RecordNumber = NEW.RecordNumber)
            BEGIN SELECT RAISE(ABORT, 'HACCP record number already exists'); END;
            """);
        migrationBuilder.Sql("""
            CREATE TRIGGER TRG_HACCPRecords_UniqueNumber_Update
            BEFORE UPDATE OF RecordNumber ON HACCPRecords
            WHEN NEW.RecordNumber <> OLD.RecordNumber AND EXISTS (
                SELECT 1 FROM HACCPRecords h WHERE h.RecordNumber = NEW.RecordNumber AND h.Id <> OLD.Id)
            BEGIN SELECT RAISE(ABORT, 'HACCP record number already exists'); END;
            """);
        migrationBuilder.Sql("""
            CREATE TRIGGER TRG_HACCPRecords_GovernedShape_Insert
            BEFORE INSERT ON HACCPRecords
            WHEN NOT (
                CAST(NEW.MinimumLimit AS NUMERIC) <= CAST(NEW.TargetValue AS NUMERIC)
                AND CAST(NEW.TargetValue AS NUMERIC) <= CAST(NEW.MaximumLimit AS NUMERIC)
                AND ((CAST(NEW.ActualValue AS NUMERIC) >= CAST(NEW.MinimumLimit AS NUMERIC)
                      AND CAST(NEW.ActualValue AS NUMERIC) <= CAST(NEW.MaximumLimit AS NUMERIC)
                      AND NEW.IsWithinLimits = 1 AND NEW.DeviationOccurred = 0)
                  OR (NOT (CAST(NEW.ActualValue AS NUMERIC) >= CAST(NEW.MinimumLimit AS NUMERIC)
                           AND CAST(NEW.ActualValue AS NUMERIC) <= CAST(NEW.MaximumLimit AS NUMERIC))
                      AND NEW.IsWithinLimits = 0 AND NEW.DeviationOccurred = 1
                      AND length(trim(NEW.DeviationDescription)) > 0))
                AND (NEW.ControlMeasureType = 1 OR
                    (NEW.ControlMeasureType IN (2, 3) AND NEW.ReferenceDocument IS NOT NULL
                     AND length(trim(NEW.ReferenceDocument)) > 0))
                AND (NEW.LifecycleStatus <> 4 OR
                    (NEW.Verified = 1 AND NEW.EffectivenessVerified = 1
                     AND NEW.VerificationDate IS NOT NULL AND NEW.EffectivenessVerificationDate IS NOT NULL)))
            BEGIN SELECT RAISE(ABORT, 'invalid governed HACCP record'); END;
            """);
        migrationBuilder.Sql("""
            CREATE TRIGGER TRG_HACCPRecords_GovernedShape_Update
            BEFORE UPDATE ON HACCPRecords
            WHEN NOT (
                CAST(NEW.MinimumLimit AS NUMERIC) <= CAST(NEW.TargetValue AS NUMERIC)
                AND CAST(NEW.TargetValue AS NUMERIC) <= CAST(NEW.MaximumLimit AS NUMERIC)
                AND ((CAST(NEW.ActualValue AS NUMERIC) >= CAST(NEW.MinimumLimit AS NUMERIC)
                      AND CAST(NEW.ActualValue AS NUMERIC) <= CAST(NEW.MaximumLimit AS NUMERIC)
                      AND NEW.IsWithinLimits = 1 AND NEW.DeviationOccurred = 0)
                  OR (NOT (CAST(NEW.ActualValue AS NUMERIC) >= CAST(NEW.MinimumLimit AS NUMERIC)
                           AND CAST(NEW.ActualValue AS NUMERIC) <= CAST(NEW.MaximumLimit AS NUMERIC))
                      AND NEW.IsWithinLimits = 0 AND NEW.DeviationOccurred = 1
                      AND length(trim(NEW.DeviationDescription)) > 0))
                AND (NEW.ControlMeasureType = 1 OR
                    (NEW.ControlMeasureType IN (2, 3) AND NEW.ReferenceDocument IS NOT NULL
                     AND length(trim(NEW.ReferenceDocument)) > 0))
                AND (NEW.LifecycleStatus <> 4 OR
                    (NEW.Verified = 1 AND NEW.EffectivenessVerified = 1
                     AND NEW.VerificationDate IS NOT NULL AND NEW.EffectivenessVerificationDate IS NOT NULL)))
            BEGIN SELECT RAISE(ABORT, 'invalid governed HACCP record'); END;
            """);
        migrationBuilder.Sql("""
            CREATE TRIGGER TRG_HACCPRecords_ClosedImmutable
            BEFORE UPDATE ON HACCPRecords WHEN OLD.LifecycleStatus = 4
            BEGIN SELECT RAISE(ABORT, 'closed HACCP record is immutable'); END;
            """);
        migrationBuilder.Sql("""
            CREATE TRIGGER TRG_HACCPRecords_ClosedNoDelete
            BEFORE DELETE ON HACCPRecords WHEN OLD.LifecycleStatus = 4
            BEGIN SELECT RAISE(ABORT, 'closed HACCP record cannot be deleted'); END;
            """);
    }

    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.Sql("DROP TRIGGER IF EXISTS TRG_HACCPRecords_ClosedNoDelete;");
        migrationBuilder.Sql("DROP TRIGGER IF EXISTS TRG_HACCPRecords_ClosedImmutable;");
        migrationBuilder.Sql("DROP TRIGGER IF EXISTS TRG_HACCPRecords_GovernedShape_Update;");
        migrationBuilder.Sql("DROP TRIGGER IF EXISTS TRG_HACCPRecords_GovernedShape_Insert;");
        migrationBuilder.Sql("DROP TRIGGER IF EXISTS TRG_HACCPRecords_UniqueNumber_Update;");
        migrationBuilder.Sql("DROP TRIGGER IF EXISTS TRG_HACCPRecords_UniqueNumber_Insert;");
        migrationBuilder.DropIndex("IX_HACCPRecords_RecordNumber", "HACCPRecords");
        migrationBuilder.DropColumn("LifecycleStatus", "HACCPRecords");
        migrationBuilder.DropColumn("EffectivenessVerificationDate", "HACCPRecords");
        migrationBuilder.DropColumn("EffectivenessVerifiedBy", "HACCPRecords");
        migrationBuilder.DropColumn("EffectivenessVerified", "HACCPRecords");
        migrationBuilder.DropColumn("CorrectiveActionDueDate", "HACCPRecords");
        migrationBuilder.DropColumn("CorrectiveActionOwner", "HACCPRecords");
        migrationBuilder.DropColumn("RootCause", "HACCPRecords");
        migrationBuilder.DropColumn("ControlMeasureType", "HACCPRecords");
    }
}
