using Discord;

namespace Lilia.Commons;

public static class MentionableFormat
{
    public static string GetMention(this IChannel channel)
    {
        return $"<#{channel.Id}>";
    }
    
    public static string GetMention(this IUser user)
    {
        return $"<@{user.Id}>";
    }
    
    public static string GetMention(this IRole role)
    {
        return $"<@&{role.Id}>";
    }
}