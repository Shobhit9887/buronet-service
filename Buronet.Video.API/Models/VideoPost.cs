using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;
using System.ComponentModel.DataAnnotations;

namespace Buronet.Video.API.Models;

public class VideoPost
{
    [BsonId]
    [BsonRepresentation(BsonType.String)]
    public string PostId { get; set; } = Guid.NewGuid().ToString();

    [BsonElement("creator")]
    public Creator Creator { get; set; } = null!;

    [BsonElement("comment")]
    public Comment Comment { get; set; } = null!;

    [BsonElement("reaction")]
    public Reaction Reaction { get; set; } = null!;

    [BsonElement("submission")]
    public Submission Submission { get; set; } = null!;
}

public class Creator
{
    [BsonElement("id")]
    public string Id { get; set; } = null!;
    [BsonElement("name")]
    public string? Name { get; set; }
    [BsonElement("handle")]
    public string Handle { get; set; } = null!;
    [BsonElement("pic")]
    public string? Pic { get; set; }
}

public class Comment
{
    [BsonElement("count")]
    public int Count { get; set; } = 0;
    [BsonElement("commentingAllowed")]
    public bool CommentingAllowed { get; set; } = true;
}

public class Reaction
{
    [BsonElement("count")]
    public int Count { get; set; } = 0;
    [BsonElement("voted")]
    public bool Voted { get; set; } = false;
}

public class Submission
{
    [BsonElement("title")]
    public string Title { get; set; } = null!;
    [BsonElement("description")]
    public string? Description { get; set; }
    [BsonElement("mediaUrl")]
    public string MediaUrl { get; set; } = null!;
    [BsonElement("thumbnail")]
    public string Thumbnail { get; set; } = null!;
    [BsonElement("hyperlink")]
    public string? Hyperlink { get; set; }
    [BsonElement("placeholderUrl")]
    public string? PlaceholderUrl { get; set; }
}

public class CreateVideoPostRequest
{
    [Required]
    public string CreatorId { get; set; } = null!;
    [Required]
    public string CreatorHandle { get; set; } = null!;
    public string? CreatorName { get; set; }
    public string? CreatorPic { get; set; }
    [Required]
    public string Title { get; set; } = null!;
    public string? Description { get; set; }
    [Required]
    public IFormFile VideoFile { get; set; } = null!;
}
