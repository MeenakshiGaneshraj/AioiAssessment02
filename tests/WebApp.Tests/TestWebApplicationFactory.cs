using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.DependencyInjection;
using WebApp.Models;
using WebApp.Services;

namespace WebApp.Tests;

public sealed class TestWebApplicationFactory : WebApplicationFactory<Program>
{
    public Func<string, CancellationToken, Task<AddressValidationResponse>>? ValidationHandler { get; set; }

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.ConfigureServices(services =>
        {
            services.AddSingleton<INzPostAddressClient>(new FakeNzPostAddressClient(
                (query, ct) =>
                    ValidationHandler?.Invoke(query, ct) ??
                    Task.FromResult(new AddressValidationResponse(true, "Address looks valid.", [query]))
            ));
        });
    }

    private sealed class FakeNzPostAddressClient(
        Func<string, CancellationToken, Task<AddressValidationResponse>> handler) : INzPostAddressClient
    {
        public Task<AddressValidationResponse> ValidateAsync(string query, CancellationToken cancellationToken)
            => handler(query, cancellationToken);
    }
}
