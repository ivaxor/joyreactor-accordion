using JoyReactor.Accordion.Logic.ApiClient;
using JoyReactor.Accordion.Logic.Crawlers;
using JoyReactor.Accordion.Logic.Database.Sql;
using JoyReactor.Accordion.Logic.Database.Sql.Entities;
using JoyReactor.Accordion.Logic.Media;
using JoyReactor.Accordion.Logic.Onnx;
using JoyReactor.Accordion.Logic.Parsers;
using JoyReactor.Accordion.Logic.SoundCloud;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using System.Reflection;

namespace JoyReactor.Accordion.Tests;

public static class SharedDependencyFactory
{
    public static async Task<SharedDependencies> CreateAsync()
    {
        var services = new ServiceCollection();

        services.AddSingleton<ILoggerFactory, NullLoggerFactory>();
        services.AddSingleton(typeof(ILogger<>), typeof(NullLogger<>));

        var userAgent = $"JoyReactor.Accordion/{Assembly.GetEntryAssembly()?.GetName().Version?.ToString(3)} (Bot; +https://github.com/ivaxor/joyreactor-accordion)";

        services.AddHttpClient();
        services
            .AddHttpClient<IMediaDownloader, MediaDownloader>(httpClient =>
            {
                httpClient.DefaultRequestHeaders.Add("Referer", "https://joyreactor.cc/");
                httpClient.DefaultRequestHeaders.Add("User-Agent", userAgent);
            });
        services
            .AddHttpClient<ISoundCloudApiClient, SoundCloudApiClient>(httpClient =>
            {
                httpClient.DefaultRequestHeaders.Add("User-Agent", userAgent);
            });

        var sqliteConnection = new SqliteConnection("DataSource=:memory:");
        services.AddDbContext<SqlDatabaseContext>(options =>
        {
            sqliteConnection.Open();
            options.UseSqlite(sqliteConnection);
        });

        services.Configure<ApiClientSettings>(options =>
        {
            options.MaxRetryAttempts = 10;
            options.SubsequentCallDelay = TimeSpan.FromSeconds(2.5);
        });

        services.Configure<MediaSettings>(options =>
        {
            options.CdnHostNames = Enumerable.Range(0, 10).Select(i => $"https://img{i}.joyreactor.cc").ToArray();
            options.BatchSize = 10;
            options.SubsequentBatchDelay = TimeSpan.FromSeconds(1);
            options.SubsequentCallDelay = TimeSpan.FromSeconds(1);
            options.ResizedSize = 224;
            options.MaxRetryAttempts = 10;
            options.RetryDelay = TimeSpan.FromSeconds(5);
        });

        services.AddSingleton<IMediaDownloader, MediaDownloader>();

        services.AddSingleton<IApiClientProvider, ApiClientProvider>();
        services.AddSingleton<ITagClient, TagClient>();
        services.AddSingleton<ITagCrawler, TagCrawler>();
        services.AddSingleton<IPostClient, PostClient>();
        services.AddSingleton<IPostParser, PostParser>();
        services.AddSingleton<IMediaReducer, MediaReducer>();
        services.AddSingleton<IOnnxVectorConverter, OnnxVectorConverter>();
        services.AddSingleton<IChangedPostClient, ChangedPostClient>();
        services.AddSingleton<ISoundCloudApiClient, SoundCloudApiClient>();

        services.AddMemoryCache();

        var serviceProvider = services.BuildServiceProvider();

        var sharedDependencies = new SharedDependencies() { ServiceProvider = serviceProvider };
        await sharedDependencies.SqlDatabaseContext.Database.EnsureCreatedAsync();

        return sharedDependencies;
    }
}

public record SharedDependencies : IAsyncDisposable
{
    public required ServiceProvider ServiceProvider { get; init; }

    public SqlDatabaseContext SqlDatabaseContext => ServiceProvider.GetRequiredService<SqlDatabaseContext>();

    public IMediaDownloader MediaDownloader => ServiceProvider.GetRequiredService<IMediaDownloader>();

    public IApiClientProvider ApiClientProvider => ServiceProvider.GetRequiredService<IApiClientProvider>();
    public ITagClient TagClient => ServiceProvider.GetRequiredService<ITagClient>();
    public ITagCrawler TagCrawler => ServiceProvider.GetRequiredService<ITagCrawler>();
    public IPostClient PostClient => ServiceProvider.GetRequiredService<IPostClient>();
    public IPostParser PostParser => ServiceProvider.GetRequiredService<IPostParser>();
    public IMediaReducer MediaReducer => ServiceProvider.GetRequiredService<IMediaReducer>();
    public IOnnxVectorConverter OnnxVectorConverter => ServiceProvider.GetRequiredService<IOnnxVectorConverter>();
    public IChangedPostClient ChangedPostClient => ServiceProvider.GetRequiredService<IChangedPostClient>();
    public ISoundCloudApiClient SoundCloudApiClient => ServiceProvider.GetRequiredService<ISoundCloudApiClient>();

    public Api Api => SqlDatabaseContext.Apis.First();

    public async ValueTask DisposeAsync()
    {
        await ServiceProvider.DisposeAsync();
    }
}