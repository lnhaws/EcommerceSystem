using System.ComponentModel.DataAnnotations;

namespace EcommerceAPI.Models
{
    public class UserAddress : AuditableEntity
    {
        [Key] public Guid Id { get; set; }
        public Guid UserId { get; set; }
        [Required, StringLength(100)] public string ReceiverName { get; set; } = string.Empty;
        [Required, StringLength(20)] public string Phone { get; set; } = string.Empty;
        [Required] public string AddressDetails { get; set; } = string.Empty;
        public bool IsDefault { get; set; }
    }
}
