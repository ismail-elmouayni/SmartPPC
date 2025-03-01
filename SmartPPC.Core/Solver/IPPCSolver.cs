using DDMRP_AI.Core.Modelling;
using DDMRP_AI.Core.Modelling.GenericModel;
using FluentResults;

namespace SmartPPC.Core.Solver;

public interface IPPCSolver
{
    const string ConfigFilePath = "DDRMP_ModelConfig.json";

    Result<IMathModel?> GetMathModel(string configFilePath = ConfigFilePath);
    Result<IMathModel> Resolve(string configFilePath = ConfigFilePath);
}