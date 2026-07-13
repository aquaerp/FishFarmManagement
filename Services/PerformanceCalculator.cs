using System;
using System.Linq;
using FishFarmManager.Models;
using FishFarmManager.Data;

namespace FishFarmManager.Services
{
    public class PerformanceCalculator
    {
        private readonly FishFarmContext _context;
        
        public PerformanceCalculator(FishFarmContext context)
        {
            _context = context;
        }
        
        // حساب معدل التحويل الغذائي FCR
        public double CalculateFCR(int cycleId)
        {
            var cycle = _context.ProductionCycles.Find(cycleId);
            if (cycle == null) return 0;
            var feedingRecords = _context.FeedingRecords
                .Where(f => f.CycleId == cycleId)
                .ToList();
            var totalFeed = feedingRecords.Sum(f => f.Quantity);
            
            var finalAvg = cycle.FinalAverageWeight ?? 0;
            var initialAvg = cycle.InitialAverageWeight;
            var weightGain = finalAvg - initialAvg;
            var finalCount = cycle.FinalFishCount ?? cycle.InitialFishCount;
            var totalWeightGain = weightGain * finalCount / 1000m; // تحويل لكجم
            
            return totalWeightGain > 0 ? (double)((decimal)totalFeed / totalWeightGain) : 0;
        }
        
        // حساب معدل النمو اليومي ADG
        public double CalculateADG(int cycleId)
        {
            var cycle = _context.ProductionCycles.Find(cycleId);
            if (cycle == null || cycle.EndDate == null) return 0;
            
            var days = (cycle.EndDate.Value - cycle.StartDate).Days;
            var weightGain = (cycle.FinalAverageWeight ?? 0) - cycle.InitialAverageWeight;
            
            return days > 0 ? (double)(weightGain / days) : 0;
        }
        
        // حساب معدل البقاء
        public double CalculateSurvivalRate(int cycleId)
        {
            var cycle = _context.ProductionCycles.Find(cycleId);
            if (cycle == null) return 0;
            var totalMortality = _context.MortalityRecords
                .Where(m => m.CycleId == cycleId)
                .Sum(m => m.DeadFishCount);
            
            if (cycle.InitialFishCount == 0) return 0;
            var survivingFish = cycle.InitialFishCount - totalMortality;
            return (survivingFish / (double)cycle.InitialFishCount) * 100;
        }
    }
}