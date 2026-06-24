using System.Security.Claims;
using AppInventory.Api.Authorization;
using AppInventory.Api.Middleware;
using AppInventory.Core.Authorization;
using AppInventory.Core.Entities;
using AppInventory.Infrastructure.Authorization;
using AppInventory.Infrastructure.Data;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace AppInventory.Api.Controllers;

/// <summary>
/// Application catalog CRUD endpoints.
/// </summary>
[ApiController]
[Route("api/v1/applications")]
[FeatureGate("ApplicationCatalog")]
[Authorize]
[RbacAuthorize]
public class ApplicationsController : ControllerBase
{
    private readonly AppInventoryDbContext _dbContext;
    private readonly IAuthorizationService _authorizationService;

    public ApplicationsController(AppInventoryDbContext dbContext, IAuthorizationService authorizationService)
    {
        _dbContext = dbContext;
        _authorizationService = authorizationService;
    }

    /// <summary>
    /// List applications with filtering, sorting, and pagination.
    /// </summary>
    /// <response code="200">Paginated list of applications.</response>
    [HttpGet]
    [ProducesResponseType(typeof(PagedResponse<ApplicationListItemDto>), 200)]
    public async Task<IActionResult> ListAsync(
        [FromQuery] ApplicationStatus? status,
        [FromQuery] ApplicationType? type,
        [FromQuery] string? team,
        [FromQuery(Name = "tag")] string[]? tags,
        [FromQuery] string? q,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20,
        [FromQuery] string? sort = null,
        CancellationToken ct = default)
    {
        if (page < 1) page = 1;
        if (pageSize < 1) pageSize = 1;
        if (pageSize > 100) pageSize = 100;

        var query = _dbContext.Applications
            .Include(a => a.Tags).ThenInclude(at => at.Tag)
            .AsQueryable();

        if (status.HasValue)
            query = query.Where(a => a.Status == status.Value);

        if (type.HasValue)
            query = query.Where(a => a.Type == type.Value);

        if (!string.IsNullOrWhiteSpace(team))
            query = query.Where(a => a.OwnerTeam.Contains(team));

        if (tags is { Length: > 0 })
        {
            foreach (var tag in tags)
            {
                query = query.Where(a => a.Tags.Any(at => at.Tag.Name == tag.ToLowerInvariant()));
            }
        }

        if (!string.IsNullOrWhiteSpace(q))
        {
            var term = q.ToLowerInvariant();
            query = query.Where(a =>
                a.Name.ToLower().Contains(term) ||
                a.ShortDescription.ToLower().Contains(term) ||
                a.OwnerTeam.ToLower().Contains(term));
        }

        query = ApplySort(query, sort);

        var totalCount = await query.CountAsync(ct);
        var items = await query
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .Select(a => new ApplicationListItemDto(
                a.Id,
                a.Name,
                a.ShortDescription,
                a.Status.ToString(),
                a.Type.ToString(),
                a.OwnerTeam,
                a.Tags.Select(t => t.Tag.Name).ToList(),
                a.CreatedAt,
                a.UpdatedAt))
            .ToListAsync(ct);

        return Ok(new PagedResponse<ApplicationListItemDto>(items, totalCount, page, pageSize));
    }

