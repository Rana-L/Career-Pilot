using System.IdentityModel.Tokens.Jwt;
using CareerPilot.Api.models;
using CareerPilot.Api.services;
using Xunit;

namespace CareerPilot.Api.Tests;

public class TokenServiceTests
{
    [Fact]
    public void CreateToken_IncludesUserIdAndEmailClaims()
    {
        var tokenService = new TokenService(TestHelpers.CreateTestConfiguration());
        var user = new User { Id = 42, Email = "test@example.com" };

        var token = tokenService.CreateToken(user);

        var jwt = new JwtSecurityTokenHandler().ReadJwtToken(token);
        Assert.Equal("42", jwt.Claims.First(c => c.Type == "sub").Value);
        Assert.Equal("test@example.com", jwt.Claims.First(c => c.Type == "email").Value);
    }

    [Fact]
    public void CreateToken_SetsConfiguredIssuerAndAudience()
    {
        var tokenService = new TokenService(TestHelpers.CreateTestConfiguration());
        var user = new User { Id = 1, Email = "a@b.com" };

        var token = tokenService.CreateToken(user);

        var jwt = new JwtSecurityTokenHandler().ReadJwtToken(token);
        Assert.Equal("CareerPilot.Api", jwt.Issuer);
        Assert.Equal("CareerPilot.Client", jwt.Audiences.Single());
    }

    [Fact]
    public void CreateToken_SetsExpiryBasedOnConfiguredMinutes()
    {
        var tokenService = new TokenService(TestHelpers.CreateTestConfiguration());
        var user = new User { Id = 1, Email = "a@b.com" };

        var token = tokenService.CreateToken(user);

        var jwt = new JwtSecurityTokenHandler().ReadJwtToken(token);
        var expectedExpiry = DateTime.UtcNow.AddMinutes(60);
        Assert.True(Math.Abs((jwt.ValidTo - expectedExpiry).TotalMinutes) < 1);
    }
}
