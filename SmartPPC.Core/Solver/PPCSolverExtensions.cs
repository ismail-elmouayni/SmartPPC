using Microsoft.Extensions.DependencyInjection;

namespace SmartPPC.Core.Solver;

public static class PpcSolverExtensions
{
    /// <summary>
    /// Adds <see cref="IPPCSolver"/> to service collection
    /// </summary>
    /// <param name="services"></param>
    /// <returns></returns>
    public static IServiceCollection AddPpcSolver(this IServiceCollection services)
        => services.AddScoped<IPPCSolver, GA.Solver>(); 
}