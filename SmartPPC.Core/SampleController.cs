
using GeneticSharp;

namespace DDMRP_AI.Core;

public class SampleController : ISampleController 
{
    protected GeneticAlgorithm GA { get; private set; }

    public IFitness CreateFitness()
    {
        throw new NotImplementedException();
    }

    public IChromosome CreateChromosome()
    {
        throw new NotImplementedException();
    }

    public virtual ITermination CreateTermination()
    {
        return new FitnessStagnationTermination(100);
    }

    public virtual ICrossover CreateCrossover()
    {
        return new UniformCrossover();
    }


    public virtual IMutation CreateMutation()
    {
        return new UniformMutation(true);
    }

    public virtual ISelection CreateSelection()
    {
        return new EliteSelection();
    }

    public void Initialize()
    {
        throw new NotImplementedException();
    }

    public void ConfigGA(GeneticAlgorithm ga)
    {
        throw new NotImplementedException();
    }

    public void Draw(IChromosome bestChromosome)
    {
        throw new NotImplementedException();
    }
}