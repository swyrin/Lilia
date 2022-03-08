namespace Lilia.Database.Models;

public class DbUser
{
	public ulong Id { get; set; }
	public short WarnCount { get; set; }
	public string OsuMode { get; set; }
	public string OsuUsername { get; set; }
}
