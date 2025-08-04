using System.Net.Http.Json;
using System.Text;
using System.Text.Json;
using Tracker.Shared.Models;

namespace Tracker.Client.Services;

public class LogService : ILogService
{
    private readonly HttpClient _httpClient;
    private const string BasePath = "api/logs";

    public LogService(HttpClient httpClient)
    {
        _httpClient = httpClient ?? throw new ArgumentNullException(nameof(httpClient));
    }

    public async Task<LogQueryResult> GetLogsAsync(
        int pageNumber = 1,
        int pageSize = 20,
        string? searchTerm = null,
        string? level = null,
        DateTime? startDate = null,
        DateTime? endDate = null)
    {
        try
        {
            // Build query string
            var queryBuilder = new StringBuilder($"{BasePath}?pageNumber={pageNumber}&pageSize={pageSize}");
            
            if (!string.IsNullOrEmpty(searchTerm))
                queryBuilder.Append($"&searchTerm={Uri.EscapeDataString(searchTerm)}");
                
            if (!string.IsNullOrEmpty(level))
                queryBuilder.Append($"&level={Uri.EscapeDataString(level)}");
                
            if (startDate.HasValue)
                queryBuilder.Append($"&startDate={startDate.Value:o}");
                
            if (endDate.HasValue)
                queryBuilder.Append($"&endDate={endDate.Value:o}");

            var response = await _httpClient.GetAsync(queryBuilder.ToString());
            response.EnsureSuccessStatusCode();

            return await response.Content.ReadFromJsonAsync<LogQueryResult>() 
                   ?? new LogQueryResult { Items = new List<LogEntry>(), TotalCount = 0 };
        }
        catch (HttpRequestException ex)
        {
            // In a real app, you might want to log this error or show a user-friendly message
            Console.WriteLine($"Error fetching logs: {ex.Message}");
            throw;
        }
    }
}