    /// <summary>
    /// Get application details by ID.
    /// </summary>
    /// <response code="200">Application details.</response>
    /// <response code="404">Application not found.</response>
    [HttpGet("{id:int}")]
    [ProducesResponseType(typeof(ApplicationDetailDto), 200)]
    [ProducesResponseType(typeof(ProblemDetails), 404)]
    public async Task<IActionResult> GetAsync(int id, CancellationToken ct)
    {
        var app = await _dbContext.Applications
            .Include(a => a.Tags).ThenInclude(at => at.Tag)
            .Include(a => a.Contacts)
            .Include(a => a.Environments)
            .Include(a => a.CreatedBy)
            .FirstOrDefaultAsync(a => a.Id == id, ct);

        if (app is null)
            return NotFoundProblem(id);

        var canViewNonPublic = QueryAuthorizationFilter.CanViewNonPublicEnvironments(User);

        var environments = app.Environments
            .Where(e => e.IsPublic || canViewNonPublic)
            .Select(e => new EnvironmentDto(e.Id, e.Type.ToString(), e.Url, e.Notes, e.IsPublic))
            .ToList();

        var contacts = app.Contacts
            .Select(c => new ContactDto(c.Id, c.Type.ToString(), c.Value, c.Label))
            .ToList();

        return Ok(new ApplicationDetailDto(
            app.Id,
            app.Name,
            app.ShortDescription,
            app.DetailedDescription,
            app.Status.ToString(),
            app.Type.ToString(),
            app.OwnerTeam,
            app.SourceControl.ToString(),
            app.RepositoryUrl,
            app.WikiUrl,
            app.Tags.Select(t => t.Tag.Name).ToList(),
            environments,
            contacts,
            app.CreatedByUserId,
            app.CreatedBy?.DisplayName,
            app.CreatedAt,
            app.UpdatedAt));
    }

    /// <summary>
    /// Create a new application.
    /// </summary>
    /// <response code="201">Application created.</response>
    /// <response code="400">Validation error.</response>
    /// <response code="409">Application name already exists.</response>
    [HttpPost]
    [RbacAuthorize(RoleNames.Developer, RoleNames.Admin)]
    [ProducesResponseType(typeof(ApplicationDetailDto), 201)]
    [ProducesResponseType(typeof(ProblemDetails), 400)]
    [ProducesResponseType(typeof(ProblemDetails), 409)]
    public async Task<IActionResult> CreateAsync([FromBody] CreateApplicationRequest request, CancellationToken ct)
    {
        var validationError = ValidateApplication(request.SourceControl, request.RepositoryUrl, request.WikiUrl);
        if (validationError is not null)
            return validationError;

        var exists = await _dbContext.Applications.IgnoreQueryFilters()
            .AnyAsync(a => a.Name == request.Name, ct);
        if (exists)
            return ConflictProblem(request.Name);

        var userId = QueryAuthorizationFilter.GetUserId(User);
        var now = DateTime.UtcNow;

        var app = new Application
        {
            Name = request.Name,
            ShortDescription = request.ShortDescription,
            DetailedDescription = request.DetailedDescription,
            Status = request.Status,
            Type = request.Type,
            OwnerTeam = request.OwnerTeam,
            SourceControl = request.SourceControl,
            RepositoryUrl = request.RepositoryUrl,
            WikiUrl = request.WikiUrl,
            CreatedByUserId = userId,
            CreatedAt = now,
            UpdatedAt = now
        };

        if (request.TagIds is { Count: > 0 })
        {
            var validTagIds = await _dbContext.Tags
                .Where(t => request.TagIds.Contains(t.Id))
                .Select(t => t.Id)
                .ToListAsync(ct);

            foreach (var tagId in validTagIds)
            {
                app.Tags.Add(new ApplicationTag { TagId = tagId });
            }
        }

        _dbContext.Applications.Add(app);
        await _dbContext.SaveChangesAsync(ct);

        return CreatedAtAction(nameof(GetAsync), new { id = app.Id }, await BuildDetailDto(app.Id, ct));
    }

