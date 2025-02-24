using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SmartPPC.Core.Modelling.MIP;

public class Constraint : IConstraint
{
    public string Expression { get; set; }

    public bool IsVerified()
    {
        return true;
    }
}

public interface IConstraint
{
    bool IsVerified();
}