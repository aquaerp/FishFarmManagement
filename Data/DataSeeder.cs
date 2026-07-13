using FishFarmManager.Models;
using FishFarmManager.Services;
using System;
using System.Linq;
using System.Security.Cryptography;
using System.Text;

namespace FishFarmManager.Data
{
    public static class DataSeeder
    {
        public static void SeedData(FishFarmContext context)
        {
            // 0. إنشاء مستخدم افتراضي أولاً (Admin)
            SeedDefaultUsers(context);

            // التحقق من وجود بيانات - إذا كانت موجودة لا نضيف
            if (context.Ponds.Any() || context.Equipment.Any())
                return;

            LoggingService.LogInfo("🌱 بدء إضافة البيانات التجريبية...");

            // 1. Seed Ponds (الأحواض)
            var ponds = new[]
            {
                new Pond { Name = "حوض الإنتاج الرئيسي 1", PondType = PondType.GrowOut, Area = 500m, Depth = 2.5m, Capacity = 10000, Status = PondStatus.Stocked, CreatedDate = DateTime.Now.AddYears(-2), Notes = "حوض رئيسي للإنتاج" },
                new Pond { Name = "حوض الإنتاج الرئيسي 2", PondType = PondType.GrowOut, Area = 500m, Depth = 2.5m, Capacity = 10000, Status = PondStatus.Stocked, CreatedDate = DateTime.Now.AddYears(-2), Notes = "حوض رئيسي للإنتاج" },
                new Pond { Name = "حوض التحضين 1", PondType = PondType.Nursery, Area = 200m, Depth = 1.5m, Capacity = 5000, Status = PondStatus.Stocked, CreatedDate = DateTime.Now.AddYears(-1), Notes = "حوض تحضين الزريعة" },
                new Pond { Name = "حوض التسمين 1", PondType = PondType.GrowOut, Area = 400m, Depth = 2.0m, Capacity = 8000, Status = PondStatus.Stocked, CreatedDate = DateTime.Now.AddYears(-1), Notes = "حوض تسمين الأسماك" },
                new Pond { Name = "حوض التسمين 2", PondType = PondType.GrowOut, Area = 400m, Depth = 2.0m, Capacity = 8000, Status = PondStatus.Maintenance, CreatedDate = DateTime.Now.AddYears(-1), Notes = "تحت الصيانة - سيتم تشغيله قريباً" },
                new Pond { Name = "حوض العزل", PondType = PondType.Quarantine, Area = 100m, Depth = 1.5m, Capacity = 2000, Status = PondStatus.Empty, CreatedDate = DateTime.Now.AddMonths(-6), Notes = "حوض عزل الأسماك المريضة" }
            };
            context.Ponds.AddRange(ponds);
            context.SaveChanges();

            // 2. Seed Production Cycles (دورات الإنتاج)
            var cycles = new[]
            {
                new ProductionCycle
                {
                    Name = "دورة البلطي الرئيسية 2025",
                    StartDate = DateTime.Now.AddMonths(-4),
                    Status = CycleStatus.Active,
                    CycleType = CycleType.GrowOut,
                    InitialFishCount = 10000,
                    InitialAverageWeight = 0.05m,
                    FrySource = "مفرخ النيل - كفر الشيخ",
                    HatcherySource = "مفرخ النيل المعتمد",
                    ExpectedHatchDate = DateTime.Now.AddMonths(2),
                    Notes = "دورة إنتاج رئيسية - بلطي نيلي - الهدف 500 كجم"
                },
                new ProductionCycle
                {
                    Name = "دورة القراميط 2025",
                    StartDate = DateTime.Now.AddMonths(-2),
                    Status = CycleStatus.Active,
                    CycleType = CycleType.GrowOut,
                    InitialFishCount = 5000,
                    InitialAverageWeight = 0.08m,
                    FrySource = "مفرخ الإسكندرية",
                    HatcherySource = "مفرخ الإسكندرية المتطور",
                    ExpectedHatchDate = DateTime.Now.AddMonths(4),
                    Notes = "دورة قراميط أفريقي - إنتاج تجاري"
                },
                new ProductionCycle
                {
                    Name = "دورة بلطي أحمر 2024 - مكتملة",
                    StartDate = DateTime.Now.AddMonths(-8),
                    EndDate = DateTime.Now.AddMonths(-2),
                    Status = CycleStatus.Completed,
                    CycleType = CycleType.GrowOut,
                    InitialFishCount = 8000,
                    InitialAverageWeight = 0.05m,
                    FinalFishCount = 7600,
                    FinalAverageWeight = 0.55m,
                    TotalHarvestWeight = 4180,
                    SurvivalRate = 95.0m,
                    FCR = 1.6m,
                    ADG = 2.75m,
                    FrySource = "مفرخ الدلتا",
                    HatcherySource = "مفرخ الدلتا",
                    Notes = "دورة مكتملة بنجاح - حصاد ممتاز 4.18 طن"
                }
            };
            context.ProductionCycles.AddRange(cycles);
            context.SaveChanges();
            
            LoggingService.LogInfo("✅ تم إضافة دورات الإنتاج - IDs: {Ids}", 
                string.Join(", ", cycles.Select(c => c.Id)));

            // 3. Link Ponds to Cycles (ربط الأحواض بالدورات)
            LoggingService.LogInfo("بدء ربط الأحواض بالدورات - Cycle IDs: {CycleIds}, Pond IDs: {PondIds}",
                string.Join(", ", cycles.Select(c => c.Id)),
                string.Join(", ", ponds.Select(p => p.Id)));
            
            var cyclePonds = new[]
            {
                new ProductionCyclePond { ProductionCycleId = cycles[0].Id, PondId = ponds[0].Id },
                new ProductionCyclePond { ProductionCycleId = cycles[0].Id, PondId = ponds[1].Id },
                new ProductionCyclePond { ProductionCycleId = cycles[1].Id, PondId = ponds[3].Id },
                new ProductionCyclePond { ProductionCycleId = cycles[2].Id, PondId = ponds[0].Id }
            };
            context.ProductionCyclePonds.AddRange(cyclePonds);
            context.SaveChanges();
            LoggingService.LogInfo("✅ تم ربط الأحواض بالدورات بنجاح");

            // 4. Seed Water Quality Records (سجلات جودة المياه)
            LoggingService.LogInfo("بدء إضافة سجلات جودة المياه - استخدام Cycle IDs: {CycleIds}",
                string.Join(", ", cycles.Select(c => c.Id)));
            
            var waterRecords = new[]
            {
                new WaterQualityRecord { PondId = ponds[0].Id, CycleId = cycles[0].Id, RecordDate = DateTime.Now.AddDays(-1), MeasurementDate = DateTime.Now.AddDays(-1), Temperature = 27.5m, DissolvedOxygen = 6.2m, pH = 7.5m, Ammonia = 0.02m, Nitrite = 0.01m, Nitrate = 5.0m, Status = WaterQualityStatus.Excellent, Notes = "جودة ممتازة" },
                new WaterQualityRecord { PondId = ponds[0].Id, CycleId = cycles[0].Id, RecordDate = DateTime.Now.AddDays(-2), MeasurementDate = DateTime.Now.AddDays(-2), Temperature = 28.0m, DissolvedOxygen = 5.8m, pH = 7.6m, Ammonia = 0.03m, Nitrite = 0.02m, Nitrate = 6.0m, Status = WaterQualityStatus.Good, Notes = "جودة جيدة" },
                new WaterQualityRecord { PondId = ponds[3].Id, CycleId = cycles[1].Id, RecordDate = DateTime.Now.AddDays(-1), MeasurementDate = DateTime.Now.AddDays(-1), Temperature = 26.0m, DissolvedOxygen = 6.5m, pH = 7.4m, Ammonia = 0.01m, Nitrite = 0.01m, Nitrate = 4.0m, Status = WaterQualityStatus.Excellent, Notes = "مياه ممتازة - القراميط بحالة جيدة" },
                new WaterQualityRecord { PondId = ponds[0].Id, CycleId = cycles[2].Id, RecordDate = DateTime.Now.AddMonths(-3), MeasurementDate = DateTime.Now.AddMonths(-3), Temperature = 25.5m, DissolvedOxygen = 6.0m, pH = 7.3m, Ammonia = 0.02m, Nitrite = 0.01m, Nitrate = 5.5m, Status = WaterQualityStatus.Good, Notes = "من الدورة المكتملة" }
            };
            context.WaterQualityRecords.AddRange(waterRecords);
            context.SaveChanges();
            LoggingService.LogInfo("✅ تم إضافة سجلات جودة المياه بنجاح");

            // 5. Seed Feeding Records (سجلات التغذية)
            var feedingRecords = new[]
            {
                new FeedingRecord { CycleId = cycles[0].Id, FeedingDate = DateTime.Now.AddDays(-1), FeedType = FeedType.Starter, Quantity = 15.5, FeedPrice = 18.0, FeedingTimes = 3, EstimatedFishWeight = 0.45, EstimatedFishCount = 9200, RecordedBy = "محمد حسن", CreatedAt = DateTime.Now.AddDays(-1), Notes = "تغذية صباحية ومسائية" },
                new FeedingRecord { CycleId = cycles[0].Id, FeedingDate = DateTime.Now.AddDays(-2), FeedType = FeedType.Starter, Quantity = 15.0, FeedPrice = 18.0, FeedingTimes = 3, EstimatedFishWeight = 0.44, EstimatedFishCount = 9200, RecordedBy = "عمر حسين", CreatedAt = DateTime.Now.AddDays(-2), Notes = "تغذية منتظمة" },
                new FeedingRecord { CycleId = cycles[1].Id, FeedingDate = DateTime.Now.AddDays(-1), FeedType = FeedType.Grower, Quantity = 12.0, FeedPrice = 16.5, FeedingTimes = 2, EstimatedFishWeight = 0.35, EstimatedFishCount = 4800, RecordedBy = "عمر حسين", CreatedAt = DateTime.Now.AddDays(-1), Notes = "علف نمو للقراميط" },
                new FeedingRecord { CycleId = cycles[1].Id, FeedingDate = DateTime.Now.AddDays(-2), FeedType = FeedType.Grower, Quantity = 11.5, FeedPrice = 16.5, FeedingTimes = 2, EstimatedFishWeight = 0.34, EstimatedFishCount = 4800, RecordedBy = "محمد حسن", CreatedAt = DateTime.Now.AddDays(-2), Notes = "علف نمو" },
                new FeedingRecord { CycleId = cycles[0].Id, FeedingDate = DateTime.Now.AddDays(-7), FeedType = FeedType.Starter, Quantity = 14.0, FeedPrice = 18.0, FeedingTimes = 3, RecordedBy = "محمد حسن", CreatedAt = DateTime.Now.AddDays(-7), Notes = "أسبوع سابق" },
                new FeedingRecord { CycleId = cycles[2].Id, FeedingDate = DateTime.Now.AddMonths(-3), FeedType = FeedType.Finisher, Quantity = 25.0, FeedPrice = 15.0, FeedingTimes = 2, RecordedBy = "عمر حسين", CreatedAt = DateTime.Now.AddMonths(-3), Notes = "من الدورة المكتملة" }
            };
            context.FeedingRecords.AddRange(feedingRecords);
            context.SaveChanges();

            // 6. Seed Batch Records (سجلات الدفعات)
            var batchRecords = new[]
            {
                new BatchRecord { BatchType = "زريعة بلطي نيلي", Source = "مفرخ النيل - كفر الشيخ", Quantity = 10000, Unit = "سمكة", ArrivalDate = DateTime.Now.AddMonths(-4), RelatedCycleId = cycles[0].Id, RecordedBy = "أحمد محمود", CreatedAt = DateTime.Now.AddMonths(-4), Notes = "زريعة عالية الجودة - وزن 0.05 جم" },
                new BatchRecord { BatchType = "علف بادئ 40% بروتين", Source = "شركة الإسكندرية للأعلاف", Quantity = 500, Unit = "كجم", ArrivalDate = DateTime.Now.AddMonths(-4), RecordedBy = "محمد حسن", CreatedAt = DateTime.Now.AddMonths(-4), Notes = "علف بادئ للزريعة الجديدة" },
                new BatchRecord { BatchType = "زريعة قراميط أفريقي", Source = "مفرخ الإسكندرية", Quantity = 5000, Unit = "سمكة", ArrivalDate = DateTime.Now.AddMonths(-2), RelatedCycleId = cycles[1].Id, RecordedBy = "أحمد محمود", CreatedAt = DateTime.Now.AddMonths(-2), Notes = "زريعة قراميط - وزن 0.08 جم" },
                new BatchRecord { BatchType = "علف نمو 32% بروتين", Source = "شركة الإسكندرية للأعلاف", Quantity = 1000, Unit = "كجم", ArrivalDate = DateTime.Now.AddMonths(-1), RecordedBy = "محمد حسن", CreatedAt = DateTime.Now.AddMonths(-1), Notes = "علف نمو - تجديد المخزون" }
            };
            context.BatchRecords.AddRange(batchRecords);
            context.SaveChanges();

            // 7. Seed Mortality Records (سجلات النفوق)
            var mortalityRecords = new[]
            {
                new MortalityRecord { CycleId = cycles[0].Id, Date = DateTime.Now.AddDays(-3), DeadFishCount = 15, AverageWeight = 0.43, Cause = MortalityCause.Unknown, CauseDescription = "نفوق طبيعي", ActionTaken = "إزالة الأسماك النافقة", RecordedBy = "محمد حسن", CreatedAt = DateTime.Now.AddDays(-3), Notes = "نفوق ضمن المعدل الطبيعي" },
                new MortalityRecord { CycleId = cycles[0].Id, Date = DateTime.Now.AddDays(-10), DeadFishCount = 25, AverageWeight = 0.40, Cause = MortalityCause.WaterQuality, CauseDescription = "انخفاض الأوكسجين المفاجئ", ActionTaken = "زيادة التهوية وتغيير 30% من المياه", Treatment = "تشغيل جميع المضخات + إضافة أوكسجين", RecordedBy = "محمد حسن", CreatedAt = DateTime.Now.AddDays(-10), Notes = "حادث انقطاع كهرباء ليلاً - تم حل المشكلة" },
                new MortalityRecord { CycleId = cycles[1].Id, Date = DateTime.Now.AddDays(-5), DeadFishCount = 8, AverageWeight = 0.33, Cause = MortalityCause.Unknown, CauseDescription = "نفوق طبيعي", ActionTaken = "إزالة الأسماك", RecordedBy = "عمر حسين", CreatedAt = DateTime.Now.AddDays(-5), Notes = "معدل نفوق منخفض جداً" },
                new MortalityRecord { CycleId = cycles[2].Id, Date = DateTime.Now.AddMonths(-4), DeadFishCount = 45, AverageWeight = 0.30, Cause = MortalityCause.Disease, CauseDescription = "التهاب بكتيري", ActionTaken = "عزل المصابين وعلاج بالمضاد الحيوي", Treatment = "أوكسي تتراسيكلين 50 جم / 100 كجم لمدة 5 أيام", RecordedBy = "د. فاطمة علي", CreatedAt = DateTime.Now.AddMonths(-4), Notes = "تم علاج الدورة بنجاح - الدورة مكتملة الآن" }
            };
            context.MortalityRecords.AddRange(mortalityRecords);
            context.SaveChanges();

            // 8. Seed Fish Health Records (سجلات الصحة)
            var healthRecords = new[]
            {
                new FishHealthRecord { PondId = ponds[0].Id, CycleId = cycles[0].Id, Date = DateTime.Now.AddDays(-3), HealthStatus = "سليم", Disease = "لا يوجد", ActionTaken = "فحص روتيني", RecordedBy = "د. أحمد محمد", CreatedAt = DateTime.Now.AddDays(-3), Notes = "الأسماك بصحة جيدة - نشاط طبيعي" },
                new FishHealthRecord { PondId = ponds[1].Id, CycleId = cycles[0].Id, Date = DateTime.Now.AddDays(-2), HealthStatus = "سليم", Disease = "لا يوجد", ActionTaken = "فحص روتيني", RecordedBy = "د. فاطمة علي", CreatedAt = DateTime.Now.AddDays(-2), Notes = "حالة ممتازة - شهية جيدة" },
                new FishHealthRecord { PondId = ponds[2].Id, Date = DateTime.Now.AddDays(-4), HealthStatus = "مقبول", Disease = "لا يوجد", ActionTaken = "تحسين التغذية", RecordedBy = "د. أحمد محمد", CreatedAt = DateTime.Now.AddDays(-4), Notes = "بعض الزريعة ضعيفة - يحتاج مراقبة" },
                new FishHealthRecord { PondId = ponds[5].Id, Date = DateTime.Now.AddDays(-1), HealthStatus = "مريض", Disease = "التهاب بكتيري", ActionTaken = "عزل وعلاج", RecordedBy = "د. فاطمة علي", CreatedAt = DateTime.Now.AddDays(-1), Notes = "تم نقل الأسماك المصابة للعزل - احمرار في الخياشيم" }
            };
            context.FishHealthRecords.AddRange(healthRecords);
            context.SaveChanges();

            // 9. Seed Treatment Records (سجلات العلاج)
            var treatmentRecords = new[]
            {
                new TreatmentRecord { PondId = ponds[5].Id, CycleId = cycles[0].Id, TreatmentName = "أوكسي تتراسيكلين", Dosage = 50.0m, Unit = "جم لكل 100 كجم سمك", Date = DateTime.Now.AddDays(-1), WithdrawalPeriod = 5, RecordedBy = "د. فاطمة علي", CreatedAt = DateTime.Now.AddDays(-1), Notes = "جرعة يومية لمدة 5 أيام - علاج التهاب بكتيري" },
                new TreatmentRecord { PondId = ponds[2].Id, CycleId = cycles[0].Id, TreatmentName = "فيتامين C + E", Dosage = 2.0m, Unit = "جم لكل كجم علف", Date = DateTime.Now.AddDays(-4), WithdrawalPeriod = null, RecordedBy = "د. أحمد محمد", CreatedAt = DateTime.Now.AddDays(-4), Notes = "تقوية مناعة الزريعة - جرعة وقائية لمدة 7 أيام" }
            };
            context.TreatmentRecords.AddRange(treatmentRecords);
            context.SaveChanges();

            // 10. Seed Environmental Records (السجلات البيئية)
            var environmentalRecords = new[]
            {
                new EnvironmentalRecord { PondId = ponds[0].Id, CycleId = cycles[0].Id, Date = DateTime.Now.AddDays(-1), Parameter = "حرارة الهواء", Value = 32.5m, Unit = "°C", RecordedBy = "محمد حسن", CreatedAt = DateTime.Now.AddDays(-1), Notes = "يوم مشمس - حار" },
                new EnvironmentalRecord { PondId = ponds[0].Id, CycleId = cycles[0].Id, Date = DateTime.Now.AddDays(-1), Parameter = "رطوبة", Value = 65m, Unit = "%", RecordedBy = "محمد حسن", CreatedAt = DateTime.Now.AddDays(-1), Notes = "رطوبة معتدلة" },
                new EnvironmentalRecord { PondId = ponds[1].Id, CycleId = cycles[0].Id, Date = DateTime.Now.AddDays(-2), Parameter = "حرارة الهواء", Value = 30.0m, Unit = "°C", RecordedBy = "محمد حسن", CreatedAt = DateTime.Now.AddDays(-2), Notes = "طقس معتدل" },
                new EnvironmentalRecord { PondId = ponds[0].Id, CycleId = cycles[0].Id, Date = DateTime.Now.AddDays(-1), Parameter = "سرعة الرياح", Value = 15m, Unit = "km/h", RecordedBy = "محمد حسن", CreatedAt = DateTime.Now.AddDays(-1), Notes = "رياح خفيفة" },
                new EnvironmentalRecord { PondId = ponds[2].Id, Date = DateTime.Now.AddDays(-3), Parameter = "أمطار", Value = 12.5m, Unit = "mm", RecordedBy = "محمد حسن", CreatedAt = DateTime.Now.AddDays(-3), Notes = "أمطار خفيفة صباحاً - تم تغطية الأحواض" }
            };
            context.EnvironmentalRecords.AddRange(environmentalRecords);
            context.SaveChanges();

            // Note: Skipping Staff Records - requires proper Employee entities and EmployeeId
            // Note: Skipping Certification Records - requires proper Certification entities first
            // Users can add both through the UI

            // ============= MAINTENANCE SYSTEM (Week 6) =============

            // 13. Seed Equipment (المعدات)
            var equipment = new[]
            {
                new Equipment { EquipmentNumber = "EQ-001", Name = "مضخة هواء رئيسية 1", Category = "أجهزة تهوية", Manufacturer = "AquaTech", Model = "AT-5000", SerialNumber = "SN-2023-001", PurchaseDate = DateTime.Now.AddYears(-2), PurchasePrice = 25000.0m, Status = "تشغيل", Location = "غرفة المضخات", MaintenanceIntervalDays = 90, LastMaintenanceDate = DateTime.Now.AddDays(-30), NextMaintenanceDate = DateTime.Now.AddDays(60), Notes = "مضخة رئيسية - حرجة" },
                new Equipment { EquipmentNumber = "EQ-002", Name = "مضخة هواء رئيسية 2", Category = "أجهزة تهوية", Manufacturer = "AquaTech", Model = "AT-5000", SerialNumber = "SN-2023-002", PurchaseDate = DateTime.Now.AddYears(-2), PurchasePrice = 25000.0m, Status = "تشغيل", Location = "غرفة المضخات", MaintenanceIntervalDays = 90, LastMaintenanceDate = DateTime.Now.AddDays(-35), NextMaintenanceDate = DateTime.Now.AddDays(55), Notes = "مضخة احتياطية" },
                new Equipment { EquipmentNumber = "EQ-003", Name = "مولد كهرباء ديزل 50 KVA", Category = "مولدات", Manufacturer = "Perkins", Model = "PK-50D", SerialNumber = "PKD-2023-100", PurchaseDate = DateTime.Now.AddYears(-1), PurchasePrice = 80000.0m, Status = "تشغيل", Location = "غرفة المولدات", MaintenanceIntervalDays = 180, LastMaintenanceDate = DateTime.Now.AddDays(-90), NextMaintenanceDate = DateTime.Now.AddDays(90), Notes = "مولد رئيسي - طوارئ" },
                new Equipment { EquipmentNumber = "EQ-004", Name = "مضخة مياه طرد مركزي 5 حصان", Category = "مضخات", Manufacturer = "Pedrollo", Model = "CP-150", SerialNumber = "PD-2024-001", PurchaseDate = DateTime.Now.AddMonths(-6), PurchasePrice = 12000.0m, Status = "تشغيل", Location = "حوض 1", MaintenanceIntervalDays = 120, LastMaintenanceDate = DateTime.Now.AddDays(-20), NextMaintenanceDate = DateTime.Now.AddDays(100), Notes = "مضخة تغيير مياه - الحوض الرئيسي" },
                new Equipment { EquipmentNumber = "EQ-005", Name = "فلتر ميكانيكي كبير", Category = "فلاتر", Manufacturer = "Aqua Filter Systems", Model = "AFS-3000", SerialNumber = "AFS-2023-050", PurchaseDate = DateTime.Now.AddYears(-1), PurchasePrice = 35000.0m, Status = "صيانة", Location = "وحدة الفلترة", MaintenanceIntervalDays = 60, LastMaintenanceDate = DateTime.Now.AddDays(-5), NextMaintenanceDate = DateTime.Now.AddDays(1), Notes = "تحت الصيانة - تنظيف شامل" },
                new Equipment { EquipmentNumber = "EQ-006", Name = "جهاز قياس الأوكسجين المذاب", Category = "أجهزة قياس", Manufacturer = "Hanna Instruments", Model = "HI-9146", SerialNumber = "HI-2024-300", PurchaseDate = DateTime.Now.AddMonths(-3), PurchasePrice = 4500.0m, Status = "تشغيل", Location = "المعمل", MaintenanceIntervalDays = 365, LastMaintenanceDate = DateTime.Now.AddMonths(-3), NextMaintenanceDate = DateTime.Now.AddMonths(9), Notes = "جهاز رقمي دقيق - معايرة شهرية" },
                new Equipment { EquipmentNumber = "EQ-007", Name = "غذاء أوتوماتيكي 100 لتر", Category = "معدات تغذية", Manufacturer = "Fish Mate", Model = "FM-P7000", SerialNumber = "FM-2024-010", PurchaseDate = DateTime.Now.AddMonths(-4), PurchasePrice = 8500.0m, Status = "تشغيل", Location = "حوض 1", MaintenanceIntervalDays = 90, LastMaintenanceDate = DateTime.Now.AddDays(-15), NextMaintenanceDate = DateTime.Now.AddDays(75), Notes = "موزع علف آلي - برمجة 3 مرات يومياً" },
                new Equipment { EquipmentNumber = "EQ-008", Name = "شبكة صيد كبيرة 50 متر", Category = "أدوات", Manufacturer = "Egyptian Nets Co.", Model = "EN-50-PRO", SerialNumber = "EN-2023-200", PurchaseDate = DateTime.Now.AddYears(-1), PurchasePrice = 3000.0m, Status = "تشغيل", Location = "مخزن الأدوات", MaintenanceIntervalDays = 180, LastMaintenanceDate = DateTime.Now.AddDays(-60), NextMaintenanceDate = DateTime.Now.AddDays(120), Notes = "شبكة الحصاد الرئيسية" }
            };
            context.Equipment.AddRange(equipment);
            context.SaveChanges();

            // 14. Seed Maintenance Schedules (جداول الصيانة)
            var schedules = new[]
            {
                new MaintenanceSchedule { EquipmentId = equipment[0].Id, MaintenanceType = "صيانة دورية", Frequency = "شهري", FrequencyDays = 90, NextMaintenanceDate = DateTime.Now.AddDays(60), Priority = 3, EstimatedCost = 500.0m, AssignedTo = "خالد عبدالله", Status = "نشط", IsActive = true, Notes = "تشحيم + فحص شامل + استبدال الفلاتر - الأولوية: عالية" },
                new MaintenanceSchedule { EquipmentId = equipment[1].Id, MaintenanceType = "صيانة دورية", Frequency = "شهري", FrequencyDays = 90, NextMaintenanceDate = DateTime.Now.AddDays(55), Priority = 3, EstimatedCost = 500.0m, AssignedTo = "خالد عبدالله", Status = "نشط", IsActive = true, Notes = "تشحيم + فحص + استبدال فلاتر - الأولوية: عالية" },
                new MaintenanceSchedule { EquipmentId = equipment[2].Id, MaintenanceType = "صيانة دورية", Frequency = "نصف سنوي", FrequencyDays = 180, NextMaintenanceDate = DateTime.Now.AddDays(90), Priority = 4, EstimatedCost = 2000.0m, AssignedTo = "خالد عبدالله", Status = "نشط", IsActive = true, Notes = "تغيير زيت + فلاتر + فحص شامل - الأولوية: حرجة" },
                new MaintenanceSchedule { EquipmentId = equipment[4].Id, MaintenanceType = "تنظيف وصيانة", Frequency = "شهري", FrequencyDays = 60, NextMaintenanceDate = DateTime.Now.AddDays(1), Priority = 2, EstimatedCost = 300.0m, AssignedTo = "خالد عبدالله", Status = "نشط", IsActive = true, Notes = "تنظيف شامل + استبدال وسائط الفلترة - الأولوية: متوسطة" },
                new MaintenanceSchedule { EquipmentId = equipment[6].Id, MaintenanceType = "صيانة وقائية", Frequency = "شهري", FrequencyDays = 90, NextMaintenanceDate = DateTime.Now.AddDays(75), Priority = 2, EstimatedCost = 200.0m, AssignedTo = "خالد عبدالله", Status = "نشط", IsActive = true, Notes = "تنظيف + فحص المحرك + معايرة الموقت - الأولوية: متوسطة" }
            };
            context.MaintenanceSchedules.AddRange(schedules);
            context.SaveChanges();

            // 15. Seed Maintenance Records (سجلات الصيانة)
            var maintenanceRecords = new[]
            {
                new MaintenanceRecord { RecordNumber = "MR-2025-001", EquipmentId = equipment[0].Id, MaintenanceDate = DateTime.Now.AddDays(-30), MaintenanceType = "صيانة دورية", ProblemDescription = "صيانة دورية مجدولة", WorkPerformed = "تشحيم جميع الأجزاء المتحركة + فحص المحرك + استبدال الفلتر", PartsReplaced = "فلتر هواء جديد", PartsCost = 180.0m, LaborCost = 320.0m, TotalCost = 500.0m, PerformedBy = "خالد عبدالله", Status = "مكتملة", Notes = "الصيانة تمت بنجاح - المضخة تعمل بكفاءة 100%" },
                new MaintenanceRecord { RecordNumber = "MR-2025-002", EquipmentId = equipment[2].Id, MaintenanceDate = DateTime.Now.AddDays(-90), MaintenanceType = "صيانة دورية", ProblemDescription = "صيانة دورية نصف سنوية", WorkPerformed = "تغيير زيت المحرك + تغيير فلتر الزيت + فلتر الوقود + فحص كامل + اختبار تشغيل", PartsReplaced = "20 لتر زيت محرك + فلتر زيت + فلتر وقود", PartsCost = 1200.0m, LaborCost = 800.0m, TotalCost = 2000.0m, PerformedBy = "خالد عبدالله + فني خارجي", Status = "مكتملة", Notes = "صيانة شاملة - المولد يعمل بكفاءة ممتازة" },
                new MaintenanceRecord { RecordNumber = "MR-2025-003", EquipmentId = equipment[4].Id, MaintenanceDate = DateTime.Now.AddDays(-5), MaintenanceType = "تنظيف وصيانة", ProblemDescription = "انخفاض كفاءة الفلترة", WorkPerformed = "تنظيف شامل للفلتر + استبدال وسائط الفلترة القديمة + غسيل عكسي", PartsReplaced = "50 كجم وسائط فلترة (رمل + حصى)", PartsCost = 250.0m, LaborCost = 150.0m, TotalCost = 400.0m, PerformedBy = "خالد عبدالله + محمد حسن", Status = "مكتملة", Notes = "تحسنت الكفاءة بشكل ملحوظ - المياه أصبحت أكثر نقاء" },
                new MaintenanceRecord { RecordNumber = "MR-2025-004", EquipmentId = equipment[1].Id, MaintenanceDate = DateTime.Now.AddDays(-35), MaintenanceType = "صيانة دورية", ProblemDescription = "صيانة دورية", WorkPerformed = "تشحيم + فحص + استبدال فلتر", PartsReplaced = "فلتر هواء", PartsCost = 180.0m, LaborCost = 300.0m, TotalCost = 480.0m, PerformedBy = "خالد عبدالله", Status = "مكتملة", Notes = "صيانة ناجحة" },
                new MaintenanceRecord { RecordNumber = "MR-2025-005", EquipmentId = equipment[3].Id, MaintenanceDate = DateTime.Now.AddDays(-20), MaintenanceType = "صيانة وقائية", ProblemDescription = "صيانة وقائية", WorkPerformed = "فحص المحرك + تنظيف المكره + تشحيم", PartsReplaced = "لا يوجد", PartsCost = 0.0m, LaborCost = 200.0m, TotalCost = 200.0m, PerformedBy = "خالد عبدالله", Status = "مكتملة", Notes = "المضخة تعمل بشكل جيد" }
            };
            context.MaintenanceRecords.AddRange(maintenanceRecords);
            context.SaveChanges();

            // 16. Seed Spare Parts (قطع الغيار)
            var spareParts = new[]
            {
                new SparePart { PartNumber = "SP-001", Name = "فلتر هواء للمضخات", Category = "فلاتر", EquipmentId = equipment[0].Id, QuantityInStock = 12, MinimumQuantity = 5, UnitPrice = 180.0m, Supplier = "AquaTech Egypt", LastPurchaseDate = DateTime.Now.AddMonths(-2), Status = "متوفر", IsCritical = true, Notes = "قطعة حرجة - يجب توفر مخزون دائم" },
                new SparePart { PartNumber = "SP-002", Name = "حلقات مطاطية (O-Rings) مقاسات متنوعة", Category = "مستهلكات", QuantityInStock = 50, MinimumQuantity = 20, UnitPrice = 15.0m, Supplier = "القاهرة للمطاط الصناعي", LastPurchaseDate = DateTime.Now.AddMonths(-1), Status = "متوفر", IsCritical = false, Notes = "مقاسات من 20 مم إلى 100 مم" },
                new SparePart { PartNumber = "SP-003", Name = "فلتر زيت للمولد", Category = "فلاتر", EquipmentId = equipment[2].Id, QuantityInStock = 6, MinimumQuantity = 3, UnitPrice = 250.0m, Supplier = "Perkins Egypt", LastPurchaseDate = DateTime.Now.AddMonths(-3), Status = "متوفر", IsCritical = true, Notes = "أصلي Perkins - قطعة حرجة" },
                new SparePart { PartNumber = "SP-004", Name = "فلتر وقود للمولد", Category = "فلاتر", EquipmentId = equipment[2].Id, QuantityInStock = 8, MinimumQuantity = 4, UnitPrice = 200.0m, Supplier = "Perkins Egypt", LastPurchaseDate = DateTime.Now.AddMonths(-3), Status = "متوفر", IsCritical = true, Notes = "أصلي Perkins" },
                new SparePart { PartNumber = "SP-005", Name = "زيت محرك ديزل SAE 15W-40", Category = "زيوت وشحوم", EquipmentId = equipment[2].Id, QuantityInStock = 60, MinimumQuantity = 40, UnitPrice = 60.0m, Supplier = "Total Egypt", LastPurchaseDate = DateTime.Now.AddMonths(-4), Status = "متوفر", IsCritical = true, Notes = "عبوات 1 لتر - Total Rubia TIR 7400" },
                new SparePart { PartNumber = "SP-006", Name = "مكره مضخة مياه (Impeller)", Category = "قطع غيار", EquipmentId = equipment[3].Id, QuantityInStock = 2, MinimumQuantity = 2, UnitPrice = 850.0m, Supplier = "Pedrollo Egypt", LastPurchaseDate = DateTime.Now.AddMonths(-6), Status = "حد أدنى", IsCritical = true, Notes = "قطعة حرجة - يفضل طلب المزيد" },
                new SparePart { PartNumber = "SP-007", Name = "ميكانيكال سيل (Mechanical Seal)", Category = "قطع غيار", EquipmentId = equipment[3].Id, QuantityInStock = 3, MinimumQuantity = 2, UnitPrice = 450.0m, Supplier = "Pedrollo Egypt", LastPurchaseDate = DateTime.Now.AddMonths(-6), Status = "متوفر", IsCritical = true, Notes = "قطعة حرجة - ضرورية للمضخات" },
                new SparePart { PartNumber = "SP-008", Name = "وسائط فلترة (رمل + حصى)", Category = "مستهلكات", EquipmentId = equipment[4].Id, QuantityInStock = 200, MinimumQuantity = 100, UnitPrice = 5.0m, Supplier = "شركة مواد البناء - المحلة", LastPurchaseDate = DateTime.Now.AddMonths(-1), Status = "متوفر", IsCritical = false, Notes = "كيس 1 كجم - يستخدم للفلاتر الميكانيكية" },
                new SparePart { PartNumber = "SP-009", Name = "بطارية جافة 12V 200Ah", Category = "كهرباء", EquipmentId = equipment[2].Id, QuantityInStock = 1, MinimumQuantity = 1, UnitPrice = 3500.0m, Supplier = "Chloride Egypt", LastPurchaseDate = DateTime.Now.AddMonths(-12), Status = "حد أدنى", IsCritical = true, Notes = "بطارية المولد - يجب استبدالها كل سنتين" },
                new SparePart { PartNumber = "SP-010", Name = "موقت رقمي (Digital Timer)", Category = "كهرباء", EquipmentId = equipment[6].Id, QuantityInStock = 2, MinimumQuantity = 1, UnitPrice = 320.0m, Supplier = "Siemens Egypt", LastPurchaseDate = DateTime.Now.AddMonths(-5), Status = "متوفر", IsCritical = false, Notes = "احتياطي لموزع العلف الآلي" }
            };
            context.SpareParts.AddRange(spareParts);
            context.SaveChanges();

            // ============= OUTPUT SUMMARY =============
            Console.WriteLine("✅ تم إضافة البيانات التجريبية بنجاح!");
            Console.WriteLine($"   📊 الإحصائيات:");
            Console.WriteLine($"   - {ponds.Length} أحواض");
            Console.WriteLine($"   - {cycles.Length} دورات إنتاج");
            Console.WriteLine($"   - {cyclePonds.Length} ربط أحواض-دورات");
            Console.WriteLine($"   - {waterRecords.Length} سجلات جودة مياه");
            Console.WriteLine($"   - {feedingRecords.Length} سجلات تغذية");
            Console.WriteLine($"   - {batchRecords.Length} سجلات دفعات");
            Console.WriteLine($"   - {mortalityRecords.Length} سجلات نفوق");
            Console.WriteLine($"   - {healthRecords.Length} سجلات صحة الأسماك");
            Console.WriteLine($"   - {treatmentRecords.Length} سجلات علاج");
            Console.WriteLine($"   - {environmentalRecords.Length} سجلات بيئية");
            // Console.WriteLine($"   - {staffRecords.Length} موظفين"); // Skipped - see note above
            // Console.WriteLine($"   - {certificationRecords.Length} شهادات"); // Skipped - see note above
            Console.WriteLine($"   - {equipment.Length} معدات");
            Console.WriteLine($"   - {schedules.Length} جداول صيانة");
            Console.WriteLine($"   - {maintenanceRecords.Length} سجلات صيانة");
            Console.WriteLine($"   - {spareParts.Length} قطع غيار");

            // 16. Seed Customers (العملاء) - 5 customers
            var customers = new[]
            {
                new Customer { Name = "شركة الخليج للتجارة", Type = CustomerType.Wholesale, Phone = "0501234567", Email = "gulf@example.com", Address = "الرياض - حي السلي", TaxNumber = "300123456700003", CommercialRegistration = "1010123456", CreditLimit = 50000, CurrentBalance = 0, PaymentTermDays = 30, Status = CustomerStatus.Active, CreatedAt = DateTime.Now.AddMonths(-6), CreatedBy = "Admin", Notes = "عميل رئيسي - جملة" },
                new Customer { Name = "مطاعم النخيل", Type = CustomerType.Restaurant, Phone = "0509876543", Email = "alnakheel@example.com", Address = "جدة - حي الشاطئ", TaxNumber = "300234567800003", CommercialRegistration = "4030234567", CreditLimit = 20000, CurrentBalance = 0, PaymentTermDays = 15, Status = CustomerStatus.Active, CreatedAt = DateTime.Now.AddMonths(-4), CreatedBy = "Admin", Notes = "مطاعم متعددة الفروع" },
                new Customer { Name = "سوبر ماركت الواحة", Type = CustomerType.Supermarket, Phone = "0503334444", Email = "oasis@example.com", Address = "المدينة المنورة - شارع العيون", TaxNumber = "300345678900003", CommercialRegistration = "4040345678", CreditLimit = 15000, CurrentBalance = 0, PaymentTermDays = 20, Status = CustomerStatus.Active, CreatedAt = DateTime.Now.AddMonths(-2), CreatedBy = "Admin", Notes = "سوبر ماركت متوسط الحجم" },
                new Customer { Name = "فندق البحر الأحمر", Type = CustomerType.Hotel, Phone = "0505556666", Email = "redseahotel@example.com", Address = "الأحساء - المبرز", TaxNumber = "300456789000003", CommercialRegistration = "2020456789", CreditLimit = 30000, CurrentBalance = 0, PaymentTermDays = 30, Status = CustomerStatus.Active, CreatedAt = DateTime.Now.AddMonths(-8), CreatedBy = "Admin", Notes = "فندق 5 نجوم" },
                new Customer { Name = "شركة التصدير الدولية", Type = CustomerType.Exporter, Phone = "0507778888", Email = "export@example.com", Address = "الدمام - الميناء", TaxNumber = "300567890100003", CommercialRegistration = "2020567890", CreditLimit = 100000, CurrentBalance = 0, PaymentTermDays = 45, Status = CustomerStatus.Active, CreatedAt = DateTime.Now.AddMonths(-10), CreatedBy = "Admin", Notes = "تصدير للخارج" }
            };
            context.Customers.AddRange(customers);
            context.SaveChanges();
            Console.WriteLine($"   - {customers.Length} عملاء");

            // 17. Seed Suppliers (الموردين) - 3 suppliers
            var suppliers = new[]
            {
                new Supplier { Name = "شركة الأعلاف المتقدمة", Type = SupplierType.Feed, Phone = "0508889999", Email = "feed@example.com", Address = "الخرج - المنطقة الصناعية", TaxNumber = "301123456700003", ContactPerson = "أحمد محمد", PaymentTermDays = 30, CreditLimit = 80000, CurrentBalance = 0, Status = SupplierStatus.Active, CreatedAt = DateTime.Now.AddYears(-1), LastPurchaseDate = DateTime.Now.AddDays(-5), Notes = "مورد رئيسي - أعلاف عالية الجودة" },
                new Supplier { Name = "مؤسسة المعدات الزراعية", Type = SupplierType.Equipment, Phone = "0509990000", Email = "equipment@example.com", Address = "الرياض - طريق الخرج", TaxNumber = "301234567800003", ContactPerson = "خالد عبدالله", PaymentTermDays = 45, CreditLimit = 150000, CurrentBalance = 0, Status = SupplierStatus.Active, CreatedAt = DateTime.Now.AddYears(-2), LastPurchaseDate = DateTime.Now.AddDays(-60), Notes = "معدات ومضخات وفلاتر" },
                new Supplier { Name = "صيدلية الثروة البحرية", Type = SupplierType.Medication, Phone = "0501112222", Email = "pharmacy@example.com", Address = "الدمام - حي الفرسان", TaxNumber = "301345678900003", ContactPerson = "محمد حسن", PaymentTermDays = 15, CreditLimit = 30000, CurrentBalance = 0, Status = SupplierStatus.Active, CreatedAt = DateTime.Now.AddMonths(-6), LastPurchaseDate = DateTime.Now.AddDays(-15), Notes = "أدوية ومستلزمات بيطرية" }
            };
            context.Suppliers.AddRange(suppliers);
            context.SaveChanges();
            Console.WriteLine($"   - {suppliers.Length} موردين");
            
            int total = ponds.Length + cycles.Length + cyclePonds.Length + waterRecords.Length + feedingRecords.Length + 
                       batchRecords.Length + mortalityRecords.Length + healthRecords.Length + treatmentRecords.Length + 
                       environmentalRecords.Length + // staffRecords and certificationRecords removed
                       equipment.Length + schedules.Length + maintenanceRecords.Length + spareParts.Length +
                       customers.Length + suppliers.Length;
            
            LoggingService.LogInfo("   📈 إجمالي: {Total} سجل في قاعدة البيانات", total);
            LoggingService.LogInfo("   🎉 النظام جاهز للاستخدام مع بيانات تجريبية شاملة!");
        }

