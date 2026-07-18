using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;

namespace AppInventory.Api.Middleware;

/// <summary>
/// Returns 501 when the specified feature is disabled (spec 8.4 step 2).
/// Order=0 ensures this runs before RbacAuthorizeAttribute (Order=1).
/// </summary>
[AttributeUsage(AttributeTargets.Class | AttributeTargets.Method)]
public sealed class FeatureGateAttribute : Attribute, IAsyncActionFilter, IOrderedFilter
{
    private readonly string _featureName;

    public int Order => 0;

    public FeatureGateAttribute(string featureName)
    {
        _featureName = featureName;
    }

    public async Task OnActionExecutionAsync(ActionExecutingContext context, ActionExecutionDelegate next)
    {
        var configuration = context.HttpContext.RequestServices.GetRequiredService<IConfiguration>();
        var enabled = configuration.GetValue<bool>($"Features:{_featureName}:Enabled");

        if (!enabled)
        {
            context.Result = new ObjectResult(new ProblemDetails
            {
                Type = "https://tools.ietf.org/html/rfc7807",
                Title = "Feature Not Available",
                Status = 501,
                Detail = $"The '{_featureName}' feature is not enabled.",
                Extensions = { ["feature"] = _featureName }
            })
            {
                StatusCode = 501,
                ContentTypes = { "application/problem+json" }
            };
            return;
        }

        await next();
    }
}
