using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Net.Http.Json;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Tracker.Shared.Models;

namespace Tracker.Client.Services
{
    public class ContactService : IContactService
    {
        private readonly HttpClient _httpClient;
        private readonly ILogger<ContactService> _logger;

        public ContactService(HttpClient httpClient, ILogger<ContactService> logger)
        {
            _httpClient = httpClient;
            _logger = logger;
        }

        public async Task<PagedResult<ContactDto>> GetContactsAsync(int page = 1, int pageSize = 10, string? searchTerm = null)
        {
            try
            {
                var url = $"api/contacts?page={page}&pageSize={pageSize}";
                if (!string.IsNullOrWhiteSpace(searchTerm))
                {
                    url += $"&search={Uri.EscapeDataString(searchTerm)}";
                }
                return await _httpClient.GetFromJsonAsync<PagedResult<ContactDto>>(url);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error fetching contacts");
                throw;
            }
        }
        
        public async Task<IEnumerable<ContactDto>> GetContactsByIncidentIdAsync(string incidentId)
        {
            try
            {
                return await _httpClient.GetFromJsonAsync<IEnumerable<ContactDto>>($"api/incidents/{incidentId}/contacts");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error fetching contacts for incident {incidentId}");
                throw;
            }
        }

        public async Task<ContactDto> GetContactByIdAsync(Guid id)
        {
            try
            {
                return await _httpClient.GetFromJsonAsync<ContactDto>($"api/contacts/{id}");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error fetching contact with ID {id}");
                throw;
            }
        }

        public async Task<ContactDto> CreateContactAsync(ContactDto contact)
        {
            try
            {
                var response = await _httpClient.PostAsJsonAsync("api/contacts", contact);
                response.EnsureSuccessStatusCode();
                return await response.Content.ReadFromJsonAsync<ContactDto>();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error creating contact");
                throw;
            }
        }

        public async Task UpdateContactAsync(Guid id, ContactDto contact)
        {
            try
            {
                var response = await _httpClient.PutAsJsonAsync($"api/contacts/{id}", contact);
                response.EnsureSuccessStatusCode();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error updating contact with ID {id}");
                throw;
            }
        }

        public async Task DeleteContactAsync(Guid id)
        {
            try
            {
                var response = await _httpClient.DeleteAsync($"api/contacts/{id}");
                response.EnsureSuccessStatusCode();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error deleting contact with ID {id}");
                throw;
            }
        }
    }
}
