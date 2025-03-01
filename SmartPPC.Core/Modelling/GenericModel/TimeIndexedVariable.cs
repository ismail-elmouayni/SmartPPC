using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DDMRP_AI.Core.Modelling.GenericModel;

public class TimeIndexedVariable : IndexedVariable
{
    public int TimeIndex { get; set; }
}