using JoyReactor.Accordion.Logic.ApiClient;
using JoyReactor.Accordion.Logic.BandCamp;
using JoyReactor.Accordion.Logic.Crawlers;
using JoyReactor.Accordion.Logic.Database.Sql;
using JoyReactor.Accordion.Logic.Media;
using JoyReactor.Accordion.Logic.Onnx;
using JoyReactor.Accordion.Logic.Parsers;
using JoyReactor.Accordion.Logic.SoundCloud;
using JoyReactor.Accordion.WebAPI.Extensions;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Serilog;
using SixLabors.ImageSharp;
using System.Reflection;

namespace JoyReactor.Accordion.Tests.Helpers;

public static class TestHostApplicationBuilder
{
    public static HostApplicationBuilder CreateInMemory()
    {
        var builder = Host.CreateEmptyApplicationBuilder(new HostApplicationBuilderSettings() { EnvironmentName = "Development" });

        var userAgent = $"JoyReactor.Accordion/{Assembly.GetEntryAssembly()?.GetName().Version?.ToString(3)} (Bot; +https://github.com/ivaxor/joyreactor-accordion)";

        builder.Services.AddHttpClient();
        builder.Services
            .AddHttpClient<IMediaDownloader, MediaDownloader>(httpClient =>
            {
                httpClient.DefaultRequestHeaders.Add("Referer", "https://joyreactor.cc/");
                httpClient.DefaultRequestHeaders.Add("User-Agent", userAgent);
            });
        builder.Services
            .AddHttpClient<IBandCampApiClient, BandCampApiClient>(httpClient =>
            {
                httpClient.DefaultRequestHeaders.Add("User-Agent", userAgent);
            });
        builder.Services
            .AddHttpClient<ISoundCloudApiClient, SoundCloudApiClient>(httpClient =>
            {
                httpClient.DefaultRequestHeaders.Add("User-Agent", userAgent);
            });

        var sqliteConnection = new SqliteConnection("DataSource=:memory:");
        builder.Services.AddDbContext<SqlDatabaseContext>(options =>
        {
            sqliteConnection.Open();
            options.UseSqlite(sqliteConnection);
        });

        builder.Services.Configure<ApiClientSettings>(options =>
        {
            options.MaxRetryAttempts = 10;
            options.SubsequentCallDelay = TimeSpan.FromSeconds(2.5);
        });

        builder.Services.Configure<MediaSettings>(options =>
        {
            options.CdnHostNames = Enumerable.Range(0, 10).Select(i => $"https://img{i}.joyreactor.cc").ToArray();
            options.BatchSize = 10;
            options.SubsequentBatchDelay = TimeSpan.FromSeconds(1);
            options.SubsequentCallDelay = TimeSpan.FromSeconds(1);
            options.ResizedSize = 224;
            options.MaxRetryAttempts = 10;
            options.RetryDelay = TimeSpan.FromSeconds(5);
        });

        builder.Services.AddSingleton<IMediaDownloader, MediaDownloader>();

        builder.Services.AddSingleton<IApiClientProvider, ApiClientProvider>();
        builder.Services.AddSingleton<ITagClient, TagClient>();
        builder.Services.AddSingleton<ITagCrawler, TagCrawler>();
        builder.Services.AddSingleton<IPostClient, PostClient>();
        builder.Services.AddSingleton<IPostParser, PostParser>();
        builder.Services.AddSingleton<IMediaReducer, MediaReducer>();
        builder.Services.AddSingleton<IOnnxVectorConverter, OnnxVectorConverter>();
        builder.Services.AddSingleton<IChangedPostClient, ChangedPostClient>();
        builder.Services.AddSingleton<ISoundCloudApiClient, SoundCloudApiClient>();

        builder.Services.AddMemoryCache();

        return builder;
    }

    public static HostApplicationBuilder CreateReal()
    {
        var builder = Host.CreateApplicationBuilder(new HostApplicationBuilderSettings() { EnvironmentName = "Development" });

        builder.Services.AddSerilog((IServiceProvider serviceProvider, LoggerConfiguration configuration) =>
        {
            configuration
                .ReadFrom.Configuration(builder.Configuration)
                .Enrich.FromLogContext()
                .Enrich.WithMachineName()
                .WriteTo.Console()
                .WriteTo.Debug();
        });

        var userAgent = $"JoyReactor.Accordion/{Assembly.GetEntryAssembly()?.GetName().Version?.ToString(3)} (Bot; +https://github.com/ivaxor/joyreactor-accordion)";

        builder.Services.AddHttpClient();
        builder.Services
            .AddHttpClient<IMediaDownloader, MediaDownloader>(httpClient =>
            {
                httpClient.DefaultRequestHeaders.Add("Referer", "https://joyreactor.cc/");
                httpClient.DefaultRequestHeaders.Add("User-Agent", userAgent);
            });
        builder.Services
            .AddHttpClient<IBandCampApiClient, BandCampApiClient>(httpClient =>
            {
                httpClient.DefaultRequestHeaders.Add("User-Agent", userAgent);
            });
        builder.Services
            .AddHttpClient<ISoundCloudApiClient, SoundCloudApiClient>(httpClient =>
            {
                httpClient.DefaultRequestHeaders.Add("User-Agent", userAgent);
            });

        HostApplicationBuilderExtensions.AddOptionsFromConfiguration(builder);
        HostApplicationBuilderExtensions.AddDatabases(builder);
        HostApplicationBuilderExtensions.AddInferenceSession(builder);

        builder.Services.AddSingleton<IApiClientProvider, ApiClientProvider>();
        builder.Services.AddSingleton<ITagClient, TagClient>();
        builder.Services.AddScoped<ITagCrawler, TagCrawler>();
        builder.Services.AddSingleton<IPostClient, PostClient>();
        builder.Services.AddScoped<IPostParser, PostParser>();
        builder.Services.AddSingleton<IMediaReducer, MediaReducer>();
        builder.Services.AddSingleton<IOnnxVectorConverter, OnnxVectorConverter>();
        builder.Services.AddSingleton<IChangedPostClient, ChangedPostClient>();
        builder.Services.AddSingleton<IBandCampApiClient, BandCampApiClient>();
        builder.Services.AddSingleton<ISoundCloudApiClient, SoundCloudApiClient>();

        builder.Services.AddMemoryCache();

        return builder;
    }
}