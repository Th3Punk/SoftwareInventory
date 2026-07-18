using AppInventory.Core.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;

namespace AppInventory.Api.Authorization;

/// <summary>
/// Enforces the RBAC evaluation order (spec 8.4): IsActive → Role check.
/// Runs after FeatureGateAttribute (Order=0) to ensure 501 precedes 403.
/// </summary>
[AttributeUsage(AttributeTargets.Class | AttributeTargets.Method, AllowMultiple = true)]
public sealed class RbacAuthorizeAttribute : Attribute, IAsyncActionFilter, IOrderedFilter
{
    private readonly string[] _roles;

    public int Order => 1;

    public RbacAuthorizeAttribute(params string[] roles)
    {
        _roles = roles;
    }

    public async Task OnActionExecutionAsync(ActionExecutingContext context, ActionExecutionDelegate next)
    {
        var user = context.HttpContext.User;

        if (user.Identity?.IsAuthenticated != true)
        {
            await next();
            return;
        }

        if (user.FindFirst(ClaimNames.IsActive)?.Value == "false")
        {
            context.Result = new ObjectResult(new ProblemDetails
            {
                Type = "https://tools.ietf.org/html/rfc7807",
                Title = "Forbidden",
                Status = 403,
                Detail = "User account is deactivated."
            })
            {
                StatusCode = 403,
                ContentTypes = { "application/problem+json" }
            };
            return;
        }

        if (_roles.Length > 0 && !_roles.Any(r => user.IsInRole(r)))
        {
            context.Result = new ObjectResult(new ProblemDetails
            {
                Type = "https://tools.ietf.org/html/rfc7807",
                Title = "Forbidden",
                Status = 403,
                Detail = "You do not have the required role to access this resource."
            })
            {
                StatusCode = 403,
                ContentTypes = { "application/problem+json" }
            };
            return;
        }

        await next();
    }
}
