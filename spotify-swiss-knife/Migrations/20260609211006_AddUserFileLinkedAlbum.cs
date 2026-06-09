using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace spotify_swiss_knife.Migrations
{
    /// <inheritdoc />
    public partial class AddUserFileLinkedAlbum : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "LinkedAlbumId",
                table: "UserFiles",
                type: "text",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_UserFiles_LinkedAlbumId",
                table: "UserFiles",
                column: "LinkedAlbumId");

            migrationBuilder.AddForeignKey(
                name: "FK_UserFiles_Albums_LinkedAlbumId",
                table: "UserFiles",
                column: "LinkedAlbumId",
                principalTable: "Albums",
                principalColumn: "Id",
                onDelete: ReferentialAction.SetNull);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_UserFiles_Albums_LinkedAlbumId",
                table: "UserFiles");

            migrationBuilder.DropIndex(
                name: "IX_UserFiles_LinkedAlbumId",
                table: "UserFiles");

            migrationBuilder.DropColumn(
                name: "LinkedAlbumId",
                table: "UserFiles");
        }
    }
}
