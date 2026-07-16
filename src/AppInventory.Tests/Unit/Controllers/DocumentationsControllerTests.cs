using System.Security.Claims;
using AppInventory.Api.Controllers;
using AppInventory.Core.Authorization;
using AppInventory.Core.Entities;
using AppInventory.Infrastructure.Data;
using FluentAssertions;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace AppInventory.Tests.Unit.Controllers;

public class DocumentationsControllerTests : IDisposable
{
    private readonly AppInventoryDbContext _dbContext;
    private readonly DocumentationsController _controller;

    public DocumentationsControllerTests()
    {
        var options = new DbContextOptionsBuilder<AppInventoryDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;

        _dbContext = new AppInventoryDbContext(options);
        _controller = new DocumentationsController(_dbContext);
    }

    public void Dispose() => _dbContext.Dispose();

    private static ClaimsPrincipal CreateUser(string role, int userId = 1)
    {
        return new ClaimsPrincipal(new ClaimsIdentity(
        [
            new Claim(ClaimTypes.NameIdentifier, userId.ToString()),
            new Claim(ClaimTypes.Role, role)
        ], "Test"));
    }

    private async Task<Application> SeedAppAsync()
    {
        var app = new Application
        {
            Name = "TestApp",
            ShortDescription = "desc",
            OwnerTeam = "team",
            Status = ApplicationStatus.Active,
            Type = ApplicationType.WebApp,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };
        _dbContext.Applications.Add(app);
        await _dbContext.SaveChangesAsync();
        return app;
    }

    private async Task<Documentation> SeedDocAsync(int appId, DocumentationType type = DocumentationType.User)
    {
        var doc = new Documentation
        {
            ApplicationId = appId,
            Title = "Guide",
            Content = "# Hello",
            Type = type,
            Status = DocumentationStatus.Published,
            Version = 1,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };
        _dbContext.Documentations.Add(doc);
        await _dbContext.SaveChangesAsync();
        return doc;
    }

    [Fact]
    public async Task List_ReturnsOnlyUserDocs_ForReadOnlyRole()
    {
        var app = await SeedAppAsync();
        await SeedDocAsync(app.Id, DocumentationType.User);
        await SeedDocAsync(app.Id, DocumentationType.Developer);
        _controller.ControllerContext = new ControllerContext
        {
            HttpContext = new DefaultHttpContext { User = CreateUser(RoleNames.ReadOnly) }
        };

        var result = await _controller.ListAsync(app.Id);

        var ok = result.Should().BeOfType<OkObjectResult>().Subject;
        var items = ok.Value.Should().BeAssignableTo<IEnumerable<DocumentationListItemDto>>().Subject;
        items.Should().HaveCount(1);
        items.First().Type.Should().Be("User");
    }

    [Fact]
    public async Task List_ReturnsUserAndDeveloperDocs_ForDeveloperRole()
    {
        var app = await SeedAppAsync();
        await SeedDocAsync(app.Id, DocumentationType.User);
        await SeedDocAsync(app.Id, DocumentationType.Developer);
        await SeedDocAsync(app.Id, DocumentationType.Operations);
        _controller.ControllerContext = new ControllerContext
        {
            HttpContext = new DefaultHttpContext { User = CreateUser(RoleNames.Developer) }
        };

        var result = await _controller.ListAsync(app.Id);

        var ok = result.Should().BeOfType<OkObjectResult>().Subject;
        var items = ok.Value.Should().BeAssignableTo<IEnumerable<DocumentationListItemDto>>().Subject;
        items.Should().HaveCount(2);
    }

    [Fact]
    public async Task List_ReturnsAllDocs_ForAdminRole()
    {
        var app = await SeedAppAsync();
        await SeedDocAsync(app.Id, DocumentationType.User);
        await SeedDocAsync(app.Id, DocumentationType.Developer);
        await SeedDocAsync(app.Id, DocumentationType.Operations);
        _controller.ControllerContext = new ControllerContext
        {
            HttpContext = new DefaultHttpContext { User = CreateUser(RoleNames.Admin) }
        };

        var result = await _controller.ListAsync(app.Id);

        var ok = result.Should().BeOfType<OkObjectResult>().Subject;
        var items = ok.Value.Should().BeAssignableTo<IEnumerable<DocumentationListItemDto>>().Subject;
        items.Should().HaveCount(3);
    }

    [Fact]
    public async Task List_Returns404_WhenAppNotFound()
    {
        _controller.ControllerContext = new ControllerContext
        {
            HttpContext = new DefaultHttpContext { User = CreateUser(RoleNames.ReadOnly) }
        };

        var result = await _controller.ListAsync(999);

        result.Should().BeOfType<ObjectResult>()
            .Which.StatusCode.Should().Be(404);
    }

