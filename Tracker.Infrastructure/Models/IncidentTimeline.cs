using System;
using System.ComponentModel.DataAnnotations;

namespace Tracker.Infrastructure.Models
{
    public class IncidentTimeline : BaseEntity
    {
        [Required]
        public Guid IncidentId { get; set; }
        public virtual Incident Incident { get; set; } = null!;

        [Required]
        public DateTime Timestamp { get; set; } = DateTime.UtcNow;

        [Required]
        [MaxLength(100)]
        public string Event { get; set; } = string.Empty;

        public string? Description { get; set; }

        [Required]
        public Guid UpdatedById { get; set; }
        public virtual User UpdatedBy { get; set; } = null!;
    }
}
