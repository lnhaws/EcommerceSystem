using System.ComponentModel.DataAnnotations;

namespace EcommerceAPI.Models
{
    public class PaymentTransaction : AuditableEntity
    {
        [Key] public Guid Id { get; set; }
        public Guid OrderId { get; set; }
        [Required, StringLength(50)] public string PaymentMethod { get; set; } = string.Empty;
        public string? ProviderTransactionId { get; set; }
        public decimal Amount { get; set; }
        [StringLength(50)] public string Status { get; set; } = "Pending";
    }
}
