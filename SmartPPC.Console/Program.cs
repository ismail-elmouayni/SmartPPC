using SmartPPC.Core.Model.DDMRP;
using SmartPPC.Core.Solver;
using SmartPPC.Core.Solver.GA;

namespace SmartPPC.Console;

public class Program
{
    public static void Main(string[] args)
    {
        IProductionControlSolver solver = new GnSolver();

        var initResult = solver.Initialize();
        if (initResult.IsFailed)
        {
            System.Console.Write($"Errors occured during initialization process : {string.Join(",", initResult.Errors)}");
            System.Console.Read();

            return;
        }

        var optResult = solver.Resolve();

        if (optResult.IsFailed)
        {
            System.Console.Write($"Errors occured during optimization process : {string.Join(",", optResult.Errors)}");
            System.Console.Read();

            return;
        }

        var buffersActivation = optResult.Value.Solution.ToGenes()
            .Select(g => (int)g.Value);
        var solution = optResult.Value.Solution;

        var printText = $"Optimal Genes {string.Join(",",buffersActivation)}\n" +
                        $"Fitness curve : {string.Join(",", optResult.Value.FitnessCurve)}" +
                        $"Average buffers level : {string.Join(",", solution.GetAverageBuffersLevel())}" +
                        $"Average not satisfied demand : {string.Join(",", solution.GetAverageNotSatisfiedDemand())}";

        File.WriteAllText("result", printText);

        ResultsSaver.SaveResultsToCsv("./Results",(ProductionControlModel)solution);
        System.Console.Write(printText);
        System.Console.Read();
    }
}