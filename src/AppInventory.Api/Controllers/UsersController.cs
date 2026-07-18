using System.Security.Claims;
using AppInventory.Api.Authorization;
using AppInventory.Api.Middleware;
using AppInventory.Core.Authorization;
using AppInventory.Core.Entities;
using AppInventory.Core.Interfaces;
using AppInventory.Infrastructure.Data;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace AppInventory.Api.Controllers;

/// <summary>
/// User and user-role management. [Admin]
/// </summary>
[ApiController]
[Route("api/v1/admin/users")]
[FeatureGate("Admin")]
[Authorize]
[RbacAuthorize]
public class UsersController : ControllerBase
{
    private readonly AppInventoryDbContext _dbContext;
    private readonly IAuditProvider _audit;

    public UsersController(AppInventoryDbContext dbContext, IAuditProvider audit)
    {
        _dbContext = dbContext;
        _audit = audit;
    }

    /// <summary>List all users with their active roles. [Admin]</summary>
    /// <response code="200">Paginated list of users.</response>
    /// <response code="403">Insufficient role.</response>
    [HttpGet]
    [ProducesResponseType(typeof(PagedResponse<AdminUserDto>), 200)]
    [ProducesResponseType(403)]
    public async Task<IActionResult> ListAsync(
        [FromQuery] bool? isActive = null,
        [FromQuery] string? q = null,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20,
        CancellationToken ct = default)
    {
        if (!User.IsInRole(RoleNames.Admin))
        {
            return ForbiddenProblem();
        }

        if (page < 1)
        {
            page = 1;
        }

        if (pageSize < 1)
        {
            pageSize = 1;
        }

        if (pageSize > 100)
        {
            pageSize = 100;
        }

        var query = _dbContext.Users.AsQueryable();

        if (isActive.HasValue)
        {
            query = query.Where(u => u.IsActive == isActive.Value);
        }

        if (!string.IsNullOrWhiteSpace(q))
        {
            var lower = q.ToLower();
            query = query.Where(u => u.DisplayName.ToLower().Contains(lower) || u.Email.ToLower().Contains(lower));
        }

        var total = await query.CountAsync(ct);

        var users = await query
            .OrderBy(u => u.DisplayName)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .Select(u => new AdminUserDto(
                u.Id, u.DisplayName, u.Email, u.IsActive, u.LastLogin, u.CreatedAt,
                u.UserRoles.Select(ur => new UserRoleSummaryDto(ur.RoleId, ur.Role.Name, ur.Source.ToString(), ur.GrantedAt)).ToList()))
            .ToListAsync(ct);

        return Ok(new PagedResponse<AdminUserDto>(users, total, page, pageSize));
    }

    /// <summary>Get a single user with roles. [Admin]</summary>
    /// <response code="200">User detail.</response>
    /// <response code="403">Insufficient role.</response>
    /// <response code="404">User not found.</response>
    [HttpGet("{id:int}")]
    [ProducesResponseType(typeof(AdminUserDto), 200)]
    [ProducesResponseType(403)]
    [ProducesResponseType(404)]
    public async Task<IActionResult> GetAsync(int id, CancellationToken ct = default)
    {
        if (!User.IsInRole(RoleNames.Admin))
        {
            return ForbiddenProblem();
        }

        var user = await _dbContext.Users
            .Include(u => u.UserRoles)
            .ThenInclude(ur => ur.Role)
            .Where(u => u.Id == id)
            .FirstOrDefaultAsync(ct);

        if (user == null)
        {
            return NotFoundProblem(id, "User");
        }

        return Ok(new AdminUserDto(
            user.Id, user.DisplayName, user.Email, user.IsActive, user.LastLogin, user.CreatedAt,
            user.UserRoles.Select(ur => new UserRoleSummaryDto(ur.RoleId, ur.Role.Name, ur.Source.ToString(), ur.GrantedAt)).ToList()));
    }

    /// <summary>Set user active/inactive status. [Admin]</summary>
    /// <response code="200">Status updated.</response>
    /// <response code="403">Insufficient role.</response>
    /// <response code="404">User not found.</response>
    [HttpPatch("{id:int}/active")]
    [ProducesResponseType(200)]
    [ProducesResponseType(403)]
    [ProducesResponseType(404)]
    public async Task<IActionResult> PatchActiveAsync(int id, [FromBody] PatchUserActiveRequest req, CancellationToken ct = default)
    {
        if (!User.IsInRole(RoleNames.Admin))
        {
            return ForbiddenProblem();
        }

        var user = await _dbContext.Users.FindAsync([id], ct);
        if (user == null)
        {
            return NotFoundProblem(id, "User");
        }

        var oldActive = user.IsActive;
        user.IsActive = req.IsActive;
        await _dbContext.SaveChangesAsync(ct);

        await _audit.LogAsync(req.IsActive ? "Activated" : "Deactivated", "User", id.ToString(),
            userId: GetUserId(),
            oldValueJson: $"{{\"isActive\":{oldActive.ToString().ToLower()}}}",
            newValueJson: $"{{\"isActive\":{req.IsActive.ToString().ToLower()}}}",
            ipAddress: HttpContext.Connection.RemoteIpAddress?.ToString(),
            userAgent: HttpContext.Request.Headers.UserAgent.ToString(), ct: ct);

        return Ok();
    }

