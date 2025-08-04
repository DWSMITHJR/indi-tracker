using CsvHelper.Configuration.Attributes;

namespace Tracker.API.Data.Seeders.Dtos;

public class OrganizationDto
{
    [Name("Id")]
    public string Id { get; set; } = string.Empty;

    [Name("Name")]
    public string Name { get; set; } = string.Empty;

    [Name("Type")]
    public string? Type { get; set; }

    [Name("Phone")]
    public string? Phone { get; set; }

    [Name("Email")]
    public string? Email { get; set; }

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

    [Name("Website")]
    public string? Website { get; set; }

    [Name("CreatedAt")]
    public DateTime CreatedAt { get; set; }

    [Name("UpdatedAt")]
    public DateTime? UpdatedAt { get; set; }
}
