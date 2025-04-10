﻿namespace SmartPPC.Core.Model.DDMRP;

public class MinBuffersAndMaxDemandsObjective : IObjective
{
    public float BuffersOptimizationWeight { get; set; } = 0.5f;
    public float DemandsOptimizationWeight { get; set; } = 10f;
    public float BufferActivationCost { get; set; } = 0.1f;


    private readonly IEnumerable<StationModel> _stations;

    public MinBuffersAndMaxDemandsObjective(IEnumerable<StationModel> stations)
    {
        _stations = stations;
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
        var outputStations = _stations.Where(s => s.IsOutputStation).ToList();

        if(outputStations.Count == 0)
        {
            throw new InvalidOperationException("No output stations found in the list of stations");
        }

        var invalidOutputStations = outputStations.Where(s => s.FutureStates.Any(state => state.Demand is null)
            || s.FutureStates.Any(state => state.Buffer == null)).ToList();

        if (invalidOutputStations.Any())
        {
            throw new InvalidOperationException($"at least one output station have either buffer or demand is null : " +
                                                $" concerned station {string.Join(",", invalidOutputStations.Select(s => s.Index))}");
        }

        var criteriaValue = outputStations.Sum(s =>
            s.FutureStates.Select(state => Math.Max(state.Demand.Value - state.Buffer.Value, 0)).Average());

        return criteriaValue;
    }

    public int GetNumberOfActivatedBuffer()
        => _stations.Where(s => s.HasBuffer).Count();
}