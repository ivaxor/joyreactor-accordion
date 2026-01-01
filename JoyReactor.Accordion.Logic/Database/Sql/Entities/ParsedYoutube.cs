using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace JoyReactor.Accordion.Logic.Database.Sql.Entities;

public record ParsedYoutube
{
    public Guid Id { get; set; }

    public string VideoId { get; set; }

    public Guid PostAttributeEmbededId { get; set; }
    public virtual ParsedPostAttributeEmbeded PostAttributeEmbeded { get; set; }

    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
}

public class ParsedYoutubeEntityTypeConfiguration : IEntityTypeConfiguration<ParsedYoutube>
{
    public void Configure(EntityTypeBuilder<ParsedYoutube> builder)
    {
        builder
            .HasIndex(e => e.VideoId)
            .IsUnique();
        builder
            .Property(e => e.VideoId)
            .IsRequired(true);

        builder
            .HasOne(e => e.PostAttributeEmbeded)
            .WithOne(e => e.YouTube)
            .HasPrincipalKey<ParsedYoutube>(e => e.PostAttributeEmbededId)
            .HasForeignKey<ParsedPostAttributeEmbeded>(e => e.YouTubeId)
            .OnDelete(DeleteBehavior.Cascade)
            .IsRequired(false);

        builder
            .Property(e => e.CreatedAt)
            .IsRequired(true);
        builder
            .Property(e => e.UpdatedAt)
            .IsRequired(true);
    }
}