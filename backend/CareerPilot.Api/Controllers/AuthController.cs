using CareerPilot.Api.data;
using CareerPilot.Api.dto;
using CareerPilot.Api.models;
using CareerPilot.Api.services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.EntityFrameworkCore;

namespace CareerPilot.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
[EnableRateLimiting("auth")]
public class AuthController : ControllerBase
{
    private readonly AppDbContext _context;
    private readonly TokenService _tokenService;

    // Computed once at startup; used to keep the login response time roughly
    // constant whether or not the email exists, so response timing can't be
    // used to enumerate registered accounts.
    private static readonly string DummyPasswordHash =
        BCrypt.Net.BCrypt.HashPassword(Guid.NewGuid().ToString());

    public AuthController(AppDbContext context, TokenService tokenServices)
    {
        _context = context;
        _tokenService = tokenServices;
    }

    [HttpPost("register")]
    public async Task<ActionResult<AuthResponse>> Register(RegisterRequest request)
    {
        var emailError = AuthValidation.ValidateEmail(request.Email);
        if (emailError is not null) return BadRequest(emailError);

        var passwordError = AuthValidation.ValidatePassword(request.Password);
        if (passwordError is not null) return BadRequest(passwordError);

        var email = AuthValidation.NormalizeEmail(request.Email);

        var emailExists = await _context.Users.AnyAsync(u => u.Email == email);
        if (emailExists)
        {
            return BadRequest("A user with this email already exists.");
        }

        var user = new User
        {
            Email = email,
            PasswordHash = BCrypt.Net.BCrypt.HashPassword(request.Password)
        };

        _context.Users.Add(user);
        await _context.SaveChangesAsync();

        var token = _tokenService.CreateToken(user);

        return Ok(new AuthResponse { Token = token, Email = user.Email });
    }

    [HttpPost("login")]
    public async Task<ActionResult<AuthResponse>> Login(LoginRequest request)
    {
        var email = AuthValidation.NormalizeEmail(request.Email);
        var user = await _context.Users.FirstOrDefaultAsync(u => u.Email == email);

        var passwordMatches = BCrypt.Net.BCrypt.Verify(
            request.Password, user?.PasswordHash ?? DummyPasswordHash);

        if (user is null || !passwordMatches)
        {
            return Unauthorized("Invalid email or password.");
        }

        var token = _tokenService.CreateToken(user);

        return Ok(new AuthResponse { Token = token, Email = user.Email });
    }
}
