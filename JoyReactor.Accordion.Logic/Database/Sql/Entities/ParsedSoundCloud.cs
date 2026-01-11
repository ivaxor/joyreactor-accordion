using JoyReactor.Accordion.Logic.ApiClient.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using System.Text.RegularExpressions;

namespace JoyReactor.Accordion.Logic.Database.Sql.Entities;

public partial record ParsedSoundCloud : ISqlUpdatedAtEntity, IParsedAttributeEmbedded
{
    [GeneratedRegex(@"(?<type>tracks|playlists)[\\/]+(?<id>\d+)", RegexOptions.Compiled | RegexOptions.IgnoreCase)]
    private static partial Regex UrlPathRegex();

    public ParsedSoundCloud() { }

    public ParsedSoundCloud(PostAttribute attribute)
    {
        Id = Guid.NewGuid();
        var match = UrlPathRegex().Match(attribute.Value);
        UrlPath = $"{match.Groups["type"].Value}/{match.Groups["id"].Value}";
        CreatedAt = DateTime.UtcNow;
        UpdatedAt = DateTime.UtcNow;
    }

    public Guid Id { get; set; }

    public string UrlPath { get; set; }

    public virtual ParsedPostAttributeEmbedded PostAttributeEmbedded { get; set; }

    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
}

public class ParsedSoundCloudEntityTypeConfiguration : IEntityTypeConfiguration<ParsedSoundCloud>
{
    public void Configure(EntityTypeBuilder<ParsedSoundCloud> builder)
    {
        builder
            .HasIndex(e => e.UrlPath)
            .IsUnique();
        builder
            .Property(e => e.UrlPath)
            .IsRequired(true);

        builder
            .HasOne(e => e.PostAttributeEmbedded)
            .WithOne(e => e.SoundCloud)
            .HasPrincipalKey<ParsedSoundCloud>(e => e.Id)
            .HasForeignKey<ParsedPostAttributeEmbedded>(e => e.SoundCloudId)
            .OnDelete(DeleteBehavior.Restrict)
            .IsRequired(false);

        builder
            .Property(e => e.CreatedAt)
            .IsRequired(true);
        builder
            .Property(e => e.UpdatedAt)
            .IsRequired(true);
    }
}