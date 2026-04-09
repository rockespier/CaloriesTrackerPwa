using System;
using System.Threading.Tasks;
using CalorieTracker.Application.Commands;
using CalorieTracker.Application.Interfaces;
using CalorieTracker.Application.Services;
using CalorieTracker.Domain.Entities;
using Moq;
using Xunit;

namespace CalorieTracker.Tests.Application
{
    public class UserServiceTests
    {
        private readonly Mock<IUserRepository> _userRepositoryMock;
        private readonly UserService _userService;

        public UserServiceTests()
        {
            _userRepositoryMock = new Mock<IUserRepository>();
            _userService = new UserService(_userRepositoryMock.Object);
        }

        [Fact]
        public async Task UpdateProfileAsync_HappyPath_UpdatesUserAndReturnsTrue()
        {
            // Arrange
            var userId = Guid.NewGuid();
            var user = new User("test@test.com", "hash", "Juan", 180, 90, 80, 30, 'M', ActivityLevel.Sedentary);
            var command = new UpdateProfileCommand(85, 178, 31, 'M', ActivityLevel.ModeratelyActive, UserGoal.Lose);

            _userRepositoryMock.Setup(r => r.GetByIdAsync(userId)).ReturnsAsync(user);
            _userRepositoryMock.Setup(r => r.AddProfileHistoryAsync(It.IsAny<UserProfileHistory>())).Returns(Task.CompletedTask);
            _userRepositoryMock.Setup(r => r.UpdateAsync(It.IsAny<User>())).Returns(Task.CompletedTask);

            // Act
            var result = await _userService.UpdateProfileAsync(userId, command);

            // Assert
            Assert.True(result);
            Assert.Equal(85, user.CurrentWeightKg);
            Assert.Equal(178, user.HeightCm);
            Assert.Equal(31, user.Age);
            Assert.Equal('M', user.BiologicalSex);
            Assert.Equal(ActivityLevel.ModeratelyActive, user.ActivityLevel);
            Assert.Equal(UserGoal.Lose, user.Goal);
            Assert.True(user.DailyCaloricTarget > 0);
            _userRepositoryMock.Verify(r => r.AddProfileHistoryAsync(It.IsAny<UserProfileHistory>()), Times.Once);
            _userRepositoryMock.Verify(r => r.UpdateAsync(user), Times.Once);
        }

        [Fact]
        public async Task UpdateProfileAsync_SadPath_UserNotFound_ReturnsFalse()
        {
            // Arrange
            var userId = Guid.NewGuid();
            var command = new UpdateProfileCommand(85, 178, 31, 'M', ActivityLevel.Sedentary, UserGoal.Maintain);

            _userRepositoryMock.Setup(r => r.GetByIdAsync(userId)).ReturnsAsync((User?)null);

            // Act
            var result = await _userService.UpdateProfileAsync(userId, command);

            // Assert
            Assert.False(result);
            _userRepositoryMock.Verify(r => r.AddProfileHistoryAsync(It.IsAny<UserProfileHistory>()), Times.Never);
            _userRepositoryMock.Verify(r => r.UpdateAsync(It.IsAny<User>()), Times.Never);
        }

        [Theory]
        [InlineData(UserGoal.Lose, -500)]
        [InlineData(UserGoal.Gain, 300)]
        [InlineData(UserGoal.Maintain, 0)]
        public async Task UpdateProfileAsync_GoalAdjustment_AppliesCorrectCalorieOffset(UserGoal goal, int expectedOffset)
        {
            // Arrange
            var userId = Guid.NewGuid();
            // Male, 80kg, 175cm, 30y, Sedentary
            var user = new User("test@test.com", "hash", "Test", 175, 80, 75, 30, 'M', ActivityLevel.Sedentary);
            var command = new UpdateProfileCommand(80, 175, 30, 'M', ActivityLevel.Sedentary, goal);

            _userRepositoryMock.Setup(r => r.GetByIdAsync(userId)).ReturnsAsync(user);
            _userRepositoryMock.Setup(r => r.AddProfileHistoryAsync(It.IsAny<UserProfileHistory>())).Returns(Task.CompletedTask);
            _userRepositoryMock.Setup(r => r.UpdateAsync(It.IsAny<User>())).Returns(Task.CompletedTask);

            // Act
            await _userService.UpdateProfileAsync(userId, command);

            // Assert: DailyCaloricTarget should be TDEE + offset
            // BMR(Mifflin-St Jeor simplified) = 10*80 + 6.25*175 - 5*30 + 5 = 1748.75
            // TDEE = 1748.75 * 1.2 = 2098.5
            int expectedTarget = (int)(2098.5 + expectedOffset);
            Assert.Equal(expectedTarget, user.DailyCaloricTarget);
        }
    }
}
