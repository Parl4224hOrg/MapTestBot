using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;

namespace CLVR.SMM.MapTestBot.Models;

[BsonIgnoreExtraElements]
public sealed class MapTestDocument
{
    [BsonId]
    [BsonRepresentation(BsonType.ObjectId)]
    public string MongoId { get; init; } = string.Empty;

    [BsonElement("id")]
    public string Id { get; init; } = string.Empty;

    [BsonElement("owner")]
    public string Owner { get; init; } = string.Empty;

    [BsonElement("time")]
    public long Time { get; init; }

    [BsonElement("deleted")]
    public bool Deleted { get; init; }

    [BsonElement("serverClaimed")]
    public bool ServerClaimed { get; init; }
}
