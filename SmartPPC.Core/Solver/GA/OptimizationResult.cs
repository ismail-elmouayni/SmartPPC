using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SmartPPC.Core.Solver.GA;
public record OptimizationResult(IEnumerable<int> BestGenes, IEnumerable<double> FitnessCurve);
