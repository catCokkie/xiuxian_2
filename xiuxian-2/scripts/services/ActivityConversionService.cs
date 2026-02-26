using Godot;

namespace Xiuxian.Scripts.Services
{
    /// <summary>
    /// Converts AP to resources on a fixed interval.
    /// </summary>
    public partial class ActivityConversionService : Node
    {
        [Signal]
        public delegate void SettlementAppliedEventHandler(
            double apFinal10s,
            double lingqiGain,
            double insightGain,
            double petAffinityGain,
            double realmExpGain);

        [Export] public NodePath ActivityStatePath = "/root/InputActivityState";
        [Export] public NodePath WalletStatePath = "/root/ResourceWalletState";
        [Export] public NodePath ProgressStatePath = "/root/PlayerProgressState";

        [Export] public double SettlementIntervalSeconds = 10.0;
        [Export] public double LingqiFactor = 0.9;
        [Export] public double InsightFactor = 0.08;
        [Export] public double PetAffinityFactor = 0.03;
        [Export] public double RealmExpFromLingqiRate = 0.25;

        private InputActivityState _activityState = null!;
        private ResourceWalletState _walletState = null!;
        private PlayerProgressState _progressState = null!;

        private double _timer;
        private double _apFinalBucket;

        public override void _Ready()
        {
            _activityState = GetNodeOrNull<InputActivityState>(ActivityStatePath);
            _walletState = GetNodeOrNull<ResourceWalletState>(WalletStatePath);
            _progressState = GetNodeOrNull<PlayerProgressState>(ProgressStatePath);

            if (_activityState == null || _walletState == null || _progressState == null)
            {
                GD.PushWarning("ActivityConversionService: missing required autoload state node(s).");
                return;
            }

            _activityState.ActivityTick += OnActivityTick;
        }

        public override void _ExitTree()
        {
            if (_activityState != null)
            {
                _activityState.ActivityTick -= OnActivityTick;
            }
        }

        public override void _Process(double delta)
        {
            if (_activityState == null || _walletState == null || _progressState == null)
            {
                return;
            }

            _timer += delta;
            if (_timer < SettlementIntervalSeconds)
            {
                return;
            }

            _timer %= SettlementIntervalSeconds;
            ApplySettlement();
        }

        private void OnActivityTick(double apThisSecond, double apFinal)
        {
            _apFinalBucket += apFinal;
        }

        private void ApplySettlement()
        {
            double apFinal10s = _apFinalBucket;
            _apFinalBucket = 0.0;

            if (apFinal10s <= 0.0)
            {
                return;
            }

            double moodMul = _progressState.GetMoodMultiplier();
            double realmMul = _progressState.GetRealmMultiplier();

            double lingqiGain = apFinal10s * LingqiFactor * moodMul * realmMul;
            double insightGain = apFinal10s * InsightFactor;
            double petAffinityGain = apFinal10s * PetAffinityFactor;
            double realmExpGain = lingqiGain * RealmExpFromLingqiRate;

            _walletState.AddLingqi(lingqiGain);
            _walletState.AddInsight(insightGain);
            _walletState.AddPetAffinity(petAffinityGain);
            _progressState.AddRealmExp(realmExpGain);

            EmitSignal(SignalName.SettlementApplied, apFinal10s, lingqiGain, insightGain, petAffinityGain, realmExpGain);
        }
    }
}
