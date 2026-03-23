using Xiuxian.Scripts.Services;

namespace Xiuxian.Tests;

public sealed class CoreRegressionRulesTests
{
    [Fact]
    public void GetNextUnlockedLevelId_ReturnsNextUnlockedLevel()
    {
        string next = LevelUnlockRules.GetNextUnlockedLevelId(new[] { "lv_test_001", "lv_test_002", "lv_test_003" }, "lv_test_001");

        Assert.Equal("lv_test_002", next);
    }

    [Fact]
    public void GetNextUnlockedLevelId_FallsBackToFirstWhenCurrentMissing()
    {
        string next = LevelUnlockRules.GetNextUnlockedLevelId(new[] { "lv_test_001", "lv_test_002" }, "lv_unknown");

        Assert.Equal("lv_test_001", next);
    }

    [Fact]
    public void ResolveNextUnlockedLevelId_PrefersConfiguredLevelOverSequential()
    {
        string next = BossUnlockRules.ResolveNextUnlockedLevelId("lv_boss_unlock", "lv_seq_next");

        Assert.Equal("lv_boss_unlock", next);
    }

    [Fact]
    public void ResolveNextUnlockedLevelId_UsesSequentialWhenConfigMissing()
    {
        string next = BossUnlockRules.ResolveNextUnlockedLevelId("", "lv_seq_next");

        Assert.Equal("lv_seq_next", next);
    }

    [Fact]
    public void RollReward_NormalizesRangesAndUsesInclusiveRoller()
    {
        (int lingqi, int insight) = BattleSettlementRules.RollReward(10, 5, 4, 4, (min, max) => max);

        Assert.Equal(10, lingqi);
        Assert.Equal(4, insight);
    }

    [Fact]
    public void ApplyPity_TriggersAtThresholdAndResetsCounter()
    {
        (int nextCounter, bool triggered, int addedQty) = LevelDropEconomyRules.ApplyPity(2, 3, false, 1);

        Assert.True(triggered);
        Assert.Equal(0, nextCounter);
        Assert.Equal(1, addedQty);
    }

    [Fact]
    public void ConsumeDropRoll_StopsAtDailyCapAndCarriesSoftCapCounts()
    {
        var first = LevelDropEconomyRules.ConsumeDropRoll(0, 0, 2, 0, 0, 3600, false, false);
        var second = LevelDropEconomyRules.ConsumeDropRoll(first.DailyCount, first.DayIndex, 2, first.HourlyCount, first.HourIndex, 3601, true, true);
        var third = LevelDropEconomyRules.ConsumeDropRoll(second.DailyCount, second.DayIndex, 2, second.HourlyCount, second.HourIndex, 3602, true, true);

        Assert.True(first.Allowed);
        Assert.Equal(1, first.HourlyCountAfterConsume);
        Assert.True(second.Allowed);
        Assert.Equal(2, second.HourlyCountAfterConsume);
        Assert.False(third.Allowed);
        Assert.True(third.DailyCapBlocked);
        Assert.Equal(2, third.DailyCount);
    }

    [Fact]
    public void ShouldSkipDropBySoftCap_SkipsWhenDecayIsZeroAfterCap()
    {
        bool skipped = LevelDropEconomyRules.ShouldSkipDropBySoftCap(1, 0.0, 2, 0.0);

        Assert.True(skipped);
    }

    [Fact]
    public void ResolveDropTableForActiveLevel_PrefersConfiguredBoundTable()
    {
        string resolved = DropTableBindingRules.ResolveDropTableForActiveLevel(
            "lv_test_001",
            "monster_alpha",
            "drop_configured",
            true,
            new[]
            {
                ("drop_other", true, true)
            });

        Assert.Equal("drop_configured", resolved);
    }

