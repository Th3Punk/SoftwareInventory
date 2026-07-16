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

public class UsersControllerTests : IDisposable
{
    private readonly AppInventoryDbContext _dbContext;
    private readonly Mock<IAuditProvider> _audit;
    private readonly UsersController _controller;

    public UsersControllerTests()
    {
        var options = new DbContextOptionsBuilder<AppInventoryDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;

        _dbContext = new AppInventoryDbContext(options);
        _audit = new Mock<IAuditProvider>();
        _controller = new UsersController(_dbContext, _audit.Object);
        _controller.ControllerContext = BuildContext("Admin");
    }

    [Fact]
    public async Task ListAsync_Returns403_ForNonAdmin()
    {
        _controller.ControllerContext = BuildContext("ReadOnly");
        var result = await _controller.ListAsync(null, null, 1, 20, CancellationToken.None);
        result.Should().BeOfType<ObjectResult>().Which.StatusCode.Should().Be(403);
    }

    [Fact]
    public async Task ListAsync_ReturnsAllUsers_ForAdmin()
    {
        _dbContext.Users.AddRange(
            new User { DisplayName = "Alice", Email = "alice@test.com", CreatedAt = DateTime.UtcNow },
            new User { DisplayName = "Bob", Email = "bob@test.com", CreatedAt = DateTime.UtcNow });
        await _dbContext.SaveChangesAsync();

        var result = await _controller.ListAsync(null, null, 1, 20, CancellationToken.None);
        var ok = result.Should().BeOfType<OkObjectResult>().Subject;
        var paged = ok.Value as PagedResponse<AdminUserDto>;
        paged!.TotalCount.Should().Be(2);
    }

    [Fact]
    public async Task GetAsync_ReturnsUser_WhenExists()
    {
        var user = new User { DisplayName = "Carol", Email = "carol@test.com", CreatedAt = DateTime.UtcNow };
        _dbContext.Users.Add(user);
        await _dbContext.SaveChangesAsync();

        var result = await _controller.GetAsync(user.Id, CancellationToken.None);
        var ok = result.Should().BeOfType<OkObjectResult>().Subject;
        var dto = ok.Value as AdminUserDto;
        dto!.Email.Should().Be("carol@test.com");
    }

    [Fact]
    public async Task GetAsync_Returns404_WhenNotFound()
    {
        var result = await _controller.GetAsync(999, CancellationToken.None);
        result.Should().BeOfType<ObjectResult>().Which.StatusCode.Should().Be(404);
    }

    [Fact]
    public async Task PatchActiveAsync_TogglesActiveState()
    {
        var user = new User { DisplayName = "Dave", Email = "dave@test.com", IsActive = true, CreatedAt = DateTime.UtcNow };
        _dbContext.Users.Add(user);
        await _dbContext.SaveChangesAsync();

        var result = await _controller.PatchActiveAsync(user.Id, new PatchUserActiveRequest(false), CancellationToken.None);
        result.Should().BeOfType<OkResult>();

        var updated = await _dbContext.Users.FindAsync(user.Id);
        updated!.IsActive.Should().BeFalse();
    }

    [Fact]
    public async Task AssignRoleAsync_AssignsManualRole()
    {
        var user = new User { DisplayName = "Eve", Email = "eve@test.com", CreatedAt = DateTime.UtcNow };
        var role = new Role { Name = "Developer", IsSystemRole = true };
        _dbContext.Users.Add(user);
        _dbContext.Roles.Add(role);
        await _dbContext.SaveChangesAsync();

        var result = await _controller.AssignRoleAsync(user.Id, new AssignRoleRequest(role.Id), CancellationToken.None);
        result.Should().BeOfType<ObjectResult>().Which.StatusCode.Should().Be(201);

        var exists = await _dbContext.UserRoles.AnyAsync(ur => ur.UserId == user.Id && ur.RoleId == role.Id);
        exists.Should().BeTrue();
    }

    [Fact]
    public async Task AssignRoleAsync_Returns400_WhenAlreadyAssigned()
    {
        var user = new User { DisplayName = "Frank", Email = "frank@test.com", CreatedAt = DateTime.UtcNow };
        var role = new Role { Name = "ReadOnly", IsSystemRole = true };
        _dbContext.Users.Add(user);
        _dbContext.Roles.Add(role);
        await _dbContext.SaveChangesAsync();

        _dbContext.UserRoles.Add(new UserRole { UserId = user.Id, RoleId = role.Id, GrantedAt = DateTime.UtcNow, Source = RoleGrantSource.Manual });
        await _dbContext.SaveChangesAsync();

        var result = await _controller.AssignRoleAsync(user.Id, new AssignRoleRequest(role.Id), CancellationToken.None);
        result.Should().BeOfType<ObjectResult>().Which.StatusCode.Should().Be(400);
    }

    [Fact]
    public async Task RemoveRoleAsync_RemovesManualRole()
    {
        var user = new User { DisplayName = "Grace", Email = "grace@test.com", CreatedAt = DateTime.UtcNow };
        var role = new Role { Name = "Admin", IsSystemRole = true };
        _dbContext.Users.Add(user);
        _dbContext.Roles.Add(role);
        await _dbContext.SaveChangesAsync();

        _dbContext.UserRoles.Add(new UserRole { UserId = user.Id, RoleId = role.Id, GrantedAt = DateTime.UtcNow, Source = RoleGrantSource.Manual });
        await _dbContext.SaveChangesAsync();

        var result = await _controller.RemoveRoleAsync(user.Id, role.Id, CancellationToken.None);
        result.Should().BeOfType<NoContentResult>();

        var stillExists = await _dbContext.UserRoles.AnyAsync(ur => ur.UserId == user.Id && ur.RoleId == role.Id);
        stillExists.Should().BeFalse();
    }

    [Fact]
    public async Task RemoveRoleAsync_Returns400_ForGroupMappedRole()
    {
        var user = new User { DisplayName = "Henry", Email = "henry@test.com", CreatedAt = DateTime.UtcNow };
        var role = new Role { Name = "Developer", IsSystemRole = true };
        _dbContext.Users.Add(user);
        _dbContext.Roles.Add(role);
        await _dbContext.SaveChangesAsync();

        _dbContext.UserRoles.Add(new UserRole { UserId = user.Id, RoleId = role.Id, GrantedAt = DateTime.UtcNow, Source = RoleGrantSource.GroupMapping });
        await _dbContext.SaveChangesAsync();

        var result = await _controller.RemoveRoleAsync(user.Id, role.Id, CancellationToken.None);
        result.Should().BeOfType<ObjectResult>().Which.StatusCode.Should().Be(400);
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
