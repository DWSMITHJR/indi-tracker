using System;

namespace Tracker.Shared.Models
{
    public class ContactDto
    {
        public Guid Id { get; set; }
        public string FirstName { get; set; } = string.Empty;
        public string LastName { get; set; } = string.Empty;
        public string? MiddleName { get; set; }
        public string? Email { get; set; }
        public string? Phone { get; set; }
        public string? Relationship { get; set; }
        public string? Notes { get; set; }
        public bool IsPrimary { get; set; }
        public bool IsEmergency { get; set; }
        public Guid? IndividualId { get; set; }
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public DateTime? UpdatedAt { get; set; }
    }
}
