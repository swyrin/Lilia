using DSharpPlus.SlashCommands;

namespace Lilia.Enums;

public enum OsuUserProfileSearchType
{
    [ChoiceName("profile")]
    Profile,

    [ChoiceName("best_plays")]
    Best,

    [ChoiceName("first_place_plays")]
    First,

    [ChoiceName("recent_plays")]
    Recent
}