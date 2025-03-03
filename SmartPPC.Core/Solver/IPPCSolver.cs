using SmartPPC.Core.Modelling;
using FluentResults;

namespace SmartPPC.Core.Solver;

public interface IPPCSolver
{
    const string ConfigFilePath = "DDRMP_ModelInputs.json";

    Result<IMathModel?> GetMathModel(string configFilePath = ConfigFilePath);
    Result<IMathModel> Resolve(string configFilePath = ConfigFilePath);
}