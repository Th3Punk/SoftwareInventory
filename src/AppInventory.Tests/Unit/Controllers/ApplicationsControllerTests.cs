using System.Security.Claims;
using AppInventory.Api.Authorization;
using AppInventory.Api.Controllers;
using AppInventory.Core.Authorization;
using AppInventory.Core.Entities;
using AppInventory.Infrastructure.Data;
using FluentAssertions;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Moq;
using Xunit;

namespace AppInventory.Tests.Unit.Controllers;

public class ApplicationsControllerTests : IDisposable
{
    private readonly AppInventoryDbContext _dbContext;
    private readonly Mock<IAuthorizationService> _authService;
    private readonly ApplicationsController _controller;

    public ApplicationsControllerTests()
    {
        var options = new DbContextOptionsBuilder<AppInventoryDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;

        _dbContext = new AppInventoryDbContext(options);
        _authService = new Mock<IAuthorizationService>();
        _controller = new ApplicationsController(_dbContext, _authService.Object);
        _controller.ControllerContext = new ControllerContext
        {
            HttpContext = new DefaultHttpContext
            {
                User = CreateUser(RoleNames.Admin)
            }
        };
    }

    public void Dispose()
    {
        _dbContext.Dispose();
    }

    private static ClaimsPrincipal CreateUser(params string[] roles)
    {
        var claims = new List<Claim>
        {
            new(ClaimTypes.NameIdentifier, "1"),
            new(ClaimNames.IsActive, "true")
        };
        claims.AddRange(roles.Select(r => new Claim(ClaimTypes.Role, r)));
        return new ClaimsPrincipal(new ClaimsIdentity(claims, "Test"));
    }

    private async Task<Application> SeedApplicationAsync(string name = "TestApp")
    {
        var app = new Application
        {
            Name = name,
            ShortDescription = "Test application",
            OwnerTeam = "TeamA",
            Status = ApplicationStatus.Active,
            Type = ApplicationType.WebApp,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow,
            CreatedByUserId = 1
        };
        _dbContext.Applications.Add(app);
        await _dbContext.SaveChangesAsync();
        return app;
    }

    [Fact]
    public async Task List_ReturnsPagedResponse()
    {
        await SeedApplicationAsync("App1");
        await SeedApplicationAsync("App2");

        var result = await _controller.ListAsync(null, null, null, null, null, 1, 20, null);

        var ok = result.Should().BeOfType<OkObjectResult>().Subject;
        var response = ok.Value.Should().BeOfType<PagedResponse<ApplicationListItemDto>>().Subject;
        response.TotalCount.Should().Be(2);
        response.Items.Should().HaveCount(2);
    }

    [Fact]
    public async Task List_FiltersByStatus()
    {
        await SeedApplicationAsync("ActiveApp");
        var retired = await SeedApplicationAsync("RetiredApp");
        retired.Status = ApplicationStatus.Retired;
        await _dbContext.SaveChangesAsync();

        var result = await _controller.ListAsync(ApplicationStatus.Active, null, null, null, null, 1, 20, null);

        var ok = result.Should().BeOfType<OkObjectResult>().Subject;
        var response = ok.Value.Should().BeOfType<PagedResponse<ApplicationListItemDto>>().Subject;
        response.TotalCount.Should().Be(1);
        response.Items[0].Name.Should().Be("ActiveApp");
    }

    [Fact]
    public async Task List_FiltersByTeamPartialMatch()
    {
        await SeedApplicationAsync("App1");
        var app2 = await SeedApplicationAsync("App2");
        app2.OwnerTeam = "TeamB";
        await _dbContext.SaveChangesAsync();

        var result = await _controller.ListAsync(null, null, "TeamA", null, null, 1, 20, null);

        var ok = result.Should().BeOfType<OkObjectResult>().Subject;
        var response = ok.Value.Should().BeOfType<PagedResponse<ApplicationListItemDto>>().Subject;
        response.TotalCount.Should().Be(1);
    }

    [Fact]
    public async Task List_PaginatesCorrectly()
    {
        for (int i = 0; i < 5; i++)
            await SeedApplicationAsync($"App{i}");

        var result = await _controller.ListAsync(null, null, null, null, null, 2, 2, null);

        var ok = result.Should().BeOfType<OkObjectResult>().Subject;
        var response = ok.Value.Should().BeOfType<PagedResponse<ApplicationListItemDto>>().Subject;
        response.TotalCount.Should().Be(5);
        response.Items.Should().HaveCount(2);
        response.Page.Should().Be(2);
    }