    /// <summary>
    /// Update an existing application.
    /// </summary>
    /// <response code="200">Application updated.</response>
    /// <response code="400">Validation error.</response>
    /// <response code="403">Insufficient permissions.</response>
    /// <response code="404">Application not found.</response>
    /// <response code="409">Application name already exists.</response>
    [HttpPut("{id:int}")]
    [RbacAuthorize(RoleNames.ApplicationOwner, RoleNames.Developer, RoleNames.Admin)]
    [ProducesResponseType(typeof(ApplicationDetailDto), 200)]
    [ProducesResponseType(typeof(ProblemDetails), 400)]
    [ProducesResponseType(typeof(ProblemDetails), 403)]
    [ProducesResponseType(typeof(ProblemDetails), 404)]
    [ProducesResponseType(typeof(ProblemDetails), 409)]
    public async Task<IActionResult> UpdateAsync(int id, [FromBody] UpdateApplicationRequest request, CancellationToken ct)
    {
        var validationError = ValidateApplication(request.SourceControl, request.RepositoryUrl, request.WikiUrl);
        if (validationError is not null)
            return validationError;

        var app = await _dbContext.Applications
            .Include(a => a.Tags)
            .FirstOrDefaultAsync(a => a.Id == id, ct);

        if (app is null)
            return NotFoundProblem(id);

        if (!QueryAuthorizationFilter.HasDeveloperAccess(User))
        {
            var authResult = await _authorizationService.AuthorizeAsync(
                User, app.CreatedByUserId ?? 0, new ResourceOwnerRequirement());
            if (!authResult.Succeeded)
            {
                return Problem(
                    statusCode: 403,
                    title: "Forbidden",
                    detail: "You can only edit applications you own.");
            }
        }

        var nameConflict = await _dbContext.Applications.IgnoreQueryFilters()
            .AnyAsync(a => a.Name == request.Name && a.Id != id, ct);
        if (nameConflict)
            return ConflictProblem(request.Name);

        app.Name = request.Name;
        app.ShortDescription = request.ShortDescription;
        app.DetailedDescription = request.DetailedDescription;
        app.Status = request.Status;
        app.Type = request.Type;
        app.OwnerTeam = request.OwnerTeam;
        app.SourceControl = request.SourceControl;
        app.RepositoryUrl = request.RepositoryUrl;
        app.WikiUrl = request.WikiUrl;
        app.UpdatedAt = DateTime.UtcNow;

        if (request.TagIds is not null)
        {
            app.Tags.Clear();
            var validTagIds = await _dbContext.Tags
                .Where(t => request.TagIds.Contains(t.Id))
                .Select(t => t.Id)
                .ToListAsync(ct);

            foreach (var tagId in validTagIds)
            {
                app.Tags.Add(new ApplicationTag { ApplicationId = id, TagId = tagId });
            }
        }

        await _dbContext.SaveChangesAsync(ct);

        return Ok(await BuildDetailDto(id, ct));
    }

    /// <summary>
    /// Soft-delete an application.
    /// </summary>
    /// <response code="204">Application deleted.</response>
    /// <response code="404">Application not found.</response>
    [HttpDelete("{id:int}")]
    [RbacAuthorize(RoleNames.Admin)]
    [ProducesResponseType(204)]
    [ProducesResponseType(typeof(ProblemDetails), 404)]
    public async Task<IActionResult> DeleteAsync(int id, CancellationToken ct)
    {
        var app = await _dbContext.Applications.FirstOrDefaultAsync(a => a.Id == id, ct);

        if (app is null)
            return NotFoundProblem(id);

        app.IsDeleted = true;
        app.UpdatedAt = DateTime.UtcNow;
        await _dbContext.SaveChangesAsync(ct);

        return NoContent();
    }

    // --- Environments ---

    /// <summary>
    /// List environments for an application.
    /// </summary>
    /// <response code="200">List of environments.</response>
    /// <response code="404">Application not found.</response>
    [HttpGet("{id:int}/environments")]
    [ProducesResponseType(typeof(List<EnvironmentDto>), 200)]
    [ProducesResponseType(typeof(ProblemDetails), 404)]
    public async Task<IActionResult> ListEnvironmentsAsync(int id, CancellationToken ct)
    {
        var appExists = await _dbContext.Applications.AnyAsync(a => a.Id == id, ct);
        if (!appExists)
            return NotFoundProblem(id);

        var canViewNonPublic = QueryAuthorizationFilter.CanViewNonPublicEnvironments(User);

        var environments = await _dbContext.ApplicationEnvironments
            .Where(e => e.ApplicationId == id)
            .Where(e => e.IsPublic || canViewNonPublic)
            .Select(e => new EnvironmentDto(e.Id, e.Type.ToString(), e.Url, e.Notes, e.IsPublic))
            .ToListAsync(ct);

        return Ok(environments);
    }

