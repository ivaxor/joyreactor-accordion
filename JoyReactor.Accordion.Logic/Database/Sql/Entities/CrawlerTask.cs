namespace JoyReactor.Accordion.Logic.Database.Sql.Entities;

public record CrawlerTask
{
    public Guid Id { get; set; }
    public bool IsCompleted { get; set; }

    public int Tag { get; set; }

    public int? PageCurrent { get; set; }
    public PostLineType PostLineType { get; set; }
    public int? PageFrom { get; set; }
    public int? PageTo { get; set; }

    public DateTime? DateTimeFromUtc { get; set; }
    public DateTime? DateTimeToUtc { get; set; }

    public bool Nsfw { get; set; }
    public bool ExcludeSfw { get; set; }
    public bool ExcludeNsfw { get; set; }

    public DateTimeOffset CreatedAt { get; set; }
    public DateTimeOffset UpdatedAt { get; set; }
}

public enum PostLineType
{
    All,
    New,
    Good,
    Best,
}