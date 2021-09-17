using Microsoft.EntityFrameworkCore.Migrations;

namespace Lilia.Migrations
{
    public partial class OsuModelUpdate : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropPrimaryKey(
                name: "PK_Users",
                table: "Users");

            migrationBuilder.DropPrimaryKey(
                name: "PK_Guilds",
                table: "Guilds");

            migrationBuilder.RenameColumn(
                name: "UserIndex",
                table: "Users",
                newName: "OsuMode");

            migrationBuilder.RenameColumn(
                name: "GuildIndex",
                table: "Guilds",
                newName: "DbGuildId");

            migrationBuilder.AlterColumn<ulong>(
                name: "UserId",
                table: "Users",
                type: "INTEGER",
                nullable: false,
                oldClrType: typeof(ulong),
                oldType: "INTEGER")
                .OldAnnotation("Sqlite:Autoincrement", true);

            migrationBuilder.AddColumn<ulong>(
                name: "DbUserId",
                table: "Users",
                type: "INTEGER",
                nullable: false,
                defaultValue: 0ul)
                .Annotation("Sqlite:Autoincrement", true);

            migrationBuilder.AddColumn<string>(
                name: "OsuUsername",
                table: "Users",
                type: "TEXT",
                nullable: true);

            migrationBuilder.AlterColumn<ulong>(
                name: "GuildId",
                table: "Guilds",
                type: "INTEGER",
                nullable: false,
                oldClrType: typeof(ulong),
                oldType: "INTEGER")
                .OldAnnotation("Sqlite:Autoincrement", true);

            migrationBuilder.AlterColumn<ulong>(
                name: "DbGuildId",
                table: "Guilds",
                type: "INTEGER",
                nullable: false,
                oldClrType: typeof(ulong),
                oldType: "INTEGER")
                .Annotation("Sqlite:Autoincrement", true);

            migrationBuilder.AddPrimaryKey(
                name: "PK_Users",
                table: "Users",
                column: "DbUserId");

            migrationBuilder.AddPrimaryKey(
                name: "PK_Guilds",
                table: "Guilds",
                column: "DbGuildId");
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropPrimaryKey(
                name: "PK_Users",
                table: "Users");

            migrationBuilder.DropPrimaryKey(
                name: "PK_Guilds",
                table: "Guilds");

            migrationBuilder.DropColumn(
                name: "DbUserId",
                table: "Users");

            migrationBuilder.DropColumn(
                name: "OsuUsername",
                table: "Users");

            migrationBuilder.RenameColumn(
                name: "OsuMode",
                table: "Users",
                newName: "UserIndex");

            migrationBuilder.RenameColumn(
                name: "DbGuildId",
                table: "Guilds",
                newName: "GuildIndex");

            migrationBuilder.AlterColumn<ulong>(
                name: "UserId",
                table: "Users",
                type: "INTEGER",
                nullable: false,
                oldClrType: typeof(ulong),
                oldType: "INTEGER")
                .Annotation("Sqlite:Autoincrement", true);

            migrationBuilder.AlterColumn<ulong>(
                name: "GuildId",
                table: "Guilds",
                type: "INTEGER",
                nullable: false,
                oldClrType: typeof(ulong),
                oldType: "INTEGER")
                .Annotation("Sqlite:Autoincrement", true);

            migrationBuilder.AlterColumn<ulong>(
                name: "GuildIndex",
                table: "Guilds",
                type: "INTEGER",
                nullable: false,
                oldClrType: typeof(ulong),
                oldType: "INTEGER")
                .OldAnnotation("Sqlite:Autoincrement", true);

            migrationBuilder.AddPrimaryKey(
                name: "PK_Users",
                table: "Users",
                column: "UserId");

            migrationBuilder.AddPrimaryKey(
                name: "PK_Guilds",
                table: "Guilds",
                column: "GuildId");
        }
    }
}
