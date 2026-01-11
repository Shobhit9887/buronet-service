//using MongoDB.Bson;
//using MongoDB.Bson.Serialization.Attributes;
//using System.ComponentModel.DataAnnotations;

//namespace Buronet.Bytes.API.Models;

//// The core document stored in MongoDB
//public class BytePost
//{
//    [BsonId]
//    [BsonRepresentation(BsonType.String)]
//    public string PostId { get; set; } = Guid.NewGuid().ToString();

//    [BsonElement("creator")]
//    public Creator Creator { get; set; } = null!;

//    [BsonElement("comment")]
//    public Comment Comment { get; set; } = new();

//    [BsonElement("reaction")]
//    public Reaction Reaction { get; set; } = new();

//    [BsonElement("submission")]
//    public Submission Submission { get; set; } = null!;
//}

//#region Sub-documents
//public class Creator
//{
//    [BsonElement("id")] public string Id { get; set; } = null!;
//    [BsonElement("name")] public string? Name { get; set; }
//    [BsonElement("handle")] public string Handle { get; set; } = null!;
//    [BsonElement("pic")] public string? Pic { get; set; }
//}

//public class Comment
//{
//    [BsonElement("count")] public int Count { get; set; } = 0;
//    [BsonElement("commentingAllowed")] public bool CommentingAllowed { get; set; } = true;
//}

//public class Reaction
//{
//    [BsonElement("count")] public int Count { get; set; } = 0;
//    [BsonElement("voted")] public bool Voted { get; set; } = false;
//}

//public class Submission
//{
//    [BsonElement("title")] public string Title { get; set; } = null!;
//    [BsonElement("description")] public string? Description { get; set; }
//    [BsonElement("mediaUrl")] public string MediaUrl { get; set; } = null!;
//    [BsonElement("thumbnail")] public string Thumbnail { get; set; } = null!;
//    [BsonElement("hyperlink")] public string? Hyperlink { get; set; }
//    [BsonElement("placeholderUrl")] public string? PlaceholderUrl { get; set; }
//}
//#endregion

//// DTO for the file upload request from the frontend
//public class CreateByteRequest
//{
//    [Required]
//    public string Title { get; set; } = null!;
//    public string? Description { get; set; }
//    [Required]
//    public string? MediaUrl { get; set; } = null!;
//    public string? ThumbnailUrl { get; set;} = null!;
//}


using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;
using System.Collections.Generic;

namespace Buronet.Bytes.API.Models;

public class BytePost
{
    [BsonId]
    [BsonRepresentation(BsonType.ObjectId)]
    public string? Id { get; set; }

    [BsonElement("creator")]
    public Creator Creator { get; set; } = new();

    [BsonElement("submission")]
    public Submission Submission { get; set; } = new();

    [BsonElement("tags")]
    public List<string> Tags { get; set; } = new();

    [BsonElement("likes")]
    public List<string> Likes { get; set; } = new(); // List of user IDs who liked it

    [BsonElement("saves")]
    public List<string> Saves { get; set; } = new(); // List of user IDs who saved it

    [BsonElement("shareCount")]
    public int ShareCount { get; set; } = 0;

    [BsonElement("commentCount")]
    public int CommentCount { get; set; } = 0;

    [BsonElement("createdAt")]
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}

public class Creator
{
    [BsonElement("id")]
    public string Id { get; set; } = string.Empty;
    [BsonElement("name")]
    public string Name { get; set; } = string.Empty;
    [BsonElement("pic")]
    public string Pic { get; set; } = string.Empty;
}

public class Submission
{
    [BsonElement("title")]
    public string Title { get; set; } = string.Empty;
    [BsonElement("description")]
    public string Description { get; set; } = string.Empty;
    [BsonElement("mediaUrl")]
    public string MediaUrl { get; set; } = string.Empty;
    [BsonElement("thumbnail")]
    public string Thumbnail { get; set; } = string.Empty;
}

public class CreateByteRequest
{
    public string MediaUrl { get; set; } = string.Empty;
    public string Thumbnail { get; set; } = string.Empty;
    public string Title { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public string CreatorPic { get; set; } = string.Empty;
    public List<string> Tags { get; set; } = new();
}


