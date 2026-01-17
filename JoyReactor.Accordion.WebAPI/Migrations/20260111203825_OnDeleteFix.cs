using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace JoyReactor.Accordion.Workers.Migrations;

/// <inheritdoc />
public partial class OnDeleteFix : Migration
{
    /// <inheritdoc />
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropForeignKey(
            name: "FK_CrawlerTasks_ParsedTags_TagId",
            table: "CrawlerTasks");

        migrationBuilder.DropForeignKey(
            name: "FK_ParsedPostAttributeEmbeds_ParsedBandCamps_BandCampId",
            table: "ParsedPostAttributeEmbeds");

        migrationBuilder.DropForeignKey(
            name: "FK_ParsedPostAttributeEmbeds_ParsedCoubs_CoubId",
            table: "ParsedPostAttributeEmbeds");

        migrationBuilder.DropForeignKey(
            name: "FK_ParsedPostAttributeEmbeds_ParsedSoundClouds_SoundCloudId",
            table: "ParsedPostAttributeEmbeds");

        migrationBuilder.DropForeignKey(
            name: "FK_ParsedPostAttributeEmbeds_ParsedVimeos_VimeoId",
            table: "ParsedPostAttributeEmbeds");

        migrationBuilder.DropForeignKey(
            name: "FK_ParsedPostAttributeEmbeds_ParsedYouTubes_YouTubeId",
            table: "ParsedPostAttributeEmbeds");

        migrationBuilder.DropForeignKey(
            name: "FK_ParsedTags_ParsedTags_MainTagId",
            table: "ParsedTags");

        migrationBuilder.DropForeignKey(
            name: "FK_ParsedTags_ParsedTags_ParentId",
            table: "ParsedTags");

        migrationBuilder.AddForeignKey(
            name: "FK_CrawlerTasks_ParsedTags_TagId",
            table: "CrawlerTasks",
            column: "TagId",
            principalTable: "ParsedTags",
            principalColumn: "Id",
            onDelete: ReferentialAction.Restrict);

        migrationBuilder.AddForeignKey(
            name: "FK_ParsedPostAttributeEmbeds_ParsedBandCamps_BandCampId",
            table: "ParsedPostAttributeEmbeds",
            column: "BandCampId",
            principalTable: "ParsedBandCamps",
            principalColumn: "Id",
            onDelete: ReferentialAction.Restrict);

        migrationBuilder.AddForeignKey(
            name: "FK_ParsedPostAttributeEmbeds_ParsedCoubs_CoubId",
            table: "ParsedPostAttributeEmbeds",
            column: "CoubId",
            principalTable: "ParsedCoubs",
            principalColumn: "Id",
            onDelete: ReferentialAction.Restrict);

        migrationBuilder.AddForeignKey(
            name: "FK_ParsedPostAttributeEmbeds_ParsedSoundClouds_SoundCloudId",
            table: "ParsedPostAttributeEmbeds",
            column: "SoundCloudId",
            principalTable: "ParsedSoundClouds",
            principalColumn: "Id",
            onDelete: ReferentialAction.Restrict);

        migrationBuilder.AddForeignKey(
            name: "FK_ParsedPostAttributeEmbeds_ParsedVimeos_VimeoId",
            table: "ParsedPostAttributeEmbeds",
            column: "VimeoId",
            principalTable: "ParsedVimeos",
            principalColumn: "Id",
            onDelete: ReferentialAction.Restrict);

        migrationBuilder.AddForeignKey(
            name: "FK_ParsedPostAttributeEmbeds_ParsedYouTubes_YouTubeId",
            table: "ParsedPostAttributeEmbeds",
            column: "YouTubeId",
            principalTable: "ParsedYouTubes",
            principalColumn: "Id",
            onDelete: ReferentialAction.Restrict);

        migrationBuilder.AddForeignKey(
            name: "FK_ParsedTags_ParsedTags_MainTagId",
            table: "ParsedTags",
            column: "MainTagId",
            principalTable: "ParsedTags",
            principalColumn: "Id",
            onDelete: ReferentialAction.Restrict);

