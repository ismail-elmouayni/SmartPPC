using System.ComponentModel;

namespace DDMRP_AI.Core.Modelling.DDMRP;

public class Station
{
    [Description("decision-variable")]
    public bool HasBuffer { get; set; }
    public bool IsOutputStation { get; set; }
    public bool IsInputStation { get; set; }
    public int HasBufferInt => HasBuffer ? 1 : 0;
    public int Index { get; set; }
    public int ProcessingTime { get; set; }
    public int? LeadTime { get; set; }

    public float? AverageDemand
    {
        get
        {
            if (StateTimeLine.Any(state => state.Demand is null))
                return null;

            return (float?)StateTimeLine.Average(state => state.Demand);
        }
    }
    public float? LeadTimeFactor { get; set; }
    public float? DemandVariability { get; set; }

    public int? TOR
    {
        get
        {
            if (LeadTimeFactor == null || DemandVariability == null || AverageDemand == null)
            {
                return null;
            }

            return (int)(LeadTime * AverageDemand * (LeadTimeFactor + LeadTimeFactor * DemandVariability));
        }
    }

    public int? TOG
    {
        get
        {
            if (LeadTimeFactor == null || DemandVariability == null || AverageDemand == null)
            {
                return null;
            }

            return TOY + (int)(LeadTime * AverageDemand * LeadTimeFactor);
        }
    }

    public int? TOY
    {
        get
        {
            if (LeadTimeFactor == null || DemandVariability == null || AverageDemand == null)
            {
                return null;
            }

            return TOR + (int)(LeadTime * AverageDemand);
        }
    }

    //public int TOR
    //{
    //    get
    //    {
    //        if (LeadTimeFactor == null || DemandVariability == null || AverageDemand == null)
    //        {
    //            throw new InvalidOperationException(
    //                "LeadTimeFactor, DemandVariability and AverageDemand must be set before calculating TOR");
    //        }

    //        return (int)(LeadTime * AverageDemand * (LeadTimeFactor + LeadTimeFactor * DemandVariability));
    //    }
    //}

    //public int TOG
    //{
    //    get
    //    {
    //        if (LeadTimeFactor == null || DemandVariability == null || AverageDemand == null)
    //        {
    //            throw new InvalidOperationException(
    //                "LeadTimeFactor, DemandVariability and AverageDemand must be set before calculating TOG");
    //        }

    //        return TOY + (int)(LeadTime * AverageDemand * LeadTimeFactor);
    //    }
    //}

    //public int TOY
    //{
    //    get
    //    {
    //        if (LeadTimeFactor == null || DemandVariability == null || AverageDemand == null)
    //        {
    //            throw new InvalidOperationException(
    //                "LeadTimeFactor, DemandVariability and AverageDemand must be set before calculating TOY");
    //        }

    //        return TOR + (int)(LeadTime * AverageDemand);
    //    }
    //}

    public List<TimeIndexedStationState> StateTimeLine { get; set; }
        = new();
}