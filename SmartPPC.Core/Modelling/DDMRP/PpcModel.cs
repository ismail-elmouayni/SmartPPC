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

    public PpcModel()
    {
        Stations = new List<Station>()
        {

        };

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

        SetInitialBufferAndOrdersAmount();


        for (int t = 0; t < PlanningHorizon; t++)
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

        for (int t = 0; t < PlanningHorizon; t++)
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

        //var peakDemand = station.DemandForecast.Select((d, i) => (d, i))
        //    .Where(couple => couple.i >= t && couple.i - t <= PeakHorizon)
        //    .Select(couple => couple.d)
        //    .SingleOrDefault(d => d >= t * station.TOR);

        int? peakDemand = null;
        for (var i = t; i < t + PeakHorizon; i++)
        {
            if (station.DemandForecast[i] >= (i - t + 1) * station.TOR)
            {
                peakDemand = station.DemandForecast[i];
                break;
            }
        }

        //var peakDemand = station.DemandForecast.Select(d => I)
        //    .SingleOrDefault(s => s.Instant >= t &&
        //                         s.Instant - t <= PeakHorizon &&
        //                         s.Demand >= t * station.TOR)?.Demand;

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
            station.FutureStates.Add(new TimeIndexedStationState
            {
                Instant = 0
            });

            if (station.HasBuffer)
            {
                station.FutureStates[0].Buffer = station.PastStates[0].Buffer;
                station.FutureStates[0].OrderAmount = station.PastStates[0].OrderAmount;

                var incomingSupply = GetIncomingSupply(station, 0);

                station.FutureStates[0].OnOrderInventory =
                    -incomingSupply + station.PastStates[0].OrderAmount;
            }
        }
    }

    public void ComputeBuffersOrdersAndReplenishment(int t)
    {
        foreach (var station in Stations.OrderByDescending(s => s.Index))
        {
            station.FutureStates.Add(new TimeIndexedStationState
            {
                Instant = t + 1
            });

            // Set demand by propagation orders amounts in parent 
            // stations. For output station, the demand is known (external)
            SetStationDemandBasedOnOrders(station, t);

            if (station.HasBuffer)
            {
                SetStationQualifiedDemand(station, t);

                if (station.TOY >= station.FutureStates[t].NetFlow)
                {
                    station.FutureStates[t + 1].Replenishment = 1;
                }

                station.FutureStates[t + 1].OrderAmount = station.FutureStates[t + 1].Replenishment ?? 0 *
                                                      (int)Math.Ceiling(station.TOG.Value - station.FutureStates[t].NetFlow.Value);

                var incomingSupply = GetIncomingSupply(station, t);
                // Buffer
                station.FutureStates[t + 1].Buffer = station.FutureStates[t].Buffer - station.FutureStates[t].Demand
                                                 + incomingSupply;
                // InOrderInventory
                station.FutureStates[t + 1].OnOrderInventory = station.FutureStates[t].OnOrderInventory
                    - incomingSupply + station.FutureStates[t].OrderAmount;
            }
        }
    }

    public int GetIncomingSupply(Station station, int t)
    {
        if (station.IsInputStation)
        {
            //return (t >= station.LeadTime + 1) ?
            //    station.FutureStates[(int)Math.Ceiling(t - 1 - station.LeadTime!.Value)].OrderAmount.Value :
            //    station.PastStates.FirstOrDefault(s => s.Instant == Math.Ceiling(t - 1 - station.LeadTime.Value))?.OrderAmount ?? 0;
            return station.FutureStates[t].Demand!.Value;
        }

        var sourceStation = Stations.Single(s => StationPrecedences[s.Index][station.Index] != 0);
        if (!sourceStation.HasBuffer)
        {
            bool hasBufferOrInputStation = false;

            while (!hasBufferOrInputStation)
            {
                Station preStation = Stations.Single(s => StationPrecedences[s.Index][sourceStation.Index] != 0);
                sourceStation = preStation;
                hasBufferOrInputStation = preStation.HasBuffer || preStation.IsInputStation;
            }
        }

        if (t >= station.LeadTime + 1)
        {
            return Math.Min(station.FutureStates[(int)Math.Ceiling(t - 1 - station.LeadTime!.Value)].OrderAmount!.Value,
                 sourceStation.FutureStates[(int)Math.Ceiling(t - 1 - station.LeadTime!.Value)].Buffer ?? 0);
        }
        else
        {
            return station.PastStates.FirstOrDefault(s => s.Instant == Math.Ceiling(t - 1 - station.LeadTime.Value))?.OrderAmount ?? 0;
        }
    }
}