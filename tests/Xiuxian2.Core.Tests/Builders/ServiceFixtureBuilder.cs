using Xiuxian2.Core.Tests.Support.Deterministic;

namespace Xiuxian2.Core.Tests.Builders;

public sealed class ServiceFixtureBuilder
{
    private string? _repositoryRoot;
    private string _fixtureRelativePath = Path.Combine("tests", "Xiuxian2.Core.Tests", "Fixtures", "config", "phase1-sample-config.json");

    public ServiceFixtureBuilder WithRepositoryRoot(string repositoryRoot)
    {
        _repositoryRoot = repositoryRoot;
        return this;
    }

    public ServiceFixtureBuilder WithFrozenConfigRelativePath(string relativePath)
    {
        _fixtureRelativePath = relativePath;
        return this;
    }

    public ServiceFixture Build()
    {
        var repositoryRoot = _repositoryRoot ?? ResolveRepositoryRoot();
        var frozenConfigPath = Path.Combine(repositoryRoot, _fixtureRelativePath);
        var frozenConfigText = File.ReadAllText(frozenConfigPath);
        var configSource = new InMemoryConfigSource().AddFile(frozenConfigPath, frozenConfigText);

        return new ServiceFixture(
            repositoryRoot,
            frozenConfigPath,
            new FakeRng(),
            new FakeClock(),
            configSource,
            new FakeFileSystem(),
            new FakePlatformInfo(),
            new FakeHookBackend());
    }

    private static string ResolveRepositoryRoot()
    {
        var current = new DirectoryInfo(AppContext.BaseDirectory);

        while (current is not null)
        {
            if (File.Exists(Path.Combine(current.FullName, "Directory.Packages.props"))
                && Directory.Exists(Path.Combine(current.FullName, "xiuxian-2")))
            {
                return current.FullName;
            }

            current = current.Parent;
        }

        throw new DirectoryNotFoundException("Could not locate the repository root from the test output directory.");
    }
}

public sealed class ServiceFixture
{
    public ServiceFixture(
        string repositoryRoot,
        string frozenConfigPath,
        FakeRng rng,
        FakeClock clock,
        InMemoryConfigSource configSource,
        FakeFileSystem fileSystem,
        FakePlatformInfo platformInfo,
        FakeHookBackend hookBackend)
    {
        RepositoryRoot = repositoryRoot;
        FrozenConfigPath = frozenConfigPath;
        Rng = rng;
        Clock = clock;
        ConfigSource = configSource;
        FileSystem = fileSystem;
        PlatformInfo = platformInfo;
        HookBackend = hookBackend;
    }

    public string RepositoryRoot { get; }

    public string FrozenConfigPath { get; }

    public FakeRng Rng { get; }

    public FakeClock Clock { get; }

    public InMemoryConfigSource ConfigSource { get; }

    public FakeFileSystem FileSystem { get; }

    public FakePlatformInfo PlatformInfo { get; }

    public FakeHookBackend HookBackend { get; }

    public string LoadFrozenConfigText()
    {
        return File.ReadAllText(FrozenConfigPath);
    }
}
