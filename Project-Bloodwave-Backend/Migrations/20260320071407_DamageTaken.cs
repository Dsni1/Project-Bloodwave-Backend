using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Project_Bloodwave_Backend.Migrations
{
    /// <inheritdoc />
    public partial class DamageTaken : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "damage_taken",
                table: "Matches",
                type: "int",
                nullable: false,
                defaultValue: 0);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "damage_taken",
                table: "Matches");
        }
    }
}
