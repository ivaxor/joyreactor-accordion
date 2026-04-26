using JoyReactor.Accordion.Logic.Database.Vector.Entities;
using JoyReactor.Accordion.Logic.Extensions;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace JoyReactor.Accordion.Logic.Database.Sql.Entities;

public record DuplicatePictureVote : ISqlUpdatedAtEntity
{
    public DuplicatePictureVote() { }

    public DuplicatePictureVote(PictureRetrivedPoint original, PictureScoredPoint duplicate)
    {
        Id = Guid.NewGuid();
        OriginalPictureId = original.PostAttributeId.Value.ToGuid();
        DuplicatePictureId = duplicate.PostAttributeId.Value.ToGuid();
        YesVotes = [];
        NoVotes = [];
        CreatedAt = DateTime.UtcNow;
        UpdatedAt = DateTime.UtcNow;
    }

    public DuplicatePictureVote(PictureScoredPoint original, PictureRetrivedPoint duplicate)
    {
        Id = Guid.NewGuid();
        OriginalPictureId = original.PostAttributeId.Value.ToGuid();
        DuplicatePictureId = duplicate.PostAttributeId.Value.ToGuid();
        YesVotes = [];
        NoVotes = [];
        CreatedAt = DateTime.UtcNow;
        UpdatedAt = DateTime.UtcNow;
    }

    public Guid Id { get; set; }

    public Guid OriginalPictureId { get; set; }
    public virtual ParsedPostAttributePicture OriginalPicture { get; set; }

    public Guid DuplicatePictureId { get; set; }
    public virtual ParsedPostAttributePicture DuplicatePicture { get; set; }

    public string[] YesVotes { get; set; }
    public string[] NoVotes { get; set; }

    public bool VotingClosed { get; set; }
    public bool SentViaTelegram { get; set; }

    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
}

public class DuplicatePictureVoteTypeConfiguration : IEntityTypeConfiguration<DuplicatePictureVote>
{
    public void Configure(EntityTypeBuilder<DuplicatePictureVote> builder)
    {
        builder
            .Property(e => e.Id)
            .HasDefaultValueSql("gen_random_uuid()");

        builder
            .HasOne(e => e.OriginalPicture)
            .WithMany(e => e.VotesAsOriginal)
            .HasPrincipalKey(e => e.Id)
            .HasForeignKey(e => e.OriginalPictureId)
            .OnDelete(DeleteBehavior.Cascade);

        builder
            .HasOne(e => e.DuplicatePicture)
            .WithMany(e => e.VotesAsDuplicate)
            .HasPrincipalKey(e => e.Id)
            .HasForeignKey(e => e.DuplicatePictureId)
            .OnDelete(DeleteBehavior.Cascade);

        builder
            .HasIndex(e => new { e.OriginalPictureId, e.DuplicatePictureId })
            .IsUnique();

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
            .Property(e => e.SentViaTelegram)
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

public record DuplicatePictureVoteExtended : DuplicatePictureVote
{
    public DuplicatePictureVoteExtended() { }

    public DuplicatePictureVoteExtended(
        DuplicatePictureVote baseVote,
        string originalHostName,
        int originalPostId,
        int originalCount,
        string duplicateHostName,
        int duplicatePostId,
        int duplicateCount,
        bool nsfw) : base(baseVote)
    {
        OriginalHostName = originalHostName;
        OriginalPostNumberId = originalPostId;
        OriginalPostPictureCount = originalCount;

        DuplicateHostName = duplicateHostName;
        DuplicatePostNumberId = duplicatePostId;
        DuplicatePostPictureCount = duplicateCount;

        Nsfw = nsfw;
    }

    public string OriginalHostName { get; set; }
    public int OriginalPostNumberId { get; set; }
    public int OriginalPostPictureCount { get; set; }

    public string DuplicateHostName { get; set; }
    public int DuplicatePostNumberId { get; set; }
    public int DuplicatePostPictureCount { get; set; }

    public bool Nsfw { get; set; }
}