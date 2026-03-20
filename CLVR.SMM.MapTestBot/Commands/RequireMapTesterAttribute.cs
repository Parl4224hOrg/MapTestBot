using CLVR.SMM.MapTestBot.Configuration;
using NetCord.Services;
using NetCord.Services.ApplicationCommands;

namespace CLVR.SMM.MapTestBot.Commands;

public class RequireMapTesterAttribute : PreconditionAttribute<SlashCommandContext>
{
    public override ValueTask<PreconditionResult> EnsureCanExecuteAsync(SlashCommandContext context, IServiceProvider? serviceProvider)
    {
        if (serviceProvider is null)
            return ValueTask.FromResult<PreconditionResult>(
                new PreconditionFailResult("Service provider cannot be null, contact parl."));

        if (context.Guild is null)
            return ValueTask.FromResult<PreconditionResult>(
                new PreconditionFailResult("This command can only be used in a guild."));

        var discordIds = serviceProvider.GetRequiredService<DiscordIds>();

        if (!context.Guild.Users.TryGetValue(context.User.Id, out var user))
        {
            user = context.Guild.GetUserAsync(context.User.Id).Result;
        }

        if (user.RoleIds.Contains(discordIds.MapTesterRole))
        {
            return ValueTask.FromResult<PreconditionResult>(
                new PreconditionSuccessResult());
        }

        return ValueTask.FromResult<PreconditionResult>(
            new PreconditionFailResult("You do not have permission to use this command."));
    }
}