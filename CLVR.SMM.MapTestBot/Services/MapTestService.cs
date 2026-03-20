using System.Numerics;
using CLVR.SMM.MapTestBot.Configuration;
using CLVR.SMM.MapTestBot.Models;
using CLVR.SMM.MapTestBot.Services.Results;
using Microsoft.Extensions.Options;
using MongoDB.Bson;
using MongoDB.Driver;
using PavlovRcon.Commands;
using PavlovRcon.Config;
using PavlovRcon.GameData;
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

        var minimumUnixTimeSeconds = DateTimeOffset.UtcNow.Subtract(PlaytestWindow).ToUnixTimeSeconds();

        var playtest = await _mapTests.Find(mapTest =>
                mapTest.Owner == discordId &&
                !mapTest.Deleted &&
                mapTest.ServerClaimed &&
                mapTest.Time >= minimumUnixTimeSeconds)
            .SortByDescending(mapTest => mapTest.Time)
            .FirstOrDefaultAsync(cancellationToken);

        if (playtest is null)
        {
            return new SwitchMapResult(false, "No active playtest owned by that Discord id was found in the past hour.");
        }

        await using var server = await GetServer(playtest.Id);

        if (server is null)
        {
            return new SwitchMapResult(false, "No reserved server was found for that Discord id.");
        }

        await server.UpdateServerName("SMM Playtest", cancellationToken);
        
        var response = await server.SwitchMap(mapUgc.Trim(), GameMode.SearchAndDestroy, cancellationToken);
        
        return response.SwitchMap
            ? new SwitchMapResult(true, "Map switched successfully.")
            : new SwitchMapResult(false, "Server failed the map switch.");
    }

    public async Task<AutoBalanceResult> AutoBalanceAsync(string testerId, CancellationToken cancellationToken = default)
    {
        var minimumUnixTimeSeconds = DateTimeOffset.UtcNow.Subtract(PlaytestWindow).ToUnixTimeSeconds();
        var playtest = await _mapTests.Find(mapTest =>
                mapTest.Owner == testerId &&
                !mapTest.Deleted &&
                mapTest.ServerClaimed &&
                mapTest.Time >= minimumUnixTimeSeconds)
            .SortByDescending(mapTest => mapTest.Time)
            .FirstOrDefaultAsync(cancellationToken);

        if (playtest is null)
        {
            return new AutoBalanceResult(false, "No active playtest owned by that Discord id was found in the past hour.");
        }
        
        await using var server = await GetServer(playtest.Id);

        if (server is null)
        {
            return AutoBalanceResult.Failed("No server was found for the playtest.");
        }

        var onServer = await server.InspectAll();
        
        if (onServer.InspectList.Count != 10)
        {
            return AutoBalanceResult.Failed("Auto balance requires exactly 10 members.");
        }

        var userNames = onServer.InspectList
            .Select(static player => player.UniqueId)
            .ToArray();

        var users = await _users.Find(user => userNames.Contains(user.OculusName))
            .ToListAsync(cancellationToken);

        if (users.Count != 10)
        {
            var missingPlayers = userNames.Except(users.Select(user => user.OculusName));
            return AutoBalanceResult.Failed("Not all users could be found: " + string.Join(", ", missingPlayers) + ".");
        }

        var userIds = users
            .Select(user => ObjectId.Parse(user.MongoId));

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

            players.Add(new BalancedPlayer(user.DiscordId, user.OculusName, stat.Mmr));
        }

        var bestSplit = FindBestSplit(players);

        
        foreach (var player in bestSplit.TeamOne)
        {
            await server.SwitchTeam(player.Name, "0", cancellationToken);
        }
        
        foreach (var player in bestSplit.TeamTwo)
        {
            await server.SwitchTeam(player.Name, "1", cancellationToken);
        }
        
        return new AutoBalanceResult(true, "Auto balance completed successfully.");
    }

    public async Task<ResetServerResult> ResetServerAsync(string testerId, CancellationToken cancellationToken = default)
    {
        var minimumUnixTimeSeconds = DateTimeOffset.UtcNow.Subtract(PlaytestWindow).ToUnixTimeSeconds();
        var playtest = await _mapTests.Find(mapTest =>
                mapTest.Owner == testerId &&
                !mapTest.Deleted &&
                mapTest.ServerClaimed &&
                mapTest.Time >= minimumUnixTimeSeconds)
            .SortByDescending(mapTest => mapTest.Time)
            .FirstOrDefaultAsync(cancellationToken);

        if (playtest is null)
        {
            return new ResetServerResult(false, "No active playtest owned by that Discord id was found in the past hour.");
        }
        
        await using var server = await GetServer(playtest.Id);

        if (server is null)
        {
            return new ResetServerResult(false, "No server was found for the playtest.");
        }
        
        var res = await server.ResetSnd(cancellationToken);
        
        return new ResetServerResult(res.WasSuccessful, res.WasSuccessful ? "Server was successfully reset." : "Server was not successfully reset.");
    }

    private async Task<RconClient?> GetServer(string playtestId)
    {
        var server = await _servers.Find(server =>
                server.Reserved &&
                server.ReservedBy == playtestId)
            .FirstOrDefaultAsync();

        if (server is null)
        {
            return null;
        }

        var connectionInfo = new RconConnectionInfo(server.Ip, server.Port, _pavlovRconOptions.Password);
        var clientOptions = new RconClientOptions
        {
            CommandTimeout = TimeSpan.FromSeconds(_pavlovRconOptions.CommandTimeoutSeconds)
        };

        return new RconClient(connectionInfo, clientOptions);
    }

    private static (IReadOnlyList<BalancedPlayer> TeamOne, IReadOnlyList<BalancedPlayer> TeamTwo) FindBestSplit(IReadOnlyList<BalancedPlayer> players)
    {
        var totalMmr = players.Sum(static player => player.Mmr);
        var target = totalMmr / 2;

        List<BalancedPlayer>? bestTeamOne = null;
        var bestMask = 0;
        var bestDifference = decimal.MaxValue;

        var combinationLimit = 1 << players.Count;
        for (var mask = 0; mask < combinationLimit; mask++)
        {
            if (BitOperations.PopCount((uint)mask) != players.Count / 2)
            {
                continue;
            }

            var teamOne = new List<BalancedPlayer>(capacity: players.Count / 2);
            decimal teamOneMmr = 0;

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
