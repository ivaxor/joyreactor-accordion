using JoyReactor.Accordion.Logic.ApiClient.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using System.Text.RegularExpressions;

namespace JoyReactor.Accordion.Logic.Database.Sql.Entities;

public partial record ParsedBandCamp : ISqlEntity, IParsedAttributeEmbeded
{
    [GeneratedRegex(@"(?<type>album|track)=(?<id>\d+)", RegexOptions.Compiled | RegexOptions.IgnoreCase)]
    private static partial Regex UrlPathRegex();

    public ParsedBandCamp() { }

    public ParsedBandCamp(PostAttribute attribute)
    {
        Id = Guid.NewGuid();
        var match = UrlPathRegex().Match(attribute.Value);
        UrlPath = $"{match.Groups["type"].Value}={match.Groups["id"].Value}";
        CreatedAt = DateTime.UtcNow;
        UpdatedAt = DateTime.UtcNow;
    }

    public Guid Id { get; set; }

    public string UrlPath { get; set; }

    public virtual ParsedPostAttributeEmbeded PostAttributeEmbeded { get; set; }

    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
}

public class ParsedBandCampEntityTypeConfiguration : IEntityTypeConfiguration<ParsedBandCamp>
{
    public void Configure(EntityTypeBuilder<ParsedBandCamp> builder)
    {
        builder
            .HasIndex(e => e.UrlPath)
            .IsUnique();
        builder
            .Property(e => e.UrlPath)
            .IsRequired(true);

        builder
            .HasOne(e => e.PostAttributeEmbeded)
            .WithOne(e => e.BandCamp)
            .HasPrincipalKey<ParsedBandCamp>(e => e.Id)
            .HasForeignKey<ParsedPostAttributeEmbeded>(e => e.BandCampId)
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