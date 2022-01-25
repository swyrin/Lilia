using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Lilia.Migrations
{
    public partial class AddIsPlayingState : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<bool>(
                name: "IsPlaying",
                table: "Guilds",
                type: "INTEGER",
                nullable: false,
                defaultValue: false);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "IsPlaying",
                table: "Guilds");
        }
    }
}
