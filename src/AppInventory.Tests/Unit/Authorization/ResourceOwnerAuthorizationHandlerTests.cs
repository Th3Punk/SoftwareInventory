using System.Security.Claims;
using AppInventory.Api.Authorization;
using FluentAssertions;
using Microsoft.AspNetCore.Authorization;

namespace AppInventory.Tests.Unit.Authorization;

public class ResourceOwnerAuthorizationHandlerTests
{
    private readonly ResourceOwnerAuthorizationHandler _handler = new();

    private static AuthorizationHandlerContext CreateContext(int? userId, int resourceOwnerId)
    {
        var claims = new List<Claim>();
        if (userId.HasValue)
        {
            claims.Add(new Claim(ClaimTypes.NameIdentifier, userId.Value.ToString()));
        }

        var identity = new ClaimsIdentity(claims, userId.HasValue ? "Test" : null);
        var user = new ClaimsPrincipal(identity);
        var requirement = new ResourceOwnerRequirement();

        return new AuthorizationHandlerContext([requirement], user, resourceOwnerId);
    }

    [Fact]
    public async Task OwnerMatchesUser_Succeeds()
    {
        var context = CreateContext(userId: 42, resourceOwnerId: 42);

        await _handler.HandleAsync(context);

        context.HasSucceeded.Should().BeTrue();
    }

    [Fact]
    public async Task OwnerDoesNotMatchUser_DoesNotSucceed()
    {
        var context = CreateContext(userId: 42, resourceOwnerId: 99);

        await _handler.HandleAsync(context);

        context.HasSucceeded.Should().BeFalse();
    }

    [Fact]
    public async Task NoUserIdClaim_DoesNotSucceed()
    {
        var context = CreateContext(userId: null, resourceOwnerId: 42);

        await _handler.HandleAsync(context);

        context.HasSucceeded.Should().BeFalse();
    }
}
