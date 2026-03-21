using JoyReactor.Accordion.Logic.ApiClient.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace JoyReactor.Accordion.Logic.Database.Sql.Entities;

public record ParsedYouube : ISqlUpdatedAtEntity, IParsedAttributeEmbedded
{
    public ParsedYouube() { }

    public ParsedYouube(PostAttribute attribute)
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

public class ParsedYouTubeEntityTypeConfiguration : IEntityTypeConfiguration<ParsedYouube>
{
    public void Configure(EntityTypeBuilder<ParsedYouube> builder)
    {
        builder
            .Property(e => e.Id)
            .ValueGeneratedOnAdd();

        builder
            .HasIndex(e => e.VideoId)
            .IsUnique();
        builder
            .Property(e => e.VideoId)
            .IsRequired(true);

        builder
            .HasOne(e => e.PostAttributeEmbedded)
            .WithOne(e => e.YouTube)
            .HasPrincipalKey<ParsedYouube>(e => e.Id)
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