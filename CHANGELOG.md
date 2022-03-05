## Additions

- Added `add_playlist`, `clear`, `remove`, `remove_range` and `remove_dupe` in `music queue` command group
- Added `mute` and `unmute` in `mod general`
- Added `music playback change_player` to change the player
- Added `music playback play_stream` to play music from a livestream or a remote stream
- Added `music queue add_tracks` and `music queue add_playlist` to add tracks from from specified source

## Removals

- Removed `config set` and `config util`, merged the necessary commands together
- Removed `mod notice` commands because of its uselessness
- Removed `music queue add` with the addition of `add_tracks` and `add_playlist`

## Changes

- Now using `Discord.Net` as dependency
- Fixed descriptions of `music playback` commands
- Renamed `mod message` to `mod ticket`
- Reworked `mod ticket appeal`: now using a modal instead of interactive response
- Renamed `music playback play` to `music playback play_queue` to comply with the addition of `music playback play_stream`
- Corrected the behavior of `music playback play_queue` command [because of my dumb moment](https://github.com/angelobreuer/Lavalink4NET/issues/91)
- Renamed `music queue check` to `music queue view` and made the index starting from 0 to comply with `remove` and `remove_range` commands
- Renamed `music playback join` to `music playback connect` and `music playback leave` to `music playback disconnect` to comply with Lavalink's convention
- Reworked on the behavior of `music playback now_playing`: the response will adapt with the player type
- Reworked on the behavior of `music playback pause`, `music playback resume`: no longer depends on a specific player type
- Renamed `connect_type` to `connection_type` and `queued_player` to `queued`