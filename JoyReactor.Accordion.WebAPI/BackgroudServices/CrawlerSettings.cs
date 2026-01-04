namespace JoyReactor.Accordion.WebAPI.BackgroudServices;

public record CrawlerSettings
{
    public TimeSpan SubsequentRunDelay { get; set; }
}