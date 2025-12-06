using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using FluentResults;
using SmartPPC.Core.ML.Domain;

namespace SmartPPC.Core.ML.Repositories;

/// <summary>
/// Repository interface for ForecastTrainingData entity persistence.
/// </summary>
public interface IForecastTrainingDataRepository
{
    /// <summary>
    /// Gets training data by ID.
    /// </summary>
    Task<Result<ForecastTrainingData?>> GetByIdAsync(Guid id);

    /// <summary>
    /// Gets historical training data for a station within a date range.
    /// </summary>
    Task<Result<IEnumerable<ForecastTrainingData>>> GetByStationAsync(
        int stationId,
        DateTime startDate,
        DateTime endDate);

    /// <summary>
    /// Gets historical training data for all stations in a configuration.
    /// </summary>
    Task<Result<Dictionary<int, IEnumerable<ForecastTrainingData>>>> GetByConfigurationAsync(
        Guid configurationId,
        DateTime startDate,
        DateTime endDate);

    /// <summary>
    /// Gets count of training data points for a station.
    /// </summary>
    Task<Result<int>> GetCountForStationAsync(int stationId);

    /// <summary>
    /// Gets the date range (earliest and latest dates) of training data for a station.
    /// </summary>
    Task<Result<(DateTime? earliestDate, DateTime? latestDate)?>> GetDateRangeForStationAsync(int stationId);

    /// <summary>
    /// Adds a single training data observation.
    /// </summary>
    Task<Result<ForecastTrainingData>> AddAsync(ForecastTrainingData trainingData);

    /// <summary>
    /// Adds multiple training data observations in batch.
    /// </summary>
    Task<Result<int>> AddRangeAsync(IEnumerable<ForecastTrainingData> trainingDataList);

    /// <summary>
    /// Deletes training data older than a specified date for a configuration.
    /// </summary>
    Task<Result<int>> DeleteOldDataAsync(Guid configurationId, DateTime cutoffDate);

    /// <summary>
    /// Deletes all training data for a specific station.
    /// </summary>
    Task<Result<int>> DeleteByStationAsync(int stationId);

    /// <summary>
    /// Deletes all training data for a configuration.
    /// </summary>
    Task<Result<int>> DeleteByConfigurationAsync(Guid configurationId);

    /// <summary>
    /// Checks if there is sufficient data for training (minimum number of days).
    /// </summary>
    Task<Result<bool>> HasSufficientDataAsync(int stationId, int minimumDays = 180);
}
