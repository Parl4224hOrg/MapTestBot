namespace CLVR.SMM.MapTestBot.Services.Results;

public sealed record AutoBalanceResult(
    bool Succeeded,
    string Message,
    IReadOnlyList<BalancedPlayer> TeamOne,
    IReadOnlyList<BalancedPlayer> TeamTwo,
    int TeamOneTotalMmr,
    int TeamTwoTotalMmr)
{
    public static AutoBalanceResult Failed(string message) => new(false, message, [], [], 0, 0);
}

public sealed record BalancedPlayer(string DiscordId, string Name, int Mmr);
