using System.ComponentModel.DataAnnotations;
using CalorieTracker.Domain.Entities;

namespace CalorieTracker.Application.Commands
{
    public record RegisterUserCommand(
        [property: Required][property: EmailAddress][property: StringLength(255)] string Email,
        [property: Required][property: StringLength(100, MinimumLength = 8)] string Password,
        [property: Required][property: StringLength(100, MinimumLength = 2)] string Name,
        [property: Range(50, 300)] double HeightCm,
        [property: Range(20, 500)] double CurrentWeightKg,
        [property: Range(20, 500)] double TargetWeightKg,
        [property: Range(1, 120)] int Age,
        [property: Required][property: RegularExpression("^[MFO]$", ErrorMessage = "BiologicalSex debe ser 'M', 'F' u 'O'.")] char BiologicalSex,
        ActivityLevel ActivityLevel
    );
}
