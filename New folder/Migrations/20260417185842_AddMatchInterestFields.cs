using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace BlindMatchAPI.Migrations
{
    /// <inheritdoc />
    public partial class AddMatchInterestFields : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "Description",
                table: "ResearchAreas",
                type: "nvarchar(max)",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "Status",
                table: "Matches",
                type: "nvarchar(max)",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<bool>(
                name: "StudentInterested",
                table: "Matches",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<bool>(
                name: "SupervisorInterested",
                table: "Matches",
                type: "bit",
                nullable: false,
                defaultValue: false);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "Description",
                table: "ResearchAreas");

            migrationBuilder.DropColumn(
                name: "Status",
                table: "Matches");

            migrationBuilder.DropColumn(
                name: "StudentInterested",
                table: "Matches");

            migrationBuilder.DropColumn(
                name: "SupervisorInterested",
                table: "Matches");
        }
    }
}