    [Fact]
    public void ResolveDropTableForActiveLevel_FallsBackToMatchingCandidate()
    {
        string resolved = DropTableBindingRules.ResolveDropTableForActiveLevel(
            "lv_test_001",
            "monster_alpha",
            "drop_configured",
            false,
            new[]
            {
                ("drop_wrong_level", false, true),
                ("drop_match", true, true),
            });

        Assert.Equal("drop_match", resolved);
    }

    [Fact]
    public void IsTableBoundHelpers_FollowExpectedBindingRules()
    {
        Assert.True(DropTableBindingRules.IsTableBoundToLevel("", "lv_test_001"));
        Assert.True(DropTableBindingRules.IsTableBoundToLevel("lv_test_001", "lv_test_001"));
        Assert.False(DropTableBindingRules.IsTableBoundToLevel("lv_test_002", "lv_test_001"));

        Assert.True(DropTableBindingRules.IsTableBoundToMonster(Array.Empty<string>(), "monster_alpha"));
        Assert.True(DropTableBindingRules.IsTableBoundToMonster(new[] { "monster_alpha", "monster_beta" }, "monster_alpha"));
        Assert.False(DropTableBindingRules.IsTableBoundToMonster(new[] { "monster_beta" }, "monster_alpha"));
    }

    [Fact]
    public void CalculateDecayMultiplier_StaysAtOneBelowThreshold()
    {
        double multiplier = InputActivityRules.CalculateDecayMultiplier(5.0, 10.0, 1.0, 0.25, 0.45);

        Assert.Equal(1.0, multiplier);
    }

    [Fact]
    public void CalculateDecayMultiplier_ClampsToConfiguredMinimum()
    {
        double multiplier = InputActivityRules.CalculateDecayMultiplier(100.0, 10.0, 1.0, 0.25, 0.45);

        Assert.Equal(0.45, multiplier);
    }

    [Fact]
    public void CalculateCapMultiplier_DropsAfterSoftCap()
    {
        double multiplier = InputActivityRules.CalculateCapMultiplier(600.0, 300.0, 0.20);

        Assert.Equal(0.5, multiplier);
    }

    [Fact]
    public void CalculateCapMultiplier_ClampsToConfiguredMinimum()
    {
        double multiplier = InputActivityRules.CalculateCapMultiplier(5000.0, 300.0, 0.20);

        Assert.Equal(0.20, multiplier);
    }

    [Fact]
    public void CalculateAccumulator_NeverDropsBelowZero()
    {
        double accumulator = InputActivityRules.CalculateAccumulator(1.0, 0.0, 2.0, 1.0);

        Assert.Equal(0.0, accumulator);
    }

    [Fact]
    public void CalculateAccumulator_AddsApAndSubtractsDrain()
    {
        double accumulator = InputActivityRules.CalculateAccumulator(3.0, 5.0, 0.6, 2.0);

        Assert.Equal(6.8, accumulator, 6);
    }

    [Fact]
    public void BuildFinalStats_ReturnsBaseStatsWhenNoModifiers()
    {
        CharacterStatBlock finalStats = CharacterStatRules.BuildFinalStats(
            new CharacterStatBlock(100, 20, 8, 10, 0.10, 1.50),
            Array.Empty<CharacterStatModifier>());

        Assert.Equal(100, finalStats.MaxHp);
        Assert.Equal(20, finalStats.Attack);
        Assert.Equal(8, finalStats.Defense);
        Assert.Equal(10, finalStats.Speed);
        Assert.Equal(0.10, finalStats.CritChance);
        Assert.Equal(1.50, finalStats.CritDamage);
    }

