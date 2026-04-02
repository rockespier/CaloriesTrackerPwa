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
            // BMR = 88.362 + (13.397 * 90) + (4.799 * 180) - (5.677 * 30) = 1987.602
            // TDEE = 1987.602 * 1.2 = 2385.1224
            // Deficit = 2385.1224 - 500 = 1885.1224
            Assert.InRange(caloricTarget, 1885.0, 1885.3);
        }
    }
}
