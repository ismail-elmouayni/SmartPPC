using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using DDMRP_AI.Core.Modelling.GenericModel;
using GeneticSharp;

namespace DDMRP_AI.Core.Modelling;

public interface IMathModel
{
    public List<Variable> Variables { get; set; }
    public int DecisionVariablesCount { get; }
    public List<IConstraint> Constraints { get; set; }
    public IObjective ObjectiveFunction { get; set; }

    IOrderedEnumerable<Gene> ToGenes();
    void GenerateRandomSolution();
    void SetDecisionVariableRandomly(int index);
}