using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace JoyReactor.Accordion.Workers.Migrations
{
    /// <inheritdoc />
    public partial class ParsedPostAttributePicture2 : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateIndex(
                name: "IX_ParsedPostAttributePictures_AttributeId",
                table: "ParsedPostAttributePictures",
                column: "AttributeId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_ParsedPostAttributePictures_AttributeId",
                table: "ParsedPostAttributePictures");
        }
    }
}
