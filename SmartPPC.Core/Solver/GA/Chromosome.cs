using SmartPPC.Core.Modelling;
using GeneticSharp;

namespace SmartPPC.Core.Solver.GA;

public class Chromosome(int length) : ChromosomeBase(length), IBinaryChromosome
{
    private readonly IMathModel _model;
    private readonly Dictionary<string, double> _solution;

    public Chromosome(IMathModel model) : this(model.DecisionVariablesCount)
    {
        _model = model;
        _solution = new Dictionary<string, double>();

        _model.GenerateRandomSolution();
        var genes = _model.ToGenes();

        for (int i = 0; i < genes.Count(); i++)
        {
            ReplaceGene(i, genes.ElementAt(i));
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
         _model.SetDecisionVariableRandomly(geneIndex);
         var genes = _model.ToGenes();
        return genes.ElementAt(geneIndex);
    }

    public IMathModel GetSolution()
    {
        return _model;
    }
}