using CsvHelper.Configuration.Attributes;

namespace Tracker.API.Data.Seeders.Dtos;

public class IncidentIndividualDto
{
    [Name("Id")]
    public string Id { get; set; } = string.Empty;

    [Name("IncidentId")]
    public string IncidentId { get; set; } = string.Empty;

    [Name("IndividualId")]
    public string IndividualId { get; set; } = string.Empty;

    [Name("Role")]
    public string Role { get; set; } = string.Empty;

    [Name("Description")]
    public string? Description { get; set; }

    [Name("CreatedAt")]
    public DateTime CreatedAt { get; set; }

    [Name("UpdatedAt")]
    public DateTime? UpdatedAt { get; set; }
}
