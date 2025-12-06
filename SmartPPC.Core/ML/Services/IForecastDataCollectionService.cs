using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using FluentResults;
using SmartPPC.Core.ML.Domain;

namespace SmartPPC.Core.ML.Services;

/// <summary>
/// Service interface for collecting historical demand data used in training forecasting models.
/// Responsible for capturing actual demand observations from DDMRP execution and storing them
/// in a format suitable for machine learning.
/// </summary>
public interface IForecastDataCollectionService
{
    /// <summary>
    /// Records a single demand observation for a specific station.
    /// </summary>
    /// <param name="configurationId">The configuration ID</param>
    /// <param name="stationId">The station declaration ID</param>
    /// <param name="observationDate">The date/time of the observation</param>
    /// <param name="demandValue">The actual demand value</param>
    /// <param name="bufferLevel">Optional buffer level at time of observation</param>
    /// <param name="orderAmount">Optional order amount</param>
    /// <param name="exogenousFactors">Optional JSON string with additional factors</param>
    /// <returns>Result indicating success or failure</returns>
    Task<Result<ForecastTrainingData>> RecordDemandObservationAsync(
        Guid configurationId,
        int stationId,
        DateTime observationDate,
        int demandValue,
        int? bufferLevel = null,
        int? orderAmount = null,
        string? exogenousFactors = null);

    /// <summary>
    /// Records multiple demand observations in a batch (more efficient).
    /// </summary>
    /// <param name="observations">Collection of training data to record</param>
    /// <returns>Result with count of successfully recorded observations</returns>
    Task<Result<int>> RecordDemandObservationsBatchAsync(
        IEnumerable<ForecastTrainingData> observations);

    /// <summary>
    /// Collects demand data from DDMRP production control model execution.
    /// Extracts demand values from the model's execution results.
    /// </summary>
    /// <param name="configurationId">The configuration ID</param>
    /// <param name="productionModel">The executed production control model</param>
    /// <param name="observationDate">The date/time of the execution</param>
    /// <returns>Result with count of recorded observations</returns>
    Task<Result<int>> CollectFromProductionModelAsync(
        Guid configurationId,
        Model.DDMRP.ProductionControlModel productionModel,
        DateTime observationDate);

    /// <summary>
    /// Retrieves historical training data for a specific station within a date range.
    /// </summary>
    /// <param name="stationId">The station declaration ID</param>
    /// <param name="startDate">Start date (inclusive)</param>
    /// <param name="endDate">End date (inclusive)</param>
    /// <returns>Collection of training data ordered by date</returns>
    Task<Result<IEnumerable<ForecastTrainingData>>> GetHistoricalDataAsync(
        int stationId,
        DateTime startDate,
        DateTime endDate);

    /// <summary>
    /// Retrieves historical training data for all stations in a configuration.
    /// </summary>
    /// <param name="configurationId">The configuration ID</param>
    /// <param name="startDate">Start date (inclusive)</param>
    /// <param name="endDate">End date (inclusive)</param>
    /// <returns>Dictionary mapping station ID to collection of training data</returns>
    Task<Result<Dictionary<int, IEnumerable<ForecastTrainingData>>>> GetHistoricalDataForAllStationsAsync(
        Guid configurationId,
        DateTime startDate,
        DateTime endDate);

    /// <summary>
    /// Gets the count of available training data points for a specific station.
    /// </summary>
    /// <param name="stationId">The station declaration ID</param>
    /// <returns>Count of data points</returns>
    Task<Result<int>> GetDataPointCountAsync(int stationId);

    /// <summary>
    /// Gets the date range (earliest to latest) of available data for a station.
    /// </summary>
    /// <param name="stationId">The station declaration ID</param>
    /// <returns>Tuple of (earliest date, latest date), or null if no data exists</returns>
    Task<Result<(DateTime? earliestDate, DateTime? latestDate)?>> GetDataDateRangeAsync(int stationId);

    /// <summary>
    /// Checks if sufficient historical data exists for model training.
    /// </summary>
    /// <param name="stationId">The station declaration ID</param>
    /// <param name="minimumDays">Minimum number of days of data required</param>
    /// <returns>True if sufficient data exists</returns>
    Task<Result<bool>> HasSufficientDataForTrainingAsync(int stationId, int minimumDays = 180);

    /// <summary>
    /// Deletes old training data beyond a retention period (for data management).
    /// </summary>
    /// <param name="configurationId">The configuration ID</param>
    /// <param name="retentionDays">Number of days to retain (older data will be deleted)</param>
    /// <returns>Count of deleted records</returns>
    Task<Result<int>> CleanupOldDataAsync(Guid configurationId, int retentionDays = 730);

    /// <summary>
    /// Generates synthetic/demo training data for testing and development purposes.
    /// Creates realistic demand patterns with seasonality and trends.
    /// </summary>
    /// <param name="configurationId">The configuration ID</param>
    /// <param name="stationId">The station declaration ID</param>
    /// <param name="startDate">Start date for synthetic data</param>
    /// <param name="numberOfDays">Number of days of data to generate</param>
    /// <param name="baseDemand">Base demand level</param>
    /// <param name="seasonalityFactor">Seasonality amplitude (0-1)</param>
    /// <returns>Count of generated records</returns>
    Task<Result<int>> GenerateSyntheticDataAsync(
        Guid configurationId,
        int stationId,
        DateTime startDate,
        int numberOfDays,
        int baseDemand = 50,
        float seasonalityFactor = 0.2f);
}
