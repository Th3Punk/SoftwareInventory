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
/// Documentation CRUD endpoints for an application.
/// </summary>
[ApiController]
[Route("api/v1/applications/{appId:int}/docs")]
[FeatureGate("Documentation")]
[Authorize]
[RbacAuthorize]
public class DocumentationsController : ControllerBase
{
    private const int MaxContentBytes = 512_000;

    private readonly AppInventoryDbContext _dbContext;
    private readonly IAuditProvider _audit;

    public DocumentationsController(AppInventoryDbContext dbContext, IAuditProvider audit)
    {
        _dbContext = dbContext;
        _audit = audit;
    }

    /// <summary>List documentation for an application (filtered by caller role).</summary>
    /// <response code="200">List of documentation items.</response>
    /// <response code="404">Application not found.</response>
    [HttpGet]
    [ProducesResponseType(typeof(IEnumerable<DocumentationListItemDto>), 200)]
    [ProducesResponseType(404)]
    public async Task<IActionResult> ListAsync(int appId, [FromQuery] DocumentationType? type = null, CancellationToken ct = default)
    {
        var appExists = await _dbContext.Applications.AnyAsync(a => a.Id == appId, ct);
        if (!appExists)
        {
            return NotFoundProblem(appId);
        }

        var query = _dbContext.Documentations
            .Where(d => d.ApplicationId == appId && d.Status != DocumentationStatus.Archived)
            .AsQueryable();

        query = ApplyTypeVisibility(query, User);

        if (type.HasValue)
        {
            query = query.Where(d => d.Type == type.Value);
        }

        var items = await query
            .OrderBy(d => d.Type)
            .ThenBy(d => d.Title)
            .Select(d => new DocumentationListItemDto(d.Id, d.Title, d.Type.ToString(), d.Status.ToString(), d.Version, d.UpdatedAt))
            .ToListAsync(ct);

        return Ok(items);
    }

    /// <summary>Get a single documentation entry.</summary>
    /// <response code="200">Documentation content.</response>
    /// <response code="403">Access denied for this documentation type.</response>
    /// <response code="404">Application or documentation not found.</response>
    [HttpGet("{docId:int}")]
    [ProducesResponseType(typeof(DocumentationDetailDto), 200)]
    [ProducesResponseType(403)]
    [ProducesResponseType(404)]
    public async Task<IActionResult> GetAsync(int appId, int docId, CancellationToken ct = default)
    {
        var doc = await _dbContext.Documentations
            .Where(d => d.ApplicationId == appId && d.Id == docId)
            .Include(d => d.Author)
            .FirstOrDefaultAsync(ct);

        if (doc == null)
        {
            return NotFoundProblem(docId, "Documentation");
        }

        if (!CanReadType(doc.Type, User))
        {
            return ForbiddenProblem();
        }

        return Ok(new DocumentationDetailDto(
            doc.Id, doc.Title, doc.Content, doc.Type.ToString(),
            doc.Status.ToString(), doc.Version, doc.CreatedAt, doc.UpdatedAt,
            doc.Author?.DisplayName));
    }

    /// <summary>Create a new documentation entry. [Developer, Admin]</summary>
    /// <response code="201">Documentation created.</response>
    /// <response code="400">Validation error.</response>
    /// <response code="403">Insufficient role.</response>
    /// <response code="404">Application not found.</response>
    [HttpPost]
    [ProducesResponseType(typeof(DocumentationDetailDto), 201)]
    [ProducesResponseType(400)]
    [ProducesResponseType(403)]
    [ProducesResponseType(404)]
    public async Task<IActionResult> CreateAsync(int appId, [FromBody] CreateDocumentationRequest req, CancellationToken ct = default)
    {
        if (!User.IsInRole(RoleNames.Developer) && !User.IsInRole(RoleNames.Admin))
        {
            return ForbiddenProblem();
        }

        var appExists = await _dbContext.Applications.AnyAsync(a => a.Id == appId, ct);
        if (!appExists)
        {
            return NotFoundProblem(appId);
        }

        if (System.Text.Encoding.UTF8.GetByteCount(req.Content) > MaxContentBytes)
        {
            return BadRequestProblem("Content exceeds the maximum allowed size of 500 KB.");
        }

        var now = DateTime.UtcNow;
        var authorId = GetUserId();

        var doc = new Documentation
        {
            ApplicationId = appId,
            Title = req.Title,
            Content = req.Content,
            Type = req.Type,
            Status = DocumentationStatus.Draft,
            Version = 1,
            CreatedAt = now,
            UpdatedAt = now,
            AuthorUserId = authorId
        };

        _dbContext.Documentations.Add(doc);
        await _dbContext.SaveChangesAsync(ct);

        await _audit.LogAsync("Created", "Documentation", doc.Id.ToString(),
            userId: authorId,
            newValueJson: $"{{\"title\":\"{doc.Title}\",\"type\":\"{doc.Type}\"}}",
            ipAddress: HttpContext.Connection.RemoteIpAddress?.ToString(),
            userAgent: HttpContext.Request.Headers.UserAgent.ToString(), ct: ct);

        var dto = new DocumentationDetailDto(
            doc.Id, doc.Title, doc.Content, doc.Type.ToString(),
            doc.Status.ToString(), doc.Version, doc.CreatedAt, doc.UpdatedAt,
            null);

        return CreatedAtAction(nameof(GetAsync), new { appId, docId = doc.Id }, dto);
    }

