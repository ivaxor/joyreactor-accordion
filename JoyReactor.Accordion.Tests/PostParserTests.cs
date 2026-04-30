using JoyReactor.Accordion.Logic.ApiClient.Models;
using Microsoft.EntityFrameworkCore;
using System.Text;

namespace JoyReactor.Accordion.Tests;

[TestClass]
public sealed class PostParserTests
{
    protected SharedDependencies SharedDependencies { get; set; }

    [TestInitialize]
    public async Task TestInitializeAsync()
    {
        SharedDependencies = await SharedDependencyFactory.CreateAsync();
    }

    [TestCleanup]
    public async Task TestCleanupAsync()
    {
        await SharedDependencies.DisposeAsync();
    }

    [TestMethod]
    public async Task ParseAsync_NewPost_Creates()
    {
        var post1 = CreateTestPost();
        var post1Result = await SharedDependencies.PostParser.ParseAsync(SharedDependencies.Api.Id, post1, default);

        var parsedPost1 = await SharedDependencies.SqlDatabaseContext.ParsedPosts
            .AsNoTracking()
            .Include(pp => pp.AttributePictures)
            .Include(pp => pp.AttributeEmbeds)
            .Where(pp => pp.NumberId == post1.NumberId)
            .FirstAsync();

        Assert.HasCount(1, post1Result.PostAttributePictureNumberIds);
        Assert.HasCount(5, post1Result.PostAttributeEmbeddedUniqueIds);
        Assert.HasCount(1, post1Result.NewPostAttributePictureNumberIds);
        Assert.HasCount(5, post1Result.NewPostAttributeEmbeddedUniqueIds);

        Assert.HasCount(1, parsedPost1.AttributePictures);
        Assert.HasCount(5, parsedPost1.AttributeEmbeds);
    }

    [TestMethod]
    public async Task ParseAsync_ExistingPost_Updates()
    {
        var post1 = CreateTestPost();
        var post1Result = await SharedDependencies.PostParser.ParseAsync(SharedDependencies.Api.Id, post1, default);
        var parsedPost1 = await SharedDependencies.SqlDatabaseContext.ParsedPosts
            .AsNoTracking()
            .Include(pp => pp.AttributePictures)
            .Include(pp => pp.AttributeEmbeds)
            .Where(pp => pp.NumberId == post1.NumberId)
            .FirstAsync();

        var post2 = post1 with
        {
            ContentVersion = post1.ContentVersion + 1,
        };
        var post2Result = await SharedDependencies.PostParser.ParseAsync(SharedDependencies.Api.Id, post2, default);
        var parsedPost2 = await SharedDependencies.SqlDatabaseContext.ParsedPosts
            .AsNoTracking()
            .Include(pp => pp.AttributePictures)
            .Include(pp => pp.AttributeEmbeds)
            .Where(pp => pp.NumberId == post1.NumberId)
            .FirstAsync();

        Assert.AreEqual(parsedPost1.Id, parsedPost2.Id);

        Assert.HasCount(1, post1Result.PostAttributePictureNumberIds);
        Assert.HasCount(5, post1Result.PostAttributeEmbeddedUniqueIds);
        Assert.HasCount(1, post1Result.NewPostAttributePictureNumberIds);
        Assert.HasCount(5, post1Result.NewPostAttributeEmbeddedUniqueIds);

        Assert.HasCount(1, post1Result.PostAttributePictureNumberIds);
        Assert.HasCount(5, post1Result.PostAttributeEmbeddedUniqueIds);
        Assert.HasCount(0, post2Result.NewPostAttributePictureNumberIds);
        Assert.HasCount(0, post2Result.NewPostAttributeEmbeddedUniqueIds);

        CollectionAssert.AreEquivalent(parsedPost1.AttributePictures.Select(ppap => ppap.Id).ToArray(), parsedPost2.AttributePictures.Select(ppap => ppap.Id).ToArray());
        CollectionAssert.AreEquivalent(parsedPost1.AttributeEmbeds.Select(ppae => ppae.Id).ToArray(), parsedPost2.AttributeEmbeds.Select(ppae => ppae.Id).ToArray());
    }

