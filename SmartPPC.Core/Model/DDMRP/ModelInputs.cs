
namespace SmartPPC.Core.Model.DDMRP;

public class ModelInputs
{
    public int PlanningHorizon { get; set; }
    public int PeakHorizon { get; set; }
    public int PastHorizon { get; set;  }

    public float PeakThreshold { get; set; }
    public List<StationDeclaration>? StationDeclarations { get; set; }

    public int NumberOfStations => StationDeclarations?.Count ?? 0;
}

public record StationDeclaration(
    int? StationIndex,
    float? ProcessingTime,
    float? LeadTime,
    int? InitialBuffer,
    int[] PastBuffer,
    int[] PastOrderAmount,
    float? DemandVariability,
    List<int>? DemandForecast,
    List<StationInput>? NextStationsInput);

public record StationInput(
    int NextStationIndex,
    int InputAmount);