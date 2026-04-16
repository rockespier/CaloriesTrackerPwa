using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using CalorieTracker.Domain.Entities;

namespace CalorieTracker.Application.Interfaces
{
    public interface IActivityLogRepository
    {
        Task AddAsync(ActivityLog log);
        Task<IEnumerable<ActivityLog>> GetByUserAndDateAsync(Guid userId, DateTime date);
        Task<int> GetTotalBurnedByUserAndDateAsync(Guid userId, DateTime date);
    }
}
