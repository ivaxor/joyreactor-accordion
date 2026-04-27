using JoyReactor.Accordion.Logic.ApiClient;
using JoyReactor.Accordion.Logic.Database.Sql;
using JoyReactor.Accordion.Logic.Parsers;
using JoyReactor.Accordion.WebAPI.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;

namespace JoyReactor.Accordion.WebAPI.BackgroudServices.Temp;

public class EmptyPostEmbedsFixer(
    IServiceScopeFactory serviceScopeFactory,
    IOptions<BackgroundServiceSettings> settings,
    ILogger<EmptyPostEmbedsFixer> logger)
    : RobustBackgroundService(settings, logger)
{
    protected override bool IsIndefinite => true;
    protected override TimeSpan SubsequentRunDelay => TimeSpan.FromMinutes(1);

    protected override async Task RunAsync(CancellationToken cancellationToken)
    {
        await using var serviceScope = serviceScopeFactory.CreateAsyncScope();
        using var sqlDatabaseContext = serviceScope.ServiceProvider.GetRequiredService<SqlDatabaseContext>();
        var postClient = serviceScope.ServiceProvider.GetRequiredService<IPostClient>();
        var postParser = serviceScope.ServiceProvider.GetRequiredService<IPostParser>();

        var embeds = await sqlDatabaseContext.ParsedPostAttributeEmbeds
            .AsNoTracking()
            .Include(ppae => ppae.Post)
            .ThenInclude(pp => pp.Api)
            .Where(ppae => ppae.BandCampId == null && ppae.CoubId == null && ppae.SoundCloudId == null && ppae.VimeoId == null && ppae.YouTubeId == null)
            .OrderBy(ppae => ppae.Id)
            .ToArrayAsync(cancellationToken);

        foreach (var embedded in embeds)
        {
            var post = await postClient.GetAsync(embedded.Post.Api, embedded.Post.NumberId, cancellationToken);
            var result = await postParser.ParseAsync(embedded.Post.ApiId, post, true, cancellationToken);
        }
    }
}