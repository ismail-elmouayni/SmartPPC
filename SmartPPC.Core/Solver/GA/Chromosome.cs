using GeneticSharp;
using SmartPPC.Core.Modelling.MIP;

namespace SmartPPC.Core.Solver.GA;

public class Chromosome(int length) : ChromosomeBase(length), IBinaryChromosome
{
    private readonly MathModel _model;
    private readonly Dictionary<string, double> _solution;

    public Chromosome(MathModel model) : this(model.Variables.Count)
    {
        _model = model;
        _solution = new Dictionary<string, double>();

        foreach (var variable in model.Variables)
        {
            var gene = new Gene(new Random().NextDouble() * (variable.UpperBound - variable.LowerBound) + variable.LowerBound);
            ReplaceGene(_model.Variables.IndexOf(variable), gene);
        }
    }

    public override IChromosome CreateNew()
    {
        return new Chromosome(_model);
    }

    public void FlipGene(int index)
    {
        //TODO : to be implemented
    }

    public override Gene GenerateGene(int geneIndex)
    {
        var variable = _model.Variables[geneIndex];
        return new Gene(new Random().NextDouble() * (variable.UpperBound - variable.LowerBound) + variable.LowerBound);
    }

    public Dictionary<string, double> GetSolution()
    {
        for (int i = 0; i < _model.Variables.Count; i++)
        {
            _solution[_model.Variables[i].Name] = (double)GetGene(i).Value;
        }
        return _solution;
    }
}