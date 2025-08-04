using System;
using System.ComponentModel.DataAnnotations;

namespace Tracker.Shared.Models
{
    public class UpdateIncidentDto
    {
        [Required]
        public required string Id { get; set; }
        
        [StringLength(200, ErrorMessage = "Title cannot exceed 200 characters")]
        public required string Title { get; set; }
        
        public string? Description { get; set; }
        
        public required string Status { get; set; }
        
        public required string Priority { get; set; }
        
        [StringLength(100, ErrorMessage = "Type cannot exceed 100 characters")]
        public string? Type { get; set; }
        
        public string? AssignedTo { get; set; }
        
        public string? IndividualId { get; set; }
        
        public required string OrganizationId { get; set; }
    }
}
