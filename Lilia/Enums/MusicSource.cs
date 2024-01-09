using Discord.Interactions;

namespace Lilia.Enums;

public enum MusicSource
{
	[ChoiceDisplay("soundcloud")] SoundCloud,

	[ChoiceDisplay("youtube")] YouTube,

	[ChoiceDisplay("raw")] None
}
