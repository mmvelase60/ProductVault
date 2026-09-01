using System.ComponentModel.DataAnnotations;

namespace ProductVault.Models;

public sealed class InventoryMovement
{
    public long InventoryMovementId { get; set; }

    [Required, StringLength(450)]
    public string OwnerId { get; set; } = string.Empty;

    public int ProductId { get; set; }

    [Required, StringLength(150)]
    public string ProductName { get; set; } = string.Empty;

    [Required, StringLength(40)]
    public string Operation { get; set; } = string.Empty;

    public int QuantityBefore { get; set; }
    public int QuantityAfter { get; set; }

    [StringLength(500)]
    public string? Note { get; set; }

    [Required, StringLength(450)]
    public string PerformedBy { get; set; } = string.Empty;

    public DateTime OccurredAt { get; set; }
}
