using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;

namespace AppInventory.Api.Middleware;

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
