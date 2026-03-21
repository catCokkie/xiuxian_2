namespace Xiuxian.Scripts.Contracts
{
    public interface IPlatformInfo
    {
        string PlatformName { get; }
        bool IsWindows();
    }
}
