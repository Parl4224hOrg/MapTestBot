using System.Numerics;
using CLVR.SMM.MapTestBot.Configuration;
using CLVR.SMM.MapTestBot.Models;
using CLVR.SMM.MapTestBot.Services.Results;
using Microsoft.Extensions.Options;
using MongoDB.Bson;
using MongoDB.Driver;
using PavlovRcon.Commands;
using PavlovRcon.Config;
using PavlovRcon.RconClient;

namespace CLVR.SMM.MapTestBot.Services;

public sealed class MapTestService : IMapTestService
{
    private const string QueueId = "SND";
    private static readonly TimeSpan PlaytestWindow = TimeSpan.FromHours(1);

    private readonly IMongoCollection<UserDocument> _users;
    private readonly IMongoCollection<ServerDocument> _servers;
    private readonly IMongoCollection<StatsDocument> _stats;
    private readonly IMongoCollection<MapTestDocument> _mapTests;
    private readonly PavlovRconOptions _pavlovRconOptions;

    public MapTestService(IMongoClient mongoClient, IOptions<MongoOptions> mongoOptions, IOptions<PavlovRconOptions> pavlovRconOptions)
    {
        var database = mongoClient.GetDatabase(mongoOptions.Value.DatabaseName);

        _users = database.GetCollection<UserDocument>(mongoOptions.Value.UsersCollectionName);
        _servers = database.GetCollection<ServerDocument>(mongoOptions.Value.ServersCollectionName);
        _stats = database.GetCollection<StatsDocument>(mongoOptions.Value.StatsCollectionName);
        _mapTests = database.GetCollection<MapTestDocument>(mongoOptions.Value.MapTestsCollectionName);
        _pavlovRconOptions = pavlovRconOptions.Value;
    }

    public async Task<SwitchMapResult> SwitchMapAsync(string discordId, string mapUgc, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(discordId))
        {
            return new SwitchMapResult(false, "Discord id is required.");
        }

        if (string.IsNullOrWhiteSpace(mapUgc))
        {
            return new SwitchMapResult(false, "Map UGC is required.");
        }

        var minimumUnixTimeMilliseconds = DateTimeOffset.UtcNow.Subtract(PlaytestWindow).ToUnixTimeMilliseconds();

        var playtest = await _mapTests.Find(mapTest =>
                mapTest.Owner == discordId &&
                !mapTest.Deleted &&
                mapTest.ServerClaimed &&
                mapTest.Time >= minimumUnixTimeMilliseconds)
            .SortByDescending(mapTest => mapTest.Time)
            .FirstOrDefaultAsync(cancellationToken);

        if (playtest is null)
        {
            return new SwitchMapResult(false, "No active playtest owned by that Discord id was found in the past hour.");
        }

        var server = await _servers.Find(server =>
                server.Reserved &&
                server.ReservedBy == discordId)
            .FirstOrDefaultAsync(cancellationToken);

        if (server is null)
        {
            return new SwitchMapResult(false, "No reserved server was found for that Discord id.");
        }

        var connectionInfo = new RconConnectionInfo(server.Ip, server.Port, _pavlovRconOptions.Password);
        var clientOptions = new RconClientOptions
        {
            CommandTimeout = TimeSpan.FromSeconds(_pavlovRconOptions.CommandTimeoutSeconds)
        };

        await using var client = new RconClient(connectionInfo, clientOptions);
        var response = await client.SwitchMap(mapUgc.Trim(), null, cancellationToken);

        return response.SwitchMap
            ? new SwitchMapResult(true, "Map switched successfully.", server.Id, server.Name)
            : new SwitchMapResult(false, "Server rejected the map switch.", server.Id, server.Name);
    }

    public async Task<AutoBalanceResult> AutoBalanceAsync(IReadOnlyCollection<string> memberDiscordIds, CancellationToken cancellationToken = default)
    {
        if (memberDiscordIds.Count != 10)
        {
            return AutoBalanceResult.Failed("Autobalance requires exactly 10 members.");
        }

        var normalizedIds = memberDiscordIds
            .Where(static id => !string.IsNullOrWhiteSpace(id))
            .Select(static id => id.Trim())
            .Distinct(StringComparer.Ordinal)
            .ToArray();

        if (normalizedIds.Length != 10)
        {
            return AutoBalanceResult.Failed("Autobalance requires 10 unique Discord ids.");
        }

        var users = await _users.Find(user => normalizedIds.Contains(user.DiscordId))
            .ToListAsync(cancellationToken);

        if (users.Count != 10)
        {
            return AutoBalanceResult.Failed("All 10 members must exist in the user database.");
        }

        var userIds = users
            .Select(user => ObjectId.Parse(user.MongoId))
            .ToArray();

        var stats = await _stats.Find(stat =>
                stat.QueueId == QueueId &&
                userIds.Contains(stat.UserId))
            .ToListAsync(cancellationToken);

        var statsByUserId = stats.ToDictionary(stat => stat.UserId, stat => stat);

        var players = new List<BalancedPlayer>(capacity: 10);

        foreach (var user in users)
        {
            var mongoId = ObjectId.Parse(user.MongoId);
            if (!statsByUserId.TryGetValue(mongoId, out var stat))
            {
                return AutoBalanceResult.Failed("All 10 members must have SND MMR data.");
            }

            players.Add(new BalancedPlayer(user.DiscordId, user.Name, stat.Mmr));
        }

        var bestSplit = FindBestSplit(players);
        return new AutoBalanceResult(
            true,
            "Teams generated successfully.",
            bestSplit.TeamOne,
            bestSplit.TeamTwo,
            bestSplit.TeamOne.Sum(static player => player.Mmr),
            bestSplit.TeamTwo.Sum(static player => player.Mmr));
    }

    private static (IReadOnlyList<BalancedPlayer> TeamOne, IReadOnlyList<BalancedPlayer> TeamTwo) FindBestSplit(IReadOnlyList<BalancedPlayer> players)
    {
        var totalMmr = players.Sum(static player => player.Mmr);
        var target = totalMmr / 2d;

        List<BalancedPlayer>? bestTeamOne = null;
        var bestMask = 0;
        var bestDifference = double.MaxValue;

        var combinationLimit = 1 << players.Count;
        for (var mask = 0; mask < combinationLimit; mask++)
        {
            if (BitOperations.PopCount((uint)mask) != players.Count / 2)
            {
                continue;
            }

            var teamOne = new List<BalancedPlayer>(capacity: players.Count / 2);
            var teamOneMmr = 0;

            for (var index = 0; index < players.Count; index++)
            {
                if ((mask & (1 << index)) == 0)
                {
                    continue;
                }

                teamOne.Add(players[index]);
                teamOneMmr += players[index].Mmr;
            }

            var difference = Math.Abs(teamOneMmr - target);
            if (difference >= bestDifference)
            {
                continue;
            }

            bestDifference = difference;
            bestMask = mask;
            bestTeamOne = teamOne;
        }

        bestTeamOne ??= players.Take(players.Count / 2).ToList();

        var teamTwo = new List<BalancedPlayer>(capacity: players.Count / 2);
        for (var index = 0; index < players.Count; index++)
        {
            if ((bestMask & (1 << index)) != 0)
            {
                continue;
            }

            teamTwo.Add(players[index]);
        }

        return (bestTeamOne, teamTwo);
    }
}
