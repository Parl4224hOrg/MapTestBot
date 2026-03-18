using System.ComponentModel.DataAnnotations;

namespace CLVR.SMM.MapTestBot.Configuration;

public sealed class PavlovRconOptions
{
    public const string SectionName = "PavlovRcon";

    [Required]
    public string Password { get; set; } = string.Empty;

    [Range(1, 60)]
    public int CommandTimeoutSeconds { get; set; } = 5;
}
