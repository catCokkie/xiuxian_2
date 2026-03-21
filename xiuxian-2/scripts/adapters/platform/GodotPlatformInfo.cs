using Godot;
using System;
using Xiuxian.Scripts.Contracts;

namespace Xiuxian.Scripts.Adapters.Platform;

public sealed class GodotPlatformInfo : IPlatformInfo
{
    public string PlatformName => OS.GetName();

    public bool IsWindows()
    {
        return string.Equals(PlatformName, "Windows", StringComparison.OrdinalIgnoreCase);
    }
}