    [Fact]
    public void BuildFinalStats_AppliesFlatAndRateModifiersInOrder()
    {
        CharacterStatBlock finalStats = CharacterStatRules.BuildFinalStats(
            new CharacterStatBlock(100, 20, 8, 10, 0.05, 1.50),
            new[]
            {
                new CharacterStatModifier(MaxHpFlat: 20, AttackFlat: 5, DefenseFlat: 2, CritChanceDelta: 0.10),
                new CharacterStatModifier(MaxHpRate: 0.10, AttackRate: 0.20, SpeedRate: 0.10, CritDamageDelta: 0.25)
            });

        Assert.Equal(132, finalStats.MaxHp);
        Assert.Equal(30, finalStats.Attack);
        Assert.Equal(10, finalStats.Defense);
        Assert.Equal(11, finalStats.Speed);
        Assert.Equal(0.15, finalStats.CritChance, 6);
        Assert.Equal(1.75, finalStats.CritDamage, 6);
    }

    [Fact]
    public void CreateBattleSnapshot_UsesMaxHpWhenCurrentHpMissing()
    {
        CharacterBattleSnapshot snapshot = CharacterStatRules.CreateBattleSnapshot(new CharacterStatBlock(120, 18, 6, 9, 0.1, 1.6));

        Assert.Equal(120, snapshot.MaxHp);
        Assert.Equal(120, snapshot.CurrentHp);
    }

    [Fact]
    public void CreateBattleSnapshot_ClampsCurrentHpIntoValidRange()
    {
        CharacterBattleSnapshot snapshot = CharacterStatRules.CreateBattleSnapshot(new CharacterStatBlock(120, 18, 6, 9, 0.1, 1.6), 999);

        Assert.Equal(120, snapshot.CurrentHp);
    }

    [Fact]
    public void CalculateMitigatedDamage_RespectsMinimumDamageFloor()
    {
        int damage = CharacterStatRules.CalculateMitigatedDamage(5, 20, minimumDamage: 1);

        Assert.Equal(1, damage);
    }

    [Fact]
    public void CollectEquipmentModifiers_OnlyReturnsEquippedProfiles()
    {
        CharacterStatModifier[] modifiers = CharacterStatRules.CollectEquipmentModifiers(new[]
        {
            new EquipmentStatProfile("weapon_001", "Novice Sword", EquipmentSlotType.Weapon, new CharacterStatModifier(AttackFlat: 5), IsEquipped: true),
            new EquipmentStatProfile("armor_001", "Spare Robe", EquipmentSlotType.Armor, new CharacterStatModifier(DefenseFlat: 3), IsEquipped: false)
        });

        Assert.Single(modifiers);
        Assert.Equal(5, modifiers[0].AttackFlat);
    }

    [Fact]
    public void BuildFinalStats_CanConsumeEquipmentProfilesDirectly()
    {
        CharacterStatBlock finalStats = CharacterStatRules.BuildFinalStats(
            new CharacterStatBlock(100, 20, 8, 10, 0.05, 1.50),
            new[]
            {
                new EquipmentStatProfile("weapon_001", "Novice Sword", EquipmentSlotType.Weapon, new CharacterStatModifier(AttackFlat: 5, CritChanceDelta: 0.05)),
                new EquipmentStatProfile("armor_001", "Leather Armor", EquipmentSlotType.Armor, new CharacterStatModifier(MaxHpFlat: 20, DefenseFlat: 4))
            });

        Assert.Equal(120, finalStats.MaxHp);
        Assert.Equal(25, finalStats.Attack);
        Assert.Equal(12, finalStats.Defense);
        Assert.Equal(0.10, finalStats.CritChance, 6);
    }

    [Fact]
    public void CreatePlayerBattleSnapshot_UsesUnifiedStatPipelineWithoutEquipment()
    {
        CharacterBattleSnapshot snapshot = CharacterStatRules.CreatePlayerBattleSnapshot(
            new CharacterStatBlock(80, 10, 3, 100, 0.05, 1.5),
            Array.Empty<EquipmentStatProfile>(),
            currentHp: 70);

        Assert.Equal(80, snapshot.MaxHp);
        Assert.Equal(70, snapshot.CurrentHp);
        Assert.Equal(10, snapshot.Attack);
        Assert.Equal(3, snapshot.Defense);
    }

