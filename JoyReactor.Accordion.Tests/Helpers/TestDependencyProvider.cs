using JoyReactor.Accordion.Logic.ApiClient;
using JoyReactor.Accordion.Logic.BandCamp;
using JoyReactor.Accordion.Logic.Crawlers;
using JoyReactor.Accordion.Logic.Database.Sql;
using JoyReactor.Accordion.Logic.Database.Sql.Entities;
using JoyReactor.Accordion.Logic.Database.Vector;
using JoyReactor.Accordion.Logic.Media;
using JoyReactor.Accordion.Logic.Onnx;
using JoyReactor.Accordion.Logic.Parsers;
using JoyReactor.Accordion.Logic.SoundCloud;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using Qdrant.Client;

namespace JoyReactor.Accordion.Tests.Helpers;

public record TestDependencyProvider
{
    public TestDependencyProvider(IServiceProvider serviceProvider)
    {
        ServiceProvider = serviceProvider;
        Scope = ServiceProvider.CreateScope();
    }

    protected IServiceProvider ServiceProvider { get; private set; }
    protected IServiceScope Scope { get; private set; }

    public SqlDatabaseContext SqlDatabaseContext => Scope.ServiceProvider.GetRequiredService<SqlDatabaseContext>();
    public IQdrantClient QdrantClient => Scope.ServiceProvider.GetRequiredService<IQdrantClient>();

    public IMediaDownloader MediaDownloader => Scope.ServiceProvider.GetRequiredService<IMediaDownloader>();

    public IApiClientProvider ApiClientProvider => Scope.ServiceProvider.GetRequiredService<IApiClientProvider>();
    public ITagClient TagClient => Scope.ServiceProvider.GetRequiredService<ITagClient>();
    public ITagCrawler TagCrawler => Scope.ServiceProvider.GetRequiredService<ITagCrawler>();
    public IPostClient PostClient => Scope.ServiceProvider.GetRequiredService<IPostClient>();
    public IPostParser PostParser => Scope.ServiceProvider.GetRequiredService<IPostParser>();
    public IMediaReducer MediaReducer => Scope.ServiceProvider.GetRequiredService<IMediaReducer>();
    public IOnnxVectorConverter OnnxVectorConverter => Scope.ServiceProvider.GetRequiredService<IOnnxVectorConverter>();
    public IChangedPostClient ChangedPostClient => Scope.ServiceProvider.GetRequiredService<IChangedPostClient>();
    public IBandCampApiClient BandCampApiClient => Scope.ServiceProvider.GetRequiredService<IBandCampApiClient>();
    public ISoundCloudApiClient SoundCloudApiClient => Scope.ServiceProvider.GetRequiredService<ISoundCloudApiClient>();

    public IOptions<QdrantSettings> QdrantSettings => Scope.ServiceProvider.GetRequiredService<IOptions<QdrantSettings>>();

    public Api Api => SqlDatabaseContext.Apis.First();
}