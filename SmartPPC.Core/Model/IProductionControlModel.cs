using GeneticSharp;
using SmartPPC.Core.Model.DDMRP;

namespace SmartPPC.Core.Model;

public interface IProductionControlModel
{
    int DecisionVariablesCount { get; }
    List<IConstraint> Constraints { get; set; }
    ControlModelStatus Status { get; set; }
    float? ObjectiveFunctionValue { get;}
    IEnumerable<Gene> ToGenes();
    void PlanBasedOnBuffersPositions(int[] buffersActivation);
    float GetAverageBuffersLevel();
    float GetAverageNotSatisfiedDemand();

    public List<StationModel> Stations { get; set; }
}

public enum ControlModelStatus
{
    Created,
    InputsImported,
    Initialized,
    OptimumFound
}