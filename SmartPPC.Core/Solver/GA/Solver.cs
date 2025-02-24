
using System.Runtime.CompilerServices;
using DDMRP_AI.Core.Modelling.MIP;
using FluentResults;
using GeneticSharp;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using SmartPPC.Core.Modelling.MIP;

namespace SmartPPC.Core.Solver.GA;

public class Solver : IPPCSolver
{
    public Result<MathModel?> GetMathModel(string configFilePath)
    {
        var jsonObject = JObject.Parse(File.ReadAllText(configFilePath));

        var constraints = jsonObject["constraints"]?.ToObject<List<Constraint>>();
        var objective = jsonObject["objective"]?.ToObject<Objective>();
        var variablesToken = jsonObject["variables"];

        if (objective == null)
        {
            Result.Fail<MathModel?>("Objective is missing in MathModel config file");
        }

        if (constraints == null)
        {
            Result.Fail<MathModel?>("Constraints are missing in MathModel config file. " +
                                    "Event if there is no constraints, constraints section should be added and empty");
        }

        if (variablesToken == null || !variablesToken.Any())
        {
            Result.Fail<MathModel?>("Variables are missing in MathModel config file");
        }


        var variables = (from variableToken in variablesToken
            let variableType = variableToken["variable_type"]?.ToString()
            select variableType switch
            {
                "indexed" => variableToken.ToObject<IndexedVariable>(),
                "time_indexed" => variableToken.ToObject<TimeIndexedVariable>(),
                _ => variableToken.ToObject<Variable>()

            }).ToList();

        return Result.Ok(new MathModel
        {
            Variables = variables,
            Constraints = constraints,
            Objective = objective
        });
    }

    public Result<Dictionary<string, double>> Resolve(string configFilePath)
    {
        var model = GetMathModel(configFilePath);

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
}