using FluentResults;
using SmartPPC.Core.Modelling.MIP;

namespace SmartPPC.Core.Solver;

public interface IPPCSolver
{
    const string ConfigFilePath = "MathModelConfig.json";

    MathModel? GetMathModel(string configFilePath = ConfigFilePath);
    Result<Dictionary<string, double>> Resolve(string configFilePath = ConfigFilePath);
}