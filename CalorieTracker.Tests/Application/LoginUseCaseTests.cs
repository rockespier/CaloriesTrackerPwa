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
    public class LoginUseCaseTests
    {
        private readonly Mock<IUserRepository> _userRepositoryMock;
        private readonly Mock<IPasswordHasher<User>> _passwordHasherMock;
        private readonly Mock<IJwtTokenGenerator> _jwtTokenGeneratorMock;
        private readonly LoginUseCase _useCase;

        public LoginUseCaseTests()
        {
            _userRepositoryMock = new Mock<IUserRepository>();
            _passwordHasherMock = new Mock<IPasswordHasher<User>>();
            _jwtTokenGeneratorMock = new Mock<IJwtTokenGenerator>();
            _useCase = new LoginUseCase(_userRepositoryMock.Object, _passwordHasherMock.Object, _jwtTokenGeneratorMock.Object);
        }

        [Fact]
        public async Task ExecuteAsync_SadPath_InvalidPassword_ThrowsUnauthorizedAccessException()
        {
            // Arrange
            var command = new LoginCommand("test@domain.com", "WrongPassword!");
            var user = new User("test@domain.com", "hashed", "Juan", 180, 80, 75, 30, 'M', ActivityLevel.Sedentary);

            _userRepositoryMock.Setup(x => x.GetByEmailAsync(command.Email)).ReturnsAsync(user);
            _passwordHasherMock.Setup(x => x.VerifyHashedPassword(user, user.PasswordHash, command.Password))
                .Returns(PasswordVerificationResult.Failed);

            // Act & Assert
            var exception = await Assert.ThrowsAsync<UnauthorizedAccessException>(() => _useCase.ExecuteAsync(command));
            Assert.Equal("Credenciales inválidas.", exception.Message);
            _jwtTokenGeneratorMock.Verify(x => x.GenerateToken(It.IsAny<User>()), Times.Never);
        }
    }
}