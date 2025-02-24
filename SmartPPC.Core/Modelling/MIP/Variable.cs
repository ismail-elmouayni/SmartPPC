using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SmartPPC.Core.Modelling.MIP;

public class Variable
{
    public string Name { get; set; }
    public string Type { get; set; }

    [JsonProperty("lower_bound")]
    public double LowerBound { get; set; }

    [JsonProperty("upper_bound")]
    public double UpperBound { get; set; }
}