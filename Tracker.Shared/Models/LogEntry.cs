using System;
using System.Text.Json.Serialization;

namespace Tracker.Shared.Models;

public class LogEntry
{
    public int Id { get; set; }
    public DateTime Timestamp { get; set; }
    public string? Level { get; set; }
    public string? Message { get; set; }
    public string? Source { get; set; }
    public string? UserId { get; set; }
    public string? Exception { get; set; }
    public string? Properties { get; set; }
}
