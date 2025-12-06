using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using FluentResults;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using SmartPPC.Api.Data;
using SmartPPC.Core.ML.Domain;
using SmartPPC.Core.ML.Repositories;

namespace SmartPPC.Api.Repositories;

/// <summary>
/// Repository implementation for ModelMetrics entity.
/// </summary>
public class ModelMetricsRepository : IModelMetricsRepository
{
    private readonly ApplicationDbContext _context;
    private readonly ILogger<ModelMetricsRepository> _logger;

    public ModelMetricsRepository(
        ApplicationDbContext context,
        ILogger<ModelMetricsRepository> logger)
    {
        _context = context ?? throw new ArgumentNullException(nameof(context));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    public async Task<Result<ModelMetrics?>> GetByIdAsync(Guid id)
    {
        try
        {
            var metrics = await _context.ModelMetrics
                .AsNoTracking()
                .FirstOrDefaultAsync(m => m.Id == id);

            return Result.Ok(metrics);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting model metrics by ID {MetricsId}", id);
            return Result.Fail<ModelMetrics?>($"Failed to get metrics: {ex.Message}");
        }
    }

    public async Task<Result<IEnumerable<ModelMetrics>>> GetByModelAsync(Guid forecastModelId)
    {
        try
        {
            var metrics = await _context.ModelMetrics
                .AsNoTracking()
                .Where(m => m.ForecastModelId == forecastModelId)
                .OrderByDescending(m => m.CreatedAt)
                .ToListAsync();

            return Result.Ok<IEnumerable<ModelMetrics>>(metrics);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting metrics for model {ModelId}", forecastModelId);
            return Result.Fail<IEnumerable<ModelMetrics>>($"Failed to get metrics: {ex.Message}");
        }
    }

    public async Task<Result<IEnumerable<ModelMetrics>>> GetByModelAndTypeAsync(
        Guid forecastModelId,
        EvaluationType evaluationType)
    {
        try
        {
            var metrics = await _context.ModelMetrics
                .AsNoTracking()
                .Where(m => m.ForecastModelId == forecastModelId && m.EvaluationType == evaluationType)
                .OrderByDescending(m => m.CreatedAt)
                .ToListAsync();

            return Result.Ok<IEnumerable<ModelMetrics>>(metrics);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting {Type} metrics for model {ModelId}", evaluationType, forecastModelId);
            return Result.Fail<IEnumerable<ModelMetrics>>($"Failed to get metrics: {ex.Message}");
        }
    }

    public async Task<Result<ModelMetrics?>> GetLatestForModelAsync(Guid forecastModelId)
    {
        try
        {
            var metrics = await _context.ModelMetrics
                .AsNoTracking()
                .Where(m => m.ForecastModelId == forecastModelId)
                .OrderByDescending(m => m.CreatedAt)
                .FirstOrDefaultAsync();

            return Result.Ok(metrics);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting latest metrics for model {ModelId}", forecastModelId);
            return Result.Fail<ModelMetrics?>($"Failed to get latest metrics: {ex.Message}");
        }
    }

    public async Task<Result<IEnumerable<ModelMetrics>>> GetByModelsAsync(
        IEnumerable<Guid> forecastModelIds,
        EvaluationType? evaluationType = null)
    {
        try
        {
            var modelIdsList = forecastModelIds.ToList();
            var query = _context.ModelMetrics
                .AsNoTracking()
                .Where(m => modelIdsList.Contains(m.ForecastModelId));

            if (evaluationType.HasValue)
            {
                query = query.Where(m => m.EvaluationType == evaluationType.Value);
            }

            var metrics = await query
                .OrderByDescending(m => m.CreatedAt)
                .ToListAsync();

            return Result.Ok<IEnumerable<ModelMetrics>>(metrics);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting metrics for multiple models");
            return Result.Fail<IEnumerable<ModelMetrics>>($"Failed to get metrics: {ex.Message}");
        }
    }

    public async Task<Result<IEnumerable<ModelMetrics>>> GetByDateRangeAsync(
        Guid forecastModelId,
        DateTime startDate,
        DateTime endDate)
    {
        try
        {
            var metrics = await _context.ModelMetrics
                .AsNoTracking()
                .Where(m => m.ForecastModelId == forecastModelId
                         && m.EvaluationStartDate >= startDate
                         && m.EvaluationEndDate <= endDate)
                .OrderByDescending(m => m.CreatedAt)
                .ToListAsync();

            return Result.Ok<IEnumerable<ModelMetrics>>(metrics);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting metrics by date range for model {ModelId}", forecastModelId);
            return Result.Fail<IEnumerable<ModelMetrics>>($"Failed to get metrics: {ex.Message}");
        }
    }

    public async Task<Result<ModelMetrics>> AddAsync(ModelMetrics metrics)
    {
        try
        {
            _context.ModelMetrics.Add(metrics);
            await _context.SaveChangesAsync();

            _logger.LogInformation("Added model metrics {MetricsId} for model {ModelId}", metrics.Id, metrics.ForecastModelId);
            return Result.Ok(metrics);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error adding model metrics");
            return Result.Fail<ModelMetrics>($"Failed to add metrics: {ex.Message}");
        }
    }

    public async Task<Result<ModelMetrics>> UpdateAsync(ModelMetrics metrics)
    {
        try
        {
            _context.ModelMetrics.Update(metrics);
            await _context.SaveChangesAsync();

            _logger.LogInformation("Updated model metrics {MetricsId}", metrics.Id);
            return Result.Ok(metrics);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating model metrics {MetricsId}", metrics.Id);
            return Result.Fail<ModelMetrics>($"Failed to update metrics: {ex.Message}");
        }
    }

    public async Task<Result> DeleteAsync(Guid id)
    {
        try
        {
            var metrics = await _context.ModelMetrics.FindAsync(id);
            if (metrics == null)
            {
                return Result.Fail($"Metrics {id} not found");
            }

            _context.ModelMetrics.Remove(metrics);
            await _context.SaveChangesAsync();

            _logger.LogInformation("Deleted model metrics {MetricsId}", id);
            return Result.Ok();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error deleting model metrics {MetricsId}", id);
            return Result.Fail($"Failed to delete metrics: {ex.Message}");
        }
    }

    public async Task<Result<int>> DeleteByModelAsync(Guid forecastModelId)
    {
        try
        {
            var metrics = await _context.ModelMetrics
                .Where(m => m.ForecastModelId == forecastModelId)
                .ToListAsync();

            _context.ModelMetrics.RemoveRange(metrics);
            await _context.SaveChangesAsync();

            _logger.LogInformation(
                "Deleted {Count} metrics for model {ModelId}",
                metrics.Count, forecastModelId);

            return Result.Ok(metrics.Count);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error deleting metrics for model {ModelId}", forecastModelId);
            return Result.Fail<int>($"Failed to delete metrics: {ex.Message}");
        }
    }

    public async Task<Result<int>> GetCountForModelAsync(Guid forecastModelId)
    {
        try
        {
            var count = await _context.ModelMetrics
                .CountAsync(m => m.ForecastModelId == forecastModelId);

            return Result.Ok(count);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting metrics count for model {ModelId}", forecastModelId);
            return Result.Fail<int>($"Failed to get metrics count: {ex.Message}");
        }
    }
}
