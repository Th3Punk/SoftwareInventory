using AppInventory.Api.Extensions;
using AppInventory.Api.Middleware;
using Microsoft.AspNetCore.Authentication;
using Serilog;

var builder = WebApplication.CreateBuilder(args);

builder.Host.UseSerilog((context, configuration) =>
    configuration.ReadFrom.Configuration(context.Configuration));

builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddOpenApi();
builder.Services.AddFeatureManagement();

builder.Services.AddAppDatabase(builder.Configuration);

builder.Services.AddAuthentication(CookieSessionDefaults.AuthenticationScheme)
    .AddScheme<AuthenticationSchemeOptions, CookieSessionAuthHandler>(
        CookieSessionDefaults.AuthenticationScheme, null);
builder.Services.AddAuthorization();

builder.Services.AddAuthProvider(builder.Configuration);
builder.Services.AddSearchProvider(builder.Configuration);
builder.Services.AddAuditProvider(builder.Configuration);
builder.Services.AddNotificationProvider(builder.Configuration);
builder.Services.AddDocumentStoreProvider(builder.Configuration);
builder.Services.AddMcpToolset(builder.Configuration);

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
    app.MapScalarApiReference();
}

app.UseSerilogRequestLogging();
app.UseAuthentication();
app.UseAuthorization();
app.UseMiddleware<MustChangePasswordMiddleware>();
app.MapControllers();

app.Run();