    /// <summary>Assign a role to a user (manual grant). [Admin]</summary>
    /// <response code="201">Role assigned.</response>
    /// <response code="400">Role already assigned.</response>
    /// <response code="403">Insufficient role.</response>
    /// <response code="404">User or role not found.</response>
    [HttpPost("{id:int}/roles")]
    [ProducesResponseType(typeof(UserRoleSummaryDto), 201)]
    [ProducesResponseType(400)]
    [ProducesResponseType(403)]
    [ProducesResponseType(404)]
    public async Task<IActionResult> AssignRoleAsync(int id, [FromBody] AssignRoleRequest req, CancellationToken ct = default)
    {
        if (!User.IsInRole(RoleNames.Admin))
        {
            return ForbiddenProblem();
        }

        var user = await _dbContext.Users.FindAsync([id], ct);
        if (user == null)
        {
            return NotFoundProblem(id, "User");
        }

        var role = await _dbContext.Roles.FindAsync([req.RoleId], ct);
        if (role == null)
        {
            return NotFoundProblem(req.RoleId, "Role");
        }

        var exists = await _dbContext.UserRoles.AnyAsync(ur => ur.UserId == id && ur.RoleId == req.RoleId, ct);
        if (exists)
        {
            return BadRequestProblem("Role is already assigned to this user.");
        }

        var now = DateTime.UtcNow;
        var grantedBy = GetUserId();

        var userRole = new UserRole
        {
            UserId = id,
            RoleId = req.RoleId,
            GrantedByUserId = grantedBy,
            GrantedAt = now,
            Source = RoleGrantSource.Manual
        };

        _dbContext.UserRoles.Add(userRole);
        await _dbContext.SaveChangesAsync(ct);

        await _audit.LogAsync("RoleAssigned", "User", id.ToString(),
            userId: grantedBy,
            newValueJson: $"{{\"roleId\":{req.RoleId},\"roleName\":\"{role.Name}\",\"source\":\"Manual\"}}",
            ipAddress: HttpContext.Connection.RemoteIpAddress?.ToString(),
            userAgent: HttpContext.Request.Headers.UserAgent.ToString(), ct: ct);

        return StatusCode(201, new UserRoleSummaryDto(role.Id, role.Name, "Manual", now));
    }

    /// <summary>Remove a manually-assigned role from a user. [Admin]</summary>
    /// <response code="204">Role removed.</response>
    /// <response code="400">Cannot remove group-mapped role via this endpoint.</response>
    /// <response code="403">Insufficient role.</response>
    /// <response code="404">User or role assignment not found.</response>
    [HttpDelete("{id:int}/roles/{roleId:int}")]
    [ProducesResponseType(204)]
    [ProducesResponseType(400)]
    [ProducesResponseType(403)]
    [ProducesResponseType(404)]
    public async Task<IActionResult> RemoveRoleAsync(int id, int roleId, CancellationToken ct = default)
    {
        if (!User.IsInRole(RoleNames.Admin))
        {
            return ForbiddenProblem();
        }

        var userRole = await _dbContext.UserRoles
            .Where(ur => ur.UserId == id && ur.RoleId == roleId)
            .FirstOrDefaultAsync(ct);

        if (userRole == null)
        {
            return NotFoundProblem(roleId, "UserRole");
        }

        if (userRole.Source != RoleGrantSource.Manual)
        {
            return BadRequestProblem("Only manually-assigned roles can be removed via this endpoint. Revoke via group mapping.");
        }

        _dbContext.UserRoles.Remove(userRole);
        await _dbContext.SaveChangesAsync(ct);

        await _audit.LogAsync("RoleRevoked", "User", id.ToString(),
            userId: GetUserId(),
            oldValueJson: $"{{\"roleId\":{roleId}}}",
            ipAddress: HttpContext.Connection.RemoteIpAddress?.ToString(),
            userAgent: HttpContext.Request.Headers.UserAgent.ToString(), ct: ct);

        return NoContent();
    }

    private int? GetUserId()
    {
        var idClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        return int.TryParse(idClaim, out var id) ? id : null;
    }

    private static IActionResult NotFoundProblem(int id, string resource)
    {
        return new ObjectResult(new ProblemDetails
        {
            Type = "https://tools.ietf.org/html/rfc7231#section-6.5.4",
            Title = "Not Found",
            Status = 404,
            Detail = $"{resource} with id {id} was not found."
        })
        {
            StatusCode = 404,
            ContentTypes = { "application/problem+json" }
        };
    }

    private static IActionResult ForbiddenProblem()
    {
        return new ObjectResult(new ProblemDetails
        {
            Type = "https://tools.ietf.org/html/rfc7807",
            Title = "Forbidden",
            Status = 403,
            Detail = "You do not have permission to perform this action."
        })
        {
            StatusCode = 403,
            ContentTypes = { "application/problem+json" }
        };
    }

    private static IActionResult BadRequestProblem(string detail)
    {
        return new ObjectResult(new ProblemDetails
        {
            Type = "https://tools.ietf.org/html/rfc7807",
            Title = "Bad Request",
            Status = 400,
            Detail = detail
        })
        {
            StatusCode = 400,
            ContentTypes = { "application/problem+json" }
        };
    }
}

// --- DTOs ---

/// <summary>User with roles (admin view).</summary>
public record AdminUserDto(int Id, string DisplayName, string Email, bool IsActive, DateTime? LastLogin, DateTime CreatedAt, IReadOnlyList<UserRoleSummaryDto> Roles);

/// <summary>User role summary.</summary>
public record UserRoleSummaryDto(int RoleId, string RoleName, string Source, DateTime GrantedAt);

/// <summary>Set user active state.</summary>
public record PatchUserActiveRequest(bool IsActive);

/// <summary>Assign role to user.</summary>
public record AssignRoleRequest(int RoleId);
