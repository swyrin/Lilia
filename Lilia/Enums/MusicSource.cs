using DSharpPlus.SlashCommands;

namespace Lilia.Enums;

public enum MusicSource
{
    [ChoiceName("soundcloud")]
    SoundCloud,
    
    [ChoiceName("youtube")]
    YouTube,
    
    [ChoiceName("raw")]
    None
}