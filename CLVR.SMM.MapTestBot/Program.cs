using CLVR.SMM.MapTestBot;
using CLVR.SMM.MapTestBot.Configuration;
using CLVR.SMM.MapTestBot.Services;
using Microsoft.Extensions.Options;
using MongoDB.Driver;

var builder = Host.CreateApplicationBuilder(args);

builder.Services
    .AddOptions<MongoOptions>()
    .Bind(builder.Configuration.GetSection(MongoOptions.SectionName))
    .ValidateDataAnnotations()
    .ValidateOnStart();

builder.Services
    .AddOptions<PavlovRconOptions>()
    .Bind(builder.Configuration.GetSection(PavlovRconOptions.SectionName))
    .ValidateDataAnnotations()
    .ValidateOnStart();

builder.Services.AddSingleton<IMongoClient>(serviceProvider =>
{
    var options = serviceProvider.GetRequiredService<IOptions<MongoOptions>>().Value;
    return new MongoClient(options.ConnectionString);
});

builder.Services.AddSingleton<IMapTestService, MapTestService>();
builder.Services.AddHostedService<Worker>();

var host = builder.Build();
host.Run();
