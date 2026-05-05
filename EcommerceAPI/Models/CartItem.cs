using System.ComponentModel.DataAnnotations;

namespace EcommerceAPI.Models
{
    public class CartItem : AuditableEntity
    {
        [Key] public Guid Id { get; set; }
        public Guid CartId { get; set; }
        public Guid VariantId { get; set; } // Món hàng khách chọn (Màu sắc, kích cỡ...)
        public int Quantity { get; set; }
    }
}