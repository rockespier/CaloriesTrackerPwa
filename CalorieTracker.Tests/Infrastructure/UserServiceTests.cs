using System;
using System.Threading.Tasks;
using CalorieTracker.Application.Commands;
using CalorieTracker.Application.Services;
using CalorieTracker.Domain.Entities;
using CalorieTracker.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace CalorieTracker.Tests.Infrastructure
{
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
            // Arrange
            await using var context = CreateContext();
            var userId = Guid.NewGuid();
            var user = new User("test@test.com", "hash", "Juan", 180, 90, 80, 30, 'M', ActivityLevel.Sedentary);

            // Use reflection to set the private Id so we can look it up later
            typeof(User).GetProperty("Id")!.SetValue(user, userId);
            context.Users.Add(user);
            await context.SaveChangesAsync();

            var command = new UpdateProfileCommand(
                Weight: 85,
                Height: 181,
                Age: 31,
                Gender: 'M',
                ActivityLevel: ActivityLevel.ModeratelyActive,
                Goal: "Mantener");

            var service = new UserService(context);

            // Act
            var result = await service.UpdateProfileAsync(userId, command);

            // Assert — operation returned true
            Assert.True(result);

            // Assert — user data was updated
            var updatedUser = await context.Users.FindAsync(userId);
            Assert.NotNull(updatedUser);
            Assert.Equal(85, updatedUser.CurrentWeightKg);
            Assert.Equal(181, updatedUser.HeightCm);
            Assert.Equal(31, updatedUser.Age);
            Assert.Equal(ActivityLevel.ModeratelyActive, updatedUser.ActivityLevel);
            Assert.Equal("Mantener", updatedUser.Goal);

            // Assert — a history entry was created with the old weight (90)
            var historyEntry = await context.UserProfileHistory
                .FirstOrDefaultAsync(h => h.UserId == userId);
            Assert.NotNull(historyEntry);
            Assert.Equal(90, historyEntry.Weight);
            Assert.Equal(180, historyEntry.Height);
            Assert.Equal(ActivityLevel.Sedentary, historyEntry.ActivityLevel);
        }

        [Fact]
        public async Task UpdateProfileAsync_HappyPath_RecalculatesDailyCaloricTarget()
        {
            // Arrange
            await using var context = CreateContext();
            var userId = Guid.NewGuid();
            var user = new User("test@test.com", "hash", "Juan", 180, 90, 80, 30, 'M', ActivityLevel.Sedentary);
            typeof(User).GetProperty("Id")!.SetValue(user, userId);
            context.Users.Add(user);
            await context.SaveChangesAsync();

            var command = new UpdateProfileCommand(
                Weight: 85,
                Height: 180,
                Age: 30,
                Gender: 'M',
                ActivityLevel: ActivityLevel.Sedentary,
                Goal: "Perder");

            var service = new UserService(context);

            // Act
            await service.UpdateProfileAsync(userId, command);

            // Assert — DailyCaloricTarget was recalculated (should be positive)
            var updatedUser = await context.Users.FindAsync(userId);
            Assert.NotNull(updatedUser);
            Assert.True(updatedUser.DailyCaloricTarget > 0);
        }

        [Fact]
        public async Task UpdateProfileAsync_SadPath_UserNotFound_ReturnsFalse()
        {
            // Arrange
            await using var context = CreateContext();
            var nonExistentId = Guid.NewGuid();

            var command = new UpdateProfileCommand(
                Weight: 80,
                Height: 175,
                Age: 28,
                Gender: 'F',
                ActivityLevel: ActivityLevel.LightlyActive,
                Goal: "Mantener");

            var service = new UserService(context);

            // Act
            var result = await service.UpdateProfileAsync(nonExistentId, command);

            // Assert
            Assert.False(result);
        }

        [Fact]
        public async Task UpdateProfileAsync_HappyPath_MultipleUpdates_CreatesMultipleHistoryEntries()
        {
            // Arrange
            await using var context = CreateContext();
            var userId = Guid.NewGuid();
            var user = new User("test@test.com", "hash", "Juan", 180, 90, 80, 30, 'M', ActivityLevel.Sedentary);
            typeof(User).GetProperty("Id")!.SetValue(user, userId);
            context.Users.Add(user);
            await context.SaveChangesAsync();

            var service = new UserService(context);

            var firstUpdate = new UpdateProfileCommand(85, 180, 30, 'M', ActivityLevel.Sedentary, "Perder");
            var secondUpdate = new UpdateProfileCommand(80, 180, 30, 'M', ActivityLevel.LightlyActive, "Mantener");

            // Act
            await service.UpdateProfileAsync(userId, firstUpdate);
            await service.UpdateProfileAsync(userId, secondUpdate);

            // Assert — two history entries should exist
            var historyCount = await context.UserProfileHistory
                .CountAsync(h => h.UserId == userId);
            Assert.Equal(2, historyCount);
        }
    }
}
