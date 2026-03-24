using Godot;

namespace Xiuxian.Scripts.Services
{
    public static class PlayerActionStateRules
    {
        public readonly record struct PlayerActionStateData(
            string ActionId,
            string ActionTargetId,
            string ActionVariant);

        public static PlayerActionStateData Normalize(string actionId, string actionTargetId = "", string actionVariant = "")
        {
            string normalizedActionId = actionId == PlayerActionState.ActionCultivation
                ? PlayerActionState.ActionCultivation
                : PlayerActionState.ActionDungeon;

            string normalizedTargetId = normalizedActionId == PlayerActionState.ActionDungeon
                ? actionTargetId ?? string.Empty
                : string.Empty;

            string normalizedVariant = actionVariant ?? string.Empty;
            return new PlayerActionStateData(normalizedActionId, normalizedTargetId, normalizedVariant);
        }

        public static PlayerActionStateData FromDictionary(Godot.Collections.Dictionary<string, Variant> data)
        {
            string actionId = data.ContainsKey("action_id")
                ? data["action_id"].AsString()
                : (data.ContainsKey("mode_id") ? data["mode_id"].AsString() : PlayerActionState.ActionDungeon);
            string targetId = data.ContainsKey("action_target_id") ? data["action_target_id"].AsString() : string.Empty;
            string variant = data.ContainsKey("action_variant") ? data["action_variant"].AsString() : string.Empty;
            return FromPersistedValues(actionId, targetId, variant, data.ContainsKey("mode_id") ? data["mode_id"].AsString() : string.Empty);
        }

        public static PlayerActionStateData FromPersistedValues(string actionId, string actionTargetId = "", string actionVariant = "", string legacyModeId = "")
        {
            string resolvedActionId = !string.IsNullOrEmpty(actionId) ? actionId : legacyModeId;
            return Normalize(resolvedActionId, actionTargetId, actionVariant);
        }
    }
}
