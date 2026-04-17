using JoyReactor.Accordion.Logic.Database.Sql;
using JoyReactor.Accordion.Logic.Database.Sql.Entities;
using JoyReactor.Accordion.WebAPI.Models;
using JoyReactor.Accordion.WebAPI.Models.Requests;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using System.Text.Json;
using Telegram.Bot;
using Telegram.Bot.Polling;
using Telegram.Bot.Types;
using Telegram.Bot.Types.Enums;

namespace JoyReactor.Accordion.WebAPI.BackgroudServices;

public class TelegramBotReceiver(
    IServiceScopeFactory serviceScopeFactory,
    IOptions<BackgroundServiceSettings> settings,
    IOptions<TelegramBotSettings> telegramBotSettings,
    ILogger<TelegramBotReceiver> logger)
    : RobustBackgroundService(settings, logger)
{
    protected override bool IsIndefinite => true;
    protected readonly ChatId ChatId = new ChatId(telegramBotSettings.Value.ChatId);
    protected readonly ReceiverOptions ReceiverOptions = new ReceiverOptions()
    {
        AllowedUpdates = [UpdateType.CallbackQuery],
    };

    protected override async Task RunAsync(CancellationToken cancellationToken)
    {
        await using var scope = serviceScopeFactory.CreateAsyncScope();
        var telegramBotClient = scope.ServiceProvider.GetRequiredService<ITelegramBotClient>();

        await telegramBotClient.ReceiveAsync(UpdateHandlerAsync, ErrorHandlerAsync, ReceiverOptions, cancellationToken);
    }

    private async Task UpdateHandlerAsync(ITelegramBotClient telegramBotClient, Update update, CancellationToken cancellationToken)
    {
        await using var scope = serviceScopeFactory.CreateAsyncScope();
        using var sqlDatabaseContext = scope.ServiceProvider.GetRequiredService<SqlDatabaseContext>();

        if (update.Type != UpdateType.CallbackQuery)
            throw new ArgumentOutOfRangeException(nameof(update.Type), "Unsupported update type recieved.");

        var voteRequest = JsonSerializer.Deserialize<DuplicatePictureVoteRequest>(update.CallbackQuery.Data);
        if (voteRequest == null)
            throw new ArgumentNullException(nameof(update.CallbackQuery.Data), "Invalid or empty callback data recieved.");

        var vote = await sqlDatabaseContext.DuplicatePictureVotes
            .AsNoTracking()
            .Where(dpv => dpv.Id == voteRequest.Id)
            .Select(dpv => new DuplicatePictureVoteExtended(
                dpv,
                dpv.OriginalPicture.Post.NumberId,
                dpv.OriginalPicture.Post.AttributePictures.Count,
                dpv.DuplicatePicture.Post.NumberId,
                dpv.DuplicatePicture.Post.AttributePictures.Count))
            .SingleAsync(cancellationToken);

        if (!vote.VotingClosed)
        {
            var userId = $"{update.CallbackQuery.From.Id}";
            if (voteRequest.Yes)
            {
                vote.YesVotes = vote.YesVotes.Append(userId)
                    .Distinct(StringComparer.OrdinalIgnoreCase)
                    .ToArray();

                var entry = sqlDatabaseContext.Entry<DuplicatePictureVote>(vote);
                entry.State = EntityState.Unchanged;
                entry.Property(p => p.YesVotes).IsModified = true;
            }
            else
            {
                vote.NoVotes = vote.NoVotes.Append(userId)
                    .Distinct(StringComparer.OrdinalIgnoreCase)
                    .ToArray();

                var entry = sqlDatabaseContext.Entry<DuplicatePictureVote>(vote);
                entry.State = EntityState.Unchanged;
                entry.Property(p => p.NoVotes).IsModified = true;
            }

            await sqlDatabaseContext.SaveChangesAsync(cancellationToken);
            sqlDatabaseContext.ChangeTracker.Clear();
        }

        var text = TelegramBotSender.GeneratePostText(vote);
        var inlineKeyboardMarkup = TelegramBotSender.GenerateInlineKeyboardMarkup(vote);
        var voteMessage = await telegramBotClient.EditMessageText(
            ChatId,
            update.CallbackQuery.Message.Id,
            text,
            ParseMode.MarkdownV2,
            replyMarkup: inlineKeyboardMarkup,
            cancellationToken: cancellationToken);
    }

    private Task ErrorHandlerAsync(ITelegramBotClient botClient, Exception exception, CancellationToken cancellationToken)
    {
        logger.LogError(exception, "Error handling telegram message.");
        return Task.CompletedTask;
    }
}