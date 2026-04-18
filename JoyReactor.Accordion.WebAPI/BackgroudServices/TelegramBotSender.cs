using JoyReactor.Accordion.Logic.Database.Sql;
using JoyReactor.Accordion.Logic.Database.Sql.Entities;
using JoyReactor.Accordion.Logic.Media;
using JoyReactor.Accordion.WebAPI.Models;
using JoyReactor.Accordion.WebAPI.Models.Requests;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using System.Text;
using System.Text.Json;
using Telegram.Bot;
using Telegram.Bot.Types;
using Telegram.Bot.Types.Enums;
using Telegram.Bot.Types.ReplyMarkups;

namespace JoyReactor.Accordion.WebAPI.BackgroudServices;

public class TelegramBotSender(
    IServiceScopeFactory serviceScopeFactory,
    IOptions<BackgroundServiceSettings> settings,
    IOptions<TelegramBotSettings> telegramBotSettings,
    ILogger<TelegramBotSender> logger)
    : RobustBackgroundService(settings, logger)
{
    protected override bool IsIndefinite => true;
    protected readonly ChatId ChatId = new ChatId(telegramBotSettings.Value.ChatId);
    protected readonly ParsedPostAttributePictureType[] AllowedImageTypes = [
        ParsedPostAttributePictureType.PNG,
        ParsedPostAttributePictureType.JPEG,
        ParsedPostAttributePictureType.BMP,
        ParsedPostAttributePictureType.TIFF,
        ParsedPostAttributePictureType.WEBP,
    ];

    protected override async Task RunAsync(CancellationToken cancellationToken)
    {
        await using var serviceScope = serviceScopeFactory.CreateAsyncScope();
        using var sqlDatabaseContext = serviceScope.ServiceProvider.GetRequiredService<SqlDatabaseContext>();
        var telegramBotClient = serviceScope.ServiceProvider.GetRequiredService<ITelegramBotClient>();
        var mediaDownloader = serviceScope.ServiceProvider.GetRequiredService<IMediaDownloader>();

        var vote = await sqlDatabaseContext.DuplicatePictureVotes
            .AsNoTracking()
            .Include(dpv => dpv.OriginalPicture)
            .Include(dpv => dpv.DuplicatePicture)
            .Where(dpv => dpv.VotingClosed == false)
            .Where(dpv => dpv.DuplicatePicture.ImageType == dpv.OriginalPicture.ImageType)
            .Where(dpv => AllowedImageTypes.Contains(dpv.DuplicatePicture.ImageType))
            .Where(dpv => dpv.DuplicatePicture.Post.AttributePictures.Count == 1)
            .OrderByDescending(dpv => dpv.DuplicatePictureId)
            .Select(dpv => new DuplicatePictureVoteExtended(
                dpv,
                dpv.OriginalPicture.Post.NumberId,
                dpv.OriginalPicture.Post.AttributePictures.Count,
                dpv.DuplicatePicture.Post.NumberId,
                dpv.DuplicatePicture.Post.AttributePictures.Count))
            .FirstAsync(cancellationToken);

        using var duplicateMediaStream = await mediaDownloader.DownloadRawAsync(vote.DuplicatePicture, cancellationToken);
        using var originalMediaStrem = await mediaDownloader.DownloadRawAsync(vote.OriginalPicture, cancellationToken);

        var duplicateMedia = new InputMediaPhoto(InputFile.FromStream(duplicateMediaStream));
        var originalMedia = new InputMediaPhoto(InputFile.FromStream(originalMediaStrem));

        var mediaGroupMessages = await telegramBotClient.SendMediaGroup(
            ChatId,
            [duplicateMedia, originalMedia],
            cancellationToken: cancellationToken);

        var text = GeneratePostText(vote);
        var inlineKeyboardMarkup = GenerateInlineKeyboardMarkup(vote);
        var voteMessage = await telegramBotClient.SendMessage(
            ChatId, text,
            ParseMode.MarkdownV2,
            replyMarkup: inlineKeyboardMarkup,
            linkPreviewOptions: new LinkPreviewOptions() { IsDisabled = true },
            cancellationToken: cancellationToken);
    }

    public static string GeneratePostText(DuplicatePictureVoteExtended vote)
    {
        var stringBuilder = new StringBuilder();

        stringBuilder.AppendFormat("Дубликат: [{0}](https://joyreactor.cc/post/{1})", vote.DuplicatePostNumberId, vote.DuplicatePostNumberId);

        stringBuilder.AppendLine();
        stringBuilder.AppendFormat("Оригинал: [{0}](https://joyreactor.cc/post/{1})", vote.OriginalPostNumberId, vote.OriginalPostNumberId);

        stringBuilder.AppendLine();
        if (!vote.VotingClosed)
            stringBuilder.AppendFormat("Голосование: {0} 🪗 / {1} ✅", vote.YesVotes.Length, vote.NoVotes.Length);
        else
            stringBuilder.AppendFormat("Результаты голосования: {0} 🪗 / {1} ✅", vote.YesVotes.Length, vote.NoVotes.Length);

        return stringBuilder.ToString();
    }

    public static InlineKeyboardMarkup? GenerateInlineKeyboardMarkup(DuplicatePictureVote vote)
    {
        if (vote.VotingClosed == true)
            return null;

        var inlineKeyboardMarkup = new InlineKeyboardMarkup()
        {
            InlineKeyboard = [
            [
                InlineKeyboardButton.WithCallbackData(
                    "🪗",
                    JsonSerializer.Serialize(new DuplicatePictureVoteRequest() { Id = vote.Id, Yes = true })),

                InlineKeyboardButton.WithCallbackData(
                    "✅",
                    JsonSerializer.Serialize(new DuplicatePictureVoteRequest() { Id = vote.Id, Yes = false })),
                ],
            ],
        };

        if (inlineKeyboardMarkup.InlineKeyboard.SelectMany(c => c).Any(ikb => ikb.CallbackData.Length > 64))
            throw new ArgumentOutOfRangeException(nameof(inlineKeyboardMarkup.InlineKeyboard), "Callback data exceeds 64 byte limit.");

        return inlineKeyboardMarkup;
    }
}