    [TestMethod]
    public async Task ParseAsync_ExistingPost_ContentVersionIsLower_Ignores()
    {
        var post1 = CreateTestPost();
        var post1Result = await SharedDependencies.PostParser.ParseAsync(SharedDependencies.Api.Id, post1, default);

        var post2 = post1 with
        {
            ContentVersion = post1.ContentVersion - 1,
            Attributes = [],
        };
        var post2Result = await SharedDependencies.PostParser.ParseAsync(SharedDependencies.Api.Id, post2, default);

        var parsedPost2 = await SharedDependencies.SqlDatabaseContext.ParsedPosts
            .AsNoTracking()
            .Include(pp => pp.AttributePictures)
            .Include(pp => pp.AttributeEmbeds)
            .Where(pp => pp.NumberId == post1.NumberId)
            .FirstAsync();

        Assert.HasCount(1, post1Result.PostAttributePictureNumberIds);
        Assert.HasCount(5, post1Result.PostAttributeEmbeddedUniqueIds);
        Assert.HasCount(1, post1Result.NewPostAttributePictureNumberIds);
        Assert.HasCount(5, post1Result.NewPostAttributeEmbeddedUniqueIds);

        Assert.IsNull(post2Result);

        Assert.HasCount(1, parsedPost2.AttributePictures);
        Assert.HasCount(5, parsedPost2.AttributeEmbeds);
    }

    [TestMethod]
    public async Task ParseAsync_ExistingPost_ContentVersionIsHigher_Updates()
    {
        var post1 = CreateTestPost();
        var post1Result = await SharedDependencies.PostParser.ParseAsync(SharedDependencies.Api.Id, post1, default);

        var post2 = post1 with
        {
            ContentVersion = post1.ContentVersion + 1,
            Attributes = [],
        };
        var post2Result = await SharedDependencies.PostParser.ParseAsync(SharedDependencies.Api.Id, post2, default);

        var parsedPost2 = await SharedDependencies.SqlDatabaseContext.ParsedPosts
            .AsNoTracking()
            .Include(pp => pp.AttributePictures)
            .Include(pp => pp.AttributeEmbeds)
            .Where(pp => pp.NumberId == post1.NumberId)
            .FirstAsync();

        Assert.HasCount(1, post1Result.PostAttributePictureNumberIds);
        Assert.HasCount(5, post1Result.PostAttributeEmbeddedUniqueIds);
        Assert.HasCount(1, post1Result.NewPostAttributePictureNumberIds);
        Assert.HasCount(5, post1Result.NewPostAttributeEmbeddedUniqueIds);

        Assert.HasCount(0, post2Result.PostAttributePictureNumberIds);
        Assert.HasCount(0, post2Result.PostAttributeEmbeddedUniqueIds);
        Assert.HasCount(0, post2Result.NewPostAttributePictureNumberIds);
        Assert.HasCount(0, post2Result.NewPostAttributeEmbeddedUniqueIds);

        Assert.HasCount(0, parsedPost2.AttributePictures);
        Assert.HasCount(0, parsedPost2.AttributeEmbeds);
    }

    [TestMethod]
    public async Task ParseAsync_ExistingPost_RemovesOnlyPictureAttribute()
    {
        var post1 = CreateTestPost();
        var post1Result = await SharedDependencies.PostParser.ParseAsync(SharedDependencies.Api.Id, post1, default);

        var post2 = post1 with
        {
            ContentVersion = post1.ContentVersion + 1,
            Attributes = post1.Attributes.Where(pa => pa.Type != "PICTURE").ToArray(),
        };
        var post2Result = await SharedDependencies.PostParser.ParseAsync(SharedDependencies.Api.Id, post2, default);

        var parsedPost2 = await SharedDependencies.SqlDatabaseContext.ParsedPosts
            .AsNoTracking()
            .Include(pp => pp.AttributePictures)
            .Include(pp => pp.AttributeEmbeds)
            .Where(pp => pp.NumberId == post1.NumberId)
            .FirstAsync();

        Assert.HasCount(1, post1Result.PostAttributePictureNumberIds);
        Assert.HasCount(5, post1Result.PostAttributeEmbeddedUniqueIds);
        Assert.HasCount(1, post1Result.NewPostAttributePictureNumberIds);
        Assert.HasCount(5, post1Result.NewPostAttributeEmbeddedUniqueIds);

        Assert.HasCount(0, post2Result.PostAttributePictureNumberIds);
        Assert.HasCount(5, post2Result.PostAttributeEmbeddedUniqueIds);
        Assert.HasCount(0, post2Result.NewPostAttributePictureNumberIds);
        Assert.HasCount(0, post2Result.NewPostAttributeEmbeddedUniqueIds);

        Assert.HasCount(0, parsedPost2.AttributePictures);
        Assert.HasCount(5, parsedPost2.AttributeEmbeds);
    }

