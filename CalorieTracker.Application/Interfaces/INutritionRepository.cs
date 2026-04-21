using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using CalorieTracker.Application.DTOs;
using CalorieTracker.Domain.Entities;

namespace CalorieTracker.Application.Interfaces
{
    public interface INutritionRepository
    {
        Task AddAsync(FoodLog log);
        Task<int> GetTotalCaloriesForDateAsync(Guid userId, DateTime date);
        Task<IEnumerable<DailyCaloriesSummaryDto>> GetDailyHistoryAsync(Guid userId, int daysBack);
        Task<IEnumerable<FoodLog>> GetLogsByDateAsync(Guid userId, DateTime date);
        Task<IEnumerable<NutritionStatDto>> GetStatsInRangeAsync(Guid userId, DateTime startDate, DateTime endDate);
    }
}