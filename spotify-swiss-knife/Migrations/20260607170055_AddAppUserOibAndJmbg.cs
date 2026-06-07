using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace spotify_swiss_knife.Migrations
{
    /// <inheritdoc />
    public partial class AddAppUserOibAndJmbg : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "JMBG",
                table: "AspNetUsers",
                type: "character varying(13)",
                maxLength: 13,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "OIB",
                table: "AspNetUsers",
                type: "character varying(11)",
                maxLength: 11,
                nullable: false,
                defaultValue: "");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "JMBG",
                table: "AspNetUsers");

            migrationBuilder.DropColumn(
                name: "OIB",
                table: "AspNetUsers");
        }
    }
}
