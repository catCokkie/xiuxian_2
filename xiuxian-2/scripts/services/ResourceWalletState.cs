using Godot;

namespace Xiuxian.Scripts.Services
{
    /// <summary>
    /// Runtime wallet for AP conversion outputs.
    /// </summary>
    public partial class ResourceWalletState : Node
    {
        [Signal]
        public delegate void WalletChangedEventHandler(double lingqi, double insight, double petAffinity);

        public double Lingqi { get; private set; }
        public double Insight { get; private set; }
        public double PetAffinity { get; private set; }

        public void AddLingqi(double amount)
        {
            if (amount <= 0.0)
            {
                return;
            }

            Lingqi += amount;
            EmitSignal(SignalName.WalletChanged, Lingqi, Insight, PetAffinity);
        }

        public void AddInsight(double amount)
        {
            if (amount <= 0.0)
            {
                return;
            }

            Insight += amount;
            EmitSignal(SignalName.WalletChanged, Lingqi, Insight, PetAffinity);
        }

        public void AddPetAffinity(double amount)
        {
            if (amount <= 0.0)
            {
                return;
            }

            PetAffinity += amount;
            EmitSignal(SignalName.WalletChanged, Lingqi, Insight, PetAffinity);
        }

        public Godot.Collections.Dictionary<string, Variant> ToDictionary()
        {
            return new Godot.Collections.Dictionary<string, Variant>
            {
                ["lingqi"] = Lingqi,
                ["insight"] = Insight,
                ["pet_affinity"] = PetAffinity
            };
        }

        public void FromDictionary(Godot.Collections.Dictionary<string, Variant> data)
        {
            Lingqi = data.ContainsKey("lingqi") ? data["lingqi"].AsDouble() : 0.0;
            Insight = data.ContainsKey("insight") ? data["insight"].AsDouble() : 0.0;
            PetAffinity = data.ContainsKey("pet_affinity") ? data["pet_affinity"].AsDouble() : 0.0;
            EmitSignal(SignalName.WalletChanged, Lingqi, Insight, PetAffinity);
        }
    }
}
