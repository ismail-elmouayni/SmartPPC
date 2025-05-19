using System.Runtime.CompilerServices;

namespace SmartPPC.Core.Model.DDMRP;

public class MinBuffersAndMaxDemandsObjective : IObjective
{
    public const int BigNumber = 1000000000;
    public float BuffersOptimizationWeight { get; set; } = 1f;
    public float DemandsOptimizationWeight { get; set; } = 100f;
    public float BufferActivationCost { get; set; } = 10f;

    private readonly int[][] _stationsPrecedencies;


    private readonly IEnumerable<StationModel> _stations;

    public MinBuffersAndMaxDemandsObjective(IEnumerable<StationModel> stations, int[][] stationsPrecedencies)
    {
        _stations = stations;
        _stationsPrecedencies = stationsPrecedencies;
    }

    public float? Evaluate()
    {
        return (float)(BuffersOptimizationWeight * AverageBuffersLevel() +
                       DemandsOptimizationWeight * AverageSatisfiedDemandsLevel() + 
                       BufferActivationCost * GetNumberOfActivatedBuffer());
    }

    public double AverageBuffersLevel()
    {
        var criteriaValue = _stations.Sum(s => s.HasBuffer ? s.FutureStates.Average(state => state.Buffer) : 0);

        return criteriaValue!.Value;
    }

    public double AverageSatisfiedDemandsLevel()
    {
        var outputStations = _stations.Where(s => s.IsInputStation)
            .ToList();

        var keyProductivityStations = new List<StationModel>();

        foreach (var outputStation in outputStations)
        {
            var sourceStation = outputStation;
            
            if (sourceStation.HasBuffer)
            {
                keyProductivityStations.Add(sourceStation);
                continue;
            }

            while (!sourceStation.HasBuffer)
            {
                var nextSourceStation =
                    _stations.FirstOrDefault(s => _stationsPrecedencies[s.Index][sourceStation.Index] == 1);

                if (nextSourceStation == null) return BigNumber;

                if (nextSourceStation.HasBuffer)
                {
                    keyProductivityStations.Add(nextSourceStation);
                }
               
                sourceStation = nextSourceStation;
            }
        }

        return keyProductivityStations.Sum(s => s.FutureStates.Average(state => Math.Max(state.Demand.Value - state.Buffer.Value, 0)));
        //= outputStations
        //       .Select(s =>
        //       {
        //           var index = _stations.Sum(preS =>_stationsPrecedencies[preS][s.Index]])
        //       });

        //   if (bufferedStations.Count == 0)
        //       return BigNumber;
        //   else
        //       return bufferedStations.Average(s => s.FutureStates.Average(state => Math.Max(state.Demand.Value - state.Buffer.Value, 0)));


        //var outputStations = _stations.Where(s => s.IsOutputStation).ToList();

        //var endpointsStations = outputStations.Select( os => 
        //{
        //    if(os.HasBuffer)
        //        return os;
        //    else
        //    {
        //      _stations.Sum
        //    }


        //});

        //if(outputStations.Count == 0)
        //{
        //    throw new InvalidOperationException("No output stations found in the list of stations");
        //}

        //var invalidOutputStations = outputStations.Where(s => s.FutureStates.Any(state => state.Demand is null)
        //    || s.FutureStates.Any(state => state.Buffer == null)).ToList();

        //if (invalidOutputStations.Any())
        //{
        //    throw new InvalidOperationException($"at least one output station have either buffer or demand is null : " +
        //                                        $" concerned station {string.Join(",", invalidOutputStations.Select(s => s.Index))}");
        //}

        //var criteriaValue = outputStations.Sum(s =>
        //    s.FutureStates.Select(state => Math.Max(state.Demand.Value - state.Buffer.Value, 0)).Average());

        //return criteriaValue;
    }

    public int GetNumberOfActivatedBuffer()
        => _stations.Where(s => s.HasBuffer).Count();
}