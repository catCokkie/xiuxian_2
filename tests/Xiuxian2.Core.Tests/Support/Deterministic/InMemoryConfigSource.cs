using Xiuxian.Scripts.Contracts;

namespace Xiuxian2.Core.Tests.Support.Deterministic;

public sealed class InMemoryConfigSource : IConfigSource
{
    private readonly Dictionary<string, string> _files = new(StringComparer.Ordinal);

    public InMemoryConfigSource AddFile(string path, string text)
    {
        _files[path] = text;
        return this;
    }

    public bool TryReadAllText(string path, out string text)
    {
        return _files.TryGetValue(path, out text!);
    }
}
