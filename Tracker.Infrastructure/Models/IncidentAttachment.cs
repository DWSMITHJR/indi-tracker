using System;
using System.ComponentModel.DataAnnotations;

namespace Tracker.Infrastructure.Models
{
    public class IncidentAttachment : BaseEntity
    {
        [Required]
        public Guid IncidentId { get; set; }
        public virtual Incident Incident { get; set; } = null!;

        [Required]
        [MaxLength(255)]
        public string FileName { get; set; } = string.Empty;

        [Required]
        [MaxLength(100)]
        public string ContentType { get; set; } = string.Empty;

        [Required]
        public long FileSize { get; set; }

        [Required]
        public string FilePath { get; set; } = string.Empty;

        [MaxLength(500)]
        public string? Description { get; set; }

        [Required]
        public Guid UploadedById { get; set; }
        public virtual User UploadedBy { get; set; } = null!;
    }
}
