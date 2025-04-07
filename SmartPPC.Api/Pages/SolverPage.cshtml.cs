using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Newtonsoft.Json;
using SmartPPC.Core.Model.DDMRP;
using SmartPPC.Core.Solver.GA;
using FluentResults;

namespace SmartPPC.Api.Pages
{
    public class SolverPageModel : PageModel
    {
        [BindProperty]
        public ModelInputs ModelInputs { get; set; } = new ModelInputs();

        [BindProperty]
        public string StationDeclarationsJson { get; set; } = string.Empty;

        public OptimizationResult? Result { get; set; }

        public void OnGet()
        {
            // Load initial values from DDRMP_ModelInputs.json
            var jsonContent = System.IO.File.ReadAllText("DDRMP_ModelInputs.json");
            ModelInputs = JsonConvert.DeserializeObject<ModelInputs>(jsonContent);
            StationDeclarationsJson = JsonConvert.SerializeObject(ModelInputs.StationDeclarations, Formatting.Indented);
        }

        public IActionResult OnPost()
        {
            // Deserialize the station declarations from the form
            ModelInputs.StationDeclarations = JsonConvert.DeserializeObject<List<StationDeclaration>>(StationDeclarationsJson);

            // Save the updated ModelInputs to a temporary JSON file
            var tempFilePath = Path.GetTempFileName();
            System.IO.File.WriteAllText(tempFilePath, JsonConvert.SerializeObject(ModelInputs));

            // Execute the solver
            var solver = new GnSolver();
            solver.Initialize(tempFilePath);
            var result = solver.Resolve();

            if (result.IsSuccess)
            {
                Result = result.Value as OptimizationResult;
            }
            else
            {
                ModelState.AddModelError(string.Empty, "An error occurred while solving the problem.");
            }

            return Page();
        }
    }
}