    [Fact]
    public async Task Create_Returns403_ForReadOnlyUser()
    {
        var app = await SeedAppAsync();
        _controller.ControllerContext = new ControllerContext
        {
            HttpContext = new DefaultHttpContext { User = CreateUser(RoleNames.ReadOnly) }
        };

        var result = await _controller.CreateAsync(app.Id, new CreateDocumentationRequest("Title", "Content", DocumentationType.User));

        result.Should().BeOfType<ObjectResult>()
            .Which.StatusCode.Should().Be(403);
    }

    [Fact]
    public async Task Create_ReturnsCreated_ForDeveloper()
    {
        var app = await SeedAppAsync();
        _controller.ControllerContext = new ControllerContext
        {
            HttpContext = new DefaultHttpContext { User = CreateUser(RoleNames.Developer) }
        };

        var result = await _controller.CreateAsync(app.Id, new CreateDocumentationRequest("Guide", "# Content", DocumentationType.User));

        var created = result.Should().BeOfType<CreatedAtActionResult>().Subject;
        var dto = created.Value.Should().BeOfType<DocumentationDetailDto>().Subject;
        dto.Title.Should().Be("Guide");
        dto.Version.Should().Be(1);
    }

    [Fact]
    public async Task Update_ArchivesOldVersion_AndIncrementsVersion()
    {
        var app = await SeedAppAsync();
        var doc = await SeedDocAsync(app.Id);
        _controller.ControllerContext = new ControllerContext
        {
            HttpContext = new DefaultHttpContext { User = CreateUser(RoleNames.Developer) }
        };

        var result = await _controller.UpdateAsync(app.Id, doc.Id, new UpdateDocumentationRequest("New Title", "# Updated", DocumentationType.User));

        result.Should().BeOfType<OkObjectResult>();
        var dto = ((OkObjectResult)result).Value.Should().BeOfType<DocumentationDetailDto>().Subject;
        dto.Version.Should().Be(2);

        var history = await _dbContext.DocumentationHistories.Where(h => h.DocumentationId == doc.Id).ToListAsync();
        history.Should().HaveCount(1);
        history[0].Version.Should().Be(1);
        history[0].Content.Should().Be("# Hello");
    }

    [Fact]
    public async Task Delete_ArchivesDoc_ForAdmin()
    {
        var app = await SeedAppAsync();
        var doc = await SeedDocAsync(app.Id);
        _controller.ControllerContext = new ControllerContext
        {
            HttpContext = new DefaultHttpContext { User = CreateUser(RoleNames.Admin) }
        };

        var result = await _controller.DeleteAsync(app.Id, doc.Id);

        result.Should().BeOfType<NoContentResult>();
        var updated = await _dbContext.Documentations.FindAsync(doc.Id);
        updated!.Status.Should().Be(DocumentationStatus.Archived);
    }

    [Fact]
    public async Task Delete_Returns403_ForDeveloper()
    {
        var app = await SeedAppAsync();
        var doc = await SeedDocAsync(app.Id);
        _controller.ControllerContext = new ControllerContext
        {
            HttpContext = new DefaultHttpContext { User = CreateUser(RoleNames.Developer) }
        };

        var result = await _controller.DeleteAsync(app.Id, doc.Id);

        result.Should().BeOfType<ObjectResult>()
            .Which.StatusCode.Should().Be(403);
    }

    [Fact]
    public async Task GetHistory_ReturnsVersionList_ForDeveloper()
    {
        var app = await SeedAppAsync();
        var doc = await SeedDocAsync(app.Id);
        _dbContext.DocumentationHistories.Add(new DocumentationHistory
        {
            DocumentationId = doc.Id,
            Content = "# Old",
            Version = 1,
            ArchivedAt = DateTime.UtcNow
        });
        await _dbContext.SaveChangesAsync();

        _controller.ControllerContext = new ControllerContext
        {
            HttpContext = new DefaultHttpContext { User = CreateUser(RoleNames.Developer) }
        };

        var result = await _controller.GetHistoryAsync(app.Id, doc.Id);

        var ok = result.Should().BeOfType<OkObjectResult>().Subject;
        var items = ok.Value.Should().BeAssignableTo<IEnumerable<DocumentationHistoryDto>>().Subject;
        items.Should().HaveCount(1);
    }

    [Fact]
    public async Task GetHistoryVersion_ReturnsContent_ForDeveloper()
    {
        var app = await SeedAppAsync();
        var doc = await SeedDocAsync(app.Id);
        _dbContext.DocumentationHistories.Add(new DocumentationHistory
        {
            DocumentationId = doc.Id,
            Content = "# Version 1",
            Version = 1,
            ArchivedAt = DateTime.UtcNow
        });
        await _dbContext.SaveChangesAsync();

        _controller.ControllerContext = new ControllerContext
        {
            HttpContext = new DefaultHttpContext { User = CreateUser(RoleNames.Developer) }
        };

        var result = await _controller.GetHistoryVersionAsync(app.Id, doc.Id, 1);

        var ok = result.Should().BeOfType<OkObjectResult>().Subject;
        var dto = ok.Value.Should().BeOfType<DocumentationHistoryDetailDto>().Subject;
        dto.Content.Should().Be("# Version 1");
        dto.Version.Should().Be(1);
    }
}
