namespace Xiuxian2.Core.Tests.Smoke;

public sealed class TestHarnessSmokeTests
{
    [Fact]
    public void StandardRunnerExecutesTrivialAssertion()
    {
        Assert.True(false, "RED: prove the harness reports failures before implementation is finalized.");
    }

    [Fact]
    public void HarnessCanLocateRepositoryRoot()
    {
        var repositoryRoot = ResolveRepositoryRoot();

        Assert.True(Directory.Exists(repositoryRoot));
        Assert.True(File.Exists(Path.Combine(repositoryRoot, "xiuxian-2", "xiuxian2.sln")));
    }

    [Fact]
    public void DefaultSuiteRemainsGodotRuntimeFree()
    {
        Assert.True(false, "RED: document the runtime-free contract with a dedicated passing implementation step.");
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
