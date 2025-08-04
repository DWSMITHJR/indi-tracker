using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Net.Http.Json;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Tracker.Shared.Models;

namespace Tracker.Client.Services
{
    public class OrganizationService : IOrganizationService
    {
        private readonly HttpClient _httpClient;
        private readonly ILogger<OrganizationService> _logger;

        public OrganizationService(HttpClient httpClient, ILogger<OrganizationService> logger)
        {
            _httpClient = httpClient;
            _logger = logger;
        }

        public async Task<PagedResult<OrganizationDto>> GetOrganizationsAsync(int page = 1, int pageSize = 10, string? searchTerm = null)
        {
            try
            {
                var url = $"api/organizations?page={page}&pageSize={pageSize}";
                if (!string.IsNullOrWhiteSpace(searchTerm))
                {
                    url += $"&search={Uri.EscapeDataString(searchTerm)}";
                }
                return await _httpClient.GetFromJsonAsync<PagedResult<OrganizationDto>>(url);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error fetching organizations");
                throw;
            }
        }

        public async Task<OrganizationDto> GetOrganizationByIdAsync(Guid id)
        {
            try
            {
                return await _httpClient.GetFromJsonAsync<OrganizationDto>($"api/organizations/{id}");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error fetching organization with ID {id}");
                throw;
            }
        }

        public async Task<OrganizationDto> CreateOrganizationAsync(OrganizationDto organization)
        {
            try
            {
                var response = await _httpClient.PostAsJsonAsync("api/organizations", organization);
                response.EnsureSuccessStatusCode();
                return await response.Content.ReadFromJsonAsync<OrganizationDto>();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error creating organization");
                throw;
            }
        }

        public async Task UpdateOrganizationAsync(Guid id, OrganizationDto organization)
        {
            try
            {
                var response = await _httpClient.PutAsJsonAsync($"api/organizations/{id}", organization);
                response.EnsureSuccessStatusCode();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error updating organization with ID {id}");
                throw;
            }
        }

        public async Task DeleteOrganizationAsync(Guid id)
        {
            try
            {
                var response = await _httpClient.DeleteAsync($"api/organizations/{id}");
                response.EnsureSuccessStatusCode();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error deleting organization with ID {id}");
                throw;
            }
        }
    }
}
