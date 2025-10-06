using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;

namespace Buronet.Bytes.API.Models;

public class Comment
{
    [BsonId]
    [BsonRepresentation(BsonType.ObjectId)]
    public string? Id { get; set; }

    [BsonElement("byteId")]
    [BsonRepresentation(BsonType.ObjectId)]
    public string ByteId { get; set; } = string.Empty;

    [BsonElement("creator")]
    public Creator Creator { get; set; } = new();

    [BsonElement("text")]
    public string Text { get; set; } = string.Empty;

    [BsonElement("createdAt")]
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}

public class CreateCommentRequest
{
    public string Text { get; set; } = string.Empty;
}


