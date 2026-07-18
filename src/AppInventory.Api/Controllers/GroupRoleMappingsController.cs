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
/// Group-to-role mapping management. [Admin]
/// </summary>
[ApiController]
[Route("api/v1/admin/group-role-mappings")]
[FeatureGate("Admin")]
[Authorize]
[RbacAuthorize]
public class GroupRoleMappingsController : ControllerBase
{
    private readonly AppInventoryDbContext _dbContext;
    private readonly IAuditProvider _audit;

    public GroupRoleMappingsController(AppInventoryDbContext dbContext, IAuditProvider audit)
    {
        _dbContext = dbContext;
        _audit = audit;
    }

    /// <summary>List all group-role mappings. [Admin]</summary>
    /// <response code="200">List of mappings.</response>
    /// <response code="403">Insufficient role.</response>
    [HttpGet]
    [ProducesResponseType(typeof(IEnumerable<GroupRoleMappingDto>), 200)]
    [ProducesResponseType(403)]
    public async Task<IActionResult> ListAsync([FromQuery] AuthProviderType? providerType = null, CancellationToken ct = default)
    {
        if (!User.IsInRole(RoleNames.Admin))
        {
            return ForbiddenProblem();
        }

        var query = _dbContext.GroupRoleMappings
            .Include(m => m.Role)
            .AsQueryable();

        if (providerType.HasValue)
        {
            query = query.Where(m => m.ProviderType == providerType.Value);
        }

        var items = await query
            .OrderBy(m => m.ProviderType)
            .ThenBy(m => m.ExternalGroupRef)
            .Select(m => new GroupRoleMappingDto(
                m.Id, m.ProviderType.ToString(), m.ExternalGroupRef,
                m.RoleId, m.Role.Name, m.Description, m.IsActive))
            .ToListAsync(ct);

        return Ok(items);
    }

    /// <summary>Get a single group-role mapping. [Admin]</summary>
    /// <response code="200">Mapping detail.</response>
    /// <response code="403">Insufficient role.</response>
    /// <response code="404">Mapping not found.</response>
    [HttpGet("{id:int}")]
    [ProducesResponseType(typeof(GroupRoleMappingDto), 200)]
    [ProducesResponseType(403)]
    [ProducesResponseType(404)]
    public async Task<IActionResult> GetAsync(int id, CancellationToken ct = default)
    {
        if (!User.IsInRole(RoleNames.Admin))
        {
            return ForbiddenProblem();
        }

        var mapping = await _dbContext.GroupRoleMappings
            .Include(m => m.Role)
            .Where(m => m.Id == id)
            .FirstOrDefaultAsync(ct);

        if (mapping == null)
        {
            return NotFoundProblem(id, "GroupRoleMapping");
        }

        return Ok(new GroupRoleMappingDto(
            mapping.Id, mapping.ProviderType.ToString(), mapping.ExternalGroupRef,
            mapping.RoleId, mapping.Role.Name, mapping.Description, mapping.IsActive));
    }

    /// <summary>Create a group-role mapping. [Admin]</summary>
    /// <response code="201">Mapping created.</response>
    /// <response code="400">Validation error.</response>
    /// <response code="403">Insufficient role.</response>
    /// <response code="404">Role not found.</response>
    /// <response code="409">Mapping already exists for this provider/group/role combination.</response>
    [HttpPost]
    [ProducesResponseType(typeof(GroupRoleMappingDto), 201)]
    [ProducesResponseType(400)]
    [ProducesResponseType(403)]
    [ProducesResponseType(404)]
    [ProducesResponseType(409)]
    public async Task<IActionResult> CreateAsync([FromBody] CreateGroupRoleMappingRequest req, CancellationToken ct = default)
    {
        if (!User.IsInRole(RoleNames.Admin))
        {
            return ForbiddenProblem();
        }

        var role = await _dbContext.Roles.FindAsync([req.RoleId], ct);
        if (role == null)
        {
            return NotFoundProblem(req.RoleId, "Role");
        }

        var exists = await _dbContext.GroupRoleMappings.AnyAsync(
            m => m.ProviderType == req.ProviderType && m.ExternalGroupRef == req.ExternalGroupRef && m.RoleId == req.RoleId, ct);

        if (exists)
        {
            return ConflictProblem("A mapping for this provider/group/role combination already exists.");
        }

        var mapping = new GroupRoleMapping
        {
            ProviderType = req.ProviderType,
            ExternalGroupRef = req.ExternalGroupRef,
            RoleId = req.RoleId,
            Description = req.Description,
            IsActive = true
        };

        _dbContext.GroupRoleMappings.Add(mapping);
        await _dbContext.SaveChangesAsync(ct);

        await _audit.LogAsync("Created", "GroupRoleMapping", mapping.Id.ToString(),
            userId: GetUserId(),
            newValueJson: $"{{\"providerType\":\"{mapping.ProviderType}\",\"group\":\"{mapping.ExternalGroupRef}\",\"roleId\":{mapping.RoleId}}}",
            ipAddress: HttpContext.Connection.RemoteIpAddress?.ToString(),
            userAgent: HttpContext.Request.Headers.UserAgent.ToString(), ct: ct);

        return CreatedAtAction(nameof(GetAsync), new { id = mapping.Id },
            new GroupRoleMappingDto(mapping.Id, mapping.ProviderType.ToString(), mapping.ExternalGroupRef,
                mapping.RoleId, role.Name, mapping.Description, mapping.IsActive));
    }

