using CalorieTracker.Application.Interfaces;
using CalorieTracker.Application.UseCases;
using CalorieTracker.Domain.Entities;
using Moq;

namespace CalorieTracker.Tests.Application;

public class GetWeeklyStatusUseCaseTests
{
    private readonly Mock<INutritionRepository> _nutritionRepoMock = new();
    private readonly Mock<IUserRepository>      _userRepoMock      = new();
    private readonly GetWeeklyStatusUseCase     _useCase;

    public GetWeeklyStatusUseCaseTests()
    {
        _useCase = new GetWeeklyStatusUseCase(_nutritionRepoMock.Object, _userRepoMock.Object);
    }

    [Fact]
    public async Task ExecuteAsync_HappyPath_UserInDeficit_ReturnsEnMeta()
    {
        var userId      = Guid.NewGuid();
        var user        = new User("test@test.com", "hash", "Juan", 180, 90, 80, 30, 'M', ActivityLevel.Sedentary);
        var dailyTarget = user.DailyCaloricTarget;

        var dailyHistory = new List<object>
        {
            new { Date = DateTime.Today.AddDays(-1), TotalCalories = (int)(dailyTarget - 200) },
            new { Date = DateTime.Today.AddDays(-2), TotalCalories = (int)(dailyTarget - 200) }
        };

        _userRepoMock.Setup(r => r.GetByIdAsync(userId)).ReturnsAsync(user);
        _nutritionRepoMock.Setup(r => r.GetDailyHistoryAsync(userId, 7)).ReturnsAsync(dailyHistory);

        dynamic result = await _useCase.ExecuteAsync(userId);

        Assert.Equal("En Meta", (string)result.ComplianceStatus);
        Assert.Equal(2, (int)result.DaysActive);
    }

    [Fact]
    public async Task ExecuteAsync_HappyPath_UserOverTarget_ReturnsExceso()
    {
        var userId      = Guid.NewGuid();
        var user        = new User("test@test.com", "hash", "Juan", 180, 90, 80, 30, 'M', ActivityLevel.Sedentary);
        var dailyTarget = user.DailyCaloricTarget;

        var dailyHistory = new List<object>
        {
            new { Date = DateTime.Today.AddDays(-1), TotalCalories = dailyTarget + 300 },
            new { Date = DateTime.Today.AddDays(-2), TotalCalories = dailyTarget + 300 }
        };

        _userRepoMock.Setup(r => r.GetByIdAsync(userId)).ReturnsAsync(user);
        _nutritionRepoMock.Setup(r => r.GetDailyHistoryAsync(userId, 7)).ReturnsAsync(dailyHistory);

        dynamic result = await _useCase.ExecuteAsync(userId);

        Assert.Equal("Exceso", (string)result.ComplianceStatus);
    }

    [Fact]
    public async Task ExecuteAsync_SadPath_UserNotFound_ThrowsInvalidOperationException()
    {
        var userId = Guid.NewGuid();
        _userRepoMock.Setup(r => r.GetByIdAsync(userId)).ReturnsAsync((User?)null);
        _nutritionRepoMock.Setup(r => r.GetDailyHistoryAsync(userId, 7)).ReturnsAsync(new List<object>());

        await Assert.ThrowsAnyAsync<Exception>(() => _useCase.ExecuteAsync(userId));
    }
}
