using System.Security.Claims;
using System.Text.Json;
using Blazored.LocalStorage;
using Microsoft.AspNetCore.Components.Authorization;

namespace Tracker.Client.Services;

public class CustomAuthStateProvider : AuthenticationStateProvider
{
    private readonly ILocalStorageService _localStorage;
    private readonly HttpClient _httpClient;
    private readonly IAuthService _authService;

    public CustomAuthStateProvider(
        ILocalStorageService localStorage,
        HttpClient httpClient,
        IAuthService authService)
    {
        _localStorage = localStorage;
        _httpClient = httpClient;
        _authService = authService;
    }

    public override async Task<AuthenticationState> GetAuthenticationStateAsync()
    {
        var savedToken = await _localStorage.GetItemAsync<string>("authToken");

        if (string.IsNullOrWhiteSpace(savedToken))
        {
            return new AuthenticationState(new ClaimsPrincipal(new ClaimsIdentity()));
        }

        try
        {
            // Check if the token is expired
            var tokenExpiration = GetTokenExpiration(savedToken);
            if (tokenExpiration < DateTime.UtcNow)
            {
                // Token is expired, try to refresh it
                var refreshResult = await _authService.RefreshToken();
                if (!refreshResult.Success)
                {
                    // Refresh failed, log the user out
                    await _authService.Logout();
                    return new AuthenticationState(new ClaimsPrincipal(new ClaimsIdentity()));
                }
                
                // Get the new token
                savedToken = await _localStorage.GetItemAsync<string>("authToken");
            }

            // Set the authorization header for subsequent requests
            _httpClient.DefaultRequestHeaders.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", savedToken);

            var claims = ParseClaimsFromJwt(savedToken);
            var user = new ClaimsPrincipal(new ClaimsIdentity(claims, "jwt"));
            
            return new AuthenticationState(user);
        }
        catch
        {
            // If there's an error, log the user out
            await _authService.Logout();
            return new AuthenticationState(new ClaimsPrincipal(new ClaimsIdentity()));
        }
    }

    public void MarkUserAsAuthenticated(string token)
    {
        var claims = ParseClaimsFromJwt(token);
        var user = new ClaimsPrincipal(new ClaimsIdentity(claims, "jwt"));
        var authState = Task.FromResult(new AuthenticationState(user));
        NotifyAuthenticationStateChanged(authState);
    }

    public void MarkUserAsLoggedOut()
    {
        var anonymousUser = new ClaimsPrincipal(new ClaimsIdentity());
        var authState = Task.FromResult(new AuthenticationState(anonymousUser));
        NotifyAuthenticationStateChanged(authState);
    }

    private static IEnumerable<Claim> ParseClaimsFromJwt(string jwt)
    {
        var payload = jwt.Split('.')[1];
        var jsonBytes = ParseBase64WithoutPadding(payload);
        var keyValuePairs = JsonSerializer.Deserialize<Dictionary<string, object>>(jsonBytes);
        
        return keyValuePairs.Select(kvp => new Claim(kvp.Key, kvp.Value?.ToString() ?? string.Empty));
    }

    private static byte[] ParseBase64WithoutPadding(string base64)
    {
        switch (base64.Length % 4)
        {
            case 2: base64 += "=="; break;
            case 3: base64 += "="; break;
        }
        return Convert.FromBase64String(base64);
    }

    private static DateTime GetTokenExpiration(string token)
    {
        var payload = token.Split('.')[1];
        var jsonBytes = ParseBase64WithoutPadding(payload);
        var keyValuePairs = JsonSerializer.Deserialize<Dictionary<string, JsonElement>>(jsonBytes);
        
        if (keyValuePairs.TryGetValue("exp", out var expValue) && 
            expValue.TryGetInt64(out var expUnixTime))
        {
            return DateTimeOffset.FromUnixTimeSeconds(expUnixTime).UtcDateTime;
        }
        
        return DateTime.MinValue;
    }
}
