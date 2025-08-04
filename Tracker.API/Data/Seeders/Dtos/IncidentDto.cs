using CsvHelper.Configuration.Attributes;

namespace Tracker.API.Data.Seeders.Dtos;

public class IncidentDto
{
    [Name("Id")]
    public string Id { get; set; } = string.Empty;

    [Name("IncidentNumber")]
    public string IncidentNumber { get; set; } = string.Empty;

    [Name("Title")]
    public string Title { get; set; } = string.Empty;

    [Name("Description")]
    public string? Description { get; set; }

    [Name("Status")]
    public string Status { get; set; } = "Open";

    [Name("Priority")]
    public string Priority { get; set; } = "Medium";

    [Name("ReportedBy")]
    public string ReportedBy { get; set; } = string.Empty;

    [Name("AssignedTo")]
    public string? AssignedTo { get; set; }

    [Name("OrganizationId")]
    public string OrganizationId { get; set; } = string.Empty;

    [Name("CreatedAt")]
    public DateTime CreatedAt { get; set; }

    [Name("UpdatedAt")]
    public DateTime? UpdatedAt { get; set; }

    [Name("ResolvedAt")]
    public DateTime? ResolvedAt { get; set; }

    [Name("Resolution")]
    public string? Resolution { get; set; }
}
