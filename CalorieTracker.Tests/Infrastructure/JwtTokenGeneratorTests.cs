using System;
using System.IdentityModel.Tokens.Jwt;
using System.Linq;
using CalorieTracker.Domain.Entities;
using CalorieTracker.Infrastructure.Auth;
using Microsoft.Extensions.Configuration;
using Xunit;

namespace CalorieTracker.Tests.Infrastructure
{
    public class JwtTokenGeneratorTests
    {
        private const string ValidSecret = "super-secret-key-used-only-in-tests-minimum-32chars";
        private const string ValidIssuer = "CalorieTrackerApi";
        private const string ValidAudience = "CalorieTrackerClient";

        private static IConfiguration BuildConfig(
            string? secret = ValidSecret,
            string issuer = ValidIssuer,
            string audience = ValidAudience,
            string expiryMinutes = "60")
        {
            return new ConfigurationBuilder()
                .AddInMemoryCollection(new System.Collections.Generic.Dictionary<string, string?>
                {
                    ["JwtSettings:Secret"] = secret,
                    ["JwtSettings:Issuer"] = issuer,
                    ["JwtSettings:Audience"] = audience,
                    ["JwtSettings:ExpiryMinutes"] = expiryMinutes
                })
                .Build();
        }

        [Fact]
        public void GenerateToken_HappyPath_ReturnsNonEmptyToken()
        {
            // Arrange
            var generator = new JwtTokenGenerator(BuildConfig());
            var user = new User("test@test.com", "hash", "Juan", 180, 80, 75, 30, 'M', ActivityLevel.Sedentary);

            // Act
            var token = generator.GenerateToken(user);

            // Assert
            Assert.False(string.IsNullOrWhiteSpace(token));
        }

        [Fact]
        public void GenerateToken_HappyPath_TokenContainsCorrectSubjectClaim()
        {
            // Arrange
            var generator = new JwtTokenGenerator(BuildConfig());
            var user = new User("test@test.com", "hash", "Juan", 180, 80, 75, 30, 'M', ActivityLevel.Sedentary);

            // Act
            var token = generator.GenerateToken(user);
            var handler = new JwtSecurityTokenHandler();
            var jwt = handler.ReadJwtToken(token);

            // Assert — the subject (sub) must equal the user ID
            Assert.Equal(user.Id.ToString(), jwt.Subject);
        }

        [Fact]
        public void GenerateToken_HappyPath_TokenContainsEmailClaim()
        {
            // Arrange
            var generator = new JwtTokenGenerator(BuildConfig());
            var user = new User("test@test.com", "hash", "Juan", 180, 80, 75, 30, 'M', ActivityLevel.Sedentary);

            // Act
            var token = generator.GenerateToken(user);
            var handler = new JwtSecurityTokenHandler();
            var jwt = handler.ReadJwtToken(token);

            // Assert
            var emailClaim = jwt.Claims.FirstOrDefault(c => c.Type == JwtRegisteredClaimNames.Email);
            Assert.NotNull(emailClaim);
            Assert.Equal(user.Email, emailClaim.Value);
        }

        [Fact]
        public void GenerateToken_HappyPath_TokenContainsCorrectIssuerAndAudience()
        {
            // Arrange
            var generator = new JwtTokenGenerator(BuildConfig());
            var user = new User("test@test.com", "hash", "Juan", 180, 80, 75, 30, 'M', ActivityLevel.Sedentary);

            // Act
            var token = generator.GenerateToken(user);
            var handler = new JwtSecurityTokenHandler();
            var jwt = handler.ReadJwtToken(token);

            // Assert
            Assert.Equal(ValidIssuer, jwt.Issuer);
            Assert.Contains(ValidAudience, jwt.Audiences);
        }

        [Fact]
        public void GenerateToken_HappyPath_TokenExpiresInFuture()
        {
            // Arrange
            var generator = new JwtTokenGenerator(BuildConfig(expiryMinutes: "30"));
            var user = new User("test@test.com", "hash", "Juan", 180, 80, 75, 30, 'M', ActivityLevel.Sedentary);

            // Act
            var token = generator.GenerateToken(user);
            var handler = new JwtSecurityTokenHandler();
            var jwt = handler.ReadJwtToken(token);

            // Assert — expiry should be ~30 minutes from now
            Assert.True(jwt.ValidTo > DateTime.UtcNow);
            Assert.True(jwt.ValidTo <= DateTime.UtcNow.AddMinutes(31));
        }

        [Fact]
        public void GenerateToken_SadPath_MissingSecret_ThrowsInvalidOperationException()
        {
            // Arrange
            var generator = new JwtTokenGenerator(BuildConfig(secret: null));
            var user = new User("test@test.com", "hash", "Juan", 180, 80, 75, 30, 'M', ActivityLevel.Sedentary);

            // Act & Assert
            Assert.Throws<InvalidOperationException>(() => generator.GenerateToken(user));
        }
    }
}
