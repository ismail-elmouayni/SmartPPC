using SmartPPC.Core.Modelling;
using FluentResults;
using SmartPPC.Core.Solver.GA;

namespace SmartPPC.Core.Solver;

public interface IPPCSolver
{
    const string ConfigFilePath = "DDRMP_ModelInputs.json";

    Result<IMathModel?> GetMathModel(string configFilePath = ConfigFilePath);
    Result<OptimizationResult> Resolve(string configFilePath = ConfigFilePath);
}