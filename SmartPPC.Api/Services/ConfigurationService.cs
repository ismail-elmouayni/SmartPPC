using Newtonsoft.Json;
using SmartPPC.Core.Model.DDMRP;

namespace SmartPPC.Api.Services;

public class ConfigurationService
{
    private readonly string _configFilePath;
    private readonly ILogger<ConfigurationService> _logger;

    public ConfigurationService(IConfiguration configuration, ILogger<ConfigurationService> logger)
    {
        _configFilePath = configuration["ConfigurationFilePath"] ?? "DDRMP_ModelInputs.json";
        _logger = logger;
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
            StationDeclarations = new List<StationDeclaration>()
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
}
