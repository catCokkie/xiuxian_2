using Godot;
using Xiuxian.Scripts.Contracts;

namespace Xiuxian.Scripts.Adapters.Godot
{
    public sealed class GodotRandomAdapter : IRng
    {
        private readonly RandomNumberGenerator _randomNumberGenerator = new();

        public void Randomize()
        {
            _randomNumberGenerator.Randomize();
        }

        public int NextInt(int minInclusive, int maxInclusive)
        {
            return _randomNumberGenerator.RandiRange(minInclusive, maxInclusive);
        }

        public float NextSingle()
        {
            return _randomNumberGenerator.Randf();
        }
    }
}
