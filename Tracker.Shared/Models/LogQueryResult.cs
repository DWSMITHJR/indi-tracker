using System.Collections.Generic;

namespace Tracker.Shared.Models
{
    public class LogQueryResult
    {
        public List<LogEntry> Items { get; set; } = new List<LogEntry>();
        public int TotalCount { get; set; }
    }
}
