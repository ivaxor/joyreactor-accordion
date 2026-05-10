using JoyReactor.Accordion.Tests.Helpers;
using Microsoft.Extensions.Hosting;

namespace JoyReactor.Accordion.Tests;

[TestClass]
public sealed class BandCampApiClientTests
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
    [DataRow("https://lbro.bandcamp.com/album/romance-dawn", "a", 3137819895)]
    [DataRow("https://procrastinatorprogress.bandcamp.com/track/--2", "t", 1389197581)]
    public async Task GetInfoAsync(string url, string type, long id)
    {
        var response = await DependencyProvider.BandCampApiClient.GetInfoAsync(url, default);

        Assert.AreEqual(type, response.Type);
        Assert.AreEqual(id, response.Id);
    }

    [TestMethod]
    public async Task GetInfoAsync_NonExisting()
    {
        var response = await DependencyProvider.BandCampApiClient.GetInfoAsync("https://test.bandcamp.com/album/test", default);

        Assert.IsNull(response);
    }
}