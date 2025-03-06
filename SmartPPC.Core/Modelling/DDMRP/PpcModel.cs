using GeneticSharp;

namespace SmartPPC.Core.Modelling.DDMRP;

public class PpcModel : IMathModel
{
    private readonly IObjective _objectiveFunction;

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
        var outputStationsIndices = Stations.Where(s => s.IsOutputStation)
            .Select(s => s.Index);

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
            if (station.HasBuffer)
            {
                station.LeadTime = 0;
            }
            else
            {
                station.LeadTime = station.ProcessingTime;
            }

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
                .Select(t => Stations
                    .Where(s => s.Index > station.Index)
                    .Select(s => (s.Index, s.StateTimeLine.Single(st => st.Instant == t.Instant).Demand))
                    // (Index, Demand) a un instant t
                    .Sum(couple => StationInputPrecedences[station.Index][couple.Index] * couple.Demand))
                .Average();
        }

        station.AverageDemand = averageDemand;
    }


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
                if (station.TOY >= stationState.NetFlow)
                {
                    if (!station.HasBuffer && station.IsOutputStation)
                    {
                        stationState.Replenishment = 1;
                    }
                    else if (station.HasBuffer)
                    {
                        stationState.Replenishment = 1;
                    }
                }

                var expectedOrderToArrive = (t >= station.LeadTime + 1) ?
                    station.StateTimeLine.ElementAt((int)Math.Ceiling(stationState.Instant - 1 - station.LeadTime!.Value)).OrderAmount : 0;

                if (t > 0)
                {
                    stationState.Buffer = station.StateTimeLine.ElementAt(stationState.Instant - 1).Buffer + expectedOrderToArrive
                                   - station.StateTimeLine.ElementAt(stationState.Instant - 1).Demand!.Value;
                }

                var parentAvailableBuffer = Stations.Where(s => s.Index < station.Index)
                    .Select(s => (s.Index, s.StateTimeLine.Single(st => st.Instant == t).Buffer))
                    .Sum(couple => StationInputPrecedences[station.Index][couple.Index] * couple.Buffer);

                stationState.OrderAmount = stationState.Replenishment * Math.Min((int)Math.Ceiling(station.TOG.Value - stationState.NetFlow),
                    parentAvailableBuffer);
            }
        }

    }
}