# Lilia
Welcome to Lilia repository, where I commit horrible quality codes.
Yeah, this is just a learning project, why not?

# Self-hosting guide
> This assumes you have installed [.NET Core SDK](https://dotnet.microsoft.com/download). I don't know if the Runtime one could work?

### Step 1: Grab the files
You can either use the [downloading way](https://github.com/Swyreee/Lilia/archive/refs/heads/master.zip) or the `git` way:
```cmd
git clone https://github.com/Swyreee/Lilia.git
```
then head to the directory where `.csproj` file exists, we will use this until the end of the guide:
```cmd
cd your-path-containing-csproj-file-here
```
### Step 2: Making configuration files
Create an `.env` file with the following content:
```ini
DISCORD_TOKEN=your-discord-token
DB_PASSWORD=your-db-password-here
```
### Step 3: Doing stuffs
Execute this command to install prequistes:
```bat
dotnet restore
```
### Step 4: We are done!!!
Just run this one then enjoy
```bat
dotnet run
```