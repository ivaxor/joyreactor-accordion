using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace JoyReactor.Accordion.Workers.Migrations;

/// <inheritdoc />
public partial class ParsedPostsFix : Migration
{
    /// <inheritdoc />
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropForeignKey(
            name: "FK_ParsedPostAttributeEmbeds_ParsedPost_PostId",
            table: "ParsedPostAttributeEmbeds");

        migrationBuilder.DropForeignKey(
            name: "FK_ParsedPostAttributePictures_ParsedPost_PostId",
            table: "ParsedPostAttributePictures");

        migrationBuilder.DropPrimaryKey(
            name: "PK_ParsedPost",
            table: "ParsedPost");

        migrationBuilder.RenameTable(
            name: "ParsedPost",
            newName: "ParsedPosts");

        migrationBuilder.RenameIndex(
            name: "IX_ParsedPost_NumberId",
            table: "ParsedPosts",
            newName: "IX_ParsedPosts_NumberId");

        migrationBuilder.AddPrimaryKey(
            name: "PK_ParsedPosts",
            table: "ParsedPosts",
            column: "Id");

        migrationBuilder.AddForeignKey(
            name: "FK_ParsedPostAttributeEmbeds_ParsedPosts_PostId",
            table: "ParsedPostAttributeEmbeds",
            column: "PostId",
            principalTable: "ParsedPosts",
            principalColumn: "Id",
            onDelete: ReferentialAction.Cascade);

        migrationBuilder.AddForeignKey(
            name: "FK_ParsedPostAttributePictures_ParsedPosts_PostId",
            table: "ParsedPostAttributePictures",
            column: "PostId",
            principalTable: "ParsedPosts",
            principalColumn: "Id",
            onDelete: ReferentialAction.Cascade);
    }

    /// <inheritdoc />
    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropForeignKey(
            name: "FK_ParsedPostAttributeEmbeds_ParsedPosts_PostId",
            table: "ParsedPostAttributeEmbeds");

        migrationBuilder.DropForeignKey(
            name: "FK_ParsedPostAttributePictures_ParsedPosts_PostId",
            table: "ParsedPostAttributePictures");

        migrationBuilder.DropPrimaryKey(
            name: "PK_ParsedPosts",
            table: "ParsedPosts");

        migrationBuilder.RenameTable(
            name: "ParsedPosts",
            newName: "ParsedPost");

        migrationBuilder.RenameIndex(
            name: "IX_ParsedPosts_NumberId",
            table: "ParsedPost",
            newName: "IX_ParsedPost_NumberId");

        migrationBuilder.AddPrimaryKey(
            name: "PK_ParsedPost",
            table: "ParsedPost",
            column: "Id");

        migrationBuilder.AddForeignKey(
            name: "FK_ParsedPostAttributeEmbeds_ParsedPost_PostId",
            table: "ParsedPostAttributeEmbeds",
            column: "PostId",
            principalTable: "ParsedPost",
            principalColumn: "Id",
            onDelete: ReferentialAction.Cascade);

        migrationBuilder.AddForeignKey(
            name: "FK_ParsedPostAttributePictures_ParsedPost_PostId",
            table: "ParsedPostAttributePictures",
            column: "PostId",
            principalTable: "ParsedPost",
            principalColumn: "Id",
            onDelete: ReferentialAction.Cascade);
    }
}