        migrationBuilder.AddForeignKey(
            name: "FK_ParsedTags_ParsedTags_ParentId",
            table: "ParsedTags",
            column: "ParentId",
            principalTable: "ParsedTags",
            principalColumn: "Id",
            onDelete: ReferentialAction.Restrict);
    }

    /// <inheritdoc />
    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropForeignKey(
            name: "FK_CrawlerTasks_ParsedTags_TagId",
            table: "CrawlerTasks");

        migrationBuilder.DropForeignKey(
            name: "FK_ParsedPostAttributeEmbeds_ParsedBandCamps_BandCampId",
            table: "ParsedPostAttributeEmbeds");

        migrationBuilder.DropForeignKey(
            name: "FK_ParsedPostAttributeEmbeds_ParsedCoubs_CoubId",
            table: "ParsedPostAttributeEmbeds");

        migrationBuilder.DropForeignKey(
            name: "FK_ParsedPostAttributeEmbeds_ParsedSoundClouds_SoundCloudId",
            table: "ParsedPostAttributeEmbeds");

        migrationBuilder.DropForeignKey(
            name: "FK_ParsedPostAttributeEmbeds_ParsedVimeos_VimeoId",
            table: "ParsedPostAttributeEmbeds");

        migrationBuilder.DropForeignKey(
            name: "FK_ParsedPostAttributeEmbeds_ParsedYouTubes_YouTubeId",
            table: "ParsedPostAttributeEmbeds");

        migrationBuilder.DropForeignKey(
            name: "FK_ParsedTags_ParsedTags_MainTagId",
            table: "ParsedTags");

        migrationBuilder.DropForeignKey(
            name: "FK_ParsedTags_ParsedTags_ParentId",
            table: "ParsedTags");

        migrationBuilder.AddForeignKey(
            name: "FK_CrawlerTasks_ParsedTags_TagId",
            table: "CrawlerTasks",
            column: "TagId",
            principalTable: "ParsedTags",
            principalColumn: "Id",
            onDelete: ReferentialAction.Cascade);

        migrationBuilder.AddForeignKey(
            name: "FK_ParsedPostAttributeEmbeds_ParsedBandCamps_BandCampId",
            table: "ParsedPostAttributeEmbeds",
            column: "BandCampId",
            principalTable: "ParsedBandCamps",
            principalColumn: "Id",
            onDelete: ReferentialAction.Cascade);

        migrationBuilder.AddForeignKey(
            name: "FK_ParsedPostAttributeEmbeds_ParsedCoubs_CoubId",
            table: "ParsedPostAttributeEmbeds",
            column: "CoubId",
            principalTable: "ParsedCoubs",
            principalColumn: "Id",
            onDelete: ReferentialAction.Cascade);

        migrationBuilder.AddForeignKey(
            name: "FK_ParsedPostAttributeEmbeds_ParsedSoundClouds_SoundCloudId",
            table: "ParsedPostAttributeEmbeds",
            column: "SoundCloudId",
            principalTable: "ParsedSoundClouds",
            principalColumn: "Id",
            onDelete: ReferentialAction.Cascade);

        migrationBuilder.AddForeignKey(
            name: "FK_ParsedPostAttributeEmbeds_ParsedVimeos_VimeoId",
            table: "ParsedPostAttributeEmbeds",
            column: "VimeoId",
            principalTable: "ParsedVimeos",
            principalColumn: "Id",
            onDelete: ReferentialAction.Cascade);

        migrationBuilder.AddForeignKey(
            name: "FK_ParsedPostAttributeEmbeds_ParsedYouTubes_YouTubeId",
            table: "ParsedPostAttributeEmbeds",
            column: "YouTubeId",
            principalTable: "ParsedYouTubes",
            principalColumn: "Id",
            onDelete: ReferentialAction.Cascade);

        migrationBuilder.AddForeignKey(
            name: "FK_ParsedTags_ParsedTags_MainTagId",
            table: "ParsedTags",
            column: "MainTagId",
            principalTable: "ParsedTags",
            principalColumn: "Id",
            onDelete: ReferentialAction.Cascade);

        migrationBuilder.AddForeignKey(
            name: "FK_ParsedTags_ParsedTags_ParentId",
            table: "ParsedTags",
            column: "ParentId",
            principalTable: "ParsedTags",
            principalColumn: "Id",
            onDelete: ReferentialAction.Cascade);
    }
}
