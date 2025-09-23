using Buronet.JobService.Models;
using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;

namespace JobService.Models;

public class Job
{
    [BsonId]
    [BsonRepresentation(BsonType.ObjectId)]
    public string? Id { get; set; }

    [BsonElement("job_title")]
    public string JobTitle { get; set; } = null!;

    [BsonElement("company_name")]
    public string CompanyName { get; set; } = null!;

    [BsonElement("sector")]
    public string Sector { get; set; } = null!;

    [BsonElement("reference_number")]
    public string ReferenceNumber { get; set; } = null!;

    [BsonElement("organization_name")]
    public string OrganizationName { get; set; } = null!;

    [BsonElement("location")]
    public string Location { get; set; } = null!;

    [BsonElement("compensation")]
    public string Compensation { get; set; } = null!;

    [BsonElement("job_description")]
    public string JobDescription { get; set; } = null!;

    [BsonElement("contact_information")]
    public string ContactInformation { get; set; } = null!;

    [BsonElement("employment_type")]
    public string EmploymentType { get; set; } = null!;

    [BsonElement("date_of_issue")]
    public string DateOfIssue { get; set; } = null!;

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

    [BsonElement("last_date_to_apply")]
    //[BsonDateTimeOptions(Kind = DateTimeKind.Utc)]
    public string LastDateToApply { get; set; }

    [BsonElement("status")]
    public string? Status { get; set; } = null!;

    [BsonElement("createdDate")]
    public DateTime CreatedDate { get; set; }

    [BsonElement("updatedDate")]
    public DateTime UpdatedDate { get; set; }


    [BsonElement("original_extraction")]
    public Job? originalExtraction { get; set; }

    // Add any other fields from your JSON object here
}