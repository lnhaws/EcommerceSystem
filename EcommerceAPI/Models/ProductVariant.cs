using System.ComponentModel.DataAnnotations;

namespace EcommerceAPI.Models
{
    public class ProductVariant : AuditableEntity
    {
        [Key] public Guid Id { get; set; }
        public Guid ProductId { get; set; }
        [Required, StringLength(50)] public string SKU_Code { get; set; } = string.Empty;
        [StringLength(50)] public string? Color { get; set; }
        [StringLength(50)] public string? Size { get; set; }
        public decimal Price { get; set; }
        public int StockQuantity { get; set; }
    }
}
