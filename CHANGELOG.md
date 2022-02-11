## New commands

- Added context menu commands for `osu` module
- Added `warnadd` and `warnremove` commands in `mod general` module for warning members
- Added `message` command group in `mod` module for sending messages to moderators

## Old commands reworks/removals

- Beatmap related parts of `osu` commands and context menus commands now include beatmap image as embed thumbnail
- Prompt for score count now includes range for ease in interactivity
- Moved "self host" button in `/info` one row higher

## Codebase changes (don't care if you don't plan to contribute the the source)
- `LiliaUtilities.GetDefaultEmbedTemplateForMember` now requires `DiscordUser`
- Redesigned whole `README.md`
