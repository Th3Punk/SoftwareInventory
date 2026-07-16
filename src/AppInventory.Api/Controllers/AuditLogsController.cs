using AppInventory.Api.Authorization;
using AppInventory.Api.Middleware;
using AppInventory.Core.Authorization;
using AppInventory.Infrastructure.Data;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace AppInventory.Api.Controllers;

/// <summary>
/// Audit log viewer. [Admin]
/// </summary>
[ApiController]
[Route("api/v1/admin/audit-logs")]
[FeatureGate("Admin")]
[Authorize]
[RbacAuthorize]
public class AuditLogsController : ControllerBase
{
    private readonly AppInventoryDbContext _dbContext;

    public AuditLogsController(AppInventoryDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    /// <summary>List audit log entries with filtering and pagination. [Admin]</summary>
    /// <response code="200">Paginated audit log entries.</response>
    /// <response code="403">Insufficient role.</response>
    [HttpGet]
    [ProducesResponseType(typeof(PagedResponse<AuditLogDto>), 200)]
    [ProducesResponseType(403)]
    public async Task<IActionResult> ListAsync(
        [FromQuery] int? userId = null,
        [FromQuery] string? resourceType = null,
        [FromQuery] string? resourceId = null,
        [FromQuery] string? action = null,
        [FromQuery] DateTime? from = null,
        [FromQuery] DateTime? to = null,
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

        var query = _dbContext.AuditLogs.AsQueryable();

        if (userId.HasValue)
        {
            query = query.Where(l => l.UserId == userId.Value);
        }

        if (!string.IsNullOrWhiteSpace(resourceType))
        {
            query = query.Where(l => l.ResourceType == resourceType);
        }

        if (!string.IsNullOrWhiteSpace(resourceId))
        {
            query = query.Where(l => l.ResourceId == resourceId);
        }

        if (!string.IsNullOrWhiteSpace(action))
        {
            query = query.Where(l => l.Action == action);
        }

        if (from.HasValue)
        {
            query = query.Where(l => l.Timestamp >= from.Value.ToUniversalTime());
        }

        if (to.HasValue)
        {
            query = query.Where(l => l.Timestamp <= to.Value.ToUniversalTime());
        }

        var total = await query.CountAsync(ct);

        var items = await query
            .OrderByDescending(l => l.Timestamp)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .Select(l => new AuditLogDto(
                l.Id, l.UserId, l.Action, l.ResourceType, l.ResourceId,
                l.OldValueJson, l.NewValueJson, l.IpAddress, l.Timestamp))
            .ToListAsync(ct);

        return Ok(new PagedResponse<AuditLogDto>(items, total, page, pageSize));
    }

    /// <summary>Get a single audit log entry. [Admin]</summary>
    /// <response code="200">Audit log entry detail.</response>
    /// <response code="403">Insufficient role.</response>
    /// <response code="404">Entry not found.</response>
    [HttpGet("{id:long}")]
    [ProducesResponseType(typeof(AuditLogDetailDto), 200)]
    [ProducesResponseType(403)]
    [ProducesResponseType(404)]
    public async Task<IActionResult> GetAsync(long id, CancellationToken ct = default)
    {
        if (!User.IsInRole(RoleNames.Admin))
        {
            return ForbiddenProblem();
        }

        var log = await _dbContext.AuditLogs
            .Where(l => l.Id == id)
            .FirstOrDefaultAsync(ct);

        if (log == null)
        {
            return new ObjectResult(new ProblemDetails
            {
                Type = "https://tools.ietf.org/html/rfc7231#section-6.5.4",
                Title = "Not Found",
                Status = 404,
                Detail = $"AuditLog with id {id} was not found."
            })
            {
                StatusCode = 404,
                ContentTypes = { "application/problem+json" }
            };
        }

        return Ok(new AuditLogDetailDto(
            log.Id, log.UserId, log.Action, log.ResourceType, log.ResourceId,
            log.OldValueJson, log.NewValueJson, log.IpAddress, log.UserAgent, log.Timestamp));
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
}

// --- DTOs ---

/// <summary>Audit log list entry.</summary>
public record AuditLogDto(long Id, int? UserId, string Action, string ResourceType, string ResourceId, string? OldValueJson, string? NewValueJson, string? IpAddress, DateTime Timestamp);

/// <summary>Audit log entry with all fields.</summary>
public record AuditLogDetailDto(long Id, int? UserId, string Action, string ResourceType, string ResourceId, string? OldValueJson, string? NewValueJson, string? IpAddress, string? UserAgent, DateTime Timestamp);
