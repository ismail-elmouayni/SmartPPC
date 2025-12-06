using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using FluentResults;
using SmartPPC.Core.ML.Domain;

namespace SmartPPC.Core.ML.Repositories;

/// <summary>
/// Repository interface for ModelMetrics entity persistence.
/// </summary>
public interface IModelMetricsRepository
{
    /// <summary>
    /// Gets model metrics by ID.
    /// </summary>
    Task<Result<ModelMetrics?>> GetByIdAsync(Guid id);

    /// <summary>
    /// Gets all metrics for a specific forecast model.
    /// </summary>
    Task<Result<IEnumerable<ModelMetrics>>> GetByModelAsync(Guid forecastModelId);

    /// <summary>
    /// Gets metrics by evaluation type.
    /// </summary>
    Task<Result<IEnumerable<ModelMetrics>>> GetByModelAndTypeAsync(
        Guid forecastModelId,
        EvaluationType evaluationType);

    /// <summary>
    /// Gets the latest metrics for a model.
    /// </summary>
    Task<Result<ModelMetrics?>> GetLatestForModelAsync(Guid forecastModelId);

    /// <summary>
    /// Gets metrics for multiple models (for comparison).
    /// </summary>
    Task<Result<IEnumerable<ModelMetrics>>> GetByModelsAsync(
        IEnumerable<Guid> forecastModelIds,
        EvaluationType? evaluationType = null);

    /// <summary>
    /// Gets metrics within a date range.
    /// </summary>
    Task<Result<IEnumerable<ModelMetrics>>> GetByDateRangeAsync(
        Guid forecastModelId,
        DateTime startDate,
        DateTime endDate);

    /// <summary>
    /// Adds new model metrics.
    /// </summary>
    Task<Result<ModelMetrics>> AddAsync(ModelMetrics metrics);

    /// <summary>
    /// Updates existing model metrics.
    /// </summary>
    Task<Result<ModelMetrics>> UpdateAsync(ModelMetrics metrics);

    /// <summary>
    /// Deletes model metrics by ID.
    /// </summary>
    Task<Result> DeleteAsync(Guid id);

    /// <summary>
    /// Deletes all metrics for a specific model.
    /// Used when deleting a model.
    /// </summary>
    Task<Result<int>> DeleteByModelAsync(Guid forecastModelId);

    /// <summary>
    /// Gets count of metrics for a model.
    /// </summary>
    Task<Result<int>> GetCountForModelAsync(Guid forecastModelId);
}
