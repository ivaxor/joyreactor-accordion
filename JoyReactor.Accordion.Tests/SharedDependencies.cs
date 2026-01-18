using GraphQL.Client.Http;
using GraphQL.Client.Serializer.SystemTextJson;
using JoyReactor.Accordion.Logic.ApiClient;
using JoyReactor.Accordion.Logic.Database.Sql;
using JoyReactor.Accordion.Logic.Database.Sql.Entities;
using JoyReactor.Accordion.Logic.Media;
using JoyReactor.Accordion.Logic.Parsers;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;

namespace JoyReactor.Accordion.Tests;

public static class SharedDependencies
{
    private static readonly IServiceProvider ServiceProvider;

    public static SqlDatabaseContext SqlDatabaseContext => ServiceProvider.GetRequiredService<SqlDatabaseContext>();
    public static ApiClientProvider ApiClientProvider => ServiceProvider.GetRequiredService<ApiClientProvider>();
    public static TagClient TagClient => ServiceProvider.GetRequiredService<TagClient>();
    public static PostClient PostClient => ServiceProvider.GetRequiredService<PostClient>();
    public static PostParser PostParser => ServiceProvider.GetRequiredService<PostParser>();
    public static MediaReducer MediaReducer => ServiceProvider.GetRequiredService<MediaReducer>();
    public static MediaDownloader MediaDownloader => ServiceProvider.GetRequiredService<MediaDownloader>();
    public static Api Api { get; }

    static SharedDependencies()
    {
        var services = new ServiceCollection();

        services.AddSingleton<ILoggerFactory, NullLoggerFactory>();
        services.AddSingleton(typeof(ILogger<>), typeof(NullLogger<>));

        services.AddHttpClient();

        services.AddDbContext<SqlDatabaseContext>(options => options.UseInMemoryDatabase(nameof(SqlDatabaseContext)));

        services.Configure<ApiClientSettings>(options =>
        {
            options.MaxRetryAttempts = 10;
            options.SubsequentCallDelay = TimeSpan.FromSeconds(2.5);
        });

        services.Configure<MediaSettings>(options =>
        {
            options.CdnDomainNames = [
                "https://img0.joyreactor.cc",
                "https://img1.joyreactor.cc",
                "https://img2.joyreactor.cc",
                "https://img10.joyreactor.cc"
            ];
            options.ResizedSize = 224;
            options.MaxRetryAttempts = 10;
            options.RetryDelay = TimeSpan.FromMinutes(2.5);
            options.ConcurrentDownloads = 10;
        });

        services.AddSingleton<ApiClientProvider>();
        services.AddSingleton<TagClient>();
        services.AddSingleton<PostClient>();
        services.AddSingleton<PostParser>();
        services.AddSingleton<MediaReducer>();
        services.AddSingleton<MediaDownloader>();

        ServiceProvider = services.BuildServiceProvider();

        Api = SqlDatabaseContext.Apis.First();
    }
}