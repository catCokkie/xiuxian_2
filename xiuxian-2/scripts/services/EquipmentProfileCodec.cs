using Godot;
using System;

namespace Xiuxian.Scripts.Services
{
    public static class EquipmentProfileCodec
    {
        public static Godot.Collections.Dictionary<string, Variant> ToDictionary(EquipmentStatProfile profile)
        {
            EquipmentProfilePersistenceData data = EquipmentProfilePersistenceRules.ToData(profile);
            return new Godot.Collections.Dictionary<string, Variant>
            {
                ["equipment_id"] = data.EquipmentId,
                ["display_name"] = data.DisplayName,
                ["slot"] = data.Slot,
                ["set_tag"] = data.SetTag,
                ["rarity"] = data.Rarity,
                ["enhance_level"] = data.EnhanceLevel,
                ["is_equipped"] = data.IsEquipped,
                ["modifier"] = ModifierToDictionary(profile.Modifier),
            };
        }

        public static EquipmentStatProfile FromDictionary(Godot.Collections.Dictionary<string, Variant> data)
        {
            CharacterStatModifier modifier = default;
            if (data.ContainsKey("modifier") && data["modifier"].VariantType == Variant.Type.Dictionary)
            {
                modifier = ModifierFromDictionary((Godot.Collections.Dictionary<string, Variant>)data["modifier"]);
            }

            return EquipmentProfilePersistenceRules.FromData(new EquipmentProfilePersistenceData(
                data.ContainsKey("equipment_id") ? data["equipment_id"].AsString() : "",
                data.ContainsKey("display_name") ? data["display_name"].AsString() : "",
                data.ContainsKey("slot") ? data["slot"].AsString() : EquipmentSlotType.Weapon.ToString(),
                data.ContainsKey("set_tag") ? data["set_tag"].AsString() : "",
                data.ContainsKey("rarity") ? data["rarity"].AsInt32() : 1,
                data.ContainsKey("enhance_level") ? data["enhance_level"].AsInt32() : 0,
                !data.ContainsKey("is_equipped") || data["is_equipped"].AsBool(),
                modifier.MaxHpFlat,
                modifier.AttackFlat,
                modifier.DefenseFlat,
                modifier.SpeedFlat,
                modifier.MaxHpRate,
                modifier.AttackRate,
                modifier.DefenseRate,
                modifier.SpeedRate,
                modifier.CritChanceDelta,
                modifier.CritDamageDelta));
        }

        private static Godot.Collections.Dictionary<string, Variant> ModifierToDictionary(CharacterStatModifier modifier)
        {
            return new Godot.Collections.Dictionary<string, Variant>
            {
                ["max_hp_flat"] = modifier.MaxHpFlat,
                ["attack_flat"] = modifier.AttackFlat,
                ["defense_flat"] = modifier.DefenseFlat,
                ["speed_flat"] = modifier.SpeedFlat,
                ["max_hp_rate"] = modifier.MaxHpRate,
                ["attack_rate"] = modifier.AttackRate,
                ["defense_rate"] = modifier.DefenseRate,
                ["speed_rate"] = modifier.SpeedRate,
                ["crit_chance_delta"] = modifier.CritChanceDelta,
                ["crit_damage_delta"] = modifier.CritDamageDelta,
            };
        }

        private static CharacterStatModifier ModifierFromDictionary(Godot.Collections.Dictionary<string, Variant> data)
        {
            return new CharacterStatModifier(
                MaxHpFlat: data.ContainsKey("max_hp_flat") ? data["max_hp_flat"].AsInt32() : 0,
                AttackFlat: data.ContainsKey("attack_flat") ? data["attack_flat"].AsInt32() : 0,
                DefenseFlat: data.ContainsKey("defense_flat") ? data["defense_flat"].AsInt32() : 0,
                SpeedFlat: data.ContainsKey("speed_flat") ? data["speed_flat"].AsInt32() : 0,
                MaxHpRate: data.ContainsKey("max_hp_rate") ? data["max_hp_rate"].AsDouble() : 0.0,
                AttackRate: data.ContainsKey("attack_rate") ? data["attack_rate"].AsDouble() : 0.0,
                DefenseRate: data.ContainsKey("defense_rate") ? data["defense_rate"].AsDouble() : 0.0,
                SpeedRate: data.ContainsKey("speed_rate") ? data["speed_rate"].AsDouble() : 0.0,
                CritChanceDelta: data.ContainsKey("crit_chance_delta") ? data["crit_chance_delta"].AsDouble() : 0.0,
                CritDamageDelta: data.ContainsKey("crit_damage_delta") ? data["crit_damage_delta"].AsDouble() : 0.0);
        }
    }
}
