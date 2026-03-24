namespace Xiuxian.Scripts.Services
{
    public static class PlayerActionCapabilityRules
    {
        public static bool HasCapability(string actionId, PlayerActionCapability capability)
        {
            return NormalizeActionId(actionId) switch
            {
                PlayerActionState.ActionCultivation => capability switch
                {
                    PlayerActionCapability.ConsumesApSettlement => true,
                    PlayerActionCapability.GrantsCultivationInputExp => true,
                    PlayerActionCapability.SupportsOfflineSettlement => true,
                    _ => false,
                },
                _ => capability switch
                {
                    PlayerActionCapability.AdvancesDungeon => true,
                    PlayerActionCapability.RunsBattle => true,
                    PlayerActionCapability.GeneratesLoot => true,
                    PlayerActionCapability.SupportsOfflineSettlement => true,
                    _ => false,
                },
            };
        }

        public static bool HasCapability(PlayerActionState? actionState, PlayerActionCapability capability)
        {
            string actionId = actionState?.ActionId ?? PlayerActionState.ActionDungeon;
            return HasCapability(actionId, capability);
        }

        public static string NormalizeActionId(string actionId)
        {
            return actionId == PlayerActionState.ActionCultivation
                ? PlayerActionState.ActionCultivation
                : PlayerActionState.ActionDungeon;
        }
    }
}
