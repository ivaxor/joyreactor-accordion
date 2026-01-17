using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace JoyReactor.Accordion.Workers.Migrations;

/// <inheritdoc />
public partial class ParsedPostAttributePictureFix2 : Migration
{
    /// <inheritdoc />
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.AlterColumn<bool>(
            name: "IsVectorCreated",
            table: "ParsedPostAttributePictures",
            type: "boolean",
            nullable: false,
            defaultValue: false,
            oldClrType: typeof(bool),
            oldType: "boolean");

        migrationBuilder.AddColumn<bool>(
            name: "NoContent",
            table: "ParsedPostAttributePictures",
            type: "boolean",
            nullable: false,
            defaultValue: false);

        migrationBuilder.AddColumn<bool>(
            name: "UnsupportedContent",
            table: "ParsedPostAttributePictures",
            type: "boolean",
            nullable: false,
            defaultValue: false);

        migrationBuilder.CreateIndex(
            name: "IX_ParsedPostAttributePictures_NoContent",
            table: "ParsedPostAttributePictures",
            column: "NoContent");

        migrationBuilder.CreateIndex(
            name: "IX_ParsedPostAttributePictures_UnsupportedContent",
            table: "ParsedPostAttributePictures",
            column: "UnsupportedContent");
    }

    /// <inheritdoc />
    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropIndex(
            name: "IX_ParsedPostAttributePictures_NoContent",
            table: "ParsedPostAttributePictures");

        migrationBuilder.DropIndex(
            name: "IX_ParsedPostAttributePictures_UnsupportedContent",
            table: "ParsedPostAttributePictures");

        migrationBuilder.DropColumn(
            name: "NoContent",
            table: "ParsedPostAttributePictures");

        migrationBuilder.DropColumn(
            name: "UnsupportedContent",
            table: "ParsedPostAttributePictures");

        migrationBuilder.AlterColumn<bool>(
            name: "IsVectorCreated",
            table: "ParsedPostAttributePictures",
            type: "boolean",
            nullable: false,
            oldClrType: typeof(bool),
            oldType: "boolean",
            oldDefaultValue: false);
    }
}
