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
    public static readonly ParsedPostAttributePictureType[] AllowedImageTypes = [
        ParsedPostAttributePictureType.PNG,
        ParsedPostAttributePictureType.JPEG,
        ParsedPostAttributePictureType.BMP,
        ParsedPostAttributePictureType.TIFF,
        ParsedPostAttributePictureType.WEBP,
    ];

    protected override bool IsIndefinite => true;
    protected readonly ChatId ChatId = new ChatId(telegramBotSettings.Value.ChatId);


    protected override async Task RunAsync(CancellationToken cancellationToken)
    {
        await using var serviceScope = serviceScopeFactory.CreateAsyncScope();
        using var sqlDatabaseContext = serviceScope.ServiceProvider.GetRequiredService<SqlDatabaseContext>();
        var telegramBotClient = serviceScope.ServiceProvider.GetRequiredService<ITelegramBotClient>();
        var mediaDownloader = serviceScope.ServiceProvider.GetRequiredService<IMediaDownloader>();

        var duplicatePictureIdIndex = await sqlDatabaseContext.Configs
            .Where(c => c.Name == ConfigConstants.TelegramBotDuplicatePictureIdIndex)
            .FirstAsync(cancellationToken);

        if (duplicatePictureIdIndex.Value == 0.ToString())
        {
            var latestDuplicatePictureIdIndex = await sqlDatabaseContext.DuplicatePictureVotes
                .AsNoTracking()
                .OrderByDescending(dpv => dpv.DuplicatePicture.AttributeId)
                .Select(dpv => dpv.DuplicatePicture.AttributeId)
                .FirstAsync(cancellationToken);

            duplicatePictureIdIndex.Value = latestDuplicatePictureIdIndex.ToString();
            await sqlDatabaseContext.SaveChangesAsync(cancellationToken);
        }

        while (true)
        {
            var parsedDuplicatePictureIdIndex = int.Parse(duplicatePictureIdIndex.Value);

            var vote = await sqlDatabaseContext.DuplicatePictureVotes
                .AsNoTracking()
                .Include(dpv => dpv.DuplicatePicture)
                .Where(dpv => dpv.VotingClosed == false)
                .Where(dpv => dpv.DuplicatePicture.ImageType == dpv.OriginalPicture.ImageType)
                .Where(dpv => AllowedImageTypes.Contains(dpv.DuplicatePicture.ImageType))
                .Where(dpv => dpv.DuplicatePicture.Post.AttributePictures.Count == 1)
                .Where(dpv => dpv.DuplicatePicture.AttributeId > parsedDuplicatePictureIdIndex)
                .OrderBy(dpv => dpv.DuplicatePictureId)
                .Select(dpv => new DuplicatePictureVoteExtended(
                    dpv,
                    dpv.OriginalPicture.Post.NumberId,
                    dpv.OriginalPicture.Post.AttributePictures.Count,
                    dpv.DuplicatePicture.Post.NumberId,
                    dpv.DuplicatePicture.Post.AttributePictures.Count))
                .FirstOrDefaultAsync(cancellationToken);

            if (vote == null)
                return;

            var votes = await sqlDatabaseContext.DuplicatePictureVotes
                .AsNoTracking()
                .Include(dpv => dpv.DuplicatePicture)
                .Where(dpv => dpv.VotingClosed == false)
                .Where(dpv => dpv.DuplicatePicture.ImageType == dpv.OriginalPicture.ImageType)
                .Where(dpv => AllowedImageTypes.Contains(dpv.DuplicatePicture.ImageType))
                .Where(dpv => dpv.DuplicatePicture.Post.AttributePictures.Count == 1)
                .Where(dpv => dpv.DuplicatePictureId == vote.DuplicatePictureId)
                .OrderBy(dpv => dpv.DuplicatePictureId)
                .ThenBy(dpv => dpv.OriginalPictureId)
                .Select(dpv => new DuplicatePictureVoteExtended(
                    dpv,
                    dpv.OriginalPicture.Post.NumberId,
                    dpv.OriginalPicture.Post.AttributePictures.Count,
                    dpv.DuplicatePicture.Post.NumberId,
                    dpv.DuplicatePicture.Post.AttributePictures.Count))
                .ToArrayAsync(cancellationToken);

            duplicatePictureIdIndex.Value = votes.First().DuplicatePicture.AttributeId.ToString();

            using var duplicateMediaStream = await mediaDownloader.DownloadRawAsync(votes.First().DuplicatePicture, cancellationToken);
            using var originalMediaStrem = await mediaDownloader.DownloadRawAsync(votes.First().OriginalPicture, cancellationToken);

            var duplicateMedia = new InputMediaPhoto(InputFile.FromStream(duplicateMediaStream));
            var originalMedia = new InputMediaPhoto(InputFile.FromStream(originalMediaStrem));

            var mediaGroupMessages = await telegramBotClient.SendMediaGroup(
                ChatId,
                [duplicateMedia, originalMedia],
                cancellationToken: cancellationToken);

            var text = GeneratePostText(votes);
            var inlineKeyboardMarkup = GenerateInlineKeyboardMarkup(votes.First());
            var voteMessage = await telegramBotClient.SendMessage(
                ChatId,
                text,
                ParseMode.MarkdownV2,
                replyMarkup: inlineKeyboardMarkup,
                linkPreviewOptions: new LinkPreviewOptions() { IsDisabled = true },
                cancellationToken: cancellationToken);

            await sqlDatabaseContext.SaveChangesAsync(cancellationToken);
            sqlDatabaseContext.ChangeTracker.Clear();
        }
    }

    public static string GeneratePostText(IEnumerable<DuplicatePictureVoteExtended> votes)
    {
        var stringBuilder = new StringBuilder();

        stringBuilder.AppendFormat("Дубликат: [{0}](https://joyreactor.cc/post/{1})", votes.First().DuplicatePostNumberId, votes.First().DuplicatePostNumberId);

        stringBuilder.AppendLine();
        stringBuilder.AppendFormat("{0}:", votes.Count() == 1 ? "Оригинал" : "Оригиналы");
        foreach (var vote in votes)
            stringBuilder.AppendFormat(" [{0}](https://joyreactor.cc/post/{1})", vote.OriginalPostNumberId, vote.OriginalPostNumberId);

        stringBuilder.AppendLine();
        stringBuilder.AppendFormat("{0}: {1} 🪗 / {2} ✅",
            votes.All(dpv => dpv.VotingClosed) ? "Результаты голосования" : "Голосование",
            votes.Sum(dpv => dpv.YesVotes.Length) / votes.Count(),
            votes.Sum(dpv => dpv.NoVotes.Length) / votes.Count());

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
                    JsonSerializer.Serialize(new DuplicatePictureTelegramVoteRequest() { DuplicatePictureId = vote.DuplicatePictureId, Yes = true })),

                InlineKeyboardButton.WithCallbackData(
                    "✅",
                    JsonSerializer.Serialize(new DuplicatePictureTelegramVoteRequest() { DuplicatePictureId = vote.DuplicatePictureId, Yes = false })),
                ],
            ],
        };

        if (inlineKeyboardMarkup.InlineKeyboard.SelectMany(c => c).Any(ikb => ikb.CallbackData.Length > 64))
            throw new ArgumentOutOfRangeException(nameof(inlineKeyboardMarkup.InlineKeyboard), "Callback data exceeds 64 byte limit.");

        return inlineKeyboardMarkup;
    }
}