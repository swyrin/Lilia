using DSharpPlus.SlashCommands;

namespace Lilia.Modules;

public enum MusicConnectType
{
    [ChoiceName("stream")]
    Stream,
    
    [ChoiceName("queued_player")]
    QueuedPlayer
}