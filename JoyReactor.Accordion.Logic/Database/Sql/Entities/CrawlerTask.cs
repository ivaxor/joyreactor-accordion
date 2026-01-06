using JoyReactor.Accordion.Logic.ApiClient.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace JoyReactor.Accordion.Logic.Database.Sql.Entities;

public record CrawlerTask : ISqlEntity
{
    public Guid Id { get; set; }

    public Guid TagId { get; set; }
    public virtual ParsedTag? Tag { get; set; }

    public PostLineType PostLineType { get; set; }
    public int? PageFrom { get; set; }
    public int? PageTo { get; set; }
    public int? PageCurrent { get; set; }

    public bool IsIndefinite { get; set; }
    public bool IsCompleted { get; set; }
    public DateTime? StartedAt { get; set; }
    public DateTime? FinishedAt { get; set; }

    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
}

public class CrawlerTaskEntityTypeConfiguration : IEntityTypeConfiguration<CrawlerTask>
{
    public void Configure(EntityTypeBuilder<CrawlerTask> builder)
    {


        builder
            .HasOne(e => e.Tag)
            .WithMany(e => e.CrawlerTasks)
            .HasPrincipalKey(e => e.Id)
            .HasForeignKey(e => e.TagId)
            .OnDelete(DeleteBehavior.Cascade);

        builder
            .Property(e => e.PostLineType)
            .IsRequired(true);

        builder
            .Property(e => e.PageFrom)
            .IsRequired(false);

        builder
            .Property(e => e.PageTo)
            .IsRequired(false);

        builder
            .Property(e => e.PageCurrent)
            .IsRequired(false);

        builder
            .Property(e => e.IsIndefinite)
            .IsRequired(true);

        builder
            .Property(e => e.IsCompleted)
            .IsRequired(true);

        builder
           .Property(e => e.StartedAt)
           .IsRequired(false);

        builder
           .Property(e => e.FinishedAt)
           .IsRequired(false);

        builder
            .Property(e => e.CreatedAt)
            .IsRequired(true);

        builder
            .Property(e => e.UpdatedAt)
            .IsRequired(true);
    }
}