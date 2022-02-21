## Additions

- Added `add_playlist`, `clear`, `remove`, `remove_range` and `remove_dupe` in `music queue` command group

## Removals

- Removed `config set` and `config util`, merged the commands together

## Changes

- Corrected the behavior of `music playback play` command
- Renamed `music queue check` to `music queue view` and made the index starting from 0 to comply with `remove` and `remove_range` commands