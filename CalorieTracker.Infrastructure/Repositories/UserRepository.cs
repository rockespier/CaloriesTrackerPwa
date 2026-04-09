using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using CalorieTracker.Application.Interfaces;
using CalorieTracker.Domain.Entities;
using CalorieTracker.Infrastructure.Data;

namespace CalorieTracker.Infrastructure.Repositories
{
    public class UserRepository : IUserRepository
    {
        private readonly CalorieTrackerDbContext _context;

        public UserRepository(CalorieTrackerDbContext context)
        {
            _context = context;
        }

        public async Task<bool> ExistsByEmailAsync(string email)
        {
            return await _context.Users.AnyAsync(u => u.Email == email);
        }
        public async Task<User?> GetByEmailAsync(string email)
        {
            return await _context.Users.FirstOrDefaultAsync(u => u.Email == email);
        }
        public async Task<User?> GetByIdAsync(Guid id)
        {
            return await _context.Users.FirstOrDefaultAsync(u => u.Id == id);
        }
        public async Task AddAsync(User user)
        {
            await _context.Users.AddAsync(user);
            await _context.SaveChangesAsync();
        }

        public async Task UpdateAsync(User user)
        {
            _context.Users.Update(user);
            await _context.SaveChangesAsync();
        }

        public async Task AddProfileHistoryAsync(UserProfileHistory history)
        {
            await _context.UserProfileHistory.AddAsync(history);
            await _context.SaveChangesAsync();
        }
    }
}