    /// <summary>Update documentation content. Previous version is archived. [Developer, Admin]</summary>
    /// <response code="200">Updated documentation.</response>
    /// <response code="400">Validation error.</response>
    /// <response code="403">Insufficient role.</response>
    /// <response code="404">Documentation not found.</response>
    [HttpPut("{docId:int}")]
    [ProducesResponseType(typeof(DocumentationDetailDto), 200)]
    [ProducesResponseType(400)]
    [ProducesResponseType(403)]
    [ProducesResponseType(404)]
    public async Task<IActionResult> UpdateAsync(int appId, int docId, [FromBody] UpdateDocumentationRequest req, CancellationToken ct = default)
    {
        if (!User.IsInRole(RoleNames.Developer) && !User.IsInRole(RoleNames.Admin))
        {
            return ForbiddenProblem();
        }

        var doc = await _dbContext.Documentations
            .Where(d => d.ApplicationId == appId && d.Id == docId)
            .FirstOrDefaultAsync(ct);

        if (doc == null)
        {
            return NotFoundProblem(docId, "Documentation");
        }

        if (System.Text.Encoding.UTF8.GetByteCount(req.Content) > MaxContentBytes)
        {
            return BadRequestProblem("Content exceeds the maximum allowed size of 500 KB.");
        }

        var history = new DocumentationHistory
        {
            DocumentationId = doc.Id,
            Content = doc.Content,
            Version = doc.Version,
            ArchivedAt = DateTime.UtcNow,
            ArchivedByUserId = GetUserId()
        };
        _dbContext.DocumentationHistories.Add(history);

        doc.Title = req.Title;
        doc.Content = req.Content;
        doc.Type = req.Type;
        doc.Version++;
        doc.UpdatedAt = DateTime.UtcNow;

        await _dbContext.SaveChangesAsync(ct);

        await _audit.LogAsync("Updated", "Documentation", doc.Id.ToString(),
            userId: GetUserId(),
            newValueJson: $"{{\"title\":\"{doc.Title}\",\"version\":{doc.Version}}}",
            ipAddress: HttpContext.Connection.RemoteIpAddress?.ToString(),
            userAgent: HttpContext.Request.Headers.UserAgent.ToString(), ct: ct);

        return Ok(new DocumentationDetailDto(
            doc.Id, doc.Title, doc.Content, doc.Type.ToString(),
            doc.Status.ToString(), doc.Version, doc.CreatedAt, doc.UpdatedAt,
            null));
    }

    /// <summary>Change documentation status. [Developer, Admin]</summary>
    /// <response code="200">Status updated.</response>
    /// <response code="400">Invalid status.</response>
    /// <response code="403">Insufficient role.</response>
    /// <response code="404">Documentation not found.</response>
    [HttpPatch("{docId:int}/status")]
    [ProducesResponseType(200)]
    [ProducesResponseType(400)]
    [ProducesResponseType(403)]
    [ProducesResponseType(404)]
    public async Task<IActionResult> PatchStatusAsync(int appId, int docId, [FromBody] PatchStatusRequest req, CancellationToken ct = default)
    {
        if (!User.IsInRole(RoleNames.Developer) && !User.IsInRole(RoleNames.Admin))
        {
            return ForbiddenProblem();
        }

        var doc = await _dbContext.Documentations
            .Where(d => d.ApplicationId == appId && d.Id == docId)
            .FirstOrDefaultAsync(ct);

        if (doc == null)
        {
            return NotFoundProblem(docId, "Documentation");
        }

        doc.Status = req.Status;
        doc.UpdatedAt = DateTime.UtcNow;
        await _dbContext.SaveChangesAsync(ct);

        await _audit.LogAsync("StatusChanged", "Documentation", doc.Id.ToString(),
            userId: GetUserId(),
            newValueJson: $"{{\"status\":\"{doc.Status}\"}}",
            ipAddress: HttpContext.Connection.RemoteIpAddress?.ToString(),
            userAgent: HttpContext.Request.Headers.UserAgent.ToString(), ct: ct);

        return Ok();
    }

    /// <summary>Archive (soft-delete) a documentation entry. [Admin]</summary>
    /// <response code="204">Documentation archived.</response>
    /// <response code="403">Insufficient role.</response>
    /// <response code="404">Documentation not found.</response>
    [HttpDelete("{docId:int}")]
    [ProducesResponseType(204)]
    [ProducesResponseType(403)]
    [ProducesResponseType(404)]
    public async Task<IActionResult> DeleteAsync(int appId, int docId, CancellationToken ct = default)
    {
        if (!User.IsInRole(RoleNames.Admin))
        {
            return ForbiddenProblem();
        }

        var doc = await _dbContext.Documentations
            .Where(d => d.ApplicationId == appId && d.Id == docId)
            .FirstOrDefaultAsync(ct);

        if (doc == null)
        {
            return NotFoundProblem(docId, "Documentation");
        }

        doc.Status = DocumentationStatus.Archived;
        doc.UpdatedAt = DateTime.UtcNow;
        await _dbContext.SaveChangesAsync(ct);

        await _audit.LogAsync("Archived", "Documentation", doc.Id.ToString(),
            userId: GetUserId(),
            ipAddress: HttpContext.Connection.RemoteIpAddress?.ToString(),
            userAgent: HttpContext.Request.Headers.UserAgent.ToString(), ct: ct);

        return NoContent();
    }

