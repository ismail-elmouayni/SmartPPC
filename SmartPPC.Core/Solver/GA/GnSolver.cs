
using SmartPPC.Core.Model;
using FluentResults;
using GeneticSharp;
using SmartPPC.Core.Model.DDMRP;

namespace SmartPPC.Core.Solver.GA;

public sealed class GnSolver : IProductionControlSolver
{
    public const int MinPopulationSize = 50;
    public const int MaxPopulationSize = 100;

    private GeneticAlgorithm? _ga;
    private ModelInputs? _modelInputs;

    public Result<IProductionControlModel?> GetModel(string configFilePath)
    {
        var importResult = ModelBuilder.CreateFromFile(configFilePath);

        return importResult.IsSuccess
            ? Result.Ok((IProductionControlModel?)importResult.Value)
            : Result.Fail<IProductionControlModel?>(importResult.Errors);
    }

    public Result Initialize(string configFilePath)
    {
        var exResult = ModelBuilder.GetInputsFromFile(configFilePath);

        if (exResult.IsFailed)
        {
            return Result.Fail(exResult.Errors);
        }
        
        return Initialize(exResult.Value);
    }

    public Result Initialize(ModelInputs modelInputs)
    {
        _modelInputs = modelInputs;
        var adamChromosome = new Chromosome(modelInputs.NumberOfStations);
        var population = new Population(MinPopulationSize, MaxPopulationSize, adamChromosome);
        var fitness = new Fitness(modelInputs);
        var selection = new TournamentSelection();
        var crossover = new UniformCrossover();
        var mutation = new FlipBitMutation();

        _ga = new GeneticAlgorithm(population, fitness, selection, crossover, mutation)
        {
            MutationProbability = 0.1f,
            Termination = new FitnessStagnationTermination(100)
        };

        return Result.Ok();
    }

    public Result<OptimizationResult> Resolve()
    {
        if (_ga == null)
        {
            return Result.Fail("Genetic Algorithm is not initialized. Please call Initialize method first.");
        }

        _ga.Start();

        var bestChromosome = _ga.BestChromosome as Chromosome;

        var buffersActivation = bestChromosome.GetGenes()
            .Select(g => (int)g.Value)
            .ToArray();

        var modelCreationResult = ModelBuilder.CreateFromInputs(_modelInputs!);
        if (modelCreationResult.IsFailed)
        {
            return Result.Fail<OptimizationResult>(modelCreationResult.Errors);
        }

        IProductionControlModel controlModel = modelCreationResult.Value;
        controlModel.PlanBasedOnBuffersPositions(buffersActivation);

        return Result.Ok(new OptimizationResult(
            controlModel,
            ((Fitness)_ga.Fitness).Curve.DistinctBy(f => f)
            .Select(f => 1/f).OrderDescending()));
    }
}