
namespace SmartPPC.Core.Modelling.DDMRP;

public class ModelInputs
{
    public int PlanningHorizon { get; set; }
    public int PeakHorizon { get; set; }
    public List<StationDeclaration>? StationDeclarations { get; set; }
}

public record StationDeclaration(
    int? StationIndex,
    float? ProcessingTime,
    int? InitialBuffer,
    float? DemandVariability,
    List<int>? DemandForecast,
    List<StationInput>? NextStationsInput);

public record StationInput(
    int NextStationIndex,
    int InputAmount);