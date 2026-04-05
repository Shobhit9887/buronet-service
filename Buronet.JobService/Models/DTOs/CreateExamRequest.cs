using System.ComponentModel.DataAnnotations;

namespace Buronet.JobService.Models.DTOs;

public sealed class CreateExamRequest
{
    [Required]
    public string ExamTitle { get; init; } = null!;

    public string? ReferenceNumber { get; init; }

    [Required]
    public string ConductingBody { get; init; } = null!;

    public List<string>? PostsIncluded { get; init; }

    [Required]
    public string ExamSummary { get; init; } = null!;

    public EligibilityCriteria? EligibilityCriteria { get; init; }

    public ApplicationDetails? ApplicationDetails { get; init; }

    public ExamPattern? ExamPattern { get; init; }

    public string? SyllabusSummary { get; init; }

    public ImportantLinks? ImportantLinks { get; init; }

    public ExamDates? ExamDates { get; init; }
}
