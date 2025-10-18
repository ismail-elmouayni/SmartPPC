using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Newtonsoft.Json;
using SmartPPC.Api.Services;
using SmartPPC.Core.Model.DDMRP;
using SmartPPC.Core.Solver.GA;
using FluentResults;

namespace SmartPPC.Api.Pages
{
    public class SolverPageModel : PageModel
    {
        private readonly ConfigurationService _configService;
        private readonly ILogger<SolverPageModel> _logger;

        public SolverPageModel(ConfigurationService configService, ILogger<SolverPageModel> logger)
        {
            _configService = configService;
            _logger = logger;
        }

        public ModelInputs ModelInputs { get; set; } = new ModelInputs();
        public OptimizationResult? Result { get; set; }
        public bool HasConfiguration { get; set; }
        public int OutputStationsCount { get; set; }

        public async Task OnGetAsync()
        {
            try
            {
                var config = await _configService.LoadConfigurationAsync();
                ModelInputs = config ?? _configService.CreateDefaultConfiguration();
                HasConfiguration = ModelInputs.NumberOfStations > 0 && ModelInputs.PlanningHorizon > 0;
                OutputStationsCount = ModelInputs.StationDeclarations?
                    .Count(s => s.DemandForecast != null && s.DemandForecast.Any()) ?? 0;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error loading configuration");
                ModelInputs = _configService.CreateDefaultConfiguration();
                HasConfiguration = false;
                OutputStationsCount = 0;
            }
        }

        public async Task<IActionResult> OnPostAsync()
        {
            try
            {
                // Load current configuration
                ModelInputs = await _configService.LoadConfigurationAsync() ?? _configService.CreateDefaultConfiguration();
                HasConfiguration = ModelInputs.NumberOfStations > 0 && ModelInputs.PlanningHorizon > 0;
                OutputStationsCount = ModelInputs.StationDeclarations?
                    .Count(s => s.DemandForecast != null && s.DemandForecast.Any()) ?? 0;

                if (!HasConfiguration)
                {
                    ModelState.AddModelError(string.Empty, "No valid configuration found. Please configure the system first.");
                    return Page();
                }

                // Save the ModelInputs to a temporary JSON file
                var tempFilePath = Path.GetTempFileName();
                await System.IO.File.WriteAllTextAsync(tempFilePath, JsonConvert.SerializeObject(ModelInputs));

                _logger.LogInformation("Starting solver with configuration: {Stations} stations, {Horizon} planning horizon",
                    ModelInputs.NumberOfStations, ModelInputs.PlanningHorizon);

                // Execute the solver
                var solver = new GnSolver();
                solver.Initialize(tempFilePath);
                var result = solver.Resolve();

                if (result.IsSuccess)
                {
                    Result = result.Value as OptimizationResult;
                    _logger.LogInformation("Solver completed successfully. Objective value: {ObjectiveValue}",
                        Result?.Solution?.ObjectiveFunctionValue);
                }
                else
                {
                    _logger.LogError("Solver failed: {Errors}", string.Join(", ", result.Errors));
                    ModelState.AddModelError(string.Empty, "An error occurred while solving the problem: " +
                        string.Join(", ", result.Errors.Select(e => e.Message)));
                }

                // Clean up temp file
                if (System.IO.File.Exists(tempFilePath))
                {
                    System.IO.File.Delete(tempFilePath);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error executing solver");
                ModelState.AddModelError(string.Empty, $"Error: {ex.Message}");
            }

            return Page();
        }

        public List<GeneDisplay> GetGeneDisplays()
        {
            if (Result?.Solution == null)
                return new List<GeneDisplay>();

            var genes = Result.Solution.ToGenes().ToList();
            var displays = new List<GeneDisplay>();

            for (int i = 0; i < genes.Count; i++)
            {
                displays.Add(new GeneDisplay
                {
                    StationIndex = i,
                    Value = (int)genes[i].Value
                });
            }

            return displays;
        }

        public class GeneDisplay
        {
            public int StationIndex { get; set; }
            public int Value { get; set; }
        }
    }
}
