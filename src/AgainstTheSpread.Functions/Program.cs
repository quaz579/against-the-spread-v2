using AgainstTheSpread.Core.Interfaces;
using AgainstTheSpread.Core.Services;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

var host = new HostBuilder()
    .ConfigureFunctionsWebApplication()
    .ConfigureServices(services =>
    {
        services.AddApplicationInsightsTelemetryWorkerService();

        // Register application services
        services.AddSingleton<IExcelService, ExcelService>();
        services.AddSingleton<IStorageService>(sp =>
        {
            // Use AZURE_STORAGE_CONNECTION_STRING for custom storage, fallback to AzureWebJobsStorage for local dev
            var connectionString = Environment.GetEnvironmentVariable("AZURE_STORAGE_CONNECTION_STRING")
                ?? Environment.GetEnvironmentVariable("AzureWebJobsStorage")
                ?? "UseDevelopmentStorage=true";
            var excelService = sp.GetRequiredService<IExcelService>();
            var logger = sp.GetRequiredService<ILogger<StorageService>>();
            return new StorageService(connectionString, excelService, logger);
        });
    })
    .Build();

host.Run();