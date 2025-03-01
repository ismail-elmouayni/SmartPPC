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

    public double Evaluate()
    {
        return BuffersOptimizationWeight * AverageBuffersLevel(_stations) +
               DemandsOptimizationWeight * AverageSatisfiedDemandsLevel(_stations);
    }

    public static double AverageBuffersLevel(IEnumerable<Station> stations)
        => stations.Sum(s => s.HasBuffer ? s.StateTimeLine.Average(state => state.Buffer) : 0);

    public static double AverageSatisfiedDemandsLevel(IEnumerable<Station> stations)
        => stations.Sum(s => s.StateTimeLine.Sum(state => Math.Max(state.Demand - state.Buffer, 0)));
}