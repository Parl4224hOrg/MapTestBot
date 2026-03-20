using CLVR.SMM.MapTestBot.Commands;
using CLVR.SMM.MapTestBot.Configuration;
using CLVR.SMM.MapTestBot.Services;
using Microsoft.Extensions.Options;
using MongoDB.Driver;
using NetCord;
using NetCord.Gateway;
using NetCord.Hosting.Gateway;
using NetCord.Hosting.Services;
using NetCord.Hosting.Services.ApplicationCommands;
using NetCord.Services.ApplicationCommands;

// Work around DNS/socket failures in some environments where IPv6-mapped DNS
// servers (for example ::ffff:127.0.2.x) are reported but not usable.
AppContext.SetSwitch("System.Net.DisableIPv6", true);

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
    var mongoUrlBuilder = new MongoUrlBuilder(options.ConnectionString);

    var mongoClientSettings = MongoClientSettings.FromUrl(mongoUrlBuilder.ToMongoUrl());
    return new MongoClient(mongoClientSettings);
});

var discordIds = builder.Configuration
    .GetSection("DiscordIds")
    .Get<DiscordIds>() ?? throw new Exception("DiscordIds not found in configuration");
builder.Services.AddSingleton(discordIds);

var pavlovRconOptions = builder.Configuration
    .GetSection("PavlovRcon")
    .Get<PavlovRconOptions>() ?? throw new Exception("PavlovRcon not found in configuration");
builder.Services.AddSingleton(pavlovRconOptions);

builder.Services.AddSingleton<IMapTestService, MapTestService>();

builder.Services
    .AddDiscordGateway(options => { options.Intents = GatewayIntents.Guilds | GatewayIntents.GuildUsers; })
    .AddApplicationCommands<SlashCommandInteraction, SlashCommandContext>(options =>
    {
        options.ResultHandler = new ApplicationCommandResultHandler<SlashCommandContext>(MessageFlags.Ephemeral);
    });

var host = builder.Build();

host.AddModules(typeof(Commands).Assembly);

host.Run();
