# :tada: Welcome to `Lilia` repository

Lilia is a Discord bot created as my hobby, so I can't really categorize it on a specific place, so general-purposed maybe?

This project undergoes continuous development so *please* don't expect everything here to work perfectly.

| Build                                                                              | Release                                                                              | Code inspection (CodeQL)                                                                    |
|------------------------------------------------------------------------------------|--------------------------------------------------------------------------------------|---------------------------------------------------------------------------------------------|
| ![Build](https://github.com/Swyreee/Lilia/actions/workflows/dotnet.yml/badge.svg)  | ![Release](https://github.com/Swyreee/Lilia/actions/workflows/release.yml/badge.svg) | ![CodeQL](https://github.com/Swyreee/Lilia/actions/workflows/codeql-analysis.yml/badge.svg) |

# :computer: Self-hosting guide

## Step 0: Prerequisites
- A Discord application in your account
  - Go [here](https://discord.com/developers/applications) and remember to login
  - Click the `New Application` button on the top right, give it a name
  - Go to the `Bot` tab, click `Add bot` button at the right side and click `Yes, do it!`
    - (Optional) Change the bot profile - it won't affect the `General Information` tab
  - Note the `Token` and store it somewhere else
  - Choose `Message Content Intent` and `Server Member Intent`
    - (Optional) Uncheck `Public Bot` - that will make bot invite link useless
- A host machine, either your PC or VPS 

## Step 1: Grab the executable

Head to [releases page](https://github.com/Swyreee/Lilia/releases), download `app.zip` from the latest release then
unzip it. Everything should be inside `app` directory. Then open the folder matching your OS.

## Step 2: Configurations

- Create new file naming `config.json` **at the same place with the executable file**
- Then paste the following template and watch [here](https://github.com/Swyreee/Lilia/wiki/Configuration-101) for helps on filling data

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

## Step 3: Run the bot

- Just simply run it like how you run apps on your OS:
    - Windows: `Lilia.exe`
    - Linux: `./Lilia`
    - MacOS: I don't have a Mac to test but I think `./Lilia` should work
### Where are the commands?

Well, the bot runs under [Discord's "Application Command"](https://discord.com/blog/slash-commands-are-here) so just
simply type `/` on the message box or Right Click if you are on PC or Web version of Discord.

# Is there any roadmap?

I don't plan to make this project popular, so probably nothing for now.