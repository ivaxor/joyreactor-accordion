using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace JoyReactor.Accordion.Logic.Database.Sql.Entities;

public record DuplicatePictureVote : ISqlUpdatedAtEntity
{
    public Guid Id { get; set; }

    public Guid OriginalPictureId { get; set; }
    public virtual ParsedPostAttributePicture OriginalPicture { get; set; }

    public Guid DuplicatePictureId { get; set; }
    public virtual ParsedPostAttributePicture DuplicatePicture { get; set; }

    public string[] YesVotes { get; set; }
    public string[] NoVotes { get; set; }

    public bool VotingClosed { get; set; }

    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
}

public class DuplicatePictureVoteTypeConfiguration : IEntityTypeConfiguration<DuplicatePictureVote>
{
    public void Configure(EntityTypeBuilder<DuplicatePictureVote> builder)
    {
        builder
            .HasOne(e => e.OriginalPicture)
            .WithMany(e => e.VotesAsOriginal)
            .HasPrincipalKey(e => e.Id)
            .HasForeignKey(e => e.OriginalPictureId)
            .OnDelete(DeleteBehavior.Restrict)
            .IsRequired(false);

        builder
            .HasOne(e => e.DuplicatePicture)
            .WithMany(e => e.VotesAsDuplicate)
            .HasPrincipalKey(e => e.Id)
            .HasForeignKey(e => e.DuplicatePictureId)
            .OnDelete(DeleteBehavior.Restrict)
            .IsRequired(false);

        builder
            .Property(u => u.YesVotes)
            .HasColumnType("text[]")
            .IsRequired(true);

        builder
            .Property(e => e.NoVotes)
            .HasColumnType("text[]")
            .IsRequired();

        builder.ToTable($"{nameof(DuplicatePictureVote)}s", t =>
        {
            t.HasCheckConstraint(
                $"CK_{nameof(DuplicatePictureVote)}s_{nameof(DuplicatePictureVote.YesVotes)}",
                $"is_array_unique(\"{nameof(DuplicatePictureVote.YesVotes)}\")");

            t.HasCheckConstraint(
                $"CK_{nameof(DuplicatePictureVote)}s_{nameof(DuplicatePictureVote.NoVotes)}",
                $"is_array_unique(\"{nameof(DuplicatePictureVote.NoVotes)}\")");
        });

        builder
            .Property(e => e.VotingClosed)
            .HasDefaultValue(false)
            .IsRequired(true);

        builder
            .Property(e => e.CreatedAt)
            .IsRequired(true);

        builder
            .Property(e => e.UpdatedAt)
            .IsRequired(true);
    }
}