    /// <summary>
    /// Add an environment to an application.
    /// </summary>
    /// <response code="201">Environment created.</response>
    /// <response code="400">Invalid URL scheme.</response>
    /// <response code="404">Application not found.</response>
    [HttpPost("{id:int}/environments")]
    [RbacAuthorize(RoleNames.Developer, RoleNames.Admin)]
    [ProducesResponseType(typeof(EnvironmentDto), 201)]
    [ProducesResponseType(typeof(ProblemDetails), 400)]
    [ProducesResponseType(typeof(ProblemDetails), 404)]
    public async Task<IActionResult> CreateEnvironmentAsync(int id, [FromBody] CreateEnvironmentRequest request, CancellationToken ct)
    {
        var appExists = await _dbContext.Applications.AnyAsync(a => a.Id == id, ct);
        if (!appExists)
            return NotFoundProblem(id);

        if (!IsValidHttpUrl(request.Url))
            return InvalidUrlProblem(request.Url);

        var env = new ApplicationEnvironment
        {
            ApplicationId = id,
            Type = request.Type,
            Url = request.Url,
            Notes = request.Notes,
            IsPublic = request.IsPublic
        };

        _dbContext.ApplicationEnvironments.Add(env);
        await _dbContext.SaveChangesAsync(ct);

        var dto = new EnvironmentDto(env.Id, env.Type.ToString(), env.Url, env.Notes, env.IsPublic);
        return CreatedAtAction(nameof(ListEnvironmentsAsync), new { id }, dto);
    }

    /// <summary>
    /// Update an environment.
    /// </summary>
    /// <response code="200">Environment updated.</response>
    /// <response code="400">Invalid URL scheme.</response>
    /// <response code="404">Environment not found.</response>
    [HttpPut("{id:int}/environments/{eid:int}")]
    [RbacAuthorize(RoleNames.Developer, RoleNames.Admin)]
    [ProducesResponseType(typeof(EnvironmentDto), 200)]
    [ProducesResponseType(typeof(ProblemDetails), 400)]
    [ProducesResponseType(typeof(ProblemDetails), 404)]
    public async Task<IActionResult> UpdateEnvironmentAsync(int id, int eid, [FromBody] UpdateEnvironmentRequest request, CancellationToken ct)
    {
        var env = await _dbContext.ApplicationEnvironments
            .FirstOrDefaultAsync(e => e.Id == eid && e.ApplicationId == id, ct);

        if (env is null)
            return Problem(statusCode: 404, title: "Not Found", detail: $"Environment with id {eid} was not found for application {id}.");

        if (!IsValidHttpUrl(request.Url))
            return InvalidUrlProblem(request.Url);

        env.Type = request.Type;
        env.Url = request.Url;
        env.Notes = request.Notes;
        env.IsPublic = request.IsPublic;
        await _dbContext.SaveChangesAsync(ct);

        return Ok(new EnvironmentDto(env.Id, env.Type.ToString(), env.Url, env.Notes, env.IsPublic));
    }