    [Fact]
    public async Task Get_ReturnsApplicationDetail()
    {
        var app = await SeedApplicationAsync();

        var result = await _controller.GetAsync(app.Id);

        var ok = result.Should().BeOfType<OkObjectResult>().Subject;
        var detail = ok.Value.Should().BeOfType<ApplicationDetailDto>().Subject;
        detail.Name.Should().Be("TestApp");
    }

    [Fact]
    public async Task Get_Returns404ForMissing()
    {
        var result = await _controller.GetAsync(999);

        var obj = result.Should().BeOfType<ObjectResult>().Subject;
        obj.StatusCode.Should().Be(404);
    }

    [Fact]
    public async Task Create_ReturnsCreatedResult()
    {
        var request = new CreateApplicationRequest(
            "NewApp", "Short desc", null, ApplicationStatus.Active,
            ApplicationType.WebApp, "TeamA", SourceControlType.None, null, null, null);

        var result = await _controller.CreateAsync(request);

        var created = result.Should().BeOfType<CreatedAtActionResult>().Subject;
        created.StatusCode.Should().Be(201);

        var detail = created.Value.Should().BeOfType<ApplicationDetailDto>().Subject;
        detail.Name.Should().Be("NewApp");
    }

    [Fact]
    public async Task Create_Returns409ForDuplicateName()
    {
        await SeedApplicationAsync("DuplicateApp");

        var request = new CreateApplicationRequest(
            "DuplicateApp", "Desc", null, ApplicationStatus.Active,
            ApplicationType.WebApp, "TeamA", SourceControlType.None, null, null, null);

        var result = await _controller.CreateAsync(request);

        var obj = result.Should().BeOfType<ObjectResult>().Subject;
        obj.StatusCode.Should().Be(409);
    }

    [Fact]
    public async Task Create_Returns400WhenSourceControlSetWithoutUrl()
    {
        var request = new CreateApplicationRequest(
            "GitApp", "Desc", null, ApplicationStatus.Active,
            ApplicationType.WebApp, "TeamA", SourceControlType.Git, null, null, null);

        var result = await _controller.CreateAsync(request);

        var obj = result.Should().BeOfType<ObjectResult>().Subject;
        obj.StatusCode.Should().Be(400);
    }

    [Fact]
    public async Task Create_AcceptsValidRepositoryUrl()
    {
        var request = new CreateApplicationRequest(
            "GitApp", "Desc", null, ApplicationStatus.Active,
            ApplicationType.WebApp, "TeamA", SourceControlType.Git,
            "https://github.com/org/repo", null, null);

        var result = await _controller.CreateAsync(request);

        result.Should().BeOfType<CreatedAtActionResult>();
    }

    [Fact]
    public async Task Create_Returns400ForInvalidUrlScheme()
    {
        var request = new CreateApplicationRequest(
            "FtpApp", "Desc", null, ApplicationStatus.Active,
            ApplicationType.WebApp, "TeamA", SourceControlType.Git,
            "ftp://files.example.com/repo", null, null);

        var result = await _controller.CreateAsync(request);

        var obj = result.Should().BeOfType<ObjectResult>().Subject;
        obj.StatusCode.Should().Be(400);
    }

    [Fact]
    public async Task Delete_SoftDeletes()
    {
        var app = await SeedApplicationAsync();

        var result = await _controller.DeleteAsync(app.Id);

        result.Should().BeOfType<NoContentResult>();

        var deleted = await _dbContext.Applications
            .IgnoreQueryFilters()
            .FirstAsync(a => a.Id == app.Id);
        deleted.IsDeleted.Should().BeTrue();
    }

    [Fact]
    public async Task Delete_Returns404ForMissing()
    {
        var result = await _controller.DeleteAsync(999);

        var obj = result.Should().BeOfType<ObjectResult>().Subject;
        obj.StatusCode.Should().Be(404);
    }

    [Fact]
    public async Task SoftDeletedApplications_NotReturnedInList()
    {
        var app = await SeedApplicationAsync();
        app.IsDeleted = true;
        await _dbContext.SaveChangesAsync();

        var result = await _controller.ListAsync(null, null, null, null, null, 1, 20, null);

        var ok = result.Should().BeOfType<OkObjectResult>().Subject;
        var response = ok.Value.Should().BeOfType<PagedResponse<ApplicationListItemDto>>().Subject;
        response.TotalCount.Should().Be(0);
    }

