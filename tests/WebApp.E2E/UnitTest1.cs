using System.Text.RegularExpressions;
using Microsoft.Playwright.NUnit;

namespace WebApp.E2E;

public class SmokeTests : PageTest
{
    private readonly string _baseUrl = Environment.GetEnvironmentVariable("E2E_BASE_URL") ?? "http://127.0.0.1:5099";

    [Test]
    public async Task Login_Succeeds_And_ShowsAddressChecker()
    {
        await Page.GotoAsync($"{_baseUrl}/Login");
        await Page.FillAsync("input[name='Input.Username']", "candidate");
        await Page.FillAsync("input[name='Input.Password']", "Passw0rd!");
        await Page.ClickAsync("button:has-text('Sign in')");

        await Expect(Page).ToHaveURLAsync($"{_baseUrl}/AddressChecker");
        await Expect(Page.Locator("h2")).ToContainTextAsync("NZ Address Checker");
    }

    [Test]
    public async Task Login_Fails_WithWrongCredentials()
    {
        await Page.GotoAsync($"{_baseUrl}/Login");
        await Page.FillAsync("input[name='Input.Username']", "bad");
        await Page.FillAsync("input[name='Input.Password']", "bad");
        await Page.ClickAsync("button:has-text('Sign in')");

        await Expect(Page.Locator(".alert-warning")).ToContainTextAsync("Invalid username or password.");
    }

    [Test]
    public async Task Login_Fails_WithEmptyFields()
    {
        await Page.GotoAsync($"{_baseUrl}/Login");
        await Page.ClickAsync("button:has-text('Sign in')");

        await Expect(Page).ToHaveURLAsync(new Regex($@"{Regex.Escape(_baseUrl)}/Login"));
    }

    [Test]
    public async Task AddressChecker_RedirectsToLogin_WhenNotAuthenticated()
    {
        await Page.GotoAsync($"{_baseUrl}/AddressChecker");

        await Expect(Page).ToHaveURLAsync($"{_baseUrl}/Login");
    }

    [Test]
    public async Task AddressChecker_ShowsSuggestions_WhenTypingPartialAddress()
    {
        await LoginAndNavigateToAddressChecker();

        await Page.FillAsync("#addressInput", "15 willis");
        await Page.WaitForSelectorAsync("#suggestions .list-group-item", new() { Timeout = 10000 });

        var suggestionItems = Page.Locator("#suggestions .list-group-item");
        var count = await suggestionItems.CountAsync();
        Assert.That(count, Is.GreaterThan(0), "Expected address suggestions to appear");
    }

    [Test]
    public async Task AddressChecker_SelectingSuggestion_PopulatesInputAndValidates()
    {
        await LoginAndNavigateToAddressChecker();

        await Page.FillAsync("#addressInput", "15 willis");
        await Page.WaitForSelectorAsync("#suggestions .list-group-item", new() { Timeout = 10000 });

        var firstSuggestion = Page.Locator("#suggestions .list-group-item").First;
        var suggestionText = await firstSuggestion.TextContentAsync();
        await firstSuggestion.ClickAsync();

        var inputValue = await Page.InputValueAsync("#addressInput");
        Assert.That(inputValue, Is.EqualTo(suggestionText?.Trim()),
            "Clicking a suggestion should populate the address input");

        await Expect(Page.Locator("#statusMessage")).ToBeVisibleAsync();
    }

    [Test]
    public async Task AddressChecker_ShowsStatusMessage_WhenAddressEntered()
    {
        await LoginAndNavigateToAddressChecker();

        await Page.FillAsync("#addressInput", "test address");
        await Page.WaitForSelectorAsync("#statusMessage:not(.d-none)", new() { Timeout = 10000 });

        await Expect(Page.Locator("#statusMessage")).ToBeVisibleAsync();
    }

    private async Task LoginAndNavigateToAddressChecker()
    {
        await Page.GotoAsync($"{_baseUrl}/Login");
        await Page.FillAsync("input[name='Input.Username']", "candidate");
        await Page.FillAsync("input[name='Input.Password']", "Passw0rd!");
        await Page.ClickAsync("button:has-text('Sign in')");
        await Expect(Page).ToHaveURLAsync($"{_baseUrl}/AddressChecker");
    }
}