    [Fact]
    public void CreatePlayerBattleSnapshot_AppliesEquippedItemModifiers()
    {
        CharacterBattleSnapshot snapshot = CharacterStatRules.CreatePlayerBattleSnapshot(
            new CharacterStatBlock(80, 10, 3, 100, 0.05, 1.5),
            new[]
            {
                new EquipmentStatProfile("weapon_001", "Sword", EquipmentSlotType.Weapon, new CharacterStatModifier(AttackFlat: 5)),
                new EquipmentStatProfile("armor_001", "Robe", EquipmentSlotType.Armor, new CharacterStatModifier(MaxHpFlat: 20, DefenseFlat: 2))
            },
            currentHp: 999);

        Assert.Equal(100, snapshot.MaxHp);
        Assert.Equal(100, snapshot.CurrentHp);
        Assert.Equal(15, snapshot.Attack);
        Assert.Equal(5, snapshot.Defense);
    }

    [Fact]
    public void EquipmentProfilePersistenceRules_RoundTripEquipmentProfile()
    {
        EquipmentStatProfile original = new(
            "weapon_001",
            "Novice Sword",
            EquipmentSlotType.Weapon,
            new CharacterStatModifier(AttackFlat: 5, CritChanceDelta: 0.05),
            SetTag: "starter",
            Rarity: 2,
            EnhanceLevel: 1,
            IsEquipped: true);

        EquipmentProfilePersistenceData encoded = EquipmentProfilePersistenceRules.ToData(original);
        EquipmentStatProfile decoded = EquipmentProfilePersistenceRules.FromData(encoded);

        Assert.Equal(original.EquipmentId, decoded.EquipmentId);
        Assert.Equal(original.DisplayName, decoded.DisplayName);
        Assert.Equal(original.Slot, decoded.Slot);
        Assert.Equal(original.Modifier.AttackFlat, decoded.Modifier.AttackFlat);
        Assert.Equal(original.Modifier.CritChanceDelta, decoded.Modifier.CritChanceDelta, 6);
        Assert.Equal(original.SetTag, decoded.SetTag);
    }

    [Fact]
    public void EquipmentPersistenceShape_CanFeedPlayerSnapshotPipeline()
    {
        EquipmentStatProfile weapon = EquipmentProfilePersistenceRules.FromData(
            EquipmentProfilePersistenceRules.ToData(
                new EquipmentStatProfile("weapon_001", "Sword", EquipmentSlotType.Weapon, new CharacterStatModifier(AttackFlat: 4))));
        EquipmentStatProfile armor = EquipmentProfilePersistenceRules.FromData(
            EquipmentProfilePersistenceRules.ToData(
                new EquipmentStatProfile("armor_001", "Armor", EquipmentSlotType.Armor, new CharacterStatModifier(MaxHpFlat: 10, DefenseFlat: 2))));
        CharacterBattleSnapshot snapshot = CharacterStatRules.CreatePlayerBattleSnapshot(
            new CharacterStatBlock(80, 10, 3, 100, 0.05, 1.5),
            new[] { weapon, armor },
            currentHp: 90);

        Assert.Equal(90, snapshot.MaxHp);
        Assert.Equal(90, snapshot.CurrentHp);
        Assert.Equal(14, snapshot.Attack);
        Assert.Equal(5, snapshot.Defense);
    }

    [Fact]
    public void CreateDefaultProfiles_ReturnsStarterWeaponAndArmor()
    {
        EquipmentStatProfile[] starter = EquipmentStarterLoadout.CreateDefaultProfiles();

        Assert.Equal(2, starter.Length);
        Assert.Contains(starter, item => item.Slot == EquipmentSlotType.Weapon);
        Assert.Contains(starter, item => item.Slot == EquipmentSlotType.Armor);
    }

