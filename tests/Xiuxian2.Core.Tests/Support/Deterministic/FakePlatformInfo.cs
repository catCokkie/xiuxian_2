using Xiuxian.Scripts.Contracts;

namespace Xiuxian2.Core.Tests.Support.Deterministic;

public sealed class FakePlatformInfo : IPlatformInfo
{
    public FakePlatformInfo(string platformName = "Windows")
    {
        PlatformName = platformName;
    }

    public string PlatformName { get; private set; }

    public bool IsWindows()
    {
        return string.Equals(PlatformName, "Windows", StringComparison.OrdinalIgnoreCase);
    }

    public FakePlatformInfo SetPlatform(string platformName)
    {
        PlatformName = platformName;
        return this;
    }
}