    [TestMethod]
    public async Task ParseAsync_ExistingPost_AddsOnlyPictureAttribute()
    {
        var post1 = CreateTestPost();
        var post1Result = await SharedDependencies.PostParser.ParseAsync(SharedDependencies.Api.Id, post1, default);

        var post2 = post1 with
        {
            ContentVersion = post1.ContentVersion + 1,
            Attributes = [.. post1.Attributes, CreateTestImagePostAttribute()],
        };
        var post2Result = await SharedDependencies.PostParser.ParseAsync(SharedDependencies.Api.Id, post2, default);

        var parsedPost2 = await SharedDependencies.SqlDatabaseContext.ParsedPosts
            .AsNoTracking()
            .Include(pp => pp.AttributePictures)
            .Include(pp => pp.AttributeEmbeds)
            .Where(pp => pp.NumberId == post1.NumberId)
            .FirstAsync();

        Assert.HasCount(1, post1Result.PostAttributePictureNumberIds);
        Assert.HasCount(5, post1Result.PostAttributeEmbeddedUniqueIds);
        Assert.HasCount(1, post1Result.NewPostAttributePictureNumberIds);
        Assert.HasCount(5, post1Result.NewPostAttributeEmbeddedUniqueIds);

        Assert.HasCount(2, post2Result.PostAttributePictureNumberIds);
        Assert.HasCount(5, post2Result.PostAttributeEmbeddedUniqueIds);
        Assert.HasCount(1, post2Result.NewPostAttributePictureNumberIds);
        Assert.HasCount(0, post2Result.NewPostAttributeEmbeddedUniqueIds);

        Assert.HasCount(2, parsedPost2.AttributePictures);
        Assert.HasCount(5, parsedPost2.AttributeEmbeds);
    }

    [TestMethod]
    [DataRow("BANDCAMP")]
    [DataRow("COUB")]
    [DataRow("SOUNDCLOUD")]
    [DataRow("VIMEO")]
    [DataRow("YOUTUBE")]
    public async Task ParseAsync_ExistingPost_RemovesOnlyEmbeddedAttribute(string type)
    {
        var post1 = CreateTestPost();
        var post1Result = await SharedDependencies.PostParser.ParseAsync(SharedDependencies.Api.Id, post1, default);

        var post2 = post1 with
        {
            ContentVersion = post1.ContentVersion + 1,
            Attributes = post1.Attributes.Where(pa => pa.Type != type).ToArray(),
        };
        var post2Result = await SharedDependencies.PostParser.ParseAsync(SharedDependencies.Api.Id, post2, default);

        var parsedPost2 = await SharedDependencies.SqlDatabaseContext.ParsedPosts
            .AsNoTracking()
            .Include(pp => pp.AttributePictures)
            .Include(pp => pp.AttributeEmbeds)
            .Where(pp => pp.NumberId == post1.NumberId)
            .FirstAsync();

        Assert.HasCount(1, post1Result.PostAttributePictureNumberIds);
        Assert.HasCount(5, post1Result.PostAttributeEmbeddedUniqueIds);
        Assert.HasCount(1, post1Result.NewPostAttributePictureNumberIds);
        Assert.HasCount(5, post1Result.NewPostAttributeEmbeddedUniqueIds);

        Assert.HasCount(1, post2Result.PostAttributePictureNumberIds);
        Assert.HasCount(4, post2Result.PostAttributeEmbeddedUniqueIds);
        Assert.HasCount(0, post2Result.NewPostAttributePictureNumberIds);
        Assert.HasCount(0, post2Result.NewPostAttributeEmbeddedUniqueIds);

        Assert.HasCount(1, parsedPost2.AttributePictures);
        Assert.HasCount(4, parsedPost2.AttributeEmbeds);
    }

    [TestMethod]
    [DataRow("BANDCAMP")]
    [DataRow("COUB")]
    [DataRow("SOUNDCLOUD")]
    [DataRow("VIMEO")]
    [DataRow("YOUTUBE")]
    public async Task ParseAsync_ExistingPost_AddsOnlyEmbeddedAttribute(string type)
    {
        var post1 = CreateTestPost();
        var post1Result = await SharedDependencies.PostParser.ParseAsync(SharedDependencies.Api.Id, post1, default);

        var post2 = post1 with
        {
            ContentVersion = post1.ContentVersion + 1,
            Attributes = [.. post1.Attributes, CreateTestEmbeddedPostAttribute(type)],
        };
        var post2Result = await SharedDependencies.PostParser.ParseAsync(SharedDependencies.Api.Id, post2, default);

        var parsedPost2 = await SharedDependencies.SqlDatabaseContext.ParsedPosts
            .AsNoTracking()
            .Include(pp => pp.AttributePictures)
            .Include(pp => pp.AttributeEmbeds)
            .Where(pp => pp.NumberId == post1.NumberId)
            .FirstAsync();

        Assert.HasCount(1, post1Result.PostAttributePictureNumberIds);
        Assert.HasCount(5, post1Result.PostAttributeEmbeddedUniqueIds);
        Assert.HasCount(1, post1Result.NewPostAttributePictureNumberIds);
        Assert.HasCount(5, post1Result.NewPostAttributeEmbeddedUniqueIds);

        Assert.HasCount(1, post2Result.PostAttributePictureNumberIds);
        Assert.HasCount(6, post2Result.PostAttributeEmbeddedUniqueIds);
        Assert.HasCount(0, post2Result.NewPostAttributePictureNumberIds);
        Assert.HasCount(1, post2Result.NewPostAttributeEmbeddedUniqueIds);

        Assert.HasCount(1, parsedPost2.AttributePictures);
        Assert.HasCount(6, parsedPost2.AttributeEmbeds);
    }

    protected static Post CreateTestPost(int? id = null, int contentVersion = 1, bool nsfw = false)
    {
        return new Post
        {
            NodeId = Convert.ToBase64String(Encoding.UTF8.GetBytes($"Post:{id ?? Random.Shared.Next()}")),
            ContentVersion = contentVersion,
            Nsfw = nsfw,
            Attributes = [
                CreateTestImagePostAttribute(),
                CreateTestEmbeddedPostAttribute("BANDCAMP"),
                CreateTestEmbeddedPostAttribute("COUB"),
                CreateTestEmbeddedPostAttribute("SOUNDCLOUD"),
                CreateTestEmbeddedPostAttribute("VIMEO"),
                CreateTestEmbeddedPostAttribute("YOUTUBE"),
            ]
        };
    }

    protected static PostAttribute CreateTestImagePostAttribute(string type = "JPEG", int? id = null)
    {
        return new PostAttribute()
        {
            NodeId = Convert.ToBase64String(Encoding.UTF8.GetBytes($"PostAttributePicture:{id ?? Random.Shared.Next()}")),
            Type = "PICTURE",
            Image = new Image() { Type = type },
        };
    }

    protected static PostAttribute CreateTestEmbeddedPostAttribute(string type, string? value = null, int? id = null)
    {
        return new PostAttribute()
        {
            NodeId = Convert.ToBase64String(Encoding.UTF8.GetBytes($"PostAttributeEmbed:{id ?? Random.Shared.Next()}")),
            Type = type,
            Value = type switch
            {
                "BANDCAMP" => $"{{\"url\":\"album={value ?? Random.Shared.Next().ToString()}\\/size=large\\/bgcol=ffffff\\/linkcol=0687f5\\/transparent=true\\/\",\"height\":905,\"width\":700}}",
                "SOUNDCLOUD" => $"{{\"url\":\"https:\\/\\/api.soundcloud.com\\/playlists\\/{value ?? Random.Shared.Next().ToString()}\",\"height\":600}}",
                _ => value ?? Random.Shared.Next().ToString(),
            },
        };
    }
}