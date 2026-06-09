using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace spotify_swiss_knife.Migrations
{
    /// <inheritdoc />
    public partial class AddScheduledShuffles : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "ScheduledShuffles",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    UserId = table.Column<string>(type: "text", nullable: false),
                    PlaylistId = table.Column<string>(type: "text", nullable: false),
                    PlaylistName = table.Column<string>(type: "text", nullable: false),
                    RandomnessLevel = table.Column<int>(type: "integer", nullable: false),
                    CronExpression = table.Column<string>(type: "text", nullable: false),
                    IsEnabled = table.Column<bool>(type: "boolean", nullable: false),
                    LastRunAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    NextRunAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    CreatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ScheduledShuffles", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "SpotifyTokens",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    SpotifyUserId = table.Column<string>(type: "text", nullable: false),
                    AccessToken = table.Column<string>(type: "text", nullable: false),
                    RefreshToken = table.Column<string>(type: "text", nullable: false),
                    ExpiresAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_SpotifyTokens", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_ScheduledShuffles_UserId",
                table: "ScheduledShuffles",
                column: "UserId");

            migrationBuilder.CreateIndex(
                name: "IX_SpotifyTokens_SpotifyUserId",
                table: "SpotifyTokens",
                column: "SpotifyUserId",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "ScheduledShuffles");

            migrationBuilder.DropTable(
                name: "SpotifyTokens");
        }
    }
}