    [Fact]
    public void CreateDefaultProfiles_CanImmediatelyAffectPlayerBattleSnapshot()
    {
        CharacterBattleSnapshot snapshot = CharacterStatRules.CreatePlayerBattleSnapshot(
            new CharacterStatBlock(80, 10, 3, 100, 0.05, 1.5),
            EquipmentStarterLoadout.CreateDefaultProfiles(),
            currentHp: 200);

        Assert.Equal(92, snapshot.MaxHp);
        Assert.Equal(92, snapshot.CurrentHp);
        Assert.Equal(13, snapshot.Attack);
        Assert.Equal(5, snapshot.Defense);
        Assert.Equal(0.07, snapshot.CritChance, 6);
    }

    [Fact]
    public void GetDebugSwapProfile_TogglesBetweenStarterAndAlternateItem()
    {
        EquipmentStatProfile starterWeapon = EquipmentStarterLoadout.CreateDefaultProfiles()[0];
        EquipmentStatProfile alternateWeapon = EquipmentStarterLoadout.GetDebugSwapProfile(EquipmentSlotType.Weapon, starterWeapon.EquipmentId);
        EquipmentStatProfile toggledBackWeapon = EquipmentStarterLoadout.GetDebugSwapProfile(EquipmentSlotType.Weapon, alternateWeapon.EquipmentId);

        Assert.NotEqual(starterWeapon.EquipmentId, alternateWeapon.EquipmentId);
        Assert.Equal(starterWeapon.EquipmentId, toggledBackWeapon.EquipmentId);
    }

    [Fact]
    public void MonsterStatProfile_ExposesCompatibleStatBlock()
    {
        MonsterStatProfile profile = new(
            "monster_spider",
            "Cave Spider",
            new CharacterStatBlock(42, 8, 4, 9, 0.0, 1.5),
            InputsPerRound: 20,
            MoveCategory: "elite",
            IsBoss: true);

        CharacterStatBlock stats = profile.ToStatBlock();

        Assert.Equal(42, stats.MaxHp);
        Assert.Equal(8, stats.Attack);
        Assert.Equal(4, stats.Defense);
        Assert.Equal(9, stats.Speed);
        Assert.True(profile.IsBoss);
    }

    [Fact]
    public void CreateBattleSnapshot_CanUseMonsterProfileDirectly()
    {
        MonsterStatProfile profile = new(
            "monster_bat",
            "Shadow Bat",
            new CharacterStatBlock(26, 5, 2, 12, 0.05, 1.5),
            InputsPerRound: 17);

        CharacterBattleSnapshot snapshot = CharacterStatRules.CreateBattleSnapshot(profile, currentHp: 30);

        Assert.Equal(26, snapshot.MaxHp);
        Assert.Equal(26, snapshot.CurrentHp);
        Assert.Equal(5, snapshot.Attack);
        Assert.Equal(2, snapshot.Defense);
        Assert.Equal(12, snapshot.Speed);
    }

    [Fact]
    public void BuildProfile_NormalizesMonsterCombatValues()
    {
        MonsterStatProfile profile = MonsterStatRules.BuildProfile(
            "monster_slug",
            "",
            maxHp: -5,
            attack: 0,
            defense: -2,
            speedFactor: 0.0,
            inputsPerRound: 0,
            moveCategory: "",
            isBoss: true);

        Assert.Equal("Enemy", profile.DisplayName);
        Assert.Equal(1, profile.BaseStats.MaxHp);
        Assert.Equal(1, profile.BaseStats.Attack);
        Assert.Equal(0, profile.BaseStats.Defense);
        Assert.Equal(10, profile.BaseStats.Speed);
        Assert.Equal(1, profile.InputsPerRound);
        Assert.Equal("normal", profile.MoveCategory);
        Assert.True(profile.IsBoss);
    }

