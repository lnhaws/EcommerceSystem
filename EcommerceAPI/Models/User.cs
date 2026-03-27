using System.ComponentModel.DataAnnotations;

namespace EcommerceAPI.Models
{
    public class User : AuditableEntity
    {
        [Key] public Guid Id { get; set; }
        [Required, StringLength(100)] public string Email { get; set; } = string.Empty;
        [Required] public string PasswordHash { get; set; } = string.Empty;
        [Required, StringLength(100)] public string FullName { get; set; } = string.Empty;
        public bool IsActive { get; set; } = true;
    }
}
