using System.Threading.Tasks;
using CalorieTracker.Domain.Entities;

namespace CalorieTracker.Application.Interfaces
{
    public interface IFoodLogRepository
    {
        Task AddAsync(FoodLog log);
    }
}