using GeneticSharp;

namespace SmartPPC.Core.Modelling.DDMRP;

public class PpcModel : IMathModel
{
    private readonly IObjective _objectiveFunction;
    public int PlanningHorizon { get; set; }
    public int PastHorizon { get; set; }

    /// <summary>
    /// The horizon in which we look for a peak demand. 
    /// </summary>
    public int PeakHorizon { get; set; }

    /// <summary>
    /// The set of constraints that the model must satisfy.
    /// </summary>
    public List<IConstraint> Constraints { get; set; }

    /// <summary>
    /// The input workflow between stations i.e.
    /// StationInputPrecedences[i][j] = 15 if station i send a 5 inputs station j.
    /// </summary>
    public int[][] StationInputPrecedences { get; set; }

    /// <summary>
    /// the link between stations StationPrecedences[i][j] = 1 if station i is a predecessor of station j.
    /// </summary>
    public int[][] StationPrecedences { get; set; }

    /// <summary>
    /// Initial buffer for each station.
    /// </summary>
    public int[] StationInitialBuffer { get; set; }

    /// <summary>
    /// Stations
    /// </summary>
    public List<Station> Stations { get; set; }

    /// <summary>
    /// The number of decision variables in the model.
    /// </summary>
    public int DecisionVariablesCount => Stations.Count();

    /// <summary>
    /// 
    /// </summary>
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

    public PpcModel(
        List<Station> stations,
        int planningHorizon,
        int peakHorizon,
        int pastHorizon,
        int[] stationInitialBuffer,
        int[][] stationPrecedences,
        int[][] stationInputPrecedences)
    {
        Stations = stations;
        PlanningHorizon = planningHorizon;
        PeakHorizon = peakHorizon;
        PastHorizon = pastHorizon;
        StationPrecedences = stationPrecedences;
        StationInputPrecedences = stationInputPrecedences;
        StationInitialBuffer = stationInitialBuffer;

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
            .Select(s => new Gene(s.HasBufferInt));
    }

    public void SetDecisionVariableRandomly(int stationIndex)
    {
        if (Status < MathModelStatus.Initialized)
        {
            throw new InvalidOperationException(
                $"Model not initialized to regenerate a new buffer config for position {stationIndex}");
        }

        var station = Stations.Single(s => s.Index == stationIndex);

        var value = new Random().Next(0, 2);
        station.HasBuffer = (value == 1);

        ReGenerateSolutionIfBuffersModified();
    }

