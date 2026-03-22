using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace JoyReactor.Accordion.Workers.Migrations
{
    /// <inheritdoc />
    public partial class ParsedPostAttributePicture : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DeleteData(
                table: "Configs",
                keyColumn: "Id",
                keyValue: new Guid("00000001-0000-0000-0000-000000000000"));

            migrationBuilder.AddColumn<bool>(
                name: "IsVectorCheckedForDuplicates",
                table: "ParsedPostAttributePictures",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.CreateIndex(
                name: "IX_ParsedPostAttributePictures_IsVectorCheckedForDuplicates",
                table: "ParsedPostAttributePictures",
                column: "IsVectorCheckedForDuplicates");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_ParsedPostAttributePictures_IsVectorCheckedForDuplicates",
                table: "ParsedPostAttributePictures");

            migrationBuilder.DropColumn(
                name: "IsVectorCheckedForDuplicates",
                table: "ParsedPostAttributePictures");

            migrationBuilder.InsertData(
                table: "Configs",
                columns: new[] { "Id", "CreatedAt", "Name", "UpdatedAt", "Value" },
                values: new object[] { new Guid("00000001-0000-0000-0000-000000000000"), new DateTime(2026, 3, 2, 0, 0, 0, 0, DateTimeKind.Utc), "DuplicatePictureIdIndex", new DateTime(2026, 3, 2, 0, 0, 0, 0, DateTimeKind.Utc), "" });
        }
    }
}
