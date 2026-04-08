namespace WorkerServiceApp;
using WorkerServiceApp.Services;
using Microsoft.Extensions.Logging;

public class Worker : BackgroundService
{
    private readonly ILogger<Worker> _logger;
    private readonly IConfiguration _configuration;
    private readonly PollerService _pollerService;
    private readonly int _pollingIntervalInMilliseconds;

    public Worker(ILogger<Worker> logger, IConfiguration configuration, ILoggerFactory loggerFactory)
    {
        _logger = logger;
        _configuration = configuration;

        // Retrieve the polling interval from appsettings.json
        var intervalInMinutes = _configuration.GetValue<int>("Polling:IntervalInMinutes");
        _pollingIntervalInMilliseconds = intervalInMinutes * 60 * 1000;

        // Retrieve connection string and output file path from appsettings.json
        var connectionString = _configuration.GetConnectionString("OdbcConnection")
            ?? throw new ArgumentNullException("Connection string cannot be null.");

        var outputFilePath = _configuration.GetValue<string>("Polling:OutputFilePath")
            ?? throw new ArgumentNullException("Output file path cannot be null.");

        // Ensure the output directory exists
        Directory.CreateDirectory(Path.GetDirectoryName(outputFilePath)
            ?? throw new ArgumentNullException(nameof(outputFilePath), "Output file path must have a valid directory."));

        // Create a logger for PollerService
        var pollerLogger = loggerFactory.CreateLogger<PollerService>();

        // Initialize PollerService with logger
        _pollerService = new PollerService(connectionString, outputFilePath, pollerLogger);
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        while (!stoppingToken.IsCancellationRequested)
        {
            _logger.LogInformation("Worker running at: {time}", DateTimeOffset.Now);

            // Execute the poller service query
            await _pollerService.ExecuteQueryAsync();

            // Wait for the configured polling interval
            await Task.Delay(_pollingIntervalInMilliseconds, stoppingToken);
        }
    }
}
