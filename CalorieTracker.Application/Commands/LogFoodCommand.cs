using System;

namespace CalorieTracker.Application.Commands
{
    public record LogFoodCommand(Guid UserId, string FoodText);
}