using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using FluentResults;
using SmartPPC.Core.ML.Domain;

namespace SmartPPC.Core.ML.Repositories;

/// <summary>
/// Repository interface for ForecastPrediction entity persistence.
/// </summary>
public interface IForecastPredictionRepository
{
    /// <summary>
    /// Gets a forecast prediction by ID.
    /// </summary>
    Task<Result<ForecastPrediction?>> GetByIdAsync(Guid id);

    /// <summary>
    /// Gets all predictions for a specific station.
    /// </summary>
    Task<Result<IEnumerable<ForecastPrediction>>> GetByStationAsync(
        int stationId,
        DateTime? startDate = null,
        DateTime? endDate = null);

    /// <summary>
    /// Gets all predictions made by a specific model.
    /// </summary>
    Task<Result<IEnumerable<ForecastPrediction>>> GetByModelAsync(
        Guid forecastModelId,
        DateTime? startDate = null,
        DateTime? endDate = null);

    /// <summary>
    /// Gets the most recent prediction for a station.
    /// </summary>
    Task<Result<ForecastPrediction?>> GetLatestForStationAsync(int stationId);

    /// <summary>
    /// Gets all predictions for a configuration.
    /// </summary>
    Task<Result<IEnumerable<ForecastPrediction>>> GetByConfigurationAsync(
        Guid configurationId,
        DateTime? startDate = null,
        DateTime? endDate = null);

    /// <summary>
    /// Gets predictions that were used in planning.
    /// </summary>
    Task<Result<IEnumerable<ForecastPrediction>>> GetUsedInPlanningAsync(
        Guid configurationId,
        DateTime? startDate = null,
        DateTime? endDate = null);

    /// <summary>
    /// Gets predictions that were overridden by users.
    /// </summary>
    Task<Result<IEnumerable<ForecastPrediction>>> GetOverriddenAsync(
        Guid configurationId,
        DateTime? startDate = null,
        DateTime? endDate = null);

    /// <summary>
    /// Adds a new forecast prediction.
    /// </summary>
    Task<Result<ForecastPrediction>> AddAsync(ForecastPrediction prediction);

    /// <summary>
    /// Updates an existing forecast prediction (e.g., with actual values).
    /// </summary>
    Task<Result<ForecastPrediction>> UpdateAsync(ForecastPrediction prediction);

    /// <summary>
    /// Deletes a forecast prediction by ID.
    /// </summary>
    Task<Result> DeleteAsync(Guid id);

    /// <summary>
    /// Deletes all predictions for a specific model.
    /// Used when deleting a model.
    /// </summary>
    Task<Result<int>> DeleteByModelAsync(Guid forecastModelId);

    /// <summary>
    /// Gets count of predictions for a station.
    /// </summary>
    Task<Result<int>> GetCountForStationAsync(int stationId);
}
