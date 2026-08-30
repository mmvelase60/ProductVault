using System.ComponentModel.DataAnnotations;

namespace ProductVault.Models;

public abstract class AuditableEntity
{
    [Required, StringLength(450)]
    public string OwnerId { get; set; } = string.Empty;

    [Required, StringLength(450)]
    public string CreatedBy { get; set; } = string.Empty;

    public DateTime CreatedDate { get; set; }
    [StringLength(450)] public string? UpdatedBy { get; set; }
    public DateTime? UpdatedDate { get; set; }

    [Timestamp]
    public byte[] RowVersion { get; set; } = Array.Empty<byte>();
}
