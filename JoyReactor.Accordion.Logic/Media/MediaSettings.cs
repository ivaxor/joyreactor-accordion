namespace JoyReactor.Accordion.Logic.Media;

public record MediaSettings
{
    public string[] CdnDomainNames { get; set; }
    public int MaxRetryAttempts { get; set; }
    public TimeSpan RetryDelay { get; set; }
    public int ConcurrentDownloads { get; set; }
    public int ResizedSize { get; set; }
}