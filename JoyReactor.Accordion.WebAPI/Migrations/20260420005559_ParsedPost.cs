using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace JoyReactor.Accordion.Workers.Migrations
{
    /// <inheritdoc />
    public partial class ParsedPost : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<bool>(
                name: "Nsfw",
                table: "ParsedPosts",
                type: "boolean",
                nullable: false,
                defaultValue: false);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "Nsfw",
                table: "ParsedPosts");
        }
    }
}
