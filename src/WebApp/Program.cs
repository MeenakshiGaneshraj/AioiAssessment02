using System.ComponentModel.DataAnnotations;
using Microsoft.Extensions.Options;
using WebApp.Auth;
using WebApp.Models;
using WebApp.Services;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddRazorPages();
builder.Services.AddHttpContextAccessor();
builder.Services.AddDistributedMemoryCache();
builder.Services.AddSession(options =>
{
    options.Cookie.Name = "WebApp.Session";
    options.Cookie.HttpOnly = true;
    options.Cookie.IsEssential = true;
    options.IdleTimeout = TimeSpan.FromMinutes(30);
});

builder.Services.AddOptions<LoginOptions>()
    .Bind(builder.Configuration.GetSection(LoginOptions.SectionName))
    .ValidateDataAnnotations()
    .ValidateOnStart();

builder.Services.AddOptions<NzPostOptions>()
    .Bind(builder.Configuration.GetSection(NzPostOptions.SectionName))
    .ValidateDataAnnotations()
    .ValidateOnStart();

builder.Services.AddHttpClient<INzPostAddressClient, NzPostAddressClient>((serviceProvider, client) =>
{
    var options = serviceProvider.GetRequiredService<IOptions<NzPostOptions>>().Value;
    client.BaseAddress = new Uri(options.BaseUrl);
    client.Timeout = TimeSpan.FromSeconds(options.TimeoutSeconds);
});

var app = builder.Build();

if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Error");
    app.UseHsts();
}

app.UseHttpsRedirection();
app.UseStaticFiles();
app.UseRouting();
app.UseSession();
app.UseAuthorization();

app.MapGet("/api/address/validate", async (
    string? query,
    HttpContext httpContext,
    INzPostAddressClient client,
    CancellationToken cancellationToken) =>
{
    if (httpContext.Session.GetString(SessionKeys.IsAuthenticated) != "true")
    {
        return Results.Unauthorized();
    }

    if (string.IsNullOrWhiteSpace(query))
    {
        return Results.BadRequest(new AddressValidationResponse(false, "Please enter an address.", []));
    }

    try
    {
        var result = await client.ValidateAsync(query, cancellationToken);
        return Results.Ok(result);
    }
    catch (TaskCanceledException)
    {
        return Results.StatusCode(StatusCodes.Status504GatewayTimeout);
    }
    catch (Exception)
    {
        return Results.StatusCode(StatusCodes.Status503ServiceUnavailable);
    }
});

app.MapRazorPages();
app.Run();

public partial class Program;

public sealed class LoginOptions
{
    public const string SectionName = "Login";

    [Required]
    public string Username { get; init; } = string.Empty;

    [Required]
    public string Password { get; init; } = string.Empty;
}

public sealed class NzPostOptions
{
    public const string SectionName = "NzPost";

    [Required]
    public string BaseUrl { get; init; } = "https://api.nzpost.co.nz";

    [Required]
    public string ApiKey { get; init; } = string.Empty;

    [Range(1, 60)]
    public int TimeoutSeconds { get; init; } = 10;
}
