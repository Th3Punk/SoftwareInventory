using System.Security.Claims;
using AppInventory.Core.Authorization;
using AppInventory.Infrastructure.Authorization;
using FluentAssertions;

namespace AppInventory.Tests.Unit.Authorization;

public class QueryAuthorizationFilterTests
{
    private static ClaimsPrincipal CreateUser(params string[] roles)
    {
        var claims = new List<Claim>
        {
            new(ClaimTypes.NameIdentifier, "1")
        };
        claims.AddRange(roles.Select(r => new Claim(ClaimTypes.Role, r)));

        return new ClaimsPrincipal(new ClaimsIdentity(claims, "Test"));
    }

    [Theory]
    [InlineData(RoleNames.Developer, true)]
    [InlineData(RoleNames.Admin, true)]
    [InlineData(RoleNames.ApplicationOwner, false)]
    [InlineData(RoleNames.ReadOnly, false)]
    public void HasDeveloperAccess_ReturnsExpected(string role, bool expected)
    {
        var user = CreateUser(role);
        QueryAuthorizationFilter.HasDeveloperAccess(user).Should().Be(expected);
    }

    [Theory]
    [InlineData(RoleNames.Admin, true)]
    [InlineData(RoleNames.Developer, false)]
    [InlineData(RoleNames.ApplicationOwner, false)]
    [InlineData(RoleNames.ReadOnly, false)]
    public void HasAdminAccess_ReturnsExpected(string role, bool expected)
    {
        var user = CreateUser(role);
        QueryAuthorizationFilter.HasAdminAccess(user).Should().Be(expected);
    }

    [Theory]
    [InlineData(RoleNames.ApplicationOwner, true)]
    [InlineData(RoleNames.Developer, true)]
    [InlineData(RoleNames.Admin, true)]
    [InlineData(RoleNames.ReadOnly, false)]
    public void CanViewNonPublicEnvironments_ReturnsExpected(string role, bool expected)
    {
        var user = CreateUser(role);
        QueryAuthorizationFilter.CanViewNonPublicEnvironments(user).Should().Be(expected);
    }

    [Fact]
    public void GetUserId_ReturnsId()
    {
        var user = CreateUser(RoleNames.Admin);
        QueryAuthorizationFilter.GetUserId(user).Should().Be(1);
    }

    [Fact]
    public void GetUserId_NoClaimReturnsNull()
    {
        var user = new ClaimsPrincipal(new ClaimsIdentity());
        QueryAuthorizationFilter.GetUserId(user).Should().BeNull();
    }
}
