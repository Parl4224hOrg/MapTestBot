using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;

namespace CLVR.SMM.MapTestBot.Models;

[BsonIgnoreExtraElements]
public sealed class ServerDocument
{
    [BsonId]
    [BsonRepresentation(BsonType.ObjectId)]
    public string MongoId { get; init; } = string.Empty;

    [BsonElement("id")]
    public string Id { get; init; } = string.Empty;

    [BsonElement("ip")]
    public string Ip { get; init; } = string.Empty;

    [BsonElement("port")]
    public int Port { get; init; }

    [BsonElement("name")]
    public string Name { get; init; } = string.Empty;

    [BsonElement("reservedBy")]
    public string? ReservedBy { get; init; }

    [BsonElement("reserved")]
    public bool Reserved { get; init; }
}
