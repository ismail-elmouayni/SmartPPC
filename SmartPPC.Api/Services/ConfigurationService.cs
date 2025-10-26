using Newtonsoft.Json;
using SmartPPC.Core.Model.DDMRP;
using SmartPPC.Core.Domain;
using SmartPPC.Api.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Identity;
using System.Collections.Generic;

namespace SmartPPC.Api.Services;

public class ValidationResult
{
    public bool IsSuccess { get; set; }
    public List<string> Errors { get; set; } = new List<string>();

    public static ValidationResult Success() => new ValidationResult { IsSuccess = true };
    public static ValidationResult Fail(params string[] errors) => new ValidationResult { IsSuccess = false, Errors = errors.ToList() };
}

public class ConfigurationDto
{
    public Guid Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
    public bool IsActive { get; set; }
}

public class ConfigurationService
{
    private readonly string _configFilePath;
    private readonly ILogger<ConfigurationService> _logger;
    private readonly ApplicationDbContext _dbContext;
    private readonly UserManager<User> _userManager;
    private readonly IHttpContextAccessor _httpContextAccessor;

    public ConfigurationService(
        IConfiguration configuration,
        ILogger<ConfigurationService> logger,
        ApplicationDbContext dbContext,
        UserManager<User> userManager,
        IHttpContextAccessor httpContextAccessor)
    {
        _configFilePath = configuration["ConfigurationFilePath"] ?? "DDRMP_ModelInputs.json";
        _logger = logger;
        _dbContext = dbContext;
        _userManager = userManager;
        _httpContextAccessor = httpContextAccessor;
    }

    public async Task<ModelInputs?> LoadConfigurationAsync()
    {
        try
        {
            if (!File.Exists(_configFilePath))
            {
                _logger.LogWarning("Configuration file not found: {FilePath}", _configFilePath);
                return CreateDefaultConfiguration();
            }

            var jsonContent = await File.ReadAllTextAsync(_configFilePath);
            var modelInputs = JsonConvert.DeserializeObject<ModelInputs>(jsonContent);
            return modelInputs;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error loading configuration from {FilePath}", _configFilePath);
            throw;
        }
    }

    public async Task SaveConfigurationAsync(ModelInputs modelInputs)
    {
        try
        {
            var jsonContent = JsonConvert.SerializeObject(modelInputs, Formatting.Indented);
            await File.WriteAllTextAsync(_configFilePath, jsonContent);
            _logger.LogInformation("Configuration saved successfully to {FilePath}", _configFilePath);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error saving configuration to {FilePath}", _configFilePath);
            throw;
        }
    }

    public ModelInputs CreateDefaultConfiguration()
    {
        return new ModelInputs
        {
            PlanningHorizon = 30,
            PeakHorizon = 5,
            PastHorizon = 3,
            PeakThreshold = 0,
            StationDeclarations = new List<Core.Model.DDMRP.StationDeclaration>()
        };
    }

    public void ValidateConfiguration(ModelInputs modelInputs)
    {
        if (modelInputs.PlanningHorizon <= 0)
            throw new ArgumentException("PlanningHorizon must be greater than 0");

        if (modelInputs.PeakHorizon <= 0)
            throw new ArgumentException("PeakHorizon must be greater than 0");

        if (modelInputs.PastHorizon <= 0)
            throw new ArgumentException("PastHorizon must be greater than 0");

        if (modelInputs.StationDeclarations != null)
        {
            foreach (var station in modelInputs.StationDeclarations)
            {
                if (station.PastBuffer != null && station.PastBuffer.Length != modelInputs.PastHorizon)
                    throw new ArgumentException($"Station {station.StationIndex}: PastBuffer length must match PastHorizon ({modelInputs.PastHorizon})");

                if (station.PastOrderAmount != null && station.PastOrderAmount.Length != modelInputs.PastHorizon)
                    throw new ArgumentException($"Station {station.StationIndex}: PastOrderAmount length must match PastHorizon ({modelInputs.PastHorizon})");

                if (station.DemandForecast != null && station.DemandForecast.Count != modelInputs.PlanningHorizon)
                    throw new ArgumentException($"Station {station.StationIndex}: DemandForecast length must match PlanningHorizon ({modelInputs.PlanningHorizon})");
            }
        }
    }

