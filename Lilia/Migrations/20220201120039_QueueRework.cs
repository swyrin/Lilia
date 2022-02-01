using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Lilia.Migrations
{
    public partial class QueueRework : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "Queue",
                table: "Guilds");

            migrationBuilder.RenameColumn(
                name: "QueueWithNames",
                table: "Guilds",
                newName: "QueueItem");
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.RenameColumn(
                name: "QueueItem",
                table: "Guilds",
                newName: "QueueWithNames");

            migrationBuilder.AddColumn<string>(
                name: "Queue",
                table: "Guilds",
                type: "TEXT",
                nullable: true);
        }
    }
}
