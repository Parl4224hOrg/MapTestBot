using NetCord;
using NetCord.Rest;

namespace CLVR.SMM.MapTestBot.Commands;

public class EphemeralMessage : InteractionMessageProperties
{
    public EphemeralMessage(string message)
    {
        Content = message;
        Flags = MessageFlags.Ephemeral;
    }
}