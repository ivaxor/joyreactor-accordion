namespace JoyReactor.Accordion.Logic.ApiClient;

public record ApiClientSettings
{
    public string GraphQlEndpointUrl { get; set; }
    public TimeSpan SubsequentCallDelay { get; set; }
    public int MaxRetryAttempts { get; set; }
}