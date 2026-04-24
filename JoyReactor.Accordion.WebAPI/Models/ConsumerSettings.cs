namespace JoyReactor.Accordion.WebAPI.Models;

public record ConsumersSettings
{
    public Dictionary<string, bool> ConsumersEnabled { get; set; }
}