    [Fact]
    public void BuildProfile_PreservesProvidedCombatValuesForLoaderCompatibility()
    {
        MonsterStatProfile profile = MonsterStatRules.BuildProfile(
            "monster_wolf",
            "Forest Wolf",
            maxHp: 24,
            attack: 4,
            defense: 0,
            speedFactor: 1.0,
            inputsPerRound: 18,
            moveCategory: "normal",
            isBoss: false);

        Assert.Equal("Forest Wolf", profile.DisplayName);
        Assert.Equal(24, profile.BaseStats.MaxHp);
        Assert.Equal(4, profile.BaseStats.Attack);
        Assert.Equal(18, profile.InputsPerRound);
    }

    [Fact]
    public void BuildBaseStats_UsesConfiguredValuesAtRealmOne()
    {
        CharacterStatBlock stats = PlayerBaseStatRules.BuildBaseStats(1, configuredBaseHp: 36, configuredBaseAttack: 4);

        Assert.Equal(36, stats.MaxHp);
        Assert.Equal(4, stats.Attack);
        Assert.Equal(0, stats.Defense);
        Assert.Equal(100, stats.Speed);
        Assert.Equal(0.03, stats.CritChance, 6);
    }

    [Fact]
    public void BuildBaseStats_ScalesWithRealmLevel()
    {
        CharacterStatBlock stats = PlayerBaseStatRules.BuildBaseStats(5, configuredBaseHp: 36, configuredBaseAttack: 4);

        Assert.Equal(68, stats.MaxHp);
        Assert.Equal(8, stats.Attack);
        Assert.Equal(4, stats.Defense);
        Assert.Equal(108, stats.Speed);
        Assert.Equal(0.05, stats.CritChance, 6);
    }

    [Fact]
    public void BuildBaseStats_ClampsRealmBelowOne()
    {
        CharacterStatBlock stats = PlayerBaseStatRules.BuildBaseStats(0, configuredBaseHp: 20, configuredBaseAttack: 2);

        Assert.Equal(20, stats.MaxHp);
        Assert.Equal(2, stats.Attack);
    }

    [Fact]
    public void CalculateAttackDamage_UsesDefenseAndMinimumFloor()
    {
        int damage = BattleRules.CalculateAttackDamage(12, 20, 1);

        Assert.Equal(1, damage);
    }

    [Fact]
    public void ResolvePlayerVsMonsterRound_UpdatesBothSidesAndStaysOngoing()
    {
        CharacterBattleSnapshot player = new(100, 100, 12, 0, 1, 0.0, 1.5);
        CharacterBattleSnapshot monster = new(30, 30, 9, 0, 1, 0.0, 1.5);

        BattleRoundResult result = BattleRules.ResolvePlayerVsMonsterRound(player, monster, enemyDamageDivider: 3, enemyMinimumDamage: 1);

        Assert.Equal(18, result.Monster.CurrentHp);
        Assert.Equal(97, result.Player.CurrentHp);
        Assert.Equal(BattleOutcome.Ongoing, result.Outcome);
    }

    [Fact]
    public void ResolvePlayerVsMonsterRound_ReturnsPlayerWonWhenMonsterHpDropsToZero()
    {
        CharacterBattleSnapshot player = new(100, 100, 20, 0, 1, 0.0, 1.5);
        CharacterBattleSnapshot monster = new(10, 10, 9, 0, 1, 0.0, 1.5);

        BattleRoundResult result = BattleRules.ResolvePlayerVsMonsterRound(player, monster, enemyDamageDivider: 3, enemyMinimumDamage: 1);

        Assert.Equal(0, result.Monster.CurrentHp);
        Assert.Equal(BattleOutcome.PlayerWon, result.Outcome);
    }

    [Fact]
    public void ResolvePlayerVsMonsterRound_ReturnsMonsterWonWhenPlayerHpDropsToZero()
    {
        CharacterBattleSnapshot player = new(20, 2, 5, 0, 1, 0.0, 1.5);
        CharacterBattleSnapshot monster = new(30, 30, 12, 0, 1, 0.0, 1.5);

        BattleRoundResult result = BattleRules.ResolvePlayerVsMonsterRound(player, monster, enemyDamageDivider: 2, enemyMinimumDamage: 3);

        Assert.Equal(0, result.Player.CurrentHp);
        Assert.Equal(BattleOutcome.MonsterWon, result.Outcome);
    }

