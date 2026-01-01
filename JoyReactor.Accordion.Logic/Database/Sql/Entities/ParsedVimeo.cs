using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace JoyReactor.Accordion.Logic.Database.Sql.Entities;

public record ParsedVimeo
{
    public Guid Id { get; set; }

    public string VideoId { get; set; }

    public Guid PostAttributeEmbededId { get; set; }
    public virtual ParsedPostAttributeEmbeded PostAttributeEmbeded { get; set; }

    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
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
            .HasOne(e => e.PostAttributeEmbeded)
            .WithOne(e => e.Vimeo)
            .HasPrincipalKey<ParsedVimeo>(e => e.PostAttributeEmbededId)
            .HasForeignKey<ParsedPostAttributeEmbeded>(e => e.VimeoId)
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