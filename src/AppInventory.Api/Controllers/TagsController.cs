using AppInventory.Api.Authorization;
using AppInventory.Api.Middleware;
using AppInventory.Core.Authorization;
using AppInventory.Core.Entities;
using AppInventory.Infrastructure.Data;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace AppInventory.Api.Controllers;

/// <summary>
/// Tag management endpoints.
/// </summary>
[ApiController]
[Route("api/v1/tags")]
[FeatureGate("ApplicationCatalog")]
[Authorize]
[RbacAuthorize]
public class TagsController : ControllerBase
{
    private readonly AppInventoryDbContext _dbContext;

    public TagsController(AppInventoryDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    /// <summary>
    /// List all tags.
    /// </summary>
    /// <response code="200">List of tags.</response>
    [HttpGet]
    [ProducesResponseType(typeof(List<TagDto>), 200)]
    public async Task<IActionResult> ListAsync(CancellationToken ct)
    {
        var tags = await _dbContext.Tags
            .OrderBy(t => t.Name)
            .Select(t => new TagDto(t.Id, t.Name, t.Color))
            .ToListAsync(ct);

        return Ok(tags);
    }

    /// <summary>
    /// Create a new tag.
    /// </summary>
    /// <response code="201">Tag created.</response>
    /// <response code="409">Tag name already exists.</response>
    [HttpPost]
    [RbacAuthorize(RoleNames.Admin)]
    [ProducesResponseType(typeof(TagDto), 201)]
    [ProducesResponseType(typeof(ProblemDetails), 409)]
    public async Task<IActionResult> CreateAsync([FromBody] CreateTagRequest request, CancellationToken ct)
    {
        var normalizedName = request.Name.ToLowerInvariant();

        var exists = await _dbContext.Tags.AnyAsync(t => t.Name == normalizedName, ct);
        if (exists)
        {
            return Problem(
                statusCode: 409,
                title: "Conflict",
                detail: $"A tag with the name '{normalizedName}' already exists.");
        }

        var tag = new Tag
        {
            Name = normalizedName,
            Color = request.Color
        };

        _dbContext.Tags.Add(tag);
        await _dbContext.SaveChangesAsync(ct);

        return CreatedAtAction(nameof(ListAsync), new TagDto(tag.Id, tag.Name, tag.Color));
    }

    /// <summary>
    /// Delete a tag.
    /// </summary>
    /// <response code="204">Tag deleted.</response>
    /// <response code="404">Tag not found.</response>
    [HttpDelete("{id:int}")]
    [RbacAuthorize(RoleNames.Admin)]
    [ProducesResponseType(204)]
    [ProducesResponseType(typeof(ProblemDetails), 404)]
    public async Task<IActionResult> DeleteAsync(int id, CancellationToken ct)
    {
        var tag = await _dbContext.Tags.FirstOrDefaultAsync(t => t.Id == id, ct);

        if (tag is null)
        {
            return Problem(
                statusCode: 404,
                title: "Not Found",
                detail: $"Tag with id {id} was not found.");
        }

        _dbContext.Tags.Remove(tag);
        await _dbContext.SaveChangesAsync(ct);

        return NoContent();
    }
}

/// <summary>Tag response.</summary>
public record TagDto(int Id, string Name, string? Color);

/// <summary>Create tag request.</summary>
public record CreateTagRequest(string Name, string? Color);
