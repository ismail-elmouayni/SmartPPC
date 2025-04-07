using GeneticSharp;

namespace SmartPPC.Core.Model.DDMRP;

public class ProductionControlModel : IProductionControlModel
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
    public List<StationModel> Stations { get; set; }

    /// <summary>
    /// The number of decision variables in the model.
    /// </summary>
    public int DecisionVariablesCount => Stations.Count();

    /// <summary>
    /// 
    /// </summary>
    public ControlModelStatus Status { get; set; }


    public float? ObjectiveFunctionValue
    {
        get
        {
            if (Status < ControlModelStatus.Initialized)
            {
                return null;
            }

            return _objectiveFunction.Evaluate();
        }
    }

    public ProductionControlModel(
        List<StationModel> stations,
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

    public void PlanBasedOnBuffersPositions(int[] buffersActivation)
    {
        foreach (var station in Stations.OrderByDescending(s => s.Index))
        {
            SetAverageDemandForStation(station);
            station.HasBuffer = buffersActivation[station.Index] == 1;
            
            if (station.IsOutputStation)
            {
                station.HasBuffer = true;
                continue;
            }

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

        Status = ControlModelStatus.Initialized;
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


    public void SetLeadTimeForStation(StationModel stationModel)
    {
        if (stationModel.IsInputStation)
        {
            stationModel.LeadTime = stationModel.ProcessingTime;
            return;
        }

        var leadTime = Stations.Where(s => s.Index < stationModel.Index)
                           .Sum(s => StationInputPrecedences[s.Index][stationModel.Index] * (1 - s.HasBufferInt) *
                                     s.LeadTime)
                       + stationModel.ProcessingTime;

        stationModel.LeadTime = leadTime;
    }

    public void SetLeadTimeFactorForStation(StationModel stationModel)
    {
        if (stationModel.LeadTime == 0)
        {
            stationModel.LeadTimeFactor = 0;
            return;
        }

        var leadTimeFactor = Stations.Where(s => s.LeadTime != 0)
            .Min(s => s.LeadTime) / stationModel.LeadTime;

        stationModel.LeadTimeFactor = leadTimeFactor;
    }

    public void SetAverageDemandForStation(StationModel stationModel)
    {
        if (stationModel.IsOutputStation)
        {
            stationModel.AverageDemand = (float)stationModel.DemandForecast.Average();
            return;
        }

        int[] forecastedDemand = new int[PlanningHorizon];

        for (int t = 0; t < PlanningHorizon; t++)
        {
            forecastedDemand[t] = Stations.Where(s => s.Index > stationModel.Index)
                .Sum(s => StationInputPrecedences[stationModel.Index][s.Index] * s.DemandForecast[t]);
        }

        stationModel.AverageDemand = (float)forecastedDemand.Average();
        stationModel.DemandForecast = forecastedDemand;
    }


    public void SetDemandVariabilityForStation(StationModel stationModel)
    {
        var variability = Stations.Where(s => s.Index > stationModel.Index)
            .Sum(s => StationInputPrecedences[stationModel.Index][s.Index] * s.DemandVariability);

        stationModel.DemandVariability = variability;
    }

    public void SetStationDemandBasedOnOrders(StationModel stationModel, int t)
    {
        if (stationModel.IsOutputStation)
        {
            stationModel.FutureStates[t].Demand = stationModel.DemandForecast[t];
            return;
        }

        bool allNextStationsSet = Stations
            .Where(s => s.Index > stationModel.Index && StationPrecedences[stationModel.Index][s.Index] == 1)
            .All(s => s.FutureStates.Any(state => state.Instant == t));

        if (!allNextStationsSet)
        {
            throw new InvalidOperationException(
                "All next stations demand must be set before setting the demand/order for station " + stationModel.Index);
        }

        stationModel.FutureStates[t].Demand = Stations
            .Where(s => s.Index > stationModel.Index)
            .Select(s => (s.Index, s.FutureStates[t].OrderAmount, s.FutureStates[t].Demand))
            .Sum(couple =>
                StationInputPrecedences[stationModel.Index][couple.Index] * (couple.OrderAmount ?? couple.Demand));
    }

    public void SetStationQualifiedDemand(StationModel stationModel, int t)
    {
        int? peakDemand = null;
        for (var i = t; i < t + PeakHorizon && i < PlanningHorizon; i++)
        {
            if (stationModel.DemandForecast[i] >= (i - t + 1) * stationModel.TOR)
            {
                peakDemand = stationModel.DemandForecast[i];
                break;
            }
        }

        if (peakDemand.HasValue)
        {
            stationModel.FutureStates[t].QualifiedDemand = stationModel.FutureStates[t].Demand + peakDemand.Value;
        }
        else
        {
            stationModel.FutureStates[t].QualifiedDemand = stationModel.FutureStates[t].Demand;
        }
    }

    public void SetInitialBufferAndOrdersAmount()
    {
        foreach (var station in Stations.OrderByDescending(s => s.Index))
        {
            var state = station.FutureStates.FirstOrDefault(s => s.Instant == 0);
            if (state == null)
            {
                station.FutureStates.Add(new TimeIndexedStationState
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

                int incomingSupply = GetIncomingSupply(station, t - 1);

                // Buffer
                station.FutureStates[t].Buffer = Math.Max(
                    station.FutureStates[t - 1].Buffer.Value - station.FutureStates[t - 1].Demand.Value +
                    incomingSupply,
                    0);

                // InOrderInventory
                station.FutureStates[t].OnOrderInventory = station.FutureStates[t - 1].OnOrderInventory
                    - incomingSupply + station.FutureStates[t - 1].OrderAmount;

                if (station.TOY >= station.FutureStates[t].NetFlow)
                {
                    station.FutureStates[t].Replenishment = 1;
                }

                station.FutureStates[t].OrderAmount = (station.FutureStates[t].Replenishment ?? 0 )*
                    (int)Math.Ceiling(station.TOG.Value - station.FutureStates[t].NetFlow.Value);

            }
        }
    }

    public int GetIncomingSupply(StationModel stationModel, int t)
    {
        if (stationModel.IsInputStation)
        {
            return t < 0
                ? stationModel.PastStates.FirstOrDefault(s => s.Instant == (int)Math.Ceiling(-t + stationModel.LeadTime.Value))
                    ?.OrderAmount ?? 0
                : stationModel.FutureStates[t].Demand!.Value;
        }

        var sourceStation = Stations.Single(s => StationPrecedences[s.Index][stationModel.Index] != 0);
        if (sourceStation is { HasBuffer: false, IsInputStation: false })
        {
            bool hasBufferOrInputStation = false;

            while (!hasBufferOrInputStation)
            {
                StationModel? preStation =
                    Stations.SingleOrDefault(s => StationPrecedences[s.Index][sourceStation.Index] != 0);

                sourceStation = preStation ?? throw new InvalidOperationException(
                    "No input station found for station " + sourceStation.Index +
                    $" During station {stationModel.Index} incoming supply calculation at t = {t}");

                hasBufferOrInputStation = sourceStation.HasBuffer || sourceStation.IsInputStation;
            }
        }

        if ((int)Math.Ceiling(t - stationModel.LeadTime!.Value) >= 0)
        {
            if (sourceStation.IsInputStation)
            {
                return stationModel.FutureStates[(int)Math.Ceiling(t - stationModel.LeadTime!.Value)].OrderAmount ??
                       throw new InvalidOperationException(
                           $"past order amount for station {stationModel.Index} at {(int)Math.Ceiling(t - stationModel.LeadTime!.Value)}" +
                           $"were not calculated yet at {t} to get incoming supply");
            }

            int stationOrderAmount = stationModel.FutureStates[(int)Math.Ceiling(t - stationModel.LeadTime!.Value)].OrderAmount ??
                                     throw new InvalidOperationException(
                                         $"past order amount for station {stationModel.Index} at {(int)Math.Ceiling(t - stationModel.LeadTime!.Value)}" +
                                         $"were not calculated yet at {t} to get incoming supply");

            int sourceStationBuffer = stationModel.FutureStates[(int)Math.Ceiling(t - stationModel.LeadTime!.Value)].Buffer ??
                                      throw new InvalidOperationException(
                                          $"source {sourceStation.Index} at supplying {stationModel.Index} should have buffer" +
                                          $"Calculating incoming supply at {t}");
            return Math.Min(sourceStationBuffer, stationOrderAmount);

        }
        else
        {
            return t < 0
                ? stationModel.PastStates.FirstOrDefault(s => s.Instant == (int)Math.Ceiling(-t + stationModel.LeadTime.Value))
                    ?.OrderAmount ?? 0
                : stationModel.PastStates.FirstOrDefault(s => s.Instant == (int)Math.Ceiling(t - stationModel.LeadTime.Value))
                    ?.OrderAmount ?? 0;
        }
    }

    public float GetAverageBuffersLevel()
        => (float)((MinBuffersAndMaxDemandsObjective)_objectiveFunction).AverageBuffersLevel();

    public float GetAverageNotSatisfiedDemand()
        => (float)((MinBuffersAndMaxDemandsObjective)_objectiveFunction).AverageSatisfiedDemandsLevel();
}