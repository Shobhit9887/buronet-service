using System.Collections.Generic;

namespace buronet_service.Models.DTOs.Search
{
    public class SearchResultDto
    {
        // The combined list of results, with a common type to allow for categorization on the frontend
        public List<UnifiedSearchResultItem> Results { get; set; } = new List<UnifiedSearchResultItem>();

        // Optional: Can include facets or counts if needed for filters
        public int TotalUserCount { get; set; }
        public int TotalJobCount { get; set; }
        public int TotalExamCount { get; set; }
    }

    public class UnifiedSearchResultItem
    {
        // Common identifier
        public string Id { get; set; }

        // Enum to tell the frontend how to render the item (UserCard, JobCard, etc.)
        public string Type { get; set; } // e.g., "User", "Job"

        // Primary title for the result card
        public string Title { get; set; }

        // Secondary descriptive text
        public string Subtitle { get; set; }

        // URL for the link on the platform (e.g., /user/guid, /jobs/guid)
        public string LinkUrl { get; set; }

        // Additional data specific to the type (e.g., location, image URL)
        public object Payload { get; set; }
    }

    // Specific Payloads for Type = "User"
    public class UserSearchResultPayload
    {
        public string ProfilePictureUrl { get; set; }
        public string CurrentPosition { get; set; }
        public string Location { get; set; }
    }

    // Specific Payloads for Type = "Job"
    public class JobSearchResultPayload
    {
        public string CompanyName { get; set; }
        public string Location { get; set; }
        public string JobType { get; set; } // FullTime, Contract, etc.
        public string ApplyLink { get; set; } // The external URL to apply
    }

    public class ExamSearchResultPayload
    {
        public string ConductingBody { get; set; }
        public string ReferenceNumber { get; set; } // FullTime, Contract, etc.
        public string ExamSummary { get; set; } // The external URL to apply
    }
}