using GeneticSharp;
using Microsoft.Extensions.Logging;
using SmartPPC.Core.Modelling.MIP;
using NCalc;

namespace SmartPPC.Core.Solver.GA;

public class Fitness : IFitness
{
    private MathModel _model;

    public Fitness(MathModel model) => _model = model;

    public double Evaluate(IChromosome chromosome)
    {
        var ppcChromosome = chromosome as Chromosome;
        var solution = ppcChromosome.GetSolution();

        double penalty = 0;

        foreach (var constraint in _model.Constraints)
        {
            if (!EvaluateConstraint(constraint.Expression, solution))
            {
                penalty += 1000; // Pénalité pour les solutions non faisables
            }
        }

        double objectiveValue = EvaluateExpression(_model.Objective.Expression, solution);
        return _model.Objective.Maximize ? objectiveValue - penalty : objectiveValue + penalty;
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