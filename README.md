# Welcome to `Lilia` repo
Lilia is a multi-purpose bot for Discord, I guess that's enough for an introduction.

| Build (Windows)                                                                              | Build (macOS)                                                                            | Build (Linux)                                                                                 |
|----------------------------------------------------------------------------------------------|------------------------------------------------------------------------------------------|-----------------------------------------------------------------------------------------------|
| ![WinBuild](https://github.com/Swyreee/Lilia/actions/workflows/dotnet_windows.yml/badge.svg) | ![MacBuild](https://github.com/Swyreee/Lilia/actions/workflows/dotnet_mac.yml/badge.svg) | ![LinuxBuild](https://github.com/Swyreee/Lilia/actions/workflows/dotnet_ubuntu.yml/badge.svg) |

| Code inspection (CodeQL)                                                                    | Release build (All platforms)                                                        |
|---------------------------------------------------------------------------------------------|--------------------------------------------------------------------------------------|
| ![CodeQL](https://github.com/Swyreee/Lilia/actions/workflows/codeql-analysis.yml/badge.svg) | ![Release](https://github.com/Swyreee/Lilia/actions/workflows/release.yml/badge.svg) |

# Changelogs
- All stable versions: Check [here](https://github.com/Swyreee/Lilia/blob/master/CHANGELOGS.md)
- In development version: Check [here](https://github.com/Swyreee/Lilia/blob/master/CHANGELOG.md)

# Self-hosting guide
### Step 0: Prerequisites
>You *might* need administrator privileges in your machine - `sudo` in Linux distros (maybe Mac), `Administrator` user in Windows
- [.NET](https://dotnet.microsoft.com/download), version 6 or higher.

### Step 1: Grab the files
Head to [releases page](https://github.com/Swyreee/Lilia/releases), download `app.zip` at the top release then unzip it. Everything should be inside `app/` directory

### Step 2: Configurations
- Create the file `config.json` with the following template
>If you are really lazy to do some clicks, follow step 3 first then go back here
```json
{
  "client": {
    "private_guilds": [],
    "slash_commands_for_global": true,
    "activity": {
      "type": "Watching",
      "name": "you",
      "status": "DoNotDisturb"
    }
  },
  "credentials": {
    "discord_token": "",
    "db_password": "",
    "osu": {
      "client_id": 0,
      "client_secret": ""
    }
  }
}
```
- See [here](https://github.com/Swyreee/Lilia/wiki/Configuration-101) for help on filling stuffs.

### Step 3: Run the bot
```shell
dotnet ./bin/Release/net6.0/Lilia.dll
```

# So... Where are the commands?
Well, the bot runs under [Discord's "Application Command"](https://discord.com/blog/slash-commands-are-here) so just simply type `/` on the message box or Right Click if you are on PC or Web version of Discord.

# Are there any roadmaps?
Maybe get some reputations first.