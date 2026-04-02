using CalorieTracker.Domain.Entities;

namespace CalorieTracker.Application.Interfaces
{
    public interface IJwtTokenGenerator
    {
        string GenerateToken(User user);
    }
}