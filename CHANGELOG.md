## Additions

- Added `add_playlist`, `clear`, `remove`, `remove_range` and `remove_dupe` in `music queue` command group
- Added `mute` and `unmute` in `mod general`
- Added `music playback play_stream` to play music from a livestream or a remote stream 

## Removals

- Removed `config set` and `config util`, merged the necessary commands together
- Removed `mod notice` commands because of its uselessness

## Changes

- Fixed descriptions of `music playback` commands
- Renamed `music playback play` to `music playback play_queue` to comply with the addition of `music playback play_stream`
- Corrected the behavior of `music playback play_queue` command [because of my dumb moment](https://github.com/angelobreuer/Lavalink4NET/issues/91)
- Renamed `music queue check` to `music queue view` and made the index starting from 0 to comply with `remove` and `remove_range` commands
- Renamed `music playback join` to `music playback coneect` and `music playback leave` to `music playback disconeect` to comply with Lavalink's convention
- Reworked on the behavior of `music queue add`: the user is able to queue multiple tracks from it
- Reworked on the behavior of `music playback play`: the user is able to request different types of players
- Reworked on the behavior of `music playback now_playing`: the response will adapt with the player type
- Reworked on the behavior of `music playback pause`, `music playback resume`: no longer need the `QueuedLavalinkPlayer`