using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using SmartPPC.Api.Services;
using SmartPPC.Core.Model.DDMRP;

namespace SmartPPC.Api.Pages.Settings;

public class GeneralSettingsModel : PageModel
{
    private readonly ConfigurationService _configService;
    private readonly ILogger<GeneralSettingsModel> _logger;

    public GeneralSettingsModel(ConfigurationService configService, ILogger<GeneralSettingsModel> logger)
    {
        _configService = configService;
        _logger = logger;
    }

    public ModelInputs ModelInputs { get; set; } = new ModelInputs();
    public string? ErrorMessage { get; set; }
    public string? SuccessMessage { get; set; }

    public async Task OnGetAsync()
    {
        try
        {
            var config = await _configService.LoadConfigurationAsync();
            ModelInputs = config ?? _configService.CreateDefaultConfiguration();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error loading configuration");
            ErrorMessage = "Error loading configuration. Using default values.";
            ModelInputs = _configService.CreateDefaultConfiguration();
        }
    }
}
