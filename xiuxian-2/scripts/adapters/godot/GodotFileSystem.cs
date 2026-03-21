using System.IO;
using Godot;
using Xiuxian.Scripts.Contracts;

namespace Xiuxian.Scripts.Adapters.Godot;

public sealed class GodotFileSystem : IFileSystem
{
    public string GlobalizePath(string path)
    {
        return ProjectSettings.GlobalizePath(path);
    }

    public bool FileExists(string path)
    {
        return File.Exists(path);
    }

    public byte[] ReadAllBytes(string path)
    {
        return File.ReadAllBytes(path);
    }

    public void WriteAllBytes(string path, byte[] data)
    {
        File.WriteAllBytes(path, data);
    }
}
