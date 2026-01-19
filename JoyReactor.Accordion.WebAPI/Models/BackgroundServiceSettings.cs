namespace JoyReactor.Accordion.WebAPI.Models;

public record BackgroundServiceSettings
{
    public TimeSpan SubsequentRunDelay { get; set; }
    public Dictionary<string, bool> ServiceNamesEnabled { get; set; }
}