using CalorieTracker.Domain.Entities;
using Xunit;

namespace CalorieTracker.Tests.Domain
{
    public class UserTests
    {
        [Fact]
        public void CalculateDailyCaloricTarget_MaleSedentaryWeightLoss_ReturnsCorrectDeficit()
        {
            // Arrange
            var user = new User(
                email: "test@domain.com",
                passwordHash: "hash",
                name: "Juan",
                heightCm: 180,
                currentWeightKg: 90,
                targetWeightKg: 80, // Quiere perder peso
                age: 30,
                biologicalSex: 'M',
                activityLevel: ActivityLevel.Sedentary
            );

            // Act
            var caloricTarget = user.CalculateDailyCaloricTarget();

            // Assert
            // Mifflin-St Jeor: BMR = (10 * 90) + (6.25 * 180) - (5 * 30) + 5 = 1880
            // TDEE = 1880 * 1.2 = 2256
            // Deficit = 2256 - 500 = 1756
            Assert.InRange(caloricTarget, 1755.9, 1756.1);
        }
    }
}
