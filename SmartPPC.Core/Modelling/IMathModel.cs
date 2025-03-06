using GeneticSharp;

namespace SmartPPC.Core.Modelling;

public interface IMathModel
{
    int DecisionVariablesCount { get; }
    List<IConstraint> Constraints { get; set; }
    MathModelStatus Status { get; set; }
    float? ObjectiveFunctionValue { get;}
    IEnumerable<Gene> ToGenes();
    void GenerateRandomSolution();
    void SetDecisionVariableRandomly(int stationIndex);
}

public enum MathModelStatus
{
    Created,
    InputsImported,
    Initialized,
    OptimumFound
}