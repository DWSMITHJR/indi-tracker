using CsvHelper.Configuration.Attributes;

namespace Tracker.API.Data.Seeders.Dtos;

public class IndividualDto
{
    [Name("Id")]
    public string Id { get; set; } = string.Empty;

    [Name("FirstName")]
    public string FirstName { get; set; } = string.Empty;

    [Name("LastName")]
    public string LastName { get; set; } = string.Empty;

    [Name("MiddleName")]
    public string? MiddleName { get; set; }

    [Name("DateOfBirth")]
    public DateTime? DateOfBirth { get; set; }

    [Name("Gender")]
    public string? Gender { get; set; }

    [Name("Email")]
    public string? Email { get; set; }

    [Name("Phone")]
    public string? Phone { get; set; }

    [Name("Address")]
    public string? Address { get; set; }

    [Name("City")]
    public string? City { get; set; }

    [Name("State")]
    public string? State { get; set; }

    [Name("ZipCode")]
    public string? ZipCode { get; set; }

    [Name("OrganizationId")]
    public string? OrganizationId { get; set; }

    [Name("CreatedAt")]
    public DateTime CreatedAt { get; set; }

    [Name("UpdatedAt")]
    public DateTime? UpdatedAt { get; set; }
}
