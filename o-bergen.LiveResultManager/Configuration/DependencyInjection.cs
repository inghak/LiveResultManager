using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using o_bergen.LiveResultManager.Application.DTOs;
using o_bergen.LiveResultManager.Application.Mappers;
using o_bergen.LiveResultManager.Core.Interfaces;
using o_bergen.LiveResultManager.Core.Services;
using o_bergen.LiveResultManager.Infrastructure.Archive;
using o_bergen.LiveResultManager.Infrastructure.Destinations;
using o_bergen.LiveResultManager.Infrastructure.Logging;
using o_bergen.LiveResultManager.Infrastructure.Sources;

namespace o_bergen.LiveResultManager.Configuration;

/// <summary>
/// Extension methods for configuring dependency injection
/// </summary>
public static class DependencyInjection
{
    /// <summary>
    /// Adds all application services to the DI container
    /// </summary>
    public static IServiceCollection AddLiveResultManagerServices(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        // Register configuration
        services.AddSingleton(configuration);

        // Bind configuration sections to DTOs
        var accessDbConfig = configuration.GetSection("AccessDb").Get<AccessDbConfig>() 
            ?? new AccessDbConfig();
        var supabaseConfig = configuration.GetSection("Supabase").Get<SupabaseConfig>() 
            ?? new SupabaseConfig();
        var archiveConfig = configuration.GetSection("Archive").Get<ArchiveConfig>() 
            ?? new ArchiveConfig();
        var transferConfig = configuration.GetSection("Transfer").Get<TransferConfig>() 
            ?? new TransferConfig();
        var loggingConfig = configuration.GetSection("Logging").Get<LoggingConfig>() 
            ?? new LoggingConfig();

        // Register configurations
        services.AddSingleton(accessDbConfig);
        services.AddSingleton(supabaseConfig);
        services.AddSingleton(archiveConfig);
        services.AddSingleton(transferConfig);
        services.AddSingleton(loggingConfig);

        // Register full configuration DTO
        services.AddSingleton(new ConfigurationDto
        {
            AccessDb = accessDbConfig,
            Supabase = supabaseConfig,
            Archive = archiveConfig,
            Transfer = transferConfig,
            Logging = loggingConfig
        });

        // Register Mappers (Singleton - stateless)
        services.AddSingleton<SourceToJsonMapper>();
        services.AddSingleton<JsonToDestinationMapper>();
        services.AddSingleton<MetadataMapper>();
        services.AddSingleton<IofXmlMapper>();
        services.AddSingleton<SploypeCsvMapper>();

        // Register Logger
        if (loggingConfig.EnableFileLogging && !string.IsNullOrEmpty(loggingConfig.LogPath))
        {
            services.AddSingleton<ILogger>(sp => new FileLogger(loggingConfig.LogPath));
        }
        else
        {
            services.AddSingleton<ILogger, NullLogger>();
        }

        // Register Infrastructure Services
        services.AddSingleton<IResultSource>(sp =>
        {
            var dbPath = accessDbConfig.Path;
            if (string.IsNullOrEmpty(dbPath) || !File.Exists(dbPath))
            {
                // Fall back to mock source if no valid database
                return new MockResultSource();
            }
            return new AccessDbResultSource(dbPath);
        });

        services.AddSingleton<IResultDestination>(sp =>
        {
            var url = supabaseConfig.Url;
            var apiKey = supabaseConfig.ApiKey;

            if (string.IsNullOrEmpty(url) || string.IsNullOrEmpty(apiKey))
            {
                // Fall back to file destination for testing
                return new FileResultDestination(Path.Combine(archiveConfig.BasePath, "test-output.txt"));
            }

            return new SupabaseResultDestination(url, apiKey);
        });

        services.AddSingleton<IResultArchive>(sp =>
        {
            var basePath = archiveConfig.BasePath;
            if (string.IsNullOrEmpty(basePath))
                basePath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Archive");

            var archives = new List<IResultArchive>();

            // JSON Archive
            archives.Add(new JsonFileArchive(basePath));

            // IOF XML Archive
            var iofMapper = sp.GetRequiredService<IofXmlMapper>();
            archives.Add(new IofXmlArchive(basePath, iofMapper));

            // Sploype CSV Archive
            var csvMapper = sp.GetRequiredService<SploypeCsvMapper>();
            archives.Add(new SploypeCsvArchive(basePath, csvMapper));

            // Supabase Storage Archive
            if (!string.IsNullOrEmpty(supabaseConfig.Url) && 
                !string.IsNullOrEmpty(supabaseConfig.ApiKey))
            {
                var httpClient = new HttpClient();
                archives.Add(new SupabaseStorageArchive(
                    httpClient,
                    supabaseConfig.Url,
                    supabaseConfig.ApiKey,
                    iofMapper,
                    csvMapper));
            }

            // Create composite archive that writes to all configured archives
            return new CompositeArchive(archives);
        });

        // Register Core Services (Transient - new instance per request)
        services.AddTransient<ResultTransferService>();

        return services;
    }

    /// <summary>
    /// Null logger implementation when file logging is disabled
    /// </summary>
    private class NullLogger : ILogger
    {
        public void LogInformation(string message) { }
        public void LogWarning(string message) { }
        public void LogError(string message, Exception? exception = null) { }
        public void LogDebug(string message) { }
        public void LogSuccess(string message) { }
    }
}
