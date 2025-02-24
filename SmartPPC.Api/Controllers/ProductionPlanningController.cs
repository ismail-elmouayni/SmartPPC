using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Infrastructure;
using SmartPPC.Core.Modelling.MIP;
using SmartPPC.Core.Solver;
using FluentResults;

namespace Api.Controllers;

[ApiController]
[Route("[controller]")]
public class ProductionPlanningController : ControllerBase
{
    private static readonly string[] Summaries = new[]
    {
        "Freezing", "Bracing", "Chilly", "Cool", "Mild", "Warm", "Balmy", "Hot", "Sweltering", "Scorching"
    };

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
       var model = _solver.GetMathModel();

       return Results.Ok(model);
    }

    [HttpGet("resolve")]
    public IResult ResolvePlanning()
    {
        var exResult = _solver.Resolve(); 

        return  exResult.IsSuccess ? Results.Ok(exResult.Value) : Results.Problem(string.Join(",", exResult.Errors));
    }
}
