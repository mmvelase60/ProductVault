using System.ComponentModel.DataAnnotations;

namespace ProductVault.Models;

public sealed class AuditEvent
{
    public long AuditEventId { get; set; }

    [Required, StringLength(450)]
    public string OwnerId { get; set; } = string.Empty;

    [Required, StringLength(450)]
    public string ActorId { get; set; } = string.Empty;

    [Required, StringLength(80)]
    public string Action { get; set; } = string.Empty;

    [Required, StringLength(80)]
    public string EntityType { get; set; } = string.Empty;

    [Required, StringLength(150)]
    public string EntityId { get; set; } = string.Empty;

    [Required, StringLength(250)]
    public string EntityName { get; set; } = string.Empty;

    [StringLength(1000)]
    public string? Detail { get; set; }

    public DateTime OccurredAt { get; set; }
}
