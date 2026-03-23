namespace Xiuxian.Scripts.Game
{
    public static class ExploreProgressRules
    {
        public static (float NextProgress, bool CompletedLevel) AdvanceProgress(
            float currentProgress,
            int inputEvents,
            float progressPerInput,
            float maxProgress)
        {
            if (inputEvents <= 0 || progressPerInput <= 0.0f || maxProgress <= 0.0f)
            {
                return (currentProgress, false);
            }

            float nextProgress = currentProgress + inputEvents * progressPerInput;
            if (nextProgress >= maxProgress)
            {
                return (0.0f, true);
            }

            return (nextProgress, false);
        }
    }
}
