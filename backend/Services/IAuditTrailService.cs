namespace ProductVault.Services;

public interface IAuditTrailService
{
    void Record(string ownerId, string actorId, string action, string entityType, string entityId, string entityName, string? detail = null);
}
