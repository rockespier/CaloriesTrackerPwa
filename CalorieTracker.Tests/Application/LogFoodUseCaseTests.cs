using System;
using System.Threading.Tasks;
using CalorieTracker.Application.Commands;
using CalorieTracker.Application.Interfaces;
using CalorieTracker.Application.UseCases;
using CalorieTracker.Domain.Entities;
using Moq;
using Xunit;

namespace CalorieTracker.Tests.Application
{
    public class LogFoodUseCaseTests
    {
        private readonly Mock<IFoodLogRepository> _repositoryMock;
        private readonly Mock<INutritionAnalyzer> _analyzerMock;
        private readonly LogFoodUseCase _useCase;

        public LogFoodUseCaseTests()
        {
            _repositoryMock = new Mock<IFoodLogRepository>();
            _analyzerMock = new Mock<INutritionAnalyzer>();
            _useCase = new LogFoodUseCase(_repositoryMock.Object, _analyzerMock.Object);
        }

        [Fact]
        public async Task ExecuteAsync_HappyPath_SavesLogAndReturnsCalories()
        {
            // Arrange
            var userId = Guid.NewGuid();
            var command = new LogFoodCommand(userId, "dos huevos");

            _analyzerMock.Setup(a => a.AnalyzeCaloriesAsync(command.FoodText)).ReturnsAsync(140);

            // Act
            var result = await _useCase.ExecuteAsync(command);

            // Assert
            Assert.Equal(140, result);
            _repositoryMock.Verify(r => r.AddAsync(It.Is<FoodLog>(
                f => f.UserId == userId && f.OriginalText == "dos huevos" && f.EstimatedCalories == 140
            )), Times.Once);
        }

        [Fact]
        public async Task ExecuteAsync_SadPath_EmptyText_ThrowsArgumentException()
        {
            // Arrange
            var userId = Guid.NewGuid();
            var command = new LogFoodCommand(userId, "");

            // Act & Assert
            // La validación ocurre en el constructor de la entidad FoodLog dentro del UseCase
            await Assert.ThrowsAsync<ArgumentException>(() => _useCase.ExecuteAsync(command));
            _repositoryMock.Verify(r => r.AddAsync(It.IsAny<FoodLog>()), Times.Never);
        }
    }
}