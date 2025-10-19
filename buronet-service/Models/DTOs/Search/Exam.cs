using MongoDB.Bson.Serialization.Attributes;

namespace buronet_service.Models.DTOs.Search
{
    public class Exam
    {
        public string? Id { get; set; }
        public string ExamTitle { get; set; } = null!;

        public string? ReferenceNumber { get; set; }

        public string? ConductingBody { get; set; }

        public List<string> PostsIncluded { get; set; } = new();

        public string? ExamSummary { get; set; }
    }
}
