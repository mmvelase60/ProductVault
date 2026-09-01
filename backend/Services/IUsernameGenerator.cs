namespace ProductVault.Services;

public interface IUsernameGenerator
{
    Task<string> NextAsync(string firstName, string surname, string? excludedUserId = null, CancellationToken cancellationToken = default);
}
