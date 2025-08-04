using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Net.Http.Json;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Tracker.Shared.Models;

namespace Tracker.Client.Services
{
    public class IncidentService : IIncidentService
    {
        private readonly HttpClient _httpClient;
        private readonly ILogger<IncidentService> _logger;
        private readonly JsonSerializerOptions _jsonOptions;

        public IncidentService(HttpClient httpClient, ILogger<IncidentService> logger)
        {
            _httpClient = httpClient ?? throw new ArgumentNullException(nameof(httpClient));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            
            _jsonOptions = new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true,
                PropertyNamingPolicy = JsonNamingPolicy.CamelCase
            };
        }

        public async Task<IncidentDto?> GetIncidentByIdAsync(string id)
        {
            try
            {
                var sharedDto = await _httpClient.GetFromJsonAsync<Shared.Models.IncidentDto>($"api/incidents/{id}", _jsonOptions);
                return sharedDto.ToClientDto();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error fetching incident with ID {IncidentId}", id);
                return null;
            }
        }

        public async Task<PagedResult<IncidentDto>> GetIncidentsAsync(int page = 1, int pageSize = 10, string? searchQuery = null, string? status = null)
        {
            try
            {
                var queryParams = new List<string>
                {
                    $"page={page}",
                    $"pageSize={pageSize}"
                };

                if (!string.IsNullOrEmpty(searchQuery))
                    queryParams.Add($"search={Uri.EscapeDataString(searchQuery)}");
                
                if (!string.IsNullOrEmpty(status))
                    queryParams.Add($"status={Uri.EscapeDataString(status)}");

                var queryString = string.Join("&", queryParams);
                var response = await _httpClient.GetFromJsonAsync<PagedResult<Shared.Models.IncidentDto>>($"api/incidents?{queryString}", _jsonOptions);
                
                if (response == null)
                    return new PagedResult<IncidentDto> { Items = new List<IncidentDto>(), TotalCount = 0 };
                
                // Map the shared DTOs to client DTOs
                var clientDtos = new List<IncidentDto>();
                foreach (var item in response.Items)
                {
                    clientDtos.Add(item.ToClientDto());
                }
                
                return new PagedResult<IncidentDto> 
                { 
                    Items = clientDtos, 
                    TotalCount = response.TotalCount,
                    PageNumber = response.PageNumber,
                    PageSize = response.PageSize
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error fetching incidents");
                return new PagedResult<IncidentDto> { Items = new List<IncidentDto>(), TotalCount = 0 };
            }
        }

        public async Task<IncidentDto?> CreateIncidentAsync(IncidentDto incident)
        {
            try
            {
                var sharedDto = incident.ToSharedDto();
                var response = await _httpClient.PostAsJsonAsync("api/incidents", sharedDto, _jsonOptions);
                response.EnsureSuccessStatusCode();
                
                var createdIncident = await response.Content.ReadFromJsonAsync<Shared.Models.IncidentDto>(_jsonOptions);
                return createdIncident?.ToClientDto();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error creating incident");
                return null;
            }
        }

        public async Task<IncidentDto?> UpdateIncidentAsync(IncidentDto incident)
        {
            try
            {
                var sharedDto = incident.ToSharedDto();
                var response = await _httpClient.PutAsJsonAsync($"api/incidents/{incident.Id}", sharedDto, _jsonOptions);
                response.EnsureSuccessStatusCode();
                
                var updatedIncident = await response.Content.ReadFromJsonAsync<Shared.Models.IncidentDto>(_jsonOptions);
                return updatedIncident?.ToClientDto();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating incident with ID {IncidentId}", incident.Id);
                return null;
            }
        }

        public async Task<bool> DeleteIncidentAsync(string id)
        {
            try
            {
                var response = await _httpClient.DeleteAsync($"api/incidents/{id}");
                return response.IsSuccessStatusCode;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error deleting incident with ID {IncidentId}", id);
                throw;
            }
        }

        public async Task<PagedResult<TimelineEntryDto>> GetTimelineEntriesAsync(string incidentId, int page = 1, int pageSize = 10)
        {
            try
            {
                return await _httpClient.GetFromJsonAsync<PagedResult<TimelineEntryDto>>(
                    $"api/incidents/{incidentId}/timeline?page={page}&pageSize={pageSize}", _jsonOptions);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error fetching timeline entries for incident {IncidentId}", incidentId);
                return new PagedResult<TimelineEntryDto>
                {
                    Items = new List<TimelineEntryDto>(),
                    PageNumber = page,
                    PageSize = pageSize,
                    TotalCount = 0
                };
            }
        }

        public async Task<bool> UpdateIncidentStatusAsync(string incidentId, string status, string? comment = null)
        {
            try
            {
                var response = await _httpClient.PutAsJsonAsync(
                    $"api/incidents/{incidentId}/status", 
                    new { Status = status, Comment = comment },
                    _jsonOptions);
                
                response.EnsureSuccessStatusCode();
                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating status for incident {IncidentId}", incidentId);
                return false;
            }
        }

        public async Task<IEnumerable<string>> GetIncidentStatusesAsync()
        {
            try
            {
                return await _httpClient.GetFromJsonAsync<IEnumerable<string>>("api/incidents/statuses", _jsonOptions);
            }
            catch (HttpRequestException ex)
            {
                _logger.LogError(ex, "Error fetching incident statuses");
                return new List<string> { "Open", "In Progress", "Resolved", "Closed" };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "An unexpected error occurred while fetching incident statuses");
                throw;
            }
        }

        public async Task<IEnumerable<string>> GetIncidentPrioritiesAsync()
        {
            try
            {
                return await _httpClient.GetFromJsonAsync<IEnumerable<string>>("api/incidents/priorities", _jsonOptions);
            }
            catch (HttpRequestException ex)
            {
                _logger.LogError(ex, "Error fetching incident priorities");
                return new List<string> { "Low", "Medium", "High", "Critical" };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "An unexpected error occurred while fetching incident priorities");
                throw;
            }
        }
    }
}
