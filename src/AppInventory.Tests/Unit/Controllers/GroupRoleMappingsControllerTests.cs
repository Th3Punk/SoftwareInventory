using System.Security.Claims;
using AppInventory.Api.Controllers;
using AppInventory.Core.Entities;
using AppInventory.Core.Interfaces;
using AppInventory.Infrastructure.Data;
using FluentAssertions;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Moq;
using Xunit;

namespace AppInventory.Tests.Unit.Controllers;

public class GroupRoleMappingsControllerTests : IDisposable
{
    private readonly AppInventoryDbContext _dbContext;
    private readonly Mock<IAuditProvider> _audit;
    private readonly GroupRoleMappingsController _controller;

    public GroupRoleMappingsControllerTests()
    {
        var options = new DbContextOptionsBuilder<AppInventoryDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;

        _dbContext = new AppInventoryDbContext(options);
        _audit = new Mock<IAuditProvider>();
        _controller = new GroupRoleMappingsController(_dbContext, _audit.Object);
        _controller.ControllerContext = BuildContext("Admin");
    }

    [Fact]
    public async Task ListAsync_ReturnsOk_ForAdmin()
    {
        SeedRole();
        var result = await _controller.ListAsync(null, CancellationToken.None);
        result.Should().BeOfType<OkObjectResult>();
    }

    [Fact]
    public async Task ListAsync_Returns403_ForNonAdmin()
    {
        _controller.ControllerContext = BuildContext("Developer");
        var result = await _controller.ListAsync(null, CancellationToken.None);
        result.Should().BeOfType<ObjectResult>().Which.StatusCode.Should().Be(403);
    }

    [Fact]
    public async Task CreateAsync_ReturnsCreated_WithValidRequest()
    {
        var role = SeedRole();

        var req = new CreateGroupRoleMappingRequest(AuthProviderType.ActiveDirectory, "CN=Developers,DC=company,DC=com", role.Id, null);
        var result = await _controller.CreateAsync(req, CancellationToken.None);

        result.Should().BeOfType<CreatedAtActionResult>()
            .Which.StatusCode.Should().Be(201);

        var dto = ((CreatedAtActionResult)result).Value as GroupRoleMappingDto;
        dto!.ExternalGroupRef.Should().Be("CN=Developers,DC=company,DC=com");
        dto.RoleId.Should().Be(role.Id);
    }

    [Fact]
    public async Task CreateAsync_Returns404_WhenRoleNotFound()
    {
        var req = new CreateGroupRoleMappingRequest(AuthProviderType.Local, "group", 999, null);
        var result = await _controller.CreateAsync(req, CancellationToken.None);
        result.Should().BeOfType<ObjectResult>().Which.StatusCode.Should().Be(404);
    }

    [Fact]
    public async Task CreateAsync_Returns409_WhenDuplicateMapping()
    {
        var role = SeedRole();
        _dbContext.GroupRoleMappings.Add(new GroupRoleMapping
        {
            ProviderType = AuthProviderType.ActiveDirectory,
            ExternalGroupRef = "CN=Admins",
            RoleId = role.Id
        });
        await _dbContext.SaveChangesAsync();

        var req = new CreateGroupRoleMappingRequest(AuthProviderType.ActiveDirectory, "CN=Admins", role.Id, null);
        var result = await _controller.CreateAsync(req, CancellationToken.None);
        result.Should().BeOfType<ObjectResult>().Which.StatusCode.Should().Be(409);
    }

    [Fact]
    public async Task DeleteAsync_ReturnsNoContent_WhenExists()
    {
        var role = SeedRole();
        var mapping = new GroupRoleMapping
        {
            ProviderType = AuthProviderType.Local,
            ExternalGroupRef = "group-a",
            RoleId = role.Id
        };
        _dbContext.GroupRoleMappings.Add(mapping);
        await _dbContext.SaveChangesAsync();

        var result = await _controller.DeleteAsync(mapping.Id, CancellationToken.None);
        result.Should().BeOfType<NoContentResult>();
    }

    [Fact]
    public async Task DeleteAsync_Returns404_WhenNotFound()
    {
        var result = await _controller.DeleteAsync(999, CancellationToken.None);
        result.Should().BeOfType<ObjectResult>().Which.StatusCode.Should().Be(404);
    }

    private Role SeedRole()
    {
        var role = new Role { Name = "TestRole", IsSystemRole = false };
        _dbContext.Roles.Add(role);
        _dbContext.SaveChanges();
        return role;
    }

    private static ControllerContext BuildContext(string role)
    {
        var claims = new List<Claim>
        {
            new(ClaimTypes.NameIdentifier, "1"),
            new(ClaimTypes.Role, role)
        };
        var identity = new ClaimsIdentity(claims, "Test");
        var principal = new ClaimsPrincipal(identity);

        return new ControllerContext
        {
            HttpContext = new DefaultHttpContext { User = principal }
        };
    }

    public void Dispose() => _dbContext.Dispose();
}
