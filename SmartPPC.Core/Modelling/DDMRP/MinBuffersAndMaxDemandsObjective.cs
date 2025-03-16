namespace SmartPPC.Core.Modelling.DDMRP;

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
        var criteriaValue = stations.Sum(s => s.HasBuffer ? s.FutureStates.Average(state => state.Buffer) : 0);

        return criteriaValue!.Value;
    }

    public static double AverageSatisfiedDemandsLevel(IEnumerable<Station> stations)
    {
        var outputStations = stations.Where(s => s.IsOutputStation).ToList();

        //if (stations.Any(s => s.AverageDemand is null))
        //{
        //    throw new InvalidOperationException("Average demand and demand not fully calculated yet to determine " +
        //                                        "satisfied demands");
        //}

        var criteriaValue = outputStations.Sum(s =>
            s.FutureStates.Select(state => Math.Min(state.Demand.Value - state.Buffer.Value, 0)).Average());

        return criteriaValue;
    }
}