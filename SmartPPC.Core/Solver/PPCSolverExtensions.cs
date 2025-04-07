using Microsoft.Extensions.DependencyInjection;

namespace SmartPPC.Core.Solver;

public static class PpcSolverExtensions
{
    /// <summary>
    /// Adds <see cref="IProductionControlSolver"/> to service collection
    /// </summary>
    /// <param name="services"></param>
    /// <returns></returns>
    public static IServiceCollection AddPpcSolver(this IServiceCollection services)
        => services.AddScoped<IProductionControlSolver, GA.GnSolver>(); 
}