        /// <summary>
        /// إنشاء مستخدمين افتراضيين للنظام
        /// </summary>
        private static void SeedDefaultUsers(FishFarmContext context)
        {
            // التحقق من وجود مستخدمين
            if (context.Users.Any())
            {
                LoggingService.LogInfo("✅ المستخدمون موجودون بالفعل");
                return;
            }

            LoggingService.LogInfo("👤 إنشاء المستخدمين الافتراضيين...");

            var users = new[]
            {
                new User
                {
                    Username = "admin",
                    PasswordHash = HashPassword("admin123"),
                    FullName = "مدير النظام",
                    Email = "admin@aquafarm.com",
                    Role = UserRole.Admin,
                    IsActive = true,
                    CreatedAt = DateTime.Now,
                    CreatedBy = "System"
                },
                new User
                {
                    Username = "manager",
                    PasswordHash = HashPassword("manager123"),
                    FullName = "مدير المزرعة",
                    Email = "manager@aquafarm.com",
                    Role = UserRole.Manager,
                    IsActive = true,
                    CreatedAt = DateTime.Now,
                    CreatedBy = "System"
                },
                new User
                {
                    Username = "accountant",
                    PasswordHash = HashPassword("acc123"),
                    FullName = "محاسب رئيسي",
                    Email = "accountant@aquafarm.com",
                    Role = UserRole.Accountant,
                    IsActive = true,
                    CreatedAt = DateTime.Now,
                    CreatedBy = "System"
                },
                new User
                {
                    Username = "production",
                    PasswordHash = HashPassword("prod123"),
                    FullName = "مشرف إنتاج",
                    Email = "production@aquafarm.com",
                    Role = UserRole.ProductionStaff,
                    IsActive = true,
                    CreatedAt = DateTime.Now,
                    CreatedBy = "System"
                },
                new User
                {
                    Username = "sales",
                    PasswordHash = HashPassword("sales123"),
                    FullName = "موظف مبيعات",
                    Email = "sales@aquafarm.com",
                    Role = UserRole.SalesStaff,
                    IsActive = true,
                    CreatedAt = DateTime.Now,
                    CreatedBy = "System"
                }
            };

            context.Users.AddRange(users);
            context.SaveChanges();

            LoggingService.LogInfo("✅ تم إنشاء المستخدمين الافتراضيين لوضع العرض. لا يتم تسجيل كلمات المرور.");
        }

        /// <summary>
        /// تشفير كلمة المرور باستخدام SHA256
        /// </summary>
        private static string HashPassword(string password)
        {
            using (var sha256 = SHA256.Create())
            {
                byte[] bytes = sha256.ComputeHash(Encoding.UTF8.GetBytes(password));
                StringBuilder builder = new StringBuilder();
                foreach (byte b in bytes)
                {
                    builder.Append(b.ToString("x2"));
                }
                return builder.ToString();
            }
        }
    }
}
