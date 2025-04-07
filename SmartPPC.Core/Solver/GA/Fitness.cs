using GeneticSharp;
using SmartPPC.Core.Model.DDMRP;

namespace SmartPPC.Core.Solver.GA;

public class Fitness : IFitness
{
    private readonly ModelInputs _modelInputs;

    public List<double> Curve = new();
    public Fitness(ModelInputs inputs) => _modelInputs = inputs;

    public double Evaluate(IChromosome chromosome)
    {
        var controlModel = ModelBuilder.CreateFromInputs(_modelInputs)
            .Value;

        var buffersActivation = chromosome.GetGenes()
            .Select(g => (int)g.Value)
            .ToArray();

        controlModel.PlanBasedOnBuffersPositions(buffersActivation);

        var fitnessValue = (double) (1/controlModel.ObjectiveFunctionValue);
        Curve.Add(fitnessValue);

        return fitnessValue;
    }
}