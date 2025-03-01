
namespace DDMRP_AI.Core.Modelling.DDMRP;

public class DDMRP_ConfigOptions
{
    public int PlanningHorizon { get; set; }
    public int PeakHorizon { get; set; }
    public float OutputStationDemandVariability { get; set; }
    public List<StationDeclaration>? StationDeclarations { get; set; }
}

public record StationDeclaration(
    int StationIndex,
    int ProcessingTime,
    int? InitialBuffer,
    List<StationInput>? NextStationsInput);

public record StationInput(
    int NextStationIndex,
    int InputAmount);