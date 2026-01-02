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

            // Register team name normalizer (for consistent team naming across sources)
            services.AddScoped<ITeamNameNormalizer, TeamNameNormalizer>();

            // Register data services (only when database is configured)
            services.AddScoped<IUserService, UserService>();
            services.AddScoped<IGameService, GameService>();
            services.AddScoped<IPickService, PickService>();
            services.AddScoped<IResultService, ResultService>();
            services.AddScoped<ILeaderboardService, LeaderboardService>();
            services.AddScoped<IBowlGameService, BowlGameService>();
            services.AddScoped<IBowlPickService, BowlPickService>();
            services.AddScoped<IBowlLeaderboardService, BowlLeaderboardService>();
            services.AddScoped<IGameResultMatcher, GameResultMatcher>();
        }

        // Register Excel parsing services
        services.AddSingleton<IExcelService, ExcelService>();
        services.AddSingleton<IBowlExcelService, BowlExcelService>();

        // Register archive service (for storing Excel file backups in blob storage)
        services.AddSingleton<IArchiveService>(sp =>
        {
            // Use AZURE_STORAGE_CONNECTION_STRING for custom storage, fallback to AzureWebJobsStorage for local dev
            var connectionString = Environment.GetEnvironmentVariable("AZURE_STORAGE_CONNECTION_STRING")
                ?? Environment.GetEnvironmentVariable("AzureWebJobsStorage")
                ?? "UseDevelopmentStorage=true";
            var logger = sp.GetRequiredService<ILogger<ArchiveService>>();
            return new ArchiveService(connectionString, logger);
        });

        // Register Sports Data Provider (optional - only if API key is configured)
        var cfbdApiKey = Environment.GetEnvironmentVariable("CFBD_API_KEY");
        if (!string.IsNullOrEmpty(cfbdApiKey))
        {
            services.AddHttpClient<ISportsDataProvider, CollegeFootballDataProvider>(client =>
            {
                client.DefaultRequestHeaders.Add("Authorization", $"Bearer {cfbdApiKey}");
                client.DefaultRequestHeaders.Add("Accept", "application/json");
            });
        }
    })
    .Build();

host.Run();