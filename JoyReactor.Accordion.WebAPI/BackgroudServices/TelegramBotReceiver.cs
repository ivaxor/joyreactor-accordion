using JoyReactor.Accordion.Logic.Database.Sql;
using JoyReactor.Accordion.WebAPI.Models;
using Microsoft.Extensions.Options;
using Telegram.Bot;
using Telegram.Bot.Polling;
using Telegram.Bot.Types;
using Telegram.Bot.Types.Enums;

namespace JoyReactor.Accordion.WebAPI.BackgroudServices;

public class TelegramBotReceiver(
    IServiceScopeFactory serviceScopeFactory,
    ITelegramBotClient telegramBotClient,
    IOptions<BackgroundServiceSettings> settings,
    ILogger<TelegramBotReceiver> logger)
    : RobustBackgroundService(settings, logger)
{
    protected override bool IsIndefinite => true;

    protected readonly ReceiverOptions ReceiverOptions = new ReceiverOptions()
    {
        AllowedUpdates = [],
    };

    protected override async Task RunAsync(CancellationToken cancellationToken)
    {
        await telegramBotClient.ReceiveAsync(UpdateHandlerAsync, ErrorHandlerAsync, ReceiverOptions, cancellationToken);
    }

    private async Task UpdateHandlerAsync(ITelegramBotClient botClient, Update update, CancellationToken ct)
    {
        await using var scope = serviceScopeFactory.CreateAsyncScope();
        using var sqlContext = scope.ServiceProvider.GetRequiredService<SqlDatabaseContext>();

        switch (update.Type)
        {
            case UpdateType.Message:
                break;

            case UpdateType.CallbackQuery:
                break;

            default:
                throw new NotImplementedException();
        }
    }

    private Task ErrorHandlerAsync(ITelegramBotClient botClient, Exception exception, CancellationToken cancellationToken)
    {
        logger.LogError(exception, "Error handling telegram message.");
        return Task.CompletedTask;
    }
}