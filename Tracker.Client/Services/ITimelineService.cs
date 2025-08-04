using System.Collections.Generic;
using System.Threading.Tasks;
using Tracker.Shared.Models;

namespace Tracker.Client.Services
{
    public interface ITimelineService
    {
        Task<IEnumerable<TimelineEntryDto>> GetTimelineForIncidentAsync(string incidentId);
        Task<TimelineEntryDto> AddTimelineEntryAsync(string incidentId, CreateTimelineEntryDto entry);
        Task<bool> DeleteTimelineEntryAsync(string incidentId, string entryId);
    }
}
