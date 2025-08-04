using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Tracker.Shared.Models;

namespace Tracker.Client.Services
{
    public interface IContactService
    {
        Task<PagedResult<ContactDto>> GetContactsAsync(int page = 1, int pageSize = 10, string? searchTerm = null);
        Task<ContactDto> GetContactByIdAsync(Guid id);
        Task<IEnumerable<ContactDto>> GetContactsByIncidentIdAsync(string incidentId);
        Task<ContactDto> CreateContactAsync(ContactDto contact);
        Task UpdateContactAsync(Guid id, ContactDto contact);
        Task DeleteContactAsync(Guid id);
    }
}
