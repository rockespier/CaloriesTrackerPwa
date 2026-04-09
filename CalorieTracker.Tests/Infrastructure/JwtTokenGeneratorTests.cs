using System;
using CalorieTracker.Domain.Entities;
using CalorieTracker.Infrastructure.Auth;
using Microsoft.Extensions.Configuration;
using Xunit;

namespace CalorieTracker.Tests.Infrastructure
{
    public class JwtTokenGeneratorTests
    {
        private static User CreateTestUser() =>
            new User("test@example.com", "hashed", "Test", 175, 70, 65, 28, 'M', ActivityLevel.Sedentary);

        private static IConfiguration BuildConfiguration(
            string? secret = "SuperSecretKeyForTestingPurposesOnly!",
            string issuer = "TestIssuer",
            string audience = "TestAudience",
            string? expiryMinutes = "60")
        {
            var inMemorySettings = new System.Collections.Generic.Dictionary<string, string?>(StringComparer.OrdinalIgnoreCase)
            {
                ["JwtSettings:Secret"] = secret,
                ["JwtSettings:Issuer"] = issuer,
                ["JwtSettings:Audience"] = audience,
                ["JwtSettings:ExpiryMinutes"] = expiryMinutes
            };

            return new ConfigurationBuilder()
                .AddInMemoryCollection(inMemorySettings)
                .Build();
        }

        [Fact]
        public void GenerateToken_HappyPath_ReturnsNonEmptyToken()
        {
            // Arrange
            var config = BuildConfiguration();
            var generator = new JwtTokenGenerator(config);
            var user = CreateTestUser();

            // Act
            var token = generator.GenerateToken(user);

            // Assert
            Assert.False(string.IsNullOrWhiteSpace(token));
        }

        [Fact]
        public void GenerateToken_SadPath_MissingSecret_ThrowsInvalidOperationException()
        {
            // Arrange
            var config = BuildConfiguration(secret: null);
            var generator = new JwtTokenGenerator(config);
            var user = CreateTestUser();

            // Act & Assert
            var ex = Assert.Throws<InvalidOperationException>(() => generator.GenerateToken(user));
            Assert.Contains("JWT Secret", ex.Message, StringComparison.OrdinalIgnoreCase);
        }

        [Fact]
        public void GenerateToken_SadPath_SecretTooShort_ThrowsInvalidOperationException()
        {
            // Arrange — secret shorter than 32 characters
            var config = BuildConfiguration(secret: "short");
            var generator = new JwtTokenGenerator(config);
            var user = CreateTestUser();

            // Act & Assert
            var ex = Assert.Throws<InvalidOperationException>(() => generator.GenerateToken(user));
            Assert.Contains("32", ex.Message);
        }

        [Fact]
        public void GenerateToken_HappyPath_InvalidExpiryMinutesFallsBackToDefault()
        {
            // Arrange — ExpiryMinutes is not a valid number
            var config = BuildConfiguration(expiryMinutes: "not-a-number");
            var generator = new JwtTokenGenerator(config);
            var user = CreateTestUser();

            // Act — should NOT throw; falls back to 60 minutes
            var token = generator.GenerateToken(user);

            // Assert
            Assert.False(string.IsNullOrWhiteSpace(token));
        }

        [Fact]
        public void GenerateToken_HappyPath_Exactly32CharSecret_Succeeds()
        {
            // Arrange — exactly 32 characters (boundary condition)
            var config = BuildConfiguration(secret: "12345678901234567890123456789012");
            var generator = new JwtTokenGenerator(config);
            var user = CreateTestUser();

            // Act
            var token = generator.GenerateToken(user);

            // Assert
            Assert.False(string.IsNullOrWhiteSpace(token));
        }
    }
}
