using JoyReactor.Accordion.Logic.Database.Sql;
using JoyReactor.Accordion.Logic.Database.Sql.Entities;
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

    protected override async Task RunAsync(CancellationToken cancellationToken)
    {
        await using var scope = serviceScopeFactory.CreateAsyncScope();
        using var sqlDatabaseContext = scope.ServiceProvider.GetRequiredService<SqlDatabaseContext>();
        var telegramBotClient = scope.ServiceProvider.GetRequiredService<ITelegramBotClient>();

        var vote = await sqlDatabaseContext.DuplicatePictureVotes
            .AsNoTracking()
            .Include(dpv => dpv.OriginalPicture)
            .Include(dpv => dpv.DuplicatePicture)
            .Where(dpv => dpv.VotingClosed == false)
            .Where(dpv => dpv.DuplicatePicture.Post.AttributePictures.Count == 1)
            .Select(dpv => new DuplicatePictureVoteExtended(
                dpv,
                dpv.OriginalPicture.Post.NumberId,
                dpv.OriginalPicture.Post.AttributePictures.Count,
                dpv.DuplicatePicture.Post.NumberId,
                dpv.DuplicatePicture.Post.AttributePictures.Count))
            .OrderBy(dpv => dpv.DuplicatePictureId)
            .LastAsync(cancellationToken);

        var duplicateMedia = new InputMediaPhoto(InputFile.FromUri(GeneratePostAttributePictureUrl(vote.DuplicatePicture)));
        var originalMedia = new InputMediaPhoto(InputFile.FromUri(GeneratePostAttributePictureUrl(vote.OriginalPicture)));
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
            cancellationToken: cancellationToken);
    }

    public static string GeneratePostText(DuplicatePictureVoteExtended vote)
    {
        var stringBuilder = new StringBuilder();

        stringBuilder.AppendFormat("Дубликат: [{0}](https://joyreactor.cc/post/{1})", vote.DuplicatePostNumberId, vote.DuplicatePostNumberId);

        stringBuilder.AppendLine();
        stringBuilder.AppendFormat("Оригинал: [{0}](https://joyreactor.cc/post/{1})", vote.OriginalPostNumberId, vote.OriginalPostNumberId);

        stringBuilder.AppendLine();
        stringBuilder.AppendFormat("Медиа: {0} / {1}", vote.DuplicatePostPictureCount, vote.OriginalPostPictureCount);

        stringBuilder.AppendLine();
        stringBuilder.AppendFormat("Голосование: {0} 🪗 / {1} ✅", vote.YesVotes.Length, vote.NoVotes.Length);

        return stringBuilder.ToString();
    }

    public static string GeneratePostAttributePictureUrl(ParsedPostAttributePicture postAttributePicture)
    {
        var extension = postAttributePicture.ImageType switch
        {
            ParsedPostAttributePictureType.PNG => "png",
            ParsedPostAttributePictureType.JPEG => "jpeg",
            ParsedPostAttributePictureType.GIF => "gif",
            ParsedPostAttributePictureType.BMP => "bmp",
            ParsedPostAttributePictureType.TIFF => "tiff",
            ParsedPostAttributePictureType.MP4 => "mp4",
            ParsedPostAttributePictureType.WEBM => "webm",
            ParsedPostAttributePictureType.WEBP => "webp",
            _ => throw new NotImplementedException(),
        };

        return $"https://img10.joyreactor.cc/pics/post/picture-${postAttributePicture.AttributeId}.{extension}";
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