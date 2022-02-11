## New commands
- Added context menu commands for `osu` module
- Added `warnadd` and `warnremove` in `mod general` module for warning members
- Added `message` command group in `mod` module for sending messages to moderators

## Old commands reworks/removals

- Beatmap related parts of osu! commands now include beatmap image as embed thumbnail
- Prompt for score count now includes range

## Codebase changes (don't care if you don't plan to contribute the the source)
- `LiliaUtilities.GetDefaultEmbedTemplateForMember` now requires `DiscordUser`
