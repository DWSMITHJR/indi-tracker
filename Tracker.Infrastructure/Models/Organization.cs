using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace Tracker.Infrastructure.Models
{
    public class Organization : BaseEntity
    {
        [Required]
        [MaxLength(200)]
        public string Name { get; set; } = string.Empty;

        [MaxLength(20)]
        public string Type { get; set; } = string.Empty; // School, Business, Healthcare, etc.

        [MaxLength(100)]
        public string Phone { get; set; } = string.Empty;

        [EmailAddress]
        [MaxLength(200)]
        public string Email { get; set; } = string.Empty;

        // Address
        [MaxLength(200)]
        public string Street { get; set; } = string.Empty;
        
        [MaxLength(100)]
        public string City { get; set; } = string.Empty;
        
        [MaxLength(100)]
        public string State { get; set; } = string.Empty;
        
        [MaxLength(20)]
        public string ZipCode { get; set; } = string.Empty;
        
        [MaxLength(100)]
        public string Country { get; set; } = string.Empty;

        public bool IsActive { get; set; } = true;

        // Navigation properties
        public virtual ICollection<User> Users { get; set; } = new List<User>();
        public virtual ICollection<Individual> Individuals { get; set; } = new List<Individual>();
        public virtual ICollection<Contact> Contacts { get; set; } = new List<Contact>();
        public virtual ICollection<Incident> Incidents { get; set; } = new List<Incident>();
        public virtual ICollection<EnrollmentCode> EnrollmentCodes { get; set; } = new List<EnrollmentCode>();
    }
}
