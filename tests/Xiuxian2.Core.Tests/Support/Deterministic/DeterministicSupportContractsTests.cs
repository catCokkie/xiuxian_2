using Xiuxian.Scripts.Contracts;

namespace Xiuxian2.Core.Tests.Support.Deterministic;

public sealed class DeterministicSupportContractsTests
{
    [Fact]
    public void FakeRngReturnsScriptedValuesInOrder()
    {
        var rng = new FakeRng()
            .EnqueueInt(4)
            .EnqueueInt(9)
            .EnqueueFloat(0.25f);

        rng.Randomize();

        Assert.Equal(4, rng.NextInt(1, 10));
        Assert.Equal(9, rng.NextInt(1, 10));
        Assert.Equal(0.25f, rng.NextSingle());
        Assert.Equal(1, rng.RandomizeCallCount);
    }

    [Fact]
    public void FakeClockCanBeSetAndAdvancedDeterministically()
    {
        var clock = new FakeClock(1_700_000_000L);

        clock.AdvanceSeconds(45);

        Assert.Equal(1_700_000_045L, clock.GetUnixTimeSeconds());

        clock.SetUnixTimeSeconds(1_800_000_000L);

        Assert.Equal(1_800_000_000L, clock.GetUnixTimeSeconds());
    }

    [Fact]
    public void InMemoryAdaptersCanBeScriptedWithoutLiveBoundaries()
    {
        var configSource = new InMemoryConfigSource()
            .AddFile("res://config/sample.json", "{\"levels\":[]}");
        var fileSystem = new FakeFileSystem("/virtual-root")
            .AddFile("/virtual-root/save.dat", new byte[] { 1, 2, 3 });
        var platformInfo = new FakePlatformInfo("Linux");
        var hookBackend = new FakeHookBackend()
            .QueueStartSuccess()
            .SetKeyboardNextResult((nint)11)
            .SetMouseNextResult((nint)22);

        Assert.True(configSource.TryReadAllText("res://config/sample.json", out var text));
        Assert.Equal("{\"levels\":[]}", text);

        Assert.True(fileSystem.FileExists("/virtual-root/save.dat"));
        Assert.Equal(new byte[] { 1, 2, 3 }, fileSystem.ReadAllBytes("/virtual-root/save.dat"));
        fileSystem.WriteAllBytes("/virtual-root/new-save.dat", new byte[] { 4, 5, 6 });
        Assert.Equal("/virtual-root/user://save.dat", fileSystem.GlobalizePath("user://save.dat"));
        Assert.Equal(new byte[] { 4, 5, 6 }, fileSystem.ReadAllBytes("/virtual-root/new-save.dat"));

        Assert.False(platformInfo.IsWindows());
        Assert.Equal("Linux", platformInfo.PlatformName);

        Assert.True(hookBackend.TryStart(static (_, _, _) => 0, static (_, _, _) => 0, out var error));
        Assert.Equal(string.Empty, error);
        Assert.True(hookBackend.IsActive);
        Assert.Equal((nint)11, hookBackend.CallNextKeyboardHook(0, 0, 0));
        Assert.Equal((nint)22, hookBackend.CallNextMouseHook(0, 0, 0));

        hookBackend.Stop();

        Assert.False(hookBackend.IsActive);
        Assert.Equal(1, hookBackend.StopCallCount);
    }
}
