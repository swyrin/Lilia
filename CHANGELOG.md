## Additions

- Add `activity` command to create Embedded Activity
- Add `music playback lyrics` command to get lyrics of currently playing track

## Removals

None

## Changes

- Added top.gg vote buttons in `bot` commands
- Extend the wait time of music inactivity from 2 minutes to 5 minutes
- Added basic Top.gg integrations
- Current client is now **sharded**
  - Which means, the startup time will now longer depending on your shard count
- Reworked on command registration
- Fixed playtime of `music playback now_playing` when on radio mode
- Renamed `config check` to `config view`
- Removed current bot restriction of `user` command
- Mentioned member list of `mod general kick` and `mod general ban` will now distinct
