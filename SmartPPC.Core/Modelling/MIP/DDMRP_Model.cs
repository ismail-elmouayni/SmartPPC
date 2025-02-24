using System.ComponentModel;
using SmartPPC.Core.Modelling.MIP;

namespace DDMRP_AI.Core.Modelling.MIP;

public class DDMRP_Model
{
    public IObjective ObjectiveFunction { get; set; }
    public List<IConstraint> Constraints { get; set; }
    public int[][] StationInputPrecedences { get; set; }
    public int[][] StationPrecedences { get; set; }
    public IEnumerable<Station> Stations { get; set; }


    public DDMRP_Model()
    {
        Stations = new List<Station>();
        Constraints = new List<IConstraint>();
        ObjectiveFunction = new MinBuffersAndMaxDemandsObjective(Stations);
    }

    public bool AreConstraintsSatisfied()
        => Constraints.Any(c => !c.IsVerified());

    public void SetQualifiedDemandForStation(int stationIndex)
    {
        var station = Stations.FirstOrDefault(s => s.Index == stationIndex);
    }

    public void SetLeadTimeForStation(int stationIndex)
    {
        var station = Stations.Single(s => s.Index == stationIndex);

        var leadTime = Stations.Where(s => s.Index < stationIndex)
            .Sum(s => StationPrecedences[stationIndex][s.Index]
                            *(1 - s.HasBufferInt)
                            *(s.ProcessingTime + s.LeadTime));

        station.LeadTime = leadTime;
    }

    public void SetDemandForStation(int stationIndex, int instant)
    {
        var station = Stations.Single(s => s.Index == stationIndex);

        var demand = Stations
            .Where(s => s.Index > stationIndex)
            .Sum(s => StationInputPrecedences[stationIndex][s.Index] * s.ProcessingTime);

        station.StateTimeLine
            .Single(s => s.Instant == instant)
            .Demand = demand;
    }

    public void SetLeadTimeFactorForStation(int stationIndex)
    {
        var station = Stations.Single(s => s.Index == stationIndex);

        var leadTimeFactor =  Stations.Min(s => s.LeadTime)/station.LeadTime;
        station.LeadTimeFactor = leadTimeFactor;
    }

    public void SetDemandVariabilities()
    {
        
    }
}

public class Station
{
    [Description("decision-variable")]
    public bool HasBuffer { get; set; }

    public int HasBufferInt => HasBuffer ? 1 : 0;
    public int Index { get; set; }
    public float ProcessingTime { get; set; }
    public float LeadTime { get; set; }
    public float AverageDemand  => StateTimeLine.Average(state => state.Demand);
    public float LeadTimeFactor { get; set; } 
    public float DemandVariability { get; set; }
    public float TOR { get; set; }
    public float TOG { get; set; }
    public float TOY { get; set; }

    public IOrderedEnumerable<TimeIndexedStationState> StateTimeLine { get; set; }
        = Enumerable.Empty<TimeIndexedStationState>().OrderBy(x => x.Instant);
}

public class TimeIndexedStationState
{
    public int Instant { get; set; }
    public int NetFlow => OnOrderInventory + QualifiedDemand + Buffer;
    public int Buffer { get; set; }
    public float Demand { get; set; }
    public int QualifiedDemand { get; set; }
    public int OnOrderInventory { get; set; }
}

public class MinBuffersAndMaxDemandsObjective : IObjective
{
    public float BuffersOptimizationWeight { get; set; } = 0.5f;
    public float DemandsOptimizationWeight { get; set; } = 0.5f;

    private readonly IEnumerable<Station> _stations;

    public MinBuffersAndMaxDemandsObjective(IEnumerable<Station> stations)
    {
        _stations = stations;
    }

    public double Evalute()
    {
        return BuffersOptimizationWeight * AverageBuffersLevel(_stations) +
                DemandsOptimizationWeight * AverageSatisfiedDemandsLevel(_stations);
    }

    public static double AverageBuffersLevel(IEnumerable<Station> stations)
        => stations.Sum(s => s.HasBuffer ? s.StateTimeLine.Average(state => state.Buffer) : 0);

    public static double AverageSatisfiedDemandsLevel(IEnumerable<Station> stations)
        => stations.Sum(s => s.StateTimeLine.Sum(state => Math.Max(state.Demand - state.Buffer, 0)));
}