using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;

namespace CLVR.SMM.MapTestBot.Models;

[BsonIgnoreExtraElements]
public sealed class StatsDocument
{
    [BsonId]
    [BsonRepresentation(BsonType.ObjectId)]
    public string MongoId { get; init; } = string.Empty;

    [BsonElement("userId")]
    public ObjectId UserId { get; init; }

    [BsonElement("queueId")]
    public string QueueId { get; init; } = string.Empty;

    [BsonElement("mmr")]
    public decimal Mmr { get; init; }
}
