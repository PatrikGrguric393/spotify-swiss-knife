using System.Collections.Generic;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace spotify_swiss_knife.Migrations
{
    /// <inheritdoc />
    public partial class MultiPlaylistScheduledShuffles : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // Add the new array columns as nullable, copy each existing single playlist into a
            // one-element array, then enforce NOT NULL and drop the old scalar columns. This
            // preserves existing schedules instead of wiping them (the default scaffold dropped
            // first and re-added non-null, which loses data).
            migrationBuilder.AddColumn<List<string>>(
                name: "PlaylistIds",
                table: "ScheduledShuffles",
                type: "text[]",
                nullable: true);

            migrationBuilder.AddColumn<List<string>>(
                name: "PlaylistNames",
                table: "ScheduledShuffles",
                type: "text[]",
                nullable: true);

            migrationBuilder.Sql(@"
                UPDATE ""ScheduledShuffles""
                SET ""PlaylistIds"" = ARRAY[""PlaylistId""],
                    ""PlaylistNames"" = ARRAY[""PlaylistName""];");

            migrationBuilder.AlterColumn<List<string>>(
                name: "PlaylistIds",
                table: "ScheduledShuffles",
                type: "text[]",
                nullable: false,
                oldClrType: typeof(List<string>),
                oldType: "text[]",
                oldNullable: true);

            migrationBuilder.AlterColumn<List<string>>(
                name: "PlaylistNames",
                table: "ScheduledShuffles",
                type: "text[]",
                nullable: false,
                oldClrType: typeof(List<string>),
                oldType: "text[]",
                oldNullable: true);

            migrationBuilder.DropColumn(
                name: "PlaylistId",
                table: "ScheduledShuffles");

            migrationBuilder.DropColumn(
                name: "PlaylistName",
                table: "ScheduledShuffles");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            // Restore the single-playlist columns, taking the first element of each array.
            // Schedules that targeted multiple playlists keep only their first one.
            migrationBuilder.AddColumn<string>(
                name: "PlaylistId",
                table: "ScheduledShuffles",
                type: "text",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "PlaylistName",
                table: "ScheduledShuffles",
                type: "text",
                nullable: false,
                defaultValue: "");

            migrationBuilder.Sql(@"
                UPDATE ""ScheduledShuffles""
                SET ""PlaylistId"" = COALESCE(""PlaylistIds""[1], ''),
                    ""PlaylistName"" = COALESCE(""PlaylistNames""[1], '');");

            migrationBuilder.DropColumn(
                name: "PlaylistIds",
                table: "ScheduledShuffles");

            migrationBuilder.DropColumn(
                name: "PlaylistNames",
                table: "ScheduledShuffles");
        }
    }
}
