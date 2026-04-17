using System.IdentityModel.Tokens.Jwt;
using System.Linq;
using CalorieTracker.Domain.Entities;
using CalorieTracker.Infrastructure.Auth;
using Microsoft.Extensions.Configuration;

namespace CalorieTracker.Tests.Infrastructure;

public class JwtTokenGeneratorTests
{
    private const string ValidSecret   = "super-secret-key-used-only-in-tests-minimum-32chars";
    private const string ValidIssuer   = "CalorieTrackerApi";
    private const string ValidAudience = "CalorieTrackerClient";

    private static IConfiguration BuildConfig(
        string? secret        = ValidSecret,
        string  issuer        = ValidIssuer,
        string  audience      = ValidAudience,
        string  expiryMinutes = "60") =>
        new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["JwtSettings:Secret"]        = secret,
                ["JwtSettings:Issuer"]        = issuer,
                ["JwtSettings:Audience"]      = audience,
                ["JwtSettings:ExpiryMinutes"] = expiryMinutes
            })
            .Build();

    [Fact]
    public void GenerateToken_HappyPath_ReturnsNonEmptyToken()
    {
        var generator = new JwtTokenGenerator(BuildConfig());
        var user      = new User("test@test.com", "hash", "Juan", 180, 80, 75, 30, 'M', ActivityLevel.Sedentary);

        var token = generator.GenerateToken(user);

        Assert.False(string.IsNullOrWhiteSpace(token));
    }

    [Fact]
    public void GenerateToken_HappyPath_TokenContainsCorrectSubjectClaim()
    {
        var generator = new JwtTokenGenerator(BuildConfig());
        var user      = new User("test@test.com", "hash", "Juan", 180, 80, 75, 30, 'M', ActivityLevel.Sedentary);

        var token   = generator.GenerateToken(user);
        var handler = new JwtSecurityTokenHandler();
        var jwt     = handler.ReadJwtToken(token);

        Assert.Equal(user.Id.ToString(), jwt.Subject);
    }

    [Fact]
    public void GenerateToken_HappyPath_TokenContainsEmailClaim()
    {
        var generator = new JwtTokenGenerator(BuildConfig());
        var user      = new User("test@test.com", "hash", "Juan", 180, 80, 75, 30, 'M', ActivityLevel.Sedentary);

        var token   = generator.GenerateToken(user);
        var handler = new JwtSecurityTokenHandler();
        var jwt     = handler.ReadJwtToken(token);

        var emailClaim = jwt.Claims.FirstOrDefault(c => c.Type == JwtRegisteredClaimNames.Email);
        Assert.NotNull(emailClaim);
        Assert.Equal(user.Email, emailClaim.Value);
    }

    [Fact]
    public void GenerateToken_HappyPath_TokenExpiresInFuture()
    {
        var generator = new JwtTokenGenerator(BuildConfig(expiryMinutes: "30"));
        var user      = new User("test@test.com", "hash", "Juan", 180, 80, 75, 30, 'M', ActivityLevel.Sedentary);

        var token   = generator.GenerateToken(user);
        var handler = new JwtSecurityTokenHandler();
        var jwt     = handler.ReadJwtToken(token);

        Assert.True(jwt.ValidTo > DateTime.UtcNow);
        Assert.True(jwt.ValidTo <= DateTime.UtcNow.AddMinutes(31));
    }

    [Fact]
    public void GenerateToken_SadPath_MissingSecret_ThrowsInvalidOperationException()
    {
        var generator = new JwtTokenGenerator(BuildConfig(secret: null));
        var user      = new User("test@test.com", "hash", "Juan", 180, 80, 75, 30, 'M', ActivityLevel.Sedentary);

        Assert.Throws<InvalidOperationException>(() => generator.GenerateToken(user));
    }
}
