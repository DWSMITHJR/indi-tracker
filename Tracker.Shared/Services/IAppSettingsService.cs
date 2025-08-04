using Tracker.Shared.Configuration;

namespace Tracker.Shared.Services;

public interface IAppSettingsService
{
    Task<AppSettings> GetAppSettingsAsync();
}
