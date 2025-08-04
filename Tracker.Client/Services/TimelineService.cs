using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Net.Http.Json;
using System.Text.Json;
using System.Text;
using System.Net;
using Microsoft.JSInterop;
using Tracker.Shared.Models;

namespace Tracker.Client.Services
{
    public class TimelineService : ITimelineService
    {
        private readonly HttpClient _httpClient;
        private readonly ILogger<TimelineService> _logger;
        private readonly IJSRuntime _jsRuntime;
        private const string AuthTokenKey = "authToken";

        public TimelineService(HttpClient httpClient, ILogger<TimelineService> logger, IJSRuntime jsRuntime)
        {
            _httpClient = httpClient;
            _logger = logger;
            _jsRuntime = jsRuntime;
        }

        private async Task<string> GetAuthTokenAsync()
        {
            try
            {
                return await _jsRuntime.InvokeAsync<string>("localStorage.getItem", AuthTokenKey);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving authentication token");
                return null;
            }
        }

        private async Task<HttpClient> GetAuthenticatedClientAsync()
        {
            var token = await GetAuthTokenAsync();
            if (!string.IsNullOrEmpty(token) && !_httpClient.DefaultRequestHeaders.Contains("Authorization"))
            {
                _httpClient.DefaultRequestHeaders.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", token);
            }
            return _httpClient;
        }

        public async Task<IEnumerable<TimelineEntryDto>> GetTimelineForIncidentAsync(string incidentId)
        {
            try
            {
                var client = await GetAuthenticatedClientAsync();
                var response = await client.GetAsync($"api/incidents/{incidentId}/timeline");
                
                if (response.IsSuccessStatusCode)
                {
                    var result = await response.Content.ReadFromJsonAsync<IEnumerable<TimelineEntryDto>>();
                    return result ?? Enumerable.Empty<TimelineEntryDto>();
                }

                await HandleApiError(response, "Error fetching timeline entries");
                return Enumerable.Empty<TimelineEntryDto>();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error fetching timeline for incident {IncidentId}", incidentId);
                throw new ApplicationException("An error occurred while fetching the timeline. Please try again later.", ex);
            }
        }

        public async Task<TimelineEntryDto> AddTimelineEntryAsync(string incidentId, CreateTimelineEntryDto entry)
        {
            try
            {
                var client = await GetAuthenticatedClientAsync();
                var response = await client.PostAsJsonAsync($"api/incidents/{incidentId}/timeline", entry);
                
                if (response.IsSuccessStatusCode)
                {
                    return await response.Content.ReadFromJsonAsync<TimelineEntryDto>();
                }

                await HandleApiError(response, "Error adding timeline entry");
                return null;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error adding timeline entry to incident {IncidentId}", incidentId);
                throw new ApplicationException("An error occurred while adding the timeline entry. Please try again.", ex);
            }
        }

        public async Task<bool> DeleteTimelineEntryAsync(string incidentId, string entryId)
        {
            try
            {
                var client = await GetAuthenticatedClientAsync();
                var response = await client.DeleteAsync($"api/incidents/{incidentId}/timeline/{entryId}");
                
                if (!response.IsSuccessStatusCode)
                {
                    await HandleApiError(response, "Error deleting timeline entry");
                    return false;
                }
                
                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error deleting timeline entry {EntryId} from incident {IncidentId}", entryId, incidentId);
                throw new ApplicationException("An error occurred while deleting the timeline entry. Please try again.", ex);
            }
        }

        private async Task HandleApiError(HttpResponseMessage response, string defaultMessage)
        {
            var errorMessage = defaultMessage;
            
            try
            {
                if (response.StatusCode == HttpStatusCode.Unauthorized)
                {
                    errorMessage = "Your session has expired. Please log in again.";
                    // Optionally trigger logout
                    // await _jsRuntime.InvokeVoidAsync("localStorage.removeItem", AuthTokenKey);
                    // _navigationManager.NavigateTo("/login");
                }
                else if (response.StatusCode == HttpStatusCode.Forbidden)
                {
                    errorMessage = "You don't have permission to perform this action.";
                }
                else if (response.StatusCode == HttpStatusCode.NotFound)
                {
                    errorMessage = "The requested resource was not found.";
                }
                else if (response.StatusCode == HttpStatusCode.BadRequest)
                {
                    var errorContent = await response.Content.ReadAsStringAsync();
                    if (!string.IsNullOrEmpty(errorContent))
                    {
                        try
                        {
                            var errorObj = JsonDocument.Parse(errorContent);
                            if (errorObj.RootElement.TryGetProperty("errors", out var errors))
                            {
                                var errorMessages = new List<string>();
                                foreach (var error in errors.EnumerateObject())
                                {
                                    var messages = error.Value.EnumerateArray()
                                        .Select(x => x.GetString())
                                        .Where(x => x != null)
                                        .Select(x => x!);
                                    errorMessages.AddRange(messages);
                                }
                                errorMessage = string.Join(" ", errorMessages);
                            }
                            else if (errorObj.RootElement.TryGetProperty("title", out var title))
                            {
                                errorMessage = title.GetString();
                            }
                        }
                        catch (JsonException)
                        {
                            errorMessage = await response.Content.ReadAsStringAsync();
                        }
                    }
                }
                else
                {
                    errorMessage = $"An error occurred: {response.StatusCode}";
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error processing API error response");
                errorMessage = "An unexpected error occurred. Please try again later.";
            }

            throw new ApplicationException(errorMessage);
        }
    }
}
