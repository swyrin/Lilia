using Discord.Interactions;

namespace Lilia.Enums;

public enum OsuUserProfileSearchType
{
    [ChoiceDisplay("profile")]
    Profile,

    [ChoiceDisplay("best_plays")]
    Best,

    [ChoiceDisplay("first_place_plays")]
    First,

    [ChoiceDisplay("recent_plays")]
    Recent
}