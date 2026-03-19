using Godot;
using Xiuxian.Scripts.Core;

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
        [Export] public NodePath ActionStatePath = "/root/PlayerActionState";

        [Export] public double SettlementIntervalSeconds = 10.0;
        [Export] public double LingqiFactor = 0.9;
        [Export] public double InsightFactor = 0.08;
        [Export] public double PetAffinityFactor = 0.03;
        [Export] public double RealmExpFromLingqiRate = 0.25;
        [Export] public bool CultivationInputExpEnabled = true;
        [Export] public double CultivationExpPerInput = 0.35;

        private InputActivityState _activityState = null!;
        private ResourceWalletState _walletState = null!;
        private PlayerProgressState _progressState = null!;
        private PlayerActionState _actionState = null!;

        private double _timer;
        private double _apFinalBucket;

        public override void _Ready()
        {
            _activityState = GetNodeOrNull<InputActivityState>(ActivityStatePath);
            _walletState = GetNodeOrNull<ResourceWalletState>(WalletStatePath);
            _progressState = GetNodeOrNull<PlayerProgressState>(ProgressStatePath);
            _actionState = GetNodeOrNull<PlayerActionState>(ActionStatePath);

            if (_activityState == null || _walletState == null || _progressState == null)
            {
                GD.PushWarning("ActivityConversionService: missing required autoload state node(s).");
                return;
            }

            _activityState.ActivityTick += OnActivityTick;
            _activityState.InputBatchTick += OnInputBatchTick;
        }

        public override void _ExitTree()
        {
            if (_activityState != null)
            {
                _activityState.ActivityTick -= OnActivityTick;
                _activityState.InputBatchTick -= OnInputBatchTick;
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
            if (_actionState != null && !_actionState.IsCultivationMode)
            {
                return;
            }

            _apFinalBucket += apFinal;
        }

        private void OnInputBatchTick(int inputEvents, double apFinal)
        {
            if (_progressState == null)
            {
                return;
            }

            bool isCultivationMode = _actionState == null || _actionState.IsCultivationMode;
            double gain = ActivitySettlementRule.CalculateInputRealmExpGain(CultivationInputExpEnabled, isCultivationMode, inputEvents, CultivationExpPerInput);
            if (gain > 0.0)
            {
                _progressState.AddRealmExp(gain);
            }
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

            bool isCultivationMode = _actionState == null || _actionState.IsCultivationMode;
            ActivitySettlementResult settlement = ActivitySettlementRule.CalculateSettlement(
                apFinal10s,
                LingqiFactor,
                InsightFactor,
                PetAffinityFactor,
                RealmExpFromLingqiRate,
                moodMul,
                realmMul,
                CultivationInputExpEnabled,
                isCultivationMode);

            _walletState.AddLingqi(settlement.LingqiGain);
            _walletState.AddInsight(settlement.InsightGain);
            _walletState.AddPetAffinity(settlement.PetAffinityGain);
            _progressState.AddRealmExp(settlement.RealmExpGain);

            EmitSignal(SignalName.SettlementApplied, apFinal10s, settlement.LingqiGain, settlement.InsightGain, settlement.PetAffinityGain, settlement.RealmExpGain);
        }
    }
}
