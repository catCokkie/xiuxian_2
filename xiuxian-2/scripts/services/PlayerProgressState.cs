using Godot;
using System;

namespace Xiuxian.Scripts.Services
{
    /// <summary>
    /// Player growth state and realm progression.
    /// </summary>
    public partial class PlayerProgressState : Node
    {
        [Signal]
        public delegate void RealmProgressChangedEventHandler(int realmLevel, double realmExp, double realmExpRequired);

        [Signal]
        public delegate void RealmLevelUpEventHandler(int newRealmLevel);

        public int RealmLevel { get; private set; } = 1;
        public double RealmExp { get; private set; }
        public int PetMood { get; private set; } = 60;
        [Export] public bool AutoBreakthrough = false;

        public double RealmExpRequired => GetExpRequired(RealmLevel);
        public bool CanBreakthrough => RealmExp >= RealmExpRequired;

        public void AddRealmExp(double amount)
        {
            if (amount <= 0.0)
            {
                return;
            }

            RealmExp += amount;

            if (!AutoBreakthrough)
            {
                EmitSignal(SignalName.RealmProgressChanged, RealmLevel, RealmExp, RealmExpRequired);
                return;
            }

            bool leveled = false;
            while (TryBreakthrough())
            {
                leveled = true;
            }
            if (!leveled)
            {
                EmitSignal(SignalName.RealmProgressChanged, RealmLevel, RealmExp, RealmExpRequired);
            }
        }

        public bool TryBreakthrough()
        {
            if (!CanBreakthrough)
            {
                return false;
            }

            RealmExp -= RealmExpRequired;
            RealmLevel++;
            EmitSignal(SignalName.RealmLevelUp, RealmLevel);
            EmitSignal(SignalName.RealmProgressChanged, RealmLevel, RealmExp, RealmExpRequired);
            GD.Print($"PlayerProgressState: realm level up -> {RealmLevel}");
            return true;
        }

        public void SetPetMood(int mood)
        {
            int clamped = Math.Clamp(mood, 0, 100);
            if (clamped == PetMood)
            {
                return;
            }

            PetMood = clamped;
            EmitSignal(SignalName.RealmProgressChanged, RealmLevel, RealmExp, RealmExpRequired);
        }

        public double GetMoodMultiplier()
        {
            if (PetMood >= 80)
            {
                return 1.10;
            }

            if (PetMood <= 30)
            {
                return 0.85;
            }

            return 1.00;
        }

        public double GetRealmMultiplier()
        {
            return 1.0 + (RealmLevel - 1) * 0.06;
        }

        public static double GetExpRequired(int realmLevel)
        {
            int r = Math.Max(1, realmLevel);
            return 120.0 * Math.Pow(r, 1.32) + 180.0;
        }

        public Godot.Collections.Dictionary<string, Variant> ToDictionary()
        {
            return new Godot.Collections.Dictionary<string, Variant>
            {
                ["realm_level"] = RealmLevel,
                ["realm_exp"] = RealmExp,
                ["pet_mood"] = PetMood
            };
        }

        public void FromDictionary(Godot.Collections.Dictionary<string, Variant> data)
        {
            RealmLevel = data.ContainsKey("realm_level") ? Math.Max(1, data["realm_level"].AsInt32()) : 1;
            RealmExp = data.ContainsKey("realm_exp") ? Math.Max(0.0, data["realm_exp"].AsDouble()) : 0.0;
            PetMood = data.ContainsKey("pet_mood") ? Math.Clamp(data["pet_mood"].AsInt32(), 0, 100) : 60;
            EmitSignal(SignalName.RealmProgressChanged, RealmLevel, RealmExp, RealmExpRequired);
        }
    }
}
