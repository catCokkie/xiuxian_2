using Xiuxian.Scripts.Contracts;

namespace Xiuxian2.Core.Tests.Support.Deterministic;

public sealed class FakeFileSystem : IFileSystem
{
    private readonly Dictionary<string, byte[]> _files = new(StringComparer.Ordinal);
    private readonly string _root;

    public int ReadAllBytesCallCount { get; private set; }

    public int WriteAllBytesCallCount { get; private set; }

    public string? LastReadPath { get; private set; }

    public string? LastWrittenPath { get; private set; }

    public FakeFileSystem(string root = "/virtual")
    {
        _root = root.TrimEnd('/');
    }

    public FakeFileSystem AddFile(string path, byte[] data)
    {
        _files[path] = data.ToArray();
        return this;
    }

    public string GlobalizePath(string path)
    {
        if (path.StartsWith(_root, StringComparison.Ordinal))
        {
            return path;
        }

        return $"{_root}/{path.TrimStart('/')}";
    }

    public bool FileExists(string path)
    {
        return _files.ContainsKey(path);
    }

    public byte[] ReadAllBytes(string path)
    {
        ReadAllBytesCallCount++;
        LastReadPath = path;

        if (!_files.TryGetValue(path, out var data))
        {
            throw new FileNotFoundException($"No scripted file exists at '{path}'.", path);
        }

        return data.ToArray();
    }

    public void WriteAllBytes(string path, byte[] data)
    {
        WriteAllBytesCallCount++;
        LastWrittenPath = path;
        _files[path] = data.ToArray();
    }
}
