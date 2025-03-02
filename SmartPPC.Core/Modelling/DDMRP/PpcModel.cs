using System.Runtime.InteropServices;
using DDMRP_AI.Core.Modelling.GenericModel;
using GeneticSharp;

namespace DDMRP_AI.Core.Modelling.DDMRP;

public class PpcModel : IMathModel
{
    public int PeakHorizon { get; set; }
    public IObjective ObjectiveFunction { get; set; }
    public List<IConstraint> Constraints { get; set; }
    public int[][] StationInputPrecedences { get; set; }
    public int[][] StationPrecedences { get; set; }
    public int[] StationInitialBuffer { get; set; }
    public List<Station> Stations { get; set; }
    public int DecisionVariablesCount => Stations.Count();
    public List<Variable> Variables { get; set; }

    public PpcModel()
    {
        Stations = new List<Station>();
        Constraints = new List<IConstraint>()
        {
            new ReplenishmentsConstraint(Stations)
        };
        ObjectiveFunction = new MinBuffersAndMaxDemandsObjective(Stations);
    }

    public IEnumerable<Gene> ToGenes()
    {
        // The solution will consist of determining the station with buffers.
        // The other info will be generated in a deterministic way using DDMRP rules.
        return Stations.OrderBy(s => s.Index)
            .Select(s => new Gene(s.HasBufferInt))
            .ToList();
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
        var outputStationsIndices = Stations.Where(s => s.IsOutputStation)
            .Select(s => s.Index)
            .ToList();

        foreach (var station in Stations.OrderByDescending(s => s.Index))
        {
            if (outputStationsIndices.Contains(station.Index))
            {
                continue;
            }

            var hasBuffer = new Random().Next(0, 2);
            station.HasBuffer = (hasBuffer == 1);

            SetDemandForStation(station);
            SetDemandVariabilityForStation(station);
        }

        foreach (var station in Stations.OrderBy(s => s.Index))
        {
            SetLeadTimeForStation(station);
        }

        foreach (var station in Stations.OrderBy(s => s.Index))
        {
            SetLeadTimeFactorForStation(station);
            SetQualifiedDemandForStation(station);
            
            // Before setting order, TOG should be set and replenishment should be calculated
            SetReplenishmentsForStation(station);
            SetOrdersAmounts(station);
            SetBufferForStation(station, StationInitialBuffer[station.Index]);
        }
    }

    public void ReGenerateSolutionIfBuffersModified()
    {
        var orderedStations = Stations.OrderByDescending(s => s.Index);

        foreach (var station in orderedStations)
        {
            SetLeadTimeForStation(station);
            SetLeadTimeFactorForStation(station);
            SetReplenishmentsForStation(station);
            SetOrdersAmounts(station);
            SetBufferForStation(station, StationInitialBuffer[station.Index]);
        }
    }

    public bool AreConstraintsSatisfied()
        => Constraints.Any(c => !c.IsVerified());

    public void SetOrdersAmounts(Station station)
    {
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

    public void SetBufferForStation(Station station, int initialBuffer)
    {
        station.StateTimeLine.ElementAt(0).Buffer = initialBuffer;

        for (var t = 1; t < station.StateTimeLine.Count(); t++)
        {
            var expectedOrderToArrive = (t >= station.LeadTime + 1) ?
                station.StateTimeLine.ElementAt((t - 1) - station.LeadTime!.Value).OrderAmount : 0;

            station.StateTimeLine.ElementAt(t).Buffer =
                station.StateTimeLine.ElementAt(t - 1).Buffer + expectedOrderToArrive
                - station.StateTimeLine.ElementAt(t - 1).Demand!.Value;
        }
    }

    public void SetLeadTimeForStation(Station station)
    {
        if (station.IsInputStation)
        {
            station.LeadTime = station.ProcessingTime;
            return;
        }

        var leadTime = (1-station.HasBufferInt)*(
                        Stations.Where(s => s.Index < station.Index)
                                .Sum(s => StationPrecedences[s.Index][station.Index]*(1 - s.HasBufferInt)*(s.ProcessingTime + s.LeadTime)) 
                        + station.ProcessingTime);

        station.LeadTime = leadTime;
    }

    public void SetLeadTimeFactorForStation(Station station)
    {
        if (station.LeadTime == 0)
        {
            station.LeadTimeFactor = 0;
            return;
        }

        var leadTimeFactor = Stations.Min(s => s.LeadTime) / station.LeadTime;
        station.LeadTimeFactor = leadTimeFactor;
    }

    public void SetDemandForStation(Station station)
    {
        station.StateTimeLine
            .ToList()
            .ForEach(t =>
            {
                t.Demand = Stations
                    .Where(s => s.Index > station.Index)
                    .Select(s => (s.Index, s.StateTimeLine.Single(st => st.Instant == t.Instant).Demand))
                    .Sum(couple => StationInputPrecedences[station.Index][couple.Index] * couple.Demand);
            });
    }

    public void SetQualifiedDemandForStation(Station station)
    {
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
                    s.QualifiedDemand = s.Demand!.Value + peakDemand.Value;
                }
                else
                {
                    s.QualifiedDemand = s.Demand!.Value;
                }
            });
    }

    public void SetDemandVariabilityForStation(Station station)
    {
        var variability = Stations.Where(s => s.Index > station.Index)
            .Sum(s => StationInputPrecedences[station.Index][s.Index] * s.DemandVariability);

        station.DemandVariability = variability;
    }

    public void SetReplenishmentsForStation(Station station)
    {
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