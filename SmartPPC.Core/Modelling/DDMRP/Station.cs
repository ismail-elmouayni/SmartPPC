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
    public float ProcessingTime { get; set; }
    public float? LeadTime { get; set; }

    public float? AverageDemand { get; set; }
 
    public float? LeadTimeFactor { get; set; }
    public float? DemandVariability { get; set; }

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