using System.ComponentModel.DataAnnotations;
using Buronet.JobService.Models;

namespace Buronet.JobService.Models.DTOs;

public sealed class CreateJobRequest
{
    [Required]
    public string JobTitle { get; init; } = null!;

    [Required]
    public string CompanyName { get; init; } = null!;

    [Required]
    public string Sector { get; init; } = null!;

    public string? ReferenceNumber { get; init; }

    [Required]
    public string OrganizationName { get; init; } = null!;

    [Required]
    public string Location { get; init; } = null!;

    public string? Compensation { get; init; }

    [Required]
    public string JobDescription { get; init; } = null!;

    public string? ContactInformation { get; init; }

    public string? EmploymentType { get; init; }

    public string? DateOfIssue { get; init; }

    public List<string>? Qualifications { get; init; }

    public List<string>? Benefits { get; init; }

    public List<string>? ApplicationProcess { get; init; }

    public List<string>? EligibilityNotes { get; init; }

    public ApplyLinkInfo? ApplyLink { get; init; }

    public string? LastDateToApply { get; init; }
}