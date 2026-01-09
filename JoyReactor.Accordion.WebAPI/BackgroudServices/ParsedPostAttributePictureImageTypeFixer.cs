using JoyReactor.Accordion.Logic.ApiClient;
using JoyReactor.Accordion.Logic.Database.Sql;
using JoyReactor.Accordion.Logic.Database.Sql.Entities;
using JoyReactor.Accordion.Logic.Extensions;
using JoyReactor.Accordion.Logic.Parsers;
using JoyReactor.Accordion.WebAPI.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;

namespace JoyReactor.Accordion.WebAPI.BackgroudServices;

public class ParsedPostAttributePictureImageTypeFixer(
    IServiceScopeFactory serviceScopeFactory,
    IOptions<BackgroundServiceSettings> settings,
    ILogger<ParsedPostAttributePictureImageTypeFixer> logger)
    : RobustBackgroundService(settings, logger)
{
    protected override bool IsIndefinite => false;

    protected override async Task RunAsync(CancellationToken cancellationToken)
    {
        await using var serviceScope = serviceScopeFactory.CreateAsyncScope();
        using var sqlDatabaseContext = serviceScope.ServiceProvider.GetService<SqlDatabaseContext>();
        var postClient = serviceScope.ServiceProvider.GetService<IPostClient>();
        var postParser = serviceScope.ServiceProvider.GetService<IPostParser>();

        var skip = 0;
        var take = 100;
        var parsedPostAttributePictures = (ParsedPostAttributePicture[])null;

        do
        {
            parsedPostAttributePictures = await sqlDatabaseContext.ParsedPostAttributePictures
                .Where(postAttribute => (int)postAttribute.ImageType > 6)
                .Skip(skip)
                .Take(take)
                .ToArrayAsync(cancellationToken);
            logger.LogInformation("Fixing {PostAttributeCount} parsed post attribute pictures image types", parsedPostAttributePictures.Length);

            var postIds = parsedPostAttributePictures
                .Select(postAttribute => postAttribute.PostId.ToInt())
                .ToHashSet();

            foreach (var postId in postIds)
            {
                var post = await postClient.GetAsync(postId, cancellationToken);
                await postParser.ParseAsync(post, cancellationToken);
            }

            skip += take;
        } while (parsedPostAttributePictures.Length != 0);
    }
}
