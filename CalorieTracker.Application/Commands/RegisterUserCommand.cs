using CalorieTracker.Domain.Entities;

namespace CalorieTracker.Application.Commands
{
    public record RegisterUserCommand(
        string Email,
        string Password,
        string Name,
        double HeightCm,
        double CurrentWeightKg,
        double TargetWeightKg,
        int Age,
        char BiologicalSex,
        ActivityLevel ActivityLevel
    );
}
