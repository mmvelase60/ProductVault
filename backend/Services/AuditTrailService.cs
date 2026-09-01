using ProductVault.Data;
using ProductVault.Models;

namespace ProductVault.Services;

public sealed class AuditTrailService(ApplicationDbContext db) : IAuditTrailService
{
    public void Record(string ownerId, string actorId, string action, string entityType, string entityId, string entityName, string? detail = null)
    {
        db.AuditEvents.Add(new AuditEvent
        {
            OwnerId = ownerId,
            ActorId = actorId,
            Action = action,
            EntityType = entityType,
            EntityId = entityId,
            EntityName = entityName,
            Detail = detail,
            OccurredAt = DateTime.UtcNow
        });
    }
}
