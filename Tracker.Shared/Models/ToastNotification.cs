using System;

namespace Tracker.Shared.Models
{
    public enum ToastLevel
    {
        Info,
        Success,
        Warning,
        Error
    }

    public class Toast
    {
        public Guid Id { get; } = Guid.NewGuid();
        public string Title { get; set; }
        public string Message { get; set; }
        public DateTimeOffset Timestamp { get; } = DateTimeOffset.Now;
        public ToastLevel Level { get; set; } = ToastLevel.Info;
        public TimeSpan AutoCloseDelay { get; set; } = TimeSpan.FromSeconds(5);
        public bool AutoClose { get; set; } = true;
        public bool ShowProgressBar { get; set; } = true;
    }
}