    public async Task<ValidationResult> ValidateConfigurationWithBusinessRules(ModelInputs modelInputs)
    {
        var errors = new List<string>();

        // Basic validation
        try
        {
            ValidateConfiguration(modelInputs);
        }
        catch (Exception ex)
        {
            errors.Add(ex.Message);
        }

        // Business logic validation using ModelBuilder
        try
        {
            var modelResult = Core.Model.DDMRP.ModelBuilder.CreateFromInputs(modelInputs);
            if (modelResult.IsFailed)
            {
                errors.AddRange(modelResult.Errors.Select(e => e.Message));
            }
        }
        catch (Exception ex)
        {
            errors.Add($"Business validation failed: {ex.Message}");
        }

        return await Task.FromResult(errors.Any()
            ? ValidationResult.Fail(errors.ToArray())
            : ValidationResult.Success());
    }

    // ==================== Database Operations ====================

    private async Task<string?> GetCurrentUserIdAsync()
    {
        var httpContext = _httpContextAccessor.HttpContext;
        if (httpContext?.User == null)
        {
            _logger.LogWarning("No HTTP context or user available");
            return null;
        }

        var user = await _userManager.GetUserAsync(httpContext.User);
        return user?.Id;
    }

    public async Task<List<ConfigurationDto>> GetAllConfigurationsAsync()
    {
        var userId = await GetCurrentUserIdAsync();
        if (userId == null)
        {
            _logger.LogWarning("Cannot get configurations - user not authenticated");
            return new List<ConfigurationDto>();
        }

        try
        {
            var configs = await _dbContext.Configurations
                .Where(c => c.UserId == userId)
                .OrderByDescending(c => c.UpdatedAt)
                .Select(c => new ConfigurationDto
                {
                    Id = c.Id,
                    Name = c.Name,
                    CreatedAt = c.CreatedAt,
                    UpdatedAt = c.UpdatedAt,
                    IsActive = c.IsActive
                })
                .ToListAsync();

            return configs;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting configurations for user {UserId}", userId);
            throw;
        }
    }

    public async Task<ModelInputs?> LoadConfigurationFromDatabaseAsync(Guid configurationId)
    {
        var userId = await GetCurrentUserIdAsync();
        if (userId == null)
        {
            _logger.LogWarning("Cannot load configuration - user not authenticated");
            return null;
        }

        try
        {
            var config = await _dbContext.Configurations
                .Include(c => c.GeneralSettings)
                .Include(c => c.StationDeclarations)
                    .ThenInclude(s => s.PastBuffers)
                .Include(c => c.StationDeclarations)
                    .ThenInclude(s => s.PastOrderAmounts)
                .Include(c => c.StationDeclarations)
                    .ThenInclude(s => s.DemandForecasts)
                .Include(c => c.StationDeclarations)
                    .ThenInclude(s => s.NextStationInputs)
                .FirstOrDefaultAsync(c => c.Id == configurationId && c.UserId == userId);

            if (config == null)
            {
                _logger.LogWarning("Configuration {ConfigId} not found for user {UserId}", configurationId, userId);
                return null;
            }

            return ConvertToModelInputs(config);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error loading configuration {ConfigId}", configurationId);
            throw;
        }
    }

    public async Task<ModelInputs?> LoadActiveConfigurationAsync()
    {
        var userId = await GetCurrentUserIdAsync();
        if (userId == null)
        {
            _logger.LogWarning("Cannot load active configuration - user not authenticated");
            return null;
        }

        try
        {
            var config = await _dbContext.Configurations
                .Include(c => c.GeneralSettings)
                .Include(c => c.StationDeclarations)
                    .ThenInclude(s => s.PastBuffers)
                .Include(c => c.StationDeclarations)
                    .ThenInclude(s => s.PastOrderAmounts)
                .Include(c => c.StationDeclarations)
                    .ThenInclude(s => s.DemandForecasts)
                .Include(c => c.StationDeclarations)
                    .ThenInclude(s => s.NextStationInputs)
                .FirstOrDefaultAsync(c => c.UserId == userId && c.IsActive);

            if (config == null)
            {
                _logger.LogInformation("No active configuration found for user {UserId}", userId);
                return null;
            }

            return ConvertToModelInputs(config);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error loading active configuration for user {UserId}", userId);
            throw;
        }
    }