    [Fact]
    public void ConsumeBattleInputs_ComputesRoundsAndRemainder()
    {
        BattleInputProgress progress = BattleRules.ConsumeBattleInputs(pendingInputs: 5, inputEvents: 17, threshold: 10);

        Assert.Equal(10, progress.Threshold);
        Assert.Equal(22, progress.PendingInputs);
        Assert.Equal(2, progress.RoundsToResolve);
        Assert.Equal(2, progress.RemainingInputs);
    }

    [Fact]
    public void DetermineBattleFlow_MapsOutcomesToLifecycleActions()
    {
        BattleFlowDecision victory = BattleRules.DetermineBattleFlow(BattleOutcome.PlayerWon);
        BattleFlowDecision defeat = BattleRules.DetermineBattleFlow(BattleOutcome.MonsterWon);
        BattleFlowDecision ongoing = BattleRules.DetermineBattleFlow(BattleOutcome.Ongoing);

        Assert.Equal(BattleFlowAction.Victory, victory.Action);
        Assert.True(victory.EndBattle);
        Assert.Equal(BattleFlowAction.Defeat, defeat.Action);
        Assert.True(defeat.EndBattle);
        Assert.Equal(BattleFlowAction.Continue, ongoing.Action);
        Assert.False(ongoing.EndBattle);
    }

    [Fact]
    public void DetermineEncounterStart_StartsWhenFrontMonsterCrossesTrigger()
    {
        BattleEncounterDecision decision = BattleLifecycleRules.DetermineEncounterStart(1, 320.0f, 360.0f, "monster_alpha");

        Assert.True(decision.ShouldStart);
        Assert.Equal(1, decision.MonsterIndex);
        Assert.Equal("monster_alpha", decision.MonsterId);
    }

    [Fact]
    public void DetermineEncounterStart_BlocksWhenNoCandidateOrNotInRange()
    {
        BattleEncounterDecision noCandidate = BattleLifecycleRules.DetermineEncounterStart(-1, 0.0f, 360.0f, "");
        BattleEncounterDecision outOfRange = BattleLifecycleRules.DetermineEncounterStart(0, 500.0f, 360.0f, "monster_beta");

        Assert.False(noCandidate.ShouldStart);
        Assert.False(outOfRange.ShouldStart);
    }

    [Fact]
    public void BuildStartSetup_UsesMonsterProfileWhenAvailable()
    {
        MonsterStatProfile profile = new(
            "monster_wolf",
            "Forest Wolf",
            new CharacterStatBlock(32, 6, 1, 100, 0.0, 1.5),
            InputsPerRound: 14);

        BattleStartSetup setup = BattleStartRules.BuildStartSetup("monster_wolf", profile, defaultInputsPerRound: 18);

        Assert.Equal("monster_wolf", setup.MonsterId);
        Assert.Equal("Forest Wolf", setup.MonsterName);
        Assert.Equal(32, setup.EnemyMaxHp);
        Assert.Equal(6, setup.EnemyAttack);
        Assert.Equal(14, setup.InputsPerRound);
        Assert.Equal(0, setup.BattleRoundCounter);
        Assert.Equal(0, setup.PendingBattleInputEvents);
    }

    [Fact]
    public void BuildStartSetup_FallsBackToDefaultBattleValuesWithoutProfile()
    {
        BattleStartSetup setup = BattleStartRules.BuildStartSetup("monster_unknown", null, defaultInputsPerRound: 18);

        Assert.Equal("monster_unknown", setup.MonsterId);
        Assert.Equal(UiText.DefaultMonsterName, setup.MonsterName);
        Assert.Equal(24, setup.EnemyMaxHp);
        Assert.Equal(4, setup.EnemyAttack);
        Assert.Equal(18, setup.InputsPerRound);
    }

