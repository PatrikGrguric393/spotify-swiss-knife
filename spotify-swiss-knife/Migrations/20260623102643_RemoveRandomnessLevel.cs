using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace spotify_swiss_knife.Migrations
{
    /// <inheritdoc />
    public partial class RemoveRandomnessLevel : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "RandomnessLevel",
                table: "ScheduledShuffles");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "RandomnessLevel",
                table: "ScheduledShuffles",
                type: "integer",
                nullable: false,
                defaultValue: 0);
        }
    }
}
