using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SmartPPC.Core.Modelling.DDMRP;

public sealed class TimeIndexedPastState
{
    public int Instant { get; set; }
    public int Buffer { get; set; }
    public int OrderAmount { get; set; }
}