using System.IdentityModel.Tokens.Jwt;
using Microsoft.Extensions.Options;
using TaskFlow.Domain.Entities;
using TaskFlow.Infrastructure.Security;

namespace TaskFlow.UnitTests.Security;

public class JwtTokenGeneratorTests
{
    private readonly JwtTokenGenerator _sut;
    private readonly JwtSettings _settings = new()
    {
        Secret = "unit-test-secret-at-least-32-characters-long",
        Issuer = "TaskFlow.Tests",
        Audience = "TaskFlow.Tests",
        ExpirationMinutes = 60
    };

    public JwtTokenGeneratorTests()
    {
        _sut = new JwtTokenGenerator(Options.Create(_settings));
    }

    [Fact]
    public void GenerateToken_ProducesAReadableJwt_WithExpectedClaims()
    {
        var user = new User { Id = 42, Name = "Ada Lovelace", Email = "ada@test.com" };

        var token = _sut.GenerateToken(user);
        var jwt = new JwtSecurityTokenHandler().ReadJwtToken(token);

        Assert.Equal(_settings.Issuer, jwt.Issuer);
        Assert.Contains(_settings.Audience, jwt.Audiences);
        Assert.Equal("42", jwt.Claims.First(c => c.Type == JwtRegisteredClaimNames.Sub).Value);
        Assert.Equal("ada@test.com", jwt.Claims.First(c => c.Type == JwtRegisteredClaimNames.Email).Value);
    }
}
