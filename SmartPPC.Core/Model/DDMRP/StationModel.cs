using System.ComponentModel;

namespace SmartPPC.Core.Model.DDMRP;

public class StationModel
{
    [Description("decision-variable")]
    public bool HasBuffer { get; set; }
    public bool IsOutputStation { get; set; }
    public bool IsInputStation { get; set; }
    public int HasBufferInt => HasBuffer ? 1 : 0;
    public int Index { get; set; }
    public float ProcessingTime { get; set; }
    public float? LeadTime { get; set; }
    public float? AverageDemand { get; set; }
    public float? LeadTimeFactor { get; set; }
    public int[]? DemandForecast { get; set; }
    public float? DemandVariability { get; set; }
    public List<TimeIndexedStationState> FutureStates { get; set; }
        = new();
    public List<TimeIndexedPastState> PastStates { get; set; }
        = new();

    public float? TOR
    {
        get
        {
            if (LeadTimeFactor == null || DemandVariability == null || AverageDemand == null)
            {
                return null;
            }

            return LeadTime * AverageDemand * (LeadTimeFactor + LeadTimeFactor * DemandVariability);
        }
    }

    public float? TOG
    {
        get
        {
            if (LeadTimeFactor == null || DemandVariability == null || AverageDemand == null)
            {
                return null;
            }

            return TOY + LeadTime * AverageDemand * LeadTimeFactor;
        }
    }

    public float? TOY
    {
        get
        {
            if (LeadTimeFactor == null || DemandVariability == null || AverageDemand == null)
            {
                return null;
            }

            return TOR + LeadTime * AverageDemand;
        }
    }

    public float GetAverageBufferLevel()
    {
        if (!HasBuffer || FutureStates == null || !FutureStates.Any())
            return 0f;

        var bufferValues = FutureStates
            .Where(state => state.Buffer.HasValue)
            .Select(state => state.Buffer.Value);

        return bufferValues.Any() ? (float)bufferValues.Average() : 0f;
    }

}