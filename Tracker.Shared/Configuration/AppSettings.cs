namespace Tracker.Shared.Configuration;

public class AppSettings
{
    public string AppTitle { get; set; } = "IndiTracker";
    public string AppFullName { get; set; } = "Individual Tracker";
    public string Secret { get; set; } = "YOUR_SECRET_KEY_HERE_AT_LEAST_32_CHARACTERS_LONG";
}
