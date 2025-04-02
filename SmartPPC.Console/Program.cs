using SmartPPC.Core.Solver;
using SmartPPC.Core.Solver.GA;

namespace SmartPPC.Console;

public class Program
{
    public static void Main(string[] args)
    {
        IPPCSolver solver = new Solver();

        var optResult = solver.Resolve();

        if (optResult.IsFailed)
        {
            System.Console.Write($"Errors occured during optimization process : {string.Join(",", optResult.Errors)}");
            System.Console.Read();

            return;
        }

        var printText = $"Optimal Genes {string.Join(",",optResult.Value.BestGenes)}\n" +
                        $"Fitness curve : {string.Join(",", optResult.Value.FitnessCurve)}";

        File.WriteAllText("result", printText);
        System.Console.Write(printText);
        System.Console.Read();
    }
}