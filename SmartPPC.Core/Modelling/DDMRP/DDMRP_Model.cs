using DDMRP_AI.Core.Modelling.GenericModel;
using GeneticSharp;

namespace DDMRP_AI.Core.Modelling.DDMRP;

public class DDMRP_Model : IMathModel
{
    public int PeakHorizon { get; set; }
    public float OutputStationDemandVariability { get; set; }
    public IObjective ObjectiveFunction { get; set; }
    public List<IConstraint> Constraints { get; set; }
    public int[][] StationInputPrecedences { get; set; }
    public int[][] StationPrecedences { get; set; }
    public int[] StationInitialBuffer { get; set; }

    public IEnumerable<Station> Stations { get; set; }

    public int DecisionVariablesCount => Stations.Count();

    public List<Variable> Variables { get; set; }


    public DDMRP_Model()
    {
        Stations = new List<Station>();
        Constraints = new List<IConstraint>()
        {
            new ReplenishmentsConstraint(Stations)
        };
        ObjectiveFunction = new MinBuffersAndMaxDemandsObjective(Stations);
    }


    public IOrderedEnumerable<Gene> ToGenes()
    {
        // The solution will consist of determining the station with buffers.
        // The other info will be generated in a deterministic way using DDMRP rules.
        return Stations.OrderBy(s => s.Index)
            .Select(s => new Gene(s.HasBufferInt))
            .OrderBy(g => 0);
    }

    public void SetDecisionVariableRandomly(int index)
    {
        var station = Stations.Single(s => s.Index == index);

        var value = new Random().Next(0, 2);
        station.HasBuffer = (value == 1);

        ReGenerateSolutionIfBuffersModified();
    }

    public void GenerateRandomSolution()
    {
        var orderedStations = Stations.OrderByDescending(s => s.Index);

        // Setting demand variability for the output station
        // given that is known
        var outputStation = orderedStations.First();
        outputStation.DemandVariability = OutputStationDemandVariability;

        foreach (var station in orderedStations)
        {
            var hasBuffer = new Random().Next(0, 2);
            station.HasBuffer = (hasBuffer == 1);

            SetDemandForStation(station.Index);
            SetLeadTimeForStation(station.Index);
            SetLeadTimeFactorForStation(station.Index);

            if (station.Index != outputStation.Index)
                SetDemandVariabilityForStation(station.Index);

            SetQualifiedDemandForStation(station.Index);

            // Before setting order, TOG should be set and replenishment should be calculated
            SetReplenishmentsForStation(station.Index);
            SetOrdersAmounts(station.Index);
            SetBufferForStation(station.Index, StationInitialBuffer[station.Index]);
        }
    }

    public void ReGenerateSolutionIfBuffersModified()
    {
        var orderedStations = Stations.OrderByDescending(s => s.Index);

        foreach (var station in orderedStations)
        {
            SetLeadTimeForStation(station.Index);
            SetLeadTimeFactorForStation(station.Index);
            SetReplenishmentsForStation(station.Index);
            SetOrdersAmounts(station.Index);
            SetBufferForStation(station.Index, StationInitialBuffer[station.Index]);
        }
    }

    public bool AreConstraintsSatisfied()
        => Constraints.Any(c => !c.IsVerified());


    public void SetOrdersAmounts(int stationIndex)
    {
        var station = Stations.Single(s => s.Index == stationIndex);

        if (station.TOG is null)
        {
            throw new InvalidOperationException("TOG should be set before calculating orders amounts");
        }

        station.StateTimeLine.ToList().ForEach(t =>
        {
            // TODO : to check if the formula is correct
            t.OrderAmount = station.HasBufferInt * t.Replenishment * (station.TOG.Value - t.NetFlow);
        });
    }

    public void SetBufferForStation(int stationIndex, int initialBuffer)
    {
        var station = Stations.Single(s => s.Index == stationIndex);

        station.StateTimeLine.ElementAt(0).Buffer = initialBuffer;

        for (var t = 1; t < station.StateTimeLine.Count(); t++)
        {
            var expectedOrderToArrive = (t >= station.LeadTime + 1) ?
                station.StateTimeLine.ElementAt((t - 1) - station.LeadTime).OrderAmount : 0;

            station.StateTimeLine.ElementAt(t).Buffer =
                station.StateTimeLine.ElementAt(t - 1).Buffer + expectedOrderToArrive
                - station.StateTimeLine.ElementAt(t - 1).Demand;
        }
    }

    public void SetLeadTimeForStation(int stationIndex)
    {
        var station = Stations.Single(s => s.Index == stationIndex);

        var leadTime = Stations.Where(s => s.Index < stationIndex)
            .Sum(s => StationPrecedences[stationIndex][s.Index]
                            * (1 - s.HasBufferInt)
                            * (s.ProcessingTime + s.LeadTime));

        station.LeadTime = leadTime;
    }

    public void SetLeadTimeFactorForStation(int stationIndex)
    {
        var station = Stations.Single(s => s.Index == stationIndex);

        var leadTimeFactor = Stations.Min(s => s.LeadTime) / station.LeadTime;
        station.LeadTimeFactor = leadTimeFactor;
    }

    public void SetDemandForStation(int stationIndex)
    {
        var station = Stations.Single(s => s.Index == stationIndex);

        station.StateTimeLine
            .ToList()
            .ForEach(t =>
            {
                t.Demand = Stations
                    .Where(s => s.Index > stationIndex)
                    .Select(s => (s.Index, s.StateTimeLine.Single(st => st.Instant == t.Instant).Demand))
                    .Sum(couple => StationInputPrecedences[stationIndex][couple.Index] * couple.Demand);
            });
    }

    public void SetQualifiedDemandForStation(int stationIndex)
    {
        var station = Stations.Single(s => s.Index == stationIndex);

        if (station.TOR is null)
        {
            throw new InvalidOperationException("TOR should be set before calculating qualified demand");
        }

        station.StateTimeLine
            .ToList()
            .ForEach(s =>
            {
                var peakDemand = station.StateTimeLine
                    .FirstOrDefault(t => t.Instant >= s.Instant &&
                                t.Instant - s.Instant <= PeakHorizon &&
                                t.Demand >= t.Instant * station.TOR)?.Demand;

                if (peakDemand.HasValue)
                {
                    s.QualifiedDemand = s.Demand + peakDemand.Value;
                }
                else
                {
                    s.QualifiedDemand = s.Demand;
                }
            });
    }

    public void SetDemandVariabilityForStations()
    {
        var orderedStations = Stations
            .OrderByDescending(s => s.Index);

        var outputStation = orderedStations.First();
        outputStation.DemandVariability = OutputStationDemandVariability;

        for (var j = outputStation.Index; j >= 1; j--)
        {
            SetDemandVariabilityForStation(j);
        }
    }

    public void SetDemandVariabilityForStation(int stationIndex)
    {
        var station = Stations.First(s => s.Index == stationIndex);

        var variability = Stations.Where(s => s.Index > stationIndex)
            .Sum(s => StationInputPrecedences[stationIndex][s.Index] * s.DemandVariability);

        station.DemandVariability = variability;
    }

    public void SetReplenishmentsForStation(int stationIndex)
    {
        var station = Stations.Single(s => s.Index == stationIndex);

        if (station.TOY is null)
        {
            throw new InvalidOperationException("TOY should be set before calculating replenishments");
        }

        station.StateTimeLine
            .ToList()
            .ForEach(t =>
            {
                if (station.TOY >= t.NetFlow)
                {
                    t.Replenishment = 1;
                }
            });
    }
}