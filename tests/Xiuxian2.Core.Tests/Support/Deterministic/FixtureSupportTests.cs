namespace Xiuxian2.Core.Tests.Support.Deterministic;

public sealed class FixtureSupportTests
{
    [Fact]
    public void BuilderCreatesReusableDeterministicServiceFixture()
    {
        var fixture = new ServiceFixtureBuilder().Build();

        Assert.NotNull(fixture.Rng);
        Assert.NotNull(fixture.Clock);
        Assert.NotNull(fixture.ConfigSource);
        Assert.NotNull(fixture.FileSystem);
        Assert.NotNull(fixture.PlatformInfo);
        Assert.NotNull(fixture.HookBackend);
        Assert.StartsWith("phase1-sample-config", Path.GetFileNameWithoutExtension(fixture.FrozenConfigPath), StringComparison.Ordinal);
    }

    [Fact]
    public void BuilderSeedsFrozenConfigIntoInMemoryConfigSource()
    {
        var fixture = new ServiceFixtureBuilder().Build();

        Assert.True(fixture.ConfigSource.TryReadAllText(fixture.FrozenConfigPath, out var text));
        Assert.Contains("\"levels\"", text, StringComparison.Ordinal);
        Assert.DoesNotContain("docs/design", fixture.FrozenConfigPath, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void BuilderLoadsFrozenConfigFixtureFromTestProject()
    {
        var fixture = new ServiceFixtureBuilder().Build();

        var document = fixture.LoadFrozenConfigText();

        Assert.Contains("starter_plain", document, StringComparison.Ordinal);
        Assert.Equal(document, fixture.LoadFrozenConfigText());
    }
}
