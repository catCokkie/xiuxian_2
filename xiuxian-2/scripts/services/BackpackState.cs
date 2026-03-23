using Godot;
using System.Collections.Generic;

namespace Xiuxian.Scripts.Services
{
    /// <summary>
    /// Lightweight backpack state for loot settlement and persistence.
    /// </summary>
    public partial class BackpackState : Node
    {
        [Signal]
        public delegate void InventoryChangedEventHandler(string itemId, int amount, int newTotal);

        [Signal]
        public delegate void EquipmentInventoryChangedEventHandler();

        private readonly Godot.Collections.Dictionary<string, Variant> _items = new();
        private readonly Dictionary<string, EquipmentStatProfile> _equipmentProfiles = new();

        public int GetItemCount(string itemId)
        {
            if (_items.ContainsKey(itemId))
            {
                return _items[itemId].AsInt32();
            }

            return 0;
        }

        public void AddItem(string itemId, int amount)
        {
            if (amount <= 0)
            {
                return;
            }

            int next = GetItemCount(itemId) + amount;
            _items[itemId] = next;
            EmitSignal(SignalName.InventoryChanged, itemId, amount, next);
        }

        public void AddEquipment(EquipmentStatProfile profile)
        {
            _equipmentProfiles[profile.EquipmentId] = profile with { IsEquipped = false };
            EmitSignal(SignalName.EquipmentInventoryChanged);
        }

        public bool HasEquipment(string equipmentId)
        {
            return _equipmentProfiles.ContainsKey(equipmentId);
        }

        public bool TryTakeEquipmentBySlot(EquipmentSlotType slot, out EquipmentStatProfile profile)
        {
            foreach (EquipmentStatProfile item in _equipmentProfiles.Values)
            {
                if (item.Slot == slot)
                {
                    profile = item with { IsEquipped = true };
                    _equipmentProfiles.Remove(item.EquipmentId);
                    EmitSignal(SignalName.EquipmentInventoryChanged);
                    return true;
                }
            }

            profile = default;
            return false;
        }

        public EquipmentStatProfile[] GetEquipmentProfiles()
        {
            EquipmentStatProfile[] result = new EquipmentStatProfile[_equipmentProfiles.Count];
            int index = 0;
            foreach (EquipmentStatProfile profile in _equipmentProfiles.Values)
            {
                result[index] = profile;
                index++;
            }

            return result;
        }

        public Godot.Collections.Dictionary<string, Variant> ToDictionary()
        {
            var result = new Godot.Collections.Dictionary<string, Variant>(_items);
            var equipment = new Godot.Collections.Array<Variant>();
            foreach (EquipmentStatProfile profile in _equipmentProfiles.Values)
            {
                equipment.Add(EquipmentProfileCodec.ToDictionary(profile));
            }

            result["__equipment_profiles"] = equipment;
            return result;
        }

        public void FromDictionary(Godot.Collections.Dictionary<string, Variant> data)
        {
            _items.Clear();
            _equipmentProfiles.Clear();
            foreach (string key in data.Keys)
            {
                if (key == "__equipment_profiles")
                {
                    continue;
                }
                _items[key] = data[key].AsInt32();
            }

            if (data.ContainsKey("__equipment_profiles") && data["__equipment_profiles"].VariantType == Variant.Type.Array)
            {
                var equipment = (Godot.Collections.Array<Variant>)data["__equipment_profiles"];
                foreach (Variant item in equipment)
                {
                    if (item.VariantType != Variant.Type.Dictionary)
                    {
                        continue;
                    }

                    EquipmentStatProfile profile = EquipmentProfileCodec.FromDictionary((Godot.Collections.Dictionary<string, Variant>)item) with { IsEquipped = false };
                    _equipmentProfiles[profile.EquipmentId] = profile;
                }
            }
        }
    }
}
