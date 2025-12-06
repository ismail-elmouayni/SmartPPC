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
/// Repository implementation for ForecastModel entity.
/// </summary>
public class ForecastModelRepository : IForecastModelRepository
{
    private readonly ApplicationDbContext _context;
    private readonly ILogger<ForecastModelRepository> _logger;

    public ForecastModelRepository(
        ApplicationDbContext context,
        ILogger<ForecastModelRepository> logger)
    {
        _context = context ?? throw new ArgumentNullException(nameof(context));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    public async Task<Result<ForecastModel?>> GetByIdAsync(Guid id)
    {
        try
        {
            var model = await _context.ForecastModels
                .AsNoTracking()
                .FirstOrDefaultAsync(m => m.Id == id);

            return Result.Ok(model);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting forecast model by ID {ModelId}", id);
            return Result.Fail<ForecastModel?>($"Failed to get model: {ex.Message}");
        }
    }

    public async Task<Result<ForecastModel?>> GetActiveModelAsync(Guid configurationId)
    {
        try
        {
            var model = await _context.ForecastModels
                .AsNoTracking()
                .Where(m => m.ConfigurationId == configurationId && m.IsActive)
                .OrderByDescending(m => m.CreatedAt)
                .FirstOrDefaultAsync();

            return Result.Ok(model);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting active model for configuration {ConfigurationId}", configurationId);
            return Result.Fail<ForecastModel?>($"Failed to get active model: {ex.Message}");
        }
    }

    public async Task<Result<IEnumerable<ForecastModel>>> GetByConfigurationAsync(
        Guid configurationId,
        bool includeInactive = true)
    {
        try
        {
            var query = _context.ForecastModels
                .AsNoTracking()
                .Where(m => m.ConfigurationId == configurationId);

            if (!includeInactive)
            {
                query = query.Where(m => m.IsActive);
            }

            var models = await query
                .OrderByDescending(m => m.CreatedAt)
                .ToListAsync();

            return Result.Ok<IEnumerable<ForecastModel>>(models);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting models for configuration {ConfigurationId}", configurationId);
            return Result.Fail<IEnumerable<ForecastModel>>($"Failed to get models: {ex.Message}");
        }
    }

    public async Task<Result<IEnumerable<ForecastModel>>> GetByStationAsync(
        int stationId,
        bool includeInactive = true)
    {
        try
        {
            var query = _context.ForecastModels
                .AsNoTracking()
                .Where(m => m.Configuration.StationDeclarations.Any(s => s.StationIndex == stationId));

            if (!includeInactive)
            {
                query = query.Where(m => m.IsActive);
            }

            var models = await query
                .OrderByDescending(m => m.CreatedAt)
                .ToListAsync();

            return Result.Ok<IEnumerable<ForecastModel>>(models);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting models for station {StationId}", stationId);
            return Result.Fail<IEnumerable<ForecastModel>>($"Failed to get models: {ex.Message}");
        }
    }

    public async Task<Result<ForecastModel>> AddAsync(ForecastModel model)
    {
        try
        {
            _context.ForecastModels.Add(model);
            await _context.SaveChangesAsync();

            _logger.LogInformation("Added forecast model {ModelId}", model.Id);
            return Result.Ok(model);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error adding forecast model");
            return Result.Fail<ForecastModel>($"Failed to add model: {ex.Message}");
        }
    }

    public async Task<Result<ForecastModel>> UpdateAsync(ForecastModel model)
    {
        try
        {
            _context.ForecastModels.Update(model);
            await _context.SaveChangesAsync();

            _logger.LogInformation("Updated forecast model {ModelId}", model.Id);
            return Result.Ok(model);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating forecast model {ModelId}", model.Id);
            return Result.Fail<ForecastModel>($"Failed to update model: {ex.Message}");
        }
    }

    public async Task<Result> DeleteAsync(Guid id)
    {
        try
        {
            var model = await _context.ForecastModels.FindAsync(id);
            if (model == null)
            {
                return Result.Fail($"Model {id} not found");
            }

            _context.ForecastModels.Remove(model);
            await _context.SaveChangesAsync();

            _logger.LogInformation("Deleted forecast model {ModelId}", id);
            return Result.Ok();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error deleting forecast model {ModelId}", id);
            return Result.Fail($"Failed to delete model: {ex.Message}");
        }
    }

    public async Task<Result> DeactivateAllForConfigurationAsync(Guid configurationId)
    {
        try
        {
            var models = await _context.ForecastModels
                .Where(m => m.ConfigurationId == configurationId && m.IsActive)
                .ToListAsync();

            foreach (var model in models)
            {
                model.IsActive = false;
            }

            await _context.SaveChangesAsync();

            _logger.LogInformation(
                "Deactivated {Count} models for configuration {ConfigurationId}",
                models.Count, configurationId);

            return Result.Ok();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error deactivating models for configuration {ConfigurationId}", configurationId);
            return Result.Fail($"Failed to deactivate models: {ex.Message}");
        }
    }

    public async Task<Result<IEnumerable<ForecastModel>>> GetAllAsync(bool? activeOnly = null)
    {
        try
        {
            var query = _context.ForecastModels.AsNoTracking();

            if (activeOnly.HasValue)
            {
                query = query.Where(m => m.IsActive == activeOnly.Value);
            }

            var models = await query
                .OrderByDescending(m => m.CreatedAt)
                .ToListAsync();

            return Result.Ok<IEnumerable<ForecastModel>>(models);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting all models");
            return Result.Fail<IEnumerable<ForecastModel>>($"Failed to get models: {ex.Message}");
        }
    }

    public async Task<Result<bool>> ExistsAsync(Guid id)
    {
        try
        {
            var exists = await _context.ForecastModels
                .AnyAsync(m => m.Id == id);

            return Result.Ok(exists);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error checking if model {ModelId} exists", id);
            return Result.Fail<bool>($"Failed to check model existence: {ex.Message}");
        }
    }
}
