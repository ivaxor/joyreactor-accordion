using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace JoyReactor.Accordion.Workers.Migrations
{
    /// <inheritdoc />
    public partial class ParsedPostAttributePictures : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<bool>(
                name: "NoContentDueToDns",
                table: "ParsedPostAttributePictures",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.CreateIndex(
                name: "IX_ParsedPostAttributePictures_NoContentDueToDns",
                table: "ParsedPostAttributePictures",
                column: "NoContentDueToDns");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_ParsedPostAttributePictures_NoContentDueToDns",
                table: "ParsedPostAttributePictures");

            migrationBuilder.DropColumn(
                name: "NoContentDueToDns",
                table: "ParsedPostAttributePictures");
        }
    }
}
