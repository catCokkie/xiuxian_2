namespace Xiuxian.Scripts.Contracts
{
    public interface IConfigSource
    {
        bool TryReadAllText(string path, out string text);
    }
}
