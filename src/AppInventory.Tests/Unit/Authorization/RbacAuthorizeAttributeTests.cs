using System.Security.Claims;
using AppInventory.Api.Authorization;
using AppInventory.Core.Authorization;
using FluentAssertions;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Abstractions;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.AspNetCore.Routing;

namespace AppInventory.Tests.Unit.Authorization;

public class RbacAuthorizeAttributeTests
{
    private static ActionExecutingContext CreateContext(ClaimsPrincipal user)
    {
        var httpContext = new DefaultHttpContext { User = user };
        var actionContext = new ActionContext(httpContext, new RouteData(), new ActionDescriptor());
        return new ActionExecutingContext(
            actionContext,
            [],
            new Dictionary<string, object?>(),
            new object());
    }

    private static ClaimsPrincipal CreateUser(bool isActive, params string[] roles)
    {
        var claims = new List<Claim>
        {
            new(ClaimTypes.NameIdentifier, "1"),
            new(ClaimNames.IsActive, isActive.ToString().ToLowerInvariant())
        };
        claims.AddRange(roles.Select(r => new Claim(ClaimTypes.Role, r)));

        var identity = new ClaimsIdentity(claims, "Test");
        return new ClaimsPrincipal(identity);
    }

    [Fact]
    public async Task InactiveUser_Returns403()
    {
        var attr = new RbacAuthorizeAttribute(RoleNames.Admin);
        var context = CreateContext(CreateUser(isActive: false, RoleNames.Admin));
        var called = false;

        await attr.OnActionExecutionAsync(context, () =>
        {
            called = true;
            return Task.FromResult<ActionExecutedContext>(null!);
        });

        called.Should().BeFalse();
        context.Result.Should().BeOfType<ObjectResult>()
            .Which.StatusCode.Should().Be(403);
    }

    [Fact]
    public async Task ActiveUserWithCorrectRole_Succeeds()
    {
        var attr = new RbacAuthorizeAttribute(RoleNames.Developer, RoleNames.Admin);
        var context = CreateContext(CreateUser(isActive: true, RoleNames.Developer));
        var called = false;

        await attr.OnActionExecutionAsync(context, () =>
        {
            called = true;
            return Task.FromResult<ActionExecutedContext>(null!);
        });

        called.Should().BeTrue();
        context.Result.Should().BeNull();
    }

    [Fact]
    public async Task ActiveUserWithWrongRole_Returns403()
    {
        var attr = new RbacAuthorizeAttribute(RoleNames.Admin);
        var context = CreateContext(CreateUser(isActive: true, RoleNames.ReadOnly));
        var called = false;

        await attr.OnActionExecutionAsync(context, () =>
        {
            called = true;
            return Task.FromResult<ActionExecutedContext>(null!);
        });

        called.Should().BeFalse();
        context.Result.Should().BeOfType<ObjectResult>()
            .Which.StatusCode.Should().Be(403);
    }

    [Fact]
    public async Task NoRoleRequirement_OnlyChecksIsActive()
    {
        var attr = new RbacAuthorizeAttribute();
        var context = CreateContext(CreateUser(isActive: true, RoleNames.ReadOnly));
        var called = false;

        await attr.OnActionExecutionAsync(context, () =>
        {
            called = true;
            return Task.FromResult<ActionExecutedContext>(null!);
        });

        called.Should().BeTrue();
    }

    [Fact]
    public async Task UnauthenticatedUser_PassesThrough()
    {
        var attr = new RbacAuthorizeAttribute(RoleNames.Admin);
        var context = CreateContext(new ClaimsPrincipal(new ClaimsIdentity()));
        var called = false;

        await attr.OnActionExecutionAsync(context, () =>
        {
            called = true;
            return Task.FromResult<ActionExecutedContext>(null!);
        });

        called.Should().BeTrue();
    }

    [Fact]
    public void Order_IsOneToRunAfterFeatureGate()
    {
        var attr = new RbacAuthorizeAttribute(RoleNames.Admin);
        attr.Order.Should().Be(1);
    }
}
