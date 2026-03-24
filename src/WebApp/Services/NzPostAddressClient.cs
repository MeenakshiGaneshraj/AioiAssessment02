using System.Net.Http.Headers;
using System.Text.Json;
using Microsoft.Extensions.Options;
using WebApp.Models;

namespace WebApp.Services;

public sealed class NzPostAddressClient : INzPostAddressClient
{
    private readonly HttpClient _httpClient;
    private readonly NzPostOptions _options;
    private readonly IHttpClientFactory _httpClientFactory;

    private string? _accessToken;
    private DateTimeOffset _tokenExpiry = DateTimeOffset.MinValue;
    private readonly SemaphoreSlim _tokenLock = new(1, 1);

    public NzPostAddressClient(HttpClient httpClient, IOptions<NzPostOptions> options, IHttpClientFactory httpClientFactory)
    {
        _httpClient = httpClient;
        _options = options.Value;
        _httpClientFactory = httpClientFactory;
    }

    public async Task<AddressValidationResponse> ValidateAsync(string query, CancellationToken cancellationToken)
    {
        var token = await GetAccessTokenAsync(cancellationToken);

        var request = new HttpRequestMessage(HttpMethod.Get,
            $"/addresschecker/1.0/suggest?q={Uri.EscapeDataString(query)}");
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);
        request.Headers.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));

        using var response = await _httpClient.SendAsync(request, cancellationToken);
        if (!response.IsSuccessStatusCode)
        {
            return new AddressValidationResponse(false, "Address service unavailable. Please try again.", []);
        }

        var body = await response.Content.ReadAsStringAsync(cancellationToken);
        var suggestions = ExtractSuggestions(body);
        if (suggestions.Count == 0)
        {
            return new AddressValidationResponse(false, "No match found for this address.", []);
        }

        var exactMatch = suggestions.Any(s => string.Equals(s, query, StringComparison.OrdinalIgnoreCase));
        if (exactMatch)
        {
            return new AddressValidationResponse(true, "Address looks valid.", suggestions);
        }

        return new AddressValidationResponse(false, "Partial match found. Please choose one of the suggestions.", suggestions);
    }

    private async Task<string> GetAccessTokenAsync(CancellationToken cancellationToken)
    {
        if (_accessToken is not null && DateTimeOffset.UtcNow < _tokenExpiry)
        {
            return _accessToken;
        }

        await _tokenLock.WaitAsync(cancellationToken);
        try
        {
            if (_accessToken is not null && DateTimeOffset.UtcNow < _tokenExpiry)
            {
                return _accessToken;
            }

            using var tokenClient = _httpClientFactory.CreateClient();
            var tokenRequest = new HttpRequestMessage(HttpMethod.Post, _options.OAuthUrl)
            {
                Content = new FormUrlEncodedContent(new Dictionary<string, string>
                {
                    ["grant_type"] = "client_credentials",
                    ["client_id"] = _options.ClientId,
                    ["client_secret"] = _options.ClientSecret
                })
            };

            using var tokenResponse = await tokenClient.SendAsync(tokenRequest, cancellationToken);
            tokenResponse.EnsureSuccessStatusCode();

            var json = await tokenResponse.Content.ReadAsStringAsync(cancellationToken);
            using var doc = JsonDocument.Parse(json);

            _accessToken = doc.RootElement.GetProperty("access_token").GetString()
                           ?? throw new InvalidOperationException("OAuth token response missing access_token.");

            var expiresIn = doc.RootElement.TryGetProperty("expires_in", out var exp)
                ? exp.GetInt32()
                : 86399;

            _tokenExpiry = DateTimeOffset.UtcNow.AddSeconds(expiresIn - 60);

            return _accessToken;
        }
        finally
        {
            _tokenLock.Release();
        }
    }

    private static List<string> ExtractSuggestions(string json)
    {
        using var document = JsonDocument.Parse(json);
        if (!document.RootElement.TryGetProperty("addresses", out var addresses) ||
            addresses.ValueKind != JsonValueKind.Array)
        {
            return [];
        }

        var values = new List<string>();
        foreach (var entry in addresses.EnumerateArray())
        {
            if (entry.TryGetProperty("FullAddress", out var fullAddress) &&
                fullAddress.ValueKind == JsonValueKind.String)
            {
                var value = fullAddress.GetString();
                if (!string.IsNullOrWhiteSpace(value))
                {
                    values.Add(value);
                }
            }
        }

        return values;
    }
}
