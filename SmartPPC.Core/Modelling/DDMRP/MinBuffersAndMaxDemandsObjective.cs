namespace DDMRP_AI.Core.Modelling.DDMRP;

public class MinBuffersAndMaxDemandsObjective : IObjective
{
    public float BuffersOptimizationWeight { get; set; } = 0.5f;
    public float DemandsOptimizationWeight { get; set; } = 0.5f;

    private readonly IEnumerable<Station> _stations;

    public MinBuffersAndMaxDemandsObjective(IEnumerable<Station> stations)
    {
        _stations = stations;
    }

    public float? Evaluate()
    {
        return (float)(BuffersOptimizationWeight * AverageBuffersLevel(_stations) +
            DemandsOptimizationWeight * AverageSatisfiedDemandsLevel(_stations));
    }

    public static double AverageBuffersLevel(IEnumerable<Station> stations)
    {
        var criteriaValue = stations.Sum(s => s.HasBuffer ? s.StateTimeLine.Average(state => state.Buffer) : 0);

        return criteriaValue;
    }

    public static double AverageSatisfiedDemandsLevel(IEnumerable<Station> stations)
    {
        if (stations.Any(s => s.AverageDemand is null))
        {
            throw new InvalidOperationException("Average demand and demand not fully calculated yet to determine " +
                                                "satisfied demands");
        }
        
        var criteriaValue =  stations.Sum(s => s.StateTimeLine.Sum(state => Math.Max(state.Demand!.Value - state.Buffer, 0)));

        return criteriaValue;
    }
}