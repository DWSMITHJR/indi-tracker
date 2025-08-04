using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace Tracker.Infrastructure.Models
{
    public class Individual : BaseEntity
    {
        [Required]
        [MaxLength(100)]
        public string FirstName { get; set; } = string.Empty;

        [Required]
        [MaxLength(100)]
        public string LastName { get; set; } = string.Empty;

        public DateTime? DateOfBirth { get; set; }

        [MaxLength(20)]
        public string? Gender { get; set; }

        [EmailAddress]
        [MaxLength(200)]
        public string? Email { get; set; }

        [MaxLength(20)]
        public string? Phone { get; set; }

        // Address
        [MaxLength(200)]
        public string? Street { get; set; }
        
        [MaxLength(100)]
        public string? City { get; set; }
        
        [MaxLength(100)]
        public string? State { get; set; }
        
        [MaxLength(20)]
        public string? ZipCode { get; set; }
        
        [MaxLength(100)]
        public string? Country { get; set; }

        // Organization relationship
        [Required]
        public Guid OrganizationId { get; set; }
        public virtual Organization Organization { get; set; } = null!;

        // Individual type based on organization type
        [MaxLength(50)]
        public string? Type { get; set; }

        [Required]
        [MaxLength(20)]
        public string Status { get; set; } = "Active";

        // Navigation properties
        public virtual ICollection<IncidentIndividual> IncidentInvolvements { get; set; } = new List<IncidentIndividual>();
    }
}
