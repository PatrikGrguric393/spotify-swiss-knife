using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

#pragma warning disable CA1814 // Prefer jagged arrays over multidimensional

namespace spotify_swiss_knife.Migrations
{
    /// <inheritdoc />
    public partial class RemoveTrackImages : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "TrackImages");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "TrackImages",
                columns: table => new
                {
                    TrackId = table.Column<string>(type: "text", nullable: false),
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    Height = table.Column<int>(type: "integer", nullable: true),
                    Url = table.Column<string>(type: "text", nullable: false),
                    Width = table.Column<int>(type: "integer", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_TrackImages", x => new { x.TrackId, x.Id });
                    table.ForeignKey(
                        name: "FK_TrackImages_Tracks_TrackId",
                        column: x => x.TrackId,
                        principalTable: "Tracks",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.InsertData(
                table: "TrackImages",
                columns: new[] { "Id", "TrackId", "Height", "Url", "Width" },
                values: new object[,]
                {
                    { 1, "track-gravity-bloom", 640, "https://images.example.com/tracks/gravity-bloom.jpg", 640 },
                    { 1, "track-midnight-circuit", 640, "https://images.example.com/tracks/midnight-circuit-640.jpg", 640 },
                    { 2, "track-midnight-circuit", 300, "https://images.example.com/tracks/midnight-circuit-300.jpg", 300 },
                    { 1, "track-river-in-binary", null, "https://images.example.com/tracks/river-in-binary.jpg", null },
                    { 1, "track-solar-echo", 640, "https://images.example.com/tracks/solar-echo.jpg", 640 },
                    { 2, "track-solar-echo", 300, "https://images.example.com/tracks/solar-echo-square.jpg", 300 },
                    { 1, "track-static-sunrise", 512, "https://images.example.com/tracks/static-sunrise.jpg", 512 }
                });
        }
    }
}
