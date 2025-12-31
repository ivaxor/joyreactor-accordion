using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace JoyReactor.Accordion.Logic.Database.Sql.Entities;

public record ParsedVimeo
{
    public Guid Id { get; set; }

    public string VideoId { get; set; }

    public string? PostIds { get; set; }
    public string? PostAttributeIds { get; set; }
    public string? CommentIds { get; set; }
    public string? CommentAttributeIds { get; set; }

    public DateTimeOffset CreatedAt { get; set; }
    public DateTimeOffset UpdatedAt { get; set; }
}

public class ParsedVimeoEntityTypeConfiguration : IEntityTypeConfiguration<ParsedVimeo>
{
    public void Configure(EntityTypeBuilder<ParsedVimeo> builder)
    {
        builder
            .HasIndex(e => e.VideoId)
            .IsUnique();
        builder
            .Property(e => e.VideoId)
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