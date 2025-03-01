using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DDMRP_AI.Core.Modelling.GenericModel;

public class Objective : IObjective
{
    public string Expression { get; set; }
    public bool Maximize { get; set; }


    public double Evaluate()
    {
        return 0;
    }
}