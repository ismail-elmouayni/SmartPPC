using Microsoft.AspNetCore.Mvc.RazorPages;
using SmartPPC.Api.Services;
using SmartPPC.Core.Model.DDMRP;

namespace SmartPPC.Api.Pages;

public class IndexModel : PageModel
{
    private readonly ConfigurationService _configService;
    private readonly ILogger<IndexModel> _logger;

    public IndexModel(ConfigurationService configService, ILogger<IndexModel> logger)
    {
        _configService = configService;
        _logger = logger;
    }

    public ModelInputs ModelInputs { get; set; } = new ModelInputs();
    public int OutputStationsCount { get; set; }

    public async Task OnGetAsync()
    {
        try
        {
            var config = await _configService.LoadConfigurationAsync();
            ModelInputs = config ?? _configService.CreateDefaultConfiguration();

            OutputStationsCount = ModelInputs.StationDeclarations?
                .Count(s => s.DemandForecast != null || s.DemandVariability.HasValue) ?? 0;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error loading configuration");
            ModelInputs = _configService.CreateDefaultConfiguration();
            OutputStationsCount = 0;
        }
    }
}
