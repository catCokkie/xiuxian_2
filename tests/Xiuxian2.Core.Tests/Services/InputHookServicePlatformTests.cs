using Xiuxian.Scripts.Services;
using Xiuxian2.Core.Tests.Builders;

namespace Xiuxian2.Core.Tests.Services;

public sealed class InputHookServicePlatformTests
{
    [Fact]
    public void UnsupportedPlatformStaysInFallbackModeWithoutStartingHookBackend()
    {
        var fixture = new ServiceFixtureBuilder().Build();
        fixture.PlatformInfo.SetPlatform("Linux");
        fixture.HookBackend.QueueStartSuccess();
        var outcome = InputHookService.EvaluateHookStartup(
            fixture.PlatformInfo,
            fixture.HookBackend,
            static (_, _, _) => 0,
            static (_, _, _) => 0);

        Assert.False(outcome.IsHookActive);
        Assert.True(outcome.IsUsingInAppFallback);
        Assert.Equal("Linux", outcome.PlatformName);
        Assert.Equal(0, fixture.HookBackend.StartCallCount);
    }

    [Fact]
    public void BackendFailuresRemainDeterministicAndObservable()
    {
        var fixture = new ServiceFixtureBuilder().Build();
        fixture.PlatformInfo.SetPlatform("Windows");
        fixture.HookBackend.QueueStartFailure("Keyboard hook failed: 5");
        var outcome = InputHookService.EvaluateHookStartup(
            fixture.PlatformInfo,
            fixture.HookBackend,
            static (_, _, _) => 0,
            static (_, _, _) => 0);

        Assert.False(outcome.IsHookActive);
        Assert.True(outcome.IsUsingInAppFallback);
        Assert.Equal("Keyboard hook failed: 5", outcome.ErrorMessage);
        Assert.Equal(1, fixture.HookBackend.StartCallCount);
    }

    [Fact]
    public void SuccessfulBackendStartEntersActiveModeThroughInjectedSeams()
    {
        var fixture = new ServiceFixtureBuilder().Build();
        fixture.PlatformInfo.SetPlatform("Windows");
        fixture.HookBackend.QueueStartSuccess();
        var outcome = InputHookService.EvaluateHookStartup(
            fixture.PlatformInfo,
            fixture.HookBackend,
            static (_, _, _) => 0,
            static (_, _, _) => 0);

        Assert.True(outcome.IsHookActive);
        Assert.False(outcome.IsUsingInAppFallback);
        Assert.Null(outcome.ErrorMessage);
        Assert.Equal(1, fixture.HookBackend.StartCallCount);
    }
}
