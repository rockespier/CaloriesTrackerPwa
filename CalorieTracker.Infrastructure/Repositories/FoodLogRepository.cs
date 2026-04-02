using CalorieTracker.Application.Interfaces;
using CalorieTracker.Domain.Entities;
using CalorieTracker.Infrastructure.Data;


namespace CalorieTracker.Infrastructure.Repositories
{
    public class FoodLogRepository : IFoodLogRepository
    {
        private readonly CalorieTrackerDbContext _context;

        public FoodLogRepository(CalorieTrackerDbContext context)
        {
            _context = context;
        }
        public async Task AddAsync(FoodLog log)
        {
            await _context.FoodLogs.AddAsync(log);
            await _context.SaveChangesAsync();
        }
    }
}
