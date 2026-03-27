// Models/AuditableEntity.cs
using System.ComponentModel.DataAnnotations;
namespace EcommerceAPI.Models;

public abstract class AuditableEntity
{
    [StringLength(100)]
    public string? CreatedBy { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    [StringLength(100)]
    public string? UpdatedBy { get; set; }
    public DateTime? UpdatedAt { get; set; }
}