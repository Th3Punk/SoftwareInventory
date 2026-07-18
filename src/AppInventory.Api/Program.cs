using AppInventory.Api.Extensions;
using AppInventory.Api.Middleware;
using Microsoft.AspNetCore.Authentication;
using Microsoft.FeatureManagement;
using ModelContextProtocol.AspNetCore;
using Scalar.AspNetCore;
using Serilog;

var builder = WebApplication.CreateBuilder(args);

builder.Host.UseSerilog((context, configuration) =>
    configuration.ReadFrom.Configuration(context.Configuration));

builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();

var openApiEnabled = builder.Configuration.GetValue<bool>("Features:OpenApiUi:Enabled");
if (openApiEnabled || builder.Environment.IsDevelopment())
{
    builder.Services.AddOpenApi(options =>
    {
        options.AddDocumentTransformer((document, context, ct) =>
        {
            document.Info.Title = "SoftwareInventory API";
            document.Info.Version = "v1";
            document.Info.Description = "Software inventory management API. Auth required for most endpoints.";
            return Task.CompletedTask;
        });
    });
}

builder.Services.AddFeatureManagement();

builder.Services.AddAppDatabase(builder.Configuration);

builder.Services.AddAuthentication(CookieSessionDefaults.AuthenticationScheme)
    .AddScheme<AuthenticationSchemeOptions, CookieSessionAuthHandler>(
        CookieSessionDefaults.AuthenticationScheme, null);
builder.Services.AddRbacAuthorization();

builder.Services.AddAuthProvider(builder.Configuration);
builder.Services.AddSearchProvider(builder.Configuration);
builder.Services.AddAuditProvider(builder.Configuration);
builder.Services.AddNotificationProvider(builder.Configuration);
builder.Services.AddDocumentStoreProvider(builder.Configuration);
builder.Services.AddMcpToolset(builder.Configuration);

var app = builder.Build();

if (openApiEnabled || app.Environment.IsDevelopment())
{
    app.MapOpenApi();
    app.MapScalarApiReference(options =>
    {
        options.Title = "SoftwareInventory API";
    });
}

app.UseSerilogRequestLogging();
app.UseAuthentication();
app.UseAuthorization();
app.UseMiddleware<MustChangePasswordMiddleware>();
app.MapControllers();

if (app.Configuration.GetValue<bool>("Features:Mcp:Enabled"))
{
    app.MapMcp("/mcp").RequireAuthorization("McpAccess");
}

app.Run();
