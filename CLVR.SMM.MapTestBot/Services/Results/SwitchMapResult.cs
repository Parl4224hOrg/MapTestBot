namespace CLVR.SMM.MapTestBot.Services.Results;

public sealed record SwitchMapResult(
    bool Succeeded,
    string Message,
    string? ServerId = null,
    string? ServerName = null);
