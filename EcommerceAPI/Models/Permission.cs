using System.ComponentModel.DataAnnotations;

namespace EcommerceAPI.Models
{
    public class Permission
    {
        [Key] public Guid Id { get; set; }
        [Required, StringLength(50)] public string Code { get; set; } = string.Empty;
    }
}
