namespace WebApp.Models;

public sealed record AddressValidationResponse(
    bool IsValid,
    string Message,
    IReadOnlyList<string> Suggestions);
