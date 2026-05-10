using JoyReactor.Accordion.Tests.Helpers;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Hosting;

namespace JoyReactor.Accordion.Tests;

#if DEBUG
[TestClass]
#endif
public sealed class PostParserDevTests
{
    protected IHost Host { get; set; }
    protected TestDependencyProvider DependencyProvider { get; set; }

    [TestInitialize]
    public async Task TestInitializeAsync()
    {
        Host = TestHostApplicationBuilder.CreateInMemory().Build();
        DependencyProvider = new TestDependencyProvider(Host.Services);
    }

    [TestCleanup]
    public async Task TestCleanupAsync()
    {
        Host.Dispose();
    }

    [TestMethod]
    // BandCamp [DataRow(4213169)]
    // Coub [DataRow(648193)]
    // SoundCloud [DataRow(5028161)]
    // Vimeo [DataRow(136679)]
    // YouTube [DataRow(214764)]
    public async Task CrawlAndParseByPostIdAsync(int postId)
    {
        var post = await DependencyProvider.PostClient.GetAsync(DependencyProvider.Api, postId, default);
        await DependencyProvider.PostParser.ParseAsync(DependencyProvider.Api.Id, post, false, default);

        var parsedPost = await DependencyProvider.SqlDatabaseContext.ParsedPosts
            .AsNoTracking()
            .Include(pp => pp.AttributeEmbeds)
            .ThenInclude(ppae => ppae.BandCamp)
            .Include(pp => pp.AttributeEmbeds)
            .ThenInclude(ppae => ppae.Coub)
            .Include(pp => pp.AttributeEmbeds)
            .ThenInclude(ppae => ppae.SoundCloud)
            .Include(pp => pp.AttributeEmbeds)
            .ThenInclude(ppae => ppae.Vimeo)
            .Include(pp => pp.AttributeEmbeds)
            .ThenInclude(ppae => ppae.YouTube)
            .Where(pp => pp.NumberId == postId)
            .SingleAsync();
    }

    /*
    [TestMethod]
    public async Task BandcampUrlPath()
    {
        var tag = await DependencyProvider.TagClient.GetByNameAsync(DependencyProvider.Api, "bandcamp", TagLineType.NEW, default);
        if (tag.PostCount == 0)
            return;

        var posts = new List<Post>();
        var postsAttributeValues = new List<string>();
        var postPager = (PostPager)null;
        var page = 0;

        do
        {
            page++;

            postPager = await DependencyProvider.PostClient.GetByTagAsync(DependencyProvider.Api, tag.NumberId, PostLineType.ALL, page, default);
            posts.AddRange(postPager.Posts);

            var postAttributeValues = postPager.Posts
                .SelectMany(post => post.Attributes)
                .Where(postAttribute => postAttribute.Type.Equals("BANDCAMP", StringComparison.OrdinalIgnoreCase))
                .Select(postAttribute => postAttribute.Value)
                .ToArray();
            postsAttributeValues.AddRange(postAttributeValues);

            await DependencyProvider.PostParser.ParseAsync(DependencyProvider.Api.Id, postPager.Posts, default);
        } while (postPager.Posts.Length > 0 && posts.Count() < postPager.TotalCount);

        var urlPaths = await DependencyProvider.SqlDatabaseContext.ParsedBandCamps
            .AsNoTracking()
            .Select(bandCamp => bandCamp.UrlPath)
            .ToArrayAsync();

        foreach (var postAttributeValue in postsAttributeValues)
        {
            Assert.IsTrue(
                urlPaths.Any(urlPath => postAttributeValue.Contains(urlPath, StringComparison.OrdinalIgnoreCase)),
                $"Failed to find match for {postAttributeValue}");
        }
    }
    */

    /*
    [TestMethod]
    public async Task SoundCloudUrlPath()
    {
        var tag = await DependencyProvider.TagClient.GetByNameAsync(DependencyProvider.Api, "soundcloud", TagLineType.NEW, default);
        if (tag.PostCount == 0)
            return;

        var posts = new List<Post>();
        var postsAttributeValues = new List<string>();
        var postPager = (PostPager)null;
        var page = 0;

        do
        {
            page++;

            postPager = await DependencyProvider.PostClient.GetByTagAsync(DependencyProvider.Api, tag.NumberId, PostLineType.ALL, page, default);
            posts.AddRange(postPager.Posts);

            var postAttributeValues = postPager.Posts
                .SelectMany(post => post.Attributes)
                .Where(postAttribute => postAttribute.Type.Equals("SOUNDCLOUD", StringComparison.OrdinalIgnoreCase))
                .Select(postAttribute => postAttribute.Value.Replace("\\/", "/"))
                .ToArray();
            postsAttributeValues.AddRange(postAttributeValues);

            await DependencyProvider.PostParser.ParseAsync(DependencyProvider.Api, postPager.Posts, default);
        } while (postPager.Posts.Length > 0 && posts.Count() < postPager.TotalCount);

        var urlPaths = await DependencyProvider.SqlDatabaseContext.ParsedSoundClouds
            .AsNoTracking()
            .Select(soundCloud => soundCloud.UrlPath)
            .ToArrayAsync();

        foreach (var postAttributeValue in postsAttributeValues)
        {
            Assert.IsTrue(
                urlPaths.Any(urlPath => postAttributeValue.Contains(urlPath, StringComparison.OrdinalIgnoreCase)),
                $"Failed to find match for {postAttributeValue}");
        }
    }
    */
}