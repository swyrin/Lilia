using Discord.Interactions;

namespace Lilia.Enums
{
    public enum OsuUserProfileSearchMode
    {
        [ChoiceDisplay("default")] Default,

        [ChoiceDisplay("osu")] Osu,

        [ChoiceDisplay("mania")] Mania,

        [ChoiceDisplay("catch_the_beat")] Fruits,

        [ChoiceDisplay("taiko")] Taiko
    }
}
