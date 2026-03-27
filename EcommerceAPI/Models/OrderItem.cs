using System.ComponentModel.DataAnnotations;

namespace EcommerceAPI.Models
{
    public class OrderItem : AuditableEntity
    {
        [Key] public Guid Id { get; set; }
        public Guid OrderId { get; set; }
        public Guid VariantId { get; set; }
        [Required, StringLength(200)] public string ProductName { get; set; } = string.Empty;
        public int Quantity { get; set; }
        public decimal UnitPrice { get; set; }
    }
}
