using Microsoft.AspNetCore.Mvc;
using SmartPPC.Core.Solver;
using SmartPPC.Core.Solver.GA;

namespace Api.Controllers;

[ApiController]
[Route("[controller]")]
public class ProductionPlanningController : ControllerBase
{
    private readonly ILogger<ProductionPlanningController> _logger;
    private readonly IPPCSolver _solver;

    public ProductionPlanningController(
        IPPCSolver solver,
        ILogger<ProductionPlanningController> logger)
    {
        _logger = logger;
        _solver = solver;
    }

    [HttpGet("math-model")]
    public IResult GetMathModel()
    {
       var getResult = _solver.GetMathModel();

       return getResult.IsSuccess ? Results.Ok(getResult.Value) : Results.Problem(string.Join(",", getResult.Errors));
    }

    [HttpGet("resolve")]
    public IResult ResolvePlanning()
    {
        var exResult = _solver.Resolve(); 

        return  exResult.IsSuccess ? Results.Ok(exResult.Value) :
            Results.Problem(string.Join(",", exResult.Errors));
    }
}
