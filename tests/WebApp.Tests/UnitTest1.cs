using System.Net;
using System.Net.Http.Json;
using Microsoft.AspNetCore.Mvc.Testing;
using WebApp.Models;

namespace WebApp.Tests;

public class AppBehaviorTests
{
    private TestWebApplicationFactory _factory = null!;
    private HttpClient _client = null!;

    [SetUp]
    public void Setup()
    {
        _factory = new TestWebApplicationFactory();
        _client = _factory.CreateClient(new WebApplicationFactoryClientOptions
        {
            AllowAutoRedirect = false
        });
    }

    [TearDown]
    public void TearDown()
    {
        _client.Dispose();
        _factory.Dispose();
    }

    [Test]
    public async Task ProtectedAddressChecker_RedirectsToLogin_WhenUnauthenticated()
    {
        var response = await _client.GetAsync("/AddressChecker");
        Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.Redirect));
        Assert.That(response.Headers.Location?.OriginalString, Is.EqualTo("/Login"));
    }

    [Test]
    public async Task Login_WithInvalidCredentials_ShowsValidationMessage()
    {
        var content = new FormUrlEncodedContent(new Dictionary<string, string>
        {
            ["Input.Username"] = "wrong",
            ["Input.Password"] = "wrong"
        });

        var response = await _client.PostAsync("/Login", content);
        var body = await response.Content.ReadAsStringAsync();

        Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.OK));
        Assert.That(body, Does.Contain("Invalid username or password."));
    }

    [Test]
    public async Task Login_WithValidCredentials_RedirectsToAddressChecker()
    {
        var content = new FormUrlEncodedContent(new Dictionary<string, string>
        {
            ["Input.Username"] = "candidate",
            ["Input.Password"] = "Passw0rd!"
        });

        var response = await _client.PostAsync("/Login", content);

        Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.Redirect));
        Assert.That(response.Headers.Location?.OriginalString, Is.EqualTo("/AddressChecker"));
    }

    [Test]
    public async Task AddressValidation_ReturnsBadRequest_ForEmptyInput()
    {
        await LoginWithValidCredentials();
        var response = await _client.GetAsync("/api/address/validate?query=");
        Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.BadRequest));
    }

    [Test]
    public async Task AddressValidation_ReturnsBadRequest_ForMissingQueryParameter()
    {
        await LoginWithValidCredentials();
        var response = await _client.GetAsync("/api/address/validate");
        Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.BadRequest));
    }

    [Test]
    public async Task AddressValidation_ReturnsValid_ForExactMatch()
    {
        _factory.ValidationHandler = (query, _) => Task.FromResult(new AddressValidationResponse(
            true,
            "Address looks valid.",
            [query]));

        await LoginWithValidCredentials();
        var response = await _client.GetAsync("/api/address/validate?query=15+Willis+Street%2C+Wellington+6011");
        var payload = await response.Content.ReadFromJsonAsync<AddressValidationResponse>();

        Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.OK));
        Assert.That(payload, Is.Not.Null);
        Assert.That(payload!.IsValid, Is.True);
        Assert.That(payload.Message, Is.EqualTo("Address looks valid."));
    }

    [Test]
    public async Task AddressValidation_ReturnsPartialMatch_Response()
    {
        _factory.ValidationHandler = (_, _) => Task.FromResult(new AddressValidationResponse(
            false,
            "Partial match found. Please choose one of the suggestions.",
            ["1 Willis Street, Wellington", "15 Willis Street, Wellington"]));

        await LoginWithValidCredentials();
        var response = await _client.GetAsync("/api/address/validate?query=willis");
        var payload = await response.Content.ReadFromJsonAsync<AddressValidationResponse>();

        Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.OK));
        Assert.That(payload, Is.Not.Null);
        Assert.That(payload!.IsValid, Is.False);
        Assert.That(payload.Suggestions.Count, Is.GreaterThan(0));
    }

    [Test]
    public async Task AddressValidation_ReturnsNoMatch_ForInvalidAddress()
    {
        _factory.ValidationHandler = (_, _) => Task.FromResult(new AddressValidationResponse(
            false,
            "No match found for this address.",
            []));

        await LoginWithValidCredentials();
        var response = await _client.GetAsync("/api/address/validate?query=zzz+nonexistent+place");
        var payload = await response.Content.ReadFromJsonAsync<AddressValidationResponse>();

        Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.OK));
        Assert.That(payload, Is.Not.Null);
        Assert.That(payload!.IsValid, Is.False);
        Assert.That(payload.Message, Is.EqualTo("No match found for this address."));
        Assert.That(payload.Suggestions, Is.Empty);
    }

    [Test]
    public async Task AddressValidation_Returns503_WhenDependencyFails()
    {
        _factory.ValidationHandler = (_, _) => throw new HttpRequestException("upstream failed");

        await LoginWithValidCredentials();
        var response = await _client.GetAsync("/api/address/validate?query=test");
        Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.ServiceUnavailable));
    }

    [Test]
    public async Task AddressValidation_Returns504_WhenDependencyTimesOut()
    {
        _factory.ValidationHandler = (_, _) => throw new TaskCanceledException("request timed out");

        await LoginWithValidCredentials();
        var response = await _client.GetAsync("/api/address/validate?query=test");
        Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.GatewayTimeout));
    }

    [Test]
    public async Task AddressValidation_ReturnsUnauthorized_WithoutLogin()
    {
        var response = await _client.GetAsync("/api/address/validate?query=test");
        Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.Unauthorized));
    }

    [Test]
    public async Task Login_WithEmptyUsername_ShowsValidationMessage()
    {
        var content = new FormUrlEncodedContent(new Dictionary<string, string>
        {
            ["Input.Username"] = "",
            ["Input.Password"] = "Passw0rd!"
        });

        var response = await _client.PostAsync("/Login", content);
        var body = await response.Content.ReadAsStringAsync();

        Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.OK));
        Assert.That(body, Does.Contain("Username and password are required.").Or.Contain("required"));
    }

    [Test]
    public async Task Login_WithEmptyPassword_ShowsValidationMessage()
    {
        var content = new FormUrlEncodedContent(new Dictionary<string, string>
        {
            ["Input.Username"] = "candidate",
            ["Input.Password"] = ""
        });

        var response = await _client.PostAsync("/Login", content);
        var body = await response.Content.ReadAsStringAsync();

        Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.OK));
        Assert.That(body, Does.Contain("Username and password are required.").Or.Contain("required"));
    }

    [Test]
    public async Task Login_WithBothFieldsEmpty_ShowsValidationMessage()
    {
        var content = new FormUrlEncodedContent(new Dictionary<string, string>
        {
            ["Input.Username"] = "",
            ["Input.Password"] = ""
        });

        var response = await _client.PostAsync("/Login", content);
        var body = await response.Content.ReadAsStringAsync();

        Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.OK));
        Assert.That(body, Does.Contain("Username and password are required.").Or.Contain("required"));
    }

    private Task<HttpResponseMessage> LoginWithValidCredentials()
    {
        var content = new FormUrlEncodedContent(new Dictionary<string, string>
        {
            ["Input.Username"] = "candidate",
            ["Input.Password"] = "Passw0rd!"
        });

        return _client.PostAsync("/Login", content);
    }
}
