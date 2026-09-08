using CareerPilot.Api.Controllers;
using CareerPilot.Api.dto;
using CareerPilot.Api.services;
using Microsoft.AspNetCore.Mvc;
using Xunit;

namespace CareerPilot.Api.Tests;

public class AuthControllerTests
{
    private static AuthController CreateController()
    {
        var context = TestHelpers.CreateInMemoryContext();
        var tokenService = new TokenService(TestHelpers.CreateTestConfiguration());
        return new AuthController(context, tokenService);
    }

    [Fact]
    public async Task Register_WithNewEmail_CreatesUserAndReturnsToken()
    {
        var controller = CreateController();

        var result = await controller.Register(new RegisterRequest { Email = "new@example.com", Password = "Password123!" });

        var ok = Assert.IsType<OkObjectResult>(result.Result);
        var response = Assert.IsType<AuthResponse>(ok.Value);
        Assert.Equal("new@example.com", response.Email);
        Assert.False(string.IsNullOrEmpty(response.Token));
    }

    [Fact]
    public async Task Register_WithDuplicateEmail_ReturnsBadRequest()
    {
        var controller = CreateController();
        await controller.Register(new RegisterRequest { Email = "dupe@example.com", Password = "Password123!" });

        var result = await controller.Register(new RegisterRequest { Email = "dupe@example.com", Password = "Different123!" });

        Assert.IsType<BadRequestObjectResult>(result.Result);
    }

    [Fact]
    public async Task Login_WithCorrectPassword_ReturnsToken()
    {
        var controller = CreateController();
        await controller.Register(new RegisterRequest { Email = "user@example.com", Password = "CorrectPassword1!" });

        var result = await controller.Login(new LoginRequest { Email = "user@example.com", Password = "CorrectPassword1!" });

        var ok = Assert.IsType<OkObjectResult>(result.Result);
        var response = Assert.IsType<AuthResponse>(ok.Value);
        Assert.Equal("user@example.com", response.Email);
    }

    [Fact]
    public async Task Login_WithWrongPassword_ReturnsUnauthorized()
    {
        var controller = CreateController();
        await controller.Register(new RegisterRequest { Email = "user2@example.com", Password = "CorrectPassword1!" });

        var result = await controller.Login(new LoginRequest { Email = "user2@example.com", Password = "WrongPassword!" });

        Assert.IsType<UnauthorizedObjectResult>(result.Result);
    }

    [Fact]
    public async Task Login_WithUnknownEmail_ReturnsUnauthorized()
    {
        var controller = CreateController();

        var result = await controller.Login(new LoginRequest { Email = "nobody@example.com", Password = "Whatever1!" });

        Assert.IsType<UnauthorizedObjectResult>(result.Result);
    }
}
