using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Project_Bloodwave_Backend.Migrations
{
    /// <inheritdoc />
    public partial class RemovedWeaponsFromMatches : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "weapon1",
                table: "Matches");

            migrationBuilder.DropColumn(
                name: "weapon2",
                table: "Matches");

            migrationBuilder.DropColumn(
                name: "weapon3",
                table: "Matches");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "weapon1",
                table: "Matches",
                type: "varchar(255)",
                maxLength: 255,
                nullable: true)
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.AddColumn<string>(
                name: "weapon2",
                table: "Matches",
                type: "varchar(255)",
                maxLength: 255,
                nullable: true)
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.AddColumn<string>(
                name: "weapon3",
                table: "Matches",
                type: "varchar(255)",
                maxLength: 255,
                nullable: true)
                .Annotation("MySql:CharSet", "utf8mb4");
        }
    }
}
