namespace JoyReactor.Accordion.Logic.ApiClient;

public record ApiClientSettings
{
    public TimeSpan SubsequentCallDelay { get; set; }
    public int MaxRetryAttempts { get; set; }
}