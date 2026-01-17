using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace JoyReactor.Accordion.Workers.Migrations;

/// <inheritdoc />
public partial class ParsedPostAttributeEmbeddedFix : Migration
{
    /// <inheritdoc />
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropIndex(
            name: "IX_ParsedPostAttributeEmbeds_PostId_BandCampId_CoubId_SoundClo~",
            table: "ParsedPostAttributeEmbeds");

        migrationBuilder.CreateIndex(
            name: "IX_ParsedPostAttributeEmbeds_PostId",
            table: "ParsedPostAttributeEmbeds",
            column: "PostId");
    }

    /// <inheritdoc />
    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropIndex(
            name: "IX_ParsedPostAttributeEmbeds_PostId",
            table: "ParsedPostAttributeEmbeds");

        migrationBuilder.CreateIndex(
            name: "IX_ParsedPostAttributeEmbeds_PostId_BandCampId_CoubId_SoundClo~",
            table: "ParsedPostAttributeEmbeds",
            columns: new[] { "PostId", "BandCampId", "CoubId", "SoundCloudId", "VimeoId", "YouTubeId" },
            unique: true);
    }
}
