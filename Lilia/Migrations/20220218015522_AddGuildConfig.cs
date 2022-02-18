using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Lilia.Migrations
{
    public partial class AddGuildConfig : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<decimal>(
                name: "GoodbyeChannelId",
                table: "Guilds",
                type: "numeric(20,0)",
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<string>(
                name: "GoodbyeMessage",
                table: "Guilds",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "IsGoodbyeEnabled",
                table: "Guilds",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<bool>(
                name: "IsWelcomeEnabled",
                table: "Guilds",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<decimal>(
                name: "WelcomeChannelId",
                table: "Guilds",
                type: "numeric(20,0)",
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<string>(
                name: "WelcomeMessage",
                table: "Guilds",
                type: "text",
                nullable: true);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "GoodbyeChannelId",
                table: "Guilds");

            migrationBuilder.DropColumn(
                name: "GoodbyeMessage",
                table: "Guilds");

            migrationBuilder.DropColumn(
                name: "IsGoodbyeEnabled",
                table: "Guilds");

            migrationBuilder.DropColumn(
                name: "IsWelcomeEnabled",
                table: "Guilds");

            migrationBuilder.DropColumn(
                name: "WelcomeChannelId",
                table: "Guilds");

            migrationBuilder.DropColumn(
                name: "WelcomeMessage",
                table: "Guilds");
        }
    }
}
