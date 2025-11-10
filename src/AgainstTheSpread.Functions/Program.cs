using AgainstTheSpread.Core.Interfaces;
using AgainstTheSpread.Core.Services;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Builder;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

var builder = FunctionsApplication.CreateBuilder(args);

builder.ConfigureFunctionsWebApplication();

// Register application services
builder.Services.AddSingleton<IExcelService, ExcelService>();
builder.Services.AddSingleton<IStorageService>(sp =>
{
    var connectionString = Environment.GetEnvironmentVariable("AzureWebJobsStorage")
        ?? "UseDevelopmentStorage=true";
    var excelService = sp.GetRequiredService<IExcelService>();
    return new StorageService(connectionString, excelService);
});

// Configure CORS for Blazor app
builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowBlazorApp", policy =>
    {
        policy.WithOrigins(
            "http://localhost:5000",
            "http://localhost:5158",
            "https://localhost:5001",
            "https://localhost:7103",
            "https://*.azurestaticapps.net"
        )
        .AllowAnyMethod()
        .AllowAnyHeader()
        .AllowCredentials();
    });
});

builder.Services
    .AddApplicationInsightsTelemetryWorkerService()
    .ConfigureFunctionsApplicationInsights();

builder.Build().Run();
