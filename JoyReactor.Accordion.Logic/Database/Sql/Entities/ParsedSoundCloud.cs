using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace JoyReactor.Accordion.Logic.Database.Sql.Entities;

public record ParsedSoundCloud
{
    public Guid Id { get; set; }

    public string UrlPath { get; set; }

    public string? PostIds { get; set; }
    public string? PostAttributeIds { get; set; }
    public string? CommentIds { get; set; }
    public string? CommentAttributeIds { get; set; }

    public DateTimeOffset CreatedAt { get; set; }
    public DateTimeOffset UpdatedAt { get; set; }
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
            .Property(e => e.PostIds)
            .IsRequired(false);

        builder
            .Property(e => e.PostAttributeIds)
            .IsRequired(false);

        builder
            .Property(e => e.CommentIds)
            .IsRequired(false);

        builder
            .Property(e => e.CommentAttributeIds)
            .IsRequired(false);

        builder
            .Property(e => e.CreatedAt)
            .IsRequired(true);
        builder
            .Property(e => e.UpdatedAt)
            .IsRequired(true);
    }
}