using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using GeneticSharp;

namespace DDMRP_AI.Core.Modelling.GenericModel;

public class MathModel : IMathModel
{
    public List<Variable> Variables { get; set; }
    public int DecisionVariablesCount => Variables.Count;
    public List<IConstraint> Constraints { get; set; }
    public IObjective ObjectiveFunction { get; set; }
    public IOrderedEnumerable<Gene> ToGenes()
    {
        throw new NotImplementedException();
    }

    public void GenerateRandomSolution()
    {
        throw new NotImplementedException();
    }

    public void SetDecisionVariableRandomly(int index)
    {
        throw new NotImplementedException();
    }
}