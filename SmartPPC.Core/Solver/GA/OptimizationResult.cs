using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using SmartPPC.Core.Model;

namespace SmartPPC.Core.Solver.GA;
public record OptimizationResult(
    IProductionControlModel Solution,
    IEnumerable<double> FitnessCurve);
