## New commands

- Added `info` command to check bot information
- Added `refresh` command to refresh commands

## Old commands reworks/removals

- Removed `changes` commands
- Created `mod` command category
    - Moved `kick`, `ban` into `mod general`
    - Moved `notice`, `copynotice` and `editnotice` into `mod notice`
        - Changed the name of the commands:
            - `notice` -> `mod notice send`
            - `copynotice` -> `mod notice copy`
            - `editnotice` -> `mod notice edit`
    - `mod notice send` (formerly `notice`) now gives you the jump link to the notice message
    - `mod notice copy` (formerly `copynotice`) now gives you the jump link to the linked message
    - Both of the commands listed above this now requires message jump link instead of message ID(s)

## Codebase changes (don't care if you don't plan to contribute the the source)

- Squashed the database
- Sanitized the codebase
- Reworked on the build, release and code analysis system
- Add tests for `LiliaUtilities.cs`