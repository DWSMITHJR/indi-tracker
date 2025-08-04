using Tracker.Shared.Models;

namespace Tracker.Shared.Services;

public interface ILogService
{
    Task<LogQueryResult> GetLogsAsync(
        int pageNumber = 1,
        int pageSize = 20,
        string searchTerm = null,
        string level = null,
        DateTime? startDate = null,
        DateTime? endDate = null);
}
