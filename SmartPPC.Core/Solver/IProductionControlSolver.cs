using SmartPPC.Core.Model;
using FluentResults;
using SmartPPC.Core.Solver.GA;

namespace SmartPPC.Core.Solver;

public interface IProductionControlSolver
{
    const string ConfigFilePath = "DDRMP_ModelInputs.json";

    Result<IProductionControlModel?> GetModel(string configFilePath = ConfigFilePath);
    Result Initialize(string configFilePath = ConfigFilePath);
    Result<OptimizationResult> Resolve();
}