using CLVR.SMM.MapTestBot.Configuration;
using CLVR.SMM.MapTestBot.Services;
using NetCord;
using NetCord.Services.ApplicationCommands;

namespace CLVR.SMM.MapTestBot.Commands;

[SlashCommand("map_test", "Commands for map tests")]
public class Commands(
    DiscordIds discordIds,
    ILogger<Commands> logger,
    IMapTestService mapTestService
) : ApplicationInteractionModuleBase<SlashCommandContext>(discordIds)
{
    [SubSlashCommand("switch_map", "Switches the map to the specified UGC")]
    [RequireMapTester]
    public Task SwitchMap(
        [SlashCommandParameter(Name = "ugc", Description = "Switches the map to the specified UGC. accepts UGC###### or ######")]
        string mapUgc
    ) => ExecuteCommand(logger, async () =>
    {
        await DeferAsync(MessageFlags.Ephemeral);
        if (string.IsNullOrWhiteSpace(mapUgc))
        {
            await ReplyAsync(new EphemeralMessage("Please specify a map UGC."));
            return;
        }

        if (!mapUgc.StartsWith("UGC", StringComparison.OrdinalIgnoreCase))
        {
            if (mapUgc.All(char.IsDigit))
            {
                mapUgc = "UGC" + mapUgc;
            }
        }
        
        var result = await mapTestService.SwitchMapAsync(Context.User.Id.ToString(), mapUgc);
        
        await ReplyAsync(new EphemeralMessage(result.Message));
    });

    [SubSlashCommand("auto_balance", "Auto balances the teams on the server")]
    [RequireMapTester]
    public Task AutoBalance() => ExecuteCommand(logger, async () =>
    {
        await DeferAsync(MessageFlags.Ephemeral);
        
        var result = await mapTestService.AutoBalanceAsync(Context.User.Id.ToString());
        
        await ReplyAsync(new EphemeralMessage(result.Message));
    });
    
    [SubSlashCommand("reset_snd", "Resets the server's state")]
    [RequireMapTester]
    public Task ResetSnd() => ExecuteCommand(logger, async () =>
    {
        var result = await mapTestService.ResetServerAsync(Context.User.Id.ToString());
        await ReplyAsync(new EphemeralMessage(result.Message));
    });
}