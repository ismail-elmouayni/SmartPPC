using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SmartPPC.Core.Modelling.MIP;

public class Objective : IObjective
{
    public string Expression { get; set; }
    public bool Maximize { get; set; }


    public float Evalute()
    {
        return 0;
    }
}

public interface IObjective
{
    double Evalute();
}