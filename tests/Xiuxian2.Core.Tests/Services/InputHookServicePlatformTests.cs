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
        var service = new InputHookService(fixture.PlatformInfo, fixture.HookBackend)
        {
            AutoStart = false,
            EnableInAppFallback = true,
            ForceGlobalCapture = false,
        };

        service.StartHook();

        Assert.False(service.IsHookActive);
        Assert.True(service.IsUsingInAppFallback);
        Assert.Equal("Linux", service.ActivePlatformName);
        Assert.Equal(0, fixture.HookBackend.StartCallCount);
    }

    [Fact]
    public void BackendFailuresRemainDeterministicAndObservable()
    {
        var fixture = new ServiceFixtureBuilder().Build();
        fixture.PlatformInfo.SetPlatform("Windows");
        fixture.HookBackend.QueueStartFailure("Keyboard hook failed: 5");
        var service = new InputHookService(fixture.PlatformInfo, fixture.HookBackend)
        {
            AutoStart = false,
            EnableInAppFallback = true,
            ForceGlobalCapture = false,
        };

        service.StartHook();

        Assert.False(service.IsHookActive);
        Assert.True(service.IsUsingInAppFallback);
        Assert.Equal("Keyboard hook failed: 5", service.LastHookErrorMessage);
        Assert.Equal(1, fixture.HookBackend.StartCallCount);
    }

    [Fact]
    public void SuccessfulBackendStartEntersActiveModeThroughInjectedSeams()
    {
        var fixture = new ServiceFixtureBuilder().Build();
        fixture.PlatformInfo.SetPlatform("Windows");
        fixture.HookBackend.QueueStartSuccess();
        var service = new InputHookService(fixture.PlatformInfo, fixture.HookBackend)
        {
            AutoStart = false,
            EnableInAppFallback = true,
            ForceGlobalCapture = true,
        };

        service.StartHook();

        Assert.True(service.IsHookActive);
        Assert.False(service.IsUsingInAppFallback);
        Assert.Null(service.LastHookErrorMessage);
        Assert.Equal(1, fixture.HookBackend.StartCallCount);
    }
}
