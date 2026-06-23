using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;

namespace AppInventory.Api.Middleware;

/// <summary>
/// Action filter that returns 501 Not Implemented when the specified feature is disabled.
/// </summary>
[AttributeUsage(AttributeTargets.Class | AttributeTargets.Method)]
public sealed class FeatureGateAttribute : Attribute, IAsyncActionFilter
{
    private readonly string _featureName;

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
                Title = "Feature Not Available",
                Status = StatusCodes.Status501NotImplemented,
                Detail = $"The {_featureName} feature is currently disabled.",
                Extensions = { ["feature"] = _featureName }
            })
            {
                StatusCode = StatusCodes.Status501NotImplemented,
                ContentTypes = { "application/problem+json" }
            };
            return;
        }

        await next();
    }
}
