using Godot;
namespace Xiuxian2.Core.Tests.Support.Serialization;

public static class StateSerializationFixtureBuilder
{
    public static Godot.Collections.Dictionary<string, Variant> CreateBackpackRoundTripPayload()
    {
        return new Godot.Collections.Dictionary<string, Variant>
        {
            ["spirit_stone"] = 12,
            ["starter_herb"] = 3
        };
    }

    public static Godot.Collections.Dictionary<string, Variant> CreateResourceWalletRoundTripPayload()
    {
        return new Godot.Collections.Dictionary<string, Variant>
        {
            ["lingqi"] = 123.5,
            ["insight"] = 9.75,
            ["pet_affinity"] = 4.25
        };
    }

    public static Godot.Collections.Dictionary<string, Variant> CreatePlayerProgressRoundTripPayload()
    {
        return new Godot.Collections.Dictionary<string, Variant>
        {
            ["realm_level"] = 2,
            ["realm_exp"] = 15.0,
            ["pet_mood"] = 85
        };
    }

    public static Godot.Collections.Dictionary<string, Variant> CreateInputActivityRoundTripPayload()
    {
        return new Godot.Collections.Dictionary<string, Variant>
        {
            ["total_key_down"] = 1L,
            ["total_mouse_click"] = 1L,
            ["total_scroll_steps"] = 2L,
            ["total_move_distance"] = 300.0,
            ["total_joypad_button"] = 1L,
            ["total_joypad_axis"] = 1L,
            ["ap_accumulator"] = 4.7
        };
    }

    public static Godot.Collections.Dictionary<string, Variant> CreatePlayerActionRoundTripPayload()
    {
        return new Godot.Collections.Dictionary<string, Variant>
        {
            ["mode_id"] = "cultivation"
        };
    }

    public static Godot.Collections.Dictionary<string, Variant> CreateBackpackMalformedPayload()
    {
        return new Godot.Collections.Dictionary<string, Variant>
        {
            ["spirit_stone"] = "bad",
            ["starter_herb"] = 2
        };
    }

    public static Godot.Collections.Dictionary<string, Variant> CreateBackpackMalformedExpectation()
    {
        return new Godot.Collections.Dictionary<string, Variant>
        {
            ["spirit_stone"] = 0,
            ["starter_herb"] = 2
        };
    }

    public static Godot.Collections.Dictionary<string, Variant> CreateResourceWalletMalformedPayload()
    {
        return new Godot.Collections.Dictionary<string, Variant>
        {
            ["lingqi"] = "bad"
        };
    }

    public static Godot.Collections.Dictionary<string, Variant> CreateResourceWalletMalformedExpectation()
    {
        return new Godot.Collections.Dictionary<string, Variant>
        {
            ["lingqi"] = 0.0,
            ["insight"] = 0.0,
            ["pet_affinity"] = 0.0
        };
    }

    public static Godot.Collections.Dictionary<string, Variant> CreatePlayerProgressMalformedPayload()
    {
        return new Godot.Collections.Dictionary<string, Variant>
        {
            ["realm_level"] = 0,
            ["realm_exp"] = -7.0,
            ["pet_mood"] = 101
        };
    }

    public static Godot.Collections.Dictionary<string, Variant> CreatePlayerProgressMalformedExpectation()
    {
        return new Godot.Collections.Dictionary<string, Variant>
        {
            ["realm_level"] = 1,
            ["realm_exp"] = 0.0,
            ["pet_mood"] = 100
        };
    }

    public static Godot.Collections.Dictionary<string, Variant> CreateInputActivityMalformedPayload()
    {
        return new Godot.Collections.Dictionary<string, Variant>
        {
            ["total_key_down"] = "bad",
            ["total_mouse_click"] = -5,
            ["total_scroll_steps"] = -1,
            ["ap_accumulator"] = -10.0
        };
    }

    public static Godot.Collections.Dictionary<string, Variant> CreateInputActivityMalformedExpectation()
    {
        return new Godot.Collections.Dictionary<string, Variant>
        {
            ["total_key_down"] = 0L,
            ["total_mouse_click"] = 0L,
            ["total_scroll_steps"] = 0L,
            ["total_move_distance"] = 0.0,
            ["total_joypad_button"] = 0L,
            ["total_joypad_axis"] = 0L,
            ["ap_accumulator"] = 0.0
        };
    }

    public static Godot.Collections.Dictionary<string, Variant> CreatePlayerActionMalformedPayload()
    {
        return new Godot.Collections.Dictionary<string, Variant>
        {
            ["mode_id"] = "invalid"
        };
    }

    public static Godot.Collections.Dictionary<string, Variant> CreatePlayerActionMalformedExpectation()
    {
        return new Godot.Collections.Dictionary<string, Variant>
        {
            ["mode_id"] = "dungeon"
        };
    }
}
