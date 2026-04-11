using JoyReactor.Accordion.Logic.ApiClient.Models;
using JoyReactor.Accordion.Logic.Database.Sql.Entities;
using Microsoft.EntityFrameworkCore;

namespace JoyReactor.Accordion.Tests;

[TestClass]
public sealed class PostParserTests
{
    protected SharedDependencies SharedDependencies { get; set; }

    [TestInitialize]
    public async Task TestInitializeAsync()
    {
        SharedDependencies = await SharedDependencyFactory.CreateAsync(Guid.NewGuid());
    }

    [TestCleanup]
    public async Task TestCleanupAsync()
    {
        await SharedDependencies.DisposeAsync();
    }

    [TestMethod]
    public async Task ParseAsync_NoExistingPost_Inserts()
    {
        var post = await SharedDependencies.PostClient.GetAsync(SharedDependencies.Api, 22, default);

        await SharedDependencies.PostParser.ParseAsync(SharedDependencies.Api, post, default);

        var parsedPost = await SharedDependencies.SqlDatabaseContext.ParsedPosts.FirstAsync(p => p.NumberId == post.NumberId);
        Assert.AreEqual(post.ContentVersion, parsedPost.ContentVersion);
    }

    [TestMethod]
    public async Task ParseAsync_NewContentVersion_Upserts()
    {
        var post = await SharedDependencies.PostClient.GetAsync(SharedDependencies.Api, 22, default);

        var existingParsedPost = new ParsedPost(SharedDependencies.Api, post);
        existingParsedPost.ContentVersion--;
        await SharedDependencies.SqlDatabaseContext.ParsedPosts.AddAsync(existingParsedPost);
        await SharedDependencies.SqlDatabaseContext.SaveChangesAsync();
        SharedDependencies.SqlDatabaseContext.ChangeTracker.Clear();

        await SharedDependencies.PostParser.ParseAsync(SharedDependencies.Api, post, default);

        var parsedPost = await SharedDependencies.SqlDatabaseContext.ParsedPosts.FirstAsync(p => p.NumberId == post.NumberId);
        Assert.AreEqual(post.ContentVersion, parsedPost.ContentVersion);
        Assert.AreNotEqual(existingParsedPost.ContentVersion, parsedPost.ContentVersion);
    }

    [TestMethod]
    public async Task ParseAsync_OldContentVersion_Ignores()
    {
        var post = await SharedDependencies.PostClient.GetAsync(SharedDependencies.Api, 22, default);

        var existingParsedPost = new ParsedPost(SharedDependencies.Api, post);
        existingParsedPost.ContentVersion++;
        await SharedDependencies.SqlDatabaseContext.ParsedPosts.AddAsync(existingParsedPost);
        await SharedDependencies.SqlDatabaseContext.SaveChangesAsync();
        SharedDependencies.SqlDatabaseContext.ChangeTracker.Clear();

        await SharedDependencies.PostParser.ParseAsync(SharedDependencies.Api, post, default);

        var parsedPost = await SharedDependencies.SqlDatabaseContext.ParsedPosts.FirstAsync(p => p.NumberId == post.NumberId);
        Assert.AreNotEqual(post.ContentVersion, parsedPost.ContentVersion);
        Assert.AreEqual(existingParsedPost.ContentVersion, parsedPost.ContentVersion);
    }

    //[TestMethod]
    public async Task CrawlAndParseByPostIdAsync(int postId)
    {
        var post = await SharedDependencies.PostClient.GetAsync(SharedDependencies.Api, postId, default);
        await SharedDependencies.PostParser.ParseAsync(SharedDependencies.Api, post, default);
    }

    //[TestMethod]
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

    //[TestMethod]
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