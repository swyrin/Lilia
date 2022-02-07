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
- Fixed critical bug in `ban` and `kick` when it only shows the last victim
- Added default reason for `ban` and `kick`
## Bot behaviors reworks/removals
- Separated `guild` and `global` command registrations
    - Reflected in JSON change
- Extended response wait time for `ban` and `kick` from 30 seconds to 5 minutes

# `release-0.0.2`
- `editpsa` command added
- `psa` command renamed to `sendpsa`
- Both `sendpsa` (formerly named `psa`) and `editpsa` now requires `Manage Server` permission
- Add option to run only in your **private** guilds
- Better error handling in `sendpsa` and `editpsa`

# `release-0.0.1`
First release so I guess nothing changes?