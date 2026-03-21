using Xiuxian.Scripts.Contracts;

namespace Xiuxian2.Core.Tests.Support.Deterministic;

public sealed class FakeClock : IClock
{
    private long _unixTimeSeconds;

    public FakeClock(long unixTimeSeconds = 1_700_000_000L)
    {
        _unixTimeSeconds = unixTimeSeconds;
    }

    public long GetUnixTimeSeconds()
    {
        return _unixTimeSeconds;
    }

    public void SetUnixTimeSeconds(long unixTimeSeconds)
    {
        _unixTimeSeconds = unixTimeSeconds;
    }

    public void AdvanceSeconds(long seconds)
    {
        _unixTimeSeconds += seconds;
    }
}
