using AppInventory.Api.Extensions;
using Serilog;

var builder = WebApplication.CreateBuilder(args);

builder.Host.UseSerilog((context, configuration) =>
    configuration.ReadFrom.Configuration(context.Configuration));

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
