using System;

namespace Lilia.Database.Models;

public class DbGuild
{
	public ulong Id { get; set; }

	public bool IsWelcomeEnabled { get; set; }
	public ulong WelcomeChannelId { get; set; }
	public string WelcomeMessage { get; set; }
	public bool IsGoodbyeEnabled { get; set; }
	public ulong GoodbyeChannelId { get; set; }
	public string GoodbyeMessage { get; set; }
	public DateTime RadioStartTime { get; set; }
}
