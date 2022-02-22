using DSharpPlus.SlashCommands;

namespace Lilia.Enums;

public enum OsuUserProfileSearchMode
{
    [ChoiceName("default")]
    Default,

    [ChoiceName("standard")]
    Osu,

    [ChoiceName("mania")]
    Mania,

    [ChoiceName("catch_the_beat")]
    Fruits,

    [ChoiceName("taiko")]
    Taiko
}