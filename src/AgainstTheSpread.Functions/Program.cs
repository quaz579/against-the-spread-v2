using AgainstTheSpread.Core.Interfaces;
using AgainstTheSpread.Core.Services;
using AgainstTheSpread.Data;
using AgainstTheSpread.Data.Interfaces;
using AgainstTheSpread.Data.Services;
using Microsoft.Azure.Functions.Worker;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

var host = new HostBuilder()
    .ConfigureFunctionsWebApplication()
    .ConfigureServices(services =>
    {
        services.AddApplicationInsightsTelemetryWorkerService();

        // Register Entity Framework DbContext
        var sqlConnectionString = Environment.GetEnvironmentVariable("SqlConnectionString");
        if (!string.IsNullOrEmpty(sqlConnectionString))
        {
            services.AddDbContext<AtsDbContext>(options =>
                options.UseSqlServer(sqlConnectionString));

            // Register data services (only when database is configured)
            services.AddScoped<IUserService, UserService>();
            services.AddScoped<IGameService, GameService>();
            services.AddScoped<IPickService, PickService>();
        }

        // Register application services
        services.AddSingleton<IExcelService, ExcelService>();
        services.AddSingleton<IBowlExcelService, BowlExcelService>();
        services.AddSingleton<IStorageService>(sp =>
        {
            // Use AZURE_STORAGE_CONNECTION_STRING for custom storage, fallback to AzureWebJobsStorage for local dev
            var connectionString = Environment.GetEnvironmentVariable("AZURE_STORAGE_CONNECTION_STRING")
                ?? Environment.GetEnvironmentVariable("AzureWebJobsStorage")
                ?? "UseDevelopmentStorage=true";
            var excelService = sp.GetRequiredService<IExcelService>();
            var bowlExcelService = sp.GetRequiredService<IBowlExcelService>();
            var logger = sp.GetRequiredService<ILogger<StorageService>>();
            return new StorageService(connectionString, excelService, bowlExcelService, logger);
        });
    })
    .Build();

host.Run();