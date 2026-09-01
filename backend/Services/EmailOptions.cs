namespace ProductVault.Services;

public sealed class EmailOptions
{
    public const string SectionName = "Email";

    public string Host { get; init; } = "smtp.gmail.com";
    public int Port { get; init; } = 587;
    public string Username { get; init; } = string.Empty;
    public string Password { get; init; } = string.Empty;
    public string FromAddress { get; init; } = string.Empty;
    public string FromName { get; init; } = "ProductVault";
    public string FrontendBaseUrl { get; init; } = "http://localhost:4200";
    public int VerificationCodeLifetimeMinutes { get; init; } = 10;
}
