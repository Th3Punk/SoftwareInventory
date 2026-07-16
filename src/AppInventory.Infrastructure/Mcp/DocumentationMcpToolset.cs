using System.ComponentModel;
using AppInventory.Core.Entities;
using AppInventory.Core.Interfaces;
using AppInventory.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
using ModelContextProtocol.Server;

namespace AppInventory.Infrastructure.Mcp;

/// <summary>
/// Documentation MCP tools. Read-only; restricted to User-type docs for ReadOnly service tokens (spec 12.4.4).
/// </summary>
[McpServerToolType]
internal sealed class DocumentationMcpToolset
{
    [McpServerTool, Description("List user-facing documentation entries for an application. Returns title, type and status without full content.")]
    public async Task<List<McpDocumentationItem>> ListDocumentation(
        AppInventoryDbContext db,
        IAuditProvider audit,
        [Description("The numeric application ID.")] int applicationId,
        CancellationToken ct = default)
    {
        await audit.LogAsync("McpToolCall", "list_documentation", applicationId.ToString(), ct: ct);

        var appExists = await db.Applications.AnyAsync(a => a.Id == applicationId, ct);
        if (!appExists)
        {
            return [];
        }

        return await db.Documentations
            .Where(d => d.ApplicationId == applicationId
                && d.Status != DocumentationStatus.Archived
                && d.Type == DocumentationType.User)
            .OrderBy(d => d.Title)
            .Select(d => new McpDocumentationItem(
                d.Id, d.Title, d.Type.ToString(), d.Status.ToString(), d.Version, d.UpdatedAt))
            .ToListAsync(ct);
    }

    [McpServerTool, Description("Get the full Markdown content of a user-facing documentation entry for an application.")]
    public async Task<McpDocumentationDetail?> GetDocumentation(
        AppInventoryDbContext db,
        IAuditProvider audit,
        [Description("The numeric application ID.")] int applicationId,
        [Description("The numeric documentation ID.")] int documentationId,
        CancellationToken ct = default)
    {
        await audit.LogAsync("McpToolCall", "get_documentation", documentationId.ToString(), ct: ct);

        var doc = await db.Documentations
            .Where(d => d.ApplicationId == applicationId
                && d.Id == documentationId
                && d.Type == DocumentationType.User
                && d.Status != DocumentationStatus.Archived)
            .FirstOrDefaultAsync(ct);

        if (doc is null)
        {
            return null;
        }

        return new McpDocumentationDetail(
            doc.Id, doc.Title, doc.Content,
            doc.Type.ToString(), doc.Status.ToString(),
            doc.Version, doc.CreatedAt, doc.UpdatedAt);
    }
}

// --- DTOs ---

public record McpDocumentationItem(int Id, string Title, string Type, string Status, int Version, DateTime UpdatedAt);
public record McpDocumentationDetail(int Id, string Title, string Content, string Type, string Status, int Version, DateTime CreatedAt, DateTime UpdatedAt);
