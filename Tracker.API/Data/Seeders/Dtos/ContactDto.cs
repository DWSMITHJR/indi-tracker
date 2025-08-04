using CsvHelper.Configuration.Attributes;

namespace Tracker.API.Data.Seeders.Dtos;

public class ContactDto
{
    [Name("Id")]
    public string Id { get; set; } = string.Empty;

    [Name("FirstName")]
    public string FirstName { get; set; } = string.Empty;

    [Name("LastName")]
    public string LastName { get; set; } = string.Empty;

    [Name("Relationship")]
    public string Relationship { get; set; } = string.Empty;

    [Name("Email")]
    public string? Email { get; set; }

    [Name("Phone")]
    public string? Phone { get; set; }

    [Name("Street")]
    public string? Street { get; set; }

    [Name("City")]
    public string? City { get; set; }

    [Name("State")]
    public string? State { get; set; }

    [Name("ZipCode")]
    public string? ZipCode { get; set; }

    [Name("Country")]
    public string? Country { get; set; }

    [Name("IsPrimary")]
    public bool IsPrimary { get; set; }

    [Name("IsEmergency")]
    public bool IsEmergency { get; set; }

    [Name("Notes")]
    public string? Notes { get; set; }

    [Name("Department")]
    public string? Department { get; set; }

    [Name("Position")]
    public string? Position { get; set; }

    [Name("PreferredContactMethod")]
    public string? PreferredContactMethod { get; set; }

    [Name("IndividualId")]
    public string IndividualId { get; set; } = string.Empty;

    [Name("OrganizationId")]
    public string OrganizationId { get; set; } = string.Empty;

    [Name("CreatedAt")]
    public DateTime CreatedAt { get; set; }

    [Name("UpdatedAt")]
    public DateTime? UpdatedAt { get; set; }
}
