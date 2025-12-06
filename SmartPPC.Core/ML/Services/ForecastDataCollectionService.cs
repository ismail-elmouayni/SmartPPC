using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Threading.Tasks;
using FluentResults;
using Microsoft.Extensions.Logging;
using SmartPPC.Core.ML.Domain;
using SmartPPC.Core.ML.Repositories;

namespace SmartPPC.Core.ML.Services;

/// <summary>
/// Implementation of demand data collection service.
/// Collects and manages historical demand data for training forecasting models.
/// </summary>
public class ForecastDataCollectionService : IForecastDataCollectionService
{
    private readonly ILogger<ForecastDataCollectionService> _logger;
    private readonly IForecastTrainingDataRepository _repository;

    public ForecastDataCollectionService(
        ILogger<ForecastDataCollectionService> logger,
        IForecastTrainingDataRepository repository)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _repository = repository ?? throw new ArgumentNullException(nameof(repository));
    }

    /// <inheritdoc/>
    public async Task<Result<ForecastTrainingData>> RecordDemandObservationAsync(
        Guid configurationId,
        int stationId,
        DateTime observationDate,
        int demandValue,
        int? bufferLevel = null,
        int? orderAmount = null,
        string? exogenousFactors = null)
    {
        try
        {
            _logger.LogInformation(
                "Recording demand observation for station {StationId}: {DemandValue} on {Date}",
                stationId, demandValue, observationDate);

            var trainingData = new ForecastTrainingData
            {
                Id = Guid.NewGuid(),
                ConfigurationId = configurationId,
                StationDeclarationId = stationId,
                ObservationDate = observationDate,
                DemandValue = demandValue,
                BufferLevel = bufferLevel,
                OrderAmount = orderAmount,
                DayOfWeek = (int)observationDate.DayOfWeek,
                Month = observationDate.Month,
                Quarter = (observationDate.Month - 1) / 3 + 1,
                ExogenousFactors = exogenousFactors,
                CreatedAt = DateTime.UtcNow
            };

            return await _repository.AddAsync(trainingData);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error recording demand observation for station {StationId}", stationId);
            return Result.Fail<ForecastTrainingData>($"Failed to record demand observation: {ex.Message}");
        }
    }

    /// <inheritdoc/>
    public async Task<Result<int>> RecordDemandObservationsBatchAsync(
        IEnumerable<ForecastTrainingData> observations)
    {
        try
        {
            var observationsList = observations.ToList();
            _logger.LogInformation("Recording {Count} demand observations in batch", observationsList.Count);

            return await _repository.AddRangeAsync(observationsList);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error recording batch demand observations");
            return Result.Fail<int>($"Failed to record batch observations: {ex.Message}");
        }
    }

    /// <inheritdoc/>
    public async Task<Result<int>> CollectFromProductionModelAsync(
        Guid configurationId,
        Model.DDMRP.ProductionControlModel productionModel,
        DateTime observationDate)
    {
        try
        {
            _logger.LogInformation(
                "Collecting demand data from production model for configuration {ConfigurationId}",
                configurationId);

            var observations = new List<ForecastTrainingData>();

            // Extract demand data from each station in the production model
            foreach (var station in productionModel.Stations)
            {
                // Get actual demand from station's future states or demand forecast
                // The first period in DemandForecast represents actual/realized demand
                if (station.DemandForecast != null && station.DemandForecast.Length > 0)
                {
                    var demandValue = station.DemandForecast[0]; // First period = actual demand

                    // Get buffer level if station has a buffer
                    int? bufferLevel = null;
                    if (station.HasBuffer && station.FutureStates?.Count > 0)
                    {
                        bufferLevel = (int)(station.FutureStates[0].Buffer ?? 0);
                    }

                    // Create training data point
                    var trainingData = new ForecastTrainingData
                    {
                        Id = Guid.NewGuid(),
                        ConfigurationId = configurationId,
                        StationDeclarationId = station.Index,
                        ObservationDate = observationDate,
                        DemandValue = demandValue,
                        BufferLevel = bufferLevel,
                        OrderAmount = station.FutureStates[0].OrderAmount,
                        DayOfWeek = (int)observationDate.DayOfWeek,
                        Month = observationDate.Month,
                        Quarter = (observationDate.Month - 1) / 3 + 1,
                        CreatedAt = DateTime.UtcNow
                    };

                    observations.Add(trainingData);
                }
            }

            // TODO: Batch save to database
            // await _repository.AddRangeAsync(observations);
            // await _repository.SaveChangesAsync();

            await Task.CompletedTask; // Placeholder

            _logger.LogInformation("Collected {Count} demand observations", observations.Count);
            return Result.Ok(observations.Count);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error collecting data from production model");
            return Result.Fail<int>($"Failed to collect production model data: {ex.Message}");
        }
    }

    /// <inheritdoc/>
    public async Task<Result<IEnumerable<ForecastTrainingData>>> GetHistoricalDataAsync(
        int stationId,
        DateTime startDate,
        DateTime endDate)
    {
        try
        {
            _logger.LogInformation(
                "Retrieving historical data for station {StationId} from {StartDate} to {EndDate}",
                stationId, startDate, endDate);

            return await _repository.GetByStationAsync(stationId, startDate, endDate);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving historical data for station {StationId}", stationId);
            return Result.Fail<IEnumerable<ForecastTrainingData>>($"Failed to retrieve historical data: {ex.Message}");
        }
    }

    /// <inheritdoc/>
    public async Task<Result<Dictionary<int, IEnumerable<ForecastTrainingData>>>> GetHistoricalDataForAllStationsAsync(
        Guid configurationId,
        DateTime startDate,
        DateTime endDate)
    {
        try
        {
            _logger.LogInformation(
                "Retrieving historical data for all stations in configuration {ConfigurationId}",
                configurationId);

            return await _repository.GetByConfigurationAsync(configurationId, startDate, endDate);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving historical data for configuration {ConfigurationId}", configurationId);
            return Result.Fail<Dictionary<int, IEnumerable<ForecastTrainingData>>>($"Failed to retrieve historical data: {ex.Message}");
        }
    }

    /// <inheritdoc/>
    public async Task<Result<int>> GetDataPointCountAsync(int stationId)
    {
        try
        {
            return await _repository.GetCountForStationAsync(stationId);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting data point count for station {StationId}", stationId);
            return Result.Fail<int>($"Failed to get data point count: {ex.Message}");
        }
    }

    /// <inheritdoc/>
    public async Task<Result<(DateTime? earliestDate, DateTime? latestDate)?>> GetDataDateRangeAsync(int stationId)
    {
        try
        {
            return await _repository.GetDateRangeForStationAsync(stationId);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting data date range for station {StationId}", stationId);
            return Result.Fail<(DateTime?, DateTime?)?>(
                $"Failed to get data date range: {ex.Message}");
        }
    }

    /// <inheritdoc/>
    public async Task<Result<bool>> HasSufficientDataForTrainingAsync(int stationId, int minimumDays = 180)
    {
        try
        {
            var rangeResult = await GetDataDateRangeAsync(stationId);
            if (rangeResult.IsFailed || !rangeResult.Value.HasValue)
            {
                return Result.Ok(false);
            }

            var (earliestDate, latestDate) = rangeResult.Value.Value;
            if (!earliestDate.HasValue || !latestDate.HasValue)
            {
                return Result.Ok(false);
            }

            var daysCovered = (latestDate.Value - earliestDate.Value).Days;
            var hasSufficient = daysCovered >= minimumDays;

            _logger.LogInformation(
                "Station {StationId} has {Days} days of data (required: {MinDays}). Sufficient: {HasSufficient}",
                stationId, daysCovered, minimumDays, hasSufficient);

            return Result.Ok(hasSufficient);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error checking data sufficiency for station {StationId}", stationId);
            return Result.Fail<bool>($"Failed to check data sufficiency: {ex.Message}");
        }
    }

    /// <inheritdoc/>
    public async Task<Result<int>> CleanupOldDataAsync(Guid configurationId, int retentionDays = 730)
    {
        try
        {
            var cutoffDate = DateTime.UtcNow.AddDays(-retentionDays);
            _logger.LogInformation(
                "Cleaning up data older than {CutoffDate} for configuration {ConfigurationId}",
                cutoffDate, configurationId);

            return await _repository.DeleteOldDataAsync(configurationId, cutoffDate);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error cleaning up old data for configuration {ConfigurationId}", configurationId);
            return Result.Fail<int>($"Failed to cleanup old data: {ex.Message}");
        }
    }

    /// <inheritdoc/>
    public async Task<Result<int>> GenerateSyntheticDataAsync(
        Guid configurationId,
        int stationId,
        DateTime startDate,
        int numberOfDays,
        int baseDemand = 50,
        float seasonalityFactor = 0.2f)
    {
        try
        {
            _logger.LogInformation(
                "Generating {Days} days of synthetic data for station {StationId}",
                numberOfDays, stationId);

            var observations = new List<ForecastTrainingData>();
            var random = new Random(42); // Fixed seed for reproducibility

            for (int day = 0; day < numberOfDays; day++)
            {
                var currentDate = startDate.AddDays(day);

                // Generate realistic demand with seasonality and noise
                var seasonalComponent = Math.Sin(2 * Math.PI * day / 7.0) * seasonalityFactor; // Weekly seasonality
                var trendComponent = day / 365.0 * 0.1; // Slight upward trend
                var noiseComponent = (random.NextDouble() - 0.5) * 0.3; // Random noise

                var demandMultiplier = 1.0 + seasonalComponent + trendComponent + noiseComponent;
                var demandValue = (int)(baseDemand * demandMultiplier);
                demandValue = Math.Max(1, demandValue); // Ensure positive demand

                var trainingData = new ForecastTrainingData
                {
                    Id = Guid.NewGuid(),
                    ConfigurationId = configurationId,
                    StationDeclarationId = stationId,
                    ObservationDate = currentDate,
                    DemandValue = demandValue,
                    BufferLevel = random.Next(20, 100),
                    OrderAmount = demandValue + random.Next(-10, 10),
                    DayOfWeek = (int)currentDate.DayOfWeek,
                    Month = currentDate.Month,
                    Quarter = (currentDate.Month - 1) / 3 + 1,
                    CreatedAt = DateTime.UtcNow
                };

                observations.Add(trainingData);
            }

            // Save to database
            var result = await _repository.AddRangeAsync(observations);

            if (result.IsSuccess)
            {
                _logger.LogInformation("Generated and saved {Count} synthetic observations", result.Value);
            }

            return result;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error generating synthetic data for station {StationId}", stationId);
            return Result.Fail<int>($"Failed to generate synthetic data: {ex.Message}");
        }
    }
}
