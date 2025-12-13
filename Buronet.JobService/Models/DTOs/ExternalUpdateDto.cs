using System;

namespace Buronet.JobService.Models
{
    // DTO for parsing Gemini response
    public class ExternalUpdateDto
    {
        public string Title { get; set; } = null!;
        public string Url { get; set; } = "#";
        public string Type { get; set; } = "Other";
        public UpdateCategory UpdateCategory { get; set; } = UpdateCategory.General;
        public DateTime? PublishedDate { get; set; }
        public string SourceName { get; set; } = "";
    }
}