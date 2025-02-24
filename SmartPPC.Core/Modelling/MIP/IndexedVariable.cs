using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using SmartPPC.Core.Modelling.MIP;

namespace DDMRP_AI.Core.Modelling.MIP;

public class IndexedVariable : Variable
{
    public int Index { get; set; }
}