
using JoyReactor.Accordion.Logic.ApiClient;
using JoyReactor.Accordion.Logic.ApiClient.Models;
using JoyReactor.Accordion.Logic.Database.Sql;
using JoyReactor.Accordion.Logic.Database.Sql.Entities;
using JoyReactor.Accordion.Logic.Extensions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;

namespace JoyReactor.Accordion.WebAPI.BackgroudServices;

public class TagOuterRangeCrawler(
    SqlDatabaseContext sqlDatabaseContext,
    ITagClient tagClient,
    IOptions<CrawlerSettings> settings,
    ILogger<TagOuterRangeCrawler> logger)
    : ScopedBackgroudService
{
    protected override async Task ExecuteAsync(CancellationToken cancellationToken)
    {
        var periodicTimer = new PeriodicTimer(settings.Value.SubsequentRunDelay);
        do
        {
            var lastTag = await sqlDatabaseContext.ParsedTags
                .OrderBy(tag => tag.NumberId)
                .LastOrDefaultAsync(cancellationToken);
            if (lastTag == null)
            {
                logger.LogInformation("No tags found. Will try again later");
                continue;
            }

            for (var tagNumber = lastTag.NumberId + 1; ; tagNumber++)
            {
                var tag = await tagClient.GetAsync(tagNumber, TagLineType.NEW, cancellationToken);
                if (tag == null)
                {
                    logger.LogInformation("No tag found with {TagNumberId} number id", tagNumber);
                    continue;
                }
                logger.LogInformation("Found tag with {TagNumberId} number id", tagNumber);

                var parsedTag = new ParsedTag(tag);
                await sqlDatabaseContext.ParsedTags.AddIgnoreExistingAsync(parsedTag, cancellationToken);
                await sqlDatabaseContext.SaveChangesAsync(cancellationToken);
            }
        } while (await periodicTimer.WaitForNextTickAsync(cancellationToken));
    }
}