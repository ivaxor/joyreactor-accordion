namespace JoyReactor.Accordion.WebAPI.Models;

public record TelegramBotSettings
{
    public required string Token { get; init; }
    public required long ChatId { get; init; }
}