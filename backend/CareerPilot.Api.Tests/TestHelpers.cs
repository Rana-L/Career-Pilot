using System.Security.Claims;
using CareerPilot.Api.data;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;

namespace CareerPilot.Api.Tests;

public static class TestHelpers
{
    public static AppDbContext CreateInMemoryContext()
    {
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;

        return new AppDbContext(options);
    }

    public static IConfiguration CreateTestConfiguration()
    {
        var values = new Dictionary<string, string?>
        {
            ["Jwt:Issuer"] = "CareerPilot.Api",
            ["Jwt:Audience"] = "CareerPilot.Client",
            ["Jwt:Key"] = "test-signing-key-that-is-long-enough-for-hmacsha256",
            ["Jwt:ExpiryMinutes"] = "60"
        };

        return new ConfigurationBuilder().AddInMemoryCollection(values).Build();
    }

    public static void SetUser(ControllerBase controller, int userId)
    {
        var claims = new List<Claim> { new Claim("sub", userId.ToString()) };
        var identity = new ClaimsIdentity(claims, "TestAuth");
        var principal = new ClaimsPrincipal(identity);

        controller.ControllerContext = new ControllerContext
        {
            HttpContext = new DefaultHttpContext { User = principal }
        };
    }
}