    /// <summary>Update a group-role mapping. [Admin]</summary>
    /// <response code="200">Mapping updated.</response>
    /// <response code="400">Validation error.</response>
    /// <response code="403">Insufficient role.</response>
    /// <response code="404">Mapping or role not found.</response>
    [HttpPut("{id:int}")]
    [ProducesResponseType(typeof(GroupRoleMappingDto), 200)]
    [ProducesResponseType(400)]
    [ProducesResponseType(403)]
    [ProducesResponseType(404)]
    public async Task<IActionResult> UpdateAsync(int id, [FromBody] UpdateGroupRoleMappingRequest req, CancellationToken ct = default)
    {
        if (!User.IsInRole(RoleNames.Admin))
        {
            return ForbiddenProblem();
        }

        var mapping = await _dbContext.GroupRoleMappings
            .Include(m => m.Role)
            .Where(m => m.Id == id)
            .FirstOrDefaultAsync(ct);

        if (mapping == null)
        {
            return NotFoundProblem(id, "GroupRoleMapping");
        }

        mapping.Description = req.Description;
        mapping.IsActive = req.IsActive;
        await _dbContext.SaveChangesAsync(ct);

        await _audit.LogAsync("Updated", "GroupRoleMapping", mapping.Id.ToString(),
            userId: GetUserId(),
            newValueJson: $"{{\"description\":\"{mapping.Description}\",\"isActive\":{mapping.IsActive.ToString().ToLower()}}}",
            ipAddress: HttpContext.Connection.RemoteIpAddress?.ToString(),
            userAgent: HttpContext.Request.Headers.UserAgent.ToString(), ct: ct);

        return Ok(new GroupRoleMappingDto(
            mapping.Id, mapping.ProviderType.ToString(), mapping.ExternalGroupRef,
            mapping.RoleId, mapping.Role.Name, mapping.Description, mapping.IsActive));
    }

    /// <summary>Delete a group-role mapping. [Admin]</summary>
    /// <response code="204">Mapping deleted.</response>
    /// <response code="403">Insufficient role.</response>
    /// <response code="404">Mapping not found.</response>
    [HttpDelete("{id:int}")]
    [ProducesResponseType(204)]
    [ProducesResponseType(403)]
    [ProducesResponseType(404)]
    public async Task<IActionResult> DeleteAsync(int id, CancellationToken ct = default)
    {
        if (!User.IsInRole(RoleNames.Admin))
        {
            return ForbiddenProblem();
        }

        var mapping = await _dbContext.GroupRoleMappings.FindAsync([id], ct);
        if (mapping == null)
        {
            return NotFoundProblem(id, "GroupRoleMapping");
        }

        _dbContext.GroupRoleMappings.Remove(mapping);
        await _dbContext.SaveChangesAsync(ct);

        await _audit.LogAsync("Deleted", "GroupRoleMapping", id.ToString(),
            userId: GetUserId(),
            ipAddress: HttpContext.Connection.RemoteIpAddress?.ToString(),
            userAgent: HttpContext.Request.Headers.UserAgent.ToString(), ct: ct);

        return NoContent();
    }

    /// <summary>List available roles. [Admin]</summary>
    /// <response code="200">List of roles.</response>
    /// <response code="403">Insufficient role.</response>
    [HttpGet("/api/v1/admin/roles")]
    [ProducesResponseType(typeof(IEnumerable<RoleDto>), 200)]
    [ProducesResponseType(403)]
    public async Task<IActionResult> ListRolesAsync(CancellationToken ct = default)
    {
        if (!User.IsInRole(RoleNames.Admin))
        {
            return ForbiddenProblem();
        }

        var roles = await _dbContext.Roles
            .OrderBy(r => r.Name)
            .Select(r => new RoleDto(r.Id, r.Name, r.Description, r.IsSystemRole))
            .ToListAsync(ct);

        return Ok(roles);
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

    private static IActionResult ConflictProblem(string detail)
    {
        return new ObjectResult(new ProblemDetails
        {
            Type = "https://tools.ietf.org/html/rfc7807",
            Title = "Conflict",
            Status = 409,
            Detail = detail
        })
        {
            StatusCode = 409,
            ContentTypes = { "application/problem+json" }
        };
    }
}

// --- DTOs ---

/// <summary>Group-role mapping entry.</summary>
public record GroupRoleMappingDto(int Id, string ProviderType, string ExternalGroupRef, int RoleId, string RoleName, string? Description, bool IsActive);

/// <summary>Role summary.</summary>
public record RoleDto(int Id, string Name, string? Description, bool IsSystemRole);

/// <summary>Create group-role mapping request.</summary>
public record CreateGroupRoleMappingRequest(AuthProviderType ProviderType, string ExternalGroupRef, int RoleId, string? Description);

/// <summary>Update group-role mapping request.</summary>
public record UpdateGroupRoleMappingRequest(string? Description, bool IsActive);
