
namespace SmartPPC.Core.Modelling.DDMRP;

public class ModelInputs
{
    public int PlanningHorizon { get; set; }
    public int PeakHorizon { get; set; }
    public int PastHorizon { get; set;  }
    public List<StationDeclaration>? StationDeclarations { get; set; }
}

public record StationDeclaration(
    int? StationIndex,
    float? ProcessingTime,
    int? InitialBuffer,
    int[] PastBuffer,
    int[] PastOrderAmount,
    float? DemandVariability,
    List<int>? DemandForecast,
    List<StationInput>? NextStationsInput);

public record StationInput(
    int NextStationIndex,
    int InputAmount);