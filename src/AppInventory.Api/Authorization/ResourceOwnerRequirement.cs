using Microsoft.AspNetCore.Authorization;

namespace AppInventory.Api.Authorization;

public sealed class ResourceOwnerRequirement : IAuthorizationRequirement;

public sealed class ResourceOwnerAuthorizationHandler : AuthorizationHandler<ResourceOwnerRequirement, int>
{
    protected override Task HandleRequirementAsync(
        AuthorizationHandlerContext context,
        ResourceOwnerRequirement requirement,
        int ownerUserId)
    {
        var userIdClaim = context.User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier);

        if (userIdClaim is null)
            return Task.CompletedTask;

        if (!int.TryParse(userIdClaim.Value, out var userId))
            return Task.CompletedTask;

        if (userId == ownerUserId)
        {
            context.Succeed(requirement);
        }

        return Task.CompletedTask;
    }
}
