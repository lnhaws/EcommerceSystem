using System.ComponentModel.DataAnnotations;

namespace EcommerceAPI.Models
{
    // --- CATALOG ---
    public class Category : AuditableEntity
    {
        [Key] public int Id { get; set; }
        [Required, StringLength(100)] public string Name { get; set; } = string.Empty;
        public int? ParentId { get; set; }
        public bool IsActive { get; set; } = true;
    }
}
