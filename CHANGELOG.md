# Writer's note
Please note that this file only lists the latest changes. If you need a history, go visit [`CHANGELOGS.md`](https://github.com/Swyreee/Lilia/blob/master/CHANGELOGS.md)

# What's new in `release-1.0.0`???
> Woah, first major update :tada:
## New commands
- Added `uptime` command to check bot uptime
- Added `changes` command to see the changelogs
- Added `copynotice` command to copy a notification
## Old commands reworks/removals
- Removed `music` commands because sanity cost is on the moon now
  - Maybe developed again in future?
- Changed `psa` to `notice`
  - Also changed `sendpsa` to `notice`
- Fixed critical behavior issue in `ban` and `kick` when it only shows the last victim
- Added default reason for `ban` and `kick`
## Bot behaviors reworks/removals
- Separated `guild` and `global` command registrations
  - Reflected in JSON change
- Extended response wait time for `ban` and `kick` from 30 seconds to 5 minutes