    public async Task<Guid> SaveConfigurationToDatabaseAsync(string configName, ModelInputs modelInputs, bool setAsActive = true)
    {
        var userId = await GetCurrentUserIdAsync();
        if (userId == null)
        {
            throw new InvalidOperationException("User not authenticated");
        }

        try
        {
            // Check if a configuration with this name already exists for this user
            var existingConfig = await _dbContext.Configurations
                .FirstOrDefaultAsync(c => c.UserId == userId && c.Name == configName);

            if (existingConfig != null)
            {
                // Update existing configuration
                await UpdateConfigurationInDatabaseAsync(existingConfig.Id, modelInputs, setAsActive);
                return existingConfig.Id;
            }

            // Create new configuration
            var config = new Configuration
            {
                Id = Guid.NewGuid(),
                UserId = userId,
                Name = configName,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow,
                IsActive = setAsActive
            };

            // If setting as active, deactivate all other configurations
            if (setAsActive)
            {
                await DeactivateAllConfigurationsAsync(userId);
            }

            // Add general settings
            config.GeneralSettings = new GeneralSettings
            {
                Id = Guid.NewGuid(),
                ConfigurationId = config.Id,
                PlanningHorizon = modelInputs.PlanningHorizon,
                PeakHorizon = modelInputs.PeakHorizon,
                PastHorizon = modelInputs.PastHorizon,
                PeakThreshold = modelInputs.PeakThreshold,
                NumberOfStations = modelInputs.NumberOfStations
            };

            // Add station declarations
            if (modelInputs.StationDeclarations != null)
            {
                foreach (var stationInput in modelInputs.StationDeclarations)
                {
                    var station = new Core.Domain.StationDeclaration
                    {
                        Id = Guid.NewGuid(),
                        ConfigurationId = config.Id,
                        StationIndex = stationInput.StationIndex,
                        ProcessingTime = stationInput.ProcessingTime,
                        LeadTime = stationInput.LeadTime,
                        InitialBuffer = stationInput.InitialBuffer,
                        DemandVariability = stationInput.DemandVariability
                    };

                    // Add past buffers
                    if (stationInput.PastBuffer != null)
                    {
                        for (int i = 0; i < stationInput.PastBuffer.Length; i++)
                        {
                            station.PastBuffers.Add(new StationPastBuffer
                            {
                                Id = Guid.NewGuid(),
                                StationDeclarationId = station.Id,
                                Instant = i,
                                Value = stationInput.PastBuffer[i]
                            });
                        }
                    }

                    // Add past order amounts
                    if (stationInput.PastOrderAmount != null)
                    {
                        for (int i = 0; i < stationInput.PastOrderAmount.Length; i++)
                        {
                            station.PastOrderAmounts.Add(new StationPastOrderAmount
                            {
                                Id = Guid.NewGuid(),
                                StationDeclarationId = station.Id,
                                Instant = i,
                                Value = stationInput.PastOrderAmount[i]
                            });
                        }
                    }

                    // Add demand forecasts
                    if (stationInput.DemandForecast != null)
                    {
                        for (int i = 0; i < stationInput.DemandForecast.Count; i++)
                        {
                            station.DemandForecasts.Add(new StationDemandForecast
                            {
                                Id = Guid.NewGuid(),
                                StationDeclarationId = station.Id,
                                Instant = i,
                                Value = stationInput.DemandForecast[i]
                            });
                        }
                    }

                    // Add next station inputs
                    if (stationInput.NextStationsInput != null)
                    {
                        foreach (var nextStationInput in stationInput.NextStationsInput)
                        {
                            station.NextStationInputs.Add(new Core.Domain.StationInput
                            {
                                Id = Guid.NewGuid(),
                                SourceStationDeclarationId = station.Id,
                                TargetStationIndex = nextStationInput.NextStationIndex,
                                Percentage = nextStationInput.InputAmount
                            });
                        }
                    }

                    config.StationDeclarations.Add(station);
                }
            }

            _dbContext.Configurations.Add(config);
            await _dbContext.SaveChangesAsync();

            _logger.LogInformation("Configuration {ConfigName} saved with ID {ConfigId} for user {UserId}", configName, config.Id, userId);
            return config.Id;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error saving configuration {ConfigName} to database", configName);
            throw;
        }
    }

