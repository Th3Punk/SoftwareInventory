using AppInventory.Api.Extensions;
using AppInventory.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.FeatureManagement;
using Scalar.AspNetCore;
using Serilog;

var builder = WebApplication.CreateBuilder(args);

builder.Host.UseSerilog((context, configuration) =>
    configuration.ReadFrom.Configuration(context.Configuration));

builder.Services.AddDbContext<AppInventoryDbContext>(options =>
    options.UseNpgsql(builder.Configuration.GetConnectionString("Default")));

builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddOpenApi();
builder.Services.AddFeatureManagement();

builder.Services.AddSearchProvider(builder.Configuration);
builder.Services.AddAuditProvider(builder.Configuration);
builder.Services.AddNotificationProvider(builder.Configuration);
builder.Services.AddDocumentStoreProvider(builder.Configuration);
builder.Services.AddMcpServerFeature(builder.Configuration);

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
    app.MapScalarApiReference();
}

app.UseSerilogRequestLogging();
app.MapControllers();

app.Run();