    /// <summary>
    /// Delete an environment.
    /// </summary>
    /// <response code="204">Environment deleted.</response>
    /// <response code="404">Environment not found.</response>
    [HttpDelete("{id:int}/environments/{eid:int}")]
    [RbacAuthorize(RoleNames.Developer, RoleNames.Admin)]
    [ProducesResponseType(204)]
    [ProducesResponseType(typeof(ProblemDetails), 404)]
    public async Task<IActionResult> DeleteEnvironmentAsync(int id, int eid, CancellationToken ct)
    {
        var env = await _dbContext.ApplicationEnvironments
            .FirstOrDefaultAsync(e => e.Id == eid && e.ApplicationId == id, ct);

        if (env is null)
            return Problem(statusCode: 404, title: "Not Found", detail: $"Environment with id {eid} was not found for application {id}.");

        _dbContext.ApplicationEnvironments.Remove(env);
        await _dbContext.SaveChangesAsync(ct);

        return NoContent();
    }

    // --- Contacts ---

    /// <summary>
    /// List contacts for an application.
    /// </summary>
    /// <response code="200">List of contacts.</response>
    /// <response code="404">Application not found.</response>
    [HttpGet("{id:int}/contacts")]
    [ProducesResponseType(typeof(List<ContactDto>), 200)]
    [ProducesResponseType(typeof(ProblemDetails), 404)]
    public async Task<IActionResult> ListContactsAsync(int id, CancellationToken ct)
    {
        var appExists = await _dbContext.Applications.AnyAsync(a => a.Id == id, ct);
        if (!appExists)
            return NotFoundProblem(id);

        var contacts = await _dbContext.ApplicationContacts
            .Where(c => c.ApplicationId == id)
            .Select(c => new ContactDto(c.Id, c.Type.ToString(), c.Value, c.Label))
            .ToListAsync(ct);

        return Ok(contacts);
    }

    /// <summary>
    /// Add a contact to an application.
    /// </summary>
    /// <response code="201">Contact created.</response>
    /// <response code="404">Application not found.</response>
    [HttpPost("{id:int}/contacts")]
    [RbacAuthorize(RoleNames.Developer, RoleNames.Admin)]
    [ProducesResponseType(typeof(ContactDto), 201)]
    [ProducesResponseType(typeof(ProblemDetails), 404)]
    public async Task<IActionResult> CreateContactAsync(int id, [FromBody] CreateContactRequest request, CancellationToken ct)
    {
        var appExists = await _dbContext.Applications.AnyAsync(a => a.Id == id, ct);
        if (!appExists)
            return NotFoundProblem(id);

        var contact = new ApplicationContact
        {
            ApplicationId = id,
            Type = request.Type,
            Value = request.Value,
            Label = request.Label
        };

        _dbContext.ApplicationContacts.Add(contact);
        await _dbContext.SaveChangesAsync(ct);

        var dto = new ContactDto(contact.Id, contact.Type.ToString(), contact.Value, contact.Label);
        return CreatedAtAction(nameof(ListContactsAsync), new { id }, dto);
    }

    /// <summary>
    /// Update a contact.
    /// </summary>
    /// <response code="200">Contact updated.</response>
    /// <response code="404">Contact not found.</response>
    [HttpPut("{id:int}/contacts/{cid:int}")]
    [RbacAuthorize(RoleNames.Developer, RoleNames.Admin)]
    [ProducesResponseType(typeof(ContactDto), 200)]
    [ProducesResponseType(typeof(ProblemDetails), 404)]
    public async Task<IActionResult> UpdateContactAsync(int id, int cid, [FromBody] UpdateContactRequest request, CancellationToken ct)
    {
        var contact = await _dbContext.ApplicationContacts
            .FirstOrDefaultAsync(c => c.Id == cid && c.ApplicationId == id, ct);

        if (contact is null)
            return Problem(statusCode: 404, title: "Not Found", detail: $"Contact with id {cid} was not found for application {id}.");

        contact.Type = request.Type;
        contact.Value = request.Value;
        contact.Label = request.Label;
        await _dbContext.SaveChangesAsync(ct);

        return Ok(new ContactDto(contact.Id, contact.Type.ToString(), contact.Value, contact.Label));
    }