    public async Task UpdateConfigurationInDatabaseAsync(Guid configurationId, ModelInputs modelInputs, bool setAsActive = false)
    {
        var userId = await GetCurrentUserIdAsync();
        if (userId == null)
        {
            throw new InvalidOperationException("User not authenticated");
        }

        try
        {
            var config = await _dbContext.Configurations
                .Include(c => c.GeneralSettings)
                .Include(c => c.StationDeclarations)
                    .ThenInclude(s => s.PastBuffers)
                .Include(c => c.StationDeclarations)
                    .ThenInclude(s => s.PastOrderAmounts)
                .Include(c => c.StationDeclarations)
                    .ThenInclude(s => s.DemandForecasts)
                .Include(c => c.StationDeclarations)
                    .ThenInclude(s => s.NextStationInputs)
                .FirstOrDefaultAsync(c => c.Id == configurationId && c.UserId == userId);

            if (config == null)
            {
                throw new InvalidOperationException($"Configuration {configurationId} not found");
            }

            // Update timestamp
            config.UpdatedAt = DateTime.UtcNow;

            // Handle active status
            if (setAsActive && !config.IsActive)
            {
                await DeactivateAllConfigurationsAsync(userId);
                config.IsActive = true;
            }

            // Update general settings
            if (config.GeneralSettings != null)
            {
                config.GeneralSettings.PlanningHorizon = modelInputs.PlanningHorizon;
                config.GeneralSettings.PeakHorizon = modelInputs.PeakHorizon;
                config.GeneralSettings.PastHorizon = modelInputs.PastHorizon;
                config.GeneralSettings.PeakThreshold = modelInputs.PeakThreshold;
                config.GeneralSettings.NumberOfStations = modelInputs.NumberOfStations;
            }
            else
            {
                config.GeneralSettings = new GeneralSettings
                {
                    Id = Guid.NewGuid(),
                    ConfigurationId = config.Id,
                    PlanningHorizon = modelInputs.PlanningHorizon,
                    PeakHorizon = modelInputs.PeakHorizon,
                    PastHorizon = modelInputs.PastHorizon,
                    PeakThreshold = modelInputs.PeakThreshold,
                    NumberOfStations = modelInputs.NumberOfStations
                };
            }

            // Remove existing station declarations and related data
            _dbContext.StationDeclarations.RemoveRange(config.StationDeclarations);

            // Add new station declarations
            config.StationDeclarations.Clear();
            if (modelInputs.StationDeclarations != null)
            {
                foreach (var stationInput in modelInputs.StationDeclarations)
                {
                    var station = new Core.Domain.StationDeclaration
                    {
                        Id = Guid.NewGuid(),
                        ConfigurationId = config.Id,
                        StationIndex = stationInput.StationIndex,
                        ProcessingTime = stationInput.ProcessingTime,
                        LeadTime = stationInput.LeadTime,
                        InitialBuffer = stationInput.InitialBuffer,
                        DemandVariability = stationInput.DemandVariability
                    };

                    // Add past buffers
                    if (stationInput.PastBuffer != null)
                    {
                        for (int i = 0; i < stationInput.PastBuffer.Length; i++)
                        {
                            station.PastBuffers.Add(new StationPastBuffer
                            {
                                Id = Guid.NewGuid(),
                                StationDeclarationId = station.Id,
                                Instant = i,
                                Value = stationInput.PastBuffer[i]
                            });
                        }
                    }

                    // Add past order amounts
                    if (stationInput.PastOrderAmount != null)
                    {
                        for (int i = 0; i < stationInput.PastOrderAmount.Length; i++)
                        {
                            station.PastOrderAmounts.Add(new StationPastOrderAmount
                            {
                                Id = Guid.NewGuid(),
                                StationDeclarationId = station.Id,
                                Instant = i,
                                Value = stationInput.PastOrderAmount[i]
                            });
                        }
                    }

                    // Add demand forecasts
                    if (stationInput.DemandForecast != null)
                    {
                        for (int i = 0; i < stationInput.DemandForecast.Count; i++)
                        {
                            station.DemandForecasts.Add(new StationDemandForecast
                            {
                                Id = Guid.NewGuid(),
                                StationDeclarationId = station.Id,
                                Instant = i,
                                Value = stationInput.DemandForecast[i]
                            });
                        }
                    }

                    // Add next station inputs
                    if (stationInput.NextStationsInput != null)
                    {
                        foreach (var nextStationInput in stationInput.NextStationsInput)
                        {
                            station.NextStationInputs.Add(new Core.Domain.StationInput
                            {
                                Id = Guid.NewGuid(),
                                SourceStationDeclarationId = station.Id,
                                TargetStationIndex = nextStationInput.NextStationIndex,
                                Percentage = nextStationInput.InputAmount
                            });
                        }
                    }

                    config.StationDeclarations.Add(station);
                }
            }

            await _dbContext.SaveChangesAsync();
            _logger.LogInformation("Configuration {ConfigId} updated for user {UserId}", configurationId, userId);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating configuration {ConfigId}", configurationId);
            throw;
        }
    }

