using CsvHelper.Configuration.Attributes;

namespace Tracker.API.Data.Seeders.Dtos;

public class UserDto
{
    [Name("Id")]
    public string Id { get; set; } = string.Empty;

    [Name("UserName")]
    public string UserName { get; set; } = string.Empty;

    [Name("Email")]
    public string Email { get; set; } = string.Empty;

    [Name("FirstName")]
    public string FirstName { get; set; } = string.Empty;

    [Name("LastName")]
    public string LastName { get; set; } = string.Empty;

    [Name("PhoneNumber")]
    public string? PhoneNumber { get; set; }

    [Name("IsActive")]
    public bool IsActive { get; set; } = true;

    [Name("EmailConfirmed")]
    public bool EmailConfirmed { get; set; } = true;

    [Name("Role")]
    public string? Role { get; set; }

    [Name("Roles")]
    public string? Roles { get; set; }

    [Name("CreatedAt")]
    public DateTime CreatedAt { get; set; }

    [Name("LastLoginAt")]
    public DateTime? LastLoginAt { get; set; }
}
