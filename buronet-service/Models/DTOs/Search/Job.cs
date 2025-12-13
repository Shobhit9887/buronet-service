using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;

namespace buronet_service.Models.DTOs.Search
{
    public class Job
    {
        public string? Id { get; set; }

        public string JobTitle { get; set; } = null!;

        public string CompanyName { get; set; } = null!;

        public string Sector { get; set; } = null!;

        public string ReferenceNumber { get; set; } = null!;

        public string OrganizationName { get; set; } = null!;

        public string Location { get; set; } = null!;

        public string Compensation { get; set; } = null!;

        public string JobDescription { get; set; } = null!;

        public string ContactInformation { get; set; } = null!;
        public string EmploymentType { get; set; } = null!;

        public string DateOfIssue { get; set; } = null!;

        public List<string> Qualifications { get; set; } = new();

        public List<string> Benefits { get; set; } = new();

        public List<string> ApplicationProcess { get; set; } = new();

        public List<string> EligibilityNotes { get; set; } = new();

        public ApplyLinkInfo? ApplyLink { get; set; }

        public string? ContentHash { get; set; }

        //[BsonDateTimeOptions(Kind = DateTimeKind.Utc)]
        public string LastDateToApply { get; set; }
    }

    public class ApplyLinkInfo
    {
        public string Link { get; set; } = string.Empty;

        public string FileName { get; set; } = string.Empty;
    }
}
