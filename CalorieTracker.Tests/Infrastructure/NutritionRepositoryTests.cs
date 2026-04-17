using CalorieTracker.Domain.Entities;
using CalorieTracker.Infrastructure.Data;
using CalorieTracker.Infrastructure.Repositories;
using Microsoft.EntityFrameworkCore;

namespace CalorieTracker.Tests.Infrastructure;

public class NutritionRepositoryTests
{
    private static CalorieTrackerDbContext CreateContext()
    {
        var options = new DbContextOptionsBuilder<CalorieTrackerDbContext>()
            .UseInMemoryDatabase(databaseName: $"NutritionRepoTestDb_{Guid.NewGuid()}")
            .Options;
        return new CalorieTrackerDbContext(options);
    }

    [Fact]
    public async Task AddAsync_HappyPath_PersistsFoodLog()
    {
        await using var context = CreateContext();
        var repo   = new NutritionRepository(context);
        var userId = Guid.NewGuid();
        var log    = new FoodLog(userId, "dos huevos", 140);

        await repo.AddAsync(log);

        var saved = await context.FoodLogs.FirstOrDefaultAsync(f => f.UserId == userId);
        Assert.NotNull(saved);
        Assert.Equal("dos huevos", saved.OriginalText);
        Assert.Equal(140, saved.EstimatedCalories);
    }

    [Fact]
    public async Task GetTotalCaloriesForDateAsync_HappyPath_ReturnsSumForDate()
    {
        await using var context = CreateContext();
        var userId  = Guid.NewGuid();
        var today   = DateTime.UtcNow.Date;

        context.FoodLogs.AddRange(
            new FoodLog(userId, "desayuno",  350),
            new FoodLog(userId, "almuerzo",  600),
            new FoodLog(userId, "cena",      500));
        await context.SaveChangesAsync();

        var repo  = new NutritionRepository(context);
        var total = await repo.GetTotalCaloriesForDateAsync(userId, today);

        Assert.Equal(1450, total);
    }

    [Fact]
    public async Task GetTotalCaloriesForDateAsync_SadPath_DifferentUser_ReturnsZero()
    {
        await using var context = CreateContext();
        var userId1 = Guid.NewGuid();
        var userId2 = Guid.NewGuid();
        var today   = DateTime.UtcNow.Date;

        context.FoodLogs.Add(new FoodLog(userId1, "desayuno", 350));
        await context.SaveChangesAsync();

        var repo  = new NutritionRepository(context);
        var total = await repo.GetTotalCaloriesForDateAsync(userId2, today);

        Assert.Equal(0, total);
    }

    [Fact]
    public async Task GetLogsByDateAsync_HappyPath_ReturnsOnlyLogsForDate()
    {
        await using var context = CreateContext();
        var userId    = Guid.NewGuid();
        var today     = DateTime.UtcNow.Date;

        context.FoodLogs.AddRange(
            new FoodLog(userId, "desayuno hoy", 300),
            new FoodLog(userId, "almuerzo hoy", 500));
        await context.SaveChangesAsync();

        var repo = new NutritionRepository(context);
        var logs = (await repo.GetLogsByDateAsync(userId, today)).ToList();

        Assert.Equal(2, logs.Count);
    }
}
