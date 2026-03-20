namespace CLVR.SMM.MapTestBot.Services.Results;

public sealed record AutoBalanceResult(
    bool Succeeded,
    string Message)
{
    public static AutoBalanceResult Failed(string message) => new(false, message);
}

public sealed record BalancedPlayer(string DiscordId, string Name, decimal Mmr);
