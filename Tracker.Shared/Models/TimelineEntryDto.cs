using System;

namespace Tracker.Shared.Models
{
    public class TimelineEntryDto
    {
        public string Id { get; set; }
        public string IncidentId { get; set; }
        public DateTime Timestamp { get; set; }
        public string Event { get; set; }
        public string Description { get; set; }
        public string UpdatedById { get; set; }
        public string UpdatedByName { get; set; }
    }

    public class CreateTimelineEntryDto
    {
        public string Event { get; set; }
        public string Description { get; set; }
    }
}
