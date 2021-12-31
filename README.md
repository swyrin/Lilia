# Welcome to `Lilia` repo
Lilia is a multi-purpose bot for Discord, I guess that's enough for an introduction.  

# Changelogs
soon:tm:

# Self-hosting guide
### Step 0: Prerequisites
>You *might* need administrator privileges in your machine - `sudo` in Linux distros (maybe Mac), `Administrator` user in Windows 

- [.NET](https://dotnet.microsoft.com/download), version 6 or higher.
- [Java](https://www.java.com/en/download/) for [Lavalink](https://github.com/freyacodes/Lavalink). **JDK** 13 or higher. Install either on server or your local machine.
- [Git](https://git-scm.com/), easier to update. Optional.

### Step 1: Grab the files
>#### 1. With `git`
>Run the following command in your favorite terminal:
>```shell
>git clone https://github.com/Swyreee/Lilia.git
>```
>If you need to update the bot (remember to shutdown the running instance first):
>```shell
>git pull origin master
>```

>#### 2. Without `git`
>There is a green-ish button calling `Code`, click it, there should be an option. `Download ZIP` if I recall correctly.
> 
>And **every time** you want to update the bot, go to this repo again, do the same thing. Remember to backup your stuffs.

Whether you have `git` or not, head to the directory **containing `Lilia.csproj` file** with `cd` command.
```shell
cd <somewhere-else-with-that-file>
```
We will use this directory until the end of the guide.

### Step 2: Configurations
- Rename the file `config.example.json` to `config.json` (or copy if you wish) and fill with appropriate stuffs. Watch [here](https://github.com/Swyreee/Lilia/wiki/Configuration-101) for helps.
- For `lavalink` part, remember to [run the server first](https://github.com/freyacodes/Lavalink#server-configuration) **before running the bot**.

### Step 3: Update dependencies
>You should run this every time you **update** the bot.
```shell
dotnet nuget add source https://nuget.emzi0767.com/api/v3/index.json
dotnet restore
```

### Step 4: Run the bot
>Known issue: https://github.com/dotnet/sdk/issues/10164
```shell
dotnet build -c Release --no-restore
dotnet ./bin/Release/net6.0/Lilia.dll
```