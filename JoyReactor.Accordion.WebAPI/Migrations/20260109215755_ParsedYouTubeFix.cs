using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace JoyReactor.Accordion.Workers.Migrations
{
    /// <inheritdoc />
    public partial class ParsedYouTubeFix : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_ParsedPostAttributeEmbeds_ParsedYoutubes_YouTubeId",
                table: "ParsedPostAttributeEmbeds");

            migrationBuilder.DropPrimaryKey(
                name: "PK_ParsedYoutubes",
                table: "ParsedYoutubes");

            migrationBuilder.RenameTable(
                name: "ParsedYoutubes",
                newName: "ParsedYouTubes");

            migrationBuilder.RenameIndex(
                name: "IX_ParsedYoutubes_VideoId",
                table: "ParsedYouTubes",
                newName: "IX_ParsedYouTubes_VideoId");

            migrationBuilder.AddPrimaryKey(
                name: "PK_ParsedYouTubes",
                table: "ParsedYouTubes",
                column: "Id");

            migrationBuilder.AddForeignKey(
                name: "FK_ParsedPostAttributeEmbeds_ParsedYouTubes_YouTubeId",
                table: "ParsedPostAttributeEmbeds",
                column: "YouTubeId",
                principalTable: "ParsedYouTubes",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_ParsedPostAttributeEmbeds_ParsedYouTubes_YouTubeId",
                table: "ParsedPostAttributeEmbeds");

            migrationBuilder.DropPrimaryKey(
                name: "PK_ParsedYouTubes",
                table: "ParsedYouTubes");

            migrationBuilder.RenameTable(
                name: "ParsedYouTubes",
                newName: "ParsedYoutubes");

            migrationBuilder.RenameIndex(
                name: "IX_ParsedYouTubes_VideoId",
                table: "ParsedYoutubes",
                newName: "IX_ParsedYoutubes_VideoId");

            migrationBuilder.AddPrimaryKey(
                name: "PK_ParsedYoutubes",
                table: "ParsedYoutubes",
                column: "Id");

            migrationBuilder.AddForeignKey(
                name: "FK_ParsedPostAttributeEmbeds_ParsedYoutubes_YouTubeId",
                table: "ParsedPostAttributeEmbeds",
                column: "YouTubeId",
                principalTable: "ParsedYoutubes",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }
    }
}
