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

        public const string ModeDungeon = "dungeon";
        public const string ModeCultivation = "cultivation";

        private string _modeId = ModeDungeon;

        public string ModeId => _modeId;
        public bool IsDungeonMode => _modeId == ModeDungeon;
        public bool IsCultivationMode => _modeId == ModeCultivation;

        public void SetMode(string modeId)
        {
            string next = NormalizeMode(modeId);
            if (next == _modeId)
            {
                return;
            }

            _modeId = next;
            EmitSignal(SignalName.ModeChanged, _modeId);
        }

        public void ToggleMode()
        {
            SetMode(IsDungeonMode ? ModeCultivation : ModeDungeon);
        }

        public Godot.Collections.Dictionary<string, Variant> ToDictionary()
        {
            return new Godot.Collections.Dictionary<string, Variant>
            {
                ["mode_id"] = _modeId
            };
        }

        public void FromDictionary(Godot.Collections.Dictionary<string, Variant> data)
        {
            string modeId = data.ContainsKey("mode_id") ? data["mode_id"].AsString() : ModeDungeon;
            _modeId = NormalizeMode(modeId);
            EmitSignal(SignalName.ModeChanged, _modeId);
        }

        private static string NormalizeMode(string modeId)
        {
            return modeId == ModeCultivation ? ModeCultivation : ModeDungeon;
        }
    }
}
