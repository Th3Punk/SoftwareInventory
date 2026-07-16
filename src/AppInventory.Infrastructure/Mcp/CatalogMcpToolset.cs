using System.ComponentModel;
using AppInventory.Core.Entities;
using AppInventory.Core.Interfaces;
using AppInventory.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
using ModelContextProtocol.Server;

namespace AppInventory.Infrastructure.Mcp;

/// <summary>
/// Application catalog MCP tools. Read-only; RBAC applied at query level (spec 8.5, 12.4).
/// Service tokens with DefaultRole=ReadOnly see only public environments.
/// </summary>
[McpServerToolType]
internal sealed class CatalogMcpToolset
{
    [McpServerTool, Description("Full-text search across the application catalog. Returns matching applications with name, description, team and tags.")]
    public async Task<McpPagedResult<McpApplicationItem>> SearchApplications(
        ISearchProvider search,
        IAuditProvider audit,
        [Description("The search term to find applications.")] string q,
        [Description("Page number, starting at 1.")] int page = 1,
        [Description("Items per page, maximum 50.")] int pageSize = 20,
        CancellationToken ct = default)
    {
        await audit.LogAsync("McpToolCall", "search_applications", q, ct: ct);

        if (page < 1) page = 1;
        if (pageSize is < 1 or > 50) pageSize = Math.Clamp(pageSize, 1, 50);

        var result = await search.SearchAsync(
            new SearchQuery(
                Term: q,
                ResourceTypes: ["Application"],
                AllowedDocumentationTypes: ["User"],
                Page: page,
                PageSize: pageSize),
            ct);

        var items = result.Items
            .Select(i => new McpApplicationItem(i.ResourceId, i.Title, i.Snippet, "Application"))
            .ToList();

        return new McpPagedResult<McpApplicationItem>(items, result.TotalCount, page, pageSize);
    }

    [McpServerTool, Description("List applications in the inventory with optional filters by status, type, and owner team.")]
    public async Task<McpPagedResult<McpApplicationSummary>> ListApplications(
        AppInventoryDbContext db,
        IAuditProvider audit,
        [Description("Filter by status: Active, Inactive, Deprecated, UnderDevelopment.")] string? status = null,
        [Description("Filter by type: WebApplication, Api, Service, Library, MobileApp, Desktop, Other.")] string? type = null,
        [Description("Filter by owner team (substring match).")] string? team = null,
        [Description("Page number, starting at 1.")] int page = 1,
        [Description("Items per page, maximum 50.")] int pageSize = 20,
        CancellationToken ct = default)
    {
        await audit.LogAsync("McpToolCall", "list_applications", null, ct: ct);

        if (page < 1) page = 1;
        if (pageSize is < 1 or > 50) pageSize = Math.Clamp(pageSize, 1, 50);

        var query = db.Applications
            .Include(a => a.Tags).ThenInclude(at => at.Tag)
            .AsQueryable();

        if (!string.IsNullOrWhiteSpace(status) &&
            Enum.TryParse<ApplicationStatus>(status, ignoreCase: true, out var statusEnum))
        {
            query = query.Where(a => a.Status == statusEnum);
        }

        if (!string.IsNullOrWhiteSpace(type) &&
            Enum.TryParse<ApplicationType>(type, ignoreCase: true, out var typeEnum))
        {
            query = query.Where(a => a.Type == typeEnum);
        }

        if (!string.IsNullOrWhiteSpace(team))
        {
            query = query.Where(a => a.OwnerTeam.Contains(team));
        }

        var totalCount = await query.CountAsync(ct);
        var items = await query
            .OrderBy(a => a.Name)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .Select(a => new McpApplicationSummary(
                a.Id,
                a.Name,
                a.ShortDescription,
                a.Status.ToString(),
                a.Type.ToString(),
                a.OwnerTeam,
                a.Tags.Select(t => t.Tag.Name).ToList()))
            .ToListAsync(ct);

        return new McpPagedResult<McpApplicationSummary>(items, totalCount, page, pageSize);
    }

    [McpServerTool, Description("Get full details of an application by its numeric ID, including contacts and public deployment environments.")]
    public async Task<McpApplicationDetail?> GetApplication(
        AppInventoryDbContext db,
        IAuditProvider audit,
        [Description("The numeric application ID.")] int id,
        CancellationToken ct = default)
    {
        await audit.LogAsync("McpToolCall", "get_application", id.ToString(), ct: ct);

        var app = await db.Applications
            .Include(a => a.Tags).ThenInclude(at => at.Tag)
            .Include(a => a.Contacts)
            .Include(a => a.Environments)
            .FirstOrDefaultAsync(a => a.Id == id, ct);

        if (app is null)
        {
            return null;
        }

        return new McpApplicationDetail(
            app.Id,
            app.Name,
            app.ShortDescription,
            app.DetailedDescription,
            app.Status.ToString(),
            app.Type.ToString(),
            app.OwnerTeam,
            app.RepositoryUrl,
            app.WikiUrl,
            app.Tags.Select(t => t.Tag.Name).ToList(),
            app.Environments
                .Where(e => e.IsPublic)
                .Select(e => new McpEnvironmentItem(e.Id, e.Type.ToString(), e.Url, e.Notes))
                .ToList(),
            app.Contacts
                .Select(c => new McpContactItem(c.Type.ToString(), c.Value, c.Label))
                .ToList(),
            app.CreatedAt,
            app.UpdatedAt);
    }

    [McpServerTool, Description("List public deployment environment URLs for an application (production, staging, etc.).")]
    public async Task<List<McpEnvironmentItem>> GetApplicationEnvironments(
        AppInventoryDbContext db,
        IAuditProvider audit,
        [Description("The numeric application ID.")] int applicationId,
        CancellationToken ct = default)
    {
        await audit.LogAsync("McpToolCall", "get_application_environments", applicationId.ToString(), ct: ct);

        var appExists = await db.Applications.AnyAsync(a => a.Id == applicationId, ct);
        if (!appExists)
        {
            return [];
        }

        return await db.ApplicationEnvironments
            .Where(e => e.ApplicationId == applicationId && e.IsPublic)
            .Select(e => new McpEnvironmentItem(e.Id, e.Type.ToString(), e.Url, e.Notes))
            .ToListAsync(ct);
    }
}

// --- DTOs ---

public record McpPagedResult<T>(List<T> Items, int TotalCount, int Page, int PageSize);
public record McpApplicationItem(int ResourceId, string Title, string Snippet, string ResourceType);
public record McpApplicationSummary(int Id, string Name, string ShortDescription, string Status, string Type, string OwnerTeam, List<string> Tags);
public record McpApplicationDetail(int Id, string Name, string ShortDescription, string? DetailedDescription, string Status, string Type, string OwnerTeam, string? RepositoryUrl, string? WikiUrl, List<string> Tags, List<McpEnvironmentItem> Environments, List<McpContactItem> Contacts, DateTime CreatedAt, DateTime UpdatedAt);
public record McpEnvironmentItem(int Id, string Type, string Url, string? Notes);
public record McpContactItem(string Type, string Value, string? Label);
