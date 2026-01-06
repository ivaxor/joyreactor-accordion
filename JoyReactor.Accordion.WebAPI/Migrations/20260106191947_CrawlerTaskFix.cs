using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace JoyReactor.Accordion.Workers.Migrations
{
    /// <inheritdoc />
    public partial class CrawlerTaskFix : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "Type",
                table: "CrawlerTasks");

            migrationBuilder.AddColumn<bool>(
                name: "IsIndefinite",
                table: "CrawlerTasks",
                type: "boolean",
                nullable: false,
                defaultValue: false);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "IsIndefinite",
                table: "CrawlerTasks");

            migrationBuilder.AddColumn<int>(
                name: "Type",
                table: "CrawlerTasks",
                type: "integer",
                nullable: false,
                defaultValue: 0);
        }
    }
}
