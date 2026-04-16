namespace JoyReactor.Accordion.WebAPI.Models;

public record TelegramBotSettings
{
    public required string Token { get; init; }
}