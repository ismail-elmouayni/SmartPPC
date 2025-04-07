using SmartPPC.Core.Model;
using GeneticSharp;

namespace SmartPPC.Core.Solver.GA;

public class Chromosome : BinaryChromosomeBase
{
    private readonly int _numberOfStations;

    public Chromosome(int numberOfStations) : base(numberOfStations)
    {
        _numberOfStations = numberOfStations;

        var stationHasBuffer = RandomizationProvider.Current
            .GetInts(numberOfStations, 0, 2);

        for (var i = 0; i < stationHasBuffer.Length; i++)
        {
            ReplaceGene(i, new Gene(stationHasBuffer[i]));
        }
    }

    public override IChromosome CreateNew()
    {
        return new Chromosome(_numberOfStations);
    }
}