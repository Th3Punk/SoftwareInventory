using AppInventory.Core.Authorization;

namespace AppInventory.Api.Middleware;

public sealed class MustChangePasswordMiddleware
{
    private readonly RequestDelegate _next;

    private static readonly HashSet<string> _allowedPaths = new(StringComparer.OrdinalIgnoreCase)
    {
        "/api/v1/auth/change-password",
        "/api/v1/auth/logout",
        "/api/v1/auth/me",
        "/health"
    };

    public MustChangePasswordMiddleware(RequestDelegate next)
    {
        _next = next;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        if (context.User.Identity?.IsAuthenticated == true &&
            context.User.HasClaim(ClaimNames.MustChangePassword, "true") &&
            !_allowedPaths.Contains(context.Request.Path.Value ?? ""))
        {
            context.Response.StatusCode = 403;
            context.Response.ContentType = "application/problem+json";
            await context.Response.WriteAsJsonAsync(new
            {
                type = "https://tools.ietf.org/html/rfc7807",
                title = "Password Change Required",
                status = 403,
                detail = "You must change your password before continuing."
            });
            return;
        }

        await _next(context);
    }
}
