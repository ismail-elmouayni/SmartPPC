using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using FluentResults;
using SmartPPC.Core.ML.Domain;

namespace SmartPPC.Core.ML.Repositories;

/// <summary>
/// Repository interface for ForecastModel entity persistence.
/// </summary>
public interface IForecastModelRepository
{
    /// <summary>
    /// Gets a forecast model by ID.
    /// </summary>
    Task<Result<ForecastModel?>> GetByIdAsync(Guid id);

    /// <summary>
    /// Gets the currently active model for a configuration.
    /// </summary>
    Task<Result<ForecastModel?>> GetActiveModelAsync(Guid configurationId);

    /// <summary>
    /// Gets all models for a configuration.
    /// </summary>
    Task<Result<IEnumerable<ForecastModel>>> GetByConfigurationAsync(
        Guid configurationId,
        bool includeInactive = true);

    /// <summary>
    /// Gets all models for a specific station.
    /// </summary>
    Task<Result<IEnumerable<ForecastModel>>> GetByStationAsync(
        int stationId,
        bool includeInactive = true);

    /// <summary>
    /// Adds a new forecast model.
    /// </summary>
    Task<Result<ForecastModel>> AddAsync(ForecastModel model);

    /// <summary>
    /// Updates an existing forecast model.
    /// </summary>
    Task<Result<ForecastModel>> UpdateAsync(ForecastModel model);

    /// <summary>
    /// Deletes a forecast model by ID.
    /// </summary>
    Task<Result> DeleteAsync(Guid id);

    /// <summary>
    /// Deactivates all models for a configuration.
    /// Used before activating a new model.
    /// </summary>
    Task<Result> DeactivateAllForConfigurationAsync(Guid configurationId);

    /// <summary>
    /// Gets all models, optionally filtered by active status.
    /// </summary>
    Task<Result<IEnumerable<ForecastModel>>> GetAllAsync(bool? activeOnly = null);

    /// <summary>
    /// Checks if a model with the given ID exists.
    /// </summary>
    Task<Result<bool>> ExistsAsync(Guid id);
}
