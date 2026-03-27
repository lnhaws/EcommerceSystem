using System.ComponentModel.DataAnnotations;

namespace EcommerceAPI.Models
{
    // --- ORDERING ---
    public class Order : AuditableEntity
    {
        [Key] public Guid Id { get; set; }
        public Guid UserId { get; set; }
        public Guid ShippingAddressId { get; set; }
        public decimal TotalAmount { get; set; }
        public decimal ShippingFee { get; set; }
        public decimal DiscountAmount { get; set; }
        public decimal FinalAmount { get; set; }
        [StringLength(50)] public string Status { get; set; } = "Pending";
        [StringLength(100)] public string? TrackingCode { get; set; }
    }
}
