using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace JoyReactor.Accordion.Workers.Migrations
{
    /// <inheritdoc />
    public partial class TelegramBotDuplicatePictureIdIndex : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.InsertData(
                table: "Configs",
                columns: new[] { "Id", "CreatedAt", "Name", "UpdatedAt", "Value" },
                values: new object[] { new Guid("00000002-0000-0000-0000-000000000000"), new DateTime(2026, 4, 19, 0, 0, 0, 0, DateTimeKind.Utc), "TelegramBotDuplicatePictureIdIndex", new DateTime(2026, 4, 19, 0, 0, 0, 0, DateTimeKind.Utc), "0" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DeleteData(
                table: "Configs",
                keyColumn: "Id",
                keyValue: new Guid("00000002-0000-0000-0000-000000000000"));
        }
    }
}
