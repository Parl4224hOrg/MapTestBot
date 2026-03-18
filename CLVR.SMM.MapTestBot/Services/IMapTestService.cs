using CLVR.SMM.MapTestBot.Services.Results;

namespace CLVR.SMM.MapTestBot.Services;

public interface IMapTestService
{
    Task<SwitchMapResult> SwitchMapAsync(string discordId, string mapUgc, CancellationToken cancellationToken = default);

    Task<AutoBalanceResult> AutoBalanceAsync(IReadOnlyCollection<string> memberDiscordIds, CancellationToken cancellationToken = default);
}
