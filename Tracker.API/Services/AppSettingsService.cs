using Microsoft.Extensions.Options;
using Tracker.Shared.Configuration;
using Tracker.Shared.Services;

namespace Tracker.API.Services;

public class AppSettingsService : IAppSettingsService
{
    private readonly AppSettings _appSettings;

    public AppSettingsService(IOptions<AppSettings> appSettings)
    {
        _appSettings = appSettings.Value;
    }

    public Task<AppSettings> GetAppSettingsAsync()
    {
        return Task.FromResult(_appSettings);
    }
}
