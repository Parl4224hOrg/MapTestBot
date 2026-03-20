using CLVR.SMM.MapTestBot.Services.Results;

namespace CLVR.SMM.MapTestBot.Services;

public interface IMapTestService
{
    Task<SwitchMapResult> SwitchMapAsync(string discordId, string mapUgc, CancellationToken cancellationToken = default);

    Task<AutoBalanceResult> AutoBalanceAsync(string testerId, CancellationToken cancellationToken = default);
    
    Task<ResetServerResult> ResetServerAsync(string testerId, CancellationToken cancellationToken = default);
}
