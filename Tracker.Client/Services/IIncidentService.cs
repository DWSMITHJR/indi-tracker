using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Tracker.Shared.Models;

namespace Tracker.Client.Services
{
    public interface IIncidentService
    {
        Task<IncidentDto?> GetIncidentByIdAsync(string id);
        Task<PagedResult<IncidentDto>> GetIncidentsAsync(int page = 1, int pageSize = 10, string? searchQuery = null, string? status = null);
        Task<IncidentDto?> CreateIncidentAsync(IncidentDto incident);
        Task<IncidentDto?> UpdateIncidentAsync(IncidentDto incident);
        Task<bool> UpdateIncidentStatusAsync(string incidentId, string status, string? comment = null);
        Task<bool> DeleteIncidentAsync(string id);
        Task<PagedResult<TimelineEntryDto>> GetTimelineEntriesAsync(string incidentId, int page = 1, int pageSize = 10);
        Task<IEnumerable<string>> GetIncidentStatusesAsync();
        Task<IEnumerable<string>> GetIncidentPrioritiesAsync();
    }
}