    public void GenerateRandomSolution()
    {
        foreach (var station in Stations.OrderByDescending(s => s.Index))
        {
            SetAverageDemandForStation(station);

            if (station.IsOutputStation)
            {
                station.HasBuffer = true;
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

        // t = 0
        SetInitialBufferAndOrdersAmount();


        for (int t = 1; t < PlanningHorizon; t++)
        {
            ComputeBuffersOrdersAndReplenishment(t);
        }

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

        SetInitialBufferAndOrdersAmount();

        for (int t = 1; t < PlanningHorizon; t++)
        {
            ComputeBuffersOrdersAndReplenishment(t);
        }
    }


    public void SetLeadTimeForStation(Station station)
    {
        if (station.IsInputStation)
        {
            station.LeadTime = station.ProcessingTime;
            return;
        }

        var leadTime = Stations.Where(s => s.Index < station.Index)
                                .Sum(s => StationInputPrecedences[s.Index][station.Index] * (1 - s.HasBufferInt) * s.LeadTime)
                        + station.ProcessingTime;

        station.LeadTime = leadTime;
    }

    public void SetLeadTimeFactorForStation(Station station)
    {
        if (station.LeadTime == 0)
        {
            station.LeadTimeFactor = 0;
            return;
        }

        var leadTimeFactor = Stations.Where(s => s.LeadTime != 0)
            .Min(s => s.LeadTime) / station.LeadTime;

        station.LeadTimeFactor = leadTimeFactor;
    }

    public void SetAverageDemandForStation(Station station)
    {
        if (station.IsOutputStation)
        {
            station.AverageDemand = (float)station.DemandForecast.Average();
            return;
        }

        int[] forecastedDemand = new int[PlanningHorizon];

        for (int t = 0; t < PlanningHorizon; t++)
        {
            forecastedDemand[t] = Stations.Where(s => s.Index > station.Index)
                .Sum(s => StationInputPrecedences[station.Index][s.Index] * s.DemandForecast[t]);
        }

        station.AverageDemand = (float)forecastedDemand.Average();
        station.DemandForecast = forecastedDemand;
    }


    public void SetDemandVariabilityForStation(Station station)
    {
        var variability = Stations.Where(s => s.Index > station.Index)
            .Sum(s => StationInputPrecedences[station.Index][s.Index] * s.DemandVariability);

        station.DemandVariability = variability;
    }

    public void SetStationDemandBasedOnOrders(Station station, int t)
    {
        if (station.IsOutputStation)
        {
            station.FutureStates[t].Demand = station.DemandForecast[t];
            return;
        }

        bool allNextStationsSet = Stations
            .Where(s => s.Index > station.Index && StationPrecedences[station.Index][s.Index] == 1)
            .All(s => s.FutureStates.Any(state => state.Instant == t));

        if (!allNextStationsSet)
        {
            throw new InvalidOperationException(
                "All next stations demand must be set before setting the demand/order for station " + station.Index);
        }

        station.FutureStates[t].Demand = Stations
            .Where(s => s.Index > station.Index)
            .Select(s => (s.Index, s.FutureStates[t].OrderAmount, s.FutureStates[t].Demand))
            .Sum(couple =>
                StationInputPrecedences[station.Index][couple.Index] * (couple.OrderAmount ?? couple.Demand));
    }

    public void SetStationQualifiedDemand(Station station, int t)
    {
        int? peakDemand = null;
        for (var i = t; i < t + PeakHorizon && i < PlanningHorizon ; i++)
        {
            if (station.DemandForecast[i] >= (i - t + 1) * station.TOR)
            {
                peakDemand = station.DemandForecast[i];
                break;
            }
        }

        if (peakDemand.HasValue)
        {
            station.FutureStates[t].QualifiedDemand = station.FutureStates[t].Demand + peakDemand.Value;
        }
        else
        {
            station.FutureStates[t].QualifiedDemand = station.FutureStates[t].Demand;
        }
    }

    public void SetInitialBufferAndOrdersAmount()
    {
        foreach (var station in Stations.OrderByDescending(s => s.Index))
        {
            var state = station.FutureStates.FirstOrDefault(s => s.Instant == 0);
            if (state == null)
            {
                station.FutureStates.Add( new TimeIndexedStationState
                {
                    Instant = 0
                });
            }
            else
            {
                station.FutureStates[0] = new TimeIndexedStationState
                {
                    Instant = 0
                };
            }

            SetStationDemandBasedOnOrders(station, 0);
            SetStationQualifiedDemand(station, 0);

            if (station.HasBuffer)
            {
                station.FutureStates[0].Buffer = station.PastStates[0].Buffer;
                station.FutureStates[0].OrderAmount = station.PastStates[0].OrderAmount;

                var incomingSupply = GetIncomingSupply(station, -1);

                station.FutureStates[0].OnOrderInventory =
                    -incomingSupply + station.PastStates[0].OrderAmount;
            }
        }
    }


    public void ComputeBuffersOrdersAndReplenishment(int t)
    {
        foreach (var station in Stations.OrderByDescending(s => s.Index))
        {
            var state = station.FutureStates.FirstOrDefault(s => s.Instant == t);

            if (state == null)
            {
                station.FutureStates.Add(new TimeIndexedStationState
                {
                    Instant = t
                });
            }
            else
            {
                station.FutureStates[t] = new TimeIndexedStationState
                {
                    Instant = t
                };
            }

            // Set demand by propagation orders amounts in parent 
            // stations. For output station, the demand is known (external)
            SetStationDemandBasedOnOrders(station, t);

            if (station.HasBuffer)
            {
                SetStationQualifiedDemand(station, t);

                if (station.TOY >= station.FutureStates[t-1].NetFlow)
                {
                    station.FutureStates[t].Replenishment = 1;
                }

                station.FutureStates[t].OrderAmount = station.FutureStates[t].Replenishment ?? 0 *
                                                      (int)Math.Ceiling(station.TOG.Value - station.FutureStates[t].NetFlow.Value);
                
                int incomingSupply;
                try
                {
                    incomingSupply = GetIncomingSupply(station, t-1);
                }
                catch (Exception e)
                {
                    Console.WriteLine(e);
                    throw;
                }
                
                // Buffer
                station.FutureStates[t].Buffer = Math.Max(station.FutureStates[t-1].Buffer.Value - station.FutureStates[t-1].Demand.Value + incomingSupply,
                    0);

                // InOrderInventory
                station.FutureStates[t].OnOrderInventory = station.FutureStates[t-1].OnOrderInventory
                    - incomingSupply + station.FutureStates[t-1].OrderAmount;
            }
        }
    }

    public int GetIncomingSupply(Station station, int t)
    {
        if (station.IsInputStation)
        {
            return t < 0 ? 
                station.PastStates.FirstOrDefault(s => s.Instant == (int)Math.Ceiling(- t + station.LeadTime.Value))?.OrderAmount ?? 0
                : station.FutureStates[t].Demand!.Value;
        }

        var sourceStation = Stations.Single(s => StationPrecedences[s.Index][station.Index] != 0);
        if (sourceStation is { HasBuffer: false, IsInputStation: false })
        {
            bool hasBufferOrInputStation = false;

            while (!hasBufferOrInputStation)
            {
                Station? preStation = Stations.SingleOrDefault(s => StationPrecedences[s.Index][sourceStation.Index] != 0);

                sourceStation = preStation ?? throw new InvalidOperationException("No input station found for station " + sourceStation.Index + 
                                                                                  $" During station {station.Index} incoming supply calculation at t = {t}");
                
                hasBufferOrInputStation = sourceStation.HasBuffer || sourceStation.IsInputStation;
            }
        }

        if ((int)Math.Ceiling(t - station.LeadTime!.Value) >= 0)
        {
            if (sourceStation.IsInputStation)
            {
                return station.FutureStates[(int)Math.Ceiling(t - station.LeadTime!.Value)].OrderAmount ?? 
                       throw new InvalidOperationException($"past order amount for station {station.Index} at {(int)Math.Ceiling(t - station.LeadTime!.Value)}" +
                                                           $"were not calculated yet at {t} to get incoming supply" );
            }

            int stationOrderAmount =  station.FutureStates[(int)Math.Ceiling(t - station.LeadTime!.Value)].OrderAmount ??
                                      throw new InvalidOperationException($"past order amount for station {station.Index} at {(int)Math.Ceiling(t - station.LeadTime!.Value)}" +
                                                                          $"were not calculated yet at {t} to get incoming supply");
            
            int sourceStationBuffer = station.FutureStates[(int)Math.Ceiling(t - station.LeadTime!.Value)].Buffer ??
                                      throw new InvalidOperationException($"source {sourceStation.Index} at supplying {station.Index} should have buffer" +
                                                                          $"Calculating incoming supply at {t}");
            return Math.Min(sourceStationBuffer, stationOrderAmount);   

        }
        else
        {
            return t < 0 ?
                station.PastStates.FirstOrDefault(s => s.Instant == (int)Math.Ceiling(-t + station.LeadTime.Value))?.OrderAmount ?? 0:
                station.PastStates.FirstOrDefault(s => s.Instant == (int)Math.Ceiling(t - station.LeadTime.Value))?.OrderAmount ?? 0;
        }
    }
}