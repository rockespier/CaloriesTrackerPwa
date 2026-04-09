using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using CalorieTracker.Application.Interfaces;
using CalorieTracker.Application.UseCases;
using CalorieTracker.Domain.Entities;
using Moq;
using Xunit;

namespace CalorieTracker.Tests.Application
{
    public class GetWeeklyStatusUseCaseTests
    {
        private readonly Mock<INutritionRepository> _nutritionRepoMock;
        private readonly Mock<IUserRepository> _userRepoMock;
        private readonly GetWeeklyStatusUseCase _useCase;

        public GetWeeklyStatusUseCaseTests()
        {
            _nutritionRepoMock = new Mock<INutritionRepository>();
            _userRepoMock = new Mock<IUserRepository>();
            _useCase = new GetWeeklyStatusUseCase(_nutritionRepoMock.Object, _userRepoMock.Object);
        }

        [Fact]
        public async Task ExecuteAsync_HappyPath_UserInDeficit_ReturnsEnMeta()
        {
            // Arrange
            var userId = Guid.NewGuid();
            // dailyTarget ≈ 1885 kcal (Sedentary, M, 90→80 kg, wants to lose)
            var user = new User("test@test.com", "hash", "Juan", 180, 90, 80, 30, 'M', ActivityLevel.Sedentary);

            var dailyHistory = new List<object>
            {
                new { Date = DateTime.Today.AddDays(-1), TotalCalories = 1800 },
                new { Date = DateTime.Today.AddDays(-2), TotalCalories = 1800 }
            };

            _userRepoMock.Setup(r => r.GetByIdAsync(userId)).ReturnsAsync(user);
            _nutritionRepoMock.Setup(r => r.GetDailyHistoryAsync(userId, 7)).ReturnsAsync(dailyHistory);

            // Act
            dynamic result = await _useCase.ExecuteAsync(userId);

            // Assert
            Assert.Equal("En Meta", (string)result.ComplianceStatus);
            Assert.Equal(2, (int)result.DaysActive);
            Assert.Equal(3600.0, (double)result.WeeklyConsumed);
        }

        [Fact]
        public async Task ExecuteAsync_HappyPath_UserOverTarget_ReturnsExceso()
        {
            // Arrange
            var userId = Guid.NewGuid();
            var user = new User("test@test.com", "hash", "Juan", 180, 90, 80, 30, 'M', ActivityLevel.Sedentary);

            // Consume more than the daily target (~1885 kcal) on both days
            var dailyHistory = new List<object>
            {
                new { Date = DateTime.Today.AddDays(-1), TotalCalories = 2500 },
                new { Date = DateTime.Today.AddDays(-2), TotalCalories = 2500 }
            };

            _userRepoMock.Setup(r => r.GetByIdAsync(userId)).ReturnsAsync(user);
            _nutritionRepoMock.Setup(r => r.GetDailyHistoryAsync(userId, 7)).ReturnsAsync(dailyHistory);

            // Act
            dynamic result = await _useCase.ExecuteAsync(userId);

            // Assert
            Assert.Equal("Exceso", (string)result.ComplianceStatus);
            Assert.Equal(5000.0, (double)result.WeeklyConsumed);
        }

        [Fact]
        public async Task ExecuteAsync_HappyPath_NoLogsForWeek_ReturnsEnMetaWithZeroConsumed()
        {
            // Arrange
            var userId = Guid.NewGuid();
            var user = new User("ana@test.com", "hash", "Ana", 165, 60, 55, 25, 'F', ActivityLevel.LightlyActive);

            _userRepoMock.Setup(r => r.GetByIdAsync(userId)).ReturnsAsync(user);
            _nutritionRepoMock.Setup(r => r.GetDailyHistoryAsync(userId, 7)).ReturnsAsync(new List<object>());

            // Act
            dynamic result = await _useCase.ExecuteAsync(userId);

            // Assert
            Assert.Equal("En Meta", (string)result.ComplianceStatus);
            Assert.Equal(0, (int)result.DaysActive);
            Assert.Equal(0.0, (double)result.WeeklyConsumed);
        }

        [Fact]
        public async Task ExecuteAsync_SadPath_UserNotFound_ThrowsException()
        {
            // Arrange
            var userId = Guid.NewGuid();
            _userRepoMock.Setup(r => r.GetByIdAsync(userId)).ReturnsAsync((User?)null);
            _nutritionRepoMock.Setup(r => r.GetDailyHistoryAsync(userId, 7)).ReturnsAsync(new List<object>());

            // Act & Assert
            // GetWeeklyStatusUseCase calls user.CalculateDailyCaloricTarget() which throws when user is null
            await Assert.ThrowsAnyAsync<Exception>(() => _useCase.ExecuteAsync(userId));
        }

        [Fact]
        public async Task ExecuteAsync_HappyPath_ReturnsCorrectWeeklyTarget()
        {
            // Arrange
            var userId = Guid.NewGuid();
            var user = new User("test@test.com", "hash", "Juan", 180, 90, 80, 30, 'M', ActivityLevel.Sedentary);
            var dailyTarget = user.CalculateDailyCaloricTarget();

            _userRepoMock.Setup(r => r.GetByIdAsync(userId)).ReturnsAsync(user);
            _nutritionRepoMock.Setup(r => r.GetDailyHistoryAsync(userId, 7)).ReturnsAsync(new List<object>());

            // Act
            dynamic result = await _useCase.ExecuteAsync(userId);

            // Assert
            Assert.Equal(dailyTarget * 7, (double)result.WeeklyTarget);
            Assert.Equal(dailyTarget, (double)result.DailyTarget);
        }
    }
}
