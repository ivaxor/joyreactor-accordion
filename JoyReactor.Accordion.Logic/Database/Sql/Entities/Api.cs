using JoyReactor.Accordion.Logic.Extensions;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace JoyReactor.Accordion.Logic.Database.Sql.Entities;

public record Api : ISqlEntity
{
    public Guid Id { get; set; }

    public int Priority { get; set; }
    public string HostName { get; set; }
    public string GraphQlEndpointUrl { get; set; }
    public string[] RootTagNames { get; set; }

    public virtual ICollection<ParsedTag>? Tags { get; set; }
    public virtual ICollection<EmptyTag>? EmptyTags { get; set; }
    public virtual ICollection<ParsedPost>? Posts { get; set; }
}

public class ApiEntityTypeConfiguration : IEntityTypeConfiguration<Api>
{
    public void Configure(EntityTypeBuilder<Api> builder)
    {
        builder
            .HasMany(e => e.Tags)
            .WithOne(e => e.Api)
            .HasPrincipalKey(e => e.Id)
            .HasForeignKey(e => e.ApiId)
            .OnDelete(DeleteBehavior.Restrict);

        builder
            .HasMany(e => e.EmptyTags)
            .WithOne(e => e.Api)
            .HasPrincipalKey(e => e.Id)
            .HasForeignKey(e => e.ApiId)
            .OnDelete(DeleteBehavior.Restrict);

        builder
            .HasMany(e => e.Posts)
            .WithOne(e => e.Api)
            .HasPrincipalKey(e => e.Id)
            .HasForeignKey(e => e.ApiId)
            .OnDelete(DeleteBehavior.Restrict);

        builder
            .HasIndex(e => e.Priority)
            .IsUnique();

        builder
            .Property(e => e.Priority)
            .IsRequired(true);

        builder
            .HasIndex(e => e.HostName)
            .IsUnique();

        builder
            .Property(e => e.HostName)
            .IsRequired(true);

        builder
            .HasIndex(e => e.GraphQlEndpointUrl)
            .IsUnique();

        builder
            .Property(e => e.GraphQlEndpointUrl)
            .IsRequired(true);

        builder
            .Property(e => e.RootTagNames)
            .IsRequired(true);

        builder.HasData(
            new Api
            {
                Id = 1.ToGuid(),
                Priority = 0,
                HostName = "joyreactor.cc",
                GraphQlEndpointUrl = "https://api.joyreactor.cc/graphql",
                RootTagNames = [
                    "общее",
                    "Комиксы",
                    "гифки",
                    "art",
                    "песочница",
                    "котэ",
                    "story",
                    "geek",
                    "видео",
                    "фэндомы",
                    "Эротика",
                    "Игры",
                    "anon",
                    "политика",
                    "разное",
                    "секретные разделы",
                    "artist",
                    "Мемы",
                    "Азиатка",
                    "Porn Model",
                    "cosplay",
                ],
            },
            new Api
            {
                Id = 2.ToGuid(),
                Priority = -1,
                HostName = "joyreactor.com",
                GraphQlEndpointUrl = "https://api.joyreactor.com/graphql",
                RootTagNames = [
                    "general",
                    "comics",
                    "gif",
                    "art",
                    "sandbox",
                    "cats",
                    "story",
                    "geek",
                    "video",
                    "fandoms",
                    "erotic",
                    "games",
                    "anon",
                    "politics",
                    // разное (?)
                    // секретные разделы (xxx-file -> fandoms)
                    "artist",
                    "memes",
                    "asian girl",
                    "Porn Model",
                    "cosplay",
                ],
            });
    }
}