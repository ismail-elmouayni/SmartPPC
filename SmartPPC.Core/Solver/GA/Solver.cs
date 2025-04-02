
using SmartPPC.Core.Modelling;
using FluentResults;
using GeneticSharp;
using SmartPPC.Core.Modelling.DDMRP;

namespace SmartPPC.Core.Solver.GA;

public class Solver : IPPCSolver
{
    public Result<IMathModel?> GetMathModel(string configFilePath)
    {
        var importResult = ModelInputsLoader.ImportModelInputs(configFilePath);

        return importResult.IsSuccess
            ? Result.Ok((IMathModel?)importResult.Value)
            : Result.Fail<IMathModel?>(importResult.Errors);
    }

    public Result<OptimizationResult> Resolve(string configFilePath)
    {
        var exResult = GetMathModel(configFilePath);

        if (exResult.IsFailed)
        {
            return Result.Fail<OptimizationResult>(exResult.Errors);
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

            var bestGenes = bestChromosome.GetGenes();


            return Result.Ok(new OptimizationResult(bestSolution.ToGenes().Select(g => (int)g.Value), 
                fitness.Curve.Distinct().OrderByDescending(e => e)));
        }

        catch (Exception ex)
        {
            return Result.Fail($"An error occured while solving the problem : {ex.Message}");
        }
    }
}