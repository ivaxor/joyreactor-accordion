using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace JoyReactor.Accordion.Logic.Database.Sql.Entities;

public record ParsedBandCamp
{
    public Guid Id { get; set; }

    public string AlbumId { get; set; }

    public Guid PostAttributeEmbededId { get; set; }
    public virtual ParsedPostAttributeEmbeded PostAttributeEmbeded { get; set; }

    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
}

public class ParsedBandCampEntityTypeConfiguration : IEntityTypeConfiguration<ParsedBandCamp>
{
    public void Configure(EntityTypeBuilder<ParsedBandCamp> builder)
    {
        builder
            .HasIndex(e => e.AlbumId)
            .IsUnique();
        builder
            .Property(e => e.AlbumId)
            .IsRequired(true);

        builder
            .HasOne(e => e.PostAttributeEmbeded)
            .WithOne(e => e.BandCamp)
            .HasPrincipalKey<ParsedBandCamp>(e => e.PostAttributeEmbededId)
            .HasForeignKey<ParsedPostAttributeEmbeded>(e => e.BandCampId)
            .OnDelete(DeleteBehavior.Cascade);

        builder
            .Property(e => e.CreatedAt)
            .IsRequired(true);
        builder
            .Property(e => e.UpdatedAt)
            .IsRequired(true);
    }
}