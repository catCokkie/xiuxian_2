using Xiuxian.Scripts.Services;

namespace Xiuxian.Tests;

public sealed class PlayerActionCapabilityRulesTests
{
    [Fact]
    public void CultivationAction_HasSettlementAndInputExpCapabilities()
    {
        Assert.True(PlayerActionCapabilityRules.HasCapability(PlayerActionState.ActionCultivation, PlayerActionCapability.ConsumesApSettlement));
        Assert.True(PlayerActionCapabilityRules.HasCapability(PlayerActionState.ActionCultivation, PlayerActionCapability.GrantsCultivationInputExp));
        Assert.False(PlayerActionCapabilityRules.HasCapability(PlayerActionState.ActionCultivation, PlayerActionCapability.AdvancesDungeon));
    }

    [Fact]
    public void DungeonAction_HasDungeonBattleAndLootCapabilities()
    {
        Assert.True(PlayerActionCapabilityRules.HasCapability(PlayerActionState.ActionDungeon, PlayerActionCapability.AdvancesDungeon));
        Assert.True(PlayerActionCapabilityRules.HasCapability(PlayerActionState.ActionDungeon, PlayerActionCapability.RunsBattle));
        Assert.True(PlayerActionCapabilityRules.HasCapability(PlayerActionState.ActionDungeon, PlayerActionCapability.GeneratesLoot));
        Assert.False(PlayerActionCapabilityRules.HasCapability(PlayerActionState.ActionDungeon, PlayerActionCapability.ConsumesApSettlement));
    }
}
