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
    List<Variable> Variables { get; set; }
    int DecisionVariablesCount { get; }
    List<IConstraint> Constraints { get; set; }
    MathModelStatus Status { get; set; }

    float? ObjectiveFunctionValue { get;}

    IEnumerable<Gene> ToGenes();
    void GenerateRandomSolution();
    void SetDecisionVariableRandomly(int index);
}

public enum MathModelStatus
{
    Created,
    InputsImported,
    Initialized,
    OptimumFound
}