using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Lilia.Migrations
{
    public partial class Sanitize : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "Shards",
                table: "Users");

            migrationBuilder.DropColumn(
                name: "GuildId",
                table: "Guilds");

            migrationBuilder.RenameColumn(
                name: "UserId",
                table: "Users",
                newName: "DiscordUserId");

            migrationBuilder.RenameColumn(
                name: "Ranking",
                table: "Guilds",
                newName: "DiscordGuildId");

            migrationBuilder.AddColumn<string>(
                name: "Queue",
                table: "Guilds",
                type: "TEXT",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "QueueWithNames",
                table: "Guilds",
                type: "TEXT",
                nullable: true);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "Queue",
                table: "Guilds");

            migrationBuilder.DropColumn(
                name: "QueueWithNames",
                table: "Guilds");

            migrationBuilder.RenameColumn(
                name: "DiscordUserId",
                table: "Users",
                newName: "UserId");

            migrationBuilder.RenameColumn(
                name: "DiscordGuildId",
                table: "Guilds",
                newName: "Ranking");

            migrationBuilder.AddColumn<ulong>(
                name: "Shards",
                table: "Users",
                type: "INTEGER",
                nullable: false,
                defaultValue: 0ul);

            migrationBuilder.AddColumn<ulong>(
                name: "GuildId",
                table: "Guilds",
                type: "INTEGER",
                nullable: false,
                defaultValue: 0ul);
        }
    }
}
