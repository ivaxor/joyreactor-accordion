using JoyReactor.Accordion.Logic.ApiClient;
using JoyReactor.Accordion.Logic.Database.Vector;
using JoyReactor.Accordion.Logic.Media.Images;
using JoyReactor.Accordion.Logic.Onnx;
using JoyReactor.Accordion.Logic.Parsers;
using JoyReactor.Accordion.WebAPI.BackgroudServices;
using JoyReactor.Accordion.WebAPI.Extensions;
using Microsoft.EntityFrameworkCore;

var builder = WebApplication.CreateBuilder(args);
builder.AddOptionsFromConfiguration();
builder.AddGraphQlClient();
builder.AddDatabases();
builder.AddInferenceSession();
builder.AddRateLimiter();
builder.AddHealthChecks();

builder.Services.AddHttpClient();

builder.Services.AddSingleton<IApiClient, ApiClient>();
builder.Services.AddSingleton<ITagClient, TagClient>();
builder.Services.AddSingleton<IPostClient, PostClient>();

builder.Services.AddScoped<IPostParser, PostParser>();

builder.Services.AddSingleton<IImageDownloader, ImageDownloader>();
builder.Services.AddHttpClient<IImageDownloader, ImageDownloader>(httpClient =>
{
    httpClient.DefaultRequestHeaders.Add("Referer", "https://joyreactor.cc");
    httpClient.Timeout = TimeSpan.FromSeconds(10);
});

builder.Services.AddSingleton<IOnnxVectorConverter, OnnxVectorConverter>();
builder.Services.AddSingleton<IVectorDatabaseContext, VectorDatabaseContext>();

builder.Services.AddScopedHostedService<MainTagsCrawler>();
builder.Services.AddScopedHostedService<UnparsedSubTagsCrawler>();
//builder.Services.AddScopedHostedService<TopWeekPostsCrawler>();
builder.Services.AddScopedHostedService<UnprocessedPictureVectorCrawler>();

builder.Services.AddControllers();
builder.Services.AddOpenApi();

var app = builder.Build();

if (app.Environment.IsDevelopment())
    app.MapOpenApi();

app.UseHttpsRedirection();
app.UseAuthorization();
app.MapControllers();
app.MapHealthChecks("/healthz");

app.Run();
