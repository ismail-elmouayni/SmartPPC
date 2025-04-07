using Microsoft.AspNetCore.Mvc;
using SmartPPC.Core.Solver;
using SmartPPC.Core.Solver.GA;

namespace Api.Controllers;

[ApiController]
[Route("[controller]")]
public class ProductionPlanningController : ControllerBase
{
    private readonly ILogger<ProductionPlanningController> _logger;
    private readonly IProductionControlSolver _solver;

    public ProductionPlanningController(
        IProductionControlSolver solver,
        ILogger<ProductionPlanningController> logger)
    {
        _logger = logger;
        _solver = solver;
    }

    [HttpGet("math-model")]
    public IResult GetMathModel()
    {
       var getResult = _solver.GetModel();

       return getResult.IsSuccess ? Results.Ok(getResult.Value) : Results.Problem(string.Join(",", getResult.Errors));
    }

    [HttpGet("resolve")]
    public IResult ResolvePlanning()
    {
        var initResult = _solver.Initialize();
        if (initResult.IsFailed)
        {
            return Results.Problem(string.Join(",", initResult.Errors));
        }

        var exResult = _solver.Resolve(); 

        return  exResult.IsSuccess ? Results.Ok(exResult.Value) :
            Results.Problem(string.Join(",", exResult.Errors));
    }
}
