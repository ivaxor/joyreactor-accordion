using JoyReactor.Accordion.Logic.Database.Sql;
using JoyReactor.Accordion.Logic.Database.Sql.Entities;
using JoyReactor.Accordion.WebAPI.Consumers;
using JoyReactor.Accordion.WebAPI.Models;
using JoyReactor.Accordion.WebAPI.Models.Requests;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using System.Text.Json;
using Telegram.Bot;
using Telegram.Bot.Exceptions;
using Telegram.Bot.Polling;
using Telegram.Bot.Types;
using Telegram.Bot.Types.Enums;

namespace JoyReactor.Accordion.WebAPI.BackgroudServices.Cron;

public class TelegramBotReceiver(
    IServiceScopeFactory serviceScopeFactory,
    IOptions<BackgroundServiceSettings> settings,
    IOptions<TelegramBotSettings> telegramBotSettings,
    ILogger<TelegramBotReceiver> logger)
    : RobustBackgroundService(settings, logger)
{
    protected override bool IsIndefinite => true;
    protected override TimeSpan SubsequentRunDelay => TimeSpan.FromMinutes(1);

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

        var voteRequest = JsonSerializer.Deserialize<DuplicatePictureTelegramVoteRequest>(update.CallbackQuery.Data);
        if (voteRequest == null)
            throw new ArgumentNullException(nameof(update.CallbackQuery.Data), "Invalid or empty callback data recieved.");

        var votes = await sqlDatabaseContext.DuplicatePictureVotes
            .AsNoTracking()
            .Where(dpv => dpv.DuplicatePicture.Post.AttributePictures.Count == 1)
            .Where(dpv => VoteCreatedConsumer.AllowedImageTypes.Contains(dpv.DuplicatePicture.ImageType))
            .Where(dpv => VoteCreatedConsumer.AllowedImageTypes.Contains(dpv.OriginalPicture.ImageType))
            .Where(dpv => dpv.DuplicatePictureId == voteRequest.DuplicatePictureId)
            .GroupBy(dpv => dpv.DuplicatePicture.AttributeId)
            .OrderBy(g => g.Key)
            .Select(g => g
                .Select(dpv => new DuplicatePictureVoteExtended(
                    dpv,
                    dpv.OriginalPicture.Post.NumberId,
                    dpv.OriginalPicture.Post.AttributePictures.Count,
                    dpv.DuplicatePicture.Post.NumberId,
                    dpv.DuplicatePicture.Post.AttributePictures.Count,
                    dpv.DuplicatePicture.Post.Nsfw || dpv.OriginalPicture.Post.Nsfw)))
            .FirstOrDefaultAsync(cancellationToken);

        if (votes == null || votes.Count() == 0)
        {
            logger.LogWarning("Failed to find votes for {DuplicatePictureId} duplicate picture id.", voteRequest.DuplicatePictureId);
            return;
        }

        var filteredVotes = votes
            .Where(dpv => dpv.VotingClosed == false)
            .ToArray();

        if (filteredVotes.Length > 0)
        {
            foreach (var vote in filteredVotes)
            {
                var userId = $"{update.CallbackQuery.From.Id}";
                if (voteRequest.Yes)
                {
                    vote.YesVotes = vote.YesVotes.Append(userId)
                        .Distinct(StringComparer.OrdinalIgnoreCase)
                        .ToArray();

                    vote.NoVotes = vote.NoVotes.Except([userId], StringComparer.OrdinalIgnoreCase)
                        .Distinct(StringComparer.OrdinalIgnoreCase)
                        .ToArray();
                }
                else
                {
                    vote.YesVotes = vote.YesVotes.Except([userId], StringComparer.OrdinalIgnoreCase)
                        .Distinct(StringComparer.OrdinalIgnoreCase)
                        .ToArray();

                    vote.NoVotes = vote.NoVotes.Append(userId)
                        .Distinct(StringComparer.OrdinalIgnoreCase)
                        .ToArray();
                }

                var entry = sqlDatabaseContext.Entry<DuplicatePictureVote>(vote);
                entry.State = EntityState.Unchanged;
                entry.Property(p => p.YesVotes).IsModified = true;
                entry.Property(p => p.NoVotes).IsModified = true;
            }

            await sqlDatabaseContext.SaveChangesAsync(cancellationToken);
            sqlDatabaseContext.ChangeTracker.Clear();
        }

        var text = VoteCreatedConsumer.GeneratePostText(votes);
        var inlineKeyboardMarkup = VoteCreatedConsumer.GenerateInlineKeyboardMarkup(votes.First());

        try
        {
            var voteMessage = await telegramBotClient.EditMessageText(
                ChatId,
                update.CallbackQuery.Message.Id,
                text,
                ParseMode.MarkdownV2,
                replyMarkup: inlineKeyboardMarkup,
                linkPreviewOptions: new LinkPreviewOptions() { IsDisabled = true },
                cancellationToken: cancellationToken);
        }
        catch (ApiRequestException ex)
        when (ex.Message.StartsWith("Bad Request: message is not modified: specified new message content and reply markup are exactly the same as a current content and reply markup of the message"))
        { }
    }

    private Task ErrorHandlerAsync(ITelegramBotClient botClient, Exception exception, CancellationToken cancellationToken)
    {
        logger.LogError(exception, "Error handling telegram message.");
        return Task.CompletedTask;
    }
}