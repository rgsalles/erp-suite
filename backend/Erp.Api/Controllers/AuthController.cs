using Erp.Api.Data;
using Erp.Api.Dtos;
using Erp.Api.Models;
using Erp.Api.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace Erp.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public sealed class AuthController(
    ErpDbContext db,
    PasswordHasher<AppUser> passwordHasher,
    JwtTokenService jwtTokenService,
    AuditLogService auditLogService) : ControllerBase
{
    [HttpPost("register")]
    [AllowAnonymous]
    public async Task<ActionResult<AuthResponse>> Register(RegisterRequest request)
    {
        var email = request.Email.Trim().ToLowerInvariant();
        var exists = await db.Users.AnyAsync(x => x.Email == email);

        if (exists)
        {
            return Conflict("This email is already registered.");
        }

        var isFirstUser = !await db.Users.AnyAsync();
        var canChooseRole = User.Identity?.IsAuthenticated == true && User.IsInRole(UserRole.Admin.ToString());

        var user = new AppUser
        {
            FullName = request.FullName.Trim(),
            Email = email,
            Role = isFirstUser ? UserRole.Admin : canChooseRole && request.Role.HasValue ? request.Role.Value : UserRole.Operator,
            IsActive = true
        };

        user.PasswordHash = passwordHasher.HashPassword(user, request.Password);

        db.Users.Add(user);
        await db.SaveChangesAsync();
        await LogAuthActionAsync("Auth.Register", user, $"Registered user {user.Email}");

        return Ok(CreateAuthResponse(user));
    }

    [HttpPost("login")]
    [AllowAnonymous]
    public async Task<ActionResult<AuthResponse>> Login(LoginRequest request)
    {
        var email = request.Email.Trim().ToLowerInvariant();
        var user = await db.Users.FirstOrDefaultAsync(x => x.Email == email);

        if (user is null || !user.IsActive)
        {
            return Unauthorized("Invalid email or password.");
        }

        var result = passwordHasher.VerifyHashedPassword(user, user.PasswordHash, request.Password);

        if (result == PasswordVerificationResult.Failed)
        {
            return Unauthorized("Invalid email or password.");
        }

        await LogAuthActionAsync("Auth.Login", user, $"User {user.Email} logged in");

        return Ok(CreateAuthResponse(user));
    }

    [HttpGet("me")]
    [Authorize]
    public async Task<ActionResult<UserSummaryDto>> Me()
    {
        var userId = User.GetUserId();
        var user = await db.Users.AsNoTracking().FirstOrDefaultAsync(x => x.Id == userId);

        return user is null ? Unauthorized() : Ok(ToDto(user));
    }

    private AuthResponse CreateAuthResponse(AppUser user)
    {
        return new AuthResponse(jwtTokenService.CreateToken(user), jwtTokenService.GetExpirationUtc(), ToDto(user));
    }

    private static UserSummaryDto ToDto(AppUser user)
    {
        return new UserSummaryDto(user.Id, user.FullName, user.Email, user.Role, user.IsActive);
    }

    private async Task LogAuthActionAsync(string action, AppUser user, string details)
    {
        await auditLogService.LogAsync(new AuditEntry(
            UserId: user.Id,
            UserName: user.FullName,
            UserEmail: user.Email,
            Action: action,
            HttpMethod: HttpContext.Request.Method,
            Path: HttpContext.Request.Path.Value ?? string.Empty,
            Controller: "Auth",
            EntityName: "User",
            EntityId: user.Id.ToString(),
            StatusCode: StatusCodes.Status200OK,
            IpAddress: HttpContext.Connection.RemoteIpAddress?.ToString(),
            UserAgent: HttpContext.Request.Headers.UserAgent.ToString(),
            Details: details),
            HttpContext.RequestAborted);
    }
}
