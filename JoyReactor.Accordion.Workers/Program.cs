using GraphQL.Client.Abstractions;
using GraphQL.Client.Http;
using GraphQL.Client.Serializer.SystemTextJson;
using JoyReactor.Accordion.Database;
using JoyReactor.Accordion.Logic.ApiClient;
using JoyReactor.Accordion.Logic.Media.Images;
using JoyReactor.Accordion.Logic.Onnx;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using Microsoft.ML.OnnxRuntime;
using Qdrant.Client;
using Qdrant.Client.Grpc;
using System.Text.Json;

var configuration = new ConfigurationBuilder()
    .SetBasePath(AppContext.BaseDirectory)
    .AddEnvironmentVariables()
    .AddJsonFile("appsettings.json", optional: false, reloadOnChange: true)
    .Build();

var services = new ServiceCollection();
services.AddSingleton<IConfiguration>(configuration);
services.Configure<ApiClientSettings>(configuration.GetSection(nameof(ApiClientSettings)));
services.Configure<ImageSettings>(configuration.GetSection(nameof(ImageSettings)));
services.Configure<OnnxSettings>(configuration.GetSection(nameof(OnnxSettings)));
services.Configure<QdrantSettings>(configuration.GetSection(nameof(QdrantSettings)));

services.AddLogging();
services.AddHttpClient();

services.AddSingleton(serviceProvider => new JsonSerializerOptions() { });

services.AddSingleton<IGraphQLClient, GraphQLHttpClient>(serviceProvider =>
{
    var settings = serviceProvider.GetRequiredService<IOptions<ApiClientSettings>>();
    return new GraphQLHttpClient(settings.Value.GraphQlEndpointUrl, new SystemTextJsonSerializer());
});

services.AddSingleton<IQdrantClient, QdrantClient>(serviceProvider =>
{
    var settings = serviceProvider.GetRequiredService<IOptions<QdrantSettings>>();
    var client = new QdrantClient(settings.Value.Host);

    var isCollectionExists = client.CollectionExistsAsync(settings.Value.CollectionName).GetAwaiter().GetResult();
    if (!isCollectionExists)
    {
        client.CreateCollectionAsync(
            settings.Value.CollectionName,
            new VectorParams
            {
                Size = settings.Value.CollectionVectorSize,
                Distance = Distance.Cosine,
                Datatype = Datatype.Float32,
            },
            quantizationConfig: new QuantizationConfig
            {
                Scalar = new ScalarQuantization
                {
                    Type = QuantizationType.Int8,
                    AlwaysRam = true,
                }
            }).GetAwaiter().GetResult();

        //client.CreatePayloadIndexAsync("joy_reactor_assets", "post_id", PayloadSchemaType.Integer);
    }

    return client;
});

services.AddSingleton(serviceProvider =>
{
    var settings = serviceProvider.GetRequiredService<IOptions<OnnxSettings>>();

    var options = new SessionOptions();
    if (settings.Value.UseCpu)
        options.AppendExecutionProvider_CPU();
    if (settings.Value.UseCuda)
        options.AppendExecutionProvider_CUDA(int.Parse(settings.Value.DeviceId));
    if (settings.Value.UseRocm)
        options.AppendExecutionProvider_ROCm(int.Parse(settings.Value.DeviceId));
    if (settings.Value.UseOpenVino)
        options.AppendExecutionProvider_OpenVINO(settings.Value.DeviceId);

    return new InferenceSession(settings.Value.ModelPath, options);
});

services.AddSingleton<IApiClient, ApiClient>();
services.AddSingleton<IImageDownloader, ImageDownloader>();
services.AddSingleton<IImageReducer, ImageReducer>();
services.AddSingleton<IOnnxVectorConverter, OnnxVectorConverter>();
services.AddSingleton<IDatabaseWrapper, DatabaseWrapper>();

var serviceProvider = services.BuildServiceProvider();

var apiClient = serviceProvider.GetRequiredService<IApiClient>();
var post = await apiClient.GetPostInformationAsync(6234782);

var imageDownloader = serviceProvider.GetRequiredService<IImageDownloader>();
using var image = await imageDownloader.DownloadAsync(post.Value.Attributes.First());

var imageOnnxVectorConverter = serviceProvider.GetRequiredService<IOnnxVectorConverter>();
var vector = await imageOnnxVectorConverter.Convert(image.Value);

var databaseWrapper = serviceProvider.GetRequiredService<IDatabaseWrapper>();
await databaseWrapper.InsertAsync(vector);

Console.WriteLine(vector.Length);