    public async Task DeleteConfigurationAsync(Guid configurationId)
    {
        var userId = await GetCurrentUserIdAsync();
        if (userId == null)
        {
            throw new InvalidOperationException("User not authenticated");
        }

        try
        {
            var config = await _dbContext.Configurations
                .FirstOrDefaultAsync(c => c.Id == configurationId && c.UserId == userId);

            if (config == null)
            {
                throw new InvalidOperationException($"Configuration {configurationId} not found");
            }

            _dbContext.Configurations.Remove(config);
            await _dbContext.SaveChangesAsync();

            _logger.LogInformation("Configuration {ConfigId} deleted for user {UserId}", configurationId, userId);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error deleting configuration {ConfigId}", configurationId);
            throw;
        }
    }

    public async Task SetActiveConfigurationAsync(Guid configurationId)
    {
        var userId = await GetCurrentUserIdAsync();
        if (userId == null)
        {
            throw new InvalidOperationException("User not authenticated");
        }

        try
        {
            var config = await _dbContext.Configurations
                .FirstOrDefaultAsync(c => c.Id == configurationId && c.UserId == userId);

            if (config == null)
            {
                throw new InvalidOperationException($"Configuration {configurationId} not found");
            }

            // Deactivate all configurations for this user
            await DeactivateAllConfigurationsAsync(userId);

            // Set this configuration as active
            config.IsActive = true;
            config.UpdatedAt = DateTime.UtcNow;

            await _dbContext.SaveChangesAsync();
            _logger.LogInformation("Configuration {ConfigId} set as active for user {UserId}", configurationId, userId);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error setting active configuration {ConfigId}", configurationId);
            throw;
        }
    }

    private async Task DeactivateAllConfigurationsAsync(string userId)
    {
        var activeConfigs = await _dbContext.Configurations
            .Where(c => c.UserId == userId && c.IsActive)
            .ToListAsync();

        foreach (var config in activeConfigs)
        {
            config.IsActive = false;
        }
    }

    private ModelInputs ConvertToModelInputs(Configuration config)
    {
        var modelInputs = new ModelInputs
        {
            PlanningHorizon = config.GeneralSettings?.PlanningHorizon ?? 0,
            PeakHorizon = config.GeneralSettings?.PeakHorizon ?? 0,
            PastHorizon = config.GeneralSettings?.PastHorizon ?? 0,
            PeakThreshold = config.GeneralSettings?.PeakThreshold ?? 0,
            StationDeclarations = new List<Core.Model.DDMRP.StationDeclaration>()
        };

        if (config.StationDeclarations != null)
        {
            foreach (var station in config.StationDeclarations)
            {
                var pastBuffer = station.PastBuffers
                    .OrderBy(pb => pb.Instant)
                    .Select(pb => pb.Value)
                    .ToArray();

                var pastOrderAmount = station.PastOrderAmounts
                    .OrderBy(poa => poa.Instant)
                    .Select(poa => poa.Value)
                    .ToArray();

                var demandForecast = station.DemandForecasts
                    .OrderBy(df => df.Instant)
                    .Select(df => df.Value)
                    .ToList();

                var nextStationsInput = station.NextStationInputs
                    .Select(nsi => new Core.Model.DDMRP.StationInput(
                        nsi.TargetStationIndex,
                        (int)nsi.Percentage))
                    .ToList();

                modelInputs.StationDeclarations.Add(new Core.Model.DDMRP.StationDeclaration(
                    station.StationIndex,
                    station.ProcessingTime,
                    station.LeadTime,
                    station.InitialBuffer,
                    pastBuffer,
                    pastOrderAmount,
                    station.DemandVariability,
                    demandForecast.Any() ? demandForecast : null,
                    nextStationsInput.Any() ? nextStationsInput : null
                ));
            }
        }

        return modelInputs;
    }
}
