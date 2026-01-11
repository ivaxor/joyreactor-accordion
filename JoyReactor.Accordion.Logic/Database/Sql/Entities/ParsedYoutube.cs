using JoyReactor.Accordion.Logic.ApiClient.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace JoyReactor.Accordion.Logic.Database.Sql.Entities;

public record ParsedYouTube : ISqlUpdatedAtEntity, IParsedAttributeEmbedded
{
    public ParsedYouTube() { }

    public ParsedYouTube(PostAttribute attribute)
    {
        Id = Guid.NewGuid();
        VideoId = attribute.Value;
        CreatedAt = DateTime.UtcNow;
        UpdatedAt = DateTime.UtcNow;
    }

    public Guid Id { get; set; }

    public string VideoId { get; set; }

    public virtual ParsedPostAttributeEmbedded PostAttributeEmbedded { get; set; }

    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
}

public class ParsedYouTubeEntityTypeConfiguration : IEntityTypeConfiguration<ParsedYouTube>
{
    public void Configure(EntityTypeBuilder<ParsedYouTube> builder)
    {
        builder
            .HasIndex(e => e.VideoId)
            .IsUnique();
        builder
            .Property(e => e.VideoId)
            .IsRequired(true);

        builder
            .HasOne(e => e.PostAttributeEmbedded)
            .WithOne(e => e.YouTube)
            .HasPrincipalKey<ParsedYouTube>(e => e.Id)
            .HasForeignKey<ParsedPostAttributeEmbedded>(e => e.YouTubeId)
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