namespace JoyReactor.Accordion.Tests;

[TestClass]
public sealed class SoundCloudApiClientTests
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
    [DataRow("tracks/1192217695", "https://soundcloud.com/ektd/winter_morning")]
    [DataRow("playlists/722549376", "https://soundcloud.com/novaypapka/sets/hbz")]
    public async Task GetByIdAsync(string urlPath, string permaLinkUrl)
    {
        var response = await SharedDependencies.SoundCloudApiClient.GetByIdAsync(urlPath, default);

        Assert.AreEqual(permaLinkUrl, response.PermalinkUrl);
    }

    [TestMethod]
    [DataRow("https://soundcloud.com/ektd/winter_morning", "tracks/1192217695")]
    [DataRow("https://soundcloud.com/novaypapka/sets/hbz", "playlists/722549376")]
    public async Task GetByPermaLinkAsyncAsync(string permaLinkUrl, string urlPath)
    {
        var response = await SharedDependencies.SoundCloudApiClient.GetByPermaLinkAsync(permaLinkUrl, default);

        Assert.AreEqual(urlPath, $"{response.Kind}s/{response.Id}");
    }

    [TestMethod]
    public async Task GetByPermaLinkAsync_NonExisting()
    {
        var response = await SharedDependencies.SoundCloudApiClient.GetByPermaLinkAsync("https://soundcloud.com/test/test", default);

        Assert.IsNull(response);
    }
}