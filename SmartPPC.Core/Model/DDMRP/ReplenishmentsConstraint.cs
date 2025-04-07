namespace SmartPPC.Core.Model.DDMRP;

public class ReplenishmentsConstraint : IConstraint
{
    private readonly IEnumerable<StationModel> _stations;

    public float BigNumber = 1000f;

    public ReplenishmentsConstraint(IEnumerable<StationModel> stations)
    {
        _stations = stations;
    }

    public bool IsVerified()
    {
        if (_stations.Any(s => s.TOY is null))
        {
            throw new InvalidOperationException(
                "TOY is not defined for all stations. Math model must be resolved before verifying constraints");
        }

        return _stations.All(s => s.FutureStates
            .All(t => BigNumber * (t.Replenishment - 1) <= s.HasBufferInt * (s.TOY - t.NetFlow) &&
                      s.HasBufferInt * (s.TOY - t.NetFlow) <= BigNumber * t.Replenishment));
    }
}