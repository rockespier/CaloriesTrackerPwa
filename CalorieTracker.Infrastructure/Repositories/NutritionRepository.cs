using CalorieTracker.Application.Interfaces;
using CalorieTracker.Domain.Entities;
using CalorieTracker.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CalorieTracker.Infrastructure.Repositories
{
    public class NutritionRepository: INutritionRepository
    {
        private readonly CalorieTrackerDbContext _context;

        public NutritionRepository(CalorieTrackerDbContext context)
        {
            _context = context;
        }

        public async Task<int> GetTotalCaloriesForDateAsync(Guid userId, DateTime date)
        {
            var start = date.Date;
            var end = date.Date.AddDays(1);
            return await _context.FoodLogs
                .Where(f => f.UserId == userId && f.LoggedAt >= start && f.LoggedAt < end)
                .SumAsync(f => f.EstimatedCalories);
        }

        public async Task<IEnumerable<object>> GetDailyHistoryAsync(Guid userId, int daysBack)
        {
            var startDate = DateTime.UtcNow.Date.AddDays(-daysBack);

            return await _context.FoodLogs
                .Where(f => f.UserId == userId && f.LoggedAt >= startDate)
                .GroupBy(f => f.LoggedAt.Date)
                .Select(g => new {
                    Date = g.Key,
                    TotalCalories = g.Sum(f => f.EstimatedCalories)
                })
                .OrderByDescending(x => x.Date)
                .ToListAsync();
        }
        public async Task<IEnumerable<FoodLog>> GetLogsByDateAsync(Guid userId, DateTime date)
        {
            var start = date.Date;
            var end = date.Date.AddDays(1);
            return await _context.FoodLogs
                .Where(f => f.UserId == userId && f.LoggedAt >= start && f.LoggedAt < end)
                .OrderByDescending(f => f.LoggedAt)
                .ToListAsync();
        }
        public async Task<IEnumerable<object>> GetStatsInRangeAsync(Guid userId, DateTime startDate, DateTime endDate)
        {
            var start = startDate.Date;
            var end = endDate.Date.AddDays(1);
            return await _context.FoodLogs
                .Where(f => f.UserId == userId && f.LoggedAt >= start && f.LoggedAt < end)
                .GroupBy(f => f.LoggedAt.Date)
                .Select(g => new
                {
                    Date = g.Key,
                    TotalCalories = g.Sum(f => f.EstimatedCalories),
                    MealCount = g.Count()
                })
                .OrderBy(x => x.Date)
                .ToListAsync();
        }
        public async Task AddAsync(FoodLog log)
        {
            await _context.FoodLogs.AddAsync(log);
            await _context.SaveChangesAsync();
        }

        

    }
}
