using Xiuxian.Scripts.Services;

namespace Xiuxian2.Core.Tests.Support.Deterministic;

public sealed class FakeCloudSaveBridge : ICloudSaveBridge
{
    private readonly Dictionary<string, byte[]> _files = new(StringComparer.Ordinal);

    public bool IsAvailable { get; set; } = true;

    public int WriteFileCallCount { get; private set; }

    public int TryReadFileCallCount { get; private set; }

    public string? LastWriteFileName { get; private set; }

    public byte[]? LastWrittenData { get; private set; }

    public string? LastReadFileName { get; private set; }

    public FakeCloudSaveBridge AddCloudFile(string fileName, byte[] data)
    {
        _files[fileName] = data.ToArray();
        return this;
    }

    public bool WriteFile(string fileName, byte[] data)
    {
        WriteFileCallCount++;
        LastWriteFileName = fileName;
        LastWrittenData = data.ToArray();
        _files[fileName] = data.ToArray();
        return true;
    }

    public bool TryReadFile(string fileName, out byte[] data)
    {
        TryReadFileCallCount++;
        LastReadFileName = fileName;

        if (_files.TryGetValue(fileName, out var existing))
        {
            data = existing.ToArray();
            return true;
        }

        data = Array.Empty<byte>();
        return false;
    }
}
