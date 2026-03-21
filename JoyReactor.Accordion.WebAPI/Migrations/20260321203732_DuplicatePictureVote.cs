using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace JoyReactor.Accordion.Workers.Migrations
{
    /// <inheritdoc />
    public partial class DuplicatePictureVote : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "Score",
                table: "DuplicatePictureVotes");

            migrationBuilder.CreateIndex(
                name: "IX_DuplicatePictureVotes_OriginalPictureId_DuplicatePictureId",
                table: "DuplicatePictureVotes",
                columns: new[] { "OriginalPictureId", "DuplicatePictureId" },
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_DuplicatePictureVotes_OriginalPictureId_DuplicatePictureId",
                table: "DuplicatePictureVotes");

            migrationBuilder.AddColumn<float>(
                name: "Score",
                table: "DuplicatePictureVotes",
                type: "real",
                nullable: false,
                defaultValue: 0f);
        }
    }
}
