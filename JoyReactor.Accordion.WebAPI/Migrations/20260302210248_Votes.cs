using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace JoyReactor.Accordion.Workers.Migrations
{
    /// <inheritdoc />
    public partial class Votes : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(@"
                CREATE OR REPLACE FUNCTION is_array_unique(anyarray)
                RETURNS boolean AS $$
                BEGIN
                    RETURN (SELECT COUNT(DISTINCT x) = cardinality($1) FROM unnest($1) AS x);
                END;
                $$ LANGUAGE plpgsql IMMUTABLE;
            ");

            migrationBuilder.CreateTable(
                name: "Configs",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    Name = table.Column<string>(type: "text", nullable: false),
                    Value = table.Column<string>(type: "text", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Configs", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "DuplicatePictureVotes",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    OriginalPictureId = table.Column<Guid>(type: "uuid", nullable: false),
                    DuplicatePictureId = table.Column<Guid>(type: "uuid", nullable: false),
                    YesVotes = table.Column<string[]>(type: "text[]", nullable: false),
                    NoVotes = table.Column<string[]>(type: "text[]", nullable: false),
                    VotingClosed = table.Column<bool>(type: "boolean", nullable: false, defaultValue: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_DuplicatePictureVotes", x => x.Id);
                    table.CheckConstraint("CK_DuplicatePictureVotes_NoVotes", "is_array_unique(\"NoVotes\")");
                    table.CheckConstraint("CK_DuplicatePictureVotes_YesVotes", "is_array_unique(\"YesVotes\")");
                    table.ForeignKey(
                        name: "FK_DuplicatePictureVotes_ParsedPostAttributePictures_Duplicate~",
                        column: x => x.DuplicatePictureId,
                        principalTable: "ParsedPostAttributePictures",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_DuplicatePictureVotes_ParsedPostAttributePictures_OriginalP~",
                        column: x => x.OriginalPictureId,
                        principalTable: "ParsedPostAttributePictures",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.InsertData(
                table: "Configs",
                columns: new[] { "Id", "CreatedAt", "Name", "UpdatedAt", "Value" },
                values: new object[] { new Guid("00000001-0000-0000-0000-000000000000"), new DateTime(2026, 3, 2, 0, 0, 0, 0, DateTimeKind.Utc), "DuplicatePictureIdIndex", new DateTime(2026, 3, 2, 0, 0, 0, 0, DateTimeKind.Utc), "" });

            migrationBuilder.CreateIndex(
                name: "IX_Configs_Name",
                table: "Configs",
                column: "Name",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_DuplicatePictureVotes_DuplicatePictureId",
                table: "DuplicatePictureVotes",
                column: "DuplicatePictureId");

            migrationBuilder.CreateIndex(
                name: "IX_DuplicatePictureVotes_OriginalPictureId",
                table: "DuplicatePictureVotes",
                column: "OriginalPictureId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "Configs");

            migrationBuilder.DropTable(
                name: "DuplicatePictureVotes");

            migrationBuilder.Sql("DROP FUNCTION IF EXISTS is_array_unique(anyarray);");
        }
    }
}
