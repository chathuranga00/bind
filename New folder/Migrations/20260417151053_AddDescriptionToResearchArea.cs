using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace BlindMatchAPI.Migrations
{
    /// <inheritdoc />
    public partial class AddDescriptionToResearchArea : Migration
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
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "Description",
                table: "ResearchAreas");
        }
    }
}
