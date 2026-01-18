using JoyReactor.Accordion.Logic.Extensions;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace JoyReactor.Accordion.Logic.Database.Sql.Entities;

public record Api : ISqlEntity
{
    public Guid Id { get; set; }

    public string Name { get; set; }
    public string GraphQlEndpointUrl { get; set; }
    public string[] RootTagNames { get; set; }
}

public class ApiEntityTypeConfiguration : IEntityTypeConfiguration<Api>
{
    public void Configure(EntityTypeBuilder<Api> builder)
    {
        builder
            .HasIndex(e => e.Name)
            .IsUnique();

        builder
            .Property(e => e.Name)
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
                Name = "JoyReactor.cc",
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
                Name = "JoyReactor.com",
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