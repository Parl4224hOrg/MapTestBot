using CLVR.SMM.MapTestBot.Configuration;
using NetCord;
using NetCord.Rest;
using NetCord.Services;
using NetCord.Services.ApplicationCommands;

namespace CLVR.SMM.MapTestBot.Commands;

public abstract class ApplicationInteractionModuleBase<TContext>(DiscordIds discordIds)
    : ApplicationCommandModule<TContext> where TContext : IApplicationCommandContext, IUserContext, IGuildContext
{
    private bool RespondedTo { get; set; }

    protected DiscordIds DiscordIds { get; } = discordIds;

    protected async Task ExecuteCommand(
        ILogger logger,
        Func<Task> action,
        string failureMessage = "An error occurred.")
    {
        try
        {
            await action();
        }
        catch (Exception e)
        {
            var errorId = Guid.NewGuid().ToString("N");
            logger.LogError(e, "Command failed with error id {ErrorId}", errorId);

            var errorProperties = new InteractionMessageProperties
            {
                Content = $"{failureMessage} Error id: {errorId}",
                Flags = MessageFlags.Ephemeral
            };

            await ReplyAsync(errorProperties);
        }
    }

    protected async Task DeferAsync(MessageFlags? flags = null)
    {
        RespondedTo = true;
        await Context.Interaction.SendResponseAsync(InteractionCallback.DeferredMessage(flags));
    }

    protected async Task ReplyAsync(InteractionMessageProperties message)
    {
        if (!RespondedTo)
        {
            await Context.Interaction.SendResponseAsync(InteractionCallback.Message(message));
            RespondedTo = true;
        }
        else
        {
            await Context.Interaction.SendFollowupMessageAsync(message);
        }
    }
}