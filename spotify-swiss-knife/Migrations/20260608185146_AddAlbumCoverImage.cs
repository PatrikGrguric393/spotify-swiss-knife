using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace spotify_swiss_knife.Migrations
{
    /// <inheritdoc />
    public partial class AddAlbumCoverImage : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "CoverImageContentType",
                table: "Albums",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "CoverImageFileName",
                table: "Albums",
                type: "text",
                nullable: true);

            migrationBuilder.UpdateData(
                table: "Albums",
                keyColumn: "Id",
                keyValue: "album-cloud-garden-ep",
                columns: new[] { "CoverImageContentType", "CoverImageFileName" },
                values: new object[] { null, null });

            migrationBuilder.UpdateData(
                table: "Albums",
                keyColumn: "Id",
                keyValue: "album-feather-and-noise",
                columns: new[] { "CoverImageContentType", "CoverImageFileName" },
                values: new object[] { null, null });

            migrationBuilder.UpdateData(
                table: "Albums",
                keyColumn: "Id",
                keyValue: "album-lunar-protocol",
                columns: new[] { "CoverImageContentType", "CoverImageFileName" },
                values: new object[] { null, null });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "CoverImageContentType",
                table: "Albums");

            migrationBuilder.DropColumn(
                name: "CoverImageFileName",
                table: "Albums");
        }
    }
}
