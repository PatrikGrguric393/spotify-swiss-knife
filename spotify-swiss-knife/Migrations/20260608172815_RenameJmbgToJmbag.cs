using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace spotify_swiss_knife.Migrations
{
    /// <inheritdoc />
    public partial class RenameJmbgToJmbag : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.RenameColumn(
                name: "JMBG",
                table: "AspNetUsers",
                newName: "JMBAG");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.RenameColumn(
                name: "JMBAG",
                table: "AspNetUsers",
                newName: "JMBG");
        }
    }
}
