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
            .Property(e => e.Id)
            .HasDefaultValueSql("gen_random_uuid()");

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
            new Config()
            {
                Id = 2.ToGuid(),
                Name = ConfigConstants.TelegramBotDuplicatePictureIdIndex,
                Value = 0.ToString(),
                CreatedAt = new DateTime(2026, 04, 19, 0, 0, 0, 0, DateTimeKind.Utc),
                UpdatedAt = new DateTime(2026, 04, 19, 0, 0, 0, 0, DateTimeKind.Utc),
            });
    }
}

public static class ConfigConstants
{
    public const string TelegramBotDuplicatePictureIdIndex = nameof(TelegramBotDuplicatePictureIdIndex);
}