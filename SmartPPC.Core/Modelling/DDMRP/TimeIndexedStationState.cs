namespace SmartPPC.Core.Modelling.DDMRP;

public sealed class TimeIndexedStationState
{
    public int Instant { get; set; }
    public int? NetFlow => Buffer + OnOrderInventory - QualifiedDemand;
    public int? Buffer { get; set; }
    public int? Demand { get; set; }
    public int? QualifiedDemand { get; set; }
    public int? OnOrderInventory { get; set; }
    public int? OrderAmount { get; set; }
    public int? Replenishment { get; set; }
}