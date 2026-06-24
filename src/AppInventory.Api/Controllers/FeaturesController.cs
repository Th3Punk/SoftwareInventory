using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace AppInventory.Api.Controllers;

/// <summary>
/// Public endpoint exposing active feature flags for frontend and integrations.
/// </summary>
[ApiController]
[Route("api/v1/config")]
public class FeaturesController : ControllerBase
{
    private readonly IConfiguration _configuration;

    public FeaturesController(IConfiguration configuration)
    {
        _configuration = configuration;
    }

    /// <summary>
    /// Returns the current feature flag configuration.
    /// </summary>
    /// <returns>Active features with their settings.</returns>
    /// <response code="200">Feature flags returned successfully.</response>
    [HttpGet("features")]
    [AllowAnonymous]
    [ProducesResponseType(typeof(Dictionary<string, object>), StatusCodes.Status200OK)]
    public IActionResult GetFeatures()
    {
        var featuresSection = _configuration.GetSection("Features");
        var result = new Dictionary<string, object>();

        foreach (var feature in featuresSection.GetChildren())
        {
            var featureDict = new Dictionary<string, object>();
            var enabled = feature.GetValue<bool>("Enabled");
            featureDict["enabled"] = enabled;

            foreach (var setting in feature.GetChildren())
            {
                if (setting.Key == "Enabled")
                    continue;

                if (setting.Value is not null)
                    featureDict[ToCamelCase(setting.Key)] = setting.Value;
            }

            result[ToCamelCase(feature.Key)] = featureDict;
        }

        return Ok(result);
    }

    private static string ToCamelCase(string value)
        => string.IsNullOrEmpty(value) ? value : char.ToLowerInvariant(value[0]) + value[1..];
}
