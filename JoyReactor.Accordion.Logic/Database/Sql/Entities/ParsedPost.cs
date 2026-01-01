using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace JoyReactor.Accordion.Logic.Database.Sql.Entities;

public record ParsedPost
{
    public Guid Id { get; set; }

    public int NumberId { get; set; }
    public int ContentVersion { get; set; }

    public virtual ParsedPostAttributePicture[] AttributePictures { get; set; }
    public virtual ParsedPostAttributeEmbeded[] AttributeEmbeds { get; set; }

    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
}

public class ParsedPostEntityTypeConfiguration : IEntityTypeConfiguration<ParsedPost>
{
    public void Configure(EntityTypeBuilder<ParsedPost> builder)
    {
        builder
            .HasIndex(e => e.NumberId)
            .IsUnique();
        builder
            .Property(e => e.NumberId)
            .IsRequired(true);

        builder
            .Property(e => e.ContentVersion)
            .IsRequired(true);

        builder
            .Property(e => e.CreatedAt)
            .IsRequired(true);
        builder
            .Property(e => e.UpdatedAt)
            .IsRequired(true);
    }
}