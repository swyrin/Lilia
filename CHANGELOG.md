> Woah, first official stable release

## New commands

Nothing, for now

## Old commands reworks/removals

- `osu` commands response reworked
  - Better readability on responses
  - Paginated response if you are requesting scores
  - String literals minor update
- `mod` commands reworked
  - `ban`, `kick`, `warnadd` and `warnremove` are merged into `mod general execute`
  - DM an embed to user instead of a wall-of-text

## Codebase changes (don't care if you don't plan to contribute to the source)

- Use official logs when dealing with database
- *Properly* reconfigured `Debug` and `Release`
- `Lilia` is now renamed as `Helya`
