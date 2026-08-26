using System.Net.Http;
using System.Net.Http.Json;
using System.Text.Json;
using System.Threading.Tasks;

namespace RockAI.App.Services.Authentication;

public class AuthenticationService : IAuthenticationService
{
    private readonly HttpClient _httpClient;
    private readonly ITokenStorage _tokenStorage;

    public AuthenticationService(HttpClient httpClient, ITokenStorage tokenStorage)
    {
        _httpClient = httpClient;
        _tokenStorage = tokenStorage;
    }

    public async Task<bool> LoginAsync(string email, string password)
    {
        var payload = new { email, password };

        HttpResponseMessage response;
        try
        {
            response = await _httpClient.PostAsJsonAsync("auth/login", payload);
        }
        catch
        {
            return false;
        }

        if (!response.IsSuccessStatusCode)
        {
            return false;
        }

        // Parse token from response JSON. Support common shapes: { "access_token": "..." } or { "token": "..." }
        string? token = null;
        try
        {
            using var stream = await response.Content.ReadAsStreamAsync();
            using var doc = await JsonDocument.ParseAsync(stream);
            if (doc.RootElement.TryGetProperty("access_token", out var at))
            {
                token = at.GetString();
            }
            else if (doc.RootElement.TryGetProperty("token", out var t))
            {
                token = t.GetString();
            }
            else if (doc.RootElement.ValueKind == JsonValueKind.String)
            {
                token = doc.RootElement.GetString();
            }
        }
        catch
        {
            return false;
        }

        if (string.IsNullOrWhiteSpace(token))
            return false;

        await _tokenStorage.SetTokenAsync(token);
        return true;
    }

    public Task LogoutAsync()
    {
        return _tokenStorage.RemoveTokenAsync();
    }

    public Task<string?> GetAccessTokenAsync()
    {
        return _tokenStorage.GetTokenAsync();
    }
}
