using System;
using System.Collections.Generic;
using System.Linq;
using FishFarmManager.Data;
using FishFarmManager.Models;

namespace FishFarmManager.Services
{
    public class NotificationService
    {
        private readonly FishFarmContext _context;

        public NotificationService(FishFarmContext context)
        {
            _context = context;
        }

        // فحص جودة المياه والتنبيه للقيم الحرجة
        public List<string> CheckWaterQualityAlerts()
        {
            var alerts = new List<string>();
            var recentRecords = _context.WaterQualityRecords
                .Where(w => w.MeasurementDate >= DateTime.Now.AddDays(-1))
                .ToList();

            foreach (var record in recentRecords)
            {
                // فحص الأوكسجين المذاب
                if (record.DissolvedOxygen < 4.0m)
                {
                    alerts.Add($"تحذير: مستوى الأوكسجين المذاب منخفض في الحوض {string.Join(", ", record.Cycle?.ProductionCyclePonds.Select(pcp => pcp.Pond.Name) ?? new List<string>())} - {record.DissolvedOxygen} mg/L");
                }

                // فحص الأمونيا
                if (record.Ammonia > 0.5m)
                {
                    alerts.Add($"تحذير: مستوى الأمونيا مرتفع في الحوض {string.Join(", ", record.Cycle?.ProductionCyclePonds.Select(pcp => pcp.Pond.Name) ?? new List<string>())} - {record.Ammonia} mg/L");
                }

                // فحص درجة الحموضة
                if (record.pH < 6.5m || record.pH > 8.5m)
                {
                    alerts.Add($"تحذير: درجة الحموضة خارج النطاق المثالي في الحوض {string.Join(", ", record.Cycle?.ProductionCyclePonds.Select(pcp => pcp.Pond.Name) ?? new List<string>())} - pH {record.pH}");
                }

                // فحص درجة الحرارة
                if (record.Temperature < 20m || record.Temperature > 30m)
                {
                    alerts.Add($"تحذير: درجة الحرارة خارج النطاق المثالي في الحوض {string.Join(", ", record.Cycle?.ProductionCyclePonds.Select(pcp => pcp.Pond.Name) ?? new List<string>())} - {record.Temperature}°C");
                }
            }

            return alerts;
        }

        // فحص معدلات النفوق العالية
        public List<string> CheckMortalityAlerts()
        {
            var alerts = new List<string>();
            var recentMortality = _context.MortalityRecords
                .Where(m => m.Date >= DateTime.Now.AddDays(-7))
                .GroupBy(m => m.CycleId)
                .ToList();

            foreach (var group in recentMortality)
            {
                var totalDeaths = group.Sum(m => m.DeadFishCount);
                var cycle = _context.ProductionCycles.Find(group.Key);
                
                if (cycle != null)
                {
                    var mortalityRate = (double)totalDeaths / cycle.InitialFishCount * 100;
                    
                    if (mortalityRate > 5) // إذا تجاوز معدل النفوق 5% في الأسبوع
                    {
                        alerts.Add($"تحذير: معدل نفوق مرتفع في الحوض {string.Join(", ", cycle.ProductionCyclePonds.Select(pcp => pcp.Pond.Name))} - {mortalityRate:F1}% خلال الأسبوع الماضي");
                    }
                }
            }

            return alerts;
        }

        // فحص الدورات التي تحتاج متابعة
        public List<string> CheckCycleAlerts()
        {
            var alerts = new List<string>();
            var activeCycles = _context.ProductionCycles
                .Where(c => c.Status == CycleStatus.Active)
                .ToList();

            foreach (var cycle in activeCycles)
            {
                var daysRunning = (DateTime.Now - cycle.StartDate).Days;
                
                // تنبيه للدورات التي تجاوزت 120 يوم (قد تحتاج حصاد)
                if (daysRunning > 120)
                {
                    alerts.Add($"تنبيه: الدورة الإنتاجية في الحوض {string.Join(", ", cycle.ProductionCyclePonds.Select(pcp => pcp.Pond.Name))} تعمل منذ {daysRunning} يوم - قد تحتاج للحصاد");
                }

                // فحص آخر تسجيل تغذية
                var lastFeeding = _context.FeedingRecords
                    .Where(f => f.CycleId == cycle.Id)
                    .OrderByDescending(f => f.FeedingDate)
                    .FirstOrDefault();

                if (lastFeeding == null || (DateTime.Now - lastFeeding.FeedingDate).Days > 2)
                {
                    alerts.Add($"تنبيه: لم يتم تسجيل تغذية للحوض {string.Join(", ", cycle.ProductionCyclePonds.Select(pcp => pcp.Pond.Name))} منذ أكثر من يومين");
                }

                // فحص آخر قياس جودة مياه
                var lastWaterTest = _context.WaterQualityRecords
                    .Where(w => w.CycleId == cycle.Id)
                    .OrderByDescending(w => w.MeasurementDate)
                    .FirstOrDefault();

                if (lastWaterTest == null || (DateTime.Now - (lastWaterTest.MeasurementDate ?? DateTime.Now)).Days > 3)
                {
                    alerts.Add($"تنبيه: لم يتم فحص جودة المياه للحوض {string.Join(", ", cycle.ProductionCyclePonds.Select(pcp => pcp.Pond.Name))} منذ أكثر من 3 أيام");
                }
            }

            return alerts;
        }

        // الحصول على جميع التنبيهات
        public List<string> GetAllAlerts()
        {
            var allAlerts = new List<string>();
            allAlerts.AddRange(CheckWaterQualityAlerts());
            allAlerts.AddRange(CheckMortalityAlerts());
            allAlerts.AddRange(CheckCycleAlerts());
            return allAlerts;
        }
    }
}
