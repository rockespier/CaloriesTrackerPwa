using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using CalorieTracker.Application.Interfaces;
using CalorieTracker.Domain.Entities;
using CalorieTracker.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace CalorieTracker.Infrastructure.Repositories
{
    public class ActivityLogRepository : IActivityLogRepository
    {
        private readonly CalorieTrackerDbContext _context;

        public ActivityLogRepository(CalorieTrackerDbContext context)
        {
            _context = context;
        }

        public async Task AddAsync(ActivityLog log)
        {
            await _context.ActivityLogs.AddAsync(log);
            await _context.SaveChangesAsync();
        }

        public async Task<IEnumerable<ActivityLog>> GetByUserAndDateAsync(Guid userId, DateTime date)
        {
            var startOfDay = date.Date;
            var endOfDay = startOfDay.AddDays(1);

            return await _context.ActivityLogs
                .Where(a => a.UserId == userId && a.LoggedAt >= startOfDay && a.LoggedAt < endOfDay)
                .OrderByDescending(a => a.LoggedAt)
                .ToListAsync();
        }

        public async Task<int> GetTotalBurnedByUserAndDateAsync(Guid userId, DateTime date)
        {
            var startOfDay = date.Date;
            var endOfDay = startOfDay.AddDays(1);

            return await _context.ActivityLogs
                .Where(a => a.UserId == userId && a.LoggedAt >= startOfDay && a.LoggedAt < endOfDay)
                .SumAsync(a => a.CaloriesBurned);
        }
    }
}
