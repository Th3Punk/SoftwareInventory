using AppInventory.Api.Authorization;
using AppInventory.Core.Authorization;
using Microsoft.AspNetCore.Authorization;

namespace AppInventory.Api.Extensions;

public static class AuthorizationServiceExtensions
{
    public static IServiceCollection AddRbacAuthorization(this IServiceCollection services)
    {
        services.AddAuthorization(options =>
        {
            options.AddPolicy(PolicyNames.ApplicationRead, policy =>
                policy.RequireAuthenticatedUser()
                    .RequireRole(RoleNames.ReadOnly, RoleNames.ApplicationOwner, RoleNames.Developer, RoleNames.Admin));

            options.AddPolicy(PolicyNames.ApplicationCreate, policy =>
                policy.RequireAuthenticatedUser()
                    .RequireRole(RoleNames.Developer, RoleNames.Admin));

            options.AddPolicy(PolicyNames.ApplicationEditOwn, policy =>
                policy.RequireAuthenticatedUser()
                    .RequireRole(RoleNames.ApplicationOwner, RoleNames.Developer, RoleNames.Admin));

            options.AddPolicy(PolicyNames.ApplicationEditAny, policy =>
                policy.RequireAuthenticatedUser()
                    .RequireRole(RoleNames.Admin));

            options.AddPolicy(PolicyNames.ApplicationDelete, policy =>
                policy.RequireAuthenticatedUser()
                    .RequireRole(RoleNames.Admin));

            options.AddPolicy(PolicyNames.UserDocRead, policy =>
                policy.RequireAuthenticatedUser()
                    .RequireRole(RoleNames.ReadOnly, RoleNames.ApplicationOwner, RoleNames.Developer, RoleNames.Admin));

            options.AddPolicy(PolicyNames.UserDocWrite, policy =>
                policy.RequireAuthenticatedUser()
                    .RequireRole(RoleNames.ApplicationOwner, RoleNames.Developer, RoleNames.Admin));

            options.AddPolicy(PolicyNames.DeveloperDocRead, policy =>
                policy.RequireAuthenticatedUser()
                    .RequireRole(RoleNames.Developer, RoleNames.Admin));

            options.AddPolicy(PolicyNames.DeveloperDocWrite, policy =>
                policy.RequireAuthenticatedUser()
                    .RequireRole(RoleNames.Developer, RoleNames.Admin));

            options.AddPolicy(PolicyNames.OpsDocReadWrite, policy =>
                policy.RequireAuthenticatedUser()
                    .RequireRole(RoleNames.Admin));

            options.AddPolicy(PolicyNames.AuditLogRead, policy =>
                policy.RequireAuthenticatedUser()
                    .RequireRole(RoleNames.Admin));

            options.AddPolicy(PolicyNames.GroupMappingManage, policy =>
                policy.RequireAuthenticatedUser()
                    .RequireRole(RoleNames.Admin));

            options.AddPolicy(PolicyNames.UserManage, policy =>
                policy.RequireAuthenticatedUser()
                    .RequireRole(RoleNames.Admin));

            options.AddPolicy(PolicyNames.NonPublicEnvironments, policy =>
                policy.RequireAuthenticatedUser()
                    .RequireRole(RoleNames.ApplicationOwner, RoleNames.Developer, RoleNames.Admin));
        });

        services.AddSingleton<IAuthorizationHandler, ResourceOwnerAuthorizationHandler>();

        return services;
    }
}
