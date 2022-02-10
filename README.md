# Welcome to `Lilia` repo
Lilia is a multi-purpose bot for Discord, I guess that's enough for an introduction.

This project undergoes heavy development 99% of the time so *please* don't expect everything here will work perfectly, especially the database part.

| Build                                                                              | Release                                                                              | Code inspection (CodeQL)                                                                    |
|------------------------------------------------------------------------------------|--------------------------------------------------------------------------------------|---------------------------------------------------------------------------------------------|
| ![Build](https://github.com/Swyreee/Lilia/actions/workflows/dotnet.yml/badge.svg)  | ![Release](https://github.com/Swyreee/Lilia/actions/workflows/release.yml/badge.svg) | ![CodeQL](https://github.com/Swyreee/Lilia/actions/workflows/codeql-analysis.yml/badge.svg) |

# Self-hosting guide
## Step 1: Grab the files
Head to [releases page](https://github.com/Swyreee/Lilia/releases), download `app.zip` at the top release then unzip it. Everything should be inside `app/` directory

## Step 2: Configurations
- Create the file `config.json` with the following template
```json
{
  "client": {
    "private_guilds": [],
    "bot_invite_link": "",
    "support_guild_invite_link": "",
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

## Step 3: Run the bot
- Pick a folder with your desired OS inside `app`
- Just simply run it like how you run other apps

# So... Where are the commands?
Well, the bot runs under [Discord's "Application Command"](https://discord.com/blog/slash-commands-are-here) so just simply type `/` on the message box or Right Click if you are on PC or Web version of Discord.

# Are there any roadmaps?
Maybe get some reputations first.