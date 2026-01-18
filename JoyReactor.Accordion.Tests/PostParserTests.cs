using JoyReactor.Accordion.Logic.ApiClient.Models;
using Microsoft.EntityFrameworkCore;

namespace JoyReactor.Accordion.Tests;

[TestClass]
public sealed class PostParserTests
{
    [TestMethod]
    public async Task CrawlAndParseByPostIdAsync(int postId)
    {
        var post = await SharedDependencies.PostClient.GetAsync(SharedDependencies.Api, postId, default);
        await SharedDependencies.PostParser.ParseAsync(SharedDependencies.Api, post, default);
    }

    [TestMethod]
    public async Task BandcampUrlPath()
    {
        var tag = await SharedDependencies.TagClient.GetByNameAsync(SharedDependencies.Api, "bandcamp", TagLineType.NEW, default);
        if (tag.PostCount == 0)
            return;

        var posts = new List<Post>();
        var postsAttributeValues = new List<string>();
        var postPager = (PostPager)null;
        var page = 0;

        do
        {
            page++;

            postPager = await SharedDependencies.PostClient.GetByTagAsync(SharedDependencies.Api, tag.NumberId, PostLineType.ALL, page, default);
            posts.AddRange(postPager.Posts);

            var postAttributeValues = postPager.Posts
                .SelectMany(post => post.Attributes)
                .Where(postAttribute => postAttribute.Type.Equals("BANDCAMP", StringComparison.OrdinalIgnoreCase))
                .Select(postAttribute => postAttribute.Value)
                .ToArray();
            postsAttributeValues.AddRange(postAttributeValues);

            await SharedDependencies.PostParser.ParseAsync(SharedDependencies.Api, postPager.Posts, default);
        } while (postPager.Posts.Length > 0 && posts.Count() < postPager.TotalCount);

        var urlPaths = await SharedDependencies.SqlDatabaseContext.ParsedBandCamps
            .Select(bandCamp => bandCamp.UrlPath)
            .ToArrayAsync();

        foreach (var postAttributeValue in postsAttributeValues)
        {
            Assert.IsTrue(
                urlPaths.Any(urlPath => postAttributeValue.Contains(urlPath, StringComparison.OrdinalIgnoreCase)),
                $"Failed to find match for {postAttributeValue}");
        }
    }

    [TestMethod]
    public async Task SoundCloudUrlPath()
    {
        var tag = await SharedDependencies.TagClient.GetByNameAsync(SharedDependencies.Api, "soundcloud", TagLineType.NEW, default);
        if (tag.PostCount == 0)
            return;

        var posts = new List<Post>();
        var postsAttributeValues = new List<string>();
        var postPager = (PostPager)null;
        var page = 0;

        do
        {
            page++;

            postPager = await SharedDependencies.PostClient.GetByTagAsync(SharedDependencies.Api, tag.NumberId, PostLineType.ALL, page, default);
            posts.AddRange(postPager.Posts);

            var postAttributeValues = postPager.Posts
                .SelectMany(post => post.Attributes)
                .Where(postAttribute => postAttribute.Type.Equals("SOUNDCLOUD", StringComparison.OrdinalIgnoreCase))
                .Select(postAttribute => postAttribute.Value.Replace("\\/", "/"))
                .ToArray();
            postsAttributeValues.AddRange(postAttributeValues);

            await SharedDependencies.PostParser.ParseAsync(SharedDependencies.Api, postPager.Posts, default);
        } while (postPager.Posts.Length > 0 && posts.Count() < postPager.TotalCount);

        var urlPaths = await SharedDependencies.SqlDatabaseContext.ParsedSoundClouds
            .Select(soundCloud => soundCloud.UrlPath)
            .ToArrayAsync();

        foreach (var postAttributeValue in postsAttributeValues)
        {
            Assert.IsTrue(
                urlPaths.Any(urlPath => postAttributeValue.Contains(urlPath, StringComparison.OrdinalIgnoreCase)),
                $"Failed to find match for {postAttributeValue}");
        }
    }
}