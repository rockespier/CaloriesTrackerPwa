using System;
using System.Linq;
using System.Threading.Tasks;
using CalorieTracker.Domain.Entities;
using CalorieTracker.Infrastructure.Data;
using CalorieTracker.Infrastructure.Repositories;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace CalorieTracker.Tests.Infrastructure
{
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
            // Arrange
            await using var context = CreateContext();
            var repo = new NutritionRepository(context);
            var userId = Guid.NewGuid();
            var log = new FoodLog(userId, "dos huevos", 140);

            // Act
            await repo.AddAsync(log);

            // Assert
            var saved = await context.FoodLogs.FirstOrDefaultAsync(f => f.UserId == userId);
            Assert.NotNull(saved);
            Assert.Equal("dos huevos", saved.OriginalText);
            Assert.Equal(140, saved.EstimatedCalories);
        }

        [Fact]
        public async Task GetTotalCaloriesForDateAsync_HappyPath_ReturnsSumForDate()
        {
            // Arrange
            await using var context = CreateContext();
            var userId = Guid.NewGuid();
            var today = DateTime.UtcNow.Date;

            context.FoodLogs.AddRange(
                new FoodLog(userId, "desayuno", 350),
                new FoodLog(userId, "almuerzo", 600),
                new FoodLog(userId, "snack", 150));
            await context.SaveChangesAsync();

            var repo = new NutritionRepository(context);

            // Act
            var total = await repo.GetTotalCaloriesForDateAsync(userId, today);

            // Assert
            Assert.Equal(1100, total);
        }

        [Fact]
        public async Task GetTotalCaloriesForDateAsync_HappyPath_ExcludesOtherUsersLogs()
        {
            // Arrange
            await using var context = CreateContext();
            var userId = Guid.NewGuid();
            var otherUserId = Guid.NewGuid();
            var today = DateTime.UtcNow.Date;

            context.FoodLogs.AddRange(
                new FoodLog(userId, "desayuno", 350),
                new FoodLog(otherUserId, "desayuno ajeno", 1000));
            await context.SaveChangesAsync();

            var repo = new NutritionRepository(context);

            // Act
            var total = await repo.GetTotalCaloriesForDateAsync(userId, today);

            // Assert — only logs for userId should be summed
            Assert.Equal(350, total);
        }

        [Fact]
        public async Task GetLogsByDateAsync_HappyPath_ReturnsLogsForSpecifiedDate()
        {
            // Arrange
            await using var context = CreateContext();
            var userId = Guid.NewGuid();
            var today = DateTime.UtcNow.Date;

            context.FoodLogs.AddRange(
                new FoodLog(userId, "desayuno", 300),
                new FoodLog(userId, "almuerzo", 550));
            await context.SaveChangesAsync();

            var repo = new NutritionRepository(context);

            // Act
            var logs = (await repo.GetLogsByDateAsync(userId, today)).ToList();

            // Assert
            Assert.Equal(2, logs.Count);
            Assert.All(logs, l => Assert.Equal(userId, l.UserId));
        }

        [Fact]
        public async Task GetLogsByDateAsync_HappyPath_ReturnsEmptyWhenNoLogsForDate()
        {
            // Arrange
            await using var context = CreateContext();
            var userId = Guid.NewGuid();
            var repo = new NutritionRepository(context);

            // Act
            var logs = (await repo.GetLogsByDateAsync(userId, DateTime.UtcNow.Date)).ToList();

            // Assert
            Assert.Empty(logs);
        }

        [Fact]
        public async Task GetDailyHistoryAsync_HappyPath_GroupsByDayAndReturnsCorrectTotals()
        {
            // Arrange
            await using var context = CreateContext();
            var userId = Guid.NewGuid();

            context.FoodLogs.AddRange(
                new FoodLog(userId, "desayuno", 300),
                new FoodLog(userId, "almuerzo", 600));
            await context.SaveChangesAsync();

            var repo = new NutritionRepository(context);

            // Act
            var history = (await repo.GetDailyHistoryAsync(userId, 7)).ToList();

            // Assert — all added logs are from today, so one group with total 900
            Assert.Single(history);
            dynamic day = history[0];
            Assert.Equal(900, (int)day.TotalCalories);
        }

        [Fact]
        public async Task GetStatsInRangeAsync_HappyPath_ReturnsAggregatedStats()
        {
            // Arrange
            await using var context = CreateContext();
            var userId = Guid.NewGuid();
            var today = DateTime.UtcNow.Date;

            context.FoodLogs.AddRange(
                new FoodLog(userId, "desayuno", 300),
                new FoodLog(userId, "almuerzo", 600));
            await context.SaveChangesAsync();

            var repo = new NutritionRepository(context);

            // Act
            var stats = (await repo.GetStatsInRangeAsync(userId, today, today)).ToList();

            // Assert — one day with total 900 and 2 meals
            Assert.Single(stats);
            dynamic stat = stats[0];
            Assert.Equal(900, (int)stat.TotalCalories);
            Assert.Equal(2, (int)stat.MealCount);
        }

        [Fact]
        public async Task GetStatsInRangeAsync_HappyPath_ExcludesLogsOutsideRange()
        {
            // Arrange
            await using var context = CreateContext();
            var userId = Guid.NewGuid();
            var today = DateTime.UtcNow.Date;

            context.FoodLogs.AddRange(
                new FoodLog(userId, "hoy", 300),
                new FoodLog(userId, "hoy también", 200));
            await context.SaveChangesAsync();

            var repo = new NutritionRepository(context);

            // Act — query only yesterday (no logs there)
            var stats = (await repo.GetStatsInRangeAsync(userId, today.AddDays(-1), today.AddDays(-1))).ToList();

            // Assert — no logs in range
            Assert.Empty(stats);
        }
    }
}
