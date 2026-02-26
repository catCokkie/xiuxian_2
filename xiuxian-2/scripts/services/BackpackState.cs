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
            return new Godot.Collections.Dictionary<string, Variant>(_items);
        }

        public void FromDictionary(Godot.Collections.Dictionary<string, Variant> data)
        {
            _items.Clear();
            foreach (string key in data.Keys)
            {
                _items[key] = data[key].AsInt32();
            }
        }
    }
}
