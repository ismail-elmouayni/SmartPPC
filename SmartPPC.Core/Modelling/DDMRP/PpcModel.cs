using DDMRP_AI.Core.Modelling.GenericModel;
using GeneticSharp;

namespace DDMRP_AI.Core.Modelling.DDMRP;

public class PpcModel : IMathModel
{
    private readonly IObjective _objectiveFunction;

    public int PeakHorizon { get; set; }
    public List<IConstraint> Constraints { get; set; }
    public int[][] StationInputPrecedences { get; set; }
    public int[][] StationPrecedences { get; set; }
    public int[] StationInitialBuffer { get; set; }
    public List<Station> Stations { get; set; }
    public int DecisionVariablesCount => Stations.Count();
    public List<Variable> Variables { get; set; }
    public MathModelStatus Status { get; set; }

    public float? ObjectiveFunctionValue
    {
        get
        {
            if (Status < MathModelStatus.Initialized)
            {
                return null;
            }

            return _objectiveFunction.Evaluate();
        }
    }

    public PpcModel()
    {
        Stations = new List<Station>();
        Constraints = new List<IConstraint>
        {
            new ReplenishmentsConstraint(Stations)
        };

        _objectiveFunction = new MinBuffersAndMaxDemandsObjective(Stations);
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
        if (Status < MathModelStatus.Initialized)
        {
            throw new InvalidOperationException(
                $"Model not initialized to regenerate a new buffer config for position {index}");
        }

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
            SetAverageDemandForStation(station);

            if (outputStationsIndices.Contains(station.Index))
            {
                continue;
            }

            var hasBuffer = new Random().Next(0, 2);
            station.HasBuffer = (hasBuffer == 1);
          
            SetDemandVariabilityForStation(station);
        }

        foreach (var station in Stations.OrderBy(s => s.Index))
        {
            SetLeadTimeForStation(station);
        }

        foreach (var station in Stations.OrderBy(s => s.Index))
        {
            SetLeadTimeFactorForStation(station);
        }

        ManageBuffersForStation();

        //foreach (var station in Stations.OrderBy(s => s.Index))
        //{
        //    SetLeadTimeFactorForStation(station);
        //    SetQualifiedDemandForStation(station);

        //    // Before managing buffer (replenishments, orders amount etc.), TOG and TOY
        //    // should be set and replenishment should be calculated
        //    ManageBuffersForStation(station, StationInitialBuffer[station.Index]);
        //}

