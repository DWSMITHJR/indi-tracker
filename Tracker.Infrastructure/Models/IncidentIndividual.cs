using System;
using System.ComponentModel.DataAnnotations;

namespace Tracker.Infrastructure.Models
{
    public class IncidentIndividual : BaseEntity
    {
        public Guid IncidentId { get; set; }
        public virtual Incident Incident { get; set; } = null!;

        public Guid IndividualId { get; set; }
        public virtual Individual Individual { get; set; } = null!;

        [Required]
        [MaxLength(50)]
        public string Role { get; set; } = string.Empty; // 'Witness', 'Affected', 'Reported By', etc.

        [MaxLength(500)]
        public string? Description { get; set; }
    }
}
