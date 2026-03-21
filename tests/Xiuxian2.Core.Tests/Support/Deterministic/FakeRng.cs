using Xiuxian.Scripts.Contracts;

namespace Xiuxian2.Core.Tests.Support.Deterministic;

public sealed class FakeRng : IRng
{
    private readonly Queue<int> _ints = new();
    private readonly Queue<float> _floats = new();

    public int RandomizeCallCount { get; private set; }

    public FakeRng EnqueueInt(int value)
    {
        _ints.Enqueue(value);
        return this;
    }

    public FakeRng EnqueueFloat(float value)
    {
        _floats.Enqueue(value);
        return this;
    }

    public void Randomize()
    {
        RandomizeCallCount++;
    }

    public int NextInt(int minInclusive, int maxInclusive)
    {
        if (_ints.Count == 0)
        {
            throw new InvalidOperationException("No scripted integer values remain.");
        }

        return _ints.Dequeue();
    }

    public float NextSingle()
    {
        if (_floats.Count == 0)
        {
            throw new InvalidOperationException("No scripted float values remain.");
        }

        return _floats.Dequeue();
    }
}
