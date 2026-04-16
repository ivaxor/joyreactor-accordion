using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace JoyReactor.Accordion.Workers.Migrations
{
    /// <inheritdoc />
    public partial class DuplicatePictureVote2 : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_DuplicatePictureVotes_ParsedPostAttributePictures_Duplicate~",
                table: "DuplicatePictureVotes");

            migrationBuilder.DropForeignKey(
                name: "FK_DuplicatePictureVotes_ParsedPostAttributePictures_OriginalP~",
                table: "DuplicatePictureVotes");

            migrationBuilder.AddForeignKey(
                name: "FK_DuplicatePictureVotes_ParsedPostAttributePictures_Duplicate~",
                table: "DuplicatePictureVotes",
                column: "DuplicatePictureId",
                principalTable: "ParsedPostAttributePictures",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_DuplicatePictureVotes_ParsedPostAttributePictures_OriginalP~",
                table: "DuplicatePictureVotes",
                column: "OriginalPictureId",
                principalTable: "ParsedPostAttributePictures",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_DuplicatePictureVotes_ParsedPostAttributePictures_Duplicate~",
                table: "DuplicatePictureVotes");

            migrationBuilder.DropForeignKey(
                name: "FK_DuplicatePictureVotes_ParsedPostAttributePictures_OriginalP~",
                table: "DuplicatePictureVotes");

            migrationBuilder.AddForeignKey(
                name: "FK_DuplicatePictureVotes_ParsedPostAttributePictures_Duplicate~",
                table: "DuplicatePictureVotes",
                column: "DuplicatePictureId",
                principalTable: "ParsedPostAttributePictures",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_DuplicatePictureVotes_ParsedPostAttributePictures_OriginalP~",
                table: "DuplicatePictureVotes",
                column: "OriginalPictureId",
                principalTable: "ParsedPostAttributePictures",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }
    }
}
