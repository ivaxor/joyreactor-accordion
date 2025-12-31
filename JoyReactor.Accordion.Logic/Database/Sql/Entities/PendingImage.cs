using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace JoyReactor.Accordion.Logic.Database.Sql.Entities;

public record PendingImage
{
    public Guid Id { get; set; }

    public string FilePath { get; set; }
    public bool IsCompleted { get; set; }

    public int ImageId { get; set; }
    public int? PostId { get; set; }
    public int? PostAttributeId { get; set; }
    public int? CommentId { get; set; }
    public int? CommentAttributeId { get; set; }

    public DateTimeOffset CreatedAt { get; set; }
    public DateTimeOffset UpdatedAt { get; set; }
}

public class PendingImageEntityTypeConfiguration : IEntityTypeConfiguration<PendingImage>
{
    public void Configure(EntityTypeBuilder<PendingImage> builder)
    {
        builder
            .Property(e => e.FilePath)
            .IsRequired(true);

        builder
            .HasIndex(e => e.IsCompleted);
        builder
            .Property(e => e.IsCompleted)
            .IsRequired(true);

        builder
            .HasIndex(e => e.ImageId)
            .IsUnique();
        builder
            .Property(e => e.ImageId)
            .IsRequired(true);

        builder
            .Property(e => e.PostId)
            .IsRequired(false);

        builder
            .Property(e => e.PostAttributeId)
            .IsRequired(false);

        builder
            .Property(e => e.CommentId)
            .IsRequired(false);

        builder
            .Property(e => e.CommentAttributeId)
            .IsRequired(false);

        builder
            .Property(e => e.CreatedAt)
            .IsRequired(true);
        builder
            .Property(e => e.UpdatedAt)
            .IsRequired(true);
    }
}