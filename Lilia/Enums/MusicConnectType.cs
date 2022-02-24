using DSharpPlus.SlashCommands;

namespace Lilia.Enums;

public enum MusicConnectType
{
    [ChoiceName("normal")]
    Normal,
    
    [ChoiceName("queued_player")]
    QueuedPlayer
}