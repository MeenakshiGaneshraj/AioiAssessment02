using WebApp.Models;

namespace WebApp.Services;

public interface INzPostAddressClient
{
    Task<AddressValidationResponse> ValidateAsync(string query, CancellationToken cancellationToken);
}
