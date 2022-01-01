# Welcome to `Lilia` repo
Lilia is a multi-purpose bot for Discord, I guess that's enough for an introduction.

![CodeQL](https://github.com/Swyreee/Lilia/actions/workflows/codeql-analysis.yml/badge.svg)
![Build](https://github.com/Swyreee/Lilia/actions/workflows/dotnet.yml/badge.svg)
![Release](https://github.com/Swyreee/Lilia/actions/workflows/release.yml/badge.svg)

# Changelogs
Watch [here](ttps://github.com/Swyreee/Lilia/blob/master/CHANGELOGS.md)

# Self-hosting guide
### Step 0: Prerequisites
>You *might* need administrator privileges in your machine - `sudo` in Linux distros (maybe Mac), `Administrator` user in Windows 

- [.NET](https://dotnet.microsoft.com/download), version 6 or higher.
- [Java](https://www.java.com/en/download/) for [Lavalink](https://github.com/freyacodes/Lavalink). **JDK** 13 or higher. Install either on server or your local machine.

### Step 1: Grab the files
Head to [releases page](https://github.com/Swyreee/Lilia/releases), download `app.zip` at the top release then unzip it. Everything should be at `app/` directory

### Step 2: Configurations
- Create the file `config.json` with the following template
```json
{
  "client": {
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
  },
  "lavalink": {
    "host": "localhost",
    "port": 2333,
    "password": "youshallnotpass"
  }
}
```
- See [here](https://github.com/Swyreee/Lilia/wiki/Configuration-101) for help on filling stuffs.
- For `lavalink` part, remember to [run the server first](https://github.com/freyacodes/Lavalink#server-configuration) **before running the bot**.

### Step 3: Run the bot
```shell
dotnet ./bin/Release/net6.0/Lilia.dll
```