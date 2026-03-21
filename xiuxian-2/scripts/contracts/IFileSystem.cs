namespace Xiuxian.Scripts.Contracts
{
    public interface IFileSystem
    {
        string GlobalizePath(string path);
        bool FileExists(string path);
        byte[] ReadAllBytes(string path);
        void WriteAllBytes(string path, byte[] data);
    }
}
