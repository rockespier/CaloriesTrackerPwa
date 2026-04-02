using System;
using System.Threading.Tasks;
using CalorieTracker.Application.Commands;
using CalorieTracker.Application.Interfaces;
using CalorieTracker.Application.UseCases;
using CalorieTracker.Domain.Entities;
using Microsoft.AspNetCore.Identity;
using Moq;
using Xunit;

namespace CalorieTracker.Tests.Application
{
    public class RegisterUserUseCaseTests
    {
        private readonly Mock<IUserRepository> _userRepositoryMock;
        private readonly Mock<IPasswordHasher<User>> _passwordHasherMock;
        private readonly RegisterUserUseCase _useCase;

        public RegisterUserUseCaseTests()
        {
            _userRepositoryMock = new Mock<IUserRepository>();
            _passwordHasherMock = new Mock<IPasswordHasher<User>>();
            _useCase = new RegisterUserUseCase(_userRepositoryMock.Object, _passwordHasherMock.Object);
        }

        [Fact]
        public async Task ExecuteAsync_HappyPath_ReturnsUserId()
        {
            // Arrange
            var command = new RegisterUserCommand("test@test.com", "Password123!", "Juan", 175, 80, 75, 28, 'M', ActivityLevel.ModeratelyActive);

            _userRepositoryMock.Setup(x => x.ExistsByEmailAsync(command.Email)).ReturnsAsync(false);
            _passwordHasherMock.Setup(x => x.HashPassword(It.IsAny<User>(), command.Password)).Returns("hashed_password");

            // Act
            var result = await _useCase.ExecuteAsync(command);

            // Assert
            Assert.NotEqual(Guid.Empty, result);
            _userRepositoryMock.Verify(x => x.AddAsync(It.Is<User>(u => u.Email == command.Email && u.PasswordHash == "hashed_password")), Times.Once);
        }

        [Fact]
        public async Task ExecuteAsync_SadPath_EmailAlreadyExists_ThrowsInvalidOperationException()
        {
            // Arrange
            var command = new RegisterUserCommand("existente@test.com", "Password123!", "Maria", 160, 60, 55, 25, 'F', ActivityLevel.Sedentary);

            _userRepositoryMock.Setup(x => x.ExistsByEmailAsync(command.Email)).ReturnsAsync(true);

            // Act & Assert
            var exception = await Assert.ThrowsAsync<InvalidOperationException>(() => _useCase.ExecuteAsync(command));
            Assert.Equal("El correo electrónico ya está registrado.", exception.Message);
            _userRepositoryMock.Verify(x => x.AddAsync(It.IsAny<User>()), Times.Never);
        }
    }
}