        Status = MathModelStatus.Initialized;
    }

    public void ReGenerateSolutionIfBuffersModified()
    {
        foreach (var station in Stations.OrderBy(s => s.Index))
        {
            SetLeadTimeForStation(station);
        }

        foreach (var station in Stations.OrderBy(s => s.Index))
        {
            SetLeadTimeFactorForStation(station);
        }

        ManageBuffersForStation();
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

        var leadTimeFactor = Stations.Where(s => s.LeadTime !=0)
            .Min(s => s.LeadTime) / station.LeadTime;

        station.LeadTimeFactor = leadTimeFactor;
    }

    public void SetAverageDemandForStation(Station station)
    {
        float averageDemand;

        if (station.IsOutputStation)
        {
            averageDemand = (float) station.StateTimeLine.Average(s => s.Demand);
        }
        else
        {
            averageDemand = (float)station.StateTimeLine
                .ToList()
                .Select(t => Stations
                    .Where(s => s.Index > station.Index)
                    .Select(s => (s.Index, s.StateTimeLine.Single(st => st.Instant == t.Instant).Demand))
                    .Sum(couple => StationInputPrecedences[station.Index][couple.Index] * couple.Demand))
                .Average();
        }

        station.AverageDemand = averageDemand;
    }

    //public void SetDemandForStation(Station station)
    //{
    //    station.StateTimeLine
    //        .ToList()
    //        .ForEach(t =>
    //        {
    //            t.Demand = Stations
    //                .Where(s => s.Index > station.Index)
    //                .Select(s => (s.Index, s.StateTimeLine.Single(st => st.Instant == t.Instant).Demand))
    //                .Sum(couple => StationInputPrecedences[station.Index][couple.Index] * couple.Demand);
    //        });
    //}

    //public void SetQualifiedDemandForStation(Station station)
    //{
    //    if (station.TOR is null)
    //    {
    //        throw new InvalidOperationException("TOR should be set before calculating qualified demand");
    //    }

    //    station.StateTimeLine
    //        .ToList()
    //        .ForEach(s =>
    //        {
    //            var peakDemand = station.StateTimeLine
    //                .FirstOrDefault(t => t.Instant >= s.Instant &&
    //                            t.Instant - s.Instant <= PeakHorizon &&
    //                            t.Demand >= t.Instant * station.TOR)?.Demand;

    //            if (peakDemand.HasValue)
    //            {
    //                s.QualifiedDemand = s.Demand!.Value + peakDemand.Value;
    //            }
    //            else
    //            {
    //                s.QualifiedDemand = s.Demand!.Value;
    //            }
    //        });
    //}

    public void SetDemandVariabilityForStation(Station station)
    {
        var variability = Stations.Where(s => s.Index > station.Index)
            .Sum(s => StationInputPrecedences[station.Index][s.Index] * s.DemandVariability);

        station.DemandVariability = variability;
    }


    public void ManageBuffersForStation()
    {
        var planningHorizon = Stations.FirstOrDefault()
            .StateTimeLine.Count;

        for (var t = 0; t < planningHorizon; t++)
        {
            foreach (var station in Stations.OrderByDescending(s => s.Index))
            {
                var stationState = station.StateTimeLine[t];

                if (t == 0)
                {
                    stationState.Buffer = StationInitialBuffer[station.Index];
                }

                // Set demands recursively using orders amount if 
                // the station is a not output station.
                if (!station.IsOutputStation)
                {
                    stationState.Demand = Stations.Where(s => s.Index > station.Index)
                        .Select(s => (s.Index, s.StateTimeLine.Single(st => st.Instant == t).OrderAmount))
                        .Sum(couple => StationInputPrecedences[station.Index][couple.Index] * couple.OrderAmount);
                }


                // Set qualified demand
                var peakDemand = station.StateTimeLine
                    .FirstOrDefault(s => s.Instant >= t &&
                                         s.Instant - t <= PeakHorizon &&
                                         s.Demand >= t* station.TOR)?.Demand;

                if (peakDemand.HasValue)
                {
                    stationState.QualifiedDemand = stationState.Demand!.Value + peakDemand.Value;
                }
                else
                {
                    stationState.QualifiedDemand = stationState.Demand!.Value;
                }

                // Set replenishments and orders 
                if (station.HasBuffer && station.TOY >= stationState.NetFlow)
                {
                    stationState.Replenishment = 1;
                }

                stationState.OrderAmount = stationState.Replenishment * (int)Math.Ceiling(station.TOG.Value - stationState.NetFlow);

                var expectedOrderToArrive = (t >= station.LeadTime + 1) ?
                    station.StateTimeLine.ElementAt((int)Math.Ceiling(stationState.Instant - 1 - station.LeadTime!.Value)).OrderAmount : 0;

                if (t > 0)
                {
                    stationState.Buffer = station.StateTimeLine.ElementAt(stationState.Instant - 1).Buffer + expectedOrderToArrive
                                   - station.StateTimeLine.ElementAt(stationState.Instant - 1).Demand!.Value;
                }
            }
        }

        //station.StateTimeLine.ElementAt(0).Buffer = initialBuffer;

        //if (station.TOY is null)
        //{
        //    throw new InvalidOperationException("TOY should be set before calculating replenishments");
        //}

        //if (station.TOG is null)
        //{
        //    throw new InvalidOperationException("TOG should be set before calculating orders amounts");
        //}

        //station.StateTimeLine
        //    .ToList()
        //    .ForEach(t =>
        //    {
        //        t.Demand = Stations
        //            .Where(s => s.Index > station.Index)
        //            .Select(s => (s.Index, s.StateTimeLine.Single(st => st.Instant == t.Instant).Demand))
        //            .Sum(couple => StationInputPrecedences[station.Index][couple.Index] * couple.Demand);
        //    });

        //station.StateTimeLine
        //    .ToList()
        //    .ForEach(s =>
        //    {
        //        var peakDemand = station.StateTimeLine
        //            .FirstOrDefault(t => t.Instant >= s.Instant &&
        //                                 t.Instant - s.Instant <= PeakHorizon &&
        //                                 t.Demand >= t.Instant * station.TOR)?.Demand;

        //        if (peakDemand.HasValue)
        //        {
        //            s.QualifiedDemand = s.Demand!.Value + peakDemand.Value;
        //        }
        //        else
        //        {
        //            s.QualifiedDemand = s.Demand!.Value;
        //        }
        //    });

        //station.StateTimeLine
        //    .ToList()
        //    .ForEach(state =>
        //    {
        //        if (station.HasBuffer && station.TOY >= state.NetFlow)
        //        {
        //            state.Replenishment = 1;
        //        }

        //        // TODO : to check if the formula is correct
        //        state.OrderAmount = state.Replenishment * (int)Math.Ceiling(station.TOG.Value - state.NetFlow);

        //        var expectedOrderToArrive = (state.Instant >= station.LeadTime + 1) ?
        //            station.StateTimeLine.ElementAt((int)Math.Ceiling(state.Instant - 1 - station.LeadTime!.Value)).OrderAmount : 0;

        //        if (state.Instant > 0)
        //        {
        //            state.Buffer = station.StateTimeLine.ElementAt(state.Instant - 1).Buffer + expectedOrderToArrive
        //                           - station.StateTimeLine.ElementAt(state.Instant - 1).Demand!.Value;
        //        }
        //    });
    }

    //public void ManageBuffersForStation(Station station, int initialBuffer)
    //{
    //    station.StateTimeLine.ElementAt(0).Buffer = initialBuffer;

    //    if (station.TOY is null)
    //    {
    //        throw new InvalidOperationException("TOY should be set before calculating replenishments");
    //    }

    //    if (station.TOG is null)
    //    {
    //        throw new InvalidOperationException("TOG should be set before calculating orders amounts");
    //    }

    //    station.StateTimeLine
    //        .ToList()
    //        .ForEach(t =>
    //        {
    //            t.Demand = Stations
    //                .Where(s => s.Index > station.Index)
    //                .Select(s => (s.Index, s.StateTimeLine.Single(st => st.Instant == t.Instant).Demand))
    //                .Sum(couple => StationInputPrecedences[station.Index][couple.Index] * couple.Demand);
    //        });

    //    station.StateTimeLine
    //        .ToList()
    //        .ForEach(s =>
    //        {
    //            var peakDemand = station.StateTimeLine
    //                .FirstOrDefault(t => t.Instant >= s.Instant &&
    //                                     t.Instant - s.Instant <= PeakHorizon &&
    //                                     t.Demand >= t.Instant * station.TOR)?.Demand;

    //            if (peakDemand.HasValue)
    //            {
    //                s.QualifiedDemand = s.Demand!.Value + peakDemand.Value;
    //            }
    //            else
    //            {
    //                s.QualifiedDemand = s.Demand!.Value;
    //            }
    //        });

    //    station.StateTimeLine
    //        .ToList()
    //        .ForEach(state =>
    //        {
    //            if (station.HasBuffer && station.TOY >= state.NetFlow)
    //            {
    //                state.Replenishment = 1;
    //            }

    //            // TODO : to check if the formula is correct
    //            state.OrderAmount = state.Replenishment * (int)Math.Ceiling(station.TOG.Value - state.NetFlow);

    //            var expectedOrderToArrive = (state.Instant >= station.LeadTime + 1) ?
    //                station.StateTimeLine.ElementAt((int)Math.Ceiling(state.Instant - 1 - station.LeadTime!.Value)).OrderAmount : 0;

    //            if (state.Instant > 0)
    //            {
    //                state.Buffer = station.StateTimeLine.ElementAt(state.Instant - 1).Buffer + expectedOrderToArrive
    //                               - station.StateTimeLine.ElementAt(state.Instant - 1).Demand!.Value;
    //            }
    //        });
    //}

    //public bool AreConstraintsSatisfied()
    //    => Constraints.Any(c => !c.IsVerified());

    //public void SetOrdersAmounts(Station station)
    //{
    //    if (station.TOG is null)
    //    {
    //        throw new InvalidOperationException("TOG should be set before calculating orders amounts");
    //    }

    //    station.StateTimeLine.ToList().ForEach(t =>
    //    {
    //        // TODO : to check if the formula is correct
    //        t.OrderAmount = station.HasBufferInt * t.Replenishment * (int)Math.Ceiling(station.TOG.Value - t.NetFlow);
    //    });
    //}

    //public void SetBufferForStation(Station station, int initialBuffer)
    //{
    //    station.StateTimeLine.ElementAt(0).Buffer = initialBuffer;

    //    for (var t = 1; t < station.StateTimeLine.Count(); t++)
    //    {
    //        var expectedOrderToArrive = (t >= station.LeadTime + 1) ?
    //            station.StateTimeLine.ElementAt((int)Math.Ceiling(t - 1 - station.LeadTime!.Value)).OrderAmount : 0;

    //        station.StateTimeLine.ElementAt(t).Buffer =
    //            station.StateTimeLine.ElementAt(t - 1).Buffer + expectedOrderToArrive
    //            - station.StateTimeLine.ElementAt(t - 1).Demand!.Value;
    //    }
    //}
}