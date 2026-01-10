using JoyReactor.Accordion.Logic.Database.Sql;
using JoyReactor.Accordion.Logic.Database.Sql.Entities;
using JoyReactor.Accordion.Logic.Database.Vector;
using JoyReactor.Accordion.Logic.Media.Images;
using JoyReactor.Accordion.Logic.Onnx;
using JoyReactor.Accordion.WebAPI.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using System.Collections.Concurrent;

namespace JoyReactor.Accordion.WebAPI.BackgroudServices;

public class GifWithoutVectorCrawler(
    IServiceScopeFactory serviceScopeFactory,
    IOptions<BackgroundServiceSettings> settings,
    ILogger<GifWithoutVectorCrawler> logger)
    : RobustBackgroundService(settings, logger)
{
    protected override bool IsIndefinite => true;

    protected override async Task RunAsync(CancellationToken cancellationToken)
    {
        var unprocessedPictures = (ParsedPostAttributePicture[])null;
        do
        {
            await using var serviceScope = serviceScopeFactory.CreateAsyncScope();
            await using var sqlDatabaseContext = serviceScope.ServiceProvider.GetRequiredService<SqlDatabaseContext>();
            var imageDownloader = serviceScope.ServiceProvider.GetRequiredService<IImageDownloader>();
            var onnxVectorConverter = serviceScope.ServiceProvider.GetRequiredService<IOnnxVectorConverter>();
            var vectorDatabaseContext = serviceScope.ServiceProvider.GetRequiredService<IVectorDatabaseContext>();

            unprocessedPictures = await sqlDatabaseContext.ParsedPostAttributePictures
                .Where(picture => picture.IsVectorCreated == false && picture.ImageType == ParsedPostAttributePictureType.GIF)
                .OrderByDescending(picture => picture.Id)
                .Take(10)
                .ToArrayAsync(cancellationToken);

            if (unprocessedPictures.Length == 0)
            {
                logger.LogInformation("No GIFs without vector found. Will try again later.");
                return;
            }
            logger.LogInformation("Starting crawling {PicturesCount} GIFs without vectors.", unprocessedPictures.Length);

            var pictureVectors = new ConcurrentDictionary<ParsedPostAttributePicture, float[]>();
            foreach (var picture in unprocessedPictures)
            {
                using var image = await imageDownloader.DownloadAsync(picture, cancellationToken);
                var vector = await onnxVectorConverter.ConvertAsync(image);
                if (pictureVectors.TryAdd(picture, vector))
                {
                    picture.IsVectorCreated = true;
                    picture.UpdatedAt = DateTime.UtcNow;
                }
            }

            await vectorDatabaseContext.UpsertAsync(pictureVectors, cancellationToken);
            sqlDatabaseContext.ParsedPostAttributePictures.UpdateRange(pictureVectors.Keys);
            await sqlDatabaseContext.SaveChangesAsync(cancellationToken);
        } while (unprocessedPictures.Length != 0);
    }
}