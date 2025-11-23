using cmsContentManagement.Application.Common.Settings;
using cmsContentManagement.Application.Interfaces;
using cmsContentManagement.Middleware;
using cmsContentManagment.Infrastructure.Persistance;
using cmsContentManagment.Infrastructure.Repositories;
using Elastic.Clients.Elasticsearch;
using Elastic.Transport;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();
string? redisOptionsConfiguration = builder.Configuration["Redis:Connection"];
string? connectionString = builder.Configuration.GetConnectionString("DefaultConnection");

builder.Services.AddDbContext<AppDbContext>(options =>
    options.UseMySql(connectionString, ServerVersion.AutoDetect(connectionString))
);


builder.Services.AddStackExchangeRedisCache(redisOptions =>
{
    redisOptions.Configuration = redisOptionsConfiguration;
});

builder.Services.Configure<ElasticSettings>(builder.Configuration.GetSection("ElasticSettings"));
builder.Services.AddSingleton(sp =>
{
    var options = sp.GetRequiredService<IOptions<ElasticSettings>>().Value;
    if (string.IsNullOrWhiteSpace(options.Url))
    {
        throw new InvalidOperationException("ElasticSettings:Url is not configured");
    }

    var clientSettings = new ElasticsearchClientSettings(new Uri(options.Url))
        .DefaultIndex(string.IsNullOrWhiteSpace(options.DefaultIndex) ? "content" : options.DefaultIndex);

    if (!string.IsNullOrWhiteSpace(options.Username) && !string.IsNullOrWhiteSpace(options.Password))
    {
        clientSettings = clientSettings.Authentication(new BasicAuthentication(options.Username, options.Password));
    }

    return new ElasticsearchClient(clientSettings);
});


builder.Services.AddControllers();
builder.Services.AddScoped<IContentManagmentService, ContentManagmentService>();

var app = builder.Build();
app.UseMiddleware<ErrorHandlingMiddleware>();


app.UseSwagger();
app.UseSwaggerUI();

app.UseAuthorization();
app.UseAuthentication();

app.UseHttpsRedirection();
app.MapControllers();
app.Run();
