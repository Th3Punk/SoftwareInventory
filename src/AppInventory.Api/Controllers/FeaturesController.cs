using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace AppInventory.Api.Controllers;

/// <summary>
/// Exposes feature flag status for the frontend.
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
    /// Returns the enabled/disabled status of all features.
    /// </summary>
    /// <response code="200">Feature flag status.</response>
    [HttpGet("features")]
    [AllowAnonymous]
    [ProducesResponseType(typeof(Dictionary<string, object>), 200)]
    public IActionResult GetFeatures()
    {
        var featuresSection = _configuration.GetSection("Features");
        var result = new Dictionary<string, object>();

        foreach (var child in featuresSection.GetChildren())
        {
            var featureConfig = new Dictionary<string, object>();
            foreach (var prop in child.GetChildren())
            {
                featureConfig[ToCamelCase(prop.Key)] = prop.Value ?? "";
            }
            result[ToCamelCase(child.Key)] = featureConfig;
        }

        return Ok(result);
    }

    private static string ToCamelCase(string value)
    {
        if (string.IsNullOrEmpty(value) || char.IsLower(value[0]))
            return value;
        return char.ToLowerInvariant(value[0]) + value[1..];
    }
}
