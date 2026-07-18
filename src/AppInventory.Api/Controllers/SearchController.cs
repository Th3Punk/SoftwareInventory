using AppInventory.Api.Authorization;
using AppInventory.Api.Middleware;
using AppInventory.Core.Authorization;
using AppInventory.Core.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace AppInventory.Api.Controllers;

/// <summary>
/// Full-text search across the application catalog and documentation.
/// </summary>
[ApiController]
[Route("api/v1/search")]
[FeatureGate("Search")]
[Authorize]
[RbacAuthorize]
public class SearchController : ControllerBase
{
    private readonly ISearchProvider _search;

    public SearchController(ISearchProvider search)
    {
        _search = search;
    }

    /// <summary>Search applications and documentation.</summary>
    /// <param name="q">Search term (required).</param>
    /// <param name="type">Filter by resource type: Application, Documentation.</param>
    /// <param name="tag">Filter by tag name (applications only).</param>
    /// <param name="page">Page number (default 1).</param>
    /// <param name="pageSize">Results per page (default 20, max 100).</param>
    /// <response code="200">Search results.</response>
    /// <response code="400">Missing or empty search term.</response>
    /// <response code="501">Search feature is not enabled.</response>
    [HttpGet]
    [ProducesResponseType(typeof(SearchResponseDto), 200)]
    [ProducesResponseType(400)]
    [ProducesResponseType(501)]
    public async Task<IActionResult> SearchAsync(
        [FromQuery] string? q,
        [FromQuery(Name = "type")] string[]? type = null,
        [FromQuery(Name = "tag")] string[]? tag = null,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20,
        CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(q))
        {
            return new ObjectResult(new ProblemDetails
            {
                Type = "https://tools.ietf.org/html/rfc7807",
                Title = "Bad Request",
                Status = 400,
                Detail = "Search term 'q' is required."
            })
            {
                StatusCode = 400,
                ContentTypes = { "application/problem+json" }
            };
        }

        if (!_search.IsAvailable)
        {
            return new ObjectResult(new ProblemDetails
            {
                Type = "https://tools.ietf.org/html/rfc7807",
                Title = "Not Implemented",
                Status = 501,
                Detail = "Search is not available."
            })
            {
                StatusCode = 501,
                ContentTypes = { "application/problem+json" }
            };
        }

        var allowedDocTypes = ResolveAllowedDocTypes();

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

        var searchQuery = new SearchQuery(
            Term: q,
            ResourceTypes: type?.Length > 0 ? type : null,
            Tags: tag?.Length > 0 ? tag : null,
            AllowedDocumentationTypes: allowedDocTypes,
            Page: page,
            PageSize: pageSize);

        var result = await _search.SearchAsync(searchQuery, ct);

        return Ok(new SearchResponseDto(
            result.Items.Select(i => new SearchResultItemDto(i.ResourceType, i.ResourceId, i.Title, i.Snippet, i.Score)).ToList(),
            result.TotalCount,
            page,
            pageSize));
    }

    private IReadOnlyList<string>? ResolveAllowedDocTypes()
    {
        if (User.IsInRole(RoleNames.Admin))
        {
            return null;
        }

        if (User.IsInRole(RoleNames.Developer))
        {
            return ["User", "Developer"];
        }

        return ["User"];
    }
}

// --- DTOs ---

/// <summary>Search response with paginated results.</summary>
public record SearchResponseDto(IReadOnlyList<SearchResultItemDto> Items, int TotalCount, int Page, int PageSize);

/// <summary>Individual search result.</summary>
public record SearchResultItemDto(string ResourceType, int ResourceId, string Title, string Snippet, double Score);
