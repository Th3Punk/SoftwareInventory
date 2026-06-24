using System.Security.Claims;
using AppInventory.Api.Controllers;
using AppInventory.Core.Authorization;
using AppInventory.Core.Entities;
using AppInventory.Infrastructure.Data;
using FluentAssertions;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace AppInventory.Tests.Unit.Controllers;

public class TagsControllerTests : IDisposable
{
    private readonly AppInventoryDbContext _dbContext;
    private readonly TagsController _controller;

    public TagsControllerTests()
    {
        var options = new DbContextOptionsBuilder<AppInventoryDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;

        _dbContext = new AppInventoryDbContext(options);
        _controller = new TagsController(_dbContext);
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

    [Fact]
    public async Task List_ReturnsAllTags()
    {
        _dbContext.Tags.AddRange(
            new Tag { Name = "java", Color = "#f89820" },
            new Tag { Name = "dotnet", Color = "#512bd4" });
        await _dbContext.SaveChangesAsync();

        var result = await _controller.ListAsync();

        var ok = result.Should().BeOfType<OkObjectResult>().Subject;
        var tags = ok.Value.Should().BeAssignableTo<List<TagDto>>().Subject;
        tags.Should().HaveCount(2);
        tags[0].Name.Should().Be("dotnet");
        tags[1].Name.Should().Be("java");
    }

    [Fact]
    public async Task Create_NormalizesToLowercase()
    {
        var result = await _controller.CreateAsync(new CreateTagRequest("JavaScript", "#f7df1e"));

        var created = result.Should().BeOfType<CreatedAtActionResult>().Subject;
        var tag = created.Value.Should().BeOfType<TagDto>().Subject;
        tag.Name.Should().Be("javascript");
    }

    [Fact]
    public async Task Create_Returns409ForDuplicate()
    {
        _dbContext.Tags.Add(new Tag { Name = "java" });
        await _dbContext.SaveChangesAsync();

        var result = await _controller.CreateAsync(new CreateTagRequest("Java", null));

        var obj = result.Should().BeOfType<ObjectResult>().Subject;
        obj.StatusCode.Should().Be(409);
    }

    [Fact]
    public async Task Delete_RemovesTag()
    {
        var tag = new Tag { Name = "obsolete" };
        _dbContext.Tags.Add(tag);
        await _dbContext.SaveChangesAsync();

        var result = await _controller.DeleteAsync(tag.Id);

        result.Should().BeOfType<NoContentResult>();
        (await _dbContext.Tags.CountAsync()).Should().Be(0);
    }

    [Fact]
    public async Task Delete_Returns404ForMissing()
    {
        var result = await _controller.DeleteAsync(999);

        var obj = result.Should().BeOfType<ObjectResult>().Subject;
        obj.StatusCode.Should().Be(404);
    }
}