    /// <summary>
    /// Delete a contact.
    /// </summary>
    /// <response code="204">Contact deleted.</response>
    /// <response code="404">Contact not found.</response>
    [HttpDelete("{id:int}/contacts/{cid:int}")]
    [RbacAuthorize(RoleNames.Developer, RoleNames.Admin)]
    [ProducesResponseType(204)]
    [ProducesResponseType(typeof(ProblemDetails), 404)]
    public async Task<IActionResult> DeleteContactAsync(int id, int cid, CancellationToken ct)
    {
        var contact = await _dbContext.ApplicationContacts
            .FirstOrDefaultAsync(c => c.Id == cid && c.ApplicationId == id, ct);

        if (contact is null)
            return Problem(statusCode: 404, title: "Not Found", detail: $"Contact with id {cid} was not found for application {id}.");

        _dbContext.ApplicationContacts.Remove(contact);
        await _dbContext.SaveChangesAsync(ct);

        return NoContent();
    }

    // --- Helpers ---

    private async Task<ApplicationDetailDto> BuildDetailDto(int id, CancellationToken ct)
    {
        var app = await _dbContext.Applications
            .Include(a => a.Tags).ThenInclude(at => at.Tag)
            .Include(a => a.Contacts)
            .Include(a => a.Environments)
            .Include(a => a.CreatedBy)
            .FirstAsync(a => a.Id == id, ct);

        var canViewNonPublic = QueryAuthorizationFilter.CanViewNonPublicEnvironments(User);

        return new ApplicationDetailDto(
            app.Id,
            app.Name,
            app.ShortDescription,
            app.DetailedDescription,
            app.Status.ToString(),
            app.Type.ToString(),
            app.OwnerTeam,
            app.SourceControl.ToString(),
            app.RepositoryUrl,
            app.WikiUrl,
            app.Tags.Select(t => t.Tag.Name).ToList(),
            app.Environments
                .Where(e => e.IsPublic || canViewNonPublic)
                .Select(e => new EnvironmentDto(e.Id, e.Type.ToString(), e.Url, e.Notes, e.IsPublic))
                .ToList(),
            app.Contacts
                .Select(c => new ContactDto(c.Id, c.Type.ToString(), c.Value, c.Label))
                .ToList(),
            app.CreatedByUserId,
            app.CreatedBy?.DisplayName,
            app.CreatedAt,
            app.UpdatedAt);
    }

    private static IQueryable<Application> ApplySort(IQueryable<Application> query, string? sort)
    {
        return sort switch
        {
            "name" => query.OrderBy(a => a.Name),
            "-name" => query.OrderByDescending(a => a.Name),
            "createdAt" => query.OrderBy(a => a.CreatedAt),
            "-createdAt" => query.OrderByDescending(a => a.CreatedAt),
            "updatedAt" => query.OrderBy(a => a.UpdatedAt),
            "-updatedAt" => query.OrderByDescending(a => a.UpdatedAt),
            _ => query.OrderBy(a => a.Name)
        };
    }

    private static IActionResult? ValidateApplication(SourceControlType sourceControl, string? repositoryUrl, string? wikiUrl)
    {
        if (sourceControl is not SourceControlType.None && string.IsNullOrWhiteSpace(repositoryUrl))
        {
            return new ObjectResult(new ProblemDetails
            {
                Type = "https://tools.ietf.org/html/rfc7807",
                Title = "Bad Request",
                Status = 400,
                Detail = "RepositoryUrl is required when SourceControl is not None."
            }) { StatusCode = 400, ContentTypes = { "application/problem+json" } };
        }

        if (!string.IsNullOrWhiteSpace(repositoryUrl) && !IsValidHttpUrl(repositoryUrl))
        {
            return InvalidUrlProblem(repositoryUrl);
        }

        if (!string.IsNullOrWhiteSpace(wikiUrl) && !IsValidHttpUrl(wikiUrl))
        {
            return InvalidUrlProblem(wikiUrl);
        }

        return null;
    }

    private static bool IsValidHttpUrl(string url)
    {
        return Uri.TryCreate(url, UriKind.Absolute, out var uri)
            && uri.Scheme is "http" or "https";
    }

    private static IActionResult InvalidUrlProblem(string url)
    {
        return new ObjectResult(new ProblemDetails
        {
            Type = "https://tools.ietf.org/html/rfc7807",
            Title = "Bad Request",
            Status = 400,
            Detail = $"URL '{url}' has an invalid scheme. Only http and https are allowed."
        }) { StatusCode = 400, ContentTypes = { "application/problem+json" } };
    }

    private static IActionResult NotFoundProblem(int id)
    {
        return new ObjectResult(new ProblemDetails
        {
            Type = "https://tools.ietf.org/html/rfc7231#section-6.5.4",
            Title = "Not Found",
            Status = 404,
            Detail = $"Application with id {id} was not found."
        }) { StatusCode = 404, ContentTypes = { "application/problem+json" } };
    }

    private static IActionResult ConflictProblem(string name)
    {
        return new ObjectResult(new ProblemDetails
        {
            Type = "https://tools.ietf.org/html/rfc7807",
            Title = "Conflict",
            Status = 409,
            Detail = $"An application with the name '{name}' already exists."
        }) { StatusCode = 409, ContentTypes = { "application/problem+json" } };
    }
}

// --- DTOs ---

/// <summary>Paginated response wrapper.</summary>
public record PagedResponse<T>(List<T> Items, int TotalCount, int Page, int PageSize);

/// <summary>Application list item.</summary>
public record ApplicationListItemDto(
    int Id,
    string Name,
    string ShortDescription,
    string Status,
    string Type,
    string OwnerTeam,
    List<string> Tags,
    DateTime CreatedAt,
    DateTime UpdatedAt);

/// <summary>Application detail view.</summary>
public record ApplicationDetailDto(
    int Id,
    string Name,
    string ShortDescription,
    string? DetailedDescription,
    string Status,
    string Type,
    string OwnerTeam,
    string SourceControl,
    string? RepositoryUrl,
    string? WikiUrl,
    List<string> Tags,
    List<EnvironmentDto> Environments,
    List<ContactDto> Contacts,
    int? CreatedByUserId,
    string? CreatedByName,
    DateTime CreatedAt,
    DateTime UpdatedAt);

/// <summary>Environment entry.</summary>
public record EnvironmentDto(int Id, string Type, string Url, string? Notes, bool IsPublic);

/// <summary>Contact entry.</summary>
public record ContactDto(int Id, string Type, string Value, string? Label);

/// <summary>Create application request.</summary>
public record CreateApplicationRequest(
    string Name,
    string ShortDescription,
    string? DetailedDescription,
    ApplicationStatus Status,
    ApplicationType Type,
    string OwnerTeam,
    SourceControlType SourceControl,
    string? RepositoryUrl,
    string? WikiUrl,
    List<int>? TagIds);

/// <summary>Update application request.</summary>
public record UpdateApplicationRequest(
    string Name,
    string ShortDescription,
    string? DetailedDescription,
    ApplicationStatus Status,
    ApplicationType Type,
    string OwnerTeam,
    SourceControlType SourceControl,
    string? RepositoryUrl,
    string? WikiUrl,
    List<int>? TagIds);

/// <summary>Create environment request.</summary>
public record CreateEnvironmentRequest(EnvironmentType Type, string Url, string? Notes, bool IsPublic = true);

/// <summary>Update environment request.</summary>
public record UpdateEnvironmentRequest(EnvironmentType Type, string Url, string? Notes, bool IsPublic = true);

/// <summary>Create contact request.</summary>
public record CreateContactRequest(ContactType Type, string Value, string? Label);

/// <summary>Update contact request.</summary>
public record UpdateContactRequest(ContactType Type, string Value, string? Label);
