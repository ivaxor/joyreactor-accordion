using JoyReactor.Accordion.Logic.Database.Sql;
using JoyReactor.Accordion.Logic.Database.Sql.Entities;
using JoyReactor.Accordion.Logic.Media;
using JoyReactor.Accordion.Logic.MQ.Messages;
using JoyReactor.Accordion.WebAPI.Models;
using JoyReactor.Accordion.WebAPI.Models.Requests;
using MassTransit;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using System.Text;
using System.Text.Json;
using Telegram.Bot;
using Telegram.Bot.Types;
using Telegram.Bot.Types.Enums;
using Telegram.Bot.Types.ReplyMarkups;

namespace JoyReactor.Accordion.WebAPI.Consumers;

public class VoteCreatedConsumer(
    SqlDatabaseContext sqlDatabaseContext,
    ITelegramBotClient telegramBotClient,
    IMediaDownloader mediaDownloader,
    IOptions<TelegramBotSettings> telegramBotSettings)
    : IConsumer<VoteCreatedMessage>
{
    public static readonly ParsedPostAttributePictureType[] AllowedImageTypes = [
        ParsedPostAttributePictureType.PNG,
        ParsedPostAttributePictureType.JPEG,
        ParsedPostAttributePictureType.GIF,
        ParsedPostAttributePictureType.BMP,
        ParsedPostAttributePictureType.TIFF,
        ParsedPostAttributePictureType.MP4,
        ParsedPostAttributePictureType.WEBM,
        ParsedPostAttributePictureType.WEBP,
    ];

    protected readonly ChatId ChatId = new ChatId(telegramBotSettings.Value.ChatId);

    public async Task Consume(ConsumeContext<VoteCreatedMessage> context)
    {
        var votes = await sqlDatabaseContext.DuplicatePictureVotes
            .AsNoTracking()
            .Where(dpv => dpv.SentViaTelegram == false)
            .Where(dpv => dpv.VotingClosed == false)
            .Where(dpv => dpv.DuplicatePicture.Post.AttributePictures.Count == 1)
            .Where(dpv => AllowedImageTypes.Contains(dpv.DuplicatePicture.ImageType))
            .Where(dpv => AllowedImageTypes.Contains(dpv.OriginalPicture.ImageType))
            .Where(dpv => dpv.DuplicatePictureId == context.Message.DuplicatePictureId)
            .OrderBy(dpv => dpv.DuplicatePictureId)
            .ThenBy(dpv => dpv.OriginalPictureId)
            .GroupBy(dpv => dpv.DuplicatePicture.AttributeId)
            .OrderBy(g => g.Key)
            .Select(g => g
                .OrderBy(dpv => dpv.DuplicatePictureId)
                .ThenBy(dpv => dpv.OriginalPictureId)
                .Select(dpv => new DuplicatePictureVoteExtended(
                    dpv,
                    dpv.OriginalPicture.Post.Api.HostName,
                    dpv.OriginalPicture.Post.NumberId,
                    dpv.OriginalPicture.Post.AttributePictures.Count,
                    dpv.DuplicatePicture.Post.Api.HostName,
                    dpv.DuplicatePicture.Post.NumberId,
                    dpv.DuplicatePicture.Post.AttributePictures.Count,
                    dpv.DuplicatePicture.Post.Nsfw || dpv.OriginalPicture.Post.Nsfw)))
            .FirstOrDefaultAsync(context.CancellationToken);

        if (votes == null)
            return;

        using var duplicateMediaStream = await mediaDownloader.DownloadRawAsync(votes.First().DuplicatePicture, context.CancellationToken);
        using var originalMediaStrem = await mediaDownloader.DownloadRawAsync(votes.First().OriginalPicture, context.CancellationToken);

        var duplicateMedia = CreateInputMedia(duplicateMediaStream, votes.First().DuplicatePicture, votes.First().Nsfw);
        var originalMedia = CreateInputMedia(originalMediaStrem, votes.First().OriginalPicture, votes.First().Nsfw);

        var mediaGroupMessages = await telegramBotClient.SendMediaGroup(
            ChatId,
            [duplicateMedia, originalMedia],
            cancellationToken: context.CancellationToken);

        var text = GeneratePostText(votes);
        var inlineKeyboardMarkup = GenerateInlineKeyboardMarkup(votes.First());

        try
        {
            var voteMessage = await telegramBotClient.SendMessage(
                ChatId,
                text,
                ParseMode.MarkdownV2,
                replyMarkup: inlineKeyboardMarkup,
                linkPreviewOptions: new LinkPreviewOptions() { IsDisabled = true },
                cancellationToken: context.CancellationToken);
        }
        catch
        {
            var messageIds = mediaGroupMessages.Select(m => m.Id).ToArray();
            await telegramBotClient.DeleteMessages(ChatId, messageIds, context.CancellationToken);

            throw;
        }

        foreach (var vote in votes)
        {
            vote.SentViaTelegram = true;
            vote.UpdatedAt = DateTime.UtcNow;

            var entry = sqlDatabaseContext.DuplicatePictureVotes.Entry(vote);
            entry.Property(e => e.SentViaTelegram).IsModified = true;
            entry.Property(e => e.UpdatedAt).IsModified = true;
        }
        await sqlDatabaseContext.SaveChangesAsync(context.CancellationToken);
    }

    protected IAlbumInputMedia CreateInputMedia(Stream stream, ParsedPostAttributePicture picture, bool nsfw)
    {
        var file = InputFile.FromStream(stream);

        return picture.ImageType switch
        {
            ParsedPostAttributePictureType.PNG => new InputMediaPhoto(file) { HasSpoiler = nsfw },
            ParsedPostAttributePictureType.JPEG => new InputMediaPhoto(file) { HasSpoiler = nsfw },
            ParsedPostAttributePictureType.GIF => new InputMediaVideo(file) { HasSpoiler = nsfw },
            ParsedPostAttributePictureType.BMP => new InputMediaPhoto(file) { HasSpoiler = nsfw },
            ParsedPostAttributePictureType.TIFF => new InputMediaPhoto(file) { HasSpoiler = nsfw },
            ParsedPostAttributePictureType.MP4 => new InputMediaVideo(file) { HasSpoiler = nsfw },
            ParsedPostAttributePictureType.WEBM => new InputMediaVideo(file) { HasSpoiler = nsfw },
            ParsedPostAttributePictureType.WEBP => new InputMediaPhoto(file) { HasSpoiler = nsfw },
            _ => throw new NotImplementedException(),
        };
    }

    public static string GeneratePostText(IEnumerable<DuplicatePictureVoteExtended> votes)
    {
        var stringBuilder = new StringBuilder();
        stringBuilder.AppendFormat(
            "Дубликат: [{0}](https://{1}/post/{2})",
            votes.First().DuplicatePostNumberId,
            votes.First().DuplicateHostName,
            votes.First().DuplicatePostNumberId);

        stringBuilder.AppendLine();
        stringBuilder.AppendFormat("{0}:", votes.Count() == 1 ? "Оригинал" : "Оригиналы");
        foreach (var vote in votes)
        {
            stringBuilder.AppendFormat(
                " [{0}](https://{1}/post/{2})",
                vote.OriginalPostNumberId,
                vote.OriginalHostName,
                vote.OriginalPostNumberId);
        }

        stringBuilder.AppendLine();
        stringBuilder.AppendFormat("{0}: {1} 🪗 / {2} 🆗",
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
                    "🆗",
                    JsonSerializer.Serialize(new DuplicatePictureTelegramVoteRequest() { DuplicatePictureId = vote.DuplicatePictureId, Yes = false })),
                ],
            ],
        };

        if (inlineKeyboardMarkup.InlineKeyboard.SelectMany(c => c).Any(ikb => ikb.CallbackData.Length > 64))
            throw new ArgumentOutOfRangeException(nameof(inlineKeyboardMarkup.InlineKeyboard), "Callback data exceeds 64 byte limit.");

        return inlineKeyboardMarkup;
    }
}

public class VoteCreatedConsumerDefinition : ConsumerDefinition<VoteCreatedConsumer>
{
    public VoteCreatedConsumerDefinition()
    {
        EndpointName = "vote_created";
        ConcurrentMessageLimit = 1;
    }

    protected override void ConfigureConsumer(
        IReceiveEndpointConfigurator endpointConfigurator,
        IConsumerConfigurator<VoteCreatedConsumer> consumerConfigurator,
        IRegistrationContext context)
    {
        endpointConfigurator.UseMessageRetry(retryConfurator => retryConfurator.Interval(3, TimeSpan.FromSeconds(5)));
    }
}