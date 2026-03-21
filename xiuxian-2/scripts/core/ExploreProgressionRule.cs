using System;

namespace Xiuxian.Scripts.Core
{
    public readonly record struct ExploreProgressAdvanceResult(float RawProgress, float NextProgress, bool Completed);

    public static class ExploreProgressionRule
    {
        public static ExploreProgressAdvanceResult Advance(float currentProgress, int inputEvents, float progressPerInput, float maxProgress)
        {
            if (inputEvents <= 0 || progressPerInput <= 0.0f || maxProgress <= 0.0f)
            {
                return new ExploreProgressAdvanceResult(currentProgress, currentProgress, false);
            }

            float rawProgress = MathF.Min(currentProgress + inputEvents * progressPerInput, maxProgress);
            bool completed = rawProgress >= maxProgress;
            return new ExploreProgressAdvanceResult(rawProgress, completed ? 0.0f : rawProgress, completed);
        }
    }
}