    // --- Environment tests ---

    [Fact]
    public async Task CreateEnvironment_ReturnsCreated()
    {
        var app = await SeedApplicationAsync();
        var request = new CreateEnvironmentRequest(EnvironmentType.Production, "https://app.example.com", null, true);

        var result = await _controller.CreateEnvironmentAsync(app.Id, request);

        result.Should().BeOfType<CreatedAtActionResult>();
    }

    [Fact]
    public async Task CreateEnvironment_Returns400ForInvalidUrl()
    {
        var app = await SeedApplicationAsync();
        var request = new CreateEnvironmentRequest(EnvironmentType.Production, "ftp://app.example.com", null, true);

        var result = await _controller.CreateEnvironmentAsync(app.Id, request);

        var obj = result.Should().BeOfType<ObjectResult>().Subject;
        obj.StatusCode.Should().Be(400);
    }

    [Fact]
    public async Task CreateEnvironment_Returns404ForMissingApp()
    {
        var request = new CreateEnvironmentRequest(EnvironmentType.Production, "https://app.example.com", null, true);

        var result = await _controller.CreateEnvironmentAsync(999, request);

        var obj = result.Should().BeOfType<ObjectResult>().Subject;
        obj.StatusCode.Should().Be(404);
    }

    [Fact]
    public async Task ListEnvironments_FiltersNonPublicForReadOnly()
    {
        var app = await SeedApplicationAsync();
        _dbContext.ApplicationEnvironments.AddRange(
            new ApplicationEnvironment { ApplicationId = app.Id, Type = EnvironmentType.Production, Url = "https://prod.example.com", IsPublic = true },
            new ApplicationEnvironment { ApplicationId = app.Id, Type = EnvironmentType.Test, Url = "https://staging.example.com", IsPublic = false });
        await _dbContext.SaveChangesAsync();

        _controller.ControllerContext.HttpContext.User = CreateUser(RoleNames.ReadOnly);

        var result = await _controller.ListEnvironmentsAsync(app.Id);

        var ok = result.Should().BeOfType<OkObjectResult>().Subject;
        var envs = ok.Value.Should().BeAssignableTo<List<EnvironmentDto>>().Subject;
        envs.Should().HaveCount(1);
        envs[0].Type.Should().Be("Production");
    }

    [Fact]
    public async Task ListEnvironments_ShowsNonPublicForDeveloper()
    {
        var app = await SeedApplicationAsync();
        _dbContext.ApplicationEnvironments.AddRange(
            new ApplicationEnvironment { ApplicationId = app.Id, Type = EnvironmentType.Production, Url = "https://prod.example.com", IsPublic = true },
            new ApplicationEnvironment { ApplicationId = app.Id, Type = EnvironmentType.Test, Url = "https://staging.example.com", IsPublic = false });
        await _dbContext.SaveChangesAsync();

        _controller.ControllerContext.HttpContext.User = CreateUser(RoleNames.Developer);

        var result = await _controller.ListEnvironmentsAsync(app.Id);

        var ok = result.Should().BeOfType<OkObjectResult>().Subject;
        var envs = ok.Value.Should().BeAssignableTo<List<EnvironmentDto>>().Subject;
        envs.Should().HaveCount(2);
    }

    // --- Contact tests ---

    [Fact]
    public async Task CreateContact_ReturnsCreated()
    {
        var app = await SeedApplicationAsync();
        var request = new CreateContactRequest(ContactType.Email, "test@example.com", "Support");

        var result = await _controller.CreateContactAsync(app.Id, request);

        result.Should().BeOfType<CreatedAtActionResult>();
    }

    [Fact]
    public async Task DeleteContact_Returns204()
    {
        var app = await SeedApplicationAsync();
        var contact = new ApplicationContact
        {
            ApplicationId = app.Id,
            Type = ContactType.Email,
            Value = "test@example.com"
        };
        _dbContext.ApplicationContacts.Add(contact);
        await _dbContext.SaveChangesAsync();

        var result = await _controller.DeleteContactAsync(app.Id, contact.Id);

        result.Should().BeOfType<NoContentResult>();
    }
}
