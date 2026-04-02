using System.Threading.Tasks;
using CalorieTracker.Domain.Entities;

namespace CalorieTracker.Application.Interfaces
{
    public interface IUserRepository
    {
        Task<bool> ExistsByEmailAsync(string email);
        Task<User?> GetByEmailAsync(string email); // Nuevo método

        Task<User?> GetByIdAsync(Guid id);
        Task AddAsync(User user);
    }
}