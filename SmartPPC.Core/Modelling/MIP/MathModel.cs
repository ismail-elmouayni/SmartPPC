using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SmartPPC.Core.Modelling.MIP;

public class MathModel
{
    public List<Variable> Variables { get; set; }
    public List<Constraint> Constraints { get; set; }
    public Objective Objective { get; set; }
}