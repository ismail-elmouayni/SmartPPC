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
/// Repository implementation for ForecastTrainingData entity.
/// </summary>
public class ForecastTrainingDataRepository : IForecastTrainingDataRepository
{
    private readonly ApplicationDbContext _context;
    private readonly ILogger<ForecastTrainingDataRepository> _logger;

    public ForecastTrainingDataRepository(
        ApplicationDbContext context,
        ILogger<ForecastTrainingDataRepository> logger)
    {
        _context = context ?? throw new ArgumentNullException(nameof(context));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    public async Task<Result<ForecastTrainingData?>> GetByIdAsync(Guid id)
    {
        try
        {
            var data = await _context.ForecastTrainingData
                .AsNoTracking()
                .FirstOrDefaultAsync(d => d.Id == id);

            return Result.Ok(data);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting training data by ID {Id}", id);
            return Result.Fail<ForecastTrainingData?>($"Failed to get training data: {ex.Message}");
        }
    }

    public async Task<Result<IEnumerable<ForecastTrainingData>>> GetByStationAsync(
        int stationId,
        DateTime startDate,
        DateTime endDate)
    {
        try
        {
            var data = await _context.ForecastTrainingData
                .AsNoTracking()
                .Where(d => d.StationDeclarationId == stationId
                         && d.ObservationDate >= startDate
                         && d.ObservationDate <= endDate)
                .OrderBy(d => d.ObservationDate)
                .ToListAsync();

            return Result.Ok<IEnumerable<ForecastTrainingData>>(data);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting training data for station {StationId}", stationId);
            return Result.Fail<IEnumerable<ForecastTrainingData>>($"Failed to get training data: {ex.Message}");
        }
    }

    public async Task<Result<Dictionary<int, IEnumerable<ForecastTrainingData>>>> GetByConfigurationAsync(
        Guid configurationId,
        DateTime startDate,
        DateTime endDate)
    {
        try
        {
            var data = await _context.ForecastTrainingData
                .AsNoTracking()
                .Where(d => d.ConfigurationId == configurationId
                         && d.ObservationDate >= startDate
                         && d.ObservationDate <= endDate)
                .OrderBy(d => d.ObservationDate)
                .ToListAsync();

            var groupedData = data
                .GroupBy(d => d.StationDeclarationId)
                .ToDictionary(g => g.Key, g => g.AsEnumerable());

            return Result.Ok(groupedData);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting training data for configuration {ConfigurationId}", configurationId);
            return Result.Fail<Dictionary<int, IEnumerable<ForecastTrainingData>>>($"Failed to get training data: {ex.Message}");
        }
    }

    public async Task<Result<int>> GetCountForStationAsync(int stationId)
    {
        try
        {
            var count = await _context.ForecastTrainingData
                .CountAsync(d => d.StationDeclarationId == stationId);

            return Result.Ok(count);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting count for station {StationId}", stationId);
            return Result.Fail<int>($"Failed to get count: {ex.Message}");
        }
    }

    public async Task<Result<(DateTime? earliestDate, DateTime? latestDate)?>> GetDateRangeForStationAsync(int stationId)
    {
        try
        {
            var dates = await _context.ForecastTrainingData
                .AsNoTracking()
                .Where(d => d.StationDeclarationId == stationId)
                .Select(d => d.ObservationDate)
                .ToListAsync();

            if (!dates.Any())
            {
                return Result.Ok<(DateTime?, DateTime?)?>(null);
            }

            var range = (dates.Min(), dates.Max());
            return Result.Ok<(DateTime?, DateTime?)?>((range));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting date range for station {StationId}", stationId);
            return Result.Fail<(DateTime?, DateTime?)?>($"Failed to get date range: {ex.Message}");
        }
    }

    public async Task<Result<ForecastTrainingData>> AddAsync(ForecastTrainingData trainingData)
    {
        try
        {
            _context.ForecastTrainingData.Add(trainingData);
            await _context.SaveChangesAsync();

            _logger.LogInformation(
                "Added training data observation for station {StationId} on {Date}",
                trainingData.StationDeclarationId, trainingData.ObservationDate);

            return Result.Ok(trainingData);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error adding training data");
            return Result.Fail<ForecastTrainingData>($"Failed to add training data: {ex.Message}");
        }
    }

    public async Task<Result<int>> AddRangeAsync(IEnumerable<ForecastTrainingData> trainingDataList)
    {
        try
        {
            var dataList = trainingDataList.ToList();
            _context.ForecastTrainingData.AddRange(dataList);
            await _context.SaveChangesAsync();

            _logger.LogInformation("Added {Count} training data observations", dataList.Count);
            return Result.Ok(dataList.Count);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error adding training data in batch");
            return Result.Fail<int>($"Failed to add training data: {ex.Message}");
        }
    }

    public async Task<Result<int>> DeleteOldDataAsync(Guid configurationId, DateTime cutoffDate)
    {
        try
        {
            var oldData = await _context.ForecastTrainingData
                .Where(d => d.ConfigurationId == configurationId && d.ObservationDate < cutoffDate)
                .ToListAsync();

            _context.ForecastTrainingData.RemoveRange(oldData);
            await _context.SaveChangesAsync();

            _logger.LogInformation(
                "Deleted {Count} old training data observations for configuration {ConfigurationId}",
                oldData.Count, configurationId);

            return Result.Ok(oldData.Count);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error deleting old training data for configuration {ConfigurationId}", configurationId);
            return Result.Fail<int>($"Failed to delete old data: {ex.Message}");
        }
    }

    public async Task<Result<int>> DeleteByStationAsync(int stationId)
    {
        try
        {
            var data = await _context.ForecastTrainingData
                .Where(d => d.StationDeclarationId == stationId)
                .ToListAsync();

            _context.ForecastTrainingData.RemoveRange(data);
            await _context.SaveChangesAsync();

            _logger.LogInformation(
                "Deleted {Count} training data observations for station {StationId}",
                data.Count, stationId);

            return Result.Ok(data.Count);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error deleting training data for station {StationId}", stationId);
            return Result.Fail<int>($"Failed to delete training data: {ex.Message}");
        }
    }

    public async Task<Result<int>> DeleteByConfigurationAsync(Guid configurationId)
    {
        try
        {
            var data = await _context.ForecastTrainingData
                .Where(d => d.ConfigurationId == configurationId)
                .ToListAsync();

            _context.ForecastTrainingData.RemoveRange(data);
            await _context.SaveChangesAsync();

            _logger.LogInformation(
                "Deleted {Count} training data observations for configuration {ConfigurationId}",
                data.Count, configurationId);

            return Result.Ok(data.Count);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error deleting training data for configuration {ConfigurationId}", configurationId);
            return Result.Fail<int>($"Failed to delete training data: {ex.Message}");
        }
    }

    public async Task<Result<bool>> HasSufficientDataAsync(int stationId, int minimumDays = 180)
    {
        try
        {
            var dateRangeResult = await GetDateRangeForStationAsync(stationId);

            if (dateRangeResult.IsFailed || !dateRangeResult.Value.HasValue)
            {
                return Result.Ok(false);
            }

            var (earliestDate, latestDate) = dateRangeResult.Value.Value;

            if (!earliestDate.HasValue || !latestDate.HasValue)
            {
                return Result.Ok(false);
            }

            var daysCovered = (latestDate.Value - earliestDate.Value).Days;
            return Result.Ok(daysCovered >= minimumDays);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error checking data sufficiency for station {StationId}", stationId);
            return Result.Fail<bool>($"Failed to check data sufficiency: {ex.Message}");
        }
    }
}
