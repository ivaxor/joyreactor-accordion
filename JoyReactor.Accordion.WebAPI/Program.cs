using JoyReactor.Accordion.Logic.ApiClient;
using JoyReactor.Accordion.Logic.Crawlers;
using JoyReactor.Accordion.Logic.Media;
using JoyReactor.Accordion.Logic.Onnx;
using JoyReactor.Accordion.Logic.Parsers;
using JoyReactor.Accordion.WebAPI.Auth;
using JoyReactor.Accordion.WebAPI.BackgroudServices;
using JoyReactor.Accordion.WebAPI.BackgroudServices.CatchUps;
using JoyReactor.Accordion.WebAPI.BackgroudServices.Publishers;
using JoyReactor.Accordion.WebAPI.Consumers;
using JoyReactor.Accordion.WebAPI.Controllers;
using JoyReactor.Accordion.WebAPI.Extensions;
using JoyReactor.Accordion.WebAPI.Models;
using MassTransit;
using Microsoft.AspNetCore.HttpOverrides;
using Microsoft.Extensions.Options;
using Scalar.AspNetCore;
using Serilog;
using System.Collections.Frozen;
using System.Reflection;
using System.Text;
using Telegram.Bot;

Console.OutputEncoding = Encoding.UTF8;

var builder = WebApplication.CreateBuilder(args);
builder.AddLogging();
builder.AddOptionsFromConfiguration();
builder.AddDatabases();
builder.AddInferenceSession();
builder.AddRateLimiter();
builder.AddHealthChecks();

builder.Services.AddMassTransit(busConfigurator =>
{
    busConfigurator.UsingRabbitMq((context, rabbitConfigurator) =>
    {
        var rabbitMqSetting = builder.Configuration.GetSection(nameof(RabbitMqSettings)).Get<RabbitMqSettings>();

        rabbitConfigurator.Host(rabbitMqSetting.Host, "/", rabbitHostConfigurator =>
        {
            rabbitHostConfigurator.Username(rabbitMqSetting.UserName);
            rabbitHostConfigurator.Password(rabbitMqSetting.Password);
        });

        rabbitConfigurator.ConfigureEndpoints(context);
    });

    var consumersSettings = builder.Configuration.GetSection(nameof(ConsumersSettings)).Get<ConsumersSettings>();

    if (consumersSettings.ConsumersEnabled[nameof(ApiPostCreatedConsumer)]) busConfigurator.AddConsumer<ApiPostCreatedConsumer, ApiPostCreatedConsumerDefinition>();
    if (consumersSettings.ConsumersEnabled[nameof(PostPictureCreatedConsumer)]) busConfigurator.AddConsumer<PostPictureCreatedConsumer, PostPictureCreatedConsumerDefinition>();
    if (consumersSettings.ConsumersEnabled[nameof(VectorCreatedConsumer)]) busConfigurator.AddConsumer<VectorCreatedConsumer, VectorCreatedConsumerDefinition>();
    if (consumersSettings.ConsumersEnabled[nameof(VoteCreatedConsumer)]) busConfigurator.AddConsumer<VoteCreatedConsumer, VoteCreatedConsumerDefinition>();
});

builder.Services.Configure<ForwardedHeadersOptions>(options =>
{
    options.ForwardedHeaders = ForwardedHeaders.XForwardedFor | ForwardedHeaders.XForwardedProto;
    options.KnownIPNetworks.Clear();
    options.KnownProxies.Clear();
    options.ForwardLimit = 5;
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
    .AddHttpClient<SearchMediaController>(httpClient =>
    {
        httpClient.DefaultRequestHeaders.Add("User-Agent", userAgent);
    });

builder.Services
    .AddHttpClient(nameof(TelegramBotClient))
    .AddTypedClient<ITelegramBotClient>((httpClient, serviceProvider) =>
    {
        var settings = serviceProvider.GetRequiredService<IOptions<TelegramBotSettings>>();
        return new TelegramBotClient(settings.Value.Token, httpClient);
    });

builder.Services.AddSingleton<IApiClientProvider, ApiClientProvider>();
builder.Services.AddSingleton<ITagClient, TagClient>();
builder.Services.AddScoped<ITagCrawler, TagCrawler>();
builder.Services.AddSingleton<IPostClient, PostClient>();
builder.Services.AddScoped<IPostParser, PostParser>();
builder.Services.AddSingleton<IMediaReducer, MediaReducer>();
builder.Services.AddSingleton<IOnnxVectorConverter, OnnxVectorConverter>();
builder.Services.AddSingleton<IChangedPostClient, ChangedPostClient>();

builder.Services.AddHostedService<PostPictureCreatedCatchUp>();
builder.Services.AddHostedService<VectorCreatedCatchUp>();

builder.Services.AddHostedService<ChangedApiPostPublisher>();
builder.Services.AddHostedService<CrawlerApiPostPublisher>();

builder.Services.AddHostedService<DuplicatePictureDetector>();
builder.Services.AddHostedService<DuplicateVoteCleaner>();
builder.Services.AddHostedService<MediaToVectorConverter>();
builder.Services.AddHostedService<RootTagsCrawler>();
builder.Services.AddHostedService<TagSubTagsCrawler>();
builder.Services.AddHostedService<TelegramBotReceiver>();
builder.Services.AddHostedService<TelegramBotSender>();
builder.Services.AddHostedService<VectorNormalizator>();

builder.Services.AddHostedService<EmptyPostEmbedsFixer>();

builder.Services.AddMemoryCache();
builder.Services.AddAuthentication()
    .AddScheme<ApiKeyAuthenticationSchemeOptions, ApiKeyAuthenticationSchemeHandler>("ApiKeyAuthenticationScheme", options =>
    {
        var authSettings = builder.Configuration.GetSection(nameof(AuthSettings)).Get<AuthSettings>();
        options.ApiKeys = authSettings.ApiKeys.ToFrozenSet(StringComparer.OrdinalIgnoreCase);
    });
builder.Services.AddAuthorization();
builder.Services.AddControllers();
builder.Services.AddOpenApi();
builder.Services.AddCors(options =>
{
    // TODO: Update with settings for prod
    options.AddDefaultPolicy(builder => builder
        .AllowAnyHeader()
        .AllowAnyOrigin()
        .AllowAnyMethod());
});

var app = builder.Build();
app.UseForwardedHeaders();
app.UseSerilogRequestLogging();
app.UseCors();

if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
    app.MapScalarApiReference();
    app.UseHttpsRedirection();
}
else
{
    //app.UseHttpsRedirection();
    //app.UseHsts();
    app.UseRateLimiter();
}

app.UseAuthentication();
app.UseAuthorization();
app.MapControllers();
app.MapHealthChecks("/healthz");

app.Run();