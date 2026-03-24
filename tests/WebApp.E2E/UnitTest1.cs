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
}
