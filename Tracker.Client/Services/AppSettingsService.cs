using System.Net.Http.Json;
using Tracker.Shared.Configuration;
using Tracker.Shared.Services;

namespace Tracker.Client.Services;

public class AppSettingsService : IAppSettingsService
{
    private readonly HttpClient _httpClient;
    private AppSettings? _appSettings;
    private bool _initialized = false;
    private readonly SemaphoreSlim _semaphore = new(1, 1);

    public AppSettingsService(HttpClient httpClient)
    {
        _httpClient = httpClient;
    }

    public async Task<AppSettings> GetAppSettingsAsync()
    {
        if (_initialized)
        {
            return _appSettings!;
        }

        await _semaphore.WaitAsync();
        try
        {
            if (!_initialized)
            {
                _appSettings = await _httpClient.GetFromJsonAsync<AppSettings>("api/appsettings");
                _initialized = true;
            }
            return _appSettings!;
        }
        finally
        {
            _semaphore.Release();
        }
    }
}
