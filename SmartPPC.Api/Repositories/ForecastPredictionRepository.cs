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
/// Repository implementation for ForecastPrediction entity.
/// </summary>
public class ForecastPredictionRepository : IForecastPredictionRepository
{
    private readonly ApplicationDbContext _context;
    private readonly ILogger<ForecastPredictionRepository> _logger;

    public ForecastPredictionRepository(
        ApplicationDbContext context,
        ILogger<ForecastPredictionRepository> logger)
    {
        _context = context ?? throw new ArgumentNullException(nameof(context));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    public async Task<Result<ForecastPrediction?>> GetByIdAsync(Guid id)
    {
        try
        {
            var prediction = await _context.ForecastPredictions
                .AsNoTracking()
                .FirstOrDefaultAsync(p => p.Id == id);

            return Result.Ok(prediction);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting forecast prediction by ID {PredictionId}", id);
            return Result.Fail<ForecastPrediction?>($"Failed to get prediction: {ex.Message}");
        }
    }

    public async Task<Result<IEnumerable<ForecastPrediction>>> GetByStationAsync(
        int stationId,
        DateTime? startDate = null,
        DateTime? endDate = null)
    {
        try
        {
            var query = _context.ForecastPredictions
                .AsNoTracking()
                .Where(p => p.StationDeclarationId == stationId);

            if (startDate.HasValue)
            {
                query = query.Where(p => p.ForecastStartDate >= startDate.Value);
            }

            if (endDate.HasValue)
            {
                query = query.Where(p => p.ForecastStartDate <= endDate.Value);
            }

            var predictions = await query
                .OrderByDescending(p => p.PredictionDate)
                .ToListAsync();

            return Result.Ok<IEnumerable<ForecastPrediction>>(predictions);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting predictions for station {StationId}", stationId);
            return Result.Fail<IEnumerable<ForecastPrediction>>($"Failed to get predictions: {ex.Message}");
        }
    }

    public async Task<Result<IEnumerable<ForecastPrediction>>> GetByModelAsync(
        Guid forecastModelId,
        DateTime? startDate = null,
        DateTime? endDate = null)
    {
        try
        {
            var query = _context.ForecastPredictions
                .AsNoTracking()
                .Where(p => p.ForecastModelId == forecastModelId);

            if (startDate.HasValue)
            {
                query = query.Where(p => p.ForecastStartDate >= startDate.Value);
            }

            if (endDate.HasValue)
            {
                query = query.Where(p => p.ForecastStartDate <= endDate.Value);
            }

            var predictions = await query
                .OrderByDescending(p => p.PredictionDate)
                .ToListAsync();

            return Result.Ok<IEnumerable<ForecastPrediction>>(predictions);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting predictions for model {ModelId}", forecastModelId);
            return Result.Fail<IEnumerable<ForecastPrediction>>($"Failed to get predictions: {ex.Message}");
        }
    }

    public async Task<Result<ForecastPrediction?>> GetLatestForStationAsync(int stationId)
    {
        try
        {
            var prediction = await _context.ForecastPredictions
                .AsNoTracking()
                .Where(p => p.StationDeclarationId == stationId)
                .OrderByDescending(p => p.PredictionDate)
                .FirstOrDefaultAsync();

            return Result.Ok(prediction);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting latest prediction for station {StationId}", stationId);
            return Result.Fail<ForecastPrediction?>($"Failed to get latest prediction: {ex.Message}");
        }
    }

    public async Task<Result<IEnumerable<ForecastPrediction>>> GetByConfigurationAsync(
        Guid configurationId,
        DateTime? startDate = null,
        DateTime? endDate = null)
    {
        try
        {
            var query = _context.ForecastPredictions
                .AsNoTracking()
                .Include(p => p.ForecastModel)
                .Where(p => p.ForecastModel!.ConfigurationId == configurationId);

            if (startDate.HasValue)
            {
                query = query.Where(p => p.ForecastStartDate >= startDate.Value);
            }

            if (endDate.HasValue)
            {
                query = query.Where(p => p.ForecastStartDate <= endDate.Value);
            }

            var predictions = await query
                .OrderByDescending(p => p.PredictionDate)
                .ToListAsync();

            return Result.Ok<IEnumerable<ForecastPrediction>>(predictions);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting predictions for configuration {ConfigurationId}", configurationId);
            return Result.Fail<IEnumerable<ForecastPrediction>>($"Failed to get predictions: {ex.Message}");
        }
    }

    public async Task<Result<IEnumerable<ForecastPrediction>>> GetUsedInPlanningAsync(
        Guid configurationId,
        DateTime? startDate = null,
        DateTime? endDate = null)
    {
        try
        {
            var query = _context.ForecastPredictions
                .AsNoTracking()
                .Include(p => p.ForecastModel)
                .Where(p => p.ForecastModel!.ConfigurationId == configurationId && p.WasUsedInPlanning);

            if (startDate.HasValue)
            {
                query = query.Where(p => p.ForecastStartDate >= startDate.Value);
            }

            if (endDate.HasValue)
            {
                query = query.Where(p => p.ForecastStartDate <= endDate.Value);
            }

            var predictions = await query
                .OrderByDescending(p => p.PredictionDate)
                .ToListAsync();

            return Result.Ok<IEnumerable<ForecastPrediction>>(predictions);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting used predictions for configuration {ConfigurationId}", configurationId);
            return Result.Fail<IEnumerable<ForecastPrediction>>($"Failed to get predictions: {ex.Message}");
        }
    }

    public async Task<Result<IEnumerable<ForecastPrediction>>> GetOverriddenAsync(
        Guid configurationId,
        DateTime? startDate = null,
        DateTime? endDate = null)
    {
        try
        {
            var query = _context.ForecastPredictions
                .AsNoTracking()
                .Include(p => p.ForecastModel)
                .Where(p => p.ForecastModel!.ConfigurationId == configurationId && p.WasOverridden);

            if (startDate.HasValue)
            {
                query = query.Where(p => p.ForecastStartDate >= startDate.Value);
            }

            if (endDate.HasValue)
            {
                query = query.Where(p => p.ForecastStartDate <= endDate.Value);
            }

            var predictions = await query
                .OrderByDescending(p => p.PredictionDate)
                .ToListAsync();

            return Result.Ok<IEnumerable<ForecastPrediction>>(predictions);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting overridden predictions for configuration {ConfigurationId}", configurationId);
            return Result.Fail<IEnumerable<ForecastPrediction>>($"Failed to get predictions: {ex.Message}");
        }
    }

    public async Task<Result<ForecastPrediction>> AddAsync(ForecastPrediction prediction)
    {
        try
        {
            _context.ForecastPredictions.Add(prediction);
            await _context.SaveChangesAsync();

            _logger.LogInformation("Added forecast prediction {PredictionId}", prediction.Id);
            return Result.Ok(prediction);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error adding forecast prediction");
            return Result.Fail<ForecastPrediction>($"Failed to add prediction: {ex.Message}");
        }
    }

    public async Task<Result<ForecastPrediction>> UpdateAsync(ForecastPrediction prediction)
    {
        try
        {
            _context.ForecastPredictions.Update(prediction);
            await _context.SaveChangesAsync();

            _logger.LogInformation("Updated forecast prediction {PredictionId}", prediction.Id);
            return Result.Ok(prediction);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating forecast prediction {PredictionId}", prediction.Id);
            return Result.Fail<ForecastPrediction>($"Failed to update prediction: {ex.Message}");
        }
    }

    public async Task<Result> DeleteAsync(Guid id)
    {
        try
        {
            var prediction = await _context.ForecastPredictions.FindAsync(id);
            if (prediction == null)
            {
                return Result.Fail($"Prediction {id} not found");
            }

            _context.ForecastPredictions.Remove(prediction);
            await _context.SaveChangesAsync();

            _logger.LogInformation("Deleted forecast prediction {PredictionId}", id);
            return Result.Ok();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error deleting forecast prediction {PredictionId}", id);
            return Result.Fail($"Failed to delete prediction: {ex.Message}");
        }
    }

    public async Task<Result<int>> DeleteByModelAsync(Guid forecastModelId)
    {
        try
        {
            var predictions = await _context.ForecastPredictions
                .Where(p => p.ForecastModelId == forecastModelId)
                .ToListAsync();

            _context.ForecastPredictions.RemoveRange(predictions);
            await _context.SaveChangesAsync();

            _logger.LogInformation(
                "Deleted {Count} predictions for model {ModelId}",
                predictions.Count, forecastModelId);

            return Result.Ok(predictions.Count);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error deleting predictions for model {ModelId}", forecastModelId);
            return Result.Fail<int>($"Failed to delete predictions: {ex.Message}");
        }
    }

    public async Task<Result<int>> GetCountForStationAsync(int stationId)
    {
        try
        {
            var count = await _context.ForecastPredictions
                .CountAsync(p => p.StationDeclarationId == stationId);

            return Result.Ok(count);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting prediction count for station {StationId}", stationId);
            return Result.Fail<int>($"Failed to get prediction count: {ex.Message}");
        }
    }
}
