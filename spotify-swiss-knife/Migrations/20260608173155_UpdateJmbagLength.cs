using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace spotify_swiss_knife.Migrations
{
    /// <inheritdoc />
    public partial class UpdateJmbagLength : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // Existing rows may still hold the old 13-digit JMBG values, which do not
            // fit varchar(10). They are a different ID scheme with no valid conversion,
            // so blank them out; users re-enter a valid JMBAG and the admin is restored
            // by IdentitySeeder. Without this the ALTER COLUMN fails with 22001.
            migrationBuilder.Sql(
                "UPDATE \"AspNetUsers\" SET \"JMBAG\" = '' WHERE length(\"JMBAG\") > 10;");

            migrationBuilder.AlterColumn<string>(
                name: "JMBAG",
                table: "AspNetUsers",
                type: "character varying(10)",
                maxLength: 10,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "character varying(13)",
                oldMaxLength: 13);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AlterColumn<string>(
                name: "JMBAG",
                table: "AspNetUsers",
                type: "character varying(13)",
                maxLength: 13,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "character varying(10)",
                oldMaxLength: 10);
        }
    }
}
