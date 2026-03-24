using System.Collections.Generic;

namespace Xiuxian.Scripts.Services
{
    public enum EquipmentRarityTier
    {
        CommonTool = 1,
        Artifact = 2,
        Spirit = 3,
        Treasure = 4,
    }

    public enum EquipmentSourceStage
    {
        Starter,
        Normal,
        Elite,
        Boss,
        Exchange,
        FirstClear,
    }

    public readonly record struct EquipmentSubStatData(
        string Stat,
        double Value);

    public sealed record EquipmentInstanceData(
        string EquipmentId,
        string EquipmentTemplateId,
        string DisplayName,
        EquipmentSlotType Slot,
        string SeriesId,
        EquipmentRarityTier RarityTier,
        EquipmentSourceStage SourceStage,
        string SourceLevelId,
        string MainStatKey,
        double MainStatValue,
        IReadOnlyList<EquipmentSubStatData> SubStats,
        int EnhanceLevel,
        int PowerBudget,
        long ObtainedUnix,
        bool IsEquipped);
}
