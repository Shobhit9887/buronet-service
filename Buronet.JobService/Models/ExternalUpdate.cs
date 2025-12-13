using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;
using System;

namespace Buronet.JobService.Models
{
    public enum UpdateCategory
    {
        General, // Default
        Job,
        Exam
    }

    public class ExternalUpdate
    {
        [BsonId]
        [BsonRepresentation(BsonType.ObjectId)]
        public string? Id { get; set; }

        [BsonElement("title")]
        public string Title { get; set; } = null!;

        [BsonElement("url")]
        public string Url { get; set; } = null!;

        [BsonElement("type")]
        public string Type { get; set; } = "Other"; // e.g., Results, Admit Card

        [BsonElement("updateCategory")]
        [BsonRepresentation(BsonType.String)] // Store Enum as string
        public UpdateCategory UpdateCategory { get; set; } = UpdateCategory.General;

        [BsonElement("publishedDate")]
        [BsonDateTimeOptions(Kind = DateTimeKind.Utc)]
        public DateTime? PublishedDate { get; set; }

        [BsonElement("sourceName")]
        public string SourceName { get; set; } = "";

        [BsonElement("contentHash")]
        public string ContentHash { get; set; } = null!; // Unique hash

        [BsonElement("fetchedDate")]
        [BsonDateTimeOptions(Kind = DateTimeKind.Utc)]
        public DateTime FetchedDate { get; set; } = DateTime.UtcNow;
    }
}