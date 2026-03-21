namespace Xiuxian.Scripts.Contracts
{
    public interface IRng
    {
        void Randomize();
        int NextInt(int minInclusive, int maxInclusive);
        float NextSingle();
    }
}
