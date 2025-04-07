using System.Globalization;

namespace SmartPPC.Core.Model.DDMRP;

public class ResultsSaver
{
    public static void SaveResultsToCsv(
        string directoryPath,
        ProductionControlModel solution,
        IEnumerable<double> fitnessCurve)

    {
        if (!Directory.Exists(directoryPath))
        {
            Directory.CreateDirectory(directoryPath);
        }

        var filePath = Path.Combine(directoryPath, "optimizationResults.csv");
        using var writer = new StreamWriter(filePath);
        using var csv = new CsvHelper.CsvWriter(writer, CultureInfo.CurrentCulture);

        var bufferedStation = solution.Stations.Where(s => s.HasBuffer).ToList();
        foreach (var station in bufferedStation)
        {
            csv.WriteComment($"Station-{station.Index}");
            csv.NextRecord();
            csv.WriteRecords(station.FutureStates);
            csv.NextRecord();
        }

        var fitnessFilePath = Path.Combine(directoryPath, "fitness.csv");
        using var fitnessWriter = new StreamWriter(fitnessFilePath);
        using var fitnessCsv = new CsvHelper.CsvWriter(fitnessWriter, CultureInfo.CurrentCulture);

        fitnessCsv.WriteRecords(fitnessCurve);
    }
}