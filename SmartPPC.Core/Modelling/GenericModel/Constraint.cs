using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DDMRP_AI.Core.Modelling.GenericModel;

public class Constraint : IConstraint
{
    public string Expression { get; set; }

    public bool IsVerified()
    {
        return true;
    }
}