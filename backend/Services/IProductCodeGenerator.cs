namespace ProductVault;

public interface IProductCodeGenerator
{
    Task<string> NextAsync(DateTime utcNow, CancellationToken cancellationToken = default);
}
