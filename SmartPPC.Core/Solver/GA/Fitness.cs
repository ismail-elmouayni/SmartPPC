using SmartPPC.Core.Modelling;
using GeneticSharp;
using NCalc;

namespace SmartPPC.Core.Solver.GA;

public class Fitness : IFitness
{
    private IMathModel _model;

    public List<double> Curve = new List<double>();
    public Fitness(IMathModel model) => _model = model;

    public double Evaluate(IChromosome chromosome)
    {
        var ppcChromosome = chromosome as Chromosome;
        var solution = ppcChromosome.GetSolution();

        double penalty = 0;

        foreach (var constraint in _model.Constraints)
        {
            // TODO : penality not working properly
            if (!constraint.IsVerified())
            {
                penalty += 1000; // Pénalité pour les solutions non faisables
            }
        }

        var fitnessValue = (double) solution.ObjectiveFunctionValue;
        Curve.Add(fitnessValue);

        return fitnessValue;
    }

    public bool EvaluateConstraint(string expression, Dictionary<string, double> solution)
    {
        var expr = new Expression(expression);
        foreach (var variable in solution)
        {
            expr.Parameters[variable.Key] = variable.Value;
        }

        var result = expr.Evaluate();
        return Convert.ToBoolean(result);
    }

    public double EvaluateExpression(string expression, Dictionary<string, double> solution)
    {
        var expr = new Expression(expression);
        foreach (var variable in solution)
        {
            expr.Parameters[variable.Key] = variable.Value;
        }

        return Convert.ToDouble(expr.Evaluate());
    }
}