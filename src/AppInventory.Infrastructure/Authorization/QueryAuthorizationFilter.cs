using System.Security.Claims;
using AppInventory.Core.Authorization;

namespace AppInventory.Infrastructure.Authorization;

public static class QueryAuthorizationFilter
{
    public static bool HasDeveloperAccess(ClaimsPrincipal user)
    {
        return user.IsInRole(RoleNames.Developer) || user.IsInRole(RoleNames.Admin);
    }

    public static bool HasAdminAccess(ClaimsPrincipal user)
    {
        return user.IsInRole(RoleNames.Admin);
    }

    public static bool CanViewNonPublicEnvironments(ClaimsPrincipal user)
    {
        return user.IsInRole(RoleNames.ApplicationOwner) ||
               user.IsInRole(RoleNames.Developer) ||
               user.IsInRole(RoleNames.Admin);
    }

    public static int? GetUserId(ClaimsPrincipal user)
    {
        var claim = user.FindFirstValue(ClaimTypes.NameIdentifier);
        return int.TryParse(claim, out var id) ? id : null;
    }
}
