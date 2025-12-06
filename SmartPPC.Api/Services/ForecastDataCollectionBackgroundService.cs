using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using SmartPPC.Core.ML.Services;

namespace SmartPPC.Api.Services;

/// <summary>
/// Background service that periodically collects demand data for training forecasting models.
/// Runs as an ASP.NET Core Hosted Service and calls Core ML services.
/// </summary>
public class ForecastDataCollectionBackgroundService : BackgroundService
{
    private readonly ILogger<ForecastDataCollectionBackgroundService> _logger;
    private readonly IServiceProvider _serviceProvider;
    private readonly ForecastDataCollectionOptions _options;

    public ForecastDataCollectionBackgroundService(
        ILogger<ForecastDataCollectionBackgroundService> logger,
        IServiceProvider serviceProvider,
        IOptions<ForecastDataCollectionOptions> options)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _serviceProvider = serviceProvider ?? throw new ArgumentNullException(nameof(serviceProvider));
        _options = options?.Value ?? throw new ArgumentNullException(nameof(options));
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("Forecast Data Collection Background Service is starting");

        if (!_options.Enabled)
        {
            _logger.LogInformation("Forecast data collection is disabled in configuration");
            return;
        }

        // Wait for initial delay before first execution (allows app to fully start)
        await Task.Delay(_options.InitialDelayMinutes * 60 * 1000, stoppingToken);

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                _logger.LogInformation("Starting forecast data collection cycle at {Time}", DateTime.UtcNow);

                await CollectDataAsync(stoppingToken);

                _logger.LogInformation(
                    "Forecast data collection cycle completed. Next run in {Interval} minutes",
                    _options.CollectionIntervalMinutes);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error occurred during forecast data collection cycle");
            }

            // Wait for the configured interval before next execution
            await Task.Delay(
                TimeSpan.FromMinutes(_options.CollectionIntervalMinutes),
                stoppingToken);
        }

        _logger.LogInformation("Forecast Data Collection Background Service is stopping");
    }

    private async Task CollectDataAsync(CancellationToken cancellationToken)
    {
        // Create a scope to resolve scoped services
        using var scope = _serviceProvider.CreateScope();

        var dataCollectionService = scope.ServiceProvider
            .GetRequiredService<IForecastDataCollectionService>();

        var configurationService = scope.ServiceProvider
            .GetRequiredService<ConfigurationService>();

        try
        {
            // Get all active configurations
            var configurations = await configurationService.GetAllConfigurationsAsync();

            if (configurations == null || !configurations.Any())
            {
                _logger.LogInformation("No configurations found to collect data from");
                return;
            }

            _logger.LogInformation("Found {Count} configurations to collect data from", configurations.Count());

            foreach (var configuration in configurations)
            {
                if (cancellationToken.IsCancellationRequested)
                    break;

                await CollectDataForConfigurationAsync(
                    configuration.Id,
                    dataCollectionService,
                    configurationService,
                    cancellationToken);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error collecting data from configurations");
        }
    }

    private async Task CollectDataForConfigurationAsync(
        Guid configurationId,
        IForecastDataCollectionService dataCollectionService,
        ConfigurationService configurationService,
        CancellationToken cancellationToken)
    {
        try
        {
            _logger.LogInformation("Collecting data for configuration {ConfigurationId}", configurationId);

            // Get the configuration with station declarations
            var configuration = await configurationService.LoadConfigurationFromDatabaseAsync(configurationId);

            if (configuration?.StationDeclarations == null || !configuration.StationDeclarations.Any())
            {
                _logger.LogWarning("Configuration {ConfigurationId} has no stations", configurationId);
                return;
            }

            var observationDate = DateTime.UtcNow;
            int collectedCount = 0;

            // Collect data for each station
            foreach (var station in configuration.StationDeclarations)
            {
                if (cancellationToken.IsCancellationRequested)
                    break;

                // Get demand forecast (actual demand is typically the current/first period)
                if (station.DemandForecast != null && station.DemandForecast.Any())
                {
                    // Take the most recent forecast or first period as "actual" demand
                    var recentForecast = station.DemandForecast
                        .FirstOrDefault();
                    int demandValue = recentForecast;

                    // Get buffer level if available
                    int? bufferLevel = null;
                    if (station.PastBuffer.Length > 0)
                    {
                        var recentBuffer = station.PastBuffer
                            .FirstOrDefault();
                        bufferLevel = recentBuffer;
                    }

                    // Get order amount if available
                    int? orderAmount = null;
                    if (station.PastOrderAmount.Length > 0)
                    {
                        var recentOrder = station.PastOrderAmount.ToList()
                            .FirstOrDefault();

                        orderAmount = recentOrder;
                    }

                    // Record the observation
                    var result = await dataCollectionService.RecordDemandObservationAsync(
                        configurationId,
                        station.StationIndex.Value,
                        observationDate,
                        demandValue,
                        bufferLevel,
                        orderAmount);

                    if (result.IsSuccess)
                    {
                        collectedCount++;
                        _logger.LogDebug(
                            "Collected data for station {StationId}: Demand={Demand}, Buffer={Buffer}",
                            station.StationIndex, demandValue, bufferLevel);
                    }
                    else
                    {
                        _logger.LogWarning(
                            "Failed to collect data for station {StationId}: {Error}",
                            station.StationIndex, result.Errors.FirstOrDefault()?.Message);
                    }
                }
            }

            _logger.LogInformation(
                "Collected {Count} demand observations for configuration {ConfigurationId}",
                collectedCount, configurationId);
        }
        catch (Exception ex)
        {
            _logger.LogError(
                ex,
                "Error collecting data for configuration {ConfigurationId}",
                configurationId);
        }
    }

    public override async Task StopAsync(CancellationToken cancellationToken)
    {
        _logger.LogInformation("Forecast Data Collection Background Service is stopping gracefully");
        await base.StopAsync(cancellationToken);
    }
}

/// <summary>
/// Configuration options for the forecast data collection background service.
/// </summary>
public class ForecastDataCollectionOptions
{
    /// <summary>
    /// Configuration section name in appsettings.json.
    /// </summary>
    public const string SectionName = "ForecastDataCollection";

    /// <summary>
    /// Whether the background data collection service is enabled.
    /// Default: false (must be explicitly enabled).
    /// </summary>
    public bool Enabled { get; set; } = false;

    /// <summary>
    /// Initial delay in minutes before the first data collection run.
    /// Allows the application to fully start before beginning data collection.
    /// Default: 5 minutes.
    /// </summary>
    public int InitialDelayMinutes { get; set; } = 5;

    /// <summary>
    /// Interval in minutes between data collection cycles.
    /// Default: 1440 minutes (24 hours = daily collection).
    /// </summary>
    public int CollectionIntervalMinutes { get; set; } = 1440;

    /// <summary>
    /// Whether to collect data immediately on startup (ignores InitialDelayMinutes).
    /// Useful for testing. Default: false.
    /// </summary>
    public bool CollectOnStartup { get; set; } = false;

    /// <summary>
    /// Maximum number of configurations to process in a single cycle.
    /// Useful for rate limiting. Default: null (no limit).
    /// </summary>
    public int? MaxConfigurationsPerCycle { get; set; }
}
