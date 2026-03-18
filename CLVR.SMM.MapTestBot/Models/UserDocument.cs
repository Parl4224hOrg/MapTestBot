using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;

namespace CLVR.SMM.MapTestBot.Models;

public sealed class UserDocument
{
    [BsonId]
    [BsonRepresentation(BsonType.ObjectId)]
    [BsonElement("_id")]
    public string MongoId { get; init; } = string.Empty;

    [BsonElement("id")]
    public string DiscordId { get; init; } = string.Empty;

    [BsonElement("name")]
    public string Name { get; init; } = string.Empty;

    [BsonElement("stats")]
    public IReadOnlyList<ObjectId> Stats { get; init; } = [];
}
