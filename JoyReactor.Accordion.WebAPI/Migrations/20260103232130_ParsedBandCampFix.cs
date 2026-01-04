using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace JoyReactor.Accordion.Workers.Migrations
{
    /// <inheritdoc />
    public partial class ParsedBandCampFix : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.RenameColumn(
                name: "AlbumId",
                table: "ParsedBandCamps",
                newName: "UrlPath");

            migrationBuilder.RenameIndex(
                name: "IX_ParsedBandCamps_AlbumId",
                table: "ParsedBandCamps",
                newName: "IX_ParsedBandCamps_UrlPath");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.RenameColumn(
                name: "UrlPath",
                table: "ParsedBandCamps",
                newName: "AlbumId");

            migrationBuilder.RenameIndex(
                name: "IX_ParsedBandCamps_UrlPath",
                table: "ParsedBandCamps",
                newName: "IX_ParsedBandCamps_AlbumId");
        }
    }
}
