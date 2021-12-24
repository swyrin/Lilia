# Welcome to `Lilia` repo
Welcome to Lilia repository, where I commit horrible quality codes.

Yeah, this is just a learning project, why not?

# Self-hosting guide
### Step 0: Prequistes
> Linux users *might* need to prepend `sudo`

- [.NET](https://dotnet.microsoft.com/download). Version 6 or higher. I don't know if the Runtime one could work?
- [Java](https://www.java.com/en/download/) for Lavalink. **JDK** 11 or higher. Install either on server or your local machine.
- [Git](https://git-scm.com/), easier to update.

### Step 1: Grab the files
Run the following command in your favorite terminal
```shell
git clone https://github.com/Swyreee/Lilia.git
```
then head to the directory containing`Lilia.csproj`, we will use this until the end of the guide:

(should be `.\Lilia\Lilia` in case I am wrong)
```shell
cd your-path-containing-csproj-file-here
```

In case we need to update the bot:
```shell
git pull
```

### Step 2: Configurations
- Rename `config.example.json` file to `config.json` and fill with appropriate contents. Everything else is self-explanatory.
- For `lavalink` part, remember to [run the server first](https://github.com/freyacodes/Lavalink#server-configuration) **before running the bot**. Your local machine is fine.
  - It is advisable that you should only change the hostname in the `config.json` file.

### Step 3: Install dependencies and build the project
```shell
dotnet restore
dotnet build
```

### Step 4: Run the bot
- For Windows users, use `dotnet run` is enough
```shell
dotnet run
```
- But for Linux users, I can't find a way after suffering with SEGVs for a long time.
- Remember to replace `6.0` with your currently installed .NET version
```shell
dotnet .\bin\Debug\net6.0\Lilia.dll
```