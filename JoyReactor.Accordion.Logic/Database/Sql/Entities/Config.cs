using JoyReactor.Accordion.Logic.Extensions;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace JoyReactor.Accordion.Logic.Database.Sql.Entities;

public record Config : ISqlUpdatedAtEntity
{
    public Guid Id { get; set; }

    public string Name { get; set; }
    public string Value { get; set; }

    public DateTime UpdatedAt { get; set; }
    public DateTime CreatedAt { get; set; }
}

public class ConfigTypeConfiguration : IEntityTypeConfiguration<Config>
{
    public void Configure(EntityTypeBuilder<Config> builder)
    {
        builder
            .HasIndex(e => e.Name)
            .IsUnique();
        builder
            .Property(e => e.Name)
            .IsRequired(true);

        builder
            .Property(e => e.Value)
            .IsRequired(true);

        builder
            .Property(e => e.CreatedAt)
            .IsRequired(true);

        builder
            .Property(e => e.UpdatedAt)
            .IsRequired(true);

        builder.HasData(
           new Config
           {
               Id = 1.ToGuid(),
               Name = ConfigConstants.DuplicatePictureIdIndex,
               Value = "",
               CreatedAt = new DateTime(2026, 03, 02, 0, 0, 0, DateTimeKind.Utc),
               UpdatedAt = new DateTime(2026, 03, 02, 0, 0, 0, DateTimeKind.Utc),
           });
    }
}

public static class ConfigConstants
{
    public const string DuplicatePictureIdIndex = nameof(DuplicatePictureIdIndex);
}