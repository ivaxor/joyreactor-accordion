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

        var parsedPost = await SharedDependencies.SqlDatabaseContext.ParsedPosts
            .AsNoTracking()
            .Where(pp => pp.NumberId == post.NumberId)
            .FirstAsync();
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

        var parsedPost = await SharedDependencies.SqlDatabaseContext.ParsedPosts
            .AsNoTracking()
            .Where(pp => pp.NumberId == post.NumberId)
            .FirstAsync();
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

        var parsedPost = await SharedDependencies.SqlDatabaseContext.ParsedPosts
            .AsNoTracking()
            .Where(pp => pp.NumberId == post.NumberId)
            .FirstAsync();
        Assert.AreNotEqual(post.ContentVersion, parsedPost.ContentVersion);
        Assert.AreEqual(existingParsedPost.ContentVersion, parsedPost.ContentVersion);
    }
}