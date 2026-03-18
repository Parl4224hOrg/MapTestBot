using System.ComponentModel.DataAnnotations;

namespace CLVR.SMM.MapTestBot.Configuration;

public sealed class MongoOptions
{
    public const string SectionName = "Mongo";

    [Required]
    public string ConnectionString { get; set; } = string.Empty;

    [Required]
    public string DatabaseName { get; set; } = string.Empty;

    [Required]
    public string UsersCollectionName { get; set; } = "users";

    [Required]
    public string ServersCollectionName { get; set; } = "servers";

    [Required]
    public string StatsCollectionName { get; set; } = "stats";

    [Required]
    public string MapTestsCollectionName { get; set; } = "maptests";
}
