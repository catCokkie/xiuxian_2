using System.Collections.Generic;

namespace Xiuxian2.Core.Tests.Support.Serialization;

public static class StateSerializationFixtureBuilder
{
    public static Dictionary<string, object?> CreateBackpackRoundTripPayload()
    {
        return new Dictionary<string, object?>
        {
            ["spirit_stone"] = 12,
            ["starter_herb"] = 3
        };
    }

    public static Dictionary<string, object?> CreateResourceWalletRoundTripPayload()
    {
        return new Dictionary<string, object?>
        {
            ["lingqi"] = 123.5,
            ["insight"] = 9.75,
            ["pet_affinity"] = 4.25
        };
    }

    public static Dictionary<string, object?> CreatePlayerProgressRoundTripPayload()
    {
        return new Dictionary<string, object?>
        {
            ["realm_level"] = 2,
            ["realm_exp"] = 15.0,
            ["pet_mood"] = 85
        };
    }

    public static Dictionary<string, object?> CreateInputActivityRoundTripPayload()
    {
        return new Dictionary<string, object?>
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

    public static Dictionary<string, object?> CreatePlayerActionRoundTripPayload()
    {
        return new Dictionary<string, object?>
        {
            ["mode_id"] = "cultivation"
        };
    }

    public static Dictionary<string, object?> CreateBackpackMalformedPayload()
    {
        return new Dictionary<string, object?>
        {
            ["spirit_stone"] = "bad",
            ["starter_herb"] = 2
        };
    }

    public static Dictionary<string, object?> CreateBackpackMalformedExpectation()
    {
        return new Dictionary<string, object?>
        {
            ["spirit_stone"] = 0,
            ["starter_herb"] = 2
        };
    }

    public static Dictionary<string, object?> CreateResourceWalletMalformedPayload()
    {
        return new Dictionary<string, object?>
        {
            ["lingqi"] = "bad"
        };
    }

    public static Dictionary<string, object?> CreateResourceWalletMalformedExpectation()
    {
        return new Dictionary<string, object?>
        {
            ["lingqi"] = 0.0,
            ["insight"] = 0.0,
            ["pet_affinity"] = 0.0
        };
    }

    public static Dictionary<string, object?> CreatePlayerProgressMalformedPayload()
    {
        return new Dictionary<string, object?>
        {
            ["realm_level"] = 0,
            ["realm_exp"] = -7.0,
            ["pet_mood"] = 101
        };
    }

    public static Dictionary<string, object?> CreatePlayerProgressMalformedExpectation()
    {
        return new Dictionary<string, object?>
        {
            ["realm_level"] = 1,
            ["realm_exp"] = 0.0,
            ["pet_mood"] = 100
        };
    }

    public static Dictionary<string, object?> CreateInputActivityMalformedPayload()
    {
        return new Dictionary<string, object?>
        {
            ["total_key_down"] = "bad",
            ["total_mouse_click"] = -5,
            ["total_scroll_steps"] = -1,
            ["ap_accumulator"] = -10.0
        };
    }

    public static Dictionary<string, object?> CreateInputActivityMalformedExpectation()
    {
        return new Dictionary<string, object?>
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

    public static Dictionary<string, object?> CreatePlayerActionMalformedPayload()
    {
        return new Dictionary<string, object?>
        {
            ["mode_id"] = "invalid"
        };
    }

    public static Dictionary<string, object?> CreatePlayerActionMalformedExpectation()
    {
        return new Dictionary<string, object?>
        {
            ["mode_id"] = "dungeon"
        };
    }
}