    /// <summary>List version history for a documentation entry. [Developer, Admin]</summary>
    /// <response code="200">List of history entries.</response>
    /// <response code="403">Insufficient role.</response>
    /// <response code="404">Documentation not found.</response>
    [HttpGet("{docId:int}/history")]
    [ProducesResponseType(typeof(IEnumerable<DocumentationHistoryDto>), 200)]
    [ProducesResponseType(403)]
    [ProducesResponseType(404)]
    public async Task<IActionResult> GetHistoryAsync(int appId, int docId, CancellationToken ct = default)
    {
        if (!User.IsInRole(RoleNames.Developer) && !User.IsInRole(RoleNames.Admin))
        {
            return ForbiddenProblem();
        }

        var docExists = await _dbContext.Documentations
            .AnyAsync(d => d.ApplicationId == appId && d.Id == docId, ct);

        if (!docExists)
        {
            return NotFoundProblem(docId, "Documentation");
        }

        var items = await _dbContext.DocumentationHistories
            .Where(h => h.DocumentationId == docId)
            .OrderByDescending(h => h.Version)
            .Select(h => new DocumentationHistoryDto(h.Id, h.Version, h.ArchivedAt))
            .ToListAsync(ct);

        return Ok(items);
    }

    /// <summary>Get a specific historical version. [Developer, Admin]</summary>
    /// <response code="200">Historical version content.</response>
    /// <response code="403">Insufficient role.</response>
    /// <response code="404">Documentation or version not found.</response>
    [HttpGet("{docId:int}/history/{version:int}")]
    [ProducesResponseType(typeof(DocumentationHistoryDetailDto), 200)]
    [ProducesResponseType(403)]
    [ProducesResponseType(404)]
    public async Task<IActionResult> GetHistoryVersionAsync(int appId, int docId, int version, CancellationToken ct = default)
    {
        if (!User.IsInRole(RoleNames.Developer) && !User.IsInRole(RoleNames.Admin))
        {
            return ForbiddenProblem();
        }

        var history = await _dbContext.DocumentationHistories
            .Include(h => h.Documentation)
            .Where(h => h.DocumentationId == docId && h.Version == version && h.Documentation.ApplicationId == appId)
            .FirstOrDefaultAsync(ct);

        if (history == null)
        {
            return NotFoundProblem(version, "DocumentationHistory");
        }

        return Ok(new DocumentationHistoryDetailDto(history.Id, history.Version, history.Content, history.ArchivedAt));
    }

    // --- Helpers ---

    private static IQueryable<Documentation> ApplyTypeVisibility(IQueryable<Documentation> query, ClaimsPrincipal user)
    {
        if (user.IsInRole(RoleNames.Admin))
        {
            return query;
        }

        if (user.IsInRole(RoleNames.Developer))
        {
            return query.Where(d => d.Type == DocumentationType.User || d.Type == DocumentationType.Developer);
        }

        return query.Where(d => d.Type == DocumentationType.User);
    }

    private static bool CanReadType(DocumentationType type, ClaimsPrincipal user)
    {
        if (user.IsInRole(RoleNames.Admin))
        {
            return true;
        }

        if (user.IsInRole(RoleNames.Developer))
        {
            return type == DocumentationType.User || type == DocumentationType.Developer;
        }

        return type == DocumentationType.User;
    }

    private int? GetUserId()
    {
        var idClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        return int.TryParse(idClaim, out var id) ? id : null;
    }

    private static IActionResult NotFoundProblem(int id, string resource = "Application")
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

/// <summary>Documentation list item.</summary>
public record DocumentationListItemDto(int Id, string Title, string Type, string Status, int Version, DateTime UpdatedAt);

/// <summary>Full documentation detail.</summary>
public record DocumentationDetailDto(int Id, string Title, string Content, string Type, string Status, int Version, DateTime CreatedAt, DateTime UpdatedAt, string? AuthorName);

/// <summary>History entry summary.</summary>
public record DocumentationHistoryDto(int Id, int Version, DateTime ArchivedAt);

/// <summary>History entry with content.</summary>
public record DocumentationHistoryDetailDto(int Id, int Version, string Content, DateTime ArchivedAt);

/// <summary>Create documentation request.</summary>
public record CreateDocumentationRequest(string Title, string Content, DocumentationType Type);

/// <summary>Update documentation request.</summary>
public record UpdateDocumentationRequest(string Title, string Content, DocumentationType Type);

/// <summary>Patch status request.</summary>
public record PatchStatusRequest(DocumentationStatus Status);
