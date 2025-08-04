using CsvHelper.Configuration.Attributes;
using System;

namespace Tracker.API.Data.Seeders.Dtos;

public class EnrollmentCodeDto
{
    [Name("Id")]
    public string Id { get; set; } = string.Empty;

    [Name("Code")]
    public string Code { get; set; } = string.Empty;

    [Name("OrganizationId")]
    public string OrganizationId { get; set; } = string.Empty;

    [Name("BeginDate")]
    public DateTime BeginDate { get; set; } = DateTime.UtcNow;

    [Name("EndDate")]
    public DateTime EndDate { get; set; } = DateTime.UtcNow.AddDays(30);

    [Name("IsActive")]
    public bool IsActive { get; set; } = true;

    [Name("Used")]
    public bool Used { get; set; }

    [Name("UsedAt")]
    public DateTime? UsedAt { get; set; }

    [Name("UsedBy")]
    public string? UsedBy { get; set; }

    [Name("UsedById")]
    public string? UsedById { get; set; }
}
