using Godot;

namespace Xiuxian.Scripts.Services
{
    /// <summary>
    /// Lightweight backpack state for loot settlement and persistence.
    /// </summary>
    public partial class BackpackState : Node
    {
        [Signal]
        public delegate void InventoryChangedEventHandler(string itemId, int amount, int newTotal);

        private readonly Godot.Collections.Dictionary<string, Variant> _items = new();

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

        public Godot.Collections.Dictionary<string, Variant> ToDictionary()
        {
            return StateSerializationContracts.NormalizeBackpack(new Godot.Collections.Dictionary<string, Variant>(_items));
        }

        public void FromDictionary(Godot.Collections.Dictionary<string, Variant> data)
        {
            _items.Clear();
            var normalized = StateSerializationContracts.NormalizeBackpack(data);
            foreach (string key in normalized.Keys)
            {
                _items[key] = normalized[key].AsInt32();
            }
        }
    }
}
