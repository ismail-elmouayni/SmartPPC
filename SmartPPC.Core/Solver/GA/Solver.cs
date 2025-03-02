
using DDMRP_AI.Core.Modelling;
using FluentResults;
using GeneticSharp;
using SmartPPC.Core.Modelling.DDMRP;

namespace SmartPPC.Core.Solver.GA;

public class Solver : IPPCSolver
{

    public Result<IMathModel?> GetMathModel(string configFilePath)
    {
        var importResult = ModelInputsLoader.ImportModelInputs(configFilePath);

        return importResult.IsSuccess ? 
            Result.Ok((IMathModel?) importResult.Value) : Result.Fail<IMathModel?>(importResult.Errors);
    }

    public Result<IMathModel> Resolve(string configFilePath)
    {
        var exResult = GetMathModel(configFilePath);

        if (exResult.IsFailed)
        {
            return Result.Fail<IMathModel>(exResult.Errors);
        }

        var model = exResult.Value;
        try
        {
            var chromosome = new Chromosome(model);
            var population = new Population(50, 100, chromosome);
            var fitness = new Fitness(model);
            var selection = new EliteSelection();
            var crossover = new UniformCrossover();
            var mutation = new UniformMutation();

            var ga = new GeneticAlgorithm(population, fitness, selection, crossover, mutation)
            {
                MutationProbability = 0.1f,
                Termination = new FitnessStagnationTermination(100)
            };

            ga.Start();

            var bestChromosome = ga.BestChromosome as Chromosome;
            var bestSolution = bestChromosome.GetSolution();

            return Result.Ok(bestSolution);
        }

        catch (Exception ex)
        {
            return Result.Fail($"An error occured while solving the problem : {ex.Message}");
        }
    }

    // Not used right now 
    //public Result<IMathModel> GetGenericMathModel(string configFilePath)
    //{
    //    var jsonObject = JObject.Parse(File.ReadAllText(configFilePath));

    //    var constraints = jsonObject["constraints"]?.ToObject<List<Constraint>>();
    //    var objective = jsonObject["objective"]?.ToObject<Objective>();
    //    var variablesToken = jsonObject["variables"];

    //    if (objective == null)
    //    {
    //        Result.Fail<MathModel?>("Objective is missing in MathModel config file");
    //    }

    //    if (constraints == null)
    //    {
    //        Result.Fail<MathModel?>("Constraints are missing in MathModel config file. " +
    //                                "Event if there is no constraints, constraints section should be added and empty");
    //    }

    //    if (variablesToken == null || !variablesToken.Any())
    //    {
    //        Result.Fail<MathModel?>("Variables are missing in MathModel config file");
    //    }


    //    var variables = (from variableToken in variablesToken
    //                     let variableType = variableToken["variable_type"]?.ToString()
    //                     select variableType switch
    //                     {
    //                         "indexed" => variableToken.ToObject<IndexedVariable>(),
    //                         "time_indexed" => variableToken.ToObject<TimeIndexedVariable>(),
    //                         _ => variableToken.ToObject<Variable>()

    //                     }).ToList();

    //    return Result.Ok(new MathModel
    //    {
    //        Variables = variables,
    //        //Constraints = constraints,
    //        ObjectiveFunction = objective
    //    });
    //}
}