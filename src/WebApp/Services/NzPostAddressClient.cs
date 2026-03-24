using System.Text.Json;
using Microsoft.Extensions.Options;
using WebApp.Models;

namespace WebApp.Services;

public sealed class NzPostAddressClient(HttpClient httpClient, IOptions<NzPostOptions> options) : INzPostAddressClient
{
    public async Task<AddressValidationResponse> ValidateAsync(string query, CancellationToken cancellationToken)
    {
        var request = new HttpRequestMessage(HttpMethod.Get, $"/postcodechecker/1.0/suggest.json?q={Uri.EscapeDataString(query)}");
        request.Headers.Add("api_key", options.Value.ApiKey);

        using var response = await httpClient.SendAsync(request, cancellationToken);
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
            if (entry.TryGetProperty("full_address", out var fullAddress) &&
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
