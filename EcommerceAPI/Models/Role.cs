using System.ComponentModel.DataAnnotations;

namespace EcommerceAPI.Models
{
    public class Role : AuditableEntity
    {
        [Key] public Guid Id { get; set; }
        [Required, StringLength(50)] public string Name { get; set; } = string.Empty;
        public string? Description { get; set; }
    }
}
