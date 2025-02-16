using GeneticSharp;
namespace DDMRP_AI.Core;

public class Chromosome(int length) : ChromosomeBase(length)
{
    private readonly List<int> _buffersConfig  = Enumerable.Range(0, length).ToList();

    public override Gene GenerateGene(int geneIndex) 
        => new (new Random().Next(0,2));

    public override IChromosome CreateNew() 
        => new Chromosome(_buffersConfig.Count);
}