using Buronet.JobService.Models;
using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;

namespace JobService.Models;

public class Job
{
    [BsonId]
    [BsonRepresentation(BsonType.ObjectId)]
    public string? Id { get; set; }

    // Required fields
    [BsonElement("job_title")]
    public string JobTitle { get; set; } = null!;

    [BsonElement("company_name")]
    public string CompanyName { get; set; } = null!;

    [BsonElement("sector")]
    public string Sector { get; set; } = null!;

    [BsonElement("organization_name")]
    public string OrganizationName { get; set; } = null!;

    [BsonElement("location")]
    public string Location { get; set; } = null!;

    [BsonElement("job_description")]
    public string JobDescription { get; set; } = null!;

    // Optional fields
    [BsonElement("reference_number")]
    public string? ReferenceNumber { get; set; }

    [BsonElement("compensation")]
    public string? Compensation { get; set; }

    [BsonElement("contact_information")]
    public string? ContactInformation { get; set; }

    [BsonElement("employment_type")]
    public string? EmploymentType { get; set; }

    [BsonElement("date_of_issue")]
    public string? DateOfIssue { get; set; }

    [BsonElement("qualifications")]
    public List<string> Qualifications { get; set; } = new();

    [BsonElement("benefits")]
    public List<string> Benefits { get; set; } = new();

    [BsonElement("application_process")]
    public List<string> ApplicationProcess { get; set; } = new();

    [BsonElement("eligibility_notes")]
    public List<string> EligibilityNotes { get; set; } = new();

    [BsonElement("apply_link")]
    public ApplyLinkInfo? ApplyLink { get; set; }

    [BsonElement("content_hash")]
    public string? ContentHash { get; set; }

    [BsonElement("last_date_to_apply")]
    public string? LastDateToApply { get; set; }

    [BsonElement("status")]
    public string? Status { get; set; }

    [BsonElement("createdDate")]
    public string? CreatedDate { get; set; }

    [BsonElement("updatedDate")]
    public string? UpdatedDate { get; set; }

    [BsonElement("original_extraction")]
    public Job? originalExtraction { get; set; }
}