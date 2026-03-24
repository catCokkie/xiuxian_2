using Godot;

namespace Xiuxian.Scripts.Services
{
    /// <summary>
    /// Global main-action mode:
    /// - dungeon: explore + battle loop
    /// - cultivation: pause dungeon progression and focus on cultivation conversion
    /// </summary>
    public partial class PlayerActionState : Node
    {
        [Signal]
        public delegate void ModeChangedEventHandler(string modeId);

        [Signal]
        public delegate void ActionChangedEventHandler(string actionId, string actionTargetId, string actionVariant);

        public const string ModeDungeon = "dungeon";
        public const string ModeCultivation = "cultivation";

        public const string ActionDungeon = ModeDungeon;
        public const string ActionCultivation = ModeCultivation;

        private string _actionId = ActionDungeon;
        private string _actionTargetId = string.Empty;
        private string _actionVariant = string.Empty;

        public string ModeId => _actionId;
        public string ActionId => _actionId;
        public string ActionTargetId => _actionTargetId;
        public string ActionVariant => _actionVariant;
        public bool IsDungeonMode => _actionId == ActionDungeon;
        public bool IsCultivationMode => _actionId == ActionCultivation;
        public bool IsDungeonAction => _actionId == ActionDungeon;
        public bool IsCultivationAction => _actionId == ActionCultivation;

        public void SetMode(string modeId)
        {
            SetAction(modeId);
        }

        public void SetAction(string actionId, string actionTargetId = "", string actionVariant = "")
        {
            PlayerActionStateRules.PlayerActionStateData next = PlayerActionStateRules.Normalize(actionId, actionTargetId, actionVariant);
            if (next.ActionId == _actionId && next.ActionTargetId == _actionTargetId && next.ActionVariant == _actionVariant)
            {
                return;
            }

            bool actionChanged = next.ActionId != _actionId;
            _actionId = next.ActionId;
            _actionTargetId = next.ActionTargetId;
            _actionVariant = next.ActionVariant;

            if (actionChanged)
            {
                EmitSignal(SignalName.ModeChanged, _actionId);
            }

            EmitSignal(SignalName.ActionChanged, _actionId, _actionTargetId, _actionVariant);
        }

        public void ToggleMode()
        {
            SetAction(IsDungeonMode ? ActionCultivation : ActionDungeon);
        }

        public Godot.Collections.Dictionary<string, Variant> ToDictionary()
        {
            return new Godot.Collections.Dictionary<string, Variant>
            {
                ["mode_id"] = _actionId,
                ["action_id"] = _actionId,
                ["action_target_id"] = _actionTargetId,
                ["action_variant"] = _actionVariant,
            };
        }

        public void FromDictionary(Godot.Collections.Dictionary<string, Variant> data)
        {
            PlayerActionStateRules.PlayerActionStateData next = PlayerActionStateRules.FromDictionary(data);
            _actionId = next.ActionId;
            _actionTargetId = next.ActionTargetId;
            _actionVariant = next.ActionVariant;
            EmitSignal(SignalName.ModeChanged, _actionId);
            EmitSignal(SignalName.ActionChanged, _actionId, _actionTargetId, _actionVariant);
        }
    }
}
