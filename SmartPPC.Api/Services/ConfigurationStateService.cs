using SmartPPC.Core.Model.DDMRP;

namespace SmartPPC.Api.Services;

/// <summary>
/// Scoped service that maintains the current configuration state across pages.
/// This eliminates the need to reload configuration from database on each page navigation.
/// </summary>
public class ConfigurationStateService
{
    private ModelInputs? _currentConfiguration;
    private Guid? _currentConfigId;
    private string? _currentConfigName;

    /// <summary>
    /// Event raised when the configuration state changes.
    /// </summary>
    public event Action? OnConfigurationChanged;

    /// <summary>
    /// Gets the current configuration. Returns null if no configuration is loaded.
    /// </summary>
    public ModelInputs? CurrentConfiguration => _currentConfiguration;

    /// <summary>
    /// Gets the current configuration ID. Returns null if no configuration is loaded.
    /// </summary>
    public Guid? CurrentConfigId => _currentConfigId;

    /// <summary>
    /// Gets the current configuration name. Returns null or empty if no configuration is loaded.
    /// </summary>
    public string? CurrentConfigName => _currentConfigName;

    /// <summary>
    /// Returns true if a configuration is currently loaded.
    /// </summary>
    public bool HasConfiguration => _currentConfiguration != null
        && _currentConfigId.HasValue
        && !string.IsNullOrEmpty(_currentConfigName);

    /// <summary>
    /// Sets the current configuration state.
    /// </summary>
    /// <param name="configId">The configuration ID</param>
    /// <param name="configName">The configuration name</param>
    /// <param name="modelInputs">The configuration data</param>
    public void SetConfiguration(Guid? configId, string? configName, ModelInputs? modelInputs)
    {
        _currentConfigId = configId;
        _currentConfigName = configName;
        _currentConfiguration = modelInputs;

        // Notify subscribers that the configuration has changed
        OnConfigurationChanged?.Invoke();
    }

    /// <summary>
    /// Clears the current configuration state.
    /// </summary>
    public void ClearConfiguration()
    {
        _currentConfigId = null;
        _currentConfigName = null;
        _currentConfiguration = null;

        // Notify subscribers that the configuration has been cleared
        OnConfigurationChanged?.Invoke();
    }

    /// <summary>
    /// Updates the ModelInputs without changing the config ID or name.
    /// Useful when saving changes to the current configuration.
    /// </summary>
    /// <param name="modelInputs">The updated configuration data</param>
    public void UpdateConfiguration(ModelInputs modelInputs)
    {
        _currentConfiguration = modelInputs;

        // Notify subscribers that the configuration has been updated
        OnConfigurationChanged?.Invoke();
    }
}
