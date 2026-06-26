using System.Security.Claims;
using AppInventory.Api.Middleware;
using AppInventory.Core.Authorization;
using AppInventory.Core.Interfaces;
using AppInventory.Infrastructure.Auth;
using AppInventory.Infrastructure.Data;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace AppInventory.Api.Controllers;

/// <summary>
/// Authentication endpoints for local (DB-based) auth.
/// </summary>
[ApiController]
[Route("api/v1/auth")]
[FeatureGate("Authentication")]
public class AuthController : ControllerBase
{
    private readonly IAuthProvider _authProvider;
    private readonly AppInventoryDbContext _dbContext;
    private readonly IConfiguration _configuration;

    public AuthController(
        IAuthProvider authProvider,
        AppInventoryDbContext dbContext,
        IConfiguration configuration)
    {
        _authProvider = authProvider;
        _dbContext = dbContext;
        _configuration = configuration;
    }

    /// <summary>
    /// Authenticate with username and password. Sets a session cookie on success.
    /// </summary>
    /// <param name="request">Login credentials.</param>
    /// <returns>User info on success.</returns>
    /// <response code="200">Authentication successful.</response>
    /// <response code="401">Invalid credentials or account locked.</response>
    [HttpPost("login")]
    [AllowAnonymous]
    [ProducesResponseType(typeof(LoginResponse), 200)]
    [ProducesResponseType(typeof(ProblemDetails), 401)]
    public async Task<IActionResult> LoginAsync([FromBody] LoginRequestDto request, CancellationToken ct)
    {
        var result = await _authProvider.AuthenticateAsync(
            new LoginRequest(request.Username, request.Password), ct);

        if (!result.Success)
        {
            return Problem(
                statusCode: 401,
                title: "Authentication Failed",
                detail: result.ErrorMessage);
        }

        var cookieName = _configuration.GetValue("LocalAuth:SessionCookieName", "appinventory.auth")!;
        var idleTimeout = _configuration.GetValue("LocalAuth:SessionIdleTimeoutMinutes", 60);

        Response.Cookies.Append(cookieName, result.UserId!.Value.ToString(), new CookieOptions
        {
            HttpOnly = true,
            Secure = true,
            SameSite = SameSiteMode.Lax,
            MaxAge = TimeSpan.FromMinutes(idleTimeout),
            Path = "/"
        });

        var user = await _dbContext.Users
            .Include(u => u.UserRoles)
                .ThenInclude(ur => ur.Role)
            .FirstAsync(u => u.Id == result.UserId.Value, ct);

        return Ok(new LoginResponse(
            user.Id,
            user.DisplayName,
            user.Email,
            user.UserRoles.Select(ur => ur.Role.Name).ToList(),
            result.MustChangePassword));
    }

    /// <summary>
    /// End the current session by clearing the auth cookie.
    /// </summary>
    /// <response code="204">Logout successful.</response>
    [HttpPost("logout")]
    [AllowAnonymous]
    [ProducesResponseType(204)]
    public IActionResult Logout()
    {
        var cookieName = _configuration.GetValue("LocalAuth:SessionCookieName", "appinventory.auth")!;
        Response.Cookies.Delete(cookieName, new CookieOptions
        {
            HttpOnly = true,
            Secure = true,
            SameSite = SameSiteMode.Lax,
            Path = "/"
        });
        return NoContent();
    }

    /// <summary>
    /// Get the current authenticated user's information and roles.
    /// </summary>
    /// <response code="200">Current user info.</response>
    /// <response code="401">Not authenticated.</response>
    [HttpGet("me")]
    [Authorize]
    [ProducesResponseType(typeof(MeResponse), 200)]
    [ProducesResponseType(typeof(ProblemDetails), 401)]
    public async Task<IActionResult> MeAsync(CancellationToken ct)
    {
        var userIdClaim = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (userIdClaim is null || !int.TryParse(userIdClaim, out var userId))
        {
            return Unauthorized();
        }

        var user = await _dbContext.Users
            .Include(u => u.UserRoles)
                .ThenInclude(ur => ur.Role)
            .FirstOrDefaultAsync(u => u.Id == userId, ct);

        if (user is null)
            return Unauthorized();

        var mustChange = User.HasClaim(ClaimNames.MustChangePassword, "true");

        return Ok(new MeResponse(
            user.Id,
            user.DisplayName,
            user.Email,
            user.UserRoles.Select(ur => ur.Role.Name).ToList(),
            mustChange));
    }

    /// <summary>
    /// Change the current user's password.
    /// </summary>
    /// <param name="request">Current and new password.</param>
    /// <response code="204">Password changed.</response>
    /// <response code="400">Password policy violation or wrong current password.</response>
    /// <response code="401">Not authenticated.</response>
    [HttpPost("change-password")]
    [Authorize]
    [ProducesResponseType(204)]
    [ProducesResponseType(typeof(ProblemDetails), 400)]
    [ProducesResponseType(typeof(ProblemDetails), 401)]
    public async Task<IActionResult> ChangePasswordAsync(
        [FromBody] ChangePasswordRequestDto request,
        [FromServices] PasswordService passwordService,
        CancellationToken ct)
    {
        var userIdClaim = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (userIdClaim is null || !int.TryParse(userIdClaim, out var userId))
        {
            return Unauthorized();
        }

        var (success, error) = await passwordService.ChangePasswordAsync(
            userId, request.CurrentPassword, request.NewPassword, ct);

        if (!success)
        {
            return Problem(
                statusCode: 400,
                title: "Password Change Failed",
                detail: error);
        }

        return NoContent();
    }
}

/// <summary>Login request body.</summary>
public record LoginRequestDto(string Username, string Password);

/// <summary>Change password request body.</summary>
public record ChangePasswordRequestDto(string CurrentPassword, string NewPassword);

/// <summary>Login response.</summary>
public record LoginResponse(int Id, string DisplayName, string Email, List<string> Roles, bool MustChangePassword);

/// <summary>Current user response.</summary>
public record MeResponse(int Id, string DisplayName, string Email, List<string> Roles, bool MustChangePassword);