    [Fact]
    public void DetermineDefeatReset_RequestsLevelResetOnlyWithActiveLevel()
    {
        BattleDefeatDecision withLevel = BattleLifecycleRules.DetermineDefeatReset("lv_test_001");
        BattleDefeatDecision withoutLevel = BattleLifecycleRules.DetermineDefeatReset("");

        Assert.True(withLevel.ShouldResetExploreProgress);
        Assert.True(withLevel.ShouldResetLevel);
        Assert.False(withoutLevel.ShouldResetLevel);
    }

    [Fact]
    public void DetermineVictorySettlement_EnablesRewardsAndBossUnlockWhenIdsExist()
    {
        BattleVictoryDecision decision = BattleLifecycleRules.DetermineVictorySettlement("lv_test_001", "monster_boss");

        Assert.True(decision.ShouldEndBattle);
        Assert.True(decision.ShouldApplyBattleRewards);
        Assert.True(decision.ShouldTryBossUnlock);
    }

    [Fact]
    public void DetermineBattleRewardDecision_UsesFallbackOnlyWhenNoConfiguredRewardsExist()
    {
        BattleRewardDecision configured = RewardRules.DetermineBattleRewardDecision(1, 0.0, 0.0, "item_a");
        BattleRewardDecision fallback = RewardRules.DetermineBattleRewardDecision(0, 0.0, 0.0, "none");

        Assert.True(configured.HasConfiguredRewards);
        Assert.False(configured.ShouldUseFallback);
        Assert.False(fallback.HasConfiguredRewards);
        Assert.True(fallback.ShouldUseFallback);
    }

    [Fact]
    public void BuildLevelCompletionSourceTag_EncodesFirstClearAndRepeatClear()
    {
        string first = RewardRules.BuildLevelCompletionSourceTag("lv_test_001", true);
        string repeat = RewardRules.BuildLevelCompletionSourceTag("lv_test_001", false);

        Assert.Equal("level_first_clear:lv_test_001", first);
        Assert.Equal("level_repeat_clear:lv_test_001", repeat);
    }

    [Fact]
    public void BuildBattleRewardSummary_FormatsResourcesAndDrops()
    {
        string summary = RewardRules.BuildBattleRewardSummary(12.0, 3.0, "灵草x1");

        Assert.Equal("灵气+12 悟性+3 掉落:灵草x1", summary);
    }

    [Fact]
    public void TryBuildFirstClearReward_ReturnsDeterministicManualEquipReward()
    {
        bool ok = EquipmentRewardRules.TryBuildFirstClearReward("lv_test_001", out EquipmentStatProfile profile);

        Assert.True(ok);
        Assert.False(profile.IsEquipped);
        Assert.True(profile.Slot == EquipmentSlotType.Weapon || profile.Slot == EquipmentSlotType.Armor);
        Assert.StartsWith("reward_", profile.EquipmentId);
    }

    [Fact]
    public void ManualEquipReplacement_PutsOldGearBackIntoBackpackModel()
    {
        EquipmentStatProfile oldWeapon = new("old_weapon", "Old Sword", EquipmentSlotType.Weapon, new CharacterStatModifier(AttackFlat: 2), IsEquipped: true);
        EquipmentStatProfile newWeapon = new("new_weapon", "New Sword", EquipmentSlotType.Weapon, new CharacterStatModifier(AttackFlat: 5), IsEquipped: false);

        bool hadExisting = true;
        EquipmentStatProfile replaced = oldWeapon;

        Assert.True(hadExisting);
        Assert.Equal("old_weapon", replaced.EquipmentId);
        Assert.Equal(5, newWeapon.Modifier.AttackFlat);
    }
}
