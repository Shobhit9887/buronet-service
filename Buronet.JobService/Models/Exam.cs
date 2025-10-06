using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;
using System.Collections.Generic;

namespace Buronet.JobService.Models;

// Main Exam Document
public class Exam
{
    [BsonId]
    [BsonRepresentation(BsonType.ObjectId)]
    public string? Id { get; set; }

    [BsonElement("exam_title")]
    public string ExamTitle { get; set; } = null!;

    [BsonElement("reference_number")]
    public string? ReferenceNumber { get; set; }

    [BsonElement("conducting_body")]
    public string? ConductingBody { get; set; }

    [BsonElement("posts_included")]
    public List<string> PostsIncluded { get; set; } = new();

    [BsonElement("exam_summary")]
    public string? ExamSummary { get; set; }

    [BsonElement("eligibility_criteria")]
    public EligibilityCriteria? EligibilityCriteria { get; set; }

    [BsonElement("application_details")]
    public ApplicationDetails? ApplicationDetails { get; set; }

    [BsonElement("exam_pattern")]
    public ExamPattern? ExamPattern { get; set; }

    [BsonElement("syllabus_summary")]
    public string? SyllabusSummary { get; set; }

    [BsonElement("important_links")]
    public ImportantLinks? ImportantLinks { get; set; }

    [BsonElement("exam_dates")]
    public ExamDates? ExamDates { get; set; }

    [BsonElement("content_hash")]
    public string? ContentHash { get; set; }

    [BsonElement("status")]
    public string? Status { get; set; }

    [BsonElement("createdDate")]
    public string? CreatedDate { get; set; }

    [BsonElement("updatedDate")]
    public string? UpdatedDate { get; set; }

    [BsonElement("original_extraction")]
    public Exam? OriginalExtraction { get; set; }
}

// Nested Classes for the Exam Model

public class EligibilityCriteria
{
    [BsonElement("educational_qualification")]
    public string? EducationalQualification { get; set; }

    [BsonElement("age_limit")]
    public AgeLimit? AgeLimit { get; set; }

    [BsonElement("nationality")]
    public string? Nationality { get; set; }

    [BsonElement("other_requirements")]
    public List<string> OtherRequirements { get; set; } = new();
}

public class AgeLimit
{
    [BsonElement("minimum")]
    public int? Minimum { get; set; }

    [BsonElement("maximum")]
    public int? Maximum { get; set; }

    [BsonElement("relaxation_notes")]
    public List<string> RelaxationNotes { get; set; } = new();
}

public class ApplicationDetails
{
    [BsonElement("application_start_date")]
    public string? ApplicationStartDate { get; set; }

    [BsonElement("application_end_date")]
    public string? ApplicationEndDate { get; set; }

    [BsonElement("application_fee")]
    public object? ApplicationFee { get; set; } // Using object to handle potential null or complex fee structures

    [BsonElement("how_to_apply")]
    public List<string> HowToApply { get; set; } = new();
}

public class ExamPattern
{
    [BsonElement("preliminary")]
    public ExamStage? Preliminary { get; set; }

    [BsonElement("main")]
    public ExamStage? Main { get; set; }

}

public class ExamStage
{
    [BsonElement("papers")]
    public List<Paper> Papers { get; set; } = new();

    [BsonElement("interview")]
    public Interview? Interview { get; set; }

    [BsonElement("total_marks")]
    public int? TotalMarks { get; set; }

    [BsonElement("qualifying_papers")]
    public List<string> QualifyingPapers { get; set; } = new();

    [BsonElement("summary")]
    public string? Summary { get; set; }

    [BsonElement("other_details")]
    public List<string>? OtherDetails { get; set; }
}

public class Paper
{
    [BsonElement("paper_name")]
    public string? PaperName { get; set; }

    [BsonElement("type")]
    public string? Type { get; set; }

    [BsonElement("marks")]
    public int? Marks { get; set; }

    [BsonElement("duration_hours")]
    public int? DurationHours { get; set; }

    [BsonElement("notes")]
    public string? Notes { get; set; }
}

public class Interview
{
    [BsonElement("stage_name")]
    public string? StageName { get; set; }

    [BsonElement("marks")]
    public int? Marks { get; set; }

    [BsonElement("notes")]
    public string? Notes { get; set; }
}

public class ImportantLinks
{
    [BsonElement("official_website")]
    public string? OfficialWebsite { get; set; }

    [BsonElement("notification_pdf")]
    public string? NotificationPdf { get; set; }
}

public class ExamDates
{
    [BsonElement("preliminary_date")]
    public string? PreliminaryDate { get; set; }

    [BsonElement("main_date")]
    public string? MainDate { get; set; }
}