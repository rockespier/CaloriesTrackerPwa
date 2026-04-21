using CalorieTracker.Application.DTOs;
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
            return await _context.FoodLogs
                .Where(f => f.UserId == userId && f.LoggedAt.Date == date.Date)
                .SumAsync(f => f.EstimatedCalories);
        }

        public async Task<IEnumerable<DailyCaloriesSummaryDto>> GetDailyHistoryAsync(Guid userId, int daysBack)
        {
            var startDate = DateTime.UtcNow.Date.AddDays(-daysBack);

            return await _context.FoodLogs
                .Where(f => f.UserId == userId && f.LoggedAt >= startDate)
                .GroupBy(f => f.LoggedAt.Date)
                .Select(g => new DailyCaloriesSummaryDto(g.Key, g.Sum(f => f.EstimatedCalories)))
                .OrderByDescending(x => x.Date)
                .ToListAsync();
        }
        public async Task<IEnumerable<FoodLog>> GetLogsByDateAsync(Guid userId, DateTime date)
        {
            // Usamos .Date para ignorar la hora y buscar todo lo de ese día.
            // OrderByDescending asegura que lo último que comió aparezca primero en la lista.
            return await _context.FoodLogs
                .Where(f => f.UserId == userId && f.LoggedAt.Date == date.Date)
                .OrderByDescending(f => f.LoggedAt)
                .ToListAsync();
        }
        public async Task<IEnumerable<NutritionStatDto>> GetStatsInRangeAsync(Guid userId, DateTime startDate, DateTime endDate)
        {
            return await _context.FoodLogs
                .Where(f => f.UserId == userId
                    && f.LoggedAt.Date >= startDate.Date
                    && f.LoggedAt.Date <= endDate.Date)
                .GroupBy(f => f.LoggedAt.Date)
                .Select(g => new NutritionStatDto(g.Key, g.Sum(f => f.EstimatedCalories), g.Count()))
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
