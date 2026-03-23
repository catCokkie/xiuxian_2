using Godot;
using System.Collections.Generic;

namespace Xiuxian.Scripts.Services
{
    public partial class EquippedItemsState : Node
    {
        [Signal]
        public delegate void EquippedItemsChangedEventHandler();

        private readonly Dictionary<EquipmentSlotType, EquipmentStatProfile> _equippedBySlot = new();

        public bool IsEmpty => _equippedBySlot.Count == 0;

        public void Equip(EquipmentStatProfile profile)
        {
            _equippedBySlot[profile.Slot] = profile with { IsEquipped = true };
            EmitSignal(SignalName.EquippedItemsChanged);
        }

        public bool TryEquipReplacing(EquipmentStatProfile profile, out EquipmentStatProfile replacedProfile)
        {
            bool hadExisting = _equippedBySlot.TryGetValue(profile.Slot, out replacedProfile);
            _equippedBySlot[profile.Slot] = profile with { IsEquipped = true };
            EmitSignal(SignalName.EquippedItemsChanged);
            return hadExisting;
        }

        public void Unequip(EquipmentSlotType slot)
        {
            if (_equippedBySlot.Remove(slot))
            {
                EmitSignal(SignalName.EquippedItemsChanged);
            }
        }

        public void SeedIfEmpty(IReadOnlyList<EquipmentStatProfile> profiles)
        {
            if (!IsEmpty)
            {
                return;
            }

            for (int i = 0; i < profiles.Count; i++)
            {
                Equip(profiles[i]);
            }
        }

        public EquipmentStatProfile[] GetEquippedProfiles()
        {
            EquipmentStatProfile[] result = new EquipmentStatProfile[_equippedBySlot.Count];
            int index = 0;
            foreach (EquipmentStatProfile profile in _equippedBySlot.Values)
            {
                result[index] = profile;
                index++;
            }
            return result;
        }

        public bool TryGetEquippedProfile(EquipmentSlotType slot, out EquipmentStatProfile profile)
        {
            return _equippedBySlot.TryGetValue(slot, out profile);
        }

        public Godot.Collections.Dictionary<string, Variant> ToDictionary()
        {
            var result = new Godot.Collections.Dictionary<string, Variant>();
            foreach ((EquipmentSlotType slot, EquipmentStatProfile profile) in _equippedBySlot)
            {
                result[slot.ToString()] = EquipmentProfileCodec.ToDictionary(profile);
            }
            return result;
        }

        public void FromDictionary(Godot.Collections.Dictionary<string, Variant> data)
        {
            _equippedBySlot.Clear();
            foreach (string key in data.Keys)
            {
                if (data[key].VariantType != Variant.Type.Dictionary)
                {
                    continue;
                }

                if (!System.Enum.TryParse(key, out EquipmentSlotType slot))
                {
                    continue;
                }

                EquipmentStatProfile profile = EquipmentProfileCodec.FromDictionary((Godot.Collections.Dictionary<string, Variant>)data[key]);
                _equippedBySlot[slot] = profile with { Slot = slot, IsEquipped = true };
            }

            EmitSignal(SignalName.EquippedItemsChanged);
        }
    }
}
