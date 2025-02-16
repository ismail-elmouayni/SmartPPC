using GeneticSharp;

namespace DDMRP_AI.Core;

public class Fitness : IFitness
{
    public double Evaluate(IChromosome chromosome)
        => Evaluate((Chromosome)chromosome);

    private double Evaluate(Chromosome chromosome)
    {
        return 0;
    }
}