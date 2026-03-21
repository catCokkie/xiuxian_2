using Godot;
using Xiuxian.Scripts.Contracts;

namespace Xiuxian.Scripts.Adapters.Godot
{
    public sealed class SystemClock : IClock
    {
        public long GetUnixTimeSeconds()
        {
            return (long)Time.GetUnixTimeFromSystem();
        }
    }
}
