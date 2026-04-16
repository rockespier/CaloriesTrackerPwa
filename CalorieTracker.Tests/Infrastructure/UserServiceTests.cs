using CalorieTracker.Application.Commands;
using CalorieTracker.Application.Services;
using CalorieTracker.Domain.Entities;
using CalorieTracker.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace CalorieTracker.Tests.Infrastructure;

public class UserServiceTests
{
    private static CalorieTrackerDbContext CreateContext()
    {
        var options = new DbContextOptionsBuilder<CalorieTrackerDbContext>()
            .UseInMemoryDatabase(databaseName: $"UserServiceTestDb_{Guid.NewGuid()}")
            .Options;
        return new CalorieTrackerDbContext(options);
    }

    [Fact]
    public async Task UpdateProfileAsync_HappyPath_UpdatesUserDataAndCreatesHistoryEntry()
    {
        await using var context = CreateContext();
        var userId = Guid.NewGuid();
        var user   = new User("test@test.com", "hash", "Juan", 180, 90, 80, 30, 'M', ActivityLevel.Sedentary);

        // Set private Id via reflection so we can look it up later
        typeof(User).GetProperty("Id")!.SetValue(user, userId);
        context.Users.Add(user);
        await context.SaveChangesAsync();

        var service = new UserService(context);
        var command = new UpdateProfileCommand(
            weight:           85,
            height:           181,
            age:              31,
            gender:           'M',
            activityLevel:    ActivityLevel.ModeratelyActive,
            goal:             "Mantener",
            dailyCaloricTarget: 0 // recalculated by service
        );

        var result = await service.UpdateProfileAsync(userId, command);

        Assert.True(result);

        var updated = await context.Users.FindAsync(userId);
        Assert.NotNull(updated);
        Assert.Equal(85, updated!.CurrentWeightKg);
        Assert.Equal(181, updated.HeightCm);
        Assert.Equal(31, updated.Age);

        var history = await context.UserProfileHistory
            .Where(h => h.UserId == userId)
            .ToListAsync();
        Assert.Single(history);
        Assert.Equal(90, history[0].Weight); // guardó el peso ANTERIOR
    }

    [Fact]
    public async Task UpdateProfileAsync_SadPath_UserNotFound_ReturnsFalse()
    {
        await using var context = CreateContext();
        var service = new UserService(context);
        var command = new UpdateProfileCommand(
            weight:           80,
            height:           175,
            age:              30,
            gender:           'F',
            activityLevel:    ActivityLevel.Sedentary,
            goal:             "Perder",
            dailyCaloricTarget: 0
        );

        var result = await service.UpdateProfileAsync(Guid.NewGuid(), command);

        Assert.False(result);
    }
}
