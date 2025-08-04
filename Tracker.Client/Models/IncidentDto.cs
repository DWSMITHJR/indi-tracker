using System;

namespace Tracker.Client.Models
{
    public class IncidentDto
    {
        public string Id { get; set; }
        public string IncidentNumber { get; set; }
        public string Title { get; set; }
        public string Description { get; set; }
        public string Status { get; set; }
        public string Priority { get; set; }
        public string AssignedTo { get; set; }
        public string ReportedBy { get; set; }
        public DateTime CreatedAt { get; set; }
        public DateTime? UpdatedAt { get; set; }
        public DateTime? ResolvedAt { get; set; }
        public string Resolution { get; set; }
        public string OrganizationId { get; set; }
        public string OrganizationName { get; set; }
        public string[] Tags { get; set; } = Array.Empty<string>();
    }
}
