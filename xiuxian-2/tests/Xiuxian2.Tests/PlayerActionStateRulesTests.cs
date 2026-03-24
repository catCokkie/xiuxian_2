using Xiuxian.Scripts.Services;

namespace Xiuxian.Tests;

public sealed class PlayerActionStateRulesTests
{
    [Fact]
    public void Normalize_ResetsTargetWhenCultivationActionSelected()
    {
        PlayerActionStateRules.PlayerActionStateData state = PlayerActionStateRules.Normalize(PlayerActionState.ActionCultivation, "lv_qi_001", "fast");

        Assert.Equal(PlayerActionState.ActionCultivation, state.ActionId);
        Assert.Equal(string.Empty, state.ActionTargetId);
        Assert.Equal("fast", state.ActionVariant);
    }

    [Fact]
    public void FromDictionary_UsesActionFieldsWhenPresent()
    {
        PlayerActionStateRules.PlayerActionStateData state = PlayerActionStateRules.FromPersistedValues(
            PlayerActionState.ActionDungeon,
            "lv_qi_002",
            "manual");

        Assert.Equal(PlayerActionState.ActionDungeon, state.ActionId);
        Assert.Equal("lv_qi_002", state.ActionTargetId);
        Assert.Equal("manual", state.ActionVariant);
    }

    [Fact]
    public void FromDictionary_FallsBackToLegacyModeField()
    {
        PlayerActionStateRules.PlayerActionStateData state = PlayerActionStateRules.FromPersistedValues(
            actionId: string.Empty,
            legacyModeId: PlayerActionState.ModeCultivation);

        Assert.Equal(PlayerActionState.ActionCultivation, state.ActionId);
        Assert.Equal(string.Empty, state.ActionTargetId);
    }
}
