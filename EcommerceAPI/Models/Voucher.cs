using System.ComponentModel.DataAnnotations;

namespace EcommerceAPI.Models
{
    // --- PROMOTION ---
    public class Voucher : AuditableEntity
    {
        [Key] public Guid Id { get; set; }
        [Required, StringLength(50)] public string Code { get; set; } = string.Empty;
        [StringLength(50)] public string DiscountType { get; set; } = "Percentage";
        public decimal DiscountValue { get; set; }
        public decimal MinOrderValue { get; set; }
        public decimal MaxDiscount { get; set; }
        public int MaxUses { get; set; }
        public int UsedCount { get; set; }
        public DateTime ExpiryDate { get; set; }
    }
}
