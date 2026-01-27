namespace JoyReactor.Accordion.Logic.Media;

public record MediaSettings
{
    public string CdnHostName { get; set; }
    public TimeSpan SubsequentCallDelay { get; set; }
    public TimeSpan RetryDelay { get; set; }
    public int MaxRetryAttempts { get; set; }
    public int ResizedSize